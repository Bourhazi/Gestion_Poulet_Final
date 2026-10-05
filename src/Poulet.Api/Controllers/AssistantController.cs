using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Poulet.Application;
using Poulet.Domain;

namespace Poulet.Api.Controllers;

[ApiController]
[Route("api/assistant")]
[Authorize(Policy = "admin")]
[EnableRateLimiting("assistant")]
public sealed class AssistantController(IRepository db, IConfiguration configuration, IHttpClientFactory httpClients) : ControllerBase
{
    private const string Endpoint = "https://api.openai.com/v1/responses";

    [HttpPost("chat")]
    public async Task<ActionResult<AssistantReply>> Chat([FromBody] AssistantQuestion request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question) || request.Question.Length > 1_000)
            return BadRequest(new { message = "Veuillez saisir une question de 1 à 1000 caractères." });

        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(503, new { message = "L'assistant n'est pas configuré. Ajoutez la clé OpenAI__ApiKey sur le serveur." });

        var client = httpClients.CreateClient("openai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var model = configuration["OpenAI:Model"] ?? "gpt-6-astra";
        var initial = await Send(client, new
        {
            model,
            instructions = $"Tu es l'assistant d'analyse d'une gestion de poulets. Réponds en français, même si la question est en arabe ou darija. Aujourd'hui est le {DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}. Pour chaque question sur les données, appelle exactement une des fonctions disponibles avant de répondre. N'invente jamais de chiffres. Explique brièvement la période et précise que le bénéfice net est une estimation opérationnelle.",
            input = request.Question.Trim(),
            tools = Tools
        }, ct);

        var call = initial["output"]?.AsArray().FirstOrDefault(item => item?["type"]?.GetValue<string>() == "function_call");
        if (call is null)
            return Ok(new AssistantReply(ReadText(initial), Array.Empty<string>()));

        var name = call["name"]?.GetValue<string>() ?? "";
        var arguments = call["arguments"]?.GetValue<string>() ?? "{}";
        var data = await RunTool(name, arguments, ct);
        var final = await Send(client, new
        {
            model,
            previous_response_id = initial["id"]?.GetValue<string>(),
            input = new[] { new { type = "function_call_output", call_id = call["call_id"]?.GetValue<string>(), output = JsonSerializer.Serialize(data) } }
        }, ct);
        return Ok(new AssistantReply(ReadText(final), new[] { name }));
    }

    private async Task<JsonNode> RunTool(string name, string rawArguments, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(rawArguments);
        var args = document.RootElement;
        var from = Date(args, "from", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1 - DateTime.UtcNow.Day)));
        var to = Date(args, "to", DateOnly.FromDateTime(DateTime.UtcNow));
        if (from > to) throw new BusinessException("La période demandée est invalide.");
        var chickenType = args.TryGetProperty("chicken_type", out var type) ? type.GetString()?.Trim() : null;

        return name switch
        {
            "get_purchase_summary" => await Purchases(from, to, chickenType, ct),
            "get_sales_summary" => await Sales(from, to, chickenType, ct),
            "get_financial_summary" => await Financials(from, to, ct),
            "get_feed_summary" => await Feed(from, to, ct),
            "get_stock_summary" => await Stock(chickenType, ct),
            _ => throw new BusinessException("Outil d'analyse non autorisé.")
        };
    }

    private async Task<JsonNode> Purchases(DateOnly from, DateOnly to, string? chickenType, CancellationToken ct)
    {
        var purchases = (await db.List<Purchase>(ct)).Where(p => p.Date >= from && p.Date <= to && (string.IsNullOrWhiteSpace(chickenType) || p.ChickenType.Equals(chickenType, StringComparison.OrdinalIgnoreCase))).ToList();
        return JsonSerializer.SerializeToNode(new { from, to, chickenType, purchaseCount = purchases.Count, quantityKg = purchases.Sum(p => p.Quantity), amount = purchases.Sum(p => (p.ActualWeight ?? p.Quantity) * p.UnitPrice) })!;
    }

    private async Task<JsonNode> Sales(DateOnly from, DateOnly to, string? chickenType, CancellationToken ct)
    {
        var sales = (await db.List<Sale>(ct)).Where(s => s.Date >= from && s.Date <= to && (string.IsNullOrWhiteSpace(chickenType) || s.ChickenType.Equals(chickenType, StringComparison.OrdinalIgnoreCase))).ToList();
        return JsonSerializer.SerializeToNode(new { from, to, chickenType, saleCount = sales.Count, quantityKg = sales.Sum(s => s.Type == "lundi" ? s.Lines.Sum(l => l.Quantity) : s.Quantity), revenue = sales.Sum(Inventory.Revenue), paid = sales.Sum(s => s.Type == "lundi" ? s.Lines.Where(l => l.Paid).Sum(l => l.Quantity * l.UnitPrice) : s.PaymentStatus == "PAID" ? Inventory.Revenue(s) : 0), unpaid = sales.Sum(s => s.Type == "lundi" ? s.Lines.Where(l => !l.Paid).Sum(l => l.Quantity * l.UnitPrice) : s.PaymentStatus == "UNPAID" ? Inventory.Revenue(s) : 0) })!;
    }

    private async Task<JsonNode> Financials(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var purchases = await db.List<Purchase>(ct); var sales = (await db.List<Sale>(ct)).Where(s => s.Date >= from && s.Date <= to).ToList(); var feed = await db.List<Feed>(ct);
        var revenue = sales.Sum(Inventory.Revenue); var cogs = sales.Sum(Inventory.SoldCost); var feedCost = Inventory.FeedCostForSales(purchases, feed, sales); var crates = sales.Sum(s => s.CrateCost);
        var losses = purchases.Where(p => p.Date >= from && p.Date <= to).Sum(p => p.DepartureWeight.HasValue && p.ActualWeight.HasValue ? Math.Max(p.DepartureWeight.Value - p.ActualWeight.Value, 0) * p.UnitPrice : 0);
        return JsonSerializer.SerializeToNode(new { from, to, revenue, costOfGoods = cogs, feedCost, crateCost = crates, losses, grossProfit = revenue - cogs, netProfit = revenue - cogs - feedCost - crates - losses })!;
    }

    private async Task<JsonNode> Feed(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var entries = (await db.List<Poulet.Domain.Feed>(ct)).Where(f => f.Date >= from && f.Date <= to).ToList();
        return JsonSerializer.SerializeToNode(new { from, to, entryCount = entries.Count, quantity = entries.Sum(f => f.Quantity), cost = entries.Sum(f => f.Cost) })!;
    }

    private async Task<JsonNode> Stock(string? chickenType, CancellationToken ct)
    {
        var purchases = await db.List<Purchase>(ct); var sales = await db.List<Sale>(ct); var chambers = await db.List<Chamber>(ct);
        var types = string.IsNullOrWhiteSpace(chickenType) ? new[] { "normal", "bibi" } : new[] { chickenType };
        return JsonSerializer.SerializeToNode(new { stock = chambers.Select(c => new { c.Name, types = types.Select(t => new { chickenType = t, quantityKg = Inventory.Stock(purchases, sales, c.Id, t) }) }) })!;
    }

    private static DateOnly Date(JsonElement args, string key, DateOnly fallback) => args.TryGetProperty(key, out var value) && DateOnly.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : fallback;
    private static async Task<JsonNode> Send(HttpClient client, object payload, CancellationToken ct)
    {
        using var response = await client.PostAsync(Endpoint, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new BusinessException("Le service d'assistant est indisponible. Vérifiez sa configuration et réessayez.");
        return JsonNode.Parse(body) ?? throw new BusinessException("Réponse assistant invalide.");
    }
    private static string ReadText(JsonNode response) => response["output_text"]?.GetValue<string>() ?? "Je n'ai pas pu formuler une réponse à partir des données disponibles.";

    private static readonly object[] Tools =
    [
        Tool("get_purchase_summary", "Résumé des achats de poulets pour une période et éventuellement un type.", true),
        Tool("get_sales_summary", "Résumé des ventes, chiffre d'affaires et paiements pour une période.", true),
        Tool("get_financial_summary", "Chiffre d'affaires, coûts et bénéfice net estimé pour une période.", false),
        Tool("get_feed_summary", "Résumé des achats d'aliment pour une période.", false),
        Tool("get_stock_summary", "Stock actuel par chambre et type de poulet.", false)
    ];
    private static object Tool(string name, string description, bool chickenType) => new { type = "function", name, description, strict = true, parameters = new { type = "object", properties = chickenType ? new Dictionary<string, object> { ["from"] = new { type = "string", description = "Date ISO YYYY-MM-DD" }, ["to"] = new { type = "string", description = "Date ISO YYYY-MM-DD" }, ["chicken_type"] = new { type = "string", description = "Type exact si demandé, sinon chaîne vide" } } : new Dictionary<string, object> { ["from"] = new { type = "string", description = "Date ISO YYYY-MM-DD" }, ["to"] = new { type = "string", description = "Date ISO YYYY-MM-DD" } }, required = chickenType ? new[] { "from", "to", "chicken_type" } : new[] { "from", "to" }, additionalProperties = false } };
}

public sealed record AssistantQuestion(string Question);
public sealed record AssistantReply(string Answer, string[] Sources);
