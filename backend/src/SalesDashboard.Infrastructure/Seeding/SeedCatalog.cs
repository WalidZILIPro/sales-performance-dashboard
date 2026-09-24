namespace SalesDashboard.Infrastructure.Seeding;

/// <summary>Static reference data for the seed: the product catalogue (RUB) and the sales team.</summary>
internal static class SeedCatalog
{
    /// <param name="Margin">Catalogue margin as a fraction of list price; cost = price * (1 - margin).</param>
    internal sealed record ProductSpec(string Name, decimal Price, decimal Margin);

    internal sealed record CategorySpec(string Name, string Code, ProductSpec[] Products);

    public static readonly CategorySpec[] Categories =
    [
        new("Consumer Drones", "CD",
        [
            new("DJI Mini 4 Pro Fly More Combo", 109990, 0.21m),
            new("DJI Mini 3", 54990, 0.22m),
            new("DJI Air 3 Fly More Combo", 139990, 0.20m),
            new("DJI Air 3S", 129990, 0.20m),
            new("DJI Neo", 24990, 0.24m),
            new("DJI Flip", 44990, 0.23m),
        ]),
        new("Professional Drones", "PD",
        [
            new("DJI Mavic 3 Pro Cine Premium Combo", 419990, 0.22m),
            new("DJI Mavic 3 Classic", 179990, 0.21m),
            new("DJI Mavic 3 Pro", 259990, 0.22m),
            new("DJI Inspire 3", 2999990, 0.20m),
            new("DJI Mavic 3 Enterprise", 339990, 0.23m),
        ]),
        new("FPV", "FV",
        [
            new("DJI Avata 2 Fly More Combo", 84990, 0.21m),
            new("DJI Goggles 3", 44990, 0.20m),
            new("DJI RC Motion 3", 19990, 0.22m),
            new("DJI O4 Air Unit", 17990, 0.24m),
            new("DJI FPV Combo", 99990, 0.20m),
        ]),
        new("Action & Pocket Cameras", "AC",
        [
            new("DJI Osmo Action 4 Adventure Combo", 34990, 0.22m),
            new("DJI Osmo Action 5 Pro", 39990, 0.22m),
            new("DJI Osmo Pocket 3 Creator Combo", 54990, 0.20m),
            new("DJI Osmo 360", 44990, 0.21m),
            new("DJI Osmo Nano", 29990, 0.23m),
            new("DJI Osmo Pocket 3", 44990, 0.20m),
        ]),
        new("Gimbals & Stabilizers", "GS",
        [
            new("DJI RS 4 Combo", 39990, 0.27m),
            new("DJI RS 4 Pro Combo", 54990, 0.26m),
            new("DJI RS 3 Mini", 29990, 0.28m),
            new("DJI Osmo Mobile 6", 13990, 0.30m),
            new("DJI Osmo Mobile SE", 7990, 0.32m),
            new("DJI Ronin 4D", 699990, 0.20m),
        ]),
        new("Enterprise Solutions", "ES",
        [
            new("DJI Matrice 350 RTK", 1099990, 0.24m),
            new("DJI Matrice 30T", 899990, 0.25m),
            new("DJI Mavic 3 Thermal", 429990, 0.24m),
            new("DJI Dock 2", 1290000, 0.26m),
            new("DJI Agras T50", 1590000, 0.22m),
            new("DJI Zenmuse H30T", 649990, 0.25m),
        ]),
        new("Batteries & Chargers", "BC",
        [
            new("Intelligent Flight Battery (Mini 4 Pro)", 6990, 0.40m),
            new("Intelligent Flight Battery (Air 3)", 8990, 0.38m),
            new("Intelligent Flight Battery (Mavic 3)", 14990, 0.36m),
            new("Battery Charging Hub (Mini 4 Pro)", 5990, 0.42m),
            new("Car Charger", 2990, 0.50m),
            new("Power Bank 65W", 7990, 0.34m),
        ]),
        new("Accessories & Parts", "AP",
        [
            new("ND Filters Set", 4990, 0.55m),
            new("Propellers (Mini 4 Pro)", 990, 0.62m),
            new("Carrying Case", 3990, 0.52m),
            new("Landing Gear Extension", 1990, 0.58m),
            new("microSD 256GB", 3490, 0.30m),
            new("DJI Mic 2 Kit", 21990, 0.26m),
            new("Neck Strap", 1290, 0.60m),
            new("Propeller Guard", 1790, 0.56m),
        ]),
    ];

    public static readonly string[] ProCategories = ["Professional Drones", "Enterprise Solutions"];

    public static readonly string[] AccessoryCategories = ["Batteries & Chargers", "Accessories & Parts"];

    public static readonly string[] AvatarColors =
    [
        "#4F46E5", "#0EA5E9", "#10B981", "#F59E0B", "#EF4444",
        "#8B5CF6", "#EC4899", "#14B8A6", "#F97316", "#6366F1",
    ];

    /// <param name="Skill">Overall sales volume multiplier (1.0 = average).</param>
    /// <param name="Ticket">Deal size multiplier: more lines and higher quantities per sale.</param>
    /// <param name="Discount">Base discount off list price: a high value squeezes margin.</param>
    /// <param name="StartDaysAgo">Days ago the manager started (large = whole period).</param>
    /// <param name="EndDaysAgo">Days ago the manager left; 0 = still working.</param>
    /// <param name="Leaves">(daysAgoWhenLeaveStarted, lengthInDays) windows with no sales at all.</param>
    internal sealed record ManagerSpec(
        string FirstName,
        string LastName,
        string Team,
        string Title,
        double Skill,
        double Ticket,
        double Discount,
        double CancelRate,
        double RefundRate,
        int StartDaysAgo = 1000,
        int EndDaysAgo = 0,
        (int DaysAgo, int Length)[]? Leaves = null);

    public static readonly ManagerSpec[] Managers =
    [
        new("Алексей", "Смирнов", "Enterprise", "Head of Sales", 1.9, 1.6, 0.02, 0.04, 0.02),
        new("Мария", "Иванова", "Enterprise", "Senior Sales Manager", 1.6, 1.4, 0.03, 0.05, 0.03, Leaves: [(120, 21)]),
        new("Дмитрий", "Кузнецов", "Enterprise", "Senior Sales Manager", 1.3, 1.5, 0.05, 0.06, 0.04, Leaves: [(75, 28), (200, 14)]),
        new("Екатерина", "Попова", "Enterprise", "Sales Manager", 1.0, 1.1, 0.04, 0.05, 0.03),
        new("Сергей", "Соколов", "Enterprise", "Sales Manager", 0.7, 1.0, 0.13, 0.08, 0.05),
        new("Анна", "Лебедева", "SMB", "Senior Sales Manager", 1.5, 0.8, 0.03, 0.05, 0.03),
        new("Николай", "Козлов", "SMB", "Sales Manager", 1.2, 0.7, 0.04, 0.06, 0.03, Leaves: [(45, 18)]),
        new("Ольга", "Новикова", "SMB", "Sales Manager", 1.0, 0.8, 0.05, 0.05, 0.04),
        new("Игорь", "Морозов", "SMB", "Sales Manager", 0.8, 0.7, 0.07, 0.14, 0.05),
        new("Татьяна", "Петрова", "SMB", "Junior Sales Manager", 0.6, 0.6, 0.06, 0.07, 0.04, Leaves: [(18, 12)]),
        new("Андрей", "Волков", "E-commerce", "Senior Sales Manager", 1.7, 0.5, 0.03, 0.05, 0.05),
        new("Юлия", "Соловьёва", "E-commerce", "Sales Manager", 1.4, 0.5, 0.04, 0.06, 0.04),
        new("Павел", "Васильев", "E-commerce", "Sales Manager", 1.1, 0.6, 0.05, 0.05, 0.05, Leaves: [(160, 30)]),
        new("Наталья", "Зайцева", "E-commerce", "Sales Manager", 0.9, 0.5, 0.06, 0.06, 0.11),
        new("Владимир", "Павлов", "E-commerce", "Junior Sales Manager", 0.5, 0.5, 0.05, 0.07, 0.05),
        new("Елена", "Семёнова", "Retail Partners", "Senior Sales Manager", 1.2, 1.0, 0.06, 0.05, 0.03),
        new("Артём", "Голубев", "Retail Partners", "Sales Manager", 0.9, 1.1, 0.07, 0.06, 0.04),
        new("Ирина", "Виноградова", "Retail Partners", "Sales Manager", 0.7, 0.9, 0.05, 0.05, 0.04, Leaves: [(30, 10)]),
        new("Максим", "Богданов", "SMB", "Sales Manager", 0.9, 0.7, 0.05, 0.06, 0.03, StartDaysAgo: 100),
        new("Виктория", "Воробьёва", "E-commerce", "Sales Manager", 1.0, 0.6, 0.04, 0.05, 0.04, EndDaysAgo: 150),
    ];

    public static readonly string[] MaleFirstNames =
    [
        "Иван", "Пётр", "Олег", "Роман", "Кирилл", "Виктор", "Антон", "Евгений", "Денис", "Михаил",
        "Станислав", "Григорий", "Леонид", "Тимур", "Борис",
    ];

    public static readonly string[] MaleLastNames =
    [
        "Фёдоров", "Михайлов", "Беляев", "Тарасов", "Белов", "Комаров", "Орлов", "Киселёв", "Макаров",
        "Андреев", "Ковалёв", "Ильин", "Гусев", "Титов", "Кузьмин",
    ];

    public static readonly string[] FemaleFirstNames =
    [
        "Светлана", "Марина", "Людмила", "Дарья", "Алина", "Полина", "Оксана", "Валентина", "Галина", "Елизавета",
        "Вера", "Надежда", "Лариса", "Кристина", "Алёна",
    ];

    public static readonly string[] FemaleLastNames =
    [
        "Фёдорова", "Михайлова", "Беляева", "Тарасова", "Белова", "Комарова", "Орлова", "Киселёва", "Макарова",
        "Андреева", "Ковалёва", "Ильина", "Гусева", "Титова", "Кузьмина",
    ];

    public static readonly string[] CompanyPrefixes =
    [
        "Аэро", "Гео", "Скай", "Вектор", "Альфа", "Регион", "Трек", "Ново", "Про", "Меридиан",
        "Орбита", "Горизонт", "Стратос", "Вертикаль", "Зенит",
    ];

    public static readonly string[] CompanySuffixes =
    [
        "Съёмка", "Сервис", "Медиа", "Строй", "Агро", "Лаб", "Групп", "Логистик", "Техно", "Инвест",
    ];
}
