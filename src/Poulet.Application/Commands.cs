using MediatR;
using Poulet.Domain;

namespace Poulet.Application;

public sealed record SaveSupplier(int Id, string Name, string? Phone) : IRequest<int>, ICommand;
public sealed record SaveClient(int Id, string Name, string? Phone, string Type) : IRequest<int>, ICommand;
public sealed record SaveChamber(int Id, string Name, decimal Capacity) : IRequest<int>, ICommand;
public sealed record AllocationInput(int? ChamberId, int? ClientId, decimal Quantity);
public sealed record SavePurchase(int Id, int? SupplierId, DateOnly Date, decimal Quantity, decimal UnitPrice, string ChickenType, decimal? DepartureWeight, decimal? ActualWeight, string? Notes, List<AllocationInput> Allocations) : IRequest<int>, ICommand;
public sealed record AddFeed(int ChamberId, DateOnly Date, decimal Quantity, string Unit, decimal UnitPrice, string? FeedType, string? Notes) : IRequest<int>, ICommand;
public sealed record AddSale(DateOnly Date, string Type, int? ClientId, string? ClientName, int? ChamberId, string ChickenType, string Mode, decimal Quantity, decimal UnitPrice, int Pieces, decimal CrateCost, string? Notes) : IRequest<int>, ICommand;
public sealed record AddMondayLine(int SaleId, string? ClientName, decimal Quantity, decimal UnitPrice, bool Paid, int PieceCount, string Mode, List<string> Numbers, string? Notes) : IRequest<int>, ICommand;
public sealed record MondayTransferInput(int ChamberId, decimal Quantity);
public sealed record TransferMondayStock(int SaleId, DateOnly Date, string? Notes, List<MondayTransferInput> Transfers) : IRequest<int>, ICommand;
public sealed record TogglePayment(int SaleId, int LineId) : IRequest<bool>, ICommand;
public sealed record SetSalePaymentStatus(int SaleId, string PaymentStatus) : IRequest<string>, ICommand;
public sealed record DeleteEntity(string Kind, int Id, int? ParentId = null) : IRequest<bool>, ICommand;
public sealed record Login(string Username, string Password) : IRequest<UserDto>;
public sealed record AddUser(string Username, string Password, string Role, int? ClientId) : IRequest<int>, ICommand;
public sealed record ResetPassword(int Id, string Password) : IRequest<bool>, ICommand;

public sealed class MasterDataHandlers(IRepository db) : IRequestHandler<SaveSupplier, int>, IRequestHandler<SaveClient, int>, IRequestHandler<SaveChamber, int>, IRequestHandler<AddFeed, int>
{
    public async Task<int> Handle(SaveSupplier r, CancellationToken ct)
    {
        Rules.Name(r.Name); Rules.Phone(r.Phone);
        var e = r.Id == 0 ? new Supplier() : await db.Find<Supplier>(r.Id, ct) ?? throw new BusinessException("Supplier not found.");
        e.Name = r.Name.Trim(); e.Phone = r.Phone; if (r.Id == 0) db.Add(e); await db.Save(ct); return e.Id;
    }
    public async Task<int> Handle(SaveClient r, CancellationToken ct)
    {
        Rules.Name(r.Name); Rules.Phone(r.Phone); Rules.Require(r.Type is "grossiste" or "detail", "Invalid client type.");
        var e = r.Id == 0 ? new Client() : await db.Find<Client>(r.Id, ct) ?? throw new BusinessException("Client not found.");
        if (r.Type != "grossiste") Rules.Require(!(await db.List<User>(ct)).Any(u => u.ClientId == r.Id && u.Role == "grossiste"), "This client is linked to a grossiste account.");
        e.Name = r.Name.Trim(); e.Phone = r.Phone; e.Type = r.Type; if (r.Id == 0) db.Add(e); await db.Save(ct); return e.Id;
    }
    public async Task<int> Handle(SaveChamber r, CancellationToken ct)
    {
        Rules.Name(r.Name); Rules.NonNegative(r.Capacity, "Capacity");
        Rules.Require(!(await db.List<Chamber>(ct)).Any(c => c.Id != r.Id && c.Name.Equals(r.Name.Trim(), StringComparison.OrdinalIgnoreCase)), "Chamber name already exists.");
        var e = r.Id == 0 ? new Chamber() : await db.Find<Chamber>(r.Id, ct) ?? throw new BusinessException("Chamber not found.");
        var stock = Inventory.Stock(await db.List<Purchase>(ct), await db.List<Sale>(ct), r.Id);
        Rules.Require(r.Capacity == 0 || r.Capacity >= stock, "Capacity cannot be smaller than current stock.");
        e.Name = r.Name.Trim(); e.Capacity = r.Capacity; if (r.Id == 0) db.Add(e); await db.Save(ct); return e.Id;
    }
    public async Task<int> Handle(AddFeed r, CancellationToken ct)
    {
        Rules.Require(await db.Find<Chamber>(r.ChamberId, ct) != null, "Chamber not found."); Rules.Positive(r.Quantity, "Feed quantity"); Rules.NonNegative(r.UnitPrice, "Feed unit price");
        Rules.Require(r.Unit is "kg" or "sac", "Feed unit must be kg or sac.");
        var e = new Feed { ChamberId = r.ChamberId, Date = r.Date, Quantity = r.Quantity, Unit = r.Unit, UnitPrice = r.UnitPrice, Cost = r.Quantity * r.UnitPrice, FeedType = string.IsNullOrWhiteSpace(r.FeedType) ? null : r.FeedType.Trim(), Notes = r.Notes };
        db.Add(e); await db.Save(ct); return e.Id;
    }
}
public sealed class PurchaseHandler(IRepository db) : IRequestHandler<SavePurchase, int>
{
    public async Task<int> Handle(SavePurchase r, CancellationToken ct)
    {
        Rules.Positive(r.Quantity, "Quantity"); Rules.NonNegative(r.UnitPrice, "Price"); Rules.Chicken(r.ChickenType);
        if (r.ActualWeight.HasValue) Rules.Positive(r.ActualWeight.Value, "Actual weight");
        if (r.DepartureWeight.HasValue) Rules.Positive(r.DepartureWeight.Value, "Departure weight");
        if (r.SupplierId.HasValue) Rules.Require(await db.Find<Supplier>(r.SupplierId.Value, ct) != null, "Supplier not found.");
        Rules.Require(r.Allocations is { Count: > 0 }, "Distribute the entire purchase quantity.");
        Rules.Require(r.Allocations.Sum(a => a.Quantity) == r.Quantity, "Distribute the entire purchase quantity.");
        foreach (var a in r.Allocations)
        {
            Rules.Positive(a.Quantity, "Allocation"); Rules.Require(a.ChamberId.HasValue != a.ClientId.HasValue, "Choose either a chamber or an external client.");
            if (a.ChamberId.HasValue) Rules.Require(await db.Find<Chamber>(a.ChamberId.Value, ct) != null, "Chamber not found.");
            if (a.ClientId.HasValue) Rules.Require(await db.Find<Client>(a.ClientId.Value, ct) != null, "Client not found.");
        }
        var e = r.Id == 0 ? new Purchase() : await db.Find<Purchase>(r.Id, ct) ?? throw new BusinessException("Purchase not found.");
        var other = (await db.List<Purchase>(ct)).Where(p => p.Id != r.Id).ToList();
        var sales = await db.List<Sale>(ct);
        foreach (var c in await db.List<Chamber>(ct))
        {
            foreach (var type in new[] { "normal", "bibi" })
            {
                var stock = Inventory.Stock(other, sales, c.Id, type) + (r.ChickenType == type ? r.Allocations.Where(a => a.ChamberId == c.Id).Sum(a => a.Quantity) : 0);
                Rules.Require(stock >= 0, "This edit would remove stock already sold.");
            }
            var total = Inventory.Stock(other, sales, c.Id) + r.Allocations.Where(a => a.ChamberId == c.Id).Sum(a => a.Quantity);
            Rules.Require(c.Capacity == 0 || total <= c.Capacity, $"Capacity exceeded in {c.Name}.");
        }
        e.SupplierId = r.SupplierId; e.Date = r.Date; e.Quantity = r.Quantity; e.UnitPrice = r.UnitPrice; e.ChickenType = r.ChickenType; e.DepartureWeight = r.DepartureWeight; e.ActualWeight = r.ActualWeight; e.Notes = r.Notes;
        foreach (var a in e.Allocations.ToList()) db.Remove(a);
        e.Allocations = r.Allocations.Select(a => new Allocation { ChamberId = a.ChamberId, ClientId = a.ClientId, Quantity = a.Quantity }).ToList();
        if (r.Id == 0) db.Add(e); await db.Save(ct); return e.Id;
    }
}
public sealed class SaleHandlers(IRepository db, ICurrentUser user) : IRequestHandler<AddSale, int>, IRequestHandler<AddMondayLine, int>, IRequestHandler<TogglePayment, bool>, IRequestHandler<TransferMondayStock, int>, IRequestHandler<SetSalePaymentStatus, string>
{
    public async Task<int> Handle(AddSale r, CancellationToken ct)
    {
        Rules.Positive(r.Quantity, "Quantity"); Rules.NonNegative(r.UnitPrice, "Price"); Rules.NonNegative(r.CrateCost, "Crate cost"); Rules.Chicken(r.ChickenType);
        Rules.Require(r.Type is "grossiste" or "detail" or "lundi", "Invalid sale type."); Rules.Require(r.Mode is "vivant" or "madbouh", "Invalid mode.");
        if (r.ClientId.HasValue) Rules.Require((await db.Find<Client>(r.ClientId.Value, ct))?.Type == r.Type, "Choose a client matching the sale type.");
        var purchases = await db.List<Purchase>(ct); var sales = await db.List<Sale>(ct); var chambers = await db.List<Chamber>(ct);
        var e = new Sale { Date = r.Date, Type = r.Type, ClientId = r.ClientId, ClientName = r.ClientName, ChickenType = r.ChickenType, Mode = r.Mode, Quantity = r.Quantity, UnitPrice = r.UnitPrice, Pieces = r.Pieces, CrateCost = r.CrateCost, Notes = r.Notes };
        if (r.Type == "lundi")
        {
            Rules.Require(r.Date.DayOfWeek == DayOfWeek.Monday, "Monday lots must have a Monday date."); Rules.Require(r.Pieces > 0, "Number of pieces must be positive.");
            e.ClientId = null; e.ClientName = null; e.UnitPrice = 0;
            var remaining = r.Quantity;
            foreach (var chamber in chambers.OrderByDescending(c => c.IsSouk).ThenBy(c => c.Name))
            {
                var taken = Math.Min(remaining, Inventory.Stock(purchases, sales, chamber.Id, r.ChickenType));
                if (taken > 0) { e.Allocations.Add(new() { ChamberId = chamber.Id, Quantity = taken }); e.CostOfGoods += taken * Inventory.AverageCost(purchases, chamber.Id, r.ChickenType); remaining -= taken; }
                if (remaining == 0) break;
            }
            Rules.Require(remaining == 0, "Insufficient stock for this chicken type.");
        }
        else
        {
            Rules.Require(r.ChamberId.HasValue && chambers.Any(c => c.Id == r.ChamberId), "Choose a chamber.");
            var chamberId = r.ChamberId!.Value;
            Rules.Require(r.Quantity <= Inventory.Stock(purchases, sales, chamberId, r.ChickenType), "Insufficient stock for this chicken type.");
            e.Allocations.Add(new() { ChamberId = chamberId, Quantity = r.Quantity }); e.CostOfGoods = r.Quantity * Inventory.AverageCost(purchases, chamberId, r.ChickenType);
        }
        db.Add(e); await db.Save(ct); return e.Id;
    }
    public async Task<int> Handle(AddMondayLine r, CancellationToken ct)
    {
        var e = await db.Find<Sale>(r.SaleId, ct) ?? throw new BusinessException("Sale not found.");
        Rules.Require(e.Type == "lundi", "This is not a Monday lot."); Rules.Positive(r.Quantity, "Quantity"); Rules.NonNegative(r.UnitPrice, "Price"); Rules.Require(r.PieceCount > 0, "Number of pieces must be positive."); Rules.Require(r.Mode is "vivant" or "madbouh", "Invalid mode.");
        Rules.Require(r.Numbers is { Count: > 0 } && r.Numbers.All(n => !string.IsNullOrWhiteSpace(n) && n.Length <= 80), "Enter chicken numbers.");
        var numbers = r.Numbers.Select(n => n.Trim()).ToList();
        Rules.Require(r.PieceCount >= 5 ? numbers.Count == 1 : numbers.Count == r.PieceCount, "Enter one order number for 5+ pieces, or one chicken number per piece.");
        Rules.Require(numbers.Distinct(StringComparer.OrdinalIgnoreCase).Count() == numbers.Count && !e.Lines.SelectMany(l => l.Pieces).Any(p => numbers.Contains(p.Number, StringComparer.OrdinalIgnoreCase)), "Chicken numbers must be unique within this lot.");
        Rules.Require(e.Lines.Sum(l => l.PieceCount) + r.PieceCount <= e.Pieces, "Not enough pieces remaining.");
        Rules.Require(e.Lines.Sum(l => l.Quantity) + r.Quantity <= e.Quantity, "Not enough kilograms remaining.");
        var line = new MondayLine { ClientName = r.ClientName, Quantity = r.Quantity, UnitPrice = r.UnitPrice, PieceCount = r.PieceCount, Paid = r.Paid, Mode = r.Mode, Notes = r.Notes, Pieces = numbers.Select(n => new ChickenPiece { Number = n }).ToList() };
        e.Lines.Add(line); await db.Save(ct); return line.Id;
    }
    public async Task<bool> Handle(TogglePayment r, CancellationToken ct)
    {
        var e = await db.Find<Sale>(r.SaleId, ct) ?? throw new BusinessException("Sale not found."); var line = e.Lines.SingleOrDefault(l => l.Id == r.LineId) ?? throw new BusinessException("Line not found.");
        line.Paid = !line.Paid; await db.Save(ct); return line.Paid;
    }
    public async Task<string> Handle(SetSalePaymentStatus r, CancellationToken ct)
    {
        Rules.Require(r.PaymentStatus is "PAID" or "UNPAID", "Invalid payment status.");
        var sale = await db.Find<Sale>(r.SaleId, ct) ?? throw new BusinessException("Sale not found.");
        Rules.Require(sale.Type != "lundi", "Manage payment per order in a Monday lot.");
        var previous = sale.PaymentStatus;
        sale.PaymentStatus = r.PaymentStatus;
        db.Add(new AuditLog { Date = DateTimeOffset.UtcNow, UserId = user.Id == 0 ? null : user.Id, Action = "Updated", Module = "Sale", EntityType = "Sale", EntityId = sale.Id, OldValues = $"{{\"paymentStatus\":\"{previous}\"}}", NewValues = $"{{\"paymentStatus\":\"{sale.PaymentStatus}\",\"client\":\"{sale.ClientName ?? sale.ClientId?.ToString() ?? "Unknown"}\"}}", Description = $"Sale #{sale.Id} payment status changed from {previous} to {sale.PaymentStatus}.", IpAddress = user.IpAddress });
        await db.Save(ct); return sale.PaymentStatus;
    }
    public async Task<int> Handle(TransferMondayStock r, CancellationToken ct)
    {
        var sale = await db.Find<Sale>(r.SaleId, ct) ?? throw new BusinessException("Monday lot not found.");
        Rules.Require(sale.Type == "lundi", "This is not a Monday lot.");
        Rules.Require(r.Transfers is { Count: > 0 }, "Choose at least one destination chamber.");
        foreach (var transfer in r.Transfers) { Rules.Positive(transfer.Quantity, "Transfer quantity"); Rules.Require(await db.Find<Chamber>(transfer.ChamberId, ct) != null, "Chamber not found."); }
        var sold = sale.Lines.Sum(l => l.Quantity);
        var alreadyTransferred = sale.Transfers.Sum(t => t.Quantity);
        var remaining = sale.Quantity - sold - alreadyTransferred;
        Rules.Require(remaining > 0, "No remaining Monday stock to transfer.");
        Rules.Require(r.Transfers.Sum(t => t.Quantity) == remaining, "Transfer the entire remaining Monday stock.");
        foreach (var chamber in await db.List<Chamber>(ct))
        {
            var added = r.Transfers.Where(t => t.ChamberId == chamber.Id).Sum(t => t.Quantity);
            Rules.Require(chamber.Capacity == 0 || Inventory.Stock(await db.List<Purchase>(ct), await db.List<Sale>(ct), chamber.Id) + added <= chamber.Capacity, $"Capacity exceeded in {chamber.Name}.");
        }
        var unitCost = sale.Quantity == 0 ? 0 : sale.CostOfGoods / sale.Quantity;
        sale.Transfers.AddRange(r.Transfers.Select(t => new MondayTransfer { Date = r.Date, ChamberId = t.ChamberId, Quantity = t.Quantity, ChickenType = sale.ChickenType, UnitCost = unitCost, UserId = user.Id, Notes = r.Notes }));
        await db.Save(ct); return sale.Transfers.Count;
    }
}
public sealed class UserHandlers(IRepository db, IPasswords passwords) : IRequestHandler<Login, UserDto>, IRequestHandler<AddUser, int>, IRequestHandler<ResetPassword, bool>
{
    public async Task<UserDto> Handle(Login r, CancellationToken ct)
    {
        Rules.Require(!string.IsNullOrWhiteSpace(r.Username) && !string.IsNullOrEmpty(r.Password), "Incorrect username or password.");
        var u = (await db.List<User>(ct)).SingleOrDefault(u => u.Username.Equals(r.Username.Trim(), StringComparison.OrdinalIgnoreCase));
        if (u == null || !passwords.Verify(u.PasswordHash, r.Password)) throw new BusinessException("Incorrect username or password.");
        return new(u.Id, u.Username, u.Role, u.ClientId);
    }
    public async Task<int> Handle(AddUser r, CancellationToken ct)
    {
        Rules.Name(r.Username); Rules.Require(r.Password is { Length: >= 8 }, "Password must contain at least 8 characters."); Rules.Require(r.Role is "admin" or "grossiste", "Invalid role.");
        Rules.Require(!(await db.List<User>(ct)).Any(u => u.Username.Equals(r.Username.Trim(), StringComparison.OrdinalIgnoreCase)), "Username already exists.");
        if (r.Role == "grossiste") Rules.Require(r.ClientId.HasValue && (await db.Find<Client>(r.ClientId.Value, ct))?.Type == "grossiste", "Link this account to a grossiste client.");
        var u = new User { Username = r.Username.Trim(), PasswordHash = passwords.Hash(r.Password), Role = r.Role, ClientId = r.Role == "admin" ? null : r.ClientId }; db.Add(u); await db.Save(ct); return u.Id;
    }
    public async Task<bool> Handle(ResetPassword r, CancellationToken ct)
    {
        Rules.Require(r.Password is { Length: >= 8 }, "Password must contain at least 8 characters."); var u = await db.Find<User>(r.Id, ct) ?? throw new BusinessException("User not found."); u.PasswordHash = passwords.Hash(r.Password); u.RefreshTokenHash = null; u.RefreshTokenExpiresAt = null; await db.Save(ct); return true;
    }
}
