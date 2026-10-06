using ComputerRepairSystem.company.Entities;

public class Repair
{
    public int RepairId { get; set; }

    public int ServiceRequestId { get; set; }
    public int? TechnicianId { get; set; }
    public string? Diagnosis { get; set; }
    public string? RepairDescription { get; set; }
    public string Status { get; set; } = "Pending";

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public ServiceRequest ServiceRequest { get; set; } = null!;
    public Employee? Technician { get; set; } = null!;
    public ICollection<RepairItem> RepairItems { get; set; }
        = new List<RepairItem>();

    public Invoice? Invoice { get; set; }
}