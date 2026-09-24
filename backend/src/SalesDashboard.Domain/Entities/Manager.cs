namespace SalesDashboard.Domain.Entities;

public class Manager
{
    private Manager()
    {
    }

    public Manager(string firstName, string lastName, string team, string title, string avatarColor, bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(team);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(avatarColor);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Team = team.Trim();
        Title = title.Trim();
        AvatarColor = avatarColor.Trim();
        IsActive = isActive;
    }

    public int Id { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    /// <summary>Sales team the manager belongs to (e.g. "Enterprise").</summary>
    public string Team { get; private set; } = null!;

    /// <summary>Job title (e.g. "Senior Sales Manager").</summary>
    public string Title { get; private set; } = null!;

    /// <summary>Hex colour (#RRGGBB) used for the avatar background.</summary>
    public string AvatarColor { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public string Initials => BuildInitials(FirstName, LastName);

    public static string BuildInitials(string firstName, string lastName) =>
        $"{firstName[0]}{lastName[0]}".ToUpperInvariant();
}
