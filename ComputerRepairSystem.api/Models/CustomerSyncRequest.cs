using ComputerRepairSystem.company.Entities;

namespace ComputerRepairSystem.api.Models;

public class CustomerSyncRequest
{
    public int CompanyId { get; set; }

    public string Operation { get; set; } = string.Empty;

    public Customer? Customer { get; set; }

    public int? CustomerId { get; set; }
}