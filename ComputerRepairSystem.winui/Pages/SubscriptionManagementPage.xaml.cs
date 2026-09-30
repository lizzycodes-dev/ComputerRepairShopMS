using ComputerRepairSystem.domain.Entities;
using ComputerRepairSystem.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class SubscriptionManagementPage : Page
{
    private readonly MasterErpDbContext _masterDb;

    private readonly ObservableCollection<SubscriptionPlan> _plans = new();

    private readonly ObservableCollection<ModuleSelectionItem> _modules = new();

    private readonly ObservableCollection<SubscriptionDisplayItem> _subscriptions = new();


    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public SubscriptionManagementPage(
        MasterErpDbContext masterDb)
    {
        this.InitializeComponent();

        _masterDb = masterDb;

        PlansListView.ItemsSource = _plans;

        ModulesListView.ItemsSource = _modules;

        CompanySubscriptionsListView.ItemsSource =
            _subscriptions;

        Loaded += SubscriptionManagementPage_Loaded;
    }


    // ==========================================
    // PAGE LOADED
    // ==========================================

    private async void SubscriptionManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadPlansAsync();

        await LoadCompanySubscriptionsAsync();
    }


    // ==========================================
    // LOAD PLANS
    // ==========================================

    private async Task LoadPlansAsync()
    {
        _plans.Clear();

        var plans =
            await _masterDb.SubscriptionPlans
                .OrderBy(x => x.Price)
                .ThenBy(x => x.PlanName)
                .ToListAsync();

        foreach (var plan in plans)
        {
            _plans.Add(plan);
        }
    }


    // ==========================================
    // PLAN SELECTION
    // ==========================================

    private async void PlansListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (PlansListView.SelectedItem
            is not SubscriptionPlan selectedPlan)
        {
            _modules.Clear();

            return;
        }

        await LoadModulesAsync(
            selectedPlan.SubscriptionPlanId);
    }


    // ==========================================
    // LOAD MODULES
    // ==========================================

    private async Task LoadModulesAsync(
        int subscriptionPlanId)
    {
        _modules.Clear();

        var modules =
            await _masterDb.ModuleDefinitions
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

        var includedModuleIds =
            await _masterDb.SubscriptionPlanModules
                .Where(x =>
                    x.SubscriptionPlanId ==
                    subscriptionPlanId)
                .Select(x => x.ModuleDefinitionId)
                .ToListAsync();

        foreach (var module in modules)
        {
            _modules.Add(
                new ModuleSelectionItem
                {
                    ModuleDefinitionId =
                        module.ModuleDefinitionId,

                    ModuleName =
                        module.ModuleName,

                    IsIncluded =
                        includedModuleIds.Contains(
                            module.ModuleDefinitionId)
                });
        }
    }
    private async void CancelSubscriptionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not SubscriptionDisplayItem item)
        {
            return;
        }

        var subscription = await _masterDb.Subscriptions
            .FirstOrDefaultAsync(
                x => x.SubscriptionId == item.SubscriptionId);

        if (subscription == null)
        {
            await ShowMessageAsync(
                "Subscription Not Found",
                "The subscription could not be found.");

            return;
        }

        if (subscription.Status == "Cancelled")
        {
            await ShowMessageAsync(
                "Already Cancelled",
                "This subscription has already been cancelled.");

            return;
        }

        var confirmDialog = new ContentDialog
        {
            Title = "Cancel Subscription",

            Content =
                $"Are you sure you want to cancel " +
                $"the subscription of '{item.CompanyName}'?",

            PrimaryButtonText = "Cancel Subscription",

            CloseButtonText = "Keep Subscription",

            DefaultButton = ContentDialogButton.Close,

            XamlRoot = XamlRoot
        };

        var result = await confirmDialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        subscription.Status = "Cancelled";

        await _masterDb.SaveChangesAsync();

        await LoadCompanySubscriptionsAsync();

        await ShowMessageAsync(
            "Subscription Cancelled",
            $"The subscription of '{item.CompanyName}' " +
            $"has been cancelled.");
    }

    // ==========================================
    // ADD PLAN
    // ==========================================

    private async void AddPlanButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var planNameBox = new TextBox
        {
            Header = "Plan Name",
            PlaceholderText = "Enter plan name"
        };

        var enterpriseTypeBox = new ComboBox
        {
            Header = "Enterprise Type",
            PlaceholderText = "Select enterprise type"
        };

        enterpriseTypeBox.Items.Add("Micro");
        enterpriseTypeBox.Items.Add("Small");
        enterpriseTypeBox.Items.Add("Medium");

        enterpriseTypeBox.SelectedIndex = 0;

        var priceBox = new NumberBox
        {
            Header = "Price",
            PlaceholderText = "Enter plan price",
            Minimum = 0,
            SmallChange = 100,
            SpinButtonPlacementMode =
                NumberBoxSpinButtonPlacementMode.Compact
        };


        var durationBox = new NumberBox
        {
            Header = "Duration (days)",
            PlaceholderText = "Enter duration",
            Minimum = 1,
            Value = 30,
            SpinButtonPlacementMode =
                NumberBoxSpinButtonPlacementMode.Compact
        };


        var activeCheckBox = new CheckBox
        {
            Content = "Active",
            IsChecked = true
        };


        var panel = new StackPanel
        {
            Spacing = 12
        };

        panel.Children.Add(planNameBox);
        panel.Children.Add(enterpriseTypeBox);
        panel.Children.Add(priceBox);
        panel.Children.Add(durationBox);
        panel.Children.Add(activeCheckBox);


        var dialog = new ContentDialog
        {
            Title = "Add Subscription Plan",

            Content = panel,

            PrimaryButtonText = "Add",

            CloseButtonText = "Cancel",

            DefaultButton =
                ContentDialogButton.Primary,

            XamlRoot = XamlRoot
        };


        var result =
            await dialog.ShowAsync();


        if (result != ContentDialogResult.Primary)
            return;


        var planName =
            planNameBox.Text.Trim();

        var enterpriseType = enterpriseTypeBox.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(enterpriseType))
        {
            await ShowMessageAsync(
                "Validation Error",
                "Please select an enterprise type.");
            return;
        }

        if (string.IsNullOrWhiteSpace(planName))
        {
            await ShowMessageAsync(
                "Validation Error",
                "Plan name is required.");

            return;
        }


        if (priceBox.Value < 0)
        {
            await ShowMessageAsync(
                "Validation Error",
                "Price cannot be negative.");

            return;
        }


        if (durationBox.Value < 1)
        {
            await ShowMessageAsync(
                "Validation Error",
                "Duration must be at least 1 day.");

            return;
        }


        var existingPlan =
            await _masterDb.SubscriptionPlans
                .FirstOrDefaultAsync(x =>
                    x.PlanName == planName);


        if (existingPlan != null)
        {
            await ShowMessageAsync(
                "Plan Already Exists",
                "A subscription plan with that name already exists.");

            return;
        }


        var plan = new SubscriptionPlan
        {
            PlanName = planName,

            EnterpriseType = enterpriseType,

            Price =
                (decimal)priceBox.Value,

            DurationInDays =
                (int)durationBox.Value,

            IsActive =
                activeCheckBox.IsChecked == true
        };


        _masterDb.SubscriptionPlans.Add(plan);

        await _masterDb.SaveChangesAsync();


        await LoadPlansAsync();


        await ShowMessageAsync(
            "Plan Added",
            $"Subscription plan '{plan.PlanName}' was added successfully.");
    }


    // ==========================================
    // SAVE PLAN MODULES
    // ==========================================

    private async void SaveModulesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (PlansListView.SelectedItem
            is not SubscriptionPlan selectedPlan)
        {
            await ShowMessageAsync(
                "No Plan Selected",
                "Please select a subscription plan first.");

            return;
        }


        try
        {
            var existingModules =
                await _masterDb.SubscriptionPlanModules
                    .Where(x =>
                        x.SubscriptionPlanId ==
                        selectedPlan.SubscriptionPlanId)
                    .ToListAsync();


            _masterDb.SubscriptionPlanModules
                .RemoveRange(existingModules);


            foreach (var module in
                _modules.Where(x => x.IsIncluded))
            {
                _masterDb.SubscriptionPlanModules.Add(
                    new SubscriptionPlanModule
                    {
                        SubscriptionPlanId =
                            selectedPlan.SubscriptionPlanId,

                        ModuleDefinitionId =
                            module.ModuleDefinitionId
                    });
            }


            await _masterDb.SaveChangesAsync();


            await LoadModulesAsync(
                selectedPlan.SubscriptionPlanId);


            await ShowMessageAsync(
                "Modules Saved",
                $"Modules for '{selectedPlan.PlanName}' were updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Save Failed",
                ex.Message);
        }
    }


    // ==========================================
    // LOAD COMPANY SUBSCRIPTIONS
    // ==========================================

    private async Task LoadCompanySubscriptionsAsync()
    {
        _subscriptions.Clear();

        var subscriptions = await _masterDb.Subscriptions
            .Include(x => x.Company)
            .Include(x => x.SubscriptionPlan)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        foreach (var subscription in subscriptions)
        {
            _subscriptions.Add(new SubscriptionDisplayItem
            {
                SubscriptionId = subscription.SubscriptionId,

                CompanyId = subscription.CompanyId,

                SubscriptionPlanId = subscription.SubscriptionPlanId,

                CompanyName = subscription.Company?.CompanyName
                    ?? "Unknown Company",

                PlanName = subscription.SubscriptionPlan?.PlanName
                    ?? "Unknown Plan",

                EnterpriseType = subscription.SubscriptionPlan?.EnterpriseType
                    ?? "Unknown",

                Price = subscription.SubscriptionPlan?.Price ?? 0,

                Status = subscription.Status,

                StartDate = subscription.StartDate,

                EndDate = subscription.EndDate,

                StartDateText = subscription.StartDate
                    .ToLocalTime()
                    .ToString("MMM dd, yyyy"),

                EndDateText = subscription.EndDate.HasValue
                    ? subscription.EndDate.Value
                        .ToLocalTime()
                        .ToString("MMM dd, yyyy")
                    : "No End Date"
            });
        }
    }

    private async void RenewSubscriptionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not SubscriptionDisplayItem item)
        {
            return;
        }

        var subscription = await _masterDb.Subscriptions
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(
                x => x.SubscriptionId == item.SubscriptionId);

        if (subscription == null)
        {
            await ShowMessageAsync(
                "Subscription Not Found",
                "The subscription could not be found.");
            return;
        }

        if (subscription.SubscriptionPlan == null)
        {
            await ShowMessageAsync(
                "Plan Not Found",
                "The subscription does not have a valid plan.");
            return;
        }

        var plan = subscription.SubscriptionPlan;

        var baseDate = subscription.EndDate.HasValue &&
                       subscription.EndDate.Value > DateTime.UtcNow
            ? subscription.EndDate.Value
            : DateTime.UtcNow;

        var newEndDate =
            baseDate.AddDays(plan.DurationInDays);

        var confirmDialog = new ContentDialog
        {
            Title = "Renew Subscription",
            Content =
                $"Company: {item.CompanyName}\n" +
                $"Plan: {plan.PlanName}\n" +
                $"Duration: {plan.DurationInDays} days\n\n" +
                $"New expiration date: " +
                $"{newEndDate.ToLocalTime():MMM dd, yyyy}",
            PrimaryButtonText = "Renew",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await confirmDialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        subscription.EndDate = newEndDate;
        subscription.Status = "Active";

        await _masterDb.SaveChangesAsync();

        await LoadCompanySubscriptionsAsync();

        await ShowMessageAsync(
            "Subscription Renewed",
            $"'{item.CompanyName}' has been renewed " +
            $"until {newEndDate.ToLocalTime():MMM dd, yyyy}.");
    }
    private async void ChangeSubscriptionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not SubscriptionDisplayItem item)
        {
            return;
        }

        var subscription = await _masterDb.Subscriptions
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(
                x => x.SubscriptionId == item.SubscriptionId);

        if (subscription == null)
        {
            await ShowMessageAsync(
                "Subscription Not Found",
                "The subscription could not be found.");
            return;
        }

        if (subscription.Status != "Active")
        {
            await ShowMessageAsync(
                "Cannot Change Plan",
                "Only active subscriptions can have their plan changed.");
            return;
        }

        var plans = await _masterDb.SubscriptionPlans
            .Where(x => x.IsActive)
            .OrderBy(x => x.Price)
            .ThenBy(x => x.PlanName)
            .ToListAsync();

        if (plans.Count == 0)
        {
            await ShowMessageAsync(
                "No Plans",
                "There are no active subscription plans available.");
            return;
        }

        var planBox = new ComboBox
        {
            Header = "New Subscription Plan",
            PlaceholderText = "Select a plan",
            DisplayMemberPath = "PlanName"
        };

        foreach (var plan in plans)
        {
            planBox.Items.Add(plan);
        }

        planBox.SelectedItem = plans
            .FirstOrDefault(x =>
                x.SubscriptionPlanId ==
                subscription.SubscriptionPlanId);

        var panel = new StackPanel
        {
            Spacing = 12
        };

        panel.Children.Add(
            new TextBlock
            {
                Text = $"Company: {item.CompanyName}"
            });

        panel.Children.Add(planBox);

        var dialog = new ContentDialog
        {
            Title = "Change Subscription Plan",
            Content = panel,
            PrimaryButtonText = "Change Plan",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        if (planBox.SelectedItem is not SubscriptionPlan selectedPlan)
        {
            await ShowMessageAsync(
                "Validation Error",
                "Please select a subscription plan.");
            return;
        }

        if (selectedPlan.SubscriptionPlanId ==
            subscription.SubscriptionPlanId)
        {
            await ShowMessageAsync(
                "No Change",
                "The company is already using this plan.");
            return;
        }

        var confirmDialog = new ContentDialog
        {
            Title = "Confirm Plan Change",
            Content =
                $"Change '{item.CompanyName}' from " +
                $"'{subscription.SubscriptionPlan?.PlanName}' " +
                $"to '{selectedPlan.PlanName}'?",
            PrimaryButtonText = "Change",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var confirmResult = await confirmDialog.ShowAsync();

        if (confirmResult != ContentDialogResult.Primary)
            return;

        subscription.SubscriptionPlanId =
            selectedPlan.SubscriptionPlanId;

        await _masterDb.SaveChangesAsync();

        await LoadCompanySubscriptionsAsync();

        await ShowMessageAsync(
            "Plan Changed",
            $"'{item.CompanyName}' is now using " +
            $"'{selectedPlan.PlanName}'.");
    }
    private async void EditPlanButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not SubscriptionPlan selectedPlan)
        {
            return;
        }

        var planNameBox = new TextBox
        {
            Header = "Plan Name",
            Text = selectedPlan.PlanName,
            PlaceholderText = "Enter plan name"
        };

        var enterpriseTypeBox = new ComboBox
        {
            Header = "Enterprise Type"
        };

        enterpriseTypeBox.Items.Add("Micro");
        enterpriseTypeBox.Items.Add("Small");
        enterpriseTypeBox.Items.Add("Medium");

        enterpriseTypeBox.SelectedItem = selectedPlan.EnterpriseType;

        var priceBox = new NumberBox
        {
            Header = "Price",
            Value = (double)selectedPlan.Price,
            Minimum = 0,
            SmallChange = 100,
            SpinButtonPlacementMode =
                NumberBoxSpinButtonPlacementMode.Compact
        };

        var durationBox = new NumberBox
        {
            Header = "Duration (days)",
            Value = selectedPlan.DurationInDays,
            Minimum = 1,
            SpinButtonPlacementMode =
                NumberBoxSpinButtonPlacementMode.Compact
        };

        var activeCheckBox = new CheckBox
        {
            Content = "Active",
            IsChecked = selectedPlan.IsActive
        };

        var panel = new StackPanel
        {
            Spacing = 12
        };

        panel.Children.Add(planNameBox);
        panel.Children.Add(enterpriseTypeBox);
        panel.Children.Add(priceBox);
        panel.Children.Add(durationBox);
        panel.Children.Add(activeCheckBox);

        var dialog = new ContentDialog
        {
            Title = "Edit Subscription Plan",
            Content = panel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        var planName = planNameBox.Text.Trim();
        var enterpriseType = enterpriseTypeBox.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(planName))
        {
            await ShowMessageAsync(
                "Validation Error",
                "Plan name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(enterpriseType))
        {
            await ShowMessageAsync(
                "Validation Error",
                "Enterprise type is required.");
            return;
        }

        if (priceBox.Value < 0)
        {
            await ShowMessageAsync(
                "Validation Error",
                "Price cannot be negative.");
            return;
        }

        if (durationBox.Value < 1)
        {
            await ShowMessageAsync(
                "Validation Error",
                "Duration must be at least 1 day.");
            return;
        }

        var duplicatePlan = await _masterDb.SubscriptionPlans
            .AnyAsync(x =>
                x.SubscriptionPlanId != selectedPlan.SubscriptionPlanId &&
                x.PlanName == planName);

        if (duplicatePlan)
        {
            await ShowMessageAsync(
                "Plan Already Exists",
                "Another subscription plan already has that name.");
            return;
        }

        selectedPlan.PlanName = planName;
        selectedPlan.EnterpriseType = enterpriseType;
        selectedPlan.Price = (decimal)priceBox.Value;
        selectedPlan.DurationInDays = (int)durationBox.Value;
        selectedPlan.IsActive = activeCheckBox.IsChecked == true;

        await _masterDb.SaveChangesAsync();

        await LoadPlansAsync();

        await ShowMessageAsync(
            "Plan Updated",
            $"Subscription plan '{selectedPlan.PlanName}' was updated successfully.");
    }

    // ==========================================
    // ADD COMPANY SUBSCRIPTION
    // ==========================================
    private async Task UpdateExpiredSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;

        var expiredSubscriptions = await _masterDb.Subscriptions
            .Where(x =>
                x.Status == "Active" &&
                x.EndDate.HasValue &&
                x.EndDate.Value <= now)
            .ToListAsync();

        if (expiredSubscriptions.Count == 0)
            return;

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = "Expired";
        }

        await _masterDb.SaveChangesAsync();
    }
    private async void AddSubscriptionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var companies =
            await _masterDb.Companies
                .Where(x => x.IsActive)
                .OrderBy(x => x.CompanyName)
                .ToListAsync();


        var plans =
            await _masterDb.SubscriptionPlans
                .Where(x => x.IsActive)
                .OrderBy(x => x.Price)
                .ThenBy(x => x.PlanName)
                .ToListAsync();


        if (companies.Count == 0)
        {
            await ShowMessageAsync(
                "No Companies",
                "There are no active companies available.");

            return;
        }


        if (plans.Count == 0)
        {
            await ShowMessageAsync(
                "No Subscription Plans",
                "There are no active subscription plans available.");

            return;
        }


        var companyBox = new ComboBox
        {
            Header = "Company",
            PlaceholderText = "Select a company",
            DisplayMemberPath = "CompanyName"
        };


        foreach (var company in companies)
        {
            companyBox.Items.Add(company);
        }


        companyBox.SelectedIndex = 0;


        var planBox = new ComboBox
        {
            Header = "Subscription Plan",
            PlaceholderText = "Select a plan",
            DisplayMemberPath = "PlanName"
        };


        foreach (var plan in plans)
        {
            planBox.Items.Add(plan);
        }


        planBox.SelectedIndex = 0;


        var panel = new StackPanel
        {
            Spacing = 12
        };


        panel.Children.Add(companyBox);

        panel.Children.Add(planBox);


        var dialog = new ContentDialog
        {
            Title = "Add Company Subscription",

            Content = panel,

            PrimaryButtonText = "Activate",

            CloseButtonText = "Cancel",

            DefaultButton =
                ContentDialogButton.Primary,

            XamlRoot = XamlRoot
        };


        var result =
            await dialog.ShowAsync();


        if (result != ContentDialogResult.Primary)
            return;


        if (companyBox.SelectedItem
            is not Company selectedCompany)
        {
            await ShowMessageAsync(
                "Validation Error",
                "Please select a company.");

            return;
        }


        if (planBox.SelectedItem
            is not SubscriptionPlan selectedPlan)
        {
            await ShowMessageAsync(
                "Validation Error",
                "Please select a subscription plan.");

            return;
        }


        var existingActiveSubscription =
            await _masterDb.Subscriptions
                .AnyAsync(x =>
                    x.CompanyId ==
                        selectedCompany.CompanyId
                    && x.Status == "Active"
                    && (
                        x.EndDate == null
                        || x.EndDate >
                            DateTime.UtcNow));


        if (existingActiveSubscription)
        {
            await ShowMessageAsync(
                "Active Subscription Exists",
                $"'{selectedCompany.CompanyName}' already has an active subscription.");

            return;
        }


        var startDate =
            DateTime.UtcNow;


        var subscription =
            new Subscription
            {
                CompanyId =
                    selectedCompany.CompanyId,

                SubscriptionPlanId =
                    selectedPlan.SubscriptionPlanId,

                StartDate =
                    startDate,

                EndDate =
                    startDate.AddDays(
                        selectedPlan.DurationInDays),

                TrialEndsAt =
                    selectedPlan.PlanName.Equals(
                        "Trial",
                        StringComparison.OrdinalIgnoreCase)
                        ? startDate.AddDays(
                            selectedPlan.DurationInDays)
                        : null,

                Status = "Active"
            };


        _masterDb.Subscriptions.Add(
            subscription);


        await _masterDb.SaveChangesAsync();


        await LoadCompanySubscriptionsAsync();


        await ShowMessageAsync(
            "Subscription Activated",
            $"'{selectedCompany.CompanyName}' is now subscribed to '{selectedPlan.PlanName}'.");
    }


    // ==========================================
    // MESSAGE DIALOG
    // ==========================================

    private async Task ShowMessageAsync(
        string title,
        string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,

            Content = message,

            CloseButtonText = "OK",

            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}


// ==============================================
// MODULE SELECTION UI MODEL
// ==============================================

public class ModuleSelectionItem
{
    public int ModuleDefinitionId { get; set; }

    public string ModuleName { get; set; }
        = string.Empty;

    public bool IsIncluded { get; set; }
}


// ==============================================
// SUBSCRIPTION DISPLAY UI MODEL
// ==============================================

public class SubscriptionDisplayItem
{
    public int SubscriptionId { get; set; }

    public int CompanyId { get; set; }

    public int SubscriptionPlanId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string PlanName { get; set; } = string.Empty;

    public string EnterpriseType { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string PriceText => $"₱{Price:N2}";

    public string Status { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string StartDateText { get; set; } = string.Empty;

    public string EndDateText { get; set; } = string.Empty;
}