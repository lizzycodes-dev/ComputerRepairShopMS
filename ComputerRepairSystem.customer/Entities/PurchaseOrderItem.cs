namespace ComputerRepairSystem.company.Entities;

public class PurchaseOrderItem
{
    public int PurchaseOrderItemId { get; set; }

    public int PurchaseOrderId { get; set; }

    public int ItemId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public InventoryItem? Item { get; set; }
}