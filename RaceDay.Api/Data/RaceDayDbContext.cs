using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------------- Roles ----------------
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.RoleName).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint(
                "CHK_Role_Name", "[RoleName] IN ('Organiser', 'Participant')"));
        });

        // ---------------- Users ----------------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Events ----------------
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.OrganisedEvents)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Distance).HasColumnType("decimal(6,2)");
            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");

            entity.ToTable(t => t.HasCheckConstraint(
                "CHK_Event_Type", "[EventType] IN ('Run', 'Walk', 'Cycle')"));
        });

        // ---------------- Categories ----------------
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(c => c.Distance).HasColumnType("decimal(6,2)");
            entity.Property(c => c.EntryFee).HasColumnType("decimal(8,2)");

            // Same business rule as Part 1's SQL script: a Distance category
            // must have a distance and no ages; an Age category must have
            // both ages and no distance.
            entity.ToTable(t => t.HasCheckConstraint(
                "CHK_Category_TypeFields",
                "([CategoryType] = 'Distance' AND [Distance] IS NOT NULL AND [MinAge] IS NULL AND [MaxAge] IS NULL) " +
                "OR ([CategoryType] = 'Age' AND [Distance] IS NULL AND [MinAge] IS NOT NULL AND [MaxAge] IS NOT NULL)"));
        });

        // ---------------- Enrolments ----------------
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasOne(e => e.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(e => e.ParticipantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Matches Part 1: a participant cannot enrol twice in the same category.
            entity.HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique();
        });

        // ---------------- Results ----------------
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasOne(r => r.Enrolment)
                  .WithOne(e => e.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            // One-to-one: EnrolmentId must be unique on Results.
            entity.HasIndex(r => r.EnrolmentId).IsUnique();
        });

        // ---------------- Seed data: matches Part 1 Section C exactly ----------------
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" }
        );
    }
}
