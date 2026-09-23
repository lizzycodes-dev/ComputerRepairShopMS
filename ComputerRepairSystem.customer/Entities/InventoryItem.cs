namespace ComputerRepairSystem.company.Entities;

public class InventoryItem
{
    public int ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    public string Unit { get; set; } = "Piece";

    public decimal UnitCost { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ReorderLevel { get; set; }

    public int? SupplierId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public Supplier? Supplier { get; set; }
    public ICollection<RepairItem> RepairItems { get; set; }
        = new List<RepairItem>();

    public ICollection<Inventory> Inventories { get; set; }
        = new List<Inventory>();
}