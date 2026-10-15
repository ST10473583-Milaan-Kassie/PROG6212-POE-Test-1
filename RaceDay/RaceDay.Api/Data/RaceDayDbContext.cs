using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

//the dbcontext is what EF uses to talk to the database
//each dbset is a table, OnModelCreating sets everything up to match the sql script
public class RaceDayDbContext : DbContext
{
    //the options (connection string etc) get passed in from Program.cs
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    //one dbset per table, Set<T>() stops the null warnings
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    //ef normally adds an index to every fk column, the sql script doesnt have any so this turns that off
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //string? columns come out as NULL and string columns come out as NOT NULL on their own

        //roles table
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(r => r.RoleID);

            entity.Property(r => r.RoleName).HasMaxLength(50);

            //no two roles can have the same name
            entity.HasAlternateKey(r => r.RoleName)
                .HasName("UQ_Roles_RoleName");
        });

        //users table
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.UserID);

            entity.Property(u => u.FirstName).HasMaxLength(50);
            entity.Property(u => u.LastName).HasMaxLength(50);
            entity.Property(u => u.DateOfBirth).HasColumnType("date");
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.PasswordHash).HasMaxLength(255);
            entity.Property(u => u.ProfileImageURL).HasMaxLength(255);

            //emails are used to log in, must be unique
            //cant be changed after registering, the profile update only touches name and phone anyway
            entity.HasAlternateKey(u => u.Email)
                .HasName("UQ_Users_Email");

            //each user has one role, a role has many users
            //noaction so nothing cascades, same as the sql
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Users_Roles");
        });

        //eventtype table, singular like the sql
        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("EventType");
            entity.HasKey(et => et.EventTypeID);

            entity.Property(et => et.EventTypeName).HasMaxLength(20);

            //no duplicate event types
            entity.HasAlternateKey(et => et.EventTypeName)
                .HasName("UQ_EventType_EventTypeName");
        });

        //events table
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(ev => ev.EventID);

            entity.Property(ev => ev.EventName).HasMaxLength(100);
            entity.Property(ev => ev.Description).HasMaxLength(500);
            entity.Property(ev => ev.EventDate).HasColumnType("date");
            entity.Property(ev => ev.Location).HasMaxLength(150);
            entity.Property(ev => ev.BannerImageURL).HasMaxLength(255);

            //6 digits total, 2 after the decimal point, so 9999.99 is the max
            entity.Property(ev => ev.Distance).HasPrecision(6, 2);

            //each event has one type, a type has many events
            entity.HasOne(ev => ev.EventType)
                .WithMany(et => et.Events)
                .HasForeignKey(ev => ev.EventTypeID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Events_EventType");

            //each event belongs to the organiser who made it
            entity.HasOne(ev => ev.User)
                .WithMany(u => u.Events)
                .HasForeignKey(ev => ev.UserID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Events_Users");
        });

        //categories table
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", t =>
            {
                //a max age needs a min age, and cant be lower than it
                t.HasCheckConstraint("CK_Categories_AgeRange", "MaxAge IS NULL OR (MinAge IS NOT NULL AND MaxAge >= MinAge)");

                //every category needs an age range, a distance, or both
                t.HasCheckConstraint("CK_Categories_AgeOrDistance", "MinAge IS NOT NULL OR Distance IS NOT NULL");
            });
            entity.HasKey(c => c.CategoryID);

            entity.Property(c => c.CategoryName).HasMaxLength(50);

            //same size as the event distance, 9999.99 is the max
            entity.Property(c => c.Distance).HasPrecision(6, 2);

            //allows EventID and CategoryID to be referenced together by enrolments
            //has to be here since the enrolments fk below cant exist without it
            entity.HasAlternateKey(c => new { c.EventID, c.CategoryID })
                .HasName("UQ_Categories_EventCategory");

            //each category belongs to one event
            entity.HasOne(c => c.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Categories_Events");
        });

        //enrolments table
        modelBuilder.Entity<Enrolment>(entity =>
        {
            //only allows pending or confirmed statuses
            entity.ToTable("Enrolments", t => t.HasCheckConstraint(
                "CK_Enrolments_Status", "Status IN ('Pending', 'Confirmed')"));
            entity.HasKey(e => e.EnrolmentID);

            //pending by default, same as the sql
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            //stops users from entering the same event twice
            entity.HasAlternateKey(e => new { e.UserID, e.EventID })
                .HasName("UQ_Enrolments_UserEvent");

            //the participant enrolling
            entity.HasOne(e => e.User)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolments_Users");

            //the event they're entering
            entity.HasOne(e => e.Event)
                .WithMany(ev => ev.Enrolments)
                .HasForeignKey(e => e.EventID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolments_Events");

            //the category they picked, eventid and categoryid together have to match a category
            //stops someone picking a category that belongs to a different event
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(e => new { e.EventID, e.CategoryID })
                .HasPrincipalKey(c => new { c.EventID, c.CategoryID })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Enrolments_EventCategory");
        });

        //results table
        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(r => r.ResultID);

            entity.Property(r => r.FinishTime).HasColumnType("time");

            //prevents an enrolment from having multiple results
            entity.HasAlternateKey(r => r.EnrolmentID)
                .HasName("UQ_Results_Enrolment");

            //one result per enrolment, withone makes it one to one
            entity.HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Results_Enrolments");
        });

        //adds the sample data, kept in SeedData.cs so this file stays about the structure
        SeedData.Seed(modelBuilder);
    }
}