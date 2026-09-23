using ComputerRepairSystem.customer.Entities;
using System;
using System.Collections.Generic;

namespace ComputerRepairSystem.company.Entities;

public class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }

    public string PurchaseOrderNumber { get; set; }
        = string.Empty;

    public int SupplierId { get; set; }

    public int? BranchId { get; set; }

    public DateTime PurchaseDate { get; set; }

    public string Status { get; set; }
        = "Received";

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public Supplier? Supplier { get; set; }

    public Branch? Branch { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; }
        = new List<PurchaseOrderItem>();
}