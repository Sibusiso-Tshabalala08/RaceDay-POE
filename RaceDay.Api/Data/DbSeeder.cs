using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Seeds the same sample data as Part 1's SQL script (Section C), so the
/// Code-First schema and its contents stay consistent with the approved plan.
/// Runs once on startup; does nothing if Users already has rows.
/// </summary>
public static class DbSeeder
{
    public static void Seed(RaceDayDbContext context)
    {
        if (context.Users.Any()) return; // already seeded

        var organiser1 = new User
        {
            FullName = "Thabo Nkosi",
            Email = "thabo.nkosi@raceday.co.za",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            RoleId = 1
        };
        var organiser2 = new User
        {
            FullName = "Lindiwe Dlamini",
            Email = "lindiwe.dlamini@raceday.co.za",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            RoleId = 1
        };
        var participant1 = new User
        {
            FullName = "Sarah van der Merwe",
            Email = "sarah.vdm@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            RoleId = 2
        };
        var participant2 = new User
        {
            FullName = "Ahmed Patel",
            Email = "ahmed.patel@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            RoleId = 2
        };

        context.Users.AddRange(organiser1, organiser2, participant1, participant2);
        context.SaveChanges();

        var sowetoMarathon = new Event
        {
            OrganiserId = organiser1.UserId,
            EventName = "Soweto Marathon",
            EventDate = new DateTime(2026, 11, 8, 6, 0, 0, DateTimeKind.Utc),
            Location = "Soweto, Johannesburg",
            Latitude = -26.2485m,
            Longitude = 27.8540m,
            Distance = 42.20m,
            EventType = "Run",
            Description = "Annual road marathon through the streets of Soweto."
        };
        var cycleTour = new Event
        {
            OrganiserId = organiser1.UserId,
            EventName = "Cape Town Cycle Tour",
            EventDate = new DateTime(2026, 3, 8, 6, 30, 0, DateTimeKind.Utc),
            Location = "Cape Town City Centre",
            Latitude = -33.9249m,
            Longitude = 18.4241m,
            Distance = 109.00m,
            EventType = "Cycle",
            Description = "Iconic cycling race around the Cape Peninsula."
        };
        var parkWalk = new Event
        {
            OrganiserId = organiser2.UserId,
            EventName = "Park Walk & Wellness Day",
            EventDate = new DateTime(2026, 9, 20, 8, 0, 0, DateTimeKind.Utc),
            Location = "Zoo Lake, Johannesburg",
            Latitude = -26.1636m,
            Longitude = 28.0384m,
            Distance = 5.00m,
            EventType = "Walk",
            Description = "Community fun walk promoting health and wellness."
        };

        context.Events.AddRange(sowetoMarathon, cycleTour, parkWalk);
        context.SaveChanges();

        var cat10km = new Category { EventId = sowetoMarathon.EventId, CategoryName = "10km", CategoryType = "Distance", Distance = 10.00m, MaxParticipants = 500, EntryFee = 150.00m };
        var cat21km = new Category { EventId = sowetoMarathon.EventId, CategoryName = "21km", CategoryType = "Distance", Distance = 21.10m, MaxParticipants = 500, EntryFee = 250.00m };
        var catUnder20 = new Category { EventId = sowetoMarathon.EventId, CategoryName = "Under 20", CategoryType = "Age", MinAge = 15, MaxAge = 19, MaxParticipants = 100, EntryFee = 100.00m };
        var cat109km = new Category { EventId = cycleTour.EventId, CategoryName = "109km", CategoryType = "Distance", Distance = 109.00m, MaxParticipants = 1000, EntryFee = 350.00m };
        var catSenior = new Category { EventId = cycleTour.EventId, CategoryName = "Senior", CategoryType = "Age", MinAge = 50, MaxAge = 99, MaxParticipants = 200, EntryFee = 300.00m };
        var cat5km = new Category { EventId = parkWalk.EventId, CategoryName = "5km", CategoryType = "Distance", Distance = 5.00m, MaxParticipants = 300, EntryFee = 50.00m };

        context.Categories.AddRange(cat10km, cat21km, catUnder20, cat109km, catSenior, cat5km);
        context.SaveChanges();

        var enr1 = new Enrolment { ParticipantId = participant1.UserId, CategoryId = cat10km.CategoryId, Status = "Confirmed" };
        var enr2 = new Enrolment { ParticipantId = participant1.UserId, CategoryId = cat109km.CategoryId, Status = "Confirmed" };
        var enr3 = new Enrolment { ParticipantId = participant2.UserId, CategoryId = cat21km.CategoryId, Status = "Confirmed" };
        var enr4 = new Enrolment { ParticipantId = participant2.UserId, CategoryId = cat5km.CategoryId, Status = "Confirmed" };

        context.Enrolments.AddRange(enr1, enr2, enr3, enr4);
        context.SaveChanges();

        context.Results.AddRange(
            new Result { EnrolmentId = enr1.EnrolmentId, FinishTime = new TimeSpan(0, 52, 14), Position = 12, Status = "Finished" },
            new Result { EnrolmentId = enr3.EnrolmentId, Status = "DNF" },
            new Result { EnrolmentId = enr4.EnrolmentId, FinishTime = new TimeSpan(0, 28, 3), Position = 5, Status = "Finished" }
        );
        context.SaveChanges();
    }
}
