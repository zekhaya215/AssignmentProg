using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AssignmentProg.Models;

namespace AssignmentProg.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Part 1
        public DbSet<Assignment> Assignments { get; set; }

        // Part 2
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Optional: store enum as string
            builder.Entity<ApplicationUser>()
                .Property(u => u.Gender)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<Event>()
                .Property(e => e.DistanceKm)
                .HasPrecision(8, 2);

            // SQL Server rejects models where one delete can reach a row by
            // more than one cascade path (Event -> Enrolment, Event -> Category -> Enrolment,
            // User -> Event, User -> Enrolment). Only the paths we actually want cascade.

            // Deleting an event removes its categories and enrolments
            // (the controller already blocks deleting an event that has enrolments).
            builder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Enrolment>()
                .HasOne(en => en.Event)
                .WithMany(e => e.Enrolments)
                .HasForeignKey(en => en.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Enrolment>()
                .HasOne(en => en.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(en => en.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Enrolment>()
                .HasOne(en => en.Participant)
                .WithMany()
                .HasForeignKey(en => en.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>()
                .HasOne(e => e.Organiser)
                .WithMany()
                .HasForeignKey(e => e.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One result per enrolment
            builder.Entity<Result>()
                .HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // A participant can enter an event only once
            builder.Entity<Enrolment>()
                .HasIndex(en => new { en.EventId, en.ParticipantId })
                .IsUnique();
        }
    }
}
