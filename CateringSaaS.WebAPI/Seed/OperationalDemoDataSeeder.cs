using CateringSaaS.Modules.Identity.Domain;
using CateringSaaS.Modules.Inventory.Domain.Enums;
using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Menu.Domain;
using CateringSaaS.Modules.Ordering.Domain;
using CateringSaaS.Modules.Tenants.Domain;
using CateringSaaS.Shared.Contracts;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.SeedData;
using Microsoft.EntityFrameworkCore;
using InventoryEntity = CateringSaaS.Modules.Inventory.Domain.Models.Inventory;

namespace CateringSaaS.WebAPI.Seed;

/// <summary>
/// Seeds a realistic 7–10 day operational dataset for the default Romashka workspace.
/// Reuses the existing global ingredient catalog (does not recreate products).
/// </summary>
public sealed class OperationalDemoDataSeeder : IOperationalDemoSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<OperationalDemoDataSeeder> _logger;

    public OperationalDemoDataSeeder(AppDbContext db, ILogger<OperationalDemoDataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var workspaceId = DevelopmentSeedIds.MockWorkspaceId;

        if (!await _db.Set<Workspace>().AnyAsync(w => w.Id == workspaceId, cancellationToken))
        {
            _logger.LogWarning("Demo workspace {WorkspaceId} is missing; skipping operational demo seed.", workspaceId);
            return;
        }

        var alreadySeeded = await _db.Set<Menu>()
            .IgnoreQueryFilters()
            .AnyAsync(
                m => m.WorkspaceId == workspaceId && m.Name == DevelopmentSeedIds.DemoMenuName,
                cancellationToken);

        if (alreadySeeded)
        {
            _logger.LogInformation("Operational demo data already present; skipping.");
            return;
        }

        _logger.LogInformation("Seeding operational demo data for workspace {WorkspaceId}...", workspaceId);

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var rangeStart = today.AddDays(-9);

        var clients = await EnsureClientsAsync(workspaceId, cancellationToken);
        var drivers = await EnsureDriversAsync(workspaceId, cancellationToken);
        var placers = await EnsurePlacersAsync(workspaceId, clients, cancellationToken);
        var suppliers = await EnsureSuppliersAsync(workspaceId, cancellationToken);
        var ingredients = await ResolveIngredientsAsync(cancellationToken);

        await EnsureIngredientCostsAsync(ingredients, cancellationToken);
        await SeedPurchasesAndStockAsync(workspaceId, ingredients, suppliers, now, cancellationToken);

        var dishes = await SeedDishesAsync(workspaceId, ingredients, cancellationToken);
        var menuItemsByDate = await SeedPublishedMenuAsync(
            workspaceId, dishes, rangeStart, today.AddDays(2), cancellationToken);

        var orders = await SeedOrdersAsync(
            workspaceId, clients, placers, drivers, menuItemsByDate, dishes, today, now, cancellationToken);

        await SeedConsumeAndAdjustmentsAsync(workspaceId, orders, dishes, ingredients, now, cancellationToken);
        await SeedReviewsAsync(workspaceId, orders, placers, today, now, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Operational demo seed complete: {Clients} clients, {Dishes} dishes, {Orders} orders.",
            clients.Count,
            dishes.Count,
            orders.Count);
    }

    private async Task<List<ClientCompany>> EnsureClientsAsync(Guid workspaceId, CancellationToken ct)
    {
        var defs = new (Guid Id, string Name, string Address, string Contact, string Phone, ClientOrderCadence Cadence)[]
        {
            (DevelopmentSeedIds.MockClientCompanyId, "EPAM Systems",
                "14a Akademika Filatova St, Kyiv 03124", "Olena Kovalenko", "+380 44 585 1100",
                ClientOrderCadence.Weekly),
            (DevelopmentSeedIds.SoftServeClientId, "SoftServe",
                "52 Velyka Vasylkivska St, Kyiv 03150", "Andriy Melnyk", "+380 32 240 9090",
                ClientOrderCadence.Daily),
            (DevelopmentSeedIds.SigmaSoftwareClientId, "Sigma Software",
                "1 Hryhorenka Ave, Kyiv 02140", "Iryna Bondar", "+380 57 766 0050",
                ClientOrderCadence.Weekly),
            (DevelopmentSeedIds.GlobalLogicClientId, "GlobalLogic",
                "1D Sportyvna Sq, Kyiv 01023", "Dmytro Shevchenko", "+380 44 594 6500",
                ClientOrderCadence.Daily),
        };

        var set = _db.Set<ClientCompany>();
        var result = new List<ClientCompany>();

        foreach (var def in defs)
        {
            var existing = await set.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == def.Id, ct);
            if (existing is null)
            {
                existing = new ClientCompany
                {
                    Id = def.Id,
                    WorkspaceId = workspaceId,
                    Name = def.Name,
                    Address = def.Address,
                    ContactName = def.Contact,
                    ContactPhone = def.Phone,
                    OrderCadence = def.Cadence,
                    IsActive = true
                };
                await set.AddAsync(existing, ct);
            }
            else
            {
                existing.Name = def.Name;
                existing.Address = def.Address;
                existing.ContactName = def.Contact;
                existing.ContactPhone = def.Phone;
                existing.OrderCadence = def.Cadence;
                existing.IsActive = true;
                existing.WorkspaceId = workspaceId;
            }

            result.Add(existing);
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }

    private async Task<List<User>> EnsureDriversAsync(Guid workspaceId, CancellationToken ct)
    {
        var users = _db.Set<User>();
        var drivers = await users
            .Where(u => u.WorkspaceId == workspaceId && u.Role == StaffRole.Driver && u.IsActive)
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

        if (drivers.Count >= 2)
        {
            return drivers.Take(2).ToList();
        }

        var hash = await users
            .Where(u => u.WorkspaceId == workspaceId)
            .Select(u => u.PasswordHash)
            .FirstOrDefaultAsync(ct)
            ?? await users.Select(u => u.PasswordHash).FirstAsync(ct);

        while (drivers.Count < 2)
        {
            var index = drivers.Count + 1;
            var username = index == 1 ? "driver" : "driver2";
            if (await users.AnyAsync(u => u.Username == username, ct))
            {
                username = $"driver{index}_{Guid.NewGuid().ToString("N")[..6]}";
            }

            var driver = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = hash,
                FirstName = index == 1 ? "Delivery" : "Route",
                LastName = index == 1 ? "Driver" : "Driver 2",
                Role = StaffRole.Driver,
                WorkspaceId = workspaceId,
                IsActive = true
            };
            await users.AddAsync(driver, ct);
            drivers.Add(driver);
        }

        await _db.SaveChangesAsync(ct);
        return drivers.Take(2).ToList();
    }

    private async Task<Dictionary<Guid, User>> EnsurePlacersAsync(
        Guid workspaceId,
        IReadOnlyList<ClientCompany> clients,
        CancellationToken ct)
    {
        var users = _db.Set<User>();
        var hash = await users
            .Where(u => u.WorkspaceId == workspaceId)
            .Select(u => u.PasswordHash)
            .FirstAsync(ct);

        var map = new Dictionary<Guid, User>();
        foreach (var client in clients)
        {
            var existing = await users.FirstOrDefaultAsync(
                u => u.WorkspaceId == workspaceId
                     && u.ClientCompanyId == client.Id
                     && (u.Role == StaffRole.ClientAdmin || u.Role == StaffRole.ClientEmployee),
                ct);

            if (existing is null)
            {
                var slug = client.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
                var username = $"{slug}.office";
                if (await users.AnyAsync(u => u.Username == username, ct))
                {
                    username = $"{slug}.office.{Guid.NewGuid().ToString("N")[..4]}";
                }

                existing = new User
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = $"{slug}.office@demo.test",
                    PasswordHash = hash,
                    FirstName = client.ContactName?.Split(' ').FirstOrDefault() ?? "Office",
                    LastName = "Manager",
                    Role = StaffRole.ClientAdmin,
                    WorkspaceId = workspaceId,
                    ClientCompanyId = client.Id,
                    CompanyId = client.Id,
                    IsActive = true
                };
                await users.AddAsync(existing, ct);
            }

            map[client.Id] = existing;
        }

        await _db.SaveChangesAsync(ct);
        return map;
    }

    private async Task<List<Supplier>> EnsureSuppliersAsync(Guid workspaceId, CancellationToken ct)
    {
        var set = _db.Set<Supplier>();
        var defs = new (Guid Id, string Name, string Phone, string Email)[]
        {
            (DevelopmentSeedIds.DemoSupplierId, "Metro Cash & Carry", "+380 44 490 2500", "orders@metro-demo.ua"),
            (DevelopmentSeedIds.DemoSupplierAltId, "AgroFusion Fresh", "+380 44 333 2211", "sales@agrofusion-demo.ua"),
        };

        var list = new List<Supplier>();
        foreach (var def in defs)
        {
            var existing = await set.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == def.Id, ct);
            if (existing is null)
            {
                existing = new Supplier
                {
                    Id = def.Id,
                    WorkspaceId = workspaceId,
                    Name = def.Name,
                    Phone = def.Phone,
                    Email = def.Email,
                    Notes = "Demo supplier",
                    IsActive = true
                };
                await set.AddAsync(existing, ct);
            }

            list.Add(existing);
        }

        await _db.SaveChangesAsync(ct);
        return list;
    }

    private async Task<Dictionary<string, Ingredient>> ResolveIngredientsAsync(CancellationToken ct)
    {
        var needed = new[]
        {
            "Chicken Breast", "Salmon", "Beef", "Rice", "Pasta", "Potato", "Lettuce",
            "Tomato", "Onion", "Garlic", "Cream", "Cheese", "Olive Oil", "Egg",
            "Carrot", "Bell Pepper", "Sour Cream", "Flour"
        };

        var found = await _db.Set<Ingredient>()
            .Where(i => i.WorkspaceId == null && needed.Contains(i.Name))
            .ToListAsync(ct);

        return found
            .GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task EnsureIngredientCostsAsync(Dictionary<string, Ingredient> ingredients, CancellationToken ct)
    {
        var costs = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Chicken Breast"] = 0.18m,
            ["Salmon"] = 0.42m,
            ["Beef"] = 0.28m,
            ["Rice"] = 0.03m,
            ["Pasta"] = 0.025m,
            ["Potato"] = 0.012m,
            ["Lettuce"] = 0.04m,
            ["Tomato"] = 0.035m,
            ["Onion"] = 0.015m,
            ["Garlic"] = 0.08m,
            ["Cream"] = 0.05m,
            ["Cheese"] = 0.12m,
            ["Olive Oil"] = 0.09m,
            ["Egg"] = 4.5m,
            ["Carrot"] = 0.014m,
            ["Bell Pepper"] = 0.05m,
            ["Sour Cream"] = 0.045m,
            ["Flour"] = 0.02m,
        };

        foreach (var (name, ingredient) in ingredients)
        {
            if (ingredient.CostPerUnit is null && costs.TryGetValue(name, out var cost))
            {
                ingredient.CostPerUnit = cost;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedPurchasesAndStockAsync(
        Guid workspaceId,
        Dictionary<string, Ingredient> ingredients,
        IReadOnlyList<Supplier> suppliers,
        DateTime now,
        CancellationToken ct)
    {
        var purchasePlan = new (string Name, decimal Qty, decimal TotalCost)[]
        {
            ("Chicken Breast", 25000m, 4500m),
            ("Salmon", 12000m, 5040m),
            ("Beef", 15000m, 4200m),
            ("Rice", 40000m, 1200m),
            ("Pasta", 20000m, 500m),
            ("Potato", 50000m, 600m),
            ("Lettuce", 8000m, 320m),
            ("Tomato", 10000m, 350m),
            ("Onion", 12000m, 180m),
            ("Garlic", 2000m, 160m),
            ("Cream", 15000m, 750m),
            ("Cheese", 8000m, 960m),
            ("Olive Oil", 5000m, 450m),
            ("Egg", 200m, 900m),
            ("Carrot", 10000m, 140m),
            ("Bell Pepper", 6000m, 300m),
            ("Sour Cream", 6000m, 270m),
            ("Flour", 15000m, 300m),
        };

        var dayOffsets = new[] { -9, -7, -5, -3, -1 };
        var batchIndex = 0;

        foreach (var (name, qty, totalCost) in purchasePlan)
        {
            if (!ingredients.TryGetValue(name, out var ingredient))
            {
                continue;
            }

            var supplier = suppliers[batchIndex % suppliers.Count];
            var receivedAt = now.Date.AddDays(dayOffsets[batchIndex % dayOffsets.Length]).AddHours(8 + batchIndex % 5);
            if (receivedAt.Kind != DateTimeKind.Utc)
            {
                receivedAt = DateTime.SpecifyKind(receivedAt, DateTimeKind.Utc);
            }

            var remainingShare = 0.55m + (batchIndex % 4) * 0.08m;
            var currentQty = Math.Round(qty * remainingShare, 2);

            await _db.Set<StockBatch>().AddAsync(
                new StockBatch
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    IngredientId = ingredient.Id,
                    SupplierId = supplier.Id,
                    InitialQuantity = qty,
                    CurrentQuantity = currentQty,
                    CostPrice = totalCost,
                    ReceivedAt = receivedAt
                },
                ct);

            await _db.Set<InventoryMovement>().AddAsync(
                new InventoryMovement
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    IngredientId = ingredient.Id,
                    Type = InventoryMovementType.Purchase,
                    Quantity = qty,
                    SignedQuantity = qty,
                    TotalCost = totalCost,
                    Source = supplier.Name,
                    Reason = null,
                    CreatedAt = receivedAt
                },
                ct);

            var inventory = await _db.Set<InventoryEntity>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    i => i.WorkspaceId == workspaceId && i.IngredientId == ingredient.Id,
                    ct);

            if (inventory is null)
            {
                await _db.Set<InventoryEntity>().AddAsync(
                    new InventoryEntity
                    {
                        Id = Guid.NewGuid(),
                        WorkspaceId = workspaceId,
                        IngredientId = ingredient.Id,
                        TotalQuantity = currentQty
                    },
                    ct);
            }
            else
            {
                inventory.TotalQuantity = currentQty;
            }

            batchIndex++;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<Dish>> SeedDishesAsync(
        Guid workspaceId,
        Dictionary<string, Ingredient> ingredients,
        CancellationToken ct)
    {
        Ingredient? Get(string name) =>
            ingredients.TryGetValue(name, out var item) ? item : null;

        var recipes = new List<(string Name, string Desc, DishCategory Cat, int Weight, (string Ing, decimal Qty)[] Lines)>
        {
            ("Creamy Salmon Pasta", "Salmon, cream sauce, pasta", DishCategory.Main, 380,
            [
                ("Salmon", 120m), ("Pasta", 100m), ("Cream", 80m), ("Garlic", 5m), ("Olive Oil", 10m)
            ]),
            ("Chicken Rice Bowl", "Grilled chicken with rice and vegetables", DishCategory.Main, 420,
            [
                ("Chicken Breast", 150m), ("Rice", 120m), ("Bell Pepper", 40m), ("Onion", 30m), ("Olive Oil", 10m)
            ]),
            ("Caesar Salad", "Classic Caesar with chicken", DishCategory.Salad, 280,
            [
                ("Lettuce", 100m), ("Chicken Breast", 80m), ("Cheese", 30m), ("Olive Oil", 15m), ("Egg", 1m)
            ]),
            ("Beef Goulash", "Slow-cooked beef with paprika", DishCategory.Main, 400,
            [
                ("Beef", 160m), ("Potato", 100m), ("Onion", 40m), ("Carrot", 40m), ("Flour", 15m)
            ]),
            ("Potato Cream Soup", "Velvety potato soup", DishCategory.Soup, 300,
            [
                ("Potato", 180m), ("Cream", 60m), ("Onion", 30m), ("Garlic", 5m), ("Olive Oil", 8m)
            ]),
            ("Veggie Omelette", "Eggs with fresh vegetables", DishCategory.Main, 250,
            [
                ("Egg", 2m), ("Tomato", 50m), ("Bell Pepper", 40m), ("Cheese", 25m), ("Olive Oil", 8m)
            ]),
            ("Beef Stroganoff", "Beef in sour cream sauce with rice", DishCategory.Main, 410,
            [
                ("Beef", 140m), ("Sour Cream", 60m), ("Onion", 35m), ("Rice", 100m), ("Flour", 12m)
            ]),
            ("Garden Side Salad", "Fresh vegetables", DishCategory.Side, 180,
            [
                ("Lettuce", 70m), ("Tomato", 50m), ("Carrot", 30m), ("Bell Pepper", 25m), ("Olive Oil", 10m)
            ]),
            ("Garlic Bread Side", "Toasted with garlic oil", DishCategory.Side, 120,
            [
                ("Flour", 60m), ("Garlic", 8m), ("Olive Oil", 15m), ("Cheese", 15m)
            ]),
        };

        var dishes = new List<Dish>();
        foreach (var recipe in recipes)
        {
            var lines = recipe.Lines
                .Select(l => (Ing: Get(l.Ing), l.Qty))
                .Where(l => l.Ing is not null)
                .Select(l => (l.Ing!, l.Qty))
                .ToList();

            if (lines.Count == 0)
            {
                continue;
            }

            var dish = new Dish
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Name = recipe.Name,
                Description = recipe.Desc,
                Category = recipe.Cat,
                OutputWeight = recipe.Weight,
                Instructions = "Demo tech card",
                IsActive = true
            };

            foreach (var (ing, qty) in lines)
            {
                dish.Ingredients.Add(new DishIngredient
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    DishId = dish.Id,
                    IngredientId = ing.Id,
                    Quantity = qty
                });
            }

            await _db.Set<Dish>().AddAsync(dish, ct);
            dishes.Add(dish);
        }

        await _db.SaveChangesAsync(ct);
        return dishes;
    }

    private async Task<Dictionary<DateOnly, List<MenuItem>>> SeedPublishedMenuAsync(
        Guid workspaceId,
        IReadOnlyList<Dish> dishes,
        DateOnly start,
        DateOnly end,
        CancellationToken ct)
    {
        var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Creamy Salmon Pasta"] = 12.5m,
            ["Chicken Rice Bowl"] = 10.9m,
            ["Caesar Salad"] = 8.5m,
            ["Beef Goulash"] = 11.8m,
            ["Potato Cream Soup"] = 6.5m,
            ["Veggie Omelette"] = 7.2m,
            ["Beef Stroganoff"] = 12.0m,
            ["Garden Side Salad"] = 4.5m,
            ["Garlic Bread Side"] = 3.5m,
        };

        var menu = new Menu
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ClientCompanyId = null,
            Name = DevelopmentSeedIds.DemoMenuName,
            StartDate = start,
            EndDate = end,
            Status = MenuStatus.Published
        };

        var byDate = new Dictionary<DateOnly, List<MenuItem>>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            // Skip Sundays for a slightly realistic corporate calendar.
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            var day = new MenuDay
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                MenuId = menu.Id,
                Date = date
            };

            var offset = date.DayNumber - start.DayNumber;
            var dayDishes = dishes
                .Where((_, index) => index % 3 != (offset % 3) || dishes.Count <= 3)
                .Take(5)
                .ToList();

            if (dayDishes.Count == 0)
            {
                dayDishes = dishes.Take(Math.Min(4, dishes.Count)).ToList();
            }

            var items = new List<MenuItem>();
            foreach (var dish in dayDishes)
            {
                var item = new MenuItem
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    MenuDayId = day.Id,
                    DishId = dish.Id,
                    SellingPrice = prices.GetValueOrDefault(dish.Name, 9.5m)
                };
                day.Items.Add(item);
                items.Add(item);
            }

            menu.Days.Add(day);
            byDate[date] = items;
        }

        await _db.Set<Menu>().AddAsync(menu, ct);
        await _db.SaveChangesAsync(ct);
        return byDate;
    }

    private async Task<List<Order>> SeedOrdersAsync(
        Guid workspaceId,
        IReadOnlyList<ClientCompany> clients,
        IReadOnlyDictionary<Guid, User> placers,
        IReadOnlyList<User> drivers,
        IReadOnlyDictionary<DateOnly, List<MenuItem>> menuItemsByDate,
        IReadOnlyList<Dish> dishes,
        DateOnly today,
        DateTime now,
        CancellationToken ct)
    {
        var dishById = dishes.ToDictionary(d => d.Id);
        var orders = new List<Order>();
        var specs = BuildOrderSpecs(today, clients.Count);

        for (var i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            var client = clients[i % clients.Count];
            if (!menuItemsByDate.TryGetValue(spec.Date, out var dayItems) || dayItems.Count == 0)
            {
                // Fall back to nearest available menu day.
                dayItems = menuItemsByDate.OrderBy(kv => Math.Abs(kv.Key.DayNumber - spec.Date.DayNumber))
                    .Select(kv => kv.Value)
                    .FirstOrDefault() ?? [];
            }

            if (dayItems.Count == 0)
            {
                continue;
            }

            var selected = dayItems.Take(2 + i % 2).ToList();
            var order = new Order
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                ClientCompanyId = client.Id,
                PlacedByUserId = placers[client.Id].Id,
                TargetDate = spec.Date,
                CreatedAt = DateTime.SpecifyKind(spec.Date.ToDateTime(new TimeOnly(9, 30)).AddHours(-(i % 5)), DateTimeKind.Utc),
                Status = spec.Status,
                DriverId = NeedsDriver(spec.Status) ? drivers[i % drivers.Count].Id : null,
                StockConsumedAt = NeedsConsume(spec.Status)
                    ? DateTime.SpecifyKind(spec.Date.ToDateTime(new TimeOnly(11, 0)), DateTimeKind.Utc)
                    : null
            };

            foreach (var menuItem in selected)
            {
                var qty = 8 + (i % 5) * 2;
                var dishName = dishById.TryGetValue(menuItem.DishId, out var dish) ? dish.Name : "Dish";
                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    OrderId = order.Id,
                    MenuItemId = menuItem.Id,
                    DishId = menuItem.DishId,
                    DishName = dishName,
                    Quantity = qty,
                    UnitPrice = menuItem.SellingPrice,
                    Subtotal = menuItem.SellingPrice * qty
                });
            }

            order.TotalAmount = order.Items.Sum(x => x.Subtotal);
            if (order.CreatedAt > now)
            {
                order.CreatedAt = now.AddHours(-1);
            }

            await _db.Set<Order>().AddAsync(order, ct);
            orders.Add(order);
        }

        await _db.SaveChangesAsync(ct);
        return orders;
    }

    private static List<(DateOnly Date, OrderStatus Status)> BuildOrderSpecs(DateOnly today, int clientCount)
    {
        var specs = new List<(DateOnly, OrderStatus)>();

        // Past week: mostly delivered + a couple cancelled.
        for (var daysAgo = 9; daysAgo >= 1; daysAgo--)
        {
            var date = today.AddDays(-daysAgo);
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            specs.Add((date, OrderStatus.Delivered));
            if (daysAgo is 2 or 6)
            {
                specs.Add((date, OrderStatus.Cancelled));
            }
            else if (daysAgo % 2 == 0)
            {
                specs.Add((date, OrderStatus.Delivered));
            }
        }

        // Today: mixed open pipeline.
        specs.Add((today, OrderStatus.Pending));
        specs.Add((today, OrderStatus.Confirmed));
        specs.Add((today, OrderStatus.InProduction));
        specs.Add((today, OrderStatus.ReadyForDelivery));
        specs.Add((today, OrderStatus.Delivered));

        // Cap around 18–20.
        while (specs.Count > 20)
        {
            specs.RemoveAt(0);
        }

        while (specs.Count < 15 && clientCount > 0)
        {
            specs.Add((today.AddDays(-3), OrderStatus.Delivered));
        }

        return specs;
    }

    private static bool NeedsDriver(OrderStatus status) =>
        status is OrderStatus.ReadyForDelivery or OrderStatus.Delivered or OrderStatus.InProduction;

    private static bool NeedsConsume(OrderStatus status) =>
        status is OrderStatus.InProduction or OrderStatus.ReadyForDelivery or OrderStatus.Delivered;

    private async Task SeedConsumeAndAdjustmentsAsync(
        Guid workspaceId,
        IReadOnlyList<Order> orders,
        IReadOnlyList<Dish> dishes,
        Dictionary<string, Ingredient> ingredients,
        DateTime now,
        CancellationToken ct)
    {
        var dishMap = dishes.ToDictionary(d => d.Id);
        var unitCost = ingredients.ToDictionary(
            kv => kv.Value.Id,
            kv => kv.Value.CostPerUnit ?? 0.05m);

        foreach (var order in orders.Where(o => o.StockConsumedAt is not null))
        {
            foreach (var item in order.Items)
            {
                if (item.DishId is not Guid dishId || !dishMap.TryGetValue(dishId, out var dish))
                {
                    continue;
                }

                foreach (var line in dish.Ingredients)
                {
                    var qty = line.Quantity * item.Quantity;
                    var cost = Math.Round(qty * unitCost.GetValueOrDefault(line.IngredientId, 0.05m), 2);
                    await _db.Set<InventoryMovement>().AddAsync(
                        new InventoryMovement
                        {
                            Id = Guid.NewGuid(),
                            WorkspaceId = workspaceId,
                            IngredientId = line.IngredientId,
                            Type = InventoryMovementType.Consume,
                            Quantity = qty,
                            SignedQuantity = -qty,
                            TotalCost = cost,
                            Source = "Kitchen production",
                            Reason = "OrderProduction",
                            CreatedAt = order.StockConsumedAt ?? now
                        },
                        ct);
                }
            }
        }

        // Minor waste / spoilage adjustments.
        var adjustTargets = ingredients.Values.Take(2).ToList();
        for (var i = 0; i < adjustTargets.Count; i++)
        {
            var ingredient = adjustTargets[i];
            var qty = ingredient.BaseUnit == UnitOfMeasure.Piece ? 2m : 350m + i * 100m;
            var cost = Math.Round(qty * (ingredient.CostPerUnit ?? 0.05m), 2);
            await _db.Set<InventoryMovement>().AddAsync(
                new InventoryMovement
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    IngredientId = ingredient.Id,
                    Type = InventoryMovementType.Adjustment,
                    Quantity = qty,
                    SignedQuantity = -qty,
                    TotalCost = cost,
                    Source = "Warehouse",
                    Reason = i == 0 ? "Spoilage" : "Prep waste",
                    CreatedAt = now.AddDays(-2).AddHours(16)
                },
                ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedReviewsAsync(
        Guid workspaceId,
        IReadOnlyList<Order> orders,
        IReadOnlyDictionary<Guid, User> placers,
        DateOnly today,
        DateTime now,
        CancellationToken ct)
    {
        var delivered = orders
            .Where(o => o.Status == OrderStatus.Delivered && o.TargetDate < today)
            .ToList();

        var negativeComments = new[]
        {
            "Cold soup",
            "Missing sauce",
            "Portion was too small",
            "Overcooked pasta",
            "Late delivery and lukewarm meal"
        };

        var positiveComments = new[]
        {
            "Great taste",
            "Fresh and filling",
            "Would order again",
            "Perfect for the office",
            null
        };

        var reviewIndex = 0;
        foreach (var order in delivered)
        {
            foreach (var item in order.Items.Take(1))
            {
                // ~20% reclamations (rating <= 3).
                var isLow = reviewIndex % 5 == 0;
                var rating = isLow
                    ? 1 + reviewIndex % 3
                    : 4 + reviewIndex % 2;

                await _db.Set<MealReview>().AddAsync(
                    new MealReview
                    {
                        Id = Guid.NewGuid(),
                        WorkspaceId = workspaceId,
                        ClientCompanyId = order.ClientCompanyId,
                        EmployeeId = placers[order.ClientCompanyId].Id,
                        TargetDate = order.TargetDate,
                        MenuItemId = item.MenuItemId,
                        Rating = rating,
                        Comment = isLow
                            ? negativeComments[reviewIndex % negativeComments.Length]
                            : positiveComments[reviewIndex % positiveComments.Length],
                        PhotoUrl = null,
                        IsReclamation = rating <= 3,
                        CreatedAt = DateTime.SpecifyKind(
                            order.TargetDate.ToDateTime(new TimeOnly(14, 30)).AddMinutes(reviewIndex * 7),
                            DateTimeKind.Utc)
                    },
                    ct);

                reviewIndex++;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
