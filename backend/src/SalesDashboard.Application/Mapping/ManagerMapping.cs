using SalesDashboard.Application.Contracts.Responses;
using SalesDashboard.Application.Persistence.Models;

namespace SalesDashboard.Application.Mapping;

internal static class ManagerMapping
{
    public static ManagerRefDto ToRef(this ManagerInfo manager) =>
        new(manager.Id, manager.FullName, manager.Initials, manager.AvatarColor, manager.Team, manager.Title);
}
