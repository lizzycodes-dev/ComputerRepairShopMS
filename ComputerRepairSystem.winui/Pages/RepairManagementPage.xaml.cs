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

    public RepairManagementPage(
        TenantDbContextFactory tenantDbFactory)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;

        Loaded += RepairManagementPage_Loaded;
    }


    private async void RepairManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadServiceRequestsAsync();
        await LoadRepairsAsync();
    }


    private async Task LoadServiceRequestsAsync()
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

            var requests =
                await db.ServiceRequests
                    .AsNoTracking()
                    .Include(x => x.Device)
                        .ThenInclude(x => x.Customer)
                    .Where(x =>
                        x.Status != "In Repair" &&
                        x.Status != "Completed" &&
                        x.Status != "Cancelled")
                    .OrderByDescending(x => x.RequestDate)
                    .ToListAsync();

            ServiceRequestList.ItemsSource =
                requests;
        }
        catch (Exception ex)
        {
            ServiceRequestList.ItemsSource = null;

            // We don't have a status bar on this page yet.
            System.Diagnostics.Debug.WriteLine(
                "Failed to load service requests: "
                + ex);
        }
    }


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
        {
            return;
        }

        // Customer does not approve the repair.
        if (result == ContentDialogResult.Secondary)
        {
            await CancelServiceRequestAsync(
                request.ServiceRequestId);

            return;
        }

        // Customer approves the repair.
        if (string.IsNullOrWhiteSpace(
                diagnosisBox.Text))
        {
            await ShowMessageAsync(
                "Diagnosis Required",
                "Please enter a diagnosis before proceeding.");

            return;
        }

        if (string.IsNullOrWhiteSpace(
                repairDescriptionBox.Text))
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
            if (CurrentUser.CompanyId == null)
            {
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var request =
                await db.ServiceRequests
                    .FirstOrDefaultAsync(
                        x =>
                            x.ServiceRequestId ==
                            serviceRequestId);

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
            if (CurrentUser.CompanyId == null)
            {
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var existingRepair =
                await db.Repairs
                    .AnyAsync(
                        x =>
                            x.ServiceRequestId ==
                            request.ServiceRequestId);

            if (existingRepair)
            {
                await ShowMessageAsync(
                    "Repair Already Exists",
                    "A repair already exists for this service request.");

                return;
            }

            // Get the actual ServiceRequest tracked by this DbContext
            var serviceRequest =
                await db.ServiceRequests
                    .FirstOrDefaultAsync(
                        x =>
                            x.ServiceRequestId ==
                            request.ServiceRequestId);

            if (serviceRequest == null)
            {
                await ShowMessageAsync(
                    "Request Not Found",
                    "The service request could not be found.");

                return;
            }

            var repair = new Repair
            {
                ServiceRequestId =
                    request.ServiceRequestId,

                Diagnosis =
                    diagnosis,

                RepairDescription =
                    repairDescription,

                Status =
                    "Pending",

                StartDate = null,
                EndDate = null,

                TechnicianId = null,
             
            };

            db.Repairs.Add(repair);

            // Update the tracked ServiceRequest
            serviceRequest.Status = "In Repair";

            await db.SaveChangesAsync();

            await LoadServiceRequestsAsync();
            await LoadRepairsAsync();

            await ShowMessageAsync(
                "Repair Created",
                "The repair was created successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Unable to Create Repair",
                ex.Message);
        }
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
    private async Task LoadRepairsAsync()
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

            var repairs =
                await db.Repairs
                    .AsNoTracking()
                    .Include(r => r.ServiceRequest)
                        .ThenInclude(sr => sr.Device)
                            .ThenInclude(d => d.Customer)
                    .Where(r => r.Status != "Completed")
                    .OrderByDescending(
                        r => r.RepairId)
                    .ToListAsync();

            RepairList.ItemsSource = repairs;

            System.Diagnostics.Debug.WriteLine(
                $"Loaded {repairs.Count} repair(s).");
        }
        catch (Exception ex)
        {
            RepairList.ItemsSource = null;

            System.Diagnostics.Debug.WriteLine(
                "Failed to load repairs: " + ex);
        }
    }

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

            var repair =
                await db.Repairs
                    .Include(r => r.ServiceRequest)
                        .ThenInclude(sr => sr.Device)
                            .ThenInclude(d => d.Customer)
                    .Include(r => r.RepairItems)
                        .ThenInclude(ri => ri.Item)
                    .FirstOrDefaultAsync(
                        r => r.RepairId == repairId);

            if (repair == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Repair not found.");

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
                    Height = 220
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
            // ==========================================

            var inventory =
                await db.Inventories
                    .Include(i => i.Item)
                    .Where(i =>
                        i.QuantityOnHand > 0 &&
                        i.Item != null &&
                        i.Item.IsActive)
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


                    var updatedInventory =
                        await db.Inventories
                            .Include(i => i.Item)
                            .Where(i =>
                                i.QuantityOnHand > 0 &&
                                i.Item != null &&
                                i.Item.IsActive)
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

                await db.SaveChangesAsync();

                await LoadRepairsAsync();

                await ShowMessageAsync(
                    "Repair Completed",
                    "The repair has been marked as completed.");

                return;
            }

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


            System.Diagnostics.Debug.WriteLine(
                "Repair updated successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Unable to open repair: " + ex);
        }
    }
    private async Task AddRepairItemAsync(
        int repairId,
        ListView repairItemsList)
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

            // Load inventory with available stock
            var inventory =
                await db.Inventories
                    .AsNoTracking()
                    .Include(x => x.Item)
                    .Where(x => x.QuantityOnHand > 0)
                    .OrderBy(x => x.Item!.ItemName)
                    .ToListAsync();

            if (inventory.Count == 0)
            {
                await ShowMessageAsync(
                    "No Items Available",
                    "There are currently no inventory items in stock.");

                return;
            }

            // Item selection
            var itemBox = new ComboBox
            {
                Header = "Inventory Item",
                ItemsSource = inventory,
                DisplayMemberPath = "Item.ItemName",
                SelectedIndex = 0
            };

            // Quantity
            var quantityBox = new NumberBox
            {
                Header = "Quantity",
                Value = 1,
                Minimum = 0.01,
                SmallChange = 1
            };

            // Discount
            var discountBox = new NumberBox
            {
                Header = "Discount",
                Value = 0,
                Minimum = 0,
                SmallChange = 10
            };

            // Available stock label
            var stockText =
                new TextBlock
                {
                    Opacity = 0.7
                };

            void UpdateStockText()
            {
                if (itemBox.SelectedItem is Inventory selectedInventory)
                {
                    stockText.Text =
                        $"Available stock: " +
                        $"{selectedInventory.QuantityOnHand}";
                }
                else
                {
                    stockText.Text =
                        "Available stock: 0";
                }
            }

            itemBox.SelectionChanged += (_, _) =>
            {
                UpdateStockText();
            };

            UpdateStockText();

            var content =
                new StackPanel
                {
                    Spacing = 12
                };

            content.Children.Add(itemBox);
            content.Children.Add(stockText);
            content.Children.Add(quantityBox);
            content.Children.Add(discountBox);

            var dialog =
                new ContentDialog
                {
                    Title = "Add Repair Item",
                    Content = content,

                    PrimaryButtonText = "Add",
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

            if (itemBox.SelectedItem is not Inventory inventoryRecord)
            {
                await ShowMessageAsync(
                    "Invalid Item",
                    "Please select an inventory item.");

                return;
            }

            if (double.IsNaN(quantityBox.Value) ||
                quantityBox.Value <= 0)
            {
                await ShowMessageAsync(
                    "Invalid Quantity",
                    "Quantity must be greater than zero.");

                return;
            }

            if (double.IsNaN(discountBox.Value) ||
                discountBox.Value < 0)
            {
                await ShowMessageAsync(
                    "Invalid Discount",
                    "Discount cannot be negative.");

                return;
            }

            var quantity =
                (decimal)quantityBox.Value;

            var discount =
                (decimal)discountBox.Value;

            // Check stock
            if (quantity >
                inventoryRecord.QuantityOnHand)
            {
                await ShowMessageAsync(
                    "Insufficient Stock",
                    $"Only {inventoryRecord.QuantityOnHand} " +
                    $"unit(s) are available.");

                return;
            }

            // Create RepairItem
            var repairItem =
                new RepairItem
                {
                    RepairId =
                        repairId,

                    ItemId =
                        inventoryRecord.ItemId,

                    Quantity =
                        quantity,

                    UnitPrice =
                        inventoryRecord.Item!.UnitPrice,

                    Discount =
                        discount
                };

            db.RepairItems.Add(
                repairItem);

            // Reduce inventory stock
            inventoryRecord.QuantityOnHand -=
                quantity;

            // We are tracking the inventory record
            // separately, so attach/update it.
            db.Inventories.Update(
                inventoryRecord);

            await db.SaveChangesAsync();

            // Reload repair items
            var updatedItems =
                await db.RepairItems
                    .AsNoTracking()
                    .Include(ri => ri.Item)
                    .Where(ri =>
                        ri.RepairId == repairId)
                    .Select(ri => new
                    {
                        ri.RepairItemId,

                        ItemName =
                            ri.Item!.ItemName,

                        ri.Quantity,

                        ri.UnitPrice,

                        ri.Discount
                    })
                    .ToListAsync();

            repairItemsList.ItemsSource =
                updatedItems;

            await ShowMessageAsync(
                "Item Added",
                $"{inventoryRecord.Item!.ItemName} " +
                $"was added to the repair.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Unable to Add Item",
                ex.Message);
        }
    }
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

}