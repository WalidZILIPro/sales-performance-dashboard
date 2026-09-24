using SalesDashboard.Domain.Entities;
using SalesDashboard.Domain.Enums;
using static SalesDashboard.Infrastructure.Seeding.SeedCatalog;

namespace SalesDashboard.Infrastructure.Seeding;

public sealed record SeedDataSet(
    IReadOnlyList<Category> Categories,
    IReadOnlyList<Product> Products,
    IReadOnlyList<Manager> Managers,
    IReadOnlyList<Customer> Customers,
    IReadOnlyList<Sale> Sales);

/// <summary>
/// Builds a realistic, uneven data set in memory. It is a pure function of (seed, now): the same
/// inputs always give the same data, so a recreated database is identical. Dates are relative to
/// <c>now</c> so the dashboard always shows recent activity, including "today".
///
/// Deliberate unevenness: strong and weak managers, different ticket sizes and discount habits (so
/// different margins), Q4 / March seasonality, weekday rhythm, cancellations and refunds that differ
/// per manager, rare very large B2B deals, a new hire, a leaver and leave windows with no sales.
/// </summary>
public sealed class SeedDataGenerator(int seed = 20240611)
{
    private const int HistoryDays = 365;
    private const int CustomerCount = 80;
    private const double BaseSalesPerManagerDay = 0.72;
    private const double WhaleProbability = 0.012;

    // Calendar-month demand factors, index 0 = January.
    private static readonly double[] Seasonality =
        [0.85, 0.90, 1.05, 1.00, 1.05, 0.95, 0.80, 0.80, 1.05, 1.15, 1.40, 1.50];

    // Day-of-week factors, index = (int)DayOfWeek, Sunday first.
    private static readonly double[] Weekday = [0.20, 1.10, 1.15, 1.10, 1.10, 1.00, 0.30];

    public SeedDataSet Generate(DateTimeOffset now)
    {
        var rng = new Random(seed);
        var utcNow = now.ToUniversalTime();
        var today = DateOnly.FromDateTime(utcNow.UtcDateTime);

        var categories = Categories.Select(c => new Category(c.Name)).ToList();
        var products = BuildProducts(categories);
        var managers = BuildManagers();
        var customers = BuildCustomers(rng);

        var popularity = BuildPopularity(rng, products);
        var customerWeights = BuildCustomerWeights(rng, customers.Count);
        var sales = BuildSales(rng, utcNow, today, managers, customers, products, popularity, customerWeights);

        return new SeedDataSet(categories, products, managers, customers, sales);
    }

    private static List<Product> BuildProducts(List<Category> categories)
    {
        var products = new List<Product>();
        for (var c = 0; c < Categories.Length; c++)
        {
            var spec = Categories[c];
            for (var p = 0; p < spec.Products.Length; p++)
            {
                var item = spec.Products[p];
                var cost = Math.Round(item.Price * (1m - item.Margin), 0);
                products.Add(new Product($"DJI-{spec.Code}-{p + 1:D3}", item.Name, categories[c], item.Price, cost));
            }
        }

        return products;
    }

    private static List<Manager> BuildManagers() =>
        Managers
            .Select((m, i) => new Manager(
                m.FirstName,
                m.LastName,
                m.Team,
                m.Title,
                AvatarColors[i % AvatarColors.Length],
                isActive: m.EndDaysAgo == 0))
            .ToList();

    private static List<Customer> BuildCustomers(Random rng)
    {
        var companies = CompanyPrefixes
            .SelectMany(prefix => CompanySuffixes.Select(suffix => $"ООО «{prefix}{suffix}»"))
            .OrderBy(_ => rng.Next())
            .Take(CustomerCount)
            .ToList();

        var customers = new List<Customer>(CustomerCount);
        for (var i = 0; i < companies.Count; i++)
        {
            var female = rng.NextDouble() < 0.4;
            var name = female
                ? $"{Pick(rng, FemaleFirstNames)} {Pick(rng, FemaleLastNames)}"
                : $"{Pick(rng, MaleFirstNames)} {Pick(rng, MaleLastNames)}";

            var roll = rng.NextDouble();
            var segment = roll switch
            {
                < 0.08 => CustomerSegment.Enterprise,
                < 0.28 => CustomerSegment.MidMarket,
                < 0.75 => CustomerSegment.Smb,
                _ => CustomerSegment.Reseller,
            };

            customers.Add(new Customer(name, companies[i], segment));
        }

        return customers;
    }

    // Zipf-like popularity so a few products dominate and there is a long tail.
    private static double[] BuildPopularity(Random rng, List<Product> products)
    {
        var order = Enumerable.Range(0, products.Count).OrderBy(_ => rng.Next()).ToArray();
        var weights = new double[products.Count];
        for (var rank = 0; rank < order.Length; rank++)
        {
            weights[order[rank]] = 1.0 / Math.Pow(rank + 1, 0.7);
        }

        return weights;
    }

    // Pareto-ish: a few customers buy very often, most buy rarely.
    private static double[] BuildCustomerWeights(Random rng, int count)
    {
        var order = Enumerable.Range(0, count).OrderBy(_ => rng.Next()).ToArray();
        var weights = new double[count];
        for (var rank = 0; rank < count; rank++)
        {
            weights[order[rank]] = 1.0 / Math.Pow(rank + 1, 0.8);
        }

        return weights;
    }

    private static List<Sale> BuildSales(
        Random rng,
        DateTimeOffset now,
        DateOnly today,
        List<Manager> managers,
        List<Customer> customers,
        List<Product> products,
        double[] popularity,
        double[] customerWeights)
    {
        var sales = new List<Sale>();
        var proProducts = products.Where(p => ProCategories.Contains(p.Category.Name)).ToList();

        for (var daysAgo = HistoryDays - 1; daysAgo >= 0; daysAgo--)
        {
            var day = today.AddDays(-daysAgo);
            var dayFactor = Seasonality[day.Month - 1] * Weekday[(int)day.DayOfWeek];

            for (var m = 0; m < managers.Count; m++)
            {
                var spec = Managers[m];
                if (!IsWorking(spec, daysAgo))
                {
                    continue;
                }

                var count = Poisson(rng, BaseSalesPerManagerDay * spec.Skill * dayFactor);
                for (var n = 0; n < count; n++)
                {
                    var soldAt = RandomTimeOnDay(rng, day);
                    if (soldAt > now)
                    {
                        continue; // "later today" has not happened yet
                    }

                    var customer = PickCustomer(rng, customers, customerWeights, spec.Team);
                    var lines = BuildLines(rng, spec, customer, products, proProducts, popularity);
                    var status = PickStatus(rng, spec);
                    sales.Add(Sale.Create(managers[m], customer, soldAt, status, lines));
                }
            }
        }

        // Insert in chronological order so ids grow with time. OrderBy is stable, so the result is deterministic.
        return sales.OrderBy(s => s.SoldAt).ToList();
    }

    private static bool IsWorking(ManagerSpec spec, int daysAgo)
    {
        if (daysAgo > spec.StartDaysAgo || daysAgo < spec.EndDaysAgo)
        {
            return false;
        }

        if (spec.Leaves is null)
        {
            return true;
        }

        foreach (var (leaveStartDaysAgo, length) in spec.Leaves)
        {
            if (daysAgo <= leaveStartDaysAgo && daysAgo > leaveStartDaysAgo - length)
            {
                return false;
            }
        }

        return true;
    }

    private static DateTimeOffset RandomTimeOnDay(Random rng, DateOnly day)
    {
        var hour = 9 + rng.Next(12); // 09:00-20:59
        return new DateTimeOffset(day.Year, day.Month, day.Day, hour, rng.Next(60), rng.Next(60), TimeSpan.Zero);
    }

    private static Customer PickCustomer(Random rng, List<Customer> customers, double[] baseWeights, string team)
    {
        // Each team leans towards the segments it serves.
        var total = 0.0;
        var weights = new double[customers.Count];
        for (var i = 0; i < customers.Count; i++)
        {
            weights[i] = baseWeights[i] * TeamAffinity(team, customers[i].Segment);
            total += weights[i];
        }

        var roll = rng.NextDouble() * total;
        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
            {
                return customers[i];
            }
        }

        return customers[^1];
    }

    private static double TeamAffinity(string team, CustomerSegment segment) => (team, segment) switch
    {
        ("Enterprise", CustomerSegment.Enterprise) => 5.0,
        ("Enterprise", CustomerSegment.MidMarket) => 2.5,
        ("SMB", CustomerSegment.Smb) => 3.0,
        ("SMB", CustomerSegment.MidMarket) => 1.5,
        ("E-commerce", CustomerSegment.Smb) => 1.5,
        ("Retail Partners", CustomerSegment.Reseller) => 5.0,
        _ => 1.0,
    };

    private static List<SaleLine> BuildLines(
        Random rng,
        ManagerSpec spec,
        Customer customer,
        List<Product> products,
        List<Product> proProducts,
        double[] popularity)
    {
        if (rng.NextDouble() < WhaleProbability)
        {
            return [BuildWhaleLine(rng, spec, customer, proProducts)];
        }

        var lineCount = 1;
        if (rng.NextDouble() < 0.35 * spec.Ticket)
        {
            lineCount++;
        }

        if (rng.NextDouble() < 0.15 * spec.Ticket)
        {
            lineCount++;
        }

        if (rng.NextDouble() < 0.05 * spec.Ticket)
        {
            lineCount++;
        }

        var lines = new List<SaleLine>(lineCount);
        var used = new HashSet<int>();
        for (var i = 0; i < lineCount; i++)
        {
            var index = PickProduct(rng, products, popularity, customer.Segment);
            if (!used.Add(index))
            {
                continue; // no duplicate product lines in one sale
            }

            var product = products[index];
            var quantity = PickQuantity(rng, spec, customer.Segment, product.ListPrice);
            lines.Add(ToLine(rng, spec, customer.Segment, product, quantity));
        }

        return lines;
    }

    private static SaleLine BuildWhaleLine(Random rng, ManagerSpec spec, Customer customer, List<Product> proProducts)
    {
        var product = proProducts[rng.Next(proProducts.Count)];
        var quantity = product.ListPrice > 1_000_000 ? rng.Next(2, 6) : rng.Next(5, 21);
        return ToLine(rng, spec, customer.Segment, product, quantity, extraDiscount: 0.04);
    }

    private static int PickProduct(Random rng, List<Product> products, double[] popularity, CustomerSegment segment)
    {
        var total = 0.0;
        var weights = new double[products.Count];
        for (var i = 0; i < products.Count; i++)
        {
            weights[i] = popularity[i] * ProductAffinity(segment, products[i].Category.Name);
            total += weights[i];
        }

        var roll = rng.NextDouble() * total;
        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0)
            {
                return i;
            }
        }

        return products.Count - 1;
    }

    private static double ProductAffinity(CustomerSegment segment, string category)
    {
        var isPro = ProCategories.Contains(category);
        var isAccessory = AccessoryCategories.Contains(category);

        return segment switch
        {
            CustomerSegment.Enterprise => isPro ? 4.0 : 1.0,
            CustomerSegment.MidMarket => isPro ? 2.0 : 1.0,
            CustomerSegment.Reseller => isAccessory ? 3.0 : isPro ? 0.3 : 1.5,
            _ => isPro ? 0.4 : 1.0,
        };
    }

    private static int PickQuantity(Random rng, ManagerSpec spec, CustomerSegment segment, decimal price)
    {
        var maxQuantity = price switch
        {
            < 10_000m => 8,
            < 60_000m => 4,
            < 300_000m => 2,
            _ => 1,
        };

        var boost = segment switch
        {
            CustomerSegment.Reseller => 2.0,
            CustomerSegment.Enterprise => 1.5,
            _ => 1.0,
        };

        var scaled = maxQuantity * spec.Ticket * boost;
        return 1 + (int)(Math.Pow(rng.NextDouble(), 3) * scaled);
    }

    private static SaleLine ToLine(
        Random rng,
        ManagerSpec spec,
        CustomerSegment segment,
        Product product,
        int quantity,
        double extraDiscount = 0)
    {
        var segmentDiscount = segment switch
        {
            CustomerSegment.Reseller => 0.06,
            CustomerSegment.Enterprise => 0.03,
            _ => 0.0,
        };

        var discount = Math.Clamp(spec.Discount + segmentDiscount + extraDiscount + (rng.NextDouble() * 0.05), 0, 0.30);
        var cost = Math.Round(product.UnitCost * (decimal)(1 + ((rng.NextDouble() - 0.5) * 0.06)), 0);

        // Price is rounded to 10 RUB and floored slightly under cost, so deep discounts on thin-margin
        // drones can produce the occasional loss-making line but never an absurd one.
        var price = Math.Round(product.ListPrice * (decimal)(1 - discount) / 10m, 0) * 10m;
        price = Math.Max(price, Math.Round(cost * 0.97m, 0));

        return new SaleLine(product, quantity, price, cost);
    }

    private static SaleStatus PickStatus(Random rng, ManagerSpec spec)
    {
        var roll = rng.NextDouble();
        if (roll < spec.CancelRate)
        {
            return SaleStatus.Cancelled;
        }

        return roll < spec.CancelRate + spec.RefundRate ? SaleStatus.Refunded : SaleStatus.Paid;
    }

    // Knuth's algorithm; lambda is always small here (< ~4).
    private static int Poisson(Random rng, double lambda)
    {
        if (lambda <= 0)
        {
            return 0;
        }

        var limit = Math.Exp(-lambda);
        var product = 1.0;
        var k = 0;
        do
        {
            k++;
            product *= rng.NextDouble();
        }
        while (product > limit);

        return k - 1;
    }

    private static T Pick<T>(Random rng, T[] items) => items[rng.Next(items.Length)];
}
