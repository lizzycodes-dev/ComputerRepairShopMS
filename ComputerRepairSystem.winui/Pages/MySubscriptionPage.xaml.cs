using ComputerRepairSystem.domain.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;


namespace ComputerRepairSystem_winui.Pages;

public sealed partial class MySubscriptionPage : Page
{
    private readonly MasterErpDbContext _masterDb;

    private Subscription? _currentSubscription;

    private readonly ObservableCollection<ModuleDisplayItem> _modules = new();

    public MySubscriptionPage(MasterErpDbContext masterDb)
    {
        this.InitializeComponent();

        _masterDb = masterDb;

        ModulesListView.ItemsSource = _modules;

        Loaded += MySubscriptionPage_Loaded;
    }


    private async void MySubscriptionPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadSubscriptionAsync();
    }


    private async Task LoadSubscriptionAsync()
    {
        try
        {
            _modules.Clear();

            var companyId = CurrentUser.CompanyId;

            if (companyId <= 0)
            {
                await ShowMessageAsync(
                    "Company Not Found",
                    "Your user account is not assigned to a company.");

                return;
            }


            // Update expired subscriptions first.

            var now = DateTime.UtcNow;

            var expiredSubscriptions =
                await _masterDb.Subscriptions
                    .Where(x =>
                        x.CompanyId == companyId &&
                        x.Status == "Active" &&
                        x.EndDate.HasValue &&
                        x.EndDate.Value <= now)
                    .ToListAsync();

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = "Expired";
            }

            if (expiredSubscriptions.Count > 0)
            {
                await _masterDb.SaveChangesAsync();
            }


            // Get current subscription.

            _currentSubscription =
                await _masterDb.Subscriptions
                    .Include(x => x.SubscriptionPlan)
                    .Where(x =>
                        x.CompanyId == companyId &&
                        x.Status == "Active" &&
                        (
                            x.EndDate == null ||
                            x.EndDate > now
                        ))
                    .OrderByDescending(x => x.StartDate)
                    .FirstOrDefaultAsync();


            if (_currentSubscription == null)
            {
                ShowNoSubscription();

                return;
            }


            var plan = _currentSubscription.SubscriptionPlan;

            if (plan == null)
            {
                await ShowMessageAsync(
                    "Plan Not Found",
                    "Your subscription is not linked to a valid subscription plan.");

                return;
            }


            // Plan information.

            PlanNameText.Text = plan.PlanName;

            EnterpriseTypeText.Text =
                plan.EnterpriseType;

            PriceText.Text =
                $"₱{plan.Price:N2}";


            StatusText.Text =
                _currentSubscription.Status;


            // Dates.

            StartDateText.Text =
                _currentSubscription.StartDate
                    .ToLocalTime()
                    .ToString("MMM dd, yyyy");


            EndDateText.Text =
                _currentSubscription.EndDate.HasValue
                    ? _currentSubscription.EndDate.Value
                        .ToLocalTime()
                        .ToString("MMM dd, yyyy")
                    : "No End Date";


            // Load included modules.

            var moduleIds =
                await _masterDb.SubscriptionPlanModules
                    .Where(x =>
                        x.SubscriptionPlanId ==
                        plan.SubscriptionPlanId)
                    .Select(x => x.ModuleDefinitionId)
                    .ToListAsync();


            var modules =
                await _masterDb.ModuleDefinitions
                    .Where(x =>
                        x.IsActive &&
                        moduleIds.Contains(
                            x.ModuleDefinitionId))
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();


            foreach (var module in modules)
            {
                _modules.Add(
                    new ModuleDisplayItem
                    {
                        ModuleName = module.ModuleName
                    });
            }


            RenewButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Unable to Load Subscription",
                ex.Message);
        }
    }


    private void ShowNoSubscription()
    {
        PlanNameText.Text =
            "No Active Subscription";

        EnterpriseTypeText.Text =
            "—";

        PriceText.Text =
            "—";

        StatusText.Text =
            "No Subscription";

        StartDateText.Text =
            "—";

        EndDateText.Text =
            "—";

        _modules.Clear();

        RenewButton.IsEnabled = false;
    }


    private async void RenewButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_currentSubscription == null)
        {
            await ShowMessageAsync(
                "No Subscription",
                "There is no active subscription to renew.");

            return;
        }


        var plan =
            _currentSubscription.SubscriptionPlan;

        if (plan == null)
        {
            await ShowMessageAsync(
                "Plan Not Found",
                "The subscription does not have a valid plan.");

            return;
        }


        var confirmDialog = new ContentDialog
        {
            Title = "Renew Subscription",

            Content =
                $"Plan: {plan.PlanName}\n" +
                $"Duration: {plan.DurationInDays} days\n" +
                $"Price: ₱{plan.Price:N2}\n\n" +
                "Would you like to renew your subscription?",

            PrimaryButtonText = "Renew",

            CloseButtonText = "Cancel",

            DefaultButton =
                ContentDialogButton.Primary,

            XamlRoot = XamlRoot
        };


        var result =
            await confirmDialog.ShowAsync();


        if (result != ContentDialogResult.Primary)
            return;


        var baseDate =
            _currentSubscription.EndDate.HasValue &&
            _currentSubscription.EndDate.Value >
            DateTime.UtcNow

                ? _currentSubscription.EndDate.Value

                : DateTime.UtcNow;


        var newEndDate =
            baseDate.AddDays(
                plan.DurationInDays);


        _currentSubscription.EndDate =
            newEndDate;

        _currentSubscription.Status =
            "Active";


        await _masterDb.SaveChangesAsync();


        await LoadSubscriptionAsync();


        await ShowMessageAsync(
            "Subscription Renewed",
            $"Your subscription has been renewed until " +
            $"{newEndDate.ToLocalTime():MMM dd, yyyy}.");
    }


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


public class ModuleDisplayItem
{
    public string ModuleName { get; set; } = string.Empty;
}