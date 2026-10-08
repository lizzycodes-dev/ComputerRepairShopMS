using ComputerRepairSystem.company.Context;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class RepairManagementPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly MasterErpDbContext _masterDb;
    private readonly CurrentBranchContext _currentBranchContext;

    // ==========================================
    // PAGINATION — SERVICE REQUESTS
    // ==========================================

    private List<ServiceRequest> _serviceRequests = new();
    private int _serviceRequestPage = 1;

    // ==========================================
    // PAGINATION — ACTIVE REPAIRS
    // ==========================================

    private List<Repair> _repairs = new();
    private int _repairPage = 1;

    // ==========================================
    // PAGINATION — REPAIR HISTORY
    // ==========================================

    private List<Repair> _repairHistory = new();
    private int _historyPage = 1;

    private const int _pageSize = 10;


    public RepairManagementPage(
        TenantDbContextFactory tenantDbFactory,
        MasterErpDbContext masterDb,
        CurrentBranchContext currentBranchContext)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;
        _masterDb = masterDb;
        _currentBranchContext = currentBranchContext;

        Loaded += RepairManagementPage_Loaded;
        Unloaded += RepairManagementPage_Unloaded;
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

    private async void RepairManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged += OnGlobalBranchChanged;

        await LoadServiceRequestsAsync();
        await LoadRepairsAsync();
        await LoadRepairHistoryAsync();
    }

    private void RepairManagementPage_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged -= OnGlobalBranchChanged;
    }

    private async void OnGlobalBranchChanged()
    {
        if (!IsLoaded)
            return;

        await LoadServiceRequestsAsync();
        await LoadRepairsAsync();
        await LoadRepairHistoryAsync();
    }

    // ==========================================
    // TAB REFRESH
    // ==========================================

    private async void RepairTabView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        switch (RepairTabView.SelectedIndex)
        {
            case 0:
                await LoadServiceRequestsAsync();
                break;

            case 1:
                await LoadRepairsAsync();
                break;

            case 2:
                await LoadRepairHistoryAsync();
                break;
        }
    }


    // ==========================================
    // LOAD SERVICE REQUESTS
    // ==========================================

    private async Task LoadServiceRequestsAsync()
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                _serviceRequests = new();
                UpdateServiceRequestPagination();
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            var query =
                db.ServiceRequests
                    .AsNoTracking()
                    .Include(x => x.Device)
                        .ThenInclude(x => x.Customer)
                    .Where(x =>
                        x.Status != "In Repair" &&
                        x.Status != "Completed" &&
                        x.Status != "Cancelled");

            if (branchManagementEnabled)
            {
                var currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    _serviceRequests = new();
                    UpdateServiceRequestPagination();

                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }

                query = query.Where(x =>
                    x.BranchId == currentBranchId.Value);
            }

            // Newest first — ServiceRequestId is an IDENTITY column
            _serviceRequests =
                await query
                    .OrderByDescending(x => x.ServiceRequestId)
                    .ToListAsync();

            _serviceRequestPage = 1;

            UpdateServiceRequestPagination();
        }
        catch (Exception ex)
        {
            _serviceRequests = new();
            UpdateServiceRequestPagination();

            System.Diagnostics.Debug.WriteLine(
                "Failed to load service requests: " + ex);
        }
    }


    private void UpdateServiceRequestPagination()
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_serviceRequests.Count / _pageSize));

        if (_serviceRequestPage > totalPages)
            _serviceRequestPage = totalPages;

        if (_serviceRequestPage < 1)
            _serviceRequestPage = 1;

        var paged =
            _serviceRequests
                .Skip((_serviceRequestPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        ServiceRequestList.ItemsSource = paged;

        ServiceRequestPageInfoText.Text =
            $"Page {_serviceRequestPage} of {totalPages}  •  " +
            $"{_serviceRequests.Count} total";

        PreviousServiceRequestPageButton.IsEnabled =
            _serviceRequestPage > 1;

        NextServiceRequestPageButton.IsEnabled =
            _serviceRequestPage < totalPages;
    }

    private void PreviousServiceRequestPageButton_Click(
        object sender, RoutedEventArgs e)
    {
        if (_serviceRequestPage <= 1) return;
        _serviceRequestPage--;
        UpdateServiceRequestPagination();
    }

    private void NextServiceRequestPageButton_Click(
        object sender, RoutedEventArgs e)
    {
        var totalPages =
            Math.Max(1, (int)Math.Ceiling(
                (double)_serviceRequests.Count / _pageSize));

        if (_serviceRequestPage >= totalPages) return;
        _serviceRequestPage++;
        UpdateServiceRequestPagination();
    }


    // ==========================================
    // DIAGNOSE
    // ==========================================

    private async void DiagnoseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not ServiceRequest request)
        {
            return;
        }

        var diagnosisBox = new TextBox
        {
            Header = "Diagnosis",
            PlaceholderText =
                "Enter the technician's findings...",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 120
        };

        var repairDescriptionBox = new TextBox
        {
            Header = "Repair Description",
            PlaceholderText =
                "Describe the repair that will be performed...",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 100
        };

        var content = new StackPanel
        {
            Spacing = 12
        };

        content.Children.Add(
            new TextBlock
            {
                Text = $"Service Request #{request.ServiceRequestId}",
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Customer: " +
                    $"{request.Device.Customer.FirstName} " +
                    $"{request.Device.Customer.LastName}"
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Device: " +
                    $"{request.Device.Brand} " +
                    $"{request.Device.Model}"
            });

        content.Children.Add(
            new TextBlock
            {
                Text =
                    $"Reported Problem: " +
                    $"{request.Description}",
                TextWrapping = TextWrapping.Wrap
            });

        content.Children.Add(
            new TextBlock
            {
                Text = $"Priority: {request.Priority}"
            });

        content.Children.Add(diagnosisBox);
        content.Children.Add(repairDescriptionBox);

        var dialog = new ContentDialog
        {
            Title = "Diagnose Service Request",
            Content = content,

            PrimaryButtonText = "Proceed with Repair",
            SecondaryButtonText = "Cancel Request",
            CloseButtonText = "Close",

            DefaultButton = ContentDialogButton.Primary,

            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.None)
            return;

        if (result == ContentDialogResult.Secondary)
        {
            await CancelServiceRequestAsync(
                request.ServiceRequestId);
            return;
        }

        if (string.IsNullOrWhiteSpace(diagnosisBox.Text))
        {
            await ShowMessageAsync(
                "Diagnosis Required",
                "Please enter a diagnosis before proceeding.");
            return;
        }

        if (string.IsNullOrWhiteSpace(repairDescriptionBox.Text))
        {
            await ShowMessageAsync(
                "Repair Description Required",
                "Please enter the repair description before proceeding.");
            return;
        }

        await CreateRepairAsync(
            request,
            diagnosisBox.Text.Trim(),
            repairDescriptionBox.Text.Trim());
    }

    private async Task CancelServiceRequestAsync(
        int serviceRequestId)
    {
        try
        {
            if (CurrentUser.CompanyId == null) return;

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var request =
                await db.ServiceRequests
                    .FirstOrDefaultAsync(x =>
                        x.ServiceRequestId == serviceRequestId);

            if (request == null)
            {
                await ShowMessageAsync(
                    "Not Found",
                    "The service request could not be found.");
                return;
            }

            request.Status = "Cancelled";
            await db.SaveChangesAsync();

            await LoadServiceRequestsAsync();

            await ShowMessageAsync(
                "Request Cancelled",
                "The service request has been cancelled.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Unable to Cancel Request",
                ex.Message);
        }
    }

    private async Task CreateRepairAsync(
        ServiceRequest request,
        string diagnosis,
        string repairDescription)
    {
        try
        {
            if (CurrentUser.CompanyId == null) return;

            if (string.IsNullOrWhiteSpace(CurrentUser.UserId))
            {
                await ShowMessageAsync(
                    "User Not Found",
                    "No logged-in user was found.");
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var existingRepair =
                await db.Repairs
                    .AnyAsync(x =>
                        x.ServiceRequestId ==
                        request.ServiceRequestId);

            if (existingRepair)
            {
                await ShowMessageAsync(
                    "Repair Already Exists",
                    "A repair already exists for this service request.");
                return;
            }

            var serviceRequest =
                await db.ServiceRequests
                    .FirstOrDefaultAsync(x =>
                        x.ServiceRequestId ==
                        request.ServiceRequestId);

            if (serviceRequest == null)
            {
                await ShowMessageAsync(
                    "Request Not Found",
                    "The service request could not be found.");
                return;
            }

            var masterUserId = CurrentUser.UserId;

            var technician =
                await db.Employees
                    .FirstOrDefaultAsync(x =>
                        x.MasterUserId == masterUserId &&
                        x.IsActive);

            if (technician == null)
            {
                await ShowMessageAsync(
                    "Employee Not Found",
                    "The logged-in user is not linked to an active employee record.");
                return;
            }

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

                if (serviceRequest.BranchId == null)
                {
                    await ShowMessageAsync(
                        "Branch Not Assigned",
                        "This service request is not assigned to a branch.");
                    return;
                }

                if (serviceRequest.BranchId != currentBranchId.Value)
                {
                    await ShowMessageAsync(
                        "Access Denied",
                        "This service request belongs to a different branch.");
                    return;
                }
            }

            var repair = new Repair
            {
                ServiceRequestId = request.ServiceRequestId,
                Diagnosis = diagnosis,
                RepairDescription = repairDescription,
                Status = "Pending",
                StartDate = null,
                EndDate = null,
                TechnicianId = technician.EmployeeId
            };

            db.Repairs.Add(repair);

            serviceRequest.Status = "In Repair";

            await db.SaveChangesAsync();

            await LoadServiceRequestsAsync();
            await LoadRepairsAsync();
            await LoadRepairHistoryAsync();

            await ShowMessageAsync(
                "Repair Created",
                $"The repair was created successfully and assigned to employee #{technician.EmployeeId}.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Unable to Create Repair",
                ex.Message);
        }
    }


    // ==========================================
    // LOAD ACTIVE REPAIRS
    // ==========================================

    private async Task LoadRepairsAsync()
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                _repairs = new();
                UpdateRepairPagination();
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            var query =
                db.Repairs
                    .AsNoTracking()
                    .Include(r => r.ServiceRequest)
                        .ThenInclude(sr => sr.Device)
                            .ThenInclude(d => d.Customer)
                    .Include(r => r.Technician)
                    .Where(r => r.Status != "Completed");

            if (branchManagementEnabled)
            {
                var currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    _repairs = new();
                    UpdateRepairPagination();
                    return;
                }

                query = query.Where(r =>
                    r.ServiceRequest.BranchId ==
                    currentBranchId.Value);
            }

            _repairs =
                await query
                    .OrderByDescending(r => r.RepairId)
                    .ToListAsync();

            _repairPage = 1;

            UpdateRepairPagination();
        }
        catch (Exception ex)
        {
            _repairs = new();
            UpdateRepairPagination();

            System.Diagnostics.Debug.WriteLine(
                "Failed to load repairs: " + ex);
        }
    }


    private void UpdateRepairPagination()
    {
        var totalPages =
            Math.Max(1, (int)Math.Ceiling(
                (double)_repairs.Count / _pageSize));

        if (_repairPage > totalPages) _repairPage = totalPages;
        if (_repairPage < 1) _repairPage = 1;

        var paged =
            _repairs
                .Skip((_repairPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        RepairList.ItemsSource = paged;

        RepairPageInfoText.Text =
            $"Page {_repairPage} of {totalPages}  •  " +
            $"{_repairs.Count} total";

        PreviousRepairPageButton.IsEnabled = _repairPage > 1;
        NextRepairPageButton.IsEnabled = _repairPage < totalPages;
    }

    private void PreviousRepairPageButton_Click(
        object sender, RoutedEventArgs e)
    {
        if (_repairPage <= 1) return;
        _repairPage--;
        UpdateRepairPagination();
    }

    private void NextRepairPageButton_Click(
        object sender, RoutedEventArgs e)
    {
        var totalPages =
            Math.Max(1, (int)Math.Ceiling(
                (double)_repairs.Count / _pageSize));

        if (_repairPage >= totalPages) return;
        _repairPage++;
        UpdateRepairPagination();
    }


    // ==========================================
    // LOAD REPAIR HISTORY
    // ==========================================

    private async Task LoadRepairHistoryAsync()
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                _repairHistory = new();
                UpdateHistoryPagination();
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            var query =
                db.Repairs
                    .AsNoTracking()
                    .Include(r => r.ServiceRequest)
                        .ThenInclude(sr => sr.Device)
                            .ThenInclude(d => d.Customer)
                    .Include(r => r.Technician)
                    .Where(r => r.Status == "Completed");

            if (branchManagementEnabled)
            {
                var currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    _repairHistory = new();
                    UpdateHistoryPagination();
                    return;
                }

                query = query.Where(r =>
                    r.ServiceRequest.BranchId ==
                    currentBranchId.Value);
            }

            _repairHistory =
                await query
                    .OrderByDescending(r => r.RepairId)
                    .ToListAsync();

            _historyPage = 1;

            UpdateHistoryPagination();
        }
        catch (Exception ex)
        {
            _repairHistory = new();
            UpdateHistoryPagination();

            System.Diagnostics.Debug.WriteLine(
                "Failed to load repair history: " + ex);
        }
    }


    private void UpdateHistoryPagination()
    {
        var totalPages =
            Math.Max(1, (int)Math.Ceiling(
                (double)_repairHistory.Count / _pageSize));

        if (_historyPage > totalPages) _historyPage = totalPages;
        if (_historyPage < 1) _historyPage = 1;

        var paged =
            _repairHistory
                .Skip((_historyPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        RepairHistoryList.ItemsSource = paged;

        HistoryPageInfoText.Text =
            $"Page {_historyPage} of {totalPages}  •  " +
            $"{_repairHistory.Count} total";

        PreviousHistoryPageButton.IsEnabled = _historyPage > 1;
        NextHistoryPageButton.IsEnabled = _historyPage < totalPages;
    }

    private void PreviousHistoryPageButton_Click(
        object sender, RoutedEventArgs e)
    {
        if (_historyPage <= 1) return;
        _historyPage--;
        UpdateHistoryPagination();
    }

    private void NextHistoryPageButton_Click(
        object sender, RoutedEventArgs e)
    {
        var totalPages =
            Math.Max(1, (int)Math.Ceiling(
                (double)_repairHistory.Count / _pageSize));

        if (_historyPage >= totalPages) return;
        _historyPage++;
        UpdateHistoryPagination();
    }


    // ==========================================
    // REPAIR WORKSPACE
    // ==========================================

    private async Task ShowRepairWorkspaceAsync(
        int repairId)
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
            // GET CURRENT EMPLOYEE
            // ==========================================

            var technician =
                await db.Employees
                    .FirstOrDefaultAsync(
                        x =>
                            x.MasterUserId ==
                            CurrentUser.UserId &&
                            x.IsActive);

            if (technician == null)
            {
                await ShowMessageAsync(
                    "Employee Not Found",
                    "The logged-in user is not linked to an active employee record.");

                return;
            }

            // ==========================================
            // CHECK BRANCH MANAGEMENT
            // ==========================================

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? currentBranchId = null;

            if (branchManagementEnabled)
            {
                currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }

            // ==========================================
            // GET REPAIR
            // ==========================================

            var repairQuery =
                db.Repairs
                    .Include(r => r.ServiceRequest)
                        .ThenInclude(sr => sr.Device)
                            .ThenInclude(d => d.Customer)
                    .Include(r => r.RepairItems)
                        .ThenInclude(ri => ri.Item)
                    .Where(r =>
                        r.RepairId == repairId);

            if (branchManagementEnabled)
            {
                repairQuery = repairQuery.Where(r =>
                    r.ServiceRequest.BranchId ==
                    currentBranchId!.Value);
            }

            var repair =
                await repairQuery.FirstOrDefaultAsync();

            if (repair == null)
            {
                await ShowMessageAsync(
                    "Repair Not Found",
                    "The repair could not be found.");

                return;
            }

            // ==========================================
            // REPAIR FIELDS
            // ==========================================

            var diagnosisBox =
                new TextBox
                {
                    Header = "Diagnosis",
                    Text = repair.Diagnosis ?? string.Empty,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 100
                };

            var repairDescriptionBox =
                new TextBox
                {
                    Header = "Repair Description",
                    Text =
                        repair.RepairDescription ??
                        string.Empty,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 100
                };

            // ==========================================
            // STATUS
            // ==========================================

            var statusBox =
                new ComboBox
                {
                    Header = "Status"
                };

            statusBox.Items.Add(
                new ComboBoxItem
                {
                    Content = "Pending"
                });

            statusBox.Items.Add(
                new ComboBoxItem
                {
                    Content = "In Repair"
                });

            statusBox.Items.Add(
                new ComboBoxItem
                {
                    Content = "Ready for Pickup"
                });

            statusBox.Items.Add(
                new ComboBoxItem
                {
                    Content = "Completed"
                });

            foreach (ComboBoxItem item
                in statusBox.Items)
            {
                if (item.Content?.ToString() ==
                    repair.Status)
                {
                    statusBox.SelectedItem = item;
                    break;
                }
            }

            if (statusBox.SelectedItem == null)
            {
                statusBox.SelectedIndex = 0;
            }

            // ==========================================
            // DATES
            // ==========================================

            var startDatePicker =
                new CalendarDatePicker
                {
                    Header = "Start Date",

                    Date =
                        repair.StartDate.HasValue
                            ? new DateTimeOffset(
                                repair.StartDate.Value)
                            : null
                };

            var endDatePicker =
                new CalendarDatePicker
                {
                    Header = "End Date",

                    Date =
                        repair.EndDate.HasValue
                            ? new DateTimeOffset(
                                repair.EndDate.Value)
                            : null
                };

            // ==========================================
            // REPAIR ITEMS TABLE
            // ==========================================

            var repairItemsList =
                new ListView
                {
                    Height = 220,
                    SelectionMode =
                        ListViewSelectionMode.None
                };

            var repairItemsHeader =
                new Grid
                {
                    Padding = new Thickness(8)
                };

            repairItemsHeader.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            3,
                            GridUnitType.Star)
                });

            repairItemsHeader.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            1,
                            GridUnitType.Star)
                });

            repairItemsHeader.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            1.5,
                            GridUnitType.Star)
                });

            repairItemsHeader.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            1.5,
                            GridUnitType.Star)
                });

            var headerItem =
                new TextBlock
                {
                    Text = "Item",
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };

            var headerQty =
                new TextBlock
                {
                    Text = "Qty",
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };

            var headerPrice =
                new TextBlock
                {
                    Text = "Unit Price",
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };

            var headerDiscount =
                new TextBlock
                {
                    Text = "Discount",
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };

            Grid.SetColumn(headerItem, 0);
            Grid.SetColumn(headerQty, 1);
            Grid.SetColumn(headerPrice, 2);
            Grid.SetColumn(headerDiscount, 3);

            repairItemsHeader.Children.Add(headerItem);
            repairItemsHeader.Children.Add(headerQty);
            repairItemsHeader.Children.Add(headerPrice);
            repairItemsHeader.Children.Add(headerDiscount);

            // ==========================================
            // FUNCTION TO ADD A ROW
            // ==========================================

            void AddRepairItemRow(
                RepairItem item)
            {
                var row =
                    new Grid
                    {
                        Padding =
                            new Thickness(8)
                    };

                row.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width =
                            new GridLength(
                                3,
                                GridUnitType.Star)
                    });

                row.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width =
                            new GridLength(
                                1,
                                GridUnitType.Star)
                    });

                row.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width =
                            new GridLength(
                                1.5,
                                GridUnitType.Star)
                    });

                row.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width =
                            new GridLength(
                                1.5,
                                GridUnitType.Star)
                    });

                var itemName =
                    new TextBlock
                    {
                        Text =
                            item.Item?.ItemName ??
                            $"Item #{item.ItemId}"
                    };

                var quantity =
                    new TextBlock
                    {
                        Text =
                            item.Quantity.ToString("0.##")
                    };

                var unitPrice =
                    new TextBlock
                    {
                        Text =
                            $"₱{item.UnitPrice:N2}"
                    };

                var discount =
                    new TextBlock
                    {
                        Text =
                            $"₱{item.Discount:N2}"
                    };

                Grid.SetColumn(itemName, 0);
                Grid.SetColumn(quantity, 1);
                Grid.SetColumn(unitPrice, 2);
                Grid.SetColumn(discount, 3);

                row.Children.Add(itemName);
                row.Children.Add(quantity);
                row.Children.Add(unitPrice);
                row.Children.Add(discount);

                repairItemsList.Items.Add(row);
            }

            // ==========================================
            // LOAD EXISTING ITEMS
            // ==========================================

            foreach (var item in repair.RepairItems)
            {
                AddRepairItemRow(item);
            }

            // ==========================================
            // ADD ITEM AREA
            // ==========================================

            var addItemPanel =
                new StackPanel
                {
                    Spacing = 10,
                    Visibility = Visibility.Collapsed
                };

            var inventoryComboBox =
                new ComboBox
                {
                    Header = "Inventory Item",
                    Width = 300
                };

            var quantityBox =
                new NumberBox
                {
                    Header = "Quantity",
                    Value = 1,
                    Minimum = 0.01,
                    SmallChange = 1,
                    Width = 300
                };

            var discountBox =
                new NumberBox
                {
                    Header = "Discount",
                    Value = 0,
                    Minimum = 0,
                    SmallChange = 10,
                    Width = 300
                };

            var availableStockText =
                new TextBlock
                {
                    Opacity = 0.7
                };

            var addItemMessage =
                new TextBlock
                {
                    Opacity = 0.8,
                    TextWrapping = TextWrapping.Wrap
                };

            // ==========================================
            // LOAD AVAILABLE INVENTORY
            // ONLY CURRENT BRANCH
            // ==========================================

            var inventoryQuery =
                db.Inventories
                    .Include(i => i.Item)
                    .Where(i =>
                        i.QuantityOnHand > 0 &&
                        i.Item != null &&
                        i.Item.IsActive);

            if (branchManagementEnabled)
            {
                inventoryQuery = inventoryQuery.Where(i =>
                    i.BranchId == currentBranchId!.Value);
            }

            var inventory =
                await inventoryQuery
                    .OrderBy(i => i.Item!.ItemName)
                    .ToListAsync();

            foreach (var inventoryRecord
                in inventory)
            {
                var comboItem =
                    new ComboBoxItem
                    {
                        Content =
                            $"{inventoryRecord.Item!.ItemName} " +
                            $"({inventoryRecord.QuantityOnHand} available)",

                        Tag =
                            inventoryRecord
                    };

                inventoryComboBox.Items.Add(
                    comboItem);
            }

            if (inventoryComboBox.Items.Count > 0)
            {
                inventoryComboBox.SelectedIndex = 0;
            }

            // ==========================================
            // SHOW AVAILABLE STOCK
            // ==========================================

            void UpdateAvailableStock()
            {
                if (inventoryComboBox.SelectedItem
                    is ComboBoxItem comboItem &&
                    comboItem.Tag is Inventory selectedInventory)
                {
                    availableStockText.Text =
                        $"Available stock: " +
                        $"{selectedInventory.QuantityOnHand}";
                }
                else
                {
                    availableStockText.Text =
                        "Available stock: 0";
                }
            }

            inventoryComboBox.SelectionChanged +=
                (_, _) =>
                {
                    UpdateAvailableStock();
                    addItemMessage.Text = string.Empty;
                };

            UpdateAvailableStock();

            // ==========================================
            // CONFIRM ADD ITEM
            // ==========================================

            var confirmAddItemButton =
                new Button
                {
                    Content = "Add Item"
                };

            confirmAddItemButton.Click +=
                async (_, _) =>
                {
                    addItemMessage.Text =
                        string.Empty;

                    if (inventoryComboBox.SelectedItem
                        is not ComboBoxItem comboItem ||
                        comboItem.Tag is not Inventory selectedInventory)
                    {
                        addItemMessage.Text =
                            "Please select an inventory item.";

                        return;
                    }

                    // Extra branch safety check
                    if (branchManagementEnabled &&
                        selectedInventory.BranchId != currentBranchId)
                    {
                        addItemMessage.Text =
                            "This inventory item does not belong to the selected branch.";

                        return;
                    }

                    if (double.IsNaN(
                            quantityBox.Value) ||
                        quantityBox.Value <= 0)
                    {
                        addItemMessage.Text =
                            "Quantity must be greater than zero.";

                        return;
                    }

                    if (double.IsNaN(
                            discountBox.Value) ||
                        discountBox.Value < 0)
                    {
                        addItemMessage.Text =
                            "Discount cannot be negative.";

                        return;
                    }

                    var quantity =
                        (decimal)quantityBox.Value;

                    var discount =
                        (decimal)discountBox.Value;

                    if (quantity >
                        selectedInventory.QuantityOnHand)
                    {
                        addItemMessage.Text =
                            $"Insufficient stock. " +
                            $"Only {selectedInventory.QuantityOnHand} " +
                            $"unit(s) available.";

                        return;
                    }

                    // ==================================
                    // CREATE REPAIR ITEM
                    // ==================================

                    var repairItem =
                        new RepairItem
                        {
                            RepairId =
                                repair.RepairId,

                            ItemId =
                                selectedInventory.ItemId,

                            Quantity =
                                quantity,

                            UnitPrice =
                                selectedInventory.Item!.UnitPrice,

                            Discount =
                                discount
                        };

                    db.RepairItems.Add(
                        repairItem);

                    // ==================================
                    // REDUCE STOCK
                    // ==================================

                    selectedInventory.QuantityOnHand -=
                        quantity;

                    await db.SaveChangesAsync();

                    // ==================================
                    // UPDATE UI
                    // ==================================

                    repairItem.Item =
                        selectedInventory.Item;

                    AddRepairItemRow(
                        repairItem);

                    inventoryComboBox.Items.Clear();

                    // ==================================
                    // RELOAD CURRENT BRANCH INVENTORY
                    // ==================================

                    var updatedInventoryQuery =
                        db.Inventories
                            .Include(i => i.Item)
                            .Where(i =>
                                i.QuantityOnHand > 0 &&
                                i.Item != null &&
                                i.Item.IsActive);

                    if (branchManagementEnabled)
                    {
                        updatedInventoryQuery =
                            updatedInventoryQuery.Where(i =>
                                i.BranchId ==
                                currentBranchId!.Value);
                    }

                    var updatedInventory =
                        await updatedInventoryQuery
                            .OrderBy(
                                i => i.Item!.ItemName)
                            .ToListAsync();

                    foreach (var inventoryRecord
                        in updatedInventory)
                    {
                        var newComboItem =
                            new ComboBoxItem
                            {
                                Content =
                                    $"{inventoryRecord.Item!.ItemName} " +
                                    $"({inventoryRecord.QuantityOnHand} available)",

                                Tag =
                                    inventoryRecord
                            };

                        inventoryComboBox.Items.Add(
                            newComboItem);
                    }

                    if (inventoryComboBox.Items.Count > 0)
                    {
                        inventoryComboBox.SelectedIndex =
                            0;
                    }

                    quantityBox.Value = 1;
                    discountBox.Value = 0;

                    addItemMessage.Text =
                        $"{selectedInventory.Item!.ItemName} " +
                        $"was added successfully.";
                };

            // ==========================================
            // CANCEL ADD ITEM
            // ==========================================

            var cancelAddItemButton =
                new Button
                {
                    Content = "Cancel"
                };

            cancelAddItemButton.Click +=
                (_, _) =>
                {
                    addItemPanel.Visibility =
                        Visibility.Collapsed;

                    addItemMessage.Text =
                        string.Empty;
                };

            // ==========================================
            // ADD ITEM PANEL CONTENT
            // ==========================================

            addItemPanel.Children.Add(
                inventoryComboBox);

            addItemPanel.Children.Add(
                availableStockText);

            addItemPanel.Children.Add(
                quantityBox);

            addItemPanel.Children.Add(
                discountBox);

            addItemPanel.Children.Add(
                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,

                    Spacing = 8,

                    Children =
                    {
                    confirmAddItemButton,
                    cancelAddItemButton
                    }
                });

            addItemPanel.Children.Add(
                addItemMessage);

            // ==========================================
            // ADD ITEM BUTTON
            // ==========================================

            var addItemButton =
                new Button
                {
                    Content = "Add Item"
                };

            addItemButton.Click +=
                (_, _) =>
                {
                    if (inventory.Count == 0)
                    {
                        addItemMessage.Text =
                            "No inventory items are currently available.";

                        addItemPanel.Visibility =
                            Visibility.Visible;

                        return;
                    }

                    addItemPanel.Visibility =
                        addItemPanel.Visibility ==
                        Visibility.Visible
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                };

            // ==========================================
            // REPAIR ITEMS PANEL
            // ==========================================

            var repairItemsPanel =
                new StackPanel
                {
                    Spacing = 4
                };

            repairItemsPanel.Children.Add(
                repairItemsHeader);

            repairItemsPanel.Children.Add(
                repairItemsList);

            repairItemsPanel.Children.Add(
                addItemButton);

            repairItemsPanel.Children.Add(
                addItemPanel);

            // ==========================================
            // MAIN CONTENT
            // ==========================================

            var contentPanel =
                new StackPanel
                {
                    Spacing = 12
                };

            contentPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        $"Repair #{repair.RepairId}",

                    FontSize = 22,

                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                });

            contentPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        $"Service Request: " +
                        $"{repair.ServiceRequestId}"
                });

            contentPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        $"Customer: " +
                        $"{repair.ServiceRequest.Device.Customer.FirstName} " +
                        $"{repair.ServiceRequest.Device.Customer.LastName}"
                });

            contentPanel.Children.Add(
                new TextBlock
                {
                    Text =
                        $"Device: " +
                        $"{repair.ServiceRequest.Device.Brand} " +
                        $"{repair.ServiceRequest.Device.Model}"
                });

            contentPanel.Children.Add(
                diagnosisBox);

            contentPanel.Children.Add(
                repairDescriptionBox);

            contentPanel.Children.Add(
                statusBox);

            contentPanel.Children.Add(
                startDatePicker);

            contentPanel.Children.Add(
                endDatePicker);

            contentPanel.Children.Add(
                new TextBlock
                {
                    Text = "Repair Items",

                    FontSize = 18,

                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold,

                    Margin =
                        new Thickness(
                            0,
                            8,
                            0,
                            0)
                });

            contentPanel.Children.Add(
                repairItemsPanel);

            // ==========================================
            // SCROLL VIEWER
            // ==========================================

            var scrollViewer =
                new ScrollViewer
                {
                    Content = contentPanel,

                    MaxHeight = 750,
                    MinWidth = 700,

                    VerticalScrollBarVisibility =
                        ScrollBarVisibility.Auto,

                    HorizontalScrollBarVisibility =
                        ScrollBarVisibility.Disabled
                };

            // ==========================================
            // DIALOG
            // ==========================================

            var dialog =
                new ContentDialog
                {
                    Title =
                        "Repair Workspace",

                    Content =
                        scrollViewer,

                    PrimaryButtonText =
                        "Save Changes",

                    SecondaryButtonText =
                        "Complete Repair",

                    CloseButtonText =
                        "Close",

                    DefaultButton =
                        ContentDialogButton.Primary,

                    XamlRoot =
                        XamlRoot
                };

            var result =
                await dialog.ShowAsync();

            // ==========================================
            // COMPLETE REPAIR
            // ==========================================

            if (result == ContentDialogResult.Secondary)
            {
                repair.Diagnosis =
                    string.IsNullOrWhiteSpace(
                        diagnosisBox.Text)
                        ? null
                        : diagnosisBox.Text.Trim();

                repair.RepairDescription =
                    string.IsNullOrWhiteSpace(
                        repairDescriptionBox.Text)
                        ? null
                        : repairDescriptionBox.Text.Trim();

                repair.Status =
                    "Completed";

                repair.EndDate =
                    endDatePicker.Date?.DateTime
                    ?? DateTime.UtcNow;

                // ==========================================
                // COMPLETE SERVICE REQUEST
                // ==========================================

                repair.ServiceRequest.Status =
                    "Completed";

                await db.SaveChangesAsync();

                await LoadServiceRequestsAsync();
                await LoadRepairsAsync();
                await LoadRepairHistoryAsync();

                await ShowMessageAsync(
                    "Repair Completed",
                    "The repair has been marked as completed.");

                return;
            }

            // ==========================================
            // CLOSE DIALOG
            // ==========================================

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            // ==========================================
            // SAVE REPAIR
            // ==========================================

            repair.Diagnosis =
                string.IsNullOrWhiteSpace(
                    diagnosisBox.Text)
                    ? null
                    : diagnosisBox.Text.Trim();

            repair.RepairDescription =
                string.IsNullOrWhiteSpace(
                    repairDescriptionBox.Text)
                    ? null
                    : repairDescriptionBox.Text.Trim();

            repair.Status =
                (statusBox.SelectedItem
                    as ComboBoxItem)?
                    .Content?
                    .ToString()
                    ?? "Pending";

            repair.StartDate =
                startDatePicker.Date?.DateTime;

            repair.EndDate =
                endDatePicker.Date?.DateTime;

            await db.SaveChangesAsync();

            await LoadRepairsAsync();
            await LoadRepairHistoryAsync();

            System.Diagnostics.Debug.WriteLine(
                "Repair updated successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Unable to open repair: " + ex);
        }
    }


    // ==========================================
    // OPEN REPAIR
    // ==========================================

    private async void OpenRepairButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.DataContext is not Repair repair)
        {
            return;
        }

        await ShowRepairWorkspaceAsync(
            repair.RepairId);
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