using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Roles lookup table, seeded with the two system roles (same ids as the SQL script)
        b.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
        b.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" });

        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
        b.Entity<User>().HasOne(u => u.Role).WithMany().HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Event>().Property(e => e.EventType).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.Entity<Event>().Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
        b.Entity<Event>().HasOne(e => e.Organiser).WithMany().HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Category>().Property(c => c.Price).HasDefaultValue(0m);
        b.Entity<Category>().Property(c => c.MaxParticipants).HasDefaultValue(0);
        b.Entity<Category>().HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId).OnDelete(DeleteBehavior.Restrict);

        // UQ_Enrolments_Participant_Category
        b.Entity<Enrolment>().HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique();
        b.Entity<Enrolment>().Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");
        b.Entity<Enrolment>().Property(e => e.Status).HasDefaultValue("Confirmed");
        b.Entity<Enrolment>().HasOne(e => e.Participant).WithMany().HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrolment>().HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Results is one-to-one with Enrolments (EnrolmentId is UNIQUE)
        b.Entity<Result>().Property(r => r.Status).HasDefaultValue("Finished");
        b.Entity<Result>().Property(r => r.CapturedAt).HasDefaultValueSql("GETDATE()");
        b.Entity<Result>().HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
