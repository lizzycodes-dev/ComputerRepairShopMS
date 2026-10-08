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
                    PurchaseListView.ItemsSource = null;

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

            var purchases =
                await query
                    .OrderByDescending(x => x.PurchaseDate)
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

            PurchaseListView.ItemsSource = purchases;

            EmptyStateText.Visibility =
                purchases.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (purchases.Count == 0)
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
    // SELECT PURCHASE
    // ==========================================

    private async void PurchaseListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (PurchaseListView.SelectedItem
            is not PurchaseDisplayItem selectedPurchase)
        {
            return;
        }

        await LoadPurchaseDetailsAsync(
            selectedPurchase.PurchaseOrderId);

        // Switch to Purchase Details tab
        PurchaseTabView.SelectedIndex = 1;
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
                    Height = 180,
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


            void RefreshDraftList()
            {
                draftList.Items.Clear();

                foreach (var item in draftItems)
                {
                    draftList.Items.Add(
                        new TextBlock
                        {
                            Text =
                                $"{item.ItemName}   " +
                                $"× {item.Quantity:0.##}   " +
                                $"₱{item.UnitCost:N2}   " +
                                $"= ₱{item.Subtotal:N2}"
                        });
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