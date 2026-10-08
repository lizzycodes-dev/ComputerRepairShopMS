using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem.winui.Pages;

public sealed partial class SupplierManagementPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;

    // ==========================================
    // PAGINATION STATE
    // ==========================================

    private List<Supplier> _allSuppliers = new();
    private List<Supplier> _filteredSuppliers = new();
    private int _currentPage = 1;
    private const int _pageSize = 10;

    public SupplierManagementPage(
        TenantDbContextFactory tenantDbFactory)
    {
        this.InitializeComponent();

        _tenantDbFactory = tenantDbFactory;

        Loaded += SupplierManagementPage_Loaded;
    }

    private async void SupplierManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadSuppliersAsync();
    }

    // ==========================================
    // LOAD SUPPLIERS
    // ==========================================

    private async Task LoadSuppliersAsync()
    {
        if (CurrentUser.CompanyId == null)
            return;

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var search = SearchBox.Text?.Trim();

            var query = db.Suppliers
                .AsNoTracking()
                .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.SupplierCode.Contains(search) ||
                    x.SupplierName.Contains(search) ||
                    (x.ContactPerson != null &&
                     x.ContactPerson.Contains(search)) ||
                    (x.Phone != null &&
                     x.Phone.Contains(search)) ||
                    (x.Email != null &&
                     x.Email.Contains(search)));
            }

            _allSuppliers = await query
                .OrderByDescending(x => x.SupplierId)
                .ToListAsync();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                $"Unable to load suppliers.\n\n{ex.Message}");
        }
    }


    private void ApplyFilter()
    {
        _filteredSuppliers = _allSuppliers;

        _currentPage = 1;

        UpdatePagination();
    }

    // ==========================================
    // SEARCH
    // ==========================================

    private async void SearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        await LoadSuppliersAsync();
    }

    // ==========================================
    // REFRESH
    // ==========================================

    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;

        await LoadSuppliersAsync();
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
                    (double)_filteredSuppliers.Count /
                    _pageSize));

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        if (_currentPage < 1)
            _currentPage = 1;

        var pagedSuppliers =
            _filteredSuppliers
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        SupplierListView.ItemsSource = pagedSuppliers;

        PageInfoText.Text =
            $"Page {_currentPage} of {totalPages}  •  " +
            $"{_filteredSuppliers.Count} total";

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
                    (double)_filteredSuppliers.Count /
                    _pageSize));

        if (_currentPage >= totalPages)
            return;

        _currentPage++;

        UpdatePagination();
    }

    // ==========================================
    // ADD SUPPLIER
    // ==========================================

    private async void AddSupplierButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var fields = CreateSupplierFields();

        var dialog = new ContentDialog
        {
            Title = "Add Supplier",
            Content = fields.Panel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        if (string.IsNullOrWhiteSpace(fields.Code.Text) ||
            string.IsNullOrWhiteSpace(fields.Name.Text))
        {
            await ShowMessageAsync(
                "Supplier Code and Supplier Name are required.");

            return;
        }

        if (CurrentUser.CompanyId == null)
            return;

        await using var db =
            await _tenantDbFactory.CreateAsync(
                CurrentUser.CompanyId.Value);

        var supplierCode = fields.Code.Text.Trim();

        var duplicateCode = await db.Suppliers
            .AnyAsync(x => x.SupplierCode == supplierCode);

        if (duplicateCode)
        {
            await ShowMessageAsync(
                "A supplier with this code already exists.");

            return;
        }

        var supplier = new Supplier
        {
            SupplierCode = supplierCode,
            SupplierName = fields.Name.Text.Trim(),
            ContactPerson = EmptyToNull(fields.Contact.Text),
            Phone = EmptyToNull(fields.Phone.Text),
            Email = EmptyToNull(fields.Email.Text),
            Address = EmptyToNull(fields.Address.Text),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Suppliers.Add(supplier);

        await db.SaveChangesAsync();

        await LoadSuppliersAsync();
    }

    // ==========================================
    // EDIT SUPPLIER
    // ==========================================

    private async void EditSupplierButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not Supplier supplierRow)
        {
            return;
        }

        var fields = CreateSupplierFields();

        fields.Code.Text = supplierRow.SupplierCode;
        fields.Name.Text = supplierRow.SupplierName;
        fields.Contact.Text = supplierRow.ContactPerson ?? string.Empty;
        fields.Phone.Text = supplierRow.Phone ?? string.Empty;
        fields.Email.Text = supplierRow.Email ?? string.Empty;
        fields.Address.Text = supplierRow.Address ?? string.Empty;

        var dialog = new ContentDialog
        {
            Title = "Edit Supplier",
            Content = fields.Panel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        if (string.IsNullOrWhiteSpace(fields.Code.Text) ||
            string.IsNullOrWhiteSpace(fields.Name.Text))
        {
            await ShowMessageAsync(
                "Supplier Code and Supplier Name are required.");

            return;
        }

        if (CurrentUser.CompanyId == null)
            return;

        await using var db =
            await _tenantDbFactory.CreateAsync(
                CurrentUser.CompanyId.Value);

        var supplier = await db.Suppliers
            .FirstOrDefaultAsync(
                x => x.SupplierId == supplierRow.SupplierId);

        if (supplier == null)
            return;

        var newCode = fields.Code.Text.Trim();

        var duplicateCode = await db.Suppliers
            .AnyAsync(x =>
                x.SupplierCode == newCode &&
                x.SupplierId != supplier.SupplierId);

        if (duplicateCode)
        {
            await ShowMessageAsync(
                "Another supplier already uses this code.");

            return;
        }

        supplier.SupplierCode = newCode;
        supplier.SupplierName = fields.Name.Text.Trim();
        supplier.ContactPerson = EmptyToNull(fields.Contact.Text);
        supplier.Phone = EmptyToNull(fields.Phone.Text);
        supplier.Email = EmptyToNull(fields.Email.Text);
        supplier.Address = EmptyToNull(fields.Address.Text);

        await db.SaveChangesAsync();

        await LoadSuppliersAsync();
    }

    // ==========================================
    // DELETE SUPPLIER
    // ==========================================

    private async void DeleteSupplierButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not Supplier supplierRow)
        {
            return;
        }

        var confirmDialog = new ContentDialog
        {
            Title = "Delete Supplier",
            Content =
                $"Are you sure you want to delete " +
                $"'{supplierRow.SupplierName}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await confirmDialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        if (CurrentUser.CompanyId == null)
            return;

        await using var db =
            await _tenantDbFactory.CreateAsync(
                CurrentUser.CompanyId.Value);

        var supplier = await db.Suppliers
            .FirstOrDefaultAsync(
                x => x.SupplierId == supplierRow.SupplierId);

        if (supplier == null)
            return;

        db.Suppliers.Remove(supplier);

        await db.SaveChangesAsync();

        await LoadSuppliersAsync();
    }

    // ==========================================
    // SUPPLIER FORM
    // ==========================================

    private static SupplierFields CreateSupplierFields()
    {
        var codeBox = new TextBox
        {
            Header = "Supplier Code",
            PlaceholderText = "e.g. SUP-001",
            Margin = new Thickness(0, 0, 0, 10)
        };

        var nameBox = new TextBox
        {
            Header = "Supplier Name",
            PlaceholderText = "Enter supplier name",
            Margin = new Thickness(0, 0, 0, 10)
        };

        var contactBox = new TextBox
        {
            Header = "Contact Person",
            PlaceholderText = "Optional",
            Margin = new Thickness(0, 0, 0, 10)
        };

        var phoneBox = new TextBox
        {
            Header = "Phone",
            PlaceholderText = "Optional",
            Margin = new Thickness(0, 0, 0, 10)
        };

        var emailBox = new TextBox
        {
            Header = "Email",
            PlaceholderText = "Optional",
            Margin = new Thickness(0, 0, 0, 10)
        };

        var addressBox = new TextBox
        {
            Header = "Address",
            PlaceholderText = "Optional",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap
        };

        var panel = new StackPanel
        {
            Width = 400,
            Spacing = 4
        };

        panel.Children.Add(codeBox);
        panel.Children.Add(nameBox);
        panel.Children.Add(contactBox);
        panel.Children.Add(phoneBox);
        panel.Children.Add(emailBox);
        panel.Children.Add(addressBox);

        return new SupplierFields
        {
            Panel = panel,
            Code = codeBox,
            Name = nameBox,
            Contact = contactBox,
            Phone = phoneBox,
            Email = emailBox,
            Address = addressBox
        };
    }

    // ==========================================
    // MESSAGE
    // ==========================================

    private async Task ShowMessageAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Supplier Management",
            Content = message,
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private static string? EmptyToNull(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? null
            : text.Trim();
    }

    // ==========================================
    // FORM HOLDER
    // ==========================================

    private sealed class SupplierFields
    {
        public StackPanel Panel { get; init; } = null!;

        public TextBox Code { get; init; } = null!;
        public TextBox Name { get; init; } = null!;
        public TextBox Contact { get; init; } = null!;
        public TextBox Phone { get; init; } = null!;
        public TextBox Email { get; init; } = null!;
        public TextBox Address { get; init; } = null!;
    }
}