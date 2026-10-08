using ComputerRepairSystem.company.Interfaces;

namespace ComputerRepairSystem.api.Services;

public class CompanyContext : ICompanyContext
{
    public int? CompanyId { get; set; }
}