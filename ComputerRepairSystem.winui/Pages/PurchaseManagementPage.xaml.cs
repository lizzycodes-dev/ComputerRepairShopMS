using ComputerRepairSystem.company.Context;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class PurchaseManagementPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly MasterErpDbContext _masterDb;
    private readonly CurrentBranchContext _currentBranchContext;

    // ==========================================
    // PAGINATION STATE
    // ==========================================

    private List<PurchaseDisplayItem> _allPurchases = new();
    private int _currentPage = 1;
    private const int _pageSize = 10;


    public PurchaseManagementPage(
        TenantDbContextFactory tenantDbFactory,
        MasterErpDbContext masterDb,
        CurrentBranchContext currentBranchContext)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;
        _masterDb = masterDb;
        _currentBranchContext = currentBranchContext;

        Loaded += PurchaseManagementPage_Loaded;
        Unloaded += PurchaseManagementPage_Unloaded;
    }


    // ==========================================
    // BRANCH MANAGEMENT
    // ==========================================

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


    // ==========================================
    // PAGE LOADED / UNLOADED
    // ==========================================

    private async void PurchaseManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged +=
            OnGlobalBranchChanged;

        await LoadPurchasesAsync();
    }


    private void PurchaseManagementPage_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged -=
            OnGlobalBranchChanged;
    }


    private async void OnGlobalBranchChanged()
    {
        if (!IsLoaded)
            return;

        await LoadPurchasesAsync();
    }


    // ==========================================
    // LOAD PURCHASES
    // ==========================================

    private async Task LoadPurchasesAsync()
    {
        try
        {
            if (CurrentUser.CompanyId == null)
                return;

            await using var db =
                await _tenantDbFactory.CreateAsync(CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? currentBranchId = null;

            if (branchManagementEnabled)
            {
                currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    _allPurchases = new();
                    UpdatePagination();

                    PurchaseItemsListView.ItemsSource = null;

                    PurchaseNumberText.Text = "—";
                    SupplierText.Text = "—";
                    PurchaseDateText.Text = "—";
                    PurchaseStatusText.Text = "—";
                    SelectedPurchaseTotalText.Text = "₱0.00";

                    EmptyStateText.Visibility =
                        Visibility.Visible;

                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }

            var query =
                db.PurchaseOrders
                    .AsNoTracking()
                    .Include(x => x.Supplier)
                    .AsQueryable();

            if (branchManagementEnabled)
            {
                query = query.Where(x =>
                    x.BranchId == currentBranchId!.Value);
            }

            _allPurchases =
                await query
                    .OrderByDescending(x => x.PurchaseOrderId)
                    .Select(x => new PurchaseDisplayItem
                    {
                        PurchaseOrderId =
                            x.PurchaseOrderId,

                        PurchaseOrderNumber =
                            x.PurchaseOrderNumber,

                        SupplierName =
                            x.Supplier != null
                                ? x.Supplier.SupplierName
                                : "No Supplier",

                        PurchaseDateText =
                            x.PurchaseDate.ToLocalTime()
                                .ToString("MMM dd, yyyy"),

                        Status =
                            x.Status,

                        TotalAmount =
                            x.TotalAmount,

                        TotalAmountText =
                            $"₱{x.TotalAmount:N2}"
                    })
                    .ToListAsync();

            _currentPage = 1;

            UpdatePagination();

            EmptyStateText.Visibility =
                _allPurchases.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (_allPurchases.Count == 0)
            {
                PurchaseItemsListView.ItemsSource = null;

                PurchaseNumberText.Text = "—";
                SupplierText.Text = "—";
                PurchaseDateText.Text = "—";
                PurchaseStatusText.Text = "—";
                SelectedPurchaseTotalText.Text = "₱0.00";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Failed to load purchases: " + ex);
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
                    (double)_allPurchases.Count /
                    _pageSize));

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        if (_currentPage < 1)
            _currentPage = 1;

        var pagedPurchases =
            _allPurchases
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        PurchaseListView.ItemsSource = pagedPurchases;

        PageInfoText.Text =
            $"Page {_currentPage} of {totalPages}  •  " +
            $"{_allPurchases.Count} total";

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
                    (double)_allPurchases.Count /
                    _pageSize));

        if (_currentPage >= totalPages)
            return;

        _currentPage++;

        UpdatePagination();
    }


    // ==========================================
    // VIEW PURCHASE
    // ==========================================

    private async void ViewPurchaseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not PurchaseDisplayItem purchase)
        {
            return;
        }

        await LoadPurchaseDetailsAsync(
            purchase.PurchaseOrderId);

        // Switch to Purchase Details tab
        PurchaseTabView.SelectedIndex = 1;
    }


    // ==========================================
    // EDIT PURCHASE
    // ==========================================

    private async void EditPurchaseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not PurchaseDisplayItem purchase)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
            return;

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

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

            var order =
                await db.PurchaseOrders
                    .Include(x => x.Supplier)
                    .FirstOrDefaultAsync(x =>
                        x.PurchaseOrderId ==
                        purchase.PurchaseOrderId);

            if (order == null)
            {
                await ShowMessageAsync(
                    "Not Found",
                    "The purchase could not be found.");

                return;
            }

            // ==========================================
            // BRANCH SAFETY CHECK
            // ==========================================

            if (branchManagementEnabled &&
                order.BranchId != currentBranchId)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "This purchase belongs to a different branch.");

                return;
            }

            // ==========================================
            // NOTES
            // ==========================================

            var notesBox =
                new TextBox
                {
                    Header = "Notes",
                    Text = order.Notes ?? string.Empty,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 100,
                    Width = 400
                };

            // ==========================================
            // STATUS
            // ==========================================

            var statusBox =
                new ComboBox
                {
                    Header = "Status",
                    Width = 400
                };

            statusBox.Items.Add("Received");
            statusBox.Items.Add("Pending");
            statusBox.Items.Add("Cancelled");

            for (int i = 0; i < statusBox.Items.Count; i++)
            {
                if (statusBox.Items[i]?.ToString() ==
                    order.Status)
                {
                    statusBox.SelectedIndex = i;
                    break;
                }
            }

            if (statusBox.SelectedIndex < 0)
            {
                statusBox.SelectedIndex = 0;
            }

            // ==========================================
            // PURCHASE DATE
            // ==========================================

            var datePicker =
                new DatePicker
                {
                    Header = "Purchase Date",
                    Date = new DateTimeOffset(
                        order.PurchaseDate.ToLocalTime()),
                    Width = 400
                };

            // ==========================================
            // PANEL
            // ==========================================

            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(
                new TextBlock
                {
                    Text = $"Purchase: {order.PurchaseOrderNumber}",
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold,
                    Opacity = 0.8
                });

            panel.Children.Add(statusBox);
            panel.Children.Add(datePicker);
            panel.Children.Add(notesBox);

            // ==========================================
            // DIALOG
            // ==========================================

            var dialog =
                new ContentDialog
                {
                    Title = "Edit Purchase",
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
                return;

            // ==========================================
            // VALIDATION
            // ==========================================

            if (statusBox.SelectedItem
                is not string newStatus)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a status.");

                return;
            }

            if (datePicker.Date == null)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a purchase date.");

                return;
            }

            // ==========================================
            // APPLY CHANGES
            //
            // NOTE: Changing status from/to "Received"
            // requires inventory adjustment. To keep this
            // edit simple and safe, status changes
            // involving "Received" are rejected. Only
            // Pending <-> Cancelled and "Received" ->
            // "Received" (no change) are allowed.
            // ==========================================

            bool statusWasReceived =
                order.Status == "Received";

            bool statusIsReceived =
                newStatus == "Received";

            if (statusWasReceived != statusIsReceived)
            {
                await ShowMessageAsync(
                    "Status Change Not Allowed",
                    "Changing the status to or from \"Received\" is not allowed here because it would affect inventory. Cancel the purchase and record a new one if needed.");

                return;
            }

            order.Status = newStatus;

            order.PurchaseDate =
                datePicker.Date
                    .DateTime
                    .ToUniversalTime();

            order.Notes =
                string.IsNullOrWhiteSpace(notesBox.Text)
                    ? null
                    : notesBox.Text.Trim();

            await db.SaveChangesAsync();

            await LoadPurchasesAsync();

            await ShowMessageAsync(
                "Purchase Updated",
                "The purchase was updated successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Failed to edit purchase: " + ex);

            await ShowMessageAsync(
                "Purchase Error",
                ex.Message);
        }
    }


    // ==========================================
    // DELETE PURCHASE
    // ==========================================

    private async void DeletePurchaseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not PurchaseDisplayItem purchase)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
            return;

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

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

            var order =
                await db.PurchaseOrders
                    .FirstOrDefaultAsync(x =>
                        x.PurchaseOrderId ==
                        purchase.PurchaseOrderId);

            if (order == null)
            {
                await ShowMessageAsync(
                    "Not Found",
                    "The purchase could not be found.");

                return;
            }

            // ==========================================
            // BRANCH SAFETY CHECK
            // ==========================================

            if (branchManagementEnabled &&
                order.BranchId != currentBranchId)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "This purchase belongs to a different branch.");

                return;
            }

            // ==========================================
            // CONFIRM
            // ==========================================

            var confirm =
                new ContentDialog
                {
                    Title = "Delete Purchase",
                    Content =
                        $"Delete purchase '{order.PurchaseOrderNumber}'?\n\n" +
                        "This will remove the purchase and all its items. If the purchase was Received, its quantities will be subtracted from inventory.",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton =
                        ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };

            var result =
                await confirm.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            // ==========================================
            // REVERSE INVENTORY IF RECEIVED
            // ==========================================

            if (order.Status == "Received")
            {
                var items =
                    await db.PurchaseOrderItems
                        .Where(x =>
                            x.PurchaseOrderId ==
                            order.PurchaseOrderId)
                        .ToListAsync();

                foreach (var item in items)
                {
                    var inventory =
                        await db.Inventories
                            .FirstOrDefaultAsync(x =>
                                x.ItemId == item.ItemId
                                &&
                                x.BranchId == order.BranchId);

                    if (inventory != null)
                    {
                        inventory.QuantityOnHand -=
                            item.Quantity;

                        if (inventory.QuantityOnHand < 0)
                            inventory.QuantityOnHand = 0;
                    }
                }
            }

            // ==========================================
            // DELETE ITEMS + ORDER
            // ==========================================

            var orderItems =
                await db.PurchaseOrderItems
                    .Where(x =>
                        x.PurchaseOrderId ==
                        order.PurchaseOrderId)
                    .ToListAsync();

            db.PurchaseOrderItems.RemoveRange(
                orderItems);

            db.PurchaseOrders.Remove(order);

            await db.SaveChangesAsync();

            await LoadPurchasesAsync();

            await ShowMessageAsync(
                "Purchase Deleted",
                "The purchase was deleted successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Failed to delete purchase: " + ex);

            await ShowMessageAsync(
                "Purchase Error",
                ex.Message);
        }
    }


    // ==========================================
    // LOAD PURCHASE DETAILS
    // ==========================================

    private async Task LoadPurchaseDetailsAsync(
        int purchaseOrderId)
    {
        try
        {
            if (CurrentUser.CompanyId == null)
                return;

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

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

            var purchase =
                await db.PurchaseOrders
                    .AsNoTracking()
                    .Include(x => x.Supplier)
                    .Include(x => x.Items)
                        .ThenInclude(x => x.Item)
                    .FirstOrDefaultAsync(
                        x => x.PurchaseOrderId == purchaseOrderId);

            if (purchase == null)
                return;

            // ==========================================
            // BRANCH SAFETY CHECK
            // ==========================================

            if (branchManagementEnabled &&
                purchase.BranchId != currentBranchId)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "This purchase belongs to a different branch.");

                return;
            }

            PurchaseNumberText.Text =
                purchase.PurchaseOrderNumber;

            SupplierText.Text =
                purchase.Supplier?.SupplierName
                ?? "No Supplier";

            PurchaseDateText.Text =
                purchase.PurchaseDate
                    .ToLocalTime()
                    .ToString("MMM dd, yyyy");

            PurchaseStatusText.Text =
                purchase.Status;

            SelectedPurchaseTotalText.Text =
                $"₱{purchase.TotalAmount:N2}";


            var items =
                purchase.Items
                    .Select(x => new PurchaseItemDisplay
                    {
                        ItemName =
                            x.Item?.ItemName
                            ?? $"Item #{x.ItemId}",

                        QuantityText =
                            x.Quantity.ToString("0.##"),

                        UnitCostText =
                            $"₱{x.UnitCost:N2}",

                        SubtotalText =
                            $"₱{(x.Quantity * x.UnitCost):N2}"
                    })
                    .ToList();

            PurchaseItemsListView.ItemsSource = items;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Failed to load purchase details: " + ex);
        }
    }


    private async Task CreateNewPurchaseAsync()
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                await ShowMessageAsync(
                    "Company Not Found",
                    "Unable to determine the current company.");

                return;
            }


            await using var db =
                await _tenantDbFactory.CreateAsync(CurrentUser.CompanyId.Value);


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
            // LOAD SUPPLIERS
            // ==========================================

            var suppliers =
                await db.Suppliers
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SupplierName)
                    .ToListAsync();


            if (suppliers.Count == 0)
            {
                await ShowMessageAsync(
                    "No Suppliers",
                    "Please add a supplier before recording a purchase.");

                return;
            }


            // ==========================================
            // LOAD BRANCHES (only when branch module is on)
            // ==========================================

            var branches =
                branchManagementEnabled
                    ? await db.Branches
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.BranchName)
                        .ToListAsync()
                    : new List<Branch>();


            // ==========================================
            // LOAD INVENTORY ITEMS
            // ==========================================

            var inventoryItems =
                await db.InventoryItems
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.ItemName)
                    .ToListAsync();


            if (inventoryItems.Count == 0)
            {
                await ShowMessageAsync(
                    "No Inventory Items",
                    "Please add an inventory item before recording a purchase.");

                return;
            }


            // ==========================================
            // SUPPLIER
            // ==========================================

            var supplierBox =
                new ComboBox
                {
                    Header = "Supplier",
                    ItemsSource = suppliers,
                    DisplayMemberPath = "SupplierName",
                    SelectedIndex = 0,
                    Width = 350
                };


            // ==========================================
            // BRANCH (only when branch module is on)
            // ==========================================

            ComboBox? branchBox = null;

            if (branchManagementEnabled)
            {
                branchBox =
                    new ComboBox
                    {
                        Header = "Branch",
                        ItemsSource = branches,
                        DisplayMemberPath = "BranchName",
                        Width = 350
                    };

                // ======================================
                // DEFAULT TO GLOBAL SELECTED BRANCH
                // ======================================

                var globalBranchId =
                    _currentBranchContext.BranchId;

                if (globalBranchId.HasValue)
                {
                    var index =
                        branches.FindIndex(x =>
                            x.BranchId ==
                            globalBranchId.Value);

                    branchBox.SelectedIndex =
                        index >= 0 ? index : 0;
                }
                else if (branches.Count > 0)
                {
                    branchBox.SelectedIndex = 0;
                }
            }


            // ==========================================
            // PURCHASE DATE
            // ==========================================

            var purchaseDatePicker =
                new DatePicker
                {
                    Header = "Purchase Date",
                    Date = DateTimeOffset.Now,
                    Width = 350
                };


            // ==========================================
            // STATUS
            // ==========================================

            var statusBox =
                new ComboBox
                {
                    Header = "Status",
                    Width = 350
                };

            statusBox.Items.Add("Received");
            statusBox.Items.Add("Pending");
            statusBox.Items.Add("Cancelled");

            statusBox.SelectedIndex = 0;


            // ==========================================
            // ITEM
            // ==========================================

            var itemBox =
                new ComboBox
                {
                    Header = "Inventory Item",
                    ItemsSource = inventoryItems,
                    DisplayMemberPath = "ItemName",
                    SelectedIndex = 0,
                    Width = 350
                };


            // ==========================================
            // QUANTITY
            // ==========================================

            var quantityBox =
                new NumberBox
                {
                    Header = "Quantity",
                    Value = 1,
                    Minimum = 0.01,
                    SmallChange = 1,
                    Width = 350
                };


            // ==========================================
            // UNIT COST
            // ==========================================

            var unitCostBox =
                new NumberBox
                {
                    Header = "Unit Cost",
                    Value =
                        inventoryItems[0].UnitCost > 0
                            ? (double)inventoryItems[0].UnitCost
                            : 0,

                    Minimum = 0,
                    SmallChange = 100,
                    Width = 350
                };


            itemBox.SelectionChanged +=
                (_, _) =>
                {
                    if (itemBox.SelectedItem
                        is InventoryItem selectedItem)
                    {
                        unitCostBox.Value =
                            (double)selectedItem.UnitCost;
                    }
                };


            // ==========================================
            // DRAFT ITEMS
            // ==========================================

            var draftItems =
                new List<PurchaseItemDraft>();


            var draftList =
                new ListView
                {
                    Height = 220,
                    SelectionMode =
                        ListViewSelectionMode.None
                };


            var totalText =
                new TextBlock
                {
                    Text = "Total: ₱0.00",
                    FontSize = 18,
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.Bold
                };


            // ==========================================
            // REFRESH DRAFT LIST
            //
            // Each row now has Edit and Remove buttons.
            // "Edit" loads the values back into the
            // input fields and removes the row from the
            // draft list so it can be re-added with new
            // values (with the "Add Item" button).
            //
            // "Remove" deletes the row from the draft
            // list entirely.
            // ==========================================

            void RefreshDraftList()
            {
                draftList.Items.Clear();

                foreach (var item in draftItems.ToList())
                {
                    var capturedItem = item;

                    // ------------------------------
                    // ROW GRID
                    // ------------------------------

                    var rowGrid =
                        new Grid
                        {
                            Padding = new Thickness(8, 4, 8, 4)
                        };

                    rowGrid.ColumnDefinitions.Add(
                        new ColumnDefinition
                        {
                            Width = new GridLength(1, GridUnitType.Star)
                        });

                    rowGrid.ColumnDefinitions.Add(
                        new ColumnDefinition
                        {
                            Width = GridLength.Auto
                        });


                    // ------------------------------
                    // TEXT
                    // ------------------------------

                    var text =
                        new TextBlock
                        {
                            Text =
                                $"{capturedItem.ItemName}   " +
                                $"× {capturedItem.Quantity:0.##}   " +
                                $"₱{capturedItem.UnitCost:N2}   " +
                                $"= ₱{capturedItem.Subtotal:N2}",

                            VerticalAlignment =
                                VerticalAlignment.Center,

                            TextTrimming =
                                TextTrimming.CharacterEllipsis
                        };

                    Grid.SetColumn(text, 0);

                    rowGrid.Children.Add(text);


                    // ------------------------------
                    // ACTIONS
                    // ------------------------------

                    var actions =
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 6,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };


                    // ------------------------------
                    // EDIT BUTTON
                    // ------------------------------

                    var editButton =
                        new Button
                        {
                            Content = "Edit"
                        };

                    editButton.Click +=
                        (_, _) =>
                        {
                            // Load this draft back into the
                            // input fields for re-entry.

                            var inventoryItem =
                                inventoryItems
                                    .FirstOrDefault(x =>
                                        x.ItemId ==
                                        capturedItem.ItemId);

                            if (inventoryItem != null)
                            {
                                itemBox.SelectedItem =
                                    inventoryItem;
                            }

                            quantityBox.Value =
                                (double)capturedItem.Quantity;

                            unitCostBox.Value =
                                (double)capturedItem.UnitCost;

                            // Remove from draft list so it
                            // can be re-added with updated values.

                            draftItems.Remove(capturedItem);

                            RefreshDraftList();
                        };


                    // ------------------------------
                    // REMOVE BUTTON
                    // ------------------------------

                    var removeButton =
                        new Button
                        {
                            Content = "Remove"
                        };

                    removeButton.Click +=
                        (_, _) =>
                        {
                            draftItems.Remove(capturedItem);

                            RefreshDraftList();
                        };


                    actions.Children.Add(editButton);
                    actions.Children.Add(removeButton);

                    Grid.SetColumn(actions, 1);

                    rowGrid.Children.Add(actions);


                    draftList.Items.Add(rowGrid);
                }

                var total =
                    draftItems.Sum(x => x.Subtotal);

                totalText.Text =
                    $"Total: ₱{total:N2}";
            }


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
                    if (itemBox.SelectedItem
                        is not InventoryItem selectedItem)
                    {
                        return;
                    }


                    if (double.IsNaN(quantityBox.Value) ||
                        quantityBox.Value <= 0)
                    {
                        return;
                    }


                    if (double.IsNaN(unitCostBox.Value) ||
                        unitCostBox.Value < 0)
                    {
                        return;
                    }


                    var quantity =
                        (decimal)quantityBox.Value;

                    var unitCost =
                        (decimal)unitCostBox.Value;


                    var existing =
                        draftItems.FirstOrDefault(
                            x => x.ItemId ==
                                selectedItem.ItemId);


                    if (existing != null)
                    {
                        existing.Quantity += quantity;
                        existing.UnitCost = unitCost;
                    }
                    else
                    {
                        draftItems.Add(
                            new PurchaseItemDraft
                            {
                                ItemId =
                                    selectedItem.ItemId,

                                ItemName =
                                    selectedItem.ItemName,

                                Quantity =
                                    quantity,

                                UnitCost =
                                    unitCost
                            });
                    }


                    RefreshDraftList();

                    quantityBox.Value = 1;
                };


            // ==========================================
            // NOTES
            // ==========================================

            var notesBox =
                new TextBox
                {
                    Header = "Notes",
                    PlaceholderText =
                        "Optional purchase notes...",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Height = 80,
                    Width = 350
                };


            // ==========================================
            // CONTENT
            // ==========================================

            var content =
                new StackPanel
                {
                    Spacing = 12
                };


            content.Children.Add(supplierBox);

            if (branchBox != null)
            {
                content.Children.Add(branchBox);
            }

            content.Children.Add(purchaseDatePicker);
            content.Children.Add(statusBox);
            content.Children.Add(itemBox);
            content.Children.Add(quantityBox);
            content.Children.Add(unitCostBox);
            content.Children.Add(addItemButton);
            content.Children.Add(draftList);
            content.Children.Add(totalText);
            content.Children.Add(notesBox);


            // ==========================================
            // DIALOG
            // ==========================================

            var dialog =
                new ContentDialog
                {
                    Title = "New Purchase",
                    Content =
                        new ScrollViewer
                        {
                            Content = content,
                            MaxHeight = 700
                        },

                    PrimaryButtonText = "Save Purchase",
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


            // ==========================================
            // VALIDATION
            // ==========================================

            if (supplierBox.SelectedItem
                is not Supplier selectedSupplier)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a supplier.");

                return;
            }


            if (statusBox.SelectedItem
                is not string status)
            {
                status = "Received";
            }


            if (draftItems.Count == 0)
            {
                await ShowMessageAsync(
                    "No Items",
                    "Please add at least one item.");

                return;
            }


            // ==========================================
            // RESOLVE BRANCH FOR PURCHASE
            // ==========================================

            int? purchaseBranchId = null;

            if (branchManagementEnabled)
            {
                if (branchBox?.SelectedItem
                    is Branch selectedBranch)
                {
                    purchaseBranchId =
                        selectedBranch.BranchId;
                }
                else if (currentBranchId.HasValue)
                {
                    purchaseBranchId =
                        currentBranchId.Value;
                }

                if (purchaseBranchId == null)
                {
                    await ShowMessageAsync(
                        "Validation Error",
                        "Please select a branch for this purchase.");

                    return;
                }
            }


            // ==========================================
            // CREATE PURCHASE
            // ==========================================

            var purchaseNumber =
                $"PO-{DateTime.Now:yyyyMMdd-HHmmss}";


            var purchase =
                new PurchaseOrder
                {
                    PurchaseOrderNumber =
                        purchaseNumber,

                    SupplierId =
                        selectedSupplier.SupplierId,

                    BranchId =
                        purchaseBranchId,

                    PurchaseDate =
                        purchaseDatePicker.Date
                            .DateTime
                            .ToUniversalTime(),

                    Status =
                        status,

                    Notes =
                        string.IsNullOrWhiteSpace(
                            notesBox.Text)
                            ? null
                            : notesBox.Text.Trim(),

                    TotalAmount =
                        draftItems.Sum(
                            x => x.Subtotal)
                };


            db.PurchaseOrders.Add(purchase);

            await db.SaveChangesAsync();


            // ==========================================
            // CREATE PURCHASE ITEMS
            // ==========================================

            foreach (var draft in draftItems)
            {
                var purchaseItem =
                    new PurchaseOrderItem
                    {
                        PurchaseOrderId =
                            purchase.PurchaseOrderId,

                        ItemId =
                            draft.ItemId,

                        Quantity =
                            draft.Quantity,

                        UnitCost =
                            draft.UnitCost
                    };


                db.PurchaseOrderItems.Add(
                    purchaseItem);
            }


            // ==========================================
            // UPDATE INVENTORY
            // ==========================================

            if (status == "Received")
            {
                foreach (var draft in draftItems)
                {
                    var inventory =
                        await db.Inventories
                            .FirstOrDefaultAsync(x =>
                                x.ItemId ==
                                    draft.ItemId
                                &&
                                x.BranchId ==
                                    purchase.BranchId);


                    if (inventory == null)
                    {
                        inventory =
                            new Inventory
                            {
                                ItemId =
                                    draft.ItemId,

                                BranchId =
                                    purchase.BranchId,

                                QuantityOnHand =
                                    draft.Quantity
                            };

                        db.Inventories.Add(
                            inventory);
                    }
                    else
                    {
                        inventory.QuantityOnHand +=
                            draft.Quantity;
                    }
                }
            }


            await db.SaveChangesAsync();

            await LoadPurchasesAsync();

            PurchaseListView.SelectedItem = null;

            await ShowMessageAsync(
                "Purchase Saved",
                $"Purchase {purchase.PurchaseOrderNumber} was recorded successfully.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Failed to create purchase: " + ex);

            await ShowMessageAsync(
                "Purchase Error",
                ex.Message);
        }
    }


    // ==========================================
    // MESSAGE
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
    // DISPLAY CLASSES
    // ==========================================

    private class PurchaseDisplayItem
    {
        public int PurchaseOrderId { get; set; }

        public string PurchaseOrderNumber { get; set; }
            = string.Empty;

        public string SupplierName { get; set; }
            = string.Empty;

        public string PurchaseDateText { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        public decimal TotalAmount { get; set; }

        public string TotalAmountText { get; set; }
            = string.Empty;
    }


    private class PurchaseItemDisplay
    {
        public string ItemName { get; set; }
            = string.Empty;

        public string QuantityText { get; set; }
            = string.Empty;

        public string UnitCostText { get; set; }
            = string.Empty;

        public string SubtotalText { get; set; }
            = string.Empty;
    }


    private class PurchaseItemDraft
    {
        public int ItemId { get; set; }

        public string ItemName { get; set; }
            = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitCost { get; set; }

        public decimal Subtotal =>
            Quantity * UnitCost;
    }

    private async void AddNewPurchaseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await CreateNewPurchaseAsync();
    }
}