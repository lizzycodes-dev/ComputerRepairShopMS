namespace ComputerRepairSystem.domain.Entities;

public class TermsAndConditions
{
    public int TermsAndConditionsId { get; set; }

    public string Version { get; set; } = "1.0";

    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }
}