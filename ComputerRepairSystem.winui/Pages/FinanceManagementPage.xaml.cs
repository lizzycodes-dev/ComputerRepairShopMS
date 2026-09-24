using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class FinanceManagementPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;

    public FinanceManagementPage(
        TenantDbContextFactory tenantDbFactory)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;

        Loaded += FinanceManagementPage_Loaded;
    }


    // ==========================================
    // PAGE LOADED
    // ==========================================

    private async void FinanceManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadInvoicesAsync();
        await LoadPaymentsAsync();
        await LoadExpensesAsync();
        await LoadFinancialReportsAsync();
    }

    // ==========================================
    // LOAD FINANCIAL REPORTS
    // ==========================================

    private async Task LoadFinancialReportsAsync()
    {
        if (CurrentUser.CompanyId == null)
        {
            ReportRevenueText.Text = "₱0.00";
            ReportExpensesText.Text = "₱0.00";
            ReportNetIncomeText.Text = "₱0.00";
            ReportLastUpdatedText.Text = "Last updated: --";

            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            // Revenue = completed customer payments
            var revenue =
                await db.Payments
                    .Where(x => x.Status == "Completed")
                    .SumAsync(x =>
                        (decimal?)x.Amount)
                    ?? 0;

            // Operating expenses = recorded expenses
            var expenses =
                await db.Expenses
                    .SumAsync(x =>
                        (decimal?)x.Amount)
                    ?? 0;

            // Net income = revenue - expenses
            var netIncome =
                revenue - expenses;

            ReportRevenueText.Text =
                $"₱{revenue:N2}";

            ReportExpensesText.Text =
                $"₱{expenses:N2}";

            ReportNetIncomeText.Text =
                $"₱{netIncome:N2}";

            ReportLastUpdatedText.Text =
                $"Last updated: {DateTime.Now:MM/dd/yyyy hh:mm tt}";
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Financial Reports",
                ex.Message);
        }
    }

    // ==========================================
    // LOAD INVOICES
    // ==========================================

    private async Task LoadInvoicesAsync()
    {
        if (CurrentUser.CompanyId == null)
        {
            InvoiceList.ItemsSource = null;
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var invoices =
                await db.Invoices
                    .AsNoTracking()
                    .Include(x => x.Repair)
                        .ThenInclude(x => x!.ServiceRequest)
                            .ThenInclude(x => x.Device)
                                .ThenInclude(x => x.Customer)
                    .OrderByDescending(x => x.InvoiceDate)
                    .ToListAsync();

            InvoiceList.ItemsSource =
                invoices
                    .Select(x => new InvoiceRow
                    {
                        InvoiceId = x.InvoiceId,
                        InvoiceNumberText = x.InvoiceNumber,

                        CustomerName =
                            x.Repair?.ServiceRequest?.Device?.Customer == null
                                ? "Unknown Customer"
                                : $"{x.Repair.ServiceRequest.Device.Customer.FirstName} " +
                                  $"{x.Repair.ServiceRequest.Device.Customer.LastName}",

                        SubtotalText =
                            $"₱{x.Subtotal:N2}",

                        DiscountText =
                            $"₱{x.Discount:N2}",

                        TaxText =
                            $"₱{x.Tax:N2}",

                        TotalAmountText =
                            $"₱{x.TotalAmount:N2}",

                        StatusText = x.Status,

                        RepairId = x.RepairId,
                        Subtotal = x.Subtotal,
                        Discount = x.Discount,
                        Tax = x.Tax,
                        TotalAmount = x.TotalAmount
                    })
                    .ToList();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Invoices",
                ex.Message);
        }
    }


    // ==========================================
    // INVOICE SEARCH
    // ==========================================

    private async void InvoiceSearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (CurrentUser.CompanyId == null)
        {
            InvoiceList.ItemsSource = null;
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var invoices =
                await db.Invoices
                    .AsNoTracking()
                    .Include(x => x.Repair)
                        .ThenInclude(x => x!.ServiceRequest)
                            .ThenInclude(x => x.Device)
                                .ThenInclude(x => x.Customer)
                    .OrderByDescending(x => x.InvoiceDate)
                    .ToListAsync();

            var search =
                InvoiceSearchBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(search))
            {
                invoices =
                    invoices
                        .Where(x =>
                            x.InvoiceNumber.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            InvoiceList.ItemsSource =
                invoices
                    .Select(x => new InvoiceRow
                    {
                        InvoiceId = x.InvoiceId,
                        InvoiceNumberText = x.InvoiceNumber,

                        CustomerName =
                            x.Repair?.ServiceRequest?.Device?.Customer == null
                                ? "Unknown Customer"
                                : $"{x.Repair.ServiceRequest.Device.Customer.FirstName} " +
                                  $"{x.Repair.ServiceRequest.Device.Customer.LastName}",

                        SubtotalText =
                            $"₱{x.Subtotal:N2}",

                        DiscountText =
                            $"₱{x.Discount:N2}",

                        TaxText =
                            $"₱{x.Tax:N2}",

                        TotalAmountText =
                            $"₱{x.TotalAmount:N2}",

                        StatusText = x.Status,

                        RepairId = x.RepairId,
                        Subtotal = x.Subtotal,
                        Discount = x.Discount,
                        Tax = x.Tax,
                        TotalAmount = x.TotalAmount
                    })
                    .ToList();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Invoice Search Error",
                ex.Message);
        }
    }


    // ==========================================
    // ADD INVOICE
    // ==========================================

    private async void AddInvoiceButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var repairs =
                await db.Repairs
                    .AsNoTracking()
                    .Include(x => x.ServiceRequest)
                        .ThenInclude(x => x.Device)
                            .ThenInclude(x => x.Customer)
                    .Where(x =>
                        x.Status != "Cancelled" &&
                        !db.Invoices.Any(i =>
                            i.RepairId == x.RepairId))
                    .OrderByDescending(x => x.RepairId)
                    .ToListAsync();

            if (repairs.Count == 0)
            {
                await ShowMessageAsync(
                    "No Repairs Available",
                    "There are no repairs available for a new invoice.");
                return;
            }

            var repairOptions =
                repairs
                    .Select(x => new RepairOption
                    {
                        RepairId = x.RepairId,

                        DisplayText =
                            x.ServiceRequest?.Device?.Customer == null
                                ? $"Repair #{x.RepairId}"
                                : $"Repair #{x.RepairId} - " +
                                  $"{x.ServiceRequest.Device.Customer.FirstName} " +
                                  $"{x.ServiceRequest.Device.Customer.LastName}"
                    })
                    .ToList();

            var repairBox =
                new ComboBox
                {
                    Header = "Repair",
                    PlaceholderText = "Select repair",
                    ItemsSource = repairOptions,
                    DisplayMemberPath = "DisplayText",
                    SelectedIndex = 0,
                    MinWidth = 360
                };

            var invoiceNumberBox =
                new TextBox
                {
                    Header = "Invoice Number",
                    PlaceholderText = "Enter invoice number"
                };

            var subtotalBox =
                new NumberBox
                {
                    Header = "Subtotal",
                    Minimum = 0,
                    Value = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var discountBox =
                new NumberBox
                {
                    Header = "Discount",
                    Minimum = 0,
                    Value = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var taxBox =
                new NumberBox
                {
                    Header = "Tax",
                    Minimum = 0,
                    Value = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var statusBox =
                new ComboBox
                {
                    Header = "Status",
                    PlaceholderText = "Select status"
                };

            statusBox.Items.Add("Unpaid");
            statusBox.Items.Add("Partially Paid");
            statusBox.Items.Add("Paid");
            statusBox.SelectedIndex = 0;

            var totalText =
                new TextBlock
                {
                    Text = "Total: ₱0.00",
                    FontSize = 18,
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };

            void UpdateTotal()
            {
                var subtotal =
                    double.IsNaN(subtotalBox.Value)
                        ? 0
                        : subtotalBox.Value;

                var discount =
                    double.IsNaN(discountBox.Value)
                        ? 0
                        : discountBox.Value;

                var tax =
                    double.IsNaN(taxBox.Value)
                        ? 0
                        : taxBox.Value;

                var total =
                    Math.Max(0, subtotal - discount + tax);

                totalText.Text =
                    $"Total: ₱{total:N2}";
            }

            subtotalBox.ValueChanged +=
                (_, _) => UpdateTotal();

            discountBox.ValueChanged +=
                (_, _) => UpdateTotal();

            taxBox.ValueChanged +=
                (_, _) => UpdateTotal();

            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(repairBox);
            panel.Children.Add(invoiceNumberBox);
            panel.Children.Add(subtotalBox);
            panel.Children.Add(discountBox);
            panel.Children.Add(taxBox);
            panel.Children.Add(statusBox);
            panel.Children.Add(totalText);

            var dialog =
                new ContentDialog
                {
                    Title = "Add Invoice",
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
            {
                return;
            }

            if (repairBox.SelectedItem
                is not RepairOption selectedRepair)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a repair.");
                return;
            }

            var invoiceNumber =
                invoiceNumberBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Invoice number is required.");
                return;
            }

            var subtotal =
                (decimal)(
                    double.IsNaN(subtotalBox.Value)
                        ? 0
                        : subtotalBox.Value);

            var discount =
                (decimal)(
                    double.IsNaN(discountBox.Value)
                        ? 0
                        : discountBox.Value);

            var tax =
                (decimal)(
                    double.IsNaN(taxBox.Value)
                        ? 0
                        : taxBox.Value);

            if (subtotal < 0 ||
                discount < 0 ||
                tax < 0)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Amounts cannot be negative.");
                return;
            }

            if (discount > subtotal)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Discount cannot be greater than the subtotal.");
                return;
            }

            var duplicateNumber =
                await db.Invoices
                    .AnyAsync(x =>
                        x.InvoiceNumber == invoiceNumber);

            if (duplicateNumber)
            {
                await ShowMessageAsync(
                    "Duplicate Invoice",
                    "That invoice number already exists.");
                return;
            }

            var totalAmount =
                subtotal - discount + tax;

            var invoice =
                new Invoice
                {
                    RepairId =
                        selectedRepair.RepairId,

                    InvoiceNumber =
                        invoiceNumber,

                    InvoiceDate =
                        DateTime.UtcNow,

                    Subtotal =
                        subtotal,

                    Discount =
                        discount,

                    Tax =
                        tax,

                    TotalAmount =
                        totalAmount,

                    Status =
                        statusBox.SelectedItem?.ToString()
                        ?? "Unpaid"
                };

            db.Invoices.Add(invoice);

            await db.SaveChangesAsync();

            await LoadInvoicesAsync();

            await ShowMessageAsync(
                "Invoice Added",
                "The invoice was added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Adding Invoice",
                ex.Message);
        }
    }


    // ==========================================
    // EDIT INVOICE
    // ==========================================

    private async void EditInvoiceButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not InvoiceRow row)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var invoice =
                await db.Invoices
                    .FirstOrDefaultAsync(
                        x => x.InvoiceId == row.InvoiceId);

            if (invoice == null)
            {
                await ShowMessageAsync(
                    "Not Found",
                    "The invoice could not be found.");
                return;
            }

            var invoiceNumberBox =
                new TextBox
                {
                    Header = "Invoice Number",
                    Text = invoice.InvoiceNumber
                };

            var subtotalBox =
                new NumberBox
                {
                    Header = "Subtotal",
                    Minimum = 0,
                    Value = (double)invoice.Subtotal,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var discountBox =
                new NumberBox
                {
                    Header = "Discount",
                    Minimum = 0,
                    Value = (double)invoice.Discount,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var taxBox =
                new NumberBox
                {
                    Header = "Tax",
                    Minimum = 0,
                    Value = (double)invoice.Tax,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var statusBox =
                new ComboBox
                {
                    Header = "Status"
                };

            statusBox.Items.Add("Unpaid");
            statusBox.Items.Add("Partially Paid");
            statusBox.Items.Add("Paid");

            for (int i = 0; i < statusBox.Items.Count; i++)
            {
                if (statusBox.Items[i]?.ToString() ==
                    invoice.Status)
                {
                    statusBox.SelectedIndex = i;
                    break;
                }
            }

            if (statusBox.SelectedIndex < 0)
            {
                statusBox.SelectedIndex = 0;
            }

            var totalText =
                new TextBlock
                {
                    FontSize = 18,
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };

            void UpdateTotal()
            {
                var subtotal =
                    double.IsNaN(subtotalBox.Value)
                        ? 0
                        : subtotalBox.Value;

                var discount =
                    double.IsNaN(discountBox.Value)
                        ? 0
                        : discountBox.Value;

                var tax =
                    double.IsNaN(taxBox.Value)
                        ? 0
                        : taxBox.Value;

                var total =
                    Math.Max(0, subtotal - discount + tax);

                totalText.Text =
                    $"Total: ₱{total:N2}";
            }

            subtotalBox.ValueChanged +=
                (_, _) => UpdateTotal();

            discountBox.ValueChanged +=
                (_, _) => UpdateTotal();

            taxBox.ValueChanged +=
                (_, _) => UpdateTotal();

            UpdateTotal();

            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(invoiceNumberBox);
            panel.Children.Add(subtotalBox);
            panel.Children.Add(discountBox);
            panel.Children.Add(taxBox);
            panel.Children.Add(statusBox);
            panel.Children.Add(totalText);

            var dialog =
                new ContentDialog
                {
                    Title = "Edit Invoice",
                    Content = panel,

                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",

                    DefaultButton =
                        ContentDialogButton.Primary,

                    XamlRoot = XamlRoot
                };

            var result =
                await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var invoiceNumber =
                invoiceNumberBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Invoice number is required.");
                return;
            }

            var subtotal =
                (decimal)(
                    double.IsNaN(subtotalBox.Value)
                        ? 0
                        : subtotalBox.Value);

            var discount =
                (decimal)(
                    double.IsNaN(discountBox.Value)
                        ? 0
                        : discountBox.Value);

            var tax =
                (decimal)(
                    double.IsNaN(taxBox.Value)
                        ? 0
                        : taxBox.Value);

            if (discount > subtotal)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Discount cannot be greater than the subtotal.");
                return;
            }

            var duplicateNumber =
                await db.Invoices
                    .AnyAsync(x =>
                        x.InvoiceId != invoice.InvoiceId &&
                        x.InvoiceNumber == invoiceNumber);

            if (duplicateNumber)
            {
                await ShowMessageAsync(
                    "Duplicate Invoice",
                    "That invoice number is already being used.");
                return;
            }

            invoice.InvoiceNumber = invoiceNumber;
            invoice.Subtotal = subtotal;
            invoice.Discount = discount;
            invoice.Tax = tax;
            invoice.TotalAmount =
                subtotal - discount + tax;

            invoice.Status =
                statusBox.SelectedItem?.ToString()
                ?? "Unpaid";

            await db.SaveChangesAsync();

            await LoadInvoicesAsync();

            await ShowMessageAsync(
                "Invoice Updated",
                "The invoice was updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Updating Invoice",
                ex.Message);
        }
    }


    // ==========================================
    // DELETE INVOICE
    // ==========================================

    private async void DeleteInvoiceButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not InvoiceRow row)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            var confirm =
                new ContentDialog
                {
                    Title = "Delete Invoice",
                    Content =
                        $"Delete invoice '{row.InvoiceNumberText}'?",

                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",

                    DefaultButton =
                        ContentDialogButton.Close,

                    XamlRoot = XamlRoot
                };

            var result =
                await confirm.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var invoice =
                await db.Invoices
                    .FirstOrDefaultAsync(
                        x =>
                            x.InvoiceId ==
                            row.InvoiceId);

            if (invoice == null)
            {
                return;
            }

            var hasPayments =
                await db.Payments
                    .AnyAsync(x =>
                        x.InvoiceId ==
                        invoice.InvoiceId);

            if (hasPayments)
            {
                await ShowMessageAsync(
                    "Cannot Delete Invoice",
                    "This invoice already has a payment record. Delete or handle its payments first.");

                return;
            }

            db.Invoices.Remove(invoice);

            await db.SaveChangesAsync();

            await LoadInvoicesAsync();

            await ShowMessageAsync(
                "Invoice Deleted",
                "The invoice was deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Deleting Invoice",
                ex.Message);
        }
    }


    // ==========================================
    // LOAD PAYMENTS
    // ==========================================

    private async Task LoadPaymentsAsync()
    {
        if (CurrentUser.CompanyId == null)
        {
            PaymentList.ItemsSource = null;
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var payments =
                await db.Payments
                    .AsNoTracking()
                    .Include(x => x.Invoice)
                    .OrderByDescending(x => x.PaymentDate)
                    .ToListAsync();

            PaymentList.ItemsSource =
                payments
                    .Select(x => new PaymentRow
                    {
                        PaymentId = x.PaymentId,

                        InvoiceNumber =
                            x.Invoice?.InvoiceNumber
                            ?? "Unknown Invoice",

                        AmountText =
                            $"₱{x.Amount:N2}",

                        PaymentMethodText =
                            x.PaymentMethod,

                        ReferenceNumberText =
                            string.IsNullOrWhiteSpace(
                                x.ReferenceNumber)
                                ? "-"
                                : x.ReferenceNumber,

                        StatusText =
                            x.Status,

                        PaidAtText =
                            x.PaymentDate.ToString(
                                "MM/dd/yyyy")
                    })
                    .ToList();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Payments",
                ex.Message);
        }
    }


    // ==========================================
    // PAYMENT SEARCH
    // ==========================================

    private async void PaymentSearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (CurrentUser.CompanyId == null)
        {
            PaymentList.ItemsSource = null;
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var payments =
                await db.Payments
                    .AsNoTracking()
                    .Include(x => x.Invoice)
                    .OrderByDescending(x => x.PaymentDate)
                    .ToListAsync();

            var search =
                PaymentSearchBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(search))
            {
                payments =
                    payments
                        .Where(x =>
                            (x.ReferenceNumber ?? "")
                                .Contains(
                                    search,
                                    StringComparison.OrdinalIgnoreCase) ||
                            (x.Invoice?.InvoiceNumber ?? "")
                                .Contains(
                                    search,
                                    StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            PaymentList.ItemsSource =
                payments
                    .Select(x => new PaymentRow
                    {
                        PaymentId = x.PaymentId,

                        InvoiceNumber =
                            x.Invoice?.InvoiceNumber
                            ?? "Unknown Invoice",

                        AmountText =
                            $"₱{x.Amount:N2}",

                        PaymentMethodText =
                            x.PaymentMethod,

                        ReferenceNumberText =
                            string.IsNullOrWhiteSpace(
                                x.ReferenceNumber)
                                ? "-"
                                : x.ReferenceNumber,

                        StatusText =
                            x.Status,

                        PaidAtText =
                            x.PaymentDate.ToString(
                                "MM/dd/yyyy")
                    })
                    .ToList();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Payment Search Error",
                ex.Message);
        }
    }


    // ==========================================
    // ADD PAYMENT
    // ==========================================

    private async void AddPaymentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var invoices =
                await db.Invoices
                    .AsNoTracking()
                    .OrderByDescending(x => x.InvoiceDate)
                    .ToListAsync();

            if (invoices.Count == 0)
            {
                await ShowMessageAsync(
                    "No Invoices",
                    "Create an invoice before recording a payment.");
                return;
            }

            var invoiceBox =
                new ComboBox
                {
                    Header = "Invoice",
                    ItemsSource = invoices,
                    DisplayMemberPath = "InvoiceNumber",
                    SelectedIndex = 0,
                    MinWidth = 320
                };

            var amountBox =
                new NumberBox
                {
                    Header = "Amount",
                    Minimum = 0,
                    Value = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var methodBox =
                new ComboBox
                {
                    Header = "Payment Method",
                    PlaceholderText = "Select method"
                };

            methodBox.Items.Add("Cash");
            methodBox.Items.Add("GCash");
            methodBox.Items.Add("Bank Transfer");
            methodBox.Items.Add("Card");
            methodBox.Items.Add("Other");

            methodBox.SelectedIndex = 0;

            var referenceBox =
                new TextBox
                {
                    Header = "Reference Number",
                    PlaceholderText = "Optional"
                };

            var statusBox =
                new ComboBox
                {
                    Header = "Status"
                };

            statusBox.Items.Add("Completed");
            statusBox.Items.Add("Pending");
            statusBox.Items.Add("Cancelled");

            statusBox.SelectedIndex = 0;

            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(invoiceBox);
            panel.Children.Add(amountBox);
            panel.Children.Add(methodBox);
            panel.Children.Add(referenceBox);
            panel.Children.Add(statusBox);

            var dialog =
                new ContentDialog
                {
                    Title = "Add Payment",
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
            {
                return;
            }

            if (invoiceBox.SelectedItem
                is not Invoice selectedInvoice)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an invoice.");
                return;
            }

            var amount =
                (decimal)(
                    double.IsNaN(amountBox.Value)
                        ? 0
                        : amountBox.Value);

            if (amount <= 0)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Payment amount must be greater than zero.");
                return;
            }

            var paymentStatus =
                statusBox.SelectedItem?.ToString()
                ?? "Completed";

            if (paymentStatus == "Completed")
            {
                var alreadyPaid =
                    await db.Payments
                        .Where(x =>
                            x.InvoiceId ==
                            selectedInvoice.InvoiceId &&
                            x.Status == "Completed")
                        .SumAsync(x =>
                            (decimal?)x.Amount)
                        ?? 0;

                var remaining =
                    selectedInvoice.TotalAmount -
                    alreadyPaid;

                if (amount > remaining)
                {
                    await ShowMessageAsync(
                        "Invalid Payment",
                        $"Payment cannot exceed the remaining balance of ₱{remaining:N2}.");
                    return;
                }
            }

            var payment =
                new Payment
                {
                    InvoiceId =
                        selectedInvoice.InvoiceId,

                    Amount =
                        amount,

                    PaymentMethod =
                        methodBox.SelectedItem?.ToString()
                        ?? "Cash",

                    ReferenceNumber =
                        string.IsNullOrWhiteSpace(
                            referenceBox.Text)
                            ? null
                            : referenceBox.Text.Trim(),

                    PaymentDate =
                        DateTime.UtcNow,

                    Status =
                        paymentStatus
                };

            db.Payments.Add(payment);

            await db.SaveChangesAsync();

            await UpdateInvoicePaymentStatusAsync(
                db,
                selectedInvoice.InvoiceId);

            await LoadPaymentsAsync();
            await LoadInvoicesAsync();
            await LoadFinancialReportsAsync();

            await ShowMessageAsync(
                "Payment Added",
                "The payment was added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Adding Payment",
                ex.Message);
        }
    }


    // ==========================================
    // EDIT PAYMENT
    // ==========================================

    private async void EditPaymentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not PaymentRow row)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var payment =
                await db.Payments
                    .FirstOrDefaultAsync(
                        x => x.PaymentId == row.PaymentId);

            if (payment == null)
            {
                return;
            }

            var amountBox =
                new NumberBox
                {
                    Header = "Amount",
                    Minimum = 0,
                    Value = (double)payment.Amount,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var methodBox =
                new ComboBox
                {
                    Header = "Payment Method"
                };

            methodBox.Items.Add("Cash");
            methodBox.Items.Add("GCash");
            methodBox.Items.Add("Bank Transfer");
            methodBox.Items.Add("Card");
            methodBox.Items.Add("Other");

            for (int i = 0;
                 i < methodBox.Items.Count;
                 i++)
            {
                if (methodBox.Items[i]?.ToString() ==
                    payment.PaymentMethod)
                {
                    methodBox.SelectedIndex = i;
                    break;
                }
            }

            var referenceBox =
                new TextBox
                {
                    Header = "Reference Number",
                    Text =
                        payment.ReferenceNumber ??
                        string.Empty
                };

            var statusBox =
                new ComboBox
                {
                    Header = "Status"
                };

            statusBox.Items.Add("Completed");
            statusBox.Items.Add("Pending");
            statusBox.Items.Add("Cancelled");

            for (int i = 0;
                 i < statusBox.Items.Count;
                 i++)
            {
                if (statusBox.Items[i]?.ToString() ==
                    payment.Status)
                {
                    statusBox.SelectedIndex = i;
                    break;
                }
            }

            if (statusBox.SelectedIndex < 0)
            {
                statusBox.SelectedIndex = 0;
            }

            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(amountBox);
            panel.Children.Add(methodBox);
            panel.Children.Add(referenceBox);
            panel.Children.Add(statusBox);

            var dialog =
                new ContentDialog
                {
                    Title = "Edit Payment",
                    Content = panel,

                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",

                    DefaultButton =
                        ContentDialogButton.Primary,

                    XamlRoot = XamlRoot
                };

            var result =
                await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var amount =
                (decimal)(
                    double.IsNaN(amountBox.Value)
                        ? 0
                        : amountBox.Value);

            if (amount <= 0)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Payment amount must be greater than zero.");
                return;
            }

            payment.Amount = amount;

            payment.PaymentMethod =
                methodBox.SelectedItem?.ToString()
                ?? "Cash";

            payment.ReferenceNumber =
                string.IsNullOrWhiteSpace(
                    referenceBox.Text)
                    ? null
                    : referenceBox.Text.Trim();

            payment.Status =
                statusBox.SelectedItem?.ToString()
                ?? "Completed";

            if (payment.Status == "Completed")
            {
                var completedOtherPayments =
                    await db.Payments
                        .Where(x =>
                            x.PaymentId != payment.PaymentId &&
                            x.InvoiceId == payment.InvoiceId &&
                            x.Status == "Completed")
                        .SumAsync(x =>
                            (decimal?)x.Amount)
                        ?? 0;

                var invoice =
                    await db.Invoices
                        .FirstOrDefaultAsync(
                            x => x.InvoiceId == payment.InvoiceId);

                if (invoice != null &&
                    completedOtherPayments + amount >
                    invoice.TotalAmount)
                {
                    await ShowMessageAsync(
                        "Invalid Payment",
                        "The total completed payments cannot exceed the invoice total.");
                    return;
                }
            }

            await db.SaveChangesAsync();

            await UpdateInvoicePaymentStatusAsync(
                db,
                payment.InvoiceId);

            await LoadPaymentsAsync();
            await LoadInvoicesAsync();
            await LoadFinancialReportsAsync();

            await ShowMessageAsync(
                "Payment Updated",
                "The payment was updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Updating Payment",
                ex.Message);
        }
    }


    // ==========================================
    // DELETE PAYMENT
    // ==========================================

    private async void DeletePaymentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not PaymentRow row)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            var confirm =
                new ContentDialog
                {
                    Title = "Delete Payment",
                    Content =
                        $"Delete payment #{row.PaymentId}?",

                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",

                    DefaultButton =
                        ContentDialogButton.Close,

                    XamlRoot = XamlRoot
                };

            var result =
                await confirm.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var payment =
                await db.Payments
                    .FirstOrDefaultAsync(
                        x => x.PaymentId == row.PaymentId);

            if (payment == null)
            {
                return;
            }

            var invoiceId =
                payment.InvoiceId;

            db.Payments.Remove(payment);

            await db.SaveChangesAsync();

            await UpdateInvoicePaymentStatusAsync(
                db,
                invoiceId);

            await LoadPaymentsAsync();
            await LoadInvoicesAsync();
            await LoadFinancialReportsAsync();

            await ShowMessageAsync(
                "Payment Deleted",
                "The payment was deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Deleting Payment",
                ex.Message);
        }
    }


    // ==========================================
    // UPDATE INVOICE PAYMENT STATUS
    // ==========================================

    private async Task UpdateInvoicePaymentStatusAsync(
        TenantDbContext db,
        int invoiceId)
    {
        var invoice =
            await db.Invoices
                .FirstOrDefaultAsync(
                    x => x.InvoiceId == invoiceId);

        if (invoice == null)
        {
            return;
        }

        var completedAmount =
            await db.Payments
                .Where(x =>
                    x.InvoiceId == invoiceId &&
                    x.Status == "Completed")
                .SumAsync(x =>
                    (decimal?)x.Amount)
                ?? 0;

        if (completedAmount <= 0)
        {
            invoice.Status = "Unpaid";
        }
        else if (completedAmount < invoice.TotalAmount)
        {
            invoice.Status = "Partially Paid";
        }
        else
        {
            invoice.Status = "Paid";
        }

        await db.SaveChangesAsync();
    }


    // ==========================================
    // LOAD EXPENSES
    // ==========================================

    private async Task LoadExpensesAsync()
    {
        if (CurrentUser.CompanyId == null)
        {
            ExpenseList.ItemsSource = null;
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var expenses =
                await db.Expenses
                    .AsNoTracking()
                    .Include(x => x.Branch)
                    .OrderByDescending(x => x.ExpenseDate)
                    .ToListAsync();

            ExpenseList.ItemsSource =
                expenses
                    .Select(x => new ExpenseRow
                    {
                        ExpenseId = x.ExpenseId,

                        BranchName =
                            x.Branch?.BranchName
                            ?? "No Branch",

                        CategoryText =
                            x.Category,

                        AmountText =
                            $"₱{x.Amount:N2}",

                        DescriptionText =
                            string.IsNullOrWhiteSpace(
                                x.Description)
                                ? "-"
                                : x.Description,

                        ExpenseDateText =
                            x.ExpenseDate.ToString(
                                "MM/dd/yyyy")
                    })
                    .ToList();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Expenses",
                ex.Message);
        }
    }


    // ==========================================
    // EXPENSE SEARCH
    // ==========================================

    private async void ExpenseSearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (CurrentUser.CompanyId == null)
        {
            ExpenseList.ItemsSource = null;
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var expenses =
                await db.Expenses
                    .AsNoTracking()
                    .Include(x => x.Branch)
                    .OrderByDescending(x => x.ExpenseDate)
                    .ToListAsync();

            var search =
                ExpenseSearchBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(search))
            {
                expenses =
                    expenses
                        .Where(x =>
                            x.Category.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase) ||
                            (x.Description ?? "")
                                .Contains(
                                    search,
                                    StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            ExpenseList.ItemsSource =
                expenses
                    .Select(x => new ExpenseRow
                    {
                        ExpenseId = x.ExpenseId,

                        BranchName =
                            x.Branch?.BranchName
                            ?? "No Branch",

                        CategoryText =
                            x.Category,

                        AmountText =
                            $"₱{x.Amount:N2}",

                        DescriptionText =
                            string.IsNullOrWhiteSpace(
                                x.Description)
                                ? "-"
                                : x.Description,

                        ExpenseDateText =
                            x.ExpenseDate.ToString(
                                "MM/dd/yyyy")
                    })
                    .ToList();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Expense Search Error",
                ex.Message);
        }
    }


    // ==========================================
    // ADD EXPENSE
    // ==========================================

    private async void AddExpenseButton_Click(
        object sender,
        RoutedEventArgs e)
    {

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branches =
                await db.Branches
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.BranchName)
                    .ToListAsync();

            var branchOptions =
                new List<BranchOption>
                {
                    new BranchOption
                    {
                        BranchId = null,
                        BranchName = "Company-wide / No specific branch"
                    }
                };

            branchOptions.AddRange(
                branches.Select(x => new BranchOption
                {
                    BranchId = x.BranchId,
                    BranchName = x.BranchName
                }));

            var branchBox =
                new ComboBox
                {
                    Header = "Branch",
                    ItemsSource = branchOptions,
                    DisplayMemberPath = "BranchName",
                    SelectedIndex = 0,
                    MinWidth = 320
                };

            var categoryBox =
                new ComboBox
                {
                    Header = "Category",
                    PlaceholderText = "Select category"
                };

            categoryBox.Items.Add("Supplies");
            categoryBox.Items.Add("Utilities");
            categoryBox.Items.Add("Rent");
            categoryBox.Items.Add("Transportation");
            categoryBox.Items.Add("Maintenance");
            categoryBox.Items.Add("Payroll");
            categoryBox.Items.Add("Other");

            categoryBox.SelectedIndex = 0;

            var amountBox =
                new NumberBox
                {
                    Header = "Amount",
                    Minimum = 0,
                    Value = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var datePicker =
                new CalendarDatePicker
                {
                    Header = "Expense Date",
                    Date = DateTimeOffset.Now
                };

            var descriptionBox =
                new TextBox
                {
                    Header = "Description",
                    PlaceholderText =
                        "Enter expense details...",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 90
                };

            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(branchBox);
            panel.Children.Add(categoryBox);
            panel.Children.Add(amountBox);
            panel.Children.Add(datePicker);
            panel.Children.Add(descriptionBox);

            var dialog =
                new ContentDialog
                {
                    Title = "Add Expense",
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
            {
                return;
            }

            var category =
                categoryBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(category))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an expense category.");
                return;
            }

            var amount =
                (decimal)(
                    double.IsNaN(amountBox.Value)
                        ? 0
                        : amountBox.Value);

            if (amount <= 0)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Expense amount must be greater than zero.");
                return;
            }

            if (!datePicker.Date.HasValue)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select the expense date.");
                return;
            }

            var selectedBranch =
                branchBox.SelectedItem as BranchOption;

            int? branchId =
                selectedBranch?.BranchId;

            var expense =
                new Expense
                {
                    BranchId =
                        branchId,

                    Category =
                        category,

                    Amount =
                        amount,

                    ExpenseDate =
                        datePicker.Date.Value.DateTime.Date,

                    Description =
                        string.IsNullOrWhiteSpace(
                            descriptionBox.Text)
                            ? null
                            : descriptionBox.Text.Trim()
                };

            db.Expenses.Add(expense);

            await db.SaveChangesAsync();

            await LoadExpensesAsync();
            await LoadFinancialReportsAsync();

            await ShowMessageAsync(
                "Expense Added",
                "The expense was added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Adding Expense",
                ex.Message);
        }
    }


    // ==========================================
    // EDIT EXPENSE
    // ==========================================

    private async void EditExpenseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not ExpenseRow row)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var expense =
                await db.Expenses
                    .FirstOrDefaultAsync(
                        x => x.ExpenseId == row.ExpenseId);

            if (expense == null)
            {
                return;
            }

            var branches =
                await db.Branches
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.BranchName)
                    .ToListAsync();

            // ==========================================
            // BRANCH
            // ==========================================

            var branchOptions =
                new List<BranchOption>
                {
                new BranchOption
                {
                    BranchId = null,
                    BranchName =
                        "Company-wide / No specific branch"
                }
                };

            branchOptions.AddRange(
                branches.Select(x => new BranchOption
                {
                    BranchId = x.BranchId,
                    BranchName = x.BranchName
                }));

            var branchBox =
                new ComboBox
                {
                    Header = "Branch",
                    ItemsSource = branchOptions,
                    DisplayMemberPath = "BranchName",
                    MinWidth = 320
                };

            // Select the expense's current branch
            var currentBranchIndex =
                branchOptions.FindIndex(
                    x => x.BranchId == expense.BranchId);

            branchBox.SelectedIndex =
                currentBranchIndex >= 0
                    ? currentBranchIndex
                    : 0;


            // ==========================================
            // CATEGORY
            // ==========================================

            var categoryBox =
                new ComboBox
                {
                    Header = "Category"
                };

            categoryBox.Items.Add("Supplies");
            categoryBox.Items.Add("Utilities");
            categoryBox.Items.Add("Rent");
            categoryBox.Items.Add("Transportation");
            categoryBox.Items.Add("Maintenance");
            categoryBox.Items.Add("Payroll");
            categoryBox.Items.Add("Other");

            for (int i = 0;
                 i < categoryBox.Items.Count;
                 i++)
            {
                if (categoryBox.Items[i]?.ToString() ==
                    expense.Category)
                {
                    categoryBox.SelectedIndex = i;
                    break;
                }
            }


            // ==========================================
            // AMOUNT
            // ==========================================

            var amountBox =
                new NumberBox
                {
                    Header = "Amount",
                    Minimum = 0,
                    Value = (double)expense.Amount,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };


            // ==========================================
            // DATE
            // ==========================================

            var datePicker =
                new CalendarDatePicker
                {
                    Header = "Expense Date",
                    Date =
                        new DateTimeOffset(
                            expense.ExpenseDate)
                };


            // ==========================================
            // DESCRIPTION
            // ==========================================

            var descriptionBox =
                new TextBox
                {
                    Header = "Description",
                    Text =
                        expense.Description ??
                        string.Empty,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 90
                };


            // ==========================================
            // PANEL
            // ==========================================

            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(branchBox);
            panel.Children.Add(categoryBox);
            panel.Children.Add(amountBox);
            panel.Children.Add(datePicker);
            panel.Children.Add(descriptionBox);


            // ==========================================
            // DIALOG
            // ==========================================

            var dialog =
                new ContentDialog
                {
                    Title = "Edit Expense",
                    Content = panel,

                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",

                    DefaultButton =
                        ContentDialogButton.Primary,

                    XamlRoot = XamlRoot
                };

            var result =
                await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }


            // ==========================================
            // VALIDATION
            // ==========================================

            var category =
                categoryBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(category))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an expense category.");
                return;
            }

            var amount =
                (decimal)(
                    double.IsNaN(amountBox.Value)
                        ? 0
                        : amountBox.Value);

            if (amount <= 0)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Expense amount must be greater than zero.");
                return;
            }

            if (!datePicker.Date.HasValue)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select the expense date.");
                return;
            }


            // ==========================================
            // GET SELECTED BRANCH
            // ==========================================

            var selectedBranch =
                branchBox.SelectedItem as BranchOption;

            int? branchId =
                selectedBranch?.BranchId;


            // ==========================================
            // UPDATE EXPENSE
            // ==========================================

            expense.BranchId =
                branchId;

            expense.Category =
                category;

            expense.Amount =
                amount;

            expense.ExpenseDate =
                datePicker.Date.Value.DateTime.Date;

            expense.Description =
                string.IsNullOrWhiteSpace(
                    descriptionBox.Text)
                    ? null
                    : descriptionBox.Text.Trim();


            // ==========================================
            // SAVE
            // ==========================================

            await db.SaveChangesAsync();

            await LoadExpensesAsync();
            await LoadFinancialReportsAsync();

            await ShowMessageAsync(
                "Expense Updated",
                "The expense was updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Updating Expense",
                ex.Message);
        }
    }


    // ==========================================
    // DELETE EXPENSE
    // ==========================================

    private async void DeleteExpenseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not ExpenseRow row)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            var confirm =
                new ContentDialog
                {
                    Title = "Delete Expense",
                    Content =
                        $"Delete expense #{row.ExpenseId}?",

                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",

                    DefaultButton =
                        ContentDialogButton.Close,

                    XamlRoot = XamlRoot
                };

            var result =
                await confirm.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var expense =
                await db.Expenses
                    .FirstOrDefaultAsync(
                        x => x.ExpenseId == row.ExpenseId);

            if (expense == null)
            {
                return;
            }

            db.Expenses.Remove(expense);

            await db.SaveChangesAsync();

            await LoadExpensesAsync();
            await LoadFinancialReportsAsync();

            await ShowMessageAsync(
                "Expense Deleted",
                "The expense was deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Deleting Expense",
                ex.Message);
        }
    }


    // ==========================================
    // MESSAGE DIALOG
    // ==========================================

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


    // ==========================================
    // DISPLAY ROWS
    // ==========================================

    private sealed class BranchOption
    {
        public int? BranchId { get; set; }

        public string BranchName { get; set; } = string.Empty;
    }

    private sealed class InvoiceRow
    {
        public int InvoiceId { get; set; }

        public string InvoiceNumberText { get; set; } =
            string.Empty;

        public string CustomerName { get; set; } =
            string.Empty;

        public string SubtotalText { get; set; } =
            string.Empty;

        public string DiscountText { get; set; } =
            string.Empty;

        public string TaxText { get; set; } =
            string.Empty;

        public string TotalAmountText { get; set; } =
            string.Empty;

        public string StatusText { get; set; } =
            string.Empty;

        public int RepairId { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Discount { get; set; }

        public decimal Tax { get; set; }

        public decimal TotalAmount { get; set; }
    }


    private sealed class PaymentRow
    {
        public int PaymentId { get; set; }

        public string InvoiceNumber { get; set; } =
            string.Empty;

        public string AmountText { get; set; } =
            string.Empty;

        public string PaymentMethodText { get; set; } =
            string.Empty;

        public string ReferenceNumberText { get; set; } =
            string.Empty;

        public string StatusText { get; set; } =
            string.Empty;

        public string PaidAtText { get; set; } =
            string.Empty;
    }


    private sealed class ExpenseRow
    {
        public int ExpenseId { get; set; }

        public string BranchName { get; set; } =
            string.Empty;

        public string CategoryText { get; set; } =
            string.Empty;

        public string AmountText { get; set; } =
            string.Empty;

        public string DescriptionText { get; set; } =
            string.Empty;

        public string ExpenseDateText { get; set; } =
            string.Empty;
    }


    private sealed class RepairOption
    {
        public int RepairId { get; set; }

        public string DisplayText { get; set; } =
            string.Empty;
    }
}