namespace ComputerRepairSystem.domain.Entities;

public class SubscriptionPlan
{
    public int SubscriptionPlanId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public string EnterpriseType { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationInDays { get; set; }

    public bool IsActive { get; set; }

    // Display properties
    public string PriceText => $"₱{Price:N2}";

    public string DurationText => $"{DurationInDays} days";

    public ICollection<SubscriptionPlanModule> SubscriptionPlanModules { get; set; }
        = new List<SubscriptionPlanModule>();

    public ICollection<Subscription> Subscriptions { get; set; }
        = new List<Subscription>();
}