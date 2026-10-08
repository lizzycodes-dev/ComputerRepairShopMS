using ComputerRepairSystem.company.Context;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class InventoryManagementPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly MasterErpDbContext _masterDb;
    private readonly CurrentBranchContext _currentBranchContext;

    // ==========================================
    // PAGINATION STATE
    // ==========================================

    private List<InventoryDisplayItem> _inventory = new();
    private List<InventoryDisplayItem> _filteredInventory = new();

    private int _currentPage = 1;
    private const int _pageSize = 10;


    public InventoryManagementPage(
        TenantDbContextFactory tenantDbFactory,
        MasterErpDbContext masterDb,
        CurrentBranchContext currentBranchContext)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;
        _masterDb = masterDb;
        _currentBranchContext = currentBranchContext;

        Loaded += InventoryManagementPage_Loaded;
        Unloaded += InventoryManagementPage_Unloaded;
    }


    // ==============================
    // BRANCH MANAGEMENT
    // ==============================

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


    // ==============================
    // PAGE LOADED / UNLOADED
    // ==============================

    private async void InventoryManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged +=
            OnGlobalBranchChanged;

        await LoadInventoryAsync();
    }


    private void InventoryManagementPage_Unloaded(
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

        SearchBox.Text = string.Empty;

        await LoadInventoryAsync();
    }


    // ==============================
    // LOAD INVENTORY
    //
    // Loads the FULL CATALOG (InventoryItems), then left-joins
    // stock from Inventories for the current branch. This way
    // items with no stock row yet still appear (qty 0).
    // ==============================

    private async Task LoadInventoryAsync()
    {
        if (CurrentUser.CompanyId == null)
        {
            _inventory = new();
            ApplySearch();
            return;
        }

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
                    _inventory = new();
                    _filteredInventory = new();
                    ApplyPagination();

                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }


            // ==========================================
            // 1. LOAD FULL CATALOG
            // ==========================================

            var catalog =
                await db.InventoryItems
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.ItemId)
                    .ToListAsync();


            if (catalog.Count == 0)
            {
                _inventory = new();
                ApplySearch();
                return;
            }


            var itemIds =
                catalog.Select(x => x.ItemId).ToList();


            // ==========================================
            // 2. LOAD INVENTORY ROWS (per branch)
            // ==========================================

            var inventoryQuery =
                db.Inventories
                    .AsNoTracking()
                    .Where(x => itemIds.Contains(x.ItemId));

            if (branchManagementEnabled)
            {
                inventoryQuery = inventoryQuery.Where(x =>
                    x.BranchId == currentBranchId!.Value);
            }
            else
            {
                inventoryQuery = inventoryQuery.Where(x =>
                    x.BranchId == null);
            }

            var inventories =
                await inventoryQuery.ToListAsync();

            var inventoryByItemId =
                inventories
                    .GroupBy(x => x.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First());


            // ==========================================
            // 3. USED QUANTITIES (branch-filtered)
            // ==========================================

            var usedQuery =
                db.RepairItems
                    .AsNoTracking()
                    .Where(x => itemIds.Contains(x.ItemId));

            if (branchManagementEnabled)
            {
                usedQuery = usedQuery.Where(x =>
                    x.Repair!.ServiceRequest!.BranchId ==
                    currentBranchId!.Value);
            }

            var usedQuantities =
                await usedQuery
                    .GroupBy(x => x.ItemId)
                    .Select(g => new
                    {
                        ItemId = g.Key,
                        Quantity = g.Sum(x => x.Quantity)
                    })
                    .ToDictionaryAsync(
                        x => x.ItemId,
                        x => x.Quantity);


            // ==========================================
            // 4. PROJECT
            // ==========================================

            _inventory =
                catalog
                    .Select(item =>
                    {
                        inventoryByItemId.TryGetValue(
                            item.ItemId,
                            out var stockRow);

                        var quantityOnHand =
                            stockRow?.QuantityOnHand ?? 0;

                        usedQuantities.TryGetValue(
                            item.ItemId,
                            out var usedQuantity);

                        return new InventoryDisplayItem
                        {
                            InventoryId =
                                stockRow?.InventoryId ?? 0,

                            ItemId = item.ItemId,

                            ItemName = item.ItemName,

                            Category = item.Category,

                            Brand = item.Brand,

                            Model = item.Model,

                            Unit = item.Unit,

                            QuantityOnHand = quantityOnHand,

                            AvailableQuantity =
                                quantityOnHand - usedQuantity
                        };
                    })
                    .ToList();

            ApplySearch();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Inventory",
                ex.Message);
        }
    }


    // ==============================
    // SEARCH
    // ==============================

    private void SearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplySearch();
    }


    private void ApplySearch()
    {
        var search =
            SearchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(search))
        {
            _filteredInventory = _inventory;
        }
        else
        {
            _filteredInventory =
                _inventory
                    .Where(x =>
                        x.ItemName.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)

                        || x.Category.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)

                        || (x.Brand ?? "")
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)

                        || (x.Model ?? "")
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
        }

        // Reset to page 1 whenever the search changes
        _currentPage = 1;

        ApplyPagination();
    }


    // ==============================
    // PAGINATION
    // ==============================

    private void ApplyPagination()
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_filteredInventory.Count /
                    _pageSize));

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        if (_currentPage < 1)
            _currentPage = 1;

        var paged =
            _filteredInventory
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        InventoryList.ItemsSource = paged;

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
            return;

        _currentPage--;

        ApplyPagination();
    }


    private void NextPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_filteredInventory.Count /
                    _pageSize));

        if (_currentPage >= totalPages)
            return;

        _currentPage++;

        ApplyPagination();
    }


    // ==============================
    // NEW ITEM
    //
    // Creates the catalog row AND a zero-quantity Inventory row
    // for the current branch (or null-branch if branch module off),
    // so the item is immediately visible in the table.
    // ==============================

    private async void NewItemButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var panel = new StackPanel
        {
            Spacing = 10
        };


        var itemNameBox = new TextBox
        {
            Header = "Item Name",
            PlaceholderText = "Example: Laptop RAM"
        };


        var categoryBox = new TextBox
        {
            Header = "Category",
            PlaceholderText = "Example: Memory"
        };


        var brandBox = new TextBox
        {
            Header = "Brand"
        };


        var modelBox = new TextBox
        {
            Header = "Model"
        };


        var unitBox = new TextBox
        {
            Header = "Unit",
            Text = "Piece"
        };


        var unitCostBox = new NumberBox
        {
            Header = "Unit Cost",
            Minimum = 0
        };


        var unitPriceBox = new NumberBox
        {
            Header = "Unit Price",
            Minimum = 0
        };


        var reorderLevelBox = new NumberBox
        {
            Header = "Reorder Level",
            Minimum = 0
        };


        panel.Children.Add(itemNameBox);
        panel.Children.Add(categoryBox);
        panel.Children.Add(brandBox);
        panel.Children.Add(modelBox);
        panel.Children.Add(unitBox);
        panel.Children.Add(unitCostBox);
        panel.Children.Add(unitPriceBox);
        panel.Children.Add(reorderLevelBox);


        var scrollViewer = new ScrollViewer
        {
            Content = panel,
            MaxHeight = 500
        };


        var dialog = new ContentDialog
        {
            Title = "New Inventory Item",
            Content = scrollViewer,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };


        var result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;


        if (string.IsNullOrWhiteSpace(itemNameBox.Text))
        {
            await ShowMessageAsync(
                "Missing Item Name",
                "Please enter an item name.");

            return;
        }


        if (string.IsNullOrWhiteSpace(categoryBox.Text))
        {
            await ShowMessageAsync(
                "Missing Category",
                "Please enter a category.");

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


            // ==========================================
            // DETERMINE TARGET BRANCH
            // ==========================================

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? targetBranchId = null;

            if (branchManagementEnabled)
            {
                targetBranchId =
                    _currentBranchContext.BranchId;

                if (targetBranchId == null)
                {
                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }


            // ==========================================
            // CREATE CATALOG ITEM
            // ==========================================

            var item = new InventoryItem
            {
                ItemName = itemNameBox.Text.Trim(),
                Category = categoryBox.Text.Trim(),
                Brand = string.IsNullOrWhiteSpace(brandBox.Text)
                    ? null
                    : brandBox.Text.Trim(),
                Model = string.IsNullOrWhiteSpace(modelBox.Text)
                    ? null
                    : modelBox.Text.Trim(),
                Unit = string.IsNullOrWhiteSpace(unitBox.Text)
                    ? "Piece"
                    : unitBox.Text.Trim(),
                UnitCost = (decimal)unitCostBox.Value,
                UnitPrice = (decimal)unitPriceBox.Value,
                ReorderLevel = (decimal)reorderLevelBox.Value,
                IsActive = true
            };


            db.InventoryItems.Add(item);

            await db.SaveChangesAsync();


            // ==========================================
            // CREATE ZERO-QUANTITY INVENTORY ROW
            //
            // This ensures the item shows up immediately in the
            // branch-filtered list without needing "Add Stock"
            // first.
            // ==========================================

            var stockRow = new Inventory
            {
                ItemId = item.ItemId,
                BranchId = targetBranchId,
                QuantityOnHand = 0
            };

            db.Inventories.Add(stockRow);

            await db.SaveChangesAsync();


            await LoadInventoryAsync();


            await ShowMessageAsync(
                "Item Added",
                "The inventory item has been added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Adding Item",
                ex.Message);
        }
    }


    // ==============================
    // ADD STOCK
    // ==============================

    private async void AddStockButton_Click(
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


            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? targetBranchId = null;

            if (branchManagementEnabled)
            {
                targetBranchId =
                    _currentBranchContext.BranchId;

                if (targetBranchId == null)
                {
                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }


            var items = await db.InventoryItems
                .Where(x => x.IsActive)
                .OrderBy(x => x.ItemName)
                .ToListAsync();


            if (items.Count == 0)
            {
                await ShowMessageAsync(
                    "No Inventory Items",
                    "Add an inventory item first.");

                return;
            }


            var itemComboBox = new ComboBox
            {
                Header = "Inventory Item",
                PlaceholderText = "Select an item",
                ItemsSource = items,
                DisplayMemberPath = "ItemName"
            };


            var quantityBox = new NumberBox
            {
                Header = "Quantity",
                Minimum = 1,
                Value = 1
            };


            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(itemComboBox);
            panel.Children.Add(quantityBox);

            if (branchManagementEnabled)
            {
                panel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            "Stock will be added to the currently selected branch.",
                        Opacity = 0.7,
                        TextWrapping = TextWrapping.Wrap
                    });
            }


            var dialog = new ContentDialog
            {
                Title = "Add Stock",
                Content = panel,
                PrimaryButtonText = "Add Stock",
                CloseButtonText = "Cancel",
                XamlRoot = XamlRoot
            };


            var result =
                await dialog.ShowAsync();


            if (result != ContentDialogResult.Primary)
                return;


            if (itemComboBox.SelectedItem is not InventoryItem selectedItem)
            {
                await ShowMessageAsync(
                    "No Item Selected",
                    "Please select an inventory item.");

                return;
            }


            var quantity =
                (decimal)quantityBox.Value;


            var inventory =
                await db.Inventories
                    .FirstOrDefaultAsync(
                        x =>
                            x.ItemId == selectedItem.ItemId &&
                            x.BranchId == targetBranchId);


            if (inventory == null)
            {
                inventory = new Inventory
                {
                    ItemId = selectedItem.ItemId,
                    BranchId = targetBranchId,
                    QuantityOnHand = quantity
                };

                db.Inventories.Add(inventory);
            }
            else
            {
                inventory.QuantityOnHand += quantity;
            }


            await db.SaveChangesAsync();


            await LoadInventoryAsync();


            await ShowMessageAsync(
                "Stock Added",
                $"{selectedItem.ItemName} stock has been updated.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Adding Stock",
                ex.Message);
        }
    }


    // ==============================
    // EDIT ITEM
    // ==============================

    private async void EditItemButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not InventoryDisplayItem displayItem)
        {
            return;
        }
        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        await using var db =
            await _tenantDbFactory.CreateAsync(
                CurrentUser.CompanyId.Value);

        var item =
            await db.InventoryItems
                .FirstOrDefaultAsync(
                    x => x.ItemId == displayItem.ItemId);

        if (item == null)
        {
            await ShowMessageAsync(
                "Item Not Found",
                "The inventory item could not be found.");

            return;
        }


        // ==============================
        // INPUT FIELDS
        // ==============================

        var itemNameBox = new TextBox
        {
            Header = "Item Name",
            Text = item.ItemName
        };

        var categoryBox = new TextBox
        {
            Header = "Category",
            Text = item.Category
        };

        var descriptionBox = new TextBox
        {
            Header = "Description",
            Text = item.Description ?? "",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap
        };

        var brandBox = new TextBox
        {
            Header = "Brand",
            Text = item.Brand ?? ""
        };

        var modelBox = new TextBox
        {
            Header = "Model",
            Text = item.Model ?? ""
        };

        var unitBox = new TextBox
        {
            Header = "Unit",
            Text = item.Unit
        };

        var unitCostBox = new NumberBox
        {
            Header = "Unit Cost",
            Value = (double)item.UnitCost,
            Minimum = 0
        };

        var unitPriceBox = new NumberBox
        {
            Header = "Unit Price",
            Value = (double)item.UnitPrice,
            Minimum = 0
        };

        var reorderLevelBox = new NumberBox
        {
            Header = "Reorder Level",
            Value = (double)item.ReorderLevel,
            Minimum = 0
        };


        // ==============================
        // FORM
        // ==============================

        var panel = new StackPanel
        {
            Spacing = 10
        };

        panel.Children.Add(itemNameBox);
        panel.Children.Add(categoryBox);
        panel.Children.Add(descriptionBox);
        panel.Children.Add(brandBox);
        panel.Children.Add(modelBox);
        panel.Children.Add(unitBox);
        panel.Children.Add(unitCostBox);
        panel.Children.Add(unitPriceBox);
        panel.Children.Add(reorderLevelBox);


        var scrollViewer = new ScrollViewer
        {
            Content = panel,
            MaxHeight = 500
        };


        // ==============================
        // DIALOG
        // ==============================

        var dialog = new ContentDialog
        {
            Title = "Edit Inventory Item",
            Content = scrollViewer,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = XamlRoot
        };

        var result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;


        // ==============================
        // VALIDATION
        // ==============================

        if (string.IsNullOrWhiteSpace(itemNameBox.Text))
        {
            await ShowMessageAsync(
                "Missing Item Name",
                "Please enter an item name.");

            return;
        }

        if (string.IsNullOrWhiteSpace(categoryBox.Text))
        {
            await ShowMessageAsync(
                "Missing Category",
                "Please enter a category.");

            return;
        }


        // ==============================
        // UPDATE ITEM
        // ==============================

        item.ItemName =
            itemNameBox.Text.Trim();

        item.Category =
            categoryBox.Text.Trim();

        item.Description =
            string.IsNullOrWhiteSpace(descriptionBox.Text)
                ? null
                : descriptionBox.Text.Trim();

        item.Brand =
            string.IsNullOrWhiteSpace(brandBox.Text)
                ? null
                : brandBox.Text.Trim();

        item.Model =
            string.IsNullOrWhiteSpace(modelBox.Text)
                ? null
                : modelBox.Text.Trim();

        item.Unit =
            string.IsNullOrWhiteSpace(unitBox.Text)
                ? "Piece"
                : unitBox.Text.Trim();

        item.UnitCost =
            (decimal)unitCostBox.Value;

        item.UnitPrice =
            (decimal)unitPriceBox.Value;

        item.ReorderLevel =
            (decimal)reorderLevelBox.Value;


        await db.SaveChangesAsync();

        await LoadInventoryAsync();

        await ShowMessageAsync(
            "Item Updated",
            "The inventory item has been updated successfully.");
    }


    // ==============================
    // MESSAGE
    // ==============================

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