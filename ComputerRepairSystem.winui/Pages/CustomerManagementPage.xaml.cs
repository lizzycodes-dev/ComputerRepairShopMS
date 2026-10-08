using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.company.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class CustomerManagementPage : Page
{
    private readonly CustomerService _customerService;

    // ==========================================
    // PAGINATION STATE
    // ==========================================

    private List<Customer> _customers = new();
    private List<Customer> _filteredCustomers = new();
    private Customer? _selectedCustomer;
    private int _currentPage = 1;
    private const int _pageSize = 10;


    public CustomerManagementPage(
        CustomerService customerService)
    {
        InitializeComponent();

        _customerService = customerService;

        Loaded += CustomerManagementPage_Loaded;
    }


    // ==========================================
    // PAGE LOADED
    // ==========================================

    private async void CustomerManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadCustomersAsync();
    }


    // ==========================================
    // LOAD CUSTOMERS
    // ==========================================

    private async Task LoadCustomersAsync()
    {
        try
        {
            _customers =
                await _customerService.GetAllAsync();

            // Newest first — CustomerId is an IDENTITY column.
            _customers =
                _customers
                    .OrderByDescending(c => c.CustomerId)
                    .ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Customers",
                ex.Message);
        }
    }


    // ==========================================
    // SEARCH + PAGINATION
    // ==========================================

    private void ApplyFilter()
    {
        var search =
            SearchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(search))
        {
            _filteredCustomers = _customers;
        }
        else
        {
            _filteredCustomers =
                _customers
                    .Where(c =>
                        c.FirstName.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        c.LastName.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        (c.Phone?.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)
                            ?? false))
                    .ToList();
        }

        _currentPage = 1;

        UpdatePagination();
    }


    private void UpdatePagination()
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_filteredCustomers.Count /
                    _pageSize));

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        if (_currentPage < 1)
            _currentPage = 1;

        var pagedCustomers =
            _filteredCustomers
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        CustomerList.ItemsSource = pagedCustomers;

        PageInfoText.Text =
            $"Page {_currentPage} of {totalPages}  •  " +
            $"{_filteredCustomers.Count} total";

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
                    (double)_filteredCustomers.Count /
                    _pageSize));

        if (_currentPage >= totalPages)
            return;

        _currentPage++;

        UpdatePagination();
    }


    // ==========================================
    // SELECTION
    // ==========================================

    private void CustomerList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        _selectedCustomer =
            CustomerList.SelectedItem as Customer;
    }


    // ==========================================
    // NEW CUSTOMER
    // ==========================================

    private async void NewCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var firstNameBox = new TextBox
        {
            Header = "First Name"
        };

        var lastNameBox = new TextBox
        {
            Header = "Last Name"
        };

        var phoneBox = new TextBox
        {
            Header = "Phone"
        };

        var emailBox = new TextBox
        {
            Header = "Email"
        };

        var addressBox = new TextBox
        {
            Header = "Address"
        };

        var content = new StackPanel
        {
            Spacing = 10
        };

        content.Children.Add(firstNameBox);
        content.Children.Add(lastNameBox);
        content.Children.Add(phoneBox);
        content.Children.Add(emailBox);
        content.Children.Add(addressBox);

        var dialog = new ContentDialog
        {
            Title = "Add Customer",
            Content = content,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };

        var result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        if (string.IsNullOrWhiteSpace(
                firstNameBox.Text) ||
            string.IsNullOrWhiteSpace(
                lastNameBox.Text))
        {
            await ShowMessageAsync(
                "Validation",
                "First Name and Last Name are required.");

            return;
        }

        var customer = new Customer
        {
            FirstName = firstNameBox.Text.Trim(),
            LastName = lastNameBox.Text.Trim(),
            Phone = string.IsNullOrWhiteSpace(phoneBox.Text)
                ? null
                : phoneBox.Text.Trim(),
            Email = string.IsNullOrWhiteSpace(emailBox.Text)
                ? null
                : emailBox.Text.Trim(),
            Address = string.IsNullOrWhiteSpace(addressBox.Text)
                ? null
                : addressBox.Text.Trim()
        };

        try
        {
            await _customerService.AddAsync(customer);

            await LoadCustomersAsync();

            await ShowMessageAsync(
                "Success",
                "Customer added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                ex.Message);
        }
    }


    // ==========================================
    // EDIT CUSTOMER
    // ==========================================

    private async void EditCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Prefer the button's Tag (row-level button),
        // otherwise fall back to the ListView selection.
        Customer? customer = null;

        if (sender is Button button &&
            button.Tag is Customer tagCustomer)
        {
            customer = tagCustomer;
        }
        else
        {
            customer = _selectedCustomer;
        }

        if (customer == null)
        {
            await ShowMessageAsync(
                "Edit Customer",
                "Please select a customer first.");

            return;
        }

        _selectedCustomer = customer;

        var firstNameBox = new TextBox
        {
            Header = "First Name",
            Text = customer.FirstName
        };

        var lastNameBox = new TextBox
        {
            Header = "Last Name",
            Text = customer.LastName
        };

        var phoneBox = new TextBox
        {
            Header = "Phone",
            Text = customer.Phone ?? string.Empty
        };

        var emailBox = new TextBox
        {
            Header = "Email",
            Text = customer.Email ?? string.Empty
        };

        var addressBox = new TextBox
        {
            Header = "Address",
            Text = customer.Address ?? string.Empty
        };

        var content = new StackPanel
        {
            Spacing = 10
        };

        content.Children.Add(firstNameBox);
        content.Children.Add(lastNameBox);
        content.Children.Add(phoneBox);
        content.Children.Add(emailBox);
        content.Children.Add(addressBox);

        var dialog = new ContentDialog
        {
            Title = "Edit Customer",
            Content = content,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };

        var result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        if (string.IsNullOrWhiteSpace(
                firstNameBox.Text) ||
            string.IsNullOrWhiteSpace(
                lastNameBox.Text))
        {
            await ShowMessageAsync(
                "Validation",
                "First Name and Last Name are required.");

            return;
        }

        customer.FirstName =
            firstNameBox.Text.Trim();

        customer.LastName =
            lastNameBox.Text.Trim();

        customer.Phone =
            string.IsNullOrWhiteSpace(phoneBox.Text)
                ? null
                : phoneBox.Text.Trim();

        customer.Email =
            string.IsNullOrWhiteSpace(emailBox.Text)
                ? null
                : emailBox.Text.Trim();

        customer.Address =
            string.IsNullOrWhiteSpace(addressBox.Text)
                ? null
                : addressBox.Text.Trim();

        try
        {
            await _customerService.UpdateAsync(customer);

            await LoadCustomersAsync();

            await ShowMessageAsync(
                "Success",
                "Customer updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                ex.Message);
        }
    }


    // ==========================================
    // DELETE CUSTOMER
    // ==========================================

    private async void DeleteCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Prefer the button's Tag (row-level button),
        // otherwise fall back to the ListView selection.
        Customer? customer = null;

        if (sender is Button button &&
            button.Tag is Customer tagCustomer)
        {
            customer = tagCustomer;
        }
        else
        {
            customer = _selectedCustomer;
        }

        if (customer == null)
        {
            await ShowMessageAsync(
                "Delete Customer",
                "Please select a customer first.");

            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Delete Customer",
            Content =
                $"Delete {customer.FirstName} " +
                $"{customer.LastName}?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };

        var result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        try
        {
            await _customerService.DeleteAsync(
                customer.CustomerId);

            _selectedCustomer = null;

            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                ex.Message);
        }
    }


    // ==========================================
    // SEARCH
    // ==========================================

    private void SearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyFilter();
    }


    // ==========================================
    // MESSAGE
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