using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests.Helpers;

//adds users straight into the test database for tests that need someone the seed data doesnt have
public static class TestUsers
{
    //a new participant with whatever date of birth the test needs, gives back their id
    public static int AddParticipant(RaceDayDbContext context, DateOnly dateOfBirth)
    {
        var participantRole = context.Roles.Single(r => r.RoleName == RoleNames.Participant);

        var user = new User
        {
            RoleID = participantRole.RoleID,
            FirstName = "Test",
            LastName = "Participant",
            DateOfBirth = dateOfBirth,

            //random so adding more than one never clashes on the unique email
            Email = $"participant.{Guid.NewGuid():N}@gmail.com",
            PasswordHash = "not-used-in-these-tests"
        };

        context.Users.Add(user);
        context.SaveChanges();

        return user.UserID;
    }
}