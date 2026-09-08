using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.BusinessRules;
using SmartDesk.Domain.Common;
using SmartDesk.Domain.Entities;
using SmartDesk.Domain.Enums;

namespace SmartDesk.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds demo data. Idempotent: it seeds only what is missing, so it is safe
/// to run on every start.
///
/// Demo account passwords come from the SEED_PASSWORD environment variable. The documented default
/// is only for local development and is stated in the README, so no real credential is committed.
/// </summary>
public sealed class DbSeeder(
    AppDbContext db,
    IPasswordHasher hasher,
    IConfiguration config,
    ILogger<DbSeeder> logger)
{
    private const string DefaultDevPassword = "Password123!";

    public async Task MigrateAndSeedAsync(CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        await SeedAsync(ct);
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var password = config["SEED_PASSWORD"] ?? DefaultDevPassword;

        var roles = await SeedRolesAsync(ct);
        var categories = await SeedCategoriesAsync(now, ct);
        var users = await SeedUsersAsync(roles, hasher.Hash(password), now, ct);
        await SeedSkillsAsync(users, categories, now, ct);
        await SeedArticlesAsync(categories, users, now, ct);
        await SeedTicketsAsync(users, categories, now, ct);

        logger.LogInformation("Database seeded.");
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken ct)
    {
        var existing = await db.Roles.ToDictionaryAsync(r => r.Name, ct);
        var descriptions = new Dictionary<string, string>
        {
            [RoleNames.Employee] = "Raises tickets and tracks their own requests.",
            [RoleNames.SupportAgent] = "Works on tickets assigned to them.",
            [RoleNames.SupportManager] = "Assigns work, reviews AI recommendations and approves high-impact actions.",
            [RoleNames.Admin] = "Manages users, categories and the knowledge base."
        };

        foreach (var name in RoleNames.All.Where(n => !existing.ContainsKey(n)))
        {
            var role = new Role { Name = name, Description = descriptions[name] };
            db.Roles.Add(role);
            existing[name] = role;
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    private async Task<Dictionary<string, TicketCategory>> SeedCategoriesAsync(DateTime now, CancellationToken ct)
    {
        var existing = await db.TicketCategories.ToDictionaryAsync(c => c.Name, ct);
        var seed = new (string Name, string Description, int Sla)[]
        {
            ("Network", "Connectivity, VPN, Wi-Fi and firewall issues.", 8),
            ("Hardware", "Laptops, monitors, printers and peripherals.", 24),
            ("Software", "Application installation, licensing and errors.", 16),
            ("Account & Access", "Passwords, permissions and account lockouts.", 4),
            ("Email", "Mailbox, calendar and distribution list issues.", 8),
            ("General", "Anything that does not fit another category.", 48)
        };

        foreach (var (name, description, sla) in seed.Where(s => !existing.ContainsKey(s.Name)))
        {
            var category = new TicketCategory
            {
                Name = name, Description = description, DefaultSlaHours = sla,
                IsActive = true, CreatedAt = now, UpdatedAt = now
            };
            db.TicketCategories.Add(category);
            existing[name] = category;
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    private async Task<Dictionary<string, User>> SeedUsersAsync(
        Dictionary<string, Role> roles, string passwordHash, DateTime now, CancellationToken ct)
    {
        var existing = await db.Users.Include(u => u.Role).ToDictionaryAsync(u => u.Email, ct);
        var seed = new (string Email, string Name, string Role, string Dept)[]
        {
            ("admin@smartdesk.local", "Alex Admin", RoleNames.Admin, "IT"),
            ("manager@smartdesk.local", "Morgan Manager", RoleNames.SupportManager, "IT Service Desk"),
            ("agent1@smartdesk.local", "Priya Network", RoleNames.SupportAgent, "IT Service Desk"),
            ("agent2@smartdesk.local", "Sam Hardware", RoleNames.SupportAgent, "IT Service Desk"),
            ("agent3@smartdesk.local", "Riya Software", RoleNames.SupportAgent, "IT Service Desk"),
            ("employee1@smartdesk.local", "Dev Employee", RoleNames.Employee, "Engineering"),
            ("employee2@smartdesk.local", "Fay Finance", RoleNames.Employee, "Finance"),
            ("employee3@smartdesk.local", "Hari Sales", RoleNames.Employee, "Sales")
        };

        foreach (var (email, name, roleName, dept) in seed.Where(s => !existing.ContainsKey(s.Email)))
        {
            var user = new User
            {
                Email = email, FullName = name, Department = dept,
                PasswordHash = passwordHash, RoleId = roles[roleName].Id, Role = roles[roleName],
                IsActive = true, CreatedAt = now, UpdatedAt = now
            };
            db.Users.Add(user);
            existing[email] = user;
        }

        await db.SaveChangesAsync(ct);
        return existing;
    }

    private async Task SeedSkillsAsync(
        Dictionary<string, User> users, Dictionary<string, TicketCategory> categories, DateTime now, CancellationToken ct)
    {
        if (await db.SupportAgentSkills.AnyAsync(ct)) return;

        // Deliberately uneven so the assignment scorer produces a clear, explainable winner in the demo.
        var seed = new (string Email, string Category, int Level)[]
        {
            ("agent1@smartdesk.local", "Network", 5),
            ("agent1@smartdesk.local", "Account & Access", 4),
            ("agent1@smartdesk.local", "Email", 3),
            ("agent2@smartdesk.local", "Hardware", 5),
            ("agent2@smartdesk.local", "Network", 2),
            ("agent2@smartdesk.local", "General", 3),
            ("agent3@smartdesk.local", "Software", 5),
            ("agent3@smartdesk.local", "Email", 4),
            ("agent3@smartdesk.local", "Account & Access", 3),
            ("agent3@smartdesk.local", "General", 4)
        };

        foreach (var (email, category, level) in seed)
        {
            db.SupportAgentSkills.Add(new SupportAgentSkill
            {
                UserId = users[email].Id, CategoryId = categories[category].Id,
                ProficiencyLevel = level, CreatedAt = now, UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedArticlesAsync(
        Dictionary<string, TicketCategory> categories, Dictionary<string, User> users, DateTime now, CancellationToken ct)
    {
        if (await db.KnowledgeArticles.AnyAsync(ct)) return;

        var author = users["admin@smartdesk.local"].Id;
        var seed = new (string Title, string Category, string[] Tags, string Body)[]
        {
            ("Resolving VPN connection failures", "Network", ["vpn", "connection", "remote"],
             "If the VPN client cannot connect, first verify the user's credentials have not expired. Restart the VPN client to clear any stale session. Confirm the device has a working internet connection outside the VPN. If the client reports a certificate error, remove and reinstall the VPN profile. Escalate to the network team if the tunnel establishes but no internal resources are reachable."),

            ("Wi-Fi keeps disconnecting on laptops", "Network", ["wifi", "wireless", "disconnect"],
             "Frequent Wi-Fi drops are usually a driver or power-management problem. Update the wireless adapter driver to the current approved version. In the adapter's power management settings, disable the option that allows the computer to turn off the device to save power. Forget and rejoin the corporate network. If drops continue in one physical area only, raise a site survey request."),

            ("Password reset and account unlock", "Account & Access", ["password", "lockout", "reset"],
             "Accounts lock after five failed sign-in attempts and unlock automatically after thirty minutes. To reset a password immediately, verify the caller's identity using two pieces of information from their HR record, then issue a temporary password that must be changed at next sign-in. Never send a password over email or chat."),

            ("Requesting access to a shared drive", "Account & Access", ["permissions", "access", "shared drive"],
             "Access to a shared drive requires written approval from the data owner. Raise a ticket that names the drive, the folder and the level of access required. Once the owner approves in the ticket, the service desk adds the user to the matching security group. Group membership takes effect after the user signs out and back in."),

            ("Printer not responding", "Hardware", ["printer", "print", "queue"],
             "First confirm the printer is powered on and shows no error on its panel. Clear the print queue on the user's device and restart the print spooler service. Reinstall the printer using the corporate print server path rather than a direct IP connection. If several users are affected, check the print server before touching individual devices."),

            ("Laptop will not power on", "Hardware", ["laptop", "power", "battery"],
             "Connect the laptop to a known-good charger and leave it for fifteen minutes before testing again. Perform a hard reset by holding the power button for thirty seconds with the charger disconnected. If the charging indicator does not light at all, the issue is likely the charger or the battery, and the device should be booked in for hardware replacement."),

            ("Outlook will not sync mail", "Email", ["outlook", "email", "sync"],
             "Check whether webmail works for the same account; if it does, the problem is local to the client. Repair the Outlook profile and rebuild the offline data file. Confirm the mailbox is not over its storage quota, which silently stops synchronisation. If the mailbox is over quota, guide the user through archiving before anything else."),

            ("Mail delivery delays to external recipients", "Email", ["email", "delay", "delivery"],
             "Delivery delays to external domains are usually queueing at the receiving end rather than a local fault. Collect the message ID and the recipient domain, then check the outbound mail queue. If several messages to the same domain are queued, the domain may be rate limiting us and the situation normally clears within an hour."),

            ("Installing approved software", "Software", ["install", "software", "licence"],
             "All software must come from the approved catalogue. Open the company portal and install from there rather than downloading an installer from the web. If the application is not in the catalogue, raise a software request ticket that includes the business justification and the number of licences required."),

            ("Application crashes on startup", "Software", ["crash", "error", "startup"],
             "Reproduce the crash and capture the exact error message. Clear the application's local cache and start it again. If the crash persists, uninstall and reinstall from the company portal. Collect the crash log from the event viewer before escalating to the application owner, as they will ask for it first.")
        };

        foreach (var (title, category, tags, body) in seed)
        {
            db.KnowledgeArticles.Add(new KnowledgeArticle
            {
                Title = title, Body = body, CategoryId = categories[category].Id,
                Tags = tags, IsPublished = true, AuthorUserId = author,
                CreatedAt = now.AddDays(-30), UpdatedAt = now.AddDays(-30)
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SeedTicketsAsync(
        Dictionary<string, User> users, Dictionary<string, TicketCategory> categories, DateTime now, CancellationToken ct)
    {
        if (await db.Tickets.AnyAsync(ct)) return;

        // hoursAgo is chosen per row so the seed contains breached, at-risk and on-track tickets
        // without any post-processing.
        var seed = new (string Title, string Desc, string Category, TicketPriority Priority,
                        TicketStatus Status, string Creator, string? Assignee, int HoursAgo)[]
        {
            ("VPN disconnects every few minutes", "Since this morning my VPN drops roughly every five minutes and I have to reconnect. I cannot stay connected long enough to finish anything.", "Network", TicketPriority.High, TicketStatus.New, "employee1@smartdesk.local", null, 2),
            ("Cannot connect to office Wi-Fi", "My laptop sees the corporate Wi-Fi but fails to authenticate. Other devices connect fine.", "Network", TicketPriority.Medium, TicketStatus.Assigned, "employee2@smartdesk.local", "agent1@smartdesk.local", 6),
            ("Account locked out", "I mistyped my password too many times and I am now locked out completely.", "Account & Access", TicketPriority.High, TicketStatus.Resolved, "employee3@smartdesk.local", "agent1@smartdesk.local", 30),
            ("Need access to the Finance shared drive", "I have moved to the Finance team and need read and write access to the Finance shared drive.", "Account & Access", TicketPriority.Low, TicketStatus.InProgress, "employee2@smartdesk.local", "agent3@smartdesk.local", 20),
            ("Printer on level 3 not working", "The printer next to the level 3 kitchen shows a paper jam error but there is no jam.", "Hardware", TicketPriority.Medium, TicketStatus.InProgress, "employee1@smartdesk.local", "agent2@smartdesk.local", 10),
            ("Laptop will not turn on", "My laptop was fine yesterday. This morning it will not power on at all, no lights, nothing.", "Hardware", TicketPriority.Critical, TicketStatus.Assigned, "employee3@smartdesk.local", "agent2@smartdesk.local", 3),
            ("Second monitor not detected", "My second monitor stopped being detected after the last Windows update.", "Hardware", TicketPriority.Low, TicketStatus.New, "employee1@smartdesk.local", null, 40),
            ("Outlook stuck on 'Trying to connect'", "Outlook has been showing 'Trying to connect' all morning. Webmail works fine.", "Email", TicketPriority.High, TicketStatus.InProgress, "employee2@smartdesk.local", "agent3@smartdesk.local", 9),
            ("Emails to a client are not arriving", "Messages I send to our client's domain are not arriving. No bounce message either.", "Email", TicketPriority.Medium, TicketStatus.OnHold, "employee3@smartdesk.local", "agent3@smartdesk.local", 26),
            ("Design software crashes on launch", "The design application closes immediately after the splash screen. It worked last week.", "Software", TicketPriority.High, TicketStatus.Assigned, "employee1@smartdesk.local", "agent3@smartdesk.local", 5),
            ("Request a licence for the reporting tool", "I need a licence for the reporting tool to prepare the quarterly board pack.", "Software", TicketPriority.Low, TicketStatus.New, "employee2@smartdesk.local", null, 12),
            ("Excel freezes on large files", "Excel becomes unresponsive whenever I open our forecasting workbook.", "Software", TicketPriority.Medium, TicketStatus.Resolved, "employee2@smartdesk.local", "agent3@smartdesk.local", 50),
            ("Whole floor lost network access", "Nobody on level 2 has network access. This is blocking the entire team.", "Network", TicketPriority.Critical, TicketStatus.InProgress, "employee3@smartdesk.local", "agent1@smartdesk.local", 4),
            ("New starter setup for Monday", "We have a new starter joining on Monday who needs a laptop, accounts and access.", "General", TicketPriority.Medium, TicketStatus.Assigned, "employee1@smartdesk.local", "agent2@smartdesk.local", 18),
            ("Meeting room screen has no signal", "The screen in the large meeting room shows no signal from any laptop.", "Hardware", TicketPriority.Medium, TicketStatus.New, "employee3@smartdesk.local", null, 14),
            ("Cannot open shared calendar", "I have been given access to the team calendar but it does not appear in Outlook.", "Email", TicketPriority.Low, TicketStatus.Closed, "employee1@smartdesk.local", "agent3@smartdesk.local", 72),
            ("Slow file transfers to the server", "Copying files to the shared server is far slower than it was last month.", "Network", TicketPriority.Low, TicketStatus.New, "employee2@smartdesk.local", null, 36),
            ("Password expiry warning will not clear", "I changed my password but I still get the expiry warning at every sign-in.", "Account & Access", TicketPriority.Low, TicketStatus.Resolved, "employee3@smartdesk.local", "agent1@smartdesk.local", 60),
            ("Headset microphone not recognised", "My headset plays audio but the microphone is not detected in calls.", "Hardware", TicketPriority.Low, TicketStatus.New, "employee1@smartdesk.local", null, 22),
            ("VPN certificate expired", "The VPN client reports that my certificate has expired and refuses to connect.", "Network", TicketPriority.High, TicketStatus.Assigned, "employee2@smartdesk.local", "agent1@smartdesk.local", 7)
        };

        var number = 1;
        foreach (var s in seed)
        {
            var category = categories[s.Category];
            var createdAt = now.AddHours(-s.HoursAgo);
            var ticket = new Ticket
            {
                TicketNumber = $"TKT-{number++:D6}",
                Title = s.Title,
                Description = s.Desc,
                CategoryId = category.Id,
                Status = s.Status,
                Priority = s.Priority,
                CreatedByUserId = users[s.Creator].Id,
                AssignedToUserId = s.Assignee is null ? null : users[s.Assignee].Id,
                SlaDueAt = SlaCalculator.CalculateDueAt(createdAt, category.DefaultSlaHours, s.Priority),
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            if (s.Status is TicketStatus.Resolved or TicketStatus.Closed)
            {
                ticket.Resolution = "Resolved by the service desk. See the ticket comments for details.";
                ticket.ResolvedAt = createdAt.AddHours(2);
                if (s.Status == TicketStatus.Closed) ticket.ClosedAt = createdAt.AddHours(4);
            }

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync(ct);

            db.TicketHistory.Add(new TicketHistoryEntry
            {
                TicketId = ticket.Id, ChangedByUserId = ticket.CreatedByUserId,
                Field = "Status", NewValue = TicketStatus.New.ToString(),
                Note = "Ticket created", CreatedAt = createdAt
            });

            if (ticket.AssignedToUserId is int assignee)
            {
                db.TicketAssignments.Add(new TicketAssignment
                {
                    TicketId = ticket.Id, AssignedToUserId = assignee,
                    AssignedByUserId = users["manager@smartdesk.local"].Id,
                    Reason = "Initial routing by the service desk manager.",
                    Source = AssignmentSource.Manual, CreatedAt = createdAt.AddMinutes(15)
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
