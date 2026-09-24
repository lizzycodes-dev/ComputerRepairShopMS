namespace ComputerRepairSystem.company.Entities;

public class Expense
{
    public int ExpenseId { get; set; }

    public int? BranchId { get; set; }

    public string Category { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }

    public string? Description { get; set; }

    public Branch? Branch { get; set; }

    public int? PayrollId { get; set; }
    public Payroll? Payroll { get; set; }
}