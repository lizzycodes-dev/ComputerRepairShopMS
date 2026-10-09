using ComputerRepairSystem.company.Context;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class BillingPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly MasterErpDbContext _masterDb;
    private readonly CurrentBranchContext _currentBranchContext;
    private List<BillingRow> _filteredRows = new();
    // ==========================================
    // PAGINATION STATE
    // ==========================================

    private List<BillingRow> _billingRows = new();
    private int _currentPage = 1;
    private const int _pageSize = 10;


    public BillingPage(
        TenantDbContextFactory tenantDbFactory,
        MasterErpDbContext masterDb,
        CurrentBranchContext currentBranchContext)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;
        _masterDb = masterDb;
        _currentBranchContext = currentBranchContext;

        Loaded += BillingPage_Loaded;
        Unloaded += BillingPage_Unloaded;
    }


    private async Task<bool> HasBranchManagementAsync()
    {
        if (CurrentUser.CompanyId == null)
            return false;

        var subscription =
            await _masterDb.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.CompanyId == CurrentUser.CompanyId.Value &&
                    s.Status == "Active" &&
                    (!s.EndDate.HasValue ||
                     s.EndDate.Value > DateTime.UtcNow))
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

        if (subscription == null)
            return false;

        return await _masterDb.SubscriptionPlanModules
            .AsNoTracking()
            .AnyAsync(x =>
                x.SubscriptionPlanId ==
                    subscription.SubscriptionPlanId &&
                x.ModuleDefinitionId == 11);
    }


    private async void BillingPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged += OnGlobalBranchChanged;

        await LoadCompletedRepairsAsync();
    }


    private void BillingPage_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged -= OnGlobalBranchChanged;
    }


    private async void OnGlobalBranchChanged()
    {
        if (!IsLoaded)
            return;

        await LoadCompletedRepairsAsync();
    }


    // ==========================================
    // LOAD COMPLETED REPAIRS
    // ==========================================

    private async Task LoadCompletedRepairsAsync()
    {
        if (CurrentUser.CompanyId == null)
        {
            _billingRows = new();
            UpdatePagination();
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            var query =
                db.Repairs
                    .AsNoTracking()
                    .Where(r =>
                        r.Status == "Completed" &&
                        r.Invoice == null)
                    .AsQueryable();

            if (branchManagementEnabled)
            {
                var currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    _billingRows = new();
                    UpdatePagination();

                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }

                query = query.Where(r =>
                    r.ServiceRequest.BranchId ==
                    currentBranchId.Value);
            }

            // Newest first — RepairId is an IDENTITY column
            _billingRows =
                await query
                    .OrderByDescending(r => r.RepairId)
                    .Select(r => new BillingRow
                    {
                        RepairId = r.RepairId,

                        CustomerName =
                            r.ServiceRequest
                                .Device
                                .Customer
                                .FirstName
                            + " "
                            + r.ServiceRequest
                                .Device
                                .Customer
                                .LastName,

                        DeviceName =
                            r.ServiceRequest
                                .Device
                                .Brand
                            + " "
                            + r.ServiceRequest
                                .Device
                                .Model,

                        RepairDescription =
                            r.RepairDescription ??
                            "No description",

                        // ⬇⬇⬇ NEW
                        CompletedDate = r.EndDate
                    })
                    .ToListAsync();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            _billingRows = new();
            UpdatePagination();

            System.Diagnostics.Debug.WriteLine(
                "Failed to load completed repairs: " + ex);
        }
    }


    // ==========================================
    // PAGINATION
    // ==========================================

    private void UpdatePagination()
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_filteredRows.Count /
                    _pageSize));

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        if (_currentPage < 1)
            _currentPage = 1;

        var pagedRows =
            _filteredRows
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        BillingList.ItemsSource = pagedRows;

        PageInfoText.Text =
            $"Page {_currentPage} of {totalPages}  •  " +
            $"{_filteredRows.Count} total";

        PreviousPageButton.IsEnabled = _currentPage > 1;
        NextPageButton.IsEnabled = _currentPage < totalPages;
    }

    private void PreviousPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_currentPage <= 1)
            return;

        _currentPage--;

        UpdatePagination();
    }


    private void NextPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_filteredRows.Count /
                    _pageSize));

        if (_currentPage >= totalPages)
            return;

        _currentPage++;

        UpdatePagination();
    }

    // ==========================================
    // FILTER (SEARCH + DATE RANGE)
    // ==========================================

    private void SearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyFilter();
    }


    private void DateFilter_DateChanged(
        CalendarDatePicker sender,
        CalendarDatePickerDateChangedEventArgs args)
    {
        ApplyFilter();
    }


    private void ClearFilterButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        StartDatePicker.Date = null;
        EndDatePicker.Date = null;

        ApplyFilter();
    }


    private void ApplyFilter()
    {
        var search =
            SearchBox.Text?.Trim() ?? string.Empty;

        var startDate =
            StartDatePicker.Date?.Date;

        var endDate =
            EndDatePicker.Date?.Date;

        IEnumerable<BillingRow> filtered = _billingRows;

        // ------------------------------
        // SEARCH FILTER
        // ------------------------------

        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered =
                filtered
                    .Where(r =>
                        (r.CustomerName ?? string.Empty)
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        ||
                        (r.DeviceName ?? string.Empty)
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        ||
                        (r.RepairDescription ?? string.Empty)
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase));
        }

        // ------------------------------
        // DATE RANGE FILTER
        // ------------------------------

        if (startDate.HasValue)
        {
            filtered =
                filtered
                    .Where(r =>
                        r.CompletedDate.HasValue &&
                        r.CompletedDate.Value.Date >=
                        startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            filtered =
                filtered
                    .Where(r =>
                        r.CompletedDate.HasValue &&
                        r.CompletedDate.Value.Date <=
                        endDate.Value.Date);
        }

        _filteredRows = filtered.ToList();

        _currentPage = 1;

        UpdatePagination();
    }


    // ==========================================
    // PAY BUTTON
    // ==========================================

    private async void PayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not BillingRow row)
        {
            return;
        }

        await ProcessPaymentAsync(row.RepairId);
    }


    // ==========================================
    // PROCESS PAYMENT
    // ==========================================

    private async Task ProcessPaymentAsync(
        int repairId)
    {
        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        await using var db =
            await _tenantDbFactory.CreateAsync(
                CurrentUser.CompanyId.Value);

        var repair =
            await db.Repairs
                .Include(r =>
                    r.RepairItems)
                    .ThenInclude(ri =>
                        ri.Item)
                .Include(r =>
                    r.ServiceRequest)
                    .ThenInclude(sr =>
                        sr.Device)
                    .ThenInclude(d =>
                        d.Customer)
                .FirstOrDefaultAsync(
                    r => r.RepairId == repairId);

        if (repair == null)
        {
            return;
        }

        // ==========================================
        // BRANCH SAFETY CHECK
        // ==========================================

        var branchManagementEnabled =
            await HasBranchManagementAsync();

        if (branchManagementEnabled)
        {
            var currentBranchId =
                _currentBranchContext.BranchId;

            if (currentBranchId == null)
            {
                await ShowMessageAsync(
                    "Branch Not Selected",
                    "Please select a branch from the global branch selector.");

                return;
            }

            if (repair.ServiceRequest.BranchId !=
                currentBranchId.Value)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "This repair belongs to a different branch than the one currently selected.");

                return;
            }
        }

        var subtotal =
            repair.RepairItems.Sum(
                x =>
                    (x.Quantity * x.UnitPrice)
                    - x.Discount);

        var settings =
            await db.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

        if (settings == null)
        {
            await ShowMessageAsync(
                "System Settings Missing",
                "Please configure the labor rates in System Settings first.");

            return;
        }

        var laborAmount =
            repair.ServiceRequest.Priority switch
            {
                "Low" => settings.LowLaborRate,
                "Medium" => settings.MediumLaborRate,
                "High" => settings.HighLaborRate,
                _ => settings.MediumLaborRate
            };

        var total =
            subtotal + laborAmount;


        var amountBox =
            new NumberBox
            {
                Header = "Amount Paid",
                Value = (double)total,
                Minimum = (double)total,
                SmallChange = 100
            };


        var paymentMethodBox =
            new ComboBox
            {
                Header = "Payment Method",
                SelectedIndex = 0
            };

        paymentMethodBox.Items.Add("Cash");
        paymentMethodBox.Items.Add("GCash");
        paymentMethodBox.Items.Add("Card");


        var content =
            new StackPanel
            {
                Spacing = 12
            };

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Customer: " +
                    $"{repair.ServiceRequest.Device.Customer.FirstName} " +
                    $"{repair.ServiceRequest.Device.Customer.LastName}"
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Device: " +
                    $"{repair.ServiceRequest.Device.Brand} " +
                    $"{repair.ServiceRequest.Device.Model}"
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Priority: {repair.ServiceRequest.Priority}"
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Labor: ₱{laborAmount:N2}",
                FontWeight =
                    Microsoft.UI.Text.FontWeights.SemiBold
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Parts: ₱{subtotal:N2}",
                FontWeight =
                    Microsoft.UI.Text.FontWeights.SemiBold
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Total: ₱{total:N2}",
                FontSize = 18,
                FontWeight =
                    Microsoft.UI.Text.FontWeights.SemiBold
            });

        content.Children.Add(
            amountBox);

        content.Children.Add(
            paymentMethodBox);


        var dialog =
            new ContentDialog
            {
                Title = "Process Payment",

                Content = content,

                PrimaryButtonText = "Pay",

                CloseButtonText = "Cancel",

                DefaultButton =
                    ContentDialogButton.Primary,

                XamlRoot = XamlRoot
            };

        var result =
            await dialog.ShowAsync();

        if (result !=
            ContentDialogResult.Primary)
        {
            return;
        }

        if (double.IsNaN(amountBox.Value) ||
            amountBox.Value < (double)total)
        {
            await ShowMessageAsync(
                "Invalid Payment",
                "The payment must cover the total amount.");

            return;
        }

        var payment =
            new Payment
            {
                Amount =
                    (decimal)amountBox.Value,

                PaymentMethod =
                    paymentMethodBox.SelectedItem?
                        .ToString()
                    ?? "Cash",

                Status =
                    "Completed"
            };

        var invoice =
            new Invoice
            {
                RepairId = repairId,

                InvoiceNumber =
                    $"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}",

                Subtotal = subtotal,

                LaborAmount = laborAmount,

                Discount = 0,

                Tax = 0,

                TotalAmount = total,

                Status = "Paid",

                Payments =
                    new List<Payment>
                    {
                        payment
                    }
            };

        db.Invoices.Add(invoice);

        await db.SaveChangesAsync();

        await LoadCompletedRepairsAsync();

        await ShowMessageAsync(
            "Payment Complete",
            $"Payment successful.\n\n" +
            $"Total: ₱{total:N2}\n" +
            $"Paid: ₱{amountBox.Value:N2}\n" +
            $"Change: ₱{amountBox.Value - (double)total:N2}");
    }


    private async Task ShowMessageAsync(
        string title,
        string message)
    {
        var dialog =
            new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

        await dialog.ShowAsync();
    }


    private sealed class BillingRow
    {
        public int RepairId { get; set; }

        public string CustomerName { get; set; }
            = string.Empty;

        public string DeviceName { get; set; }
            = string.Empty;

        public string RepairDescription { get; set; }
            = string.Empty;

        // ⬇⬇⬇ NEW
        public DateTime? CompletedDate { get; set; }
    }
}