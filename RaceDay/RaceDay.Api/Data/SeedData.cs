using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

//sample data, same rows as the inserts in the sql script
//ids have to be set by hand for seeding, they match the order the sql inserts them in
public static class SeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        //both roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, RoleName = "Organiser" },
            new Role { RoleID = 2, RoleName = "Participant" }
        );

        //the three event types
        modelBuilder.Entity<EventType>().HasData(
            new EventType { EventTypeID = 1, EventTypeName = "Run" },
            new EventType { EventTypeID = 2, EventTypeName = "Walk" },
            new EventType { EventTypeID = 3, EventTypeName = "Cycle" }
        );

        //users, 2 organisers (roleID 1) and 2 participants (roleID 2)
        //participant DOBs fit the age ranges of the categories they enrol in below
        //same bcrypt hashes as the sql so the same passwords work
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserID = 1,
                RoleID = 1,
                FirstName = "Trevor",
                LastName = "Belmont",
                DateOfBirth = new DateOnly(1985, 3, 21),
                Email = "thelastbelmont@raceday.co.za",
                PhoneNumber = "0821234567",
                PasswordHash = "$2b$11$BkpZnaPwOkrZxW.Nlk2y9e8mCo3fhDDhhi9QGnwNUw3dlUzfyWXpa"
            },
            new User
            {
                UserID = 2,
                RoleID = 1,
                FirstName = "Sypha",
                LastName = "Belnades",
                DateOfBirth = new DateOnly(1990, 8, 9),
                Email = "speakersypha@raceday.co.za",
                PhoneNumber = "0837654321",
                PasswordHash = "$2b$11$U4/77zOepUUoaMQRBiyQI.OvbezrbqS8GkyKW5i6XTqyquPaZr8Ia"
            },
            new User
            {
                UserID = 3,
                RoleID = 2,
                FirstName = "Alucard",
                LastName = "Tepes",
                DateOfBirth = new DateOnly(1998, 5, 2),
                Email = "floatingsword@gmail.com",
                PhoneNumber = "0731122334",
                PasswordHash = "$2b$11$lbEz7U7rWaa0WlM4RN.ZquQgFi0af44pl//TgJnSqr9LnISv40U6i"
            },
            new User
            {
                UserID = 4,
                RoleID = 2,
                FirstName = "Carmilla",
                LastName = "Karnstein",
                DateOfBirth = new DateOnly(1982, 11, 17),
                Email = "queenofstyria@gmail.com",
                PhoneNumber = "0845566778",
                PasswordHash = "$2b$11$JVzq35RGkGJtCSS3vRTVGec3Nb9mDrrkKL81ggI7.P.sT2Z1ib3zC"
            }
        );

        //events, comrades already happened this year, the other two are still upcoming
        //the m on the distance makes it a decimal
        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                EventID = 1,
                EventName = "Comrades Marathon",
                EventTypeID = 1,
                UserID = 1,
                Description = "Iconic ultra marathon between Pietermaritzburg and Durban.",
                EventDate = new DateOnly(2026, 6, 14),
                Location = "Pietermaritzburg to Durban",
                Distance = 87.70m
            },
            new Event
            {
                EventID = 2,
                EventName = "Cape Town Cycle Tour",
                EventTypeID = 3,
                UserID = 2,
                Description = "Scenic cycle race around the Cape Peninsula.",
                EventDate = new DateOnly(2027, 3, 8),
                Location = "Cape Town",
                Distance = 109.00m
            },
            new Event
            {
                EventID = 3,
                EventName = "Durban Beachfront Park Run",
                EventTypeID = 2,
                UserID = 1,
                Description = "Casual weekly walk along the Durban beachfront.",
                EventDate = new DateOnly(2027, 1, 16),
                Location = "Durban",
                Distance = 5.00m
            }
        );

        //categories, a couple per event, mix of age based and open
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryID = 1, EventID = 1, CategoryName = "Senior", MinAge = 20, MaxAge = 39 },
            new Category { CategoryID = 2, EventID = 1, CategoryName = "Veteran", MinAge = 40, MaxAge = 59 },
            new Category { CategoryID = 3, EventID = 2, CategoryName = "Open", MinAge = 18, MaxAge = 49 },
            new Category { CategoryID = 4, EventID = 2, CategoryName = "Masters", MinAge = 50, MaxAge = 65 },
            new Category { CategoryID = 5, EventID = 3, CategoryName = "Junior", MinAge = 10, MaxAge = 17 },
            new Category { CategoryID = 6, EventID = 3, CategoryName = "Open", MinAge = 18, MaxAge = null }
        );

        //enrolments, participants entering the events above
        //eventid and categoryid have to line up since the category fk uses both
        modelBuilder.Entity<Enrolment>().HasData(
            new Enrolment { EnrolmentID = 1, UserID = 3, EventID = 1, CategoryID = 1, Status = "Confirmed" },
            new Enrolment { EnrolmentID = 2, UserID = 4, EventID = 1, CategoryID = 2, Status = "Confirmed" },
            new Enrolment { EnrolmentID = 3, UserID = 3, EventID = 2, CategoryID = 3, Status = "Pending" },
            new Enrolment { EnrolmentID = 4, UserID = 4, EventID = 3, CategoryID = 6, Status = "Confirmed" }
        );

        //results, only for comrades since thats the only event thats actually happened
        modelBuilder.Entity<Result>().HasData(
            new Result { ResultID = 1, EnrolmentID = 1, FinishTime = new TimeOnly(7, 45, 12), FinishingPosition = 4520 },
            new Result { ResultID = 2, EnrolmentID = 2, FinishTime = new TimeOnly(8, 2, 45), FinishingPosition = 6789 }
        );
    }
}