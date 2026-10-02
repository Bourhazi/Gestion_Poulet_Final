namespace Poulet.Domain;

public abstract class Entity { public int Id { get; set; } }
public sealed class Supplier : Entity { public string Name { get; set; } = ""; public string? Phone { get; set; } }
public sealed class Client : Entity { public string Name { get; set; } = ""; public string? Phone { get; set; } public string Type { get; set; } = "grossiste"; }
public sealed class Chamber : Entity { public string Name { get; set; } = ""; public decimal Capacity { get; set; } public bool IsSouk { get; set; } }
public sealed class Purchase : Entity
{
    public int? SupplierId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ChickenType { get; set; } = "normal";
    public decimal? DepartureWeight { get; set; }
    public decimal? ActualWeight { get; set; }
    public string? Notes { get; set; }
    public List<Allocation> Allocations { get; set; } = [];
}
public sealed class Allocation : Entity { public int PurchaseId { get; set; } public int? ChamberId { get; set; } public int? ClientId { get; set; } public decimal Quantity { get; set; } }
public sealed class Feed : Entity { public int ChamberId { get; set; } public DateOnly Date { get; set; } public decimal Quantity { get; set; } public decimal Cost { get; set; } public string? Notes { get; set; } }
public sealed class Sale : Entity
{
    public DateOnly Date { get; set; }
    public string Type { get; set; } = "grossiste";
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string ChickenType { get; set; } = "normal";
    public string Mode { get; set; } = "vivant";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int Pieces { get; set; }
    public decimal CrateCost { get; set; }
    public decimal CostOfGoods { get; set; }
    public string? Notes { get; set; }
    public List<SaleAllocation> Allocations { get; set; } = [];
    public List<MondayLine> Lines { get; set; } = [];
}
public sealed class SaleAllocation : Entity { public int SaleId { get; set; } public int ChamberId { get; set; } public decimal Quantity { get; set; } }
public sealed class MondayLine : Entity
{
    public int SaleId { get; set; }
    public string? ClientName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool Paid { get; set; }
    public string Mode { get; set; } = "vivant";
    public string? Notes { get; set; }
    public List<ChickenPiece> Pieces { get; set; } = [];
}
public sealed class ChickenPiece : Entity { public int MondayLineId { get; set; } public string Number { get; set; } = ""; }
public sealed class ProductPurchase : Entity
{
    public string Product { get; set; } = "olive";
    public string Variety { get; set; } = "noire";
    public int? SupplierId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Quantity { get; set; } // kg for olives; individual eggs for eggs
    public decimal UnitPrice { get; set; } // price per kg or per individual egg
    public int EggsPerTray { get; set; } = 30;
    public string? Notes { get; set; }
}
public sealed class ProductSale : Entity
{
    public string Product { get; set; } = "olive";
    public string Variety { get; set; } = "noire";
    public DateOnly Date { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Mode { get; set; } = "kg";
    public int EggsPerTray { get; set; } = 30;
    public decimal CostOfGoods { get; set; }
    public string? Notes { get; set; }
}
public sealed class User : Entity { public string Username { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string Role { get; set; } = "grossiste"; public int? ClientId { get; set; } }
public sealed class BusinessException(string message) : Exception(message);

public static class Rules
{
    public static void Require(bool condition, string message) { if (!condition) throw new BusinessException(message); }
    public static void Positive(decimal value, string name) => Require(value > 0, $"{name} must be greater than zero.");
    public static void NonNegative(decimal value, string name) => Require(value >= 0, $"{name} cannot be negative.");
    public static void Name(string name) => Require(!string.IsNullOrWhiteSpace(name) && name.Length <= 120, "Name is required (maximum 120 characters).");
    public static void Phone(string? phone) => Require(string.IsNullOrEmpty(phone) || phone.Length == 10 && phone.All(char.IsAsciiDigit), "Phone must contain exactly 10 digits.");
    public static void Chicken(string type) => Require(type is "normal" or "bibi", "Invalid chicken type.");
    public static void Product(string product, string variety)
    {
        Require(product is "olive" or "egg", "Invalid product.");
        Require(product == "egg" ? variety == "egg" : variety is "noire" or "verte" or "mchermel" or "hroure", "Invalid variety.");
    }
}
