using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.company.Services;
using ComputerRepairSystem.infrastructure.data;

using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace ComputerRepairSystem_winui.Pages;

public sealed partial class ServiceManagementPage : Page
{
    private readonly CustomerService _customerService;
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly MasterErpDbContext _masterDb;

    private Customer? _selectedCustomer;
    private List<Customer> _customers = new();
    private int _currentPage = 1;
    private const int _pageSize = 5;
    public ServiceManagementPage(
        CustomerService customerService,
        TenantDbContextFactory tenantDbFactory,
        MasterErpDbContext masterDb)
    {
        InitializeComponent();

        _customerService = customerService;
        _tenantDbFactory = tenantDbFactory;
        _masterDb = masterDb;

        Loaded += ServiceManagementPage_Loaded;
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

    private async Task<int?> GetCurrentBranchIdAsync(
        TenantDbContext db)
    {
        if (string.IsNullOrWhiteSpace(CurrentUser.UserId))
            return null;

        var employee =
            await db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.MasterUserId == CurrentUser.UserId &&
                    e.IsActive);

        return employee?.BranchId;
    }

    private async void ServiceManagementPage_Loaded (
        object sender,
        RoutedEventArgs e)
    {
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customers =
                await _customerService.GetAllAsync();

            _customers = customers.ToList();

            _currentPage = 1;

            UpdateCustomerPagination();
        }
        catch (Exception ex)
        {
            ShowStatus(
                "Failed to load customers: " + ex.Message,
                InfoBarSeverity.Error);
        }
    }

    private async void AddButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ValidateInput())
            return;

        try
        {
            var customer = new Customer
            {
                FirstName = FirstNameBox.Text.Trim(),

                MiddleName = string.IsNullOrWhiteSpace(
                    MiddleNameBox.Text)
                    ? null
                    : MiddleNameBox.Text.Trim(),

                LastName = LastNameBox.Text.Trim(),

                Phone = string.IsNullOrWhiteSpace(
                    PhoneBox.Text)
                    ? null
                    : PhoneBox.Text.Trim(),

                Email = string.IsNullOrWhiteSpace(
                    EmailBox.Text)
                    ? null
                    : EmailBox.Text.Trim(),

                Address = string.IsNullOrWhiteSpace(
                    AddressBox.Text)
                    ? null
                    : AddressBox.Text.Trim()
            };

            await _customerService.AddAsync(customer);

            ClearForm();

            await LoadCustomersAsync();

            ShowStatus(
                "Customer added successfully.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus(
                "Failed to add customer: " + ex.Message,
                InfoBarSeverity.Error);
        }
    }

    private async void UpdateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedCustomer == null)
        {
            ShowStatus(
                "Please select a customer first.",
                InfoBarSeverity.Warning);

            return;
        }

        if (!ValidateInput())
            return;

        try
        {
            _selectedCustomer.FirstName =
                FirstNameBox.Text.Trim();

            _selectedCustomer.MiddleName =
                string.IsNullOrWhiteSpace(MiddleNameBox.Text)
                    ? null
                    : MiddleNameBox.Text.Trim();

            _selectedCustomer.LastName =
                LastNameBox.Text.Trim();

            _selectedCustomer.Phone =
                string.IsNullOrWhiteSpace(PhoneBox.Text)
                    ? null
                    : PhoneBox.Text.Trim();

            _selectedCustomer.Email =
                string.IsNullOrWhiteSpace(EmailBox.Text)
                    ? null
                    : EmailBox.Text.Trim();

            _selectedCustomer.Address =
                string.IsNullOrWhiteSpace(AddressBox.Text)
                    ? null
                    : AddressBox.Text.Trim();

            await _customerService.UpdateAsync(
                _selectedCustomer);

            ClearForm();

            await LoadCustomersAsync();

            ShowStatus(
                "Customer updated successfully.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus(
                "Failed to update customer: " + ex.Message,
                InfoBarSeverity.Error);
        }
    }

    private async void DeleteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedCustomer == null)
        {
            ShowStatus(
                "Please select a customer first.",
                InfoBarSeverity.Warning);

            return;
        }

        try
        {
            await _customerService.DeleteAsync(
                _selectedCustomer.CustomerId);

            ClearForm();

            await LoadCustomersAsync();

            ShowStatus(
                "Customer deleted successfully.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus(
                "Failed to delete customer: " + ex.Message,
                InfoBarSeverity.Error);
        }
    }

    private void ClearButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ClearForm();
    }



    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(
                FirstNameBox.Text))
        {
            ShowStatus(
                "First Name is required.",
                InfoBarSeverity.Warning);

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                LastNameBox.Text))
        {
            ShowStatus(
                "Last Name is required.",
                InfoBarSeverity.Warning);

            return false;
        }

        return true;
    }
    private void ClearForm()
    {
        _selectedCustomer = null;

        // Customer
        FirstNameBox.Text = string.Empty;
        MiddleNameBox.Text = string.Empty;
        LastNameBox.Text = string.Empty;
        PhoneBox.Text = string.Empty;
        EmailBox.Text = string.Empty;
        AddressBox.Text = string.Empty;

        // Device
        DeviceTypeBox.Text = string.Empty;
        BrandBox.Text = string.Empty;
        ModelBox.Text = string.Empty;
        SerialNumberBox.Text = string.Empty;
        DeviceConditionBox.Text = string.Empty;

        // Service Request
        DescriptionBox.Text = string.Empty;

        PriorityBox.SelectedIndex = 1;
        StatusBox.SelectedIndex = 0;

        CustomerList.SelectedItem = null;
    }

    private void ShowStatus(
        string message,
        InfoBarSeverity severity)
    {
        StatusBar.Message = message;
        StatusBar.Severity = severity;
        StatusBar.IsOpen = true;
    }

    //Customer info
    private async void SubmitRequestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        // ==========================================
        // VALIDATE CUSTOMER
        // ==========================================

        if (!ValidateCustomerInput())
            return;


        // ==========================================
        // VALIDATE DEVICE
        // ==========================================

        if (!ValidateDeviceInput())
            return;


        // ==========================================
        // VALIDATE SERVICE REQUEST
        // ==========================================

        if (string.IsNullOrWhiteSpace(
                DescriptionBox.Text))
        {
            ShowStatus(
                "Service request description is required.",
                InfoBarSeverity.Warning);

            return;
        }


        try
        {
            int customerId;


            // ==========================================
            // 1. CREATE OR REUSE CUSTOMER
            // ==========================================

            if (_selectedCustomer == null)
            {
                // NEW CUSTOMER

                var customer = new Customer
                {
                    FirstName =
                        FirstNameBox.Text.Trim(),

                    MiddleName =
                        string.IsNullOrWhiteSpace(
                            MiddleNameBox.Text)
                            ? null
                            : MiddleNameBox.Text.Trim(),

                    LastName =
                        LastNameBox.Text.Trim(),

                    Phone =
                        string.IsNullOrWhiteSpace(
                            PhoneBox.Text)
                            ? null
                            : PhoneBox.Text.Trim(),

                    Email =
                        string.IsNullOrWhiteSpace(
                            EmailBox.Text)
                            ? null
                            : EmailBox.Text.Trim(),

                    Address =
                        string.IsNullOrWhiteSpace(
                            AddressBox.Text)
                            ? null
                            : AddressBox.Text.Trim()
                };


                await _customerService.AddAsync(
                    customer);


                customerId =
                    customer.CustomerId;
            }
            else
            {
                // EXISTING CUSTOMER

                customerId =
                    _selectedCustomer.CustomerId;
            }


            // ==========================================
            // 2. CREATE DEVICE
            // ==========================================

            if (CurrentUser.CompanyId == null)
            {
                return;
            }

            await using var context =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? currentBranchId = null;

            if (branchManagementEnabled)
            {
                currentBranchId =
                    await GetCurrentBranchIdAsync(context);

                if (currentBranchId == null)
                {
                    ShowStatus(
                        "The logged-in employee is not assigned to a branch.",
                        InfoBarSeverity.Warning);

                    return;
                }
            }


            var device = new Device
            {
                CustomerId =
                    customerId,

                DeviceType =
                    DeviceTypeBox.Text.Trim(),

                Brand =
                    BrandBox.Text.Trim(),

                Model =
                    ModelBox.Text.Trim(),

                SerialNumber =
                    string.IsNullOrWhiteSpace(
                        SerialNumberBox.Text)
                        ? null
                        : SerialNumberBox.Text.Trim(),

                DeviceCondition =
                    string.IsNullOrWhiteSpace(
                        DeviceConditionBox.Text)
                        ? null
                        : DeviceConditionBox.Text.Trim()
            };


            context.Devices.Add(device);

            await context.SaveChangesAsync();



            // ==========================================
            // 3. CREATE SERVICE REQUEST
            // ==========================================

            var priority =
                (PriorityBox.SelectedItem
                    as ComboBoxItem)?
                    .Content?
                    .ToString()
                ?? "Medium";


            var status =
                (StatusBox.SelectedItem
                    as ComboBoxItem)?
                    .Content?
                    .ToString()
                ?? "Pending";


            var serviceRequest = new ServiceRequest
            {
                DeviceId = device.DeviceId,
                BranchId = currentBranchId,
                Description = DescriptionBox.Text.Trim(),
                Priority = priority,
                Status = status,
                RequestDate = DateTime.UtcNow
            };


            context.ServiceRequests.Add(
                serviceRequest);

            await context.SaveChangesAsync();


            // ==========================================
            // SUCCESS
            // ==========================================

            ClearForm();

            await LoadCustomersAsync();

            ShowStatus(
                "Service request created successfully.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowStatus(
                "Failed to create service request: "
                + ex.Message,
                InfoBarSeverity.Error);
        }
    }
    private bool ValidateCustomerInput()
    {
        if (string.IsNullOrWhiteSpace(
                FirstNameBox.Text))
        {
            ShowStatus(
                "First Name is required.",
                InfoBarSeverity.Warning);

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                LastNameBox.Text))
        {
            ShowStatus(
                "Last Name is required.",
                InfoBarSeverity.Warning);

            return false;
        }

        return true;
    }
    private bool ValidateDeviceInput()
    {
        if (string.IsNullOrWhiteSpace(
                DeviceTypeBox.Text))
        {
            ShowStatus(
                "Device Type is required.",
                InfoBarSeverity.Warning);

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                BrandBox.Text))
        {
            ShowStatus(
                "Brand is required.",
                InfoBarSeverity.Warning);

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                ModelBox.Text))
        {
            ShowStatus(
                "Model is required.",
                InfoBarSeverity.Warning);

            return false;
        }

        return true;
    }

    private void CustomerSearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        var search =
            CustomerSearchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(search))
        {
            CustomerList.ItemsSource =
                _customers;

            return;
        }

        var results =
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
                    (
                        c.FirstName + " " + c.LastName
                    ).Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        CustomerList.ItemsSource =
            results;
    }
    private void NewCustomerButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        _selectedCustomer = null;

        CustomerList.SelectedItem = null;

        CustomerSearchBox.Text =
            string.Empty;

        FirstNameBox.Text =
            string.Empty;

        MiddleNameBox.Text =
            string.Empty;

        LastNameBox.Text =
            string.Empty;

        PhoneBox.Text =
            string.Empty;

        EmailBox.Text =
            string.Empty;

        AddressBox.Text =
            string.Empty;

        ShowStatus(
            "Ready to register a new customer.",
            InfoBarSeverity.Informational);
    }

    private async void ViewCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.Tag is not Customer customer)
        {
            return;
        }

        _selectedCustomer = customer;

        await ShowCustomerDetailsAsync(
            customer.CustomerId);
    }
    private async Task ShowCustomerDetailsAsync(
        int customerId)
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            // ==========================================
            // LOAD CUSTOMER
            // ==========================================

            var customer =
                await db.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c =>
                            c.CustomerId == customerId);

            if (customer == null)
            {
                return;
            }


            // ==========================================
            // LOAD DEVICES + SERVICE REQUESTS
            // ==========================================

            var devices =
                await db.Devices
                    .AsNoTracking()
                    .Where(d =>
                        d.CustomerId == customerId)
                    .OrderBy(d =>
                        d.DeviceId)
                    .ToListAsync();


            var deviceIds =
                devices
                    .Select(d => d.DeviceId)
                    .ToList();


            var branchManagementEnabled =
                await HasBranchManagementAsync();

            var serviceRequestQuery =
                db.ServiceRequests
                    .AsNoTracking()
                    .Where(r =>
                        deviceIds.Contains(r.DeviceId));

            if (branchManagementEnabled)
            {
                var currentBranchId =
                    await GetCurrentBranchIdAsync(db);

                if (currentBranchId == null)
                {
                    ShowStatus(
                        "The logged-in employee is not assigned to a branch.",
                        InfoBarSeverity.Warning);

                    return;
                }

                serviceRequestQuery =
                    serviceRequestQuery.Where(r =>
                        r.BranchId == currentBranchId.Value);
            }

            var serviceRequests =
                await serviceRequestQuery
                    .OrderByDescending(r =>
                        r.RequestDate)
                    .ToListAsync();


            // ==========================================
            // DEVICE EXPANDERS
            // ==========================================

            var devicePanel =
                new StackPanel
                {
                    Spacing = 8
                };


            foreach (var device in devices)
            {
                var deviceRequests =
                    serviceRequests
                        .Where(r =>
                            r.DeviceId ==
                            device.DeviceId)
                        .ToList();


                // ------------------------------
                // DEVICE INFORMATION
                // ------------------------------

                var deviceHeader =
                    new StackPanel
                    {
                        Spacing = 2
                    };


                deviceHeader.Children.Add(
                    new TextBlock
                    {
                        Text =
                            $"{device.Brand} {device.Model}",

                        FontWeight =
                            Microsoft.UI.Text.FontWeights.SemiBold
                    });


                deviceHeader.Children.Add(
                    new TextBlock
                    {
                        Text =
                            $"{device.DeviceType} • " +
                            $"{device.SerialNumber ?? "No serial number"}",

                        Opacity = 0.7
                    });


                // ------------------------------
                // REQUEST LIST
                // ------------------------------

                var requestPanel =
                    new StackPanel
                    {
                        Spacing = 10
                    };


                if (deviceRequests.Count == 0)
                {
                    requestPanel.Children.Add(
                        new TextBlock
                        {
                            Text =
                                "No service requests for this device.",

                            Opacity = 0.6
                        });
                }
                else
                {
                    requestPanel.Children.Add(
                        new TextBlock
                        {
                            Text =
                                "Service Requests",

                            FontWeight =
                                Microsoft.UI.Text.FontWeights.SemiBold
                        });


                    foreach (var request
                        in deviceRequests)
                    {
                        var requestBorder =
                            new Border
                            {
                                Padding =
                                    new Thickness(10),

                                BorderThickness =
                                    new Thickness(1),

                                CornerRadius = new CornerRadius(6)
                            };


                        var requestContent =
                            new StackPanel
                            {
                                Spacing = 4
                            };


                        requestContent.Children.Add(
                            new TextBlock
                            {
                                Text =
                                    $"Request #{request.ServiceRequestId}",

                                FontWeight =
                                    Microsoft.UI.Text.FontWeights.SemiBold
                            });


                        requestContent.Children.Add(
                            new TextBlock
                            {
                                Text =
                                    request.Description,

                                TextWrapping =
                                    TextWrapping.Wrap
                            });


                        requestContent.Children.Add(
                            new TextBlock
                            {
                                Text =
                                    $"Priority: {request.Priority}",

                                Opacity = 0.8
                            });


                        requestContent.Children.Add(
                            new TextBlock
                            {
                                Text =
                                    $"Status: {request.Status}",

                                Opacity = 0.8
                            });


                        requestContent.Children.Add(
                            new TextBlock
                            {
                                Text =
                                    request.RequestDate
                                        .ToLocalTime()
                                        .ToString(
                                            "MMM dd, yyyy hh:mm tt"),

                                Opacity = 0.6
                            });


                        requestBorder.Child =
                            requestContent;


                        requestPanel.Children.Add(
                            requestBorder);
                    }
                }


                // ------------------------------
                // EXPANDER
                // ------------------------------

                var expander =
                    new Expander
                    {
                        Header =
                            deviceHeader,

                        Content =
                            requestPanel,

                        HorizontalAlignment =
                            HorizontalAlignment.Stretch,

                        IsExpanded = false
                    };


                devicePanel.Children.Add(
                    expander);
            }


            // ==========================================
            // NO DEVICES
            // ==========================================

            if (devices.Count == 0)
            {
                devicePanel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            "This customer has no registered devices.",

                        Opacity = 0.6
                    });
            }


            // ==========================================
            // CUSTOMER INFORMATION
            // ==========================================

            var customerInfo =
                new StackPanel
                {
                    Spacing = 4
                };


            customerInfo.Children.Add(
                new TextBlock
                {
                    Text =
                        $"{customer.FirstName} " +
                        $"{customer.MiddleName} " +
                        $"{customer.LastName}",

                    FontSize = 24,

                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                });


            customerInfo.Children.Add(
                new TextBlock
                {
                    Text =
                        customer.Email ??
                        "No email",

                    Opacity = 0.8
                });


            customerInfo.Children.Add(
                new TextBlock
                {
                    Text =
                        customer.Phone ??
                        "No phone number",

                    Opacity = 0.7
                });


            customerInfo.Children.Add(
                new TextBlock
                {
                    Text =
                        customer.Address ??
                        "No address",

                    Opacity = 0.7
                });


            // ==========================================
            // MODAL CONTENT
            // ==========================================

            var content =
                new StackPanel
                {
                    Spacing = 16
                };


            content.Children.Add(
                customerInfo);


            content.Children.Add(
                new TextBlock
                {
                    Text = "Devices",
                    FontSize = 20,

                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                });


            var deviceScrollViewer =
                new ScrollViewer
                {
                    Content =
                        devicePanel,

                    MaxHeight = 450,

                    VerticalScrollBarVisibility =
                        ScrollBarVisibility.Auto
                };


            content.Children.Add(
                deviceScrollViewer);


            // ==========================================
            // DIALOG
            // ==========================================

            var dialog =
                new ContentDialog
                {
                    Title =
                        "Customer Details",

                    Content =
                        content,

                    CloseButtonText =
                        "Close",

                    XamlRoot =
                        XamlRoot
                };


            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            ShowStatus(
                "Failed to load customer details: " +
                ex.Message,
                InfoBarSeverity.Error);
        }
    }
    private async void CustomerList_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (CustomerList.SelectedItem is not Customer customer)
        {
            return;
        }

        _selectedCustomer = customer;

        // ==========================================
        // FILL CUSTOMER INFORMATION
        // ==========================================

        FirstNameBox.Text =
            customer.FirstName;

        MiddleNameBox.Text =
            customer.MiddleName ?? string.Empty;

        LastNameBox.Text =
            customer.LastName;

        PhoneBox.Text =
            customer.Phone ?? string.Empty;

        EmailBox.Text =
            customer.Email ?? string.Empty;

        AddressBox.Text =
            customer.Address ?? string.Empty;


        // ==========================================
        // CLEAR DEVICE/REQUEST FORM
        // ==========================================

        DeviceTypeBox.Text = string.Empty;
        BrandBox.Text = string.Empty;
        ModelBox.Text = string.Empty;
        SerialNumberBox.Text = string.Empty;
        DeviceConditionBox.Text = string.Empty;

        DescriptionBox.Text = string.Empty;

        PriorityBox.SelectedIndex = 1;
        StatusBox.SelectedIndex = 0;
    }

    private void UpdateCustomerPagination()
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_customers.Count /
                    _pageSize));

        if (_currentPage > totalPages)
        {
            _currentPage = totalPages;
        }

        var pagedCustomers =
            _customers
                .Skip(
                    (_currentPage - 1) *
                    _pageSize)
                .Take(_pageSize)
                .ToList();

        CustomerList.ItemsSource =
            pagedCustomers;

        PageInfoText.Text =
            $"Page {_currentPage} of {totalPages}";

        PreviousPageButton.IsEnabled =
            _currentPage > 1;

        NextPageButton.IsEnabled =
            _currentPage < totalPages;
    }
    private void PreviousPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_currentPage <= 1)
        {
            return;
        }

        _currentPage--;

        UpdateCustomerPagination();
    }
    private void NextPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var totalPages =
            (int)Math.Ceiling(
                (double)_customers.Count /
                _pageSize);

        if (_currentPage >= totalPages)
        {
            return;
        }

        _currentPage++;

        UpdateCustomerPagination();
    }
}
