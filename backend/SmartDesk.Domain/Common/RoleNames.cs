namespace SmartDesk.Domain.Common;

/// <summary>
/// The four business roles. Kept as constants so [Authorize(Roles = ...)] attributes and the
/// seeded Roles table can never drift apart.
/// </summary>
public static class RoleNames
{
    public const string Employee = "Employee";
    public const string SupportAgent = "SupportAgent";
    public const string SupportManager = "SupportManager";
    public const string Admin = "Admin";

    public const string ManagerOrAdmin = SupportManager + "," + Admin;
    public const string Staff = SupportAgent + "," + SupportManager + "," + Admin;

    public static readonly string[] All = [Employee, SupportAgent, SupportManager, Admin];
}
