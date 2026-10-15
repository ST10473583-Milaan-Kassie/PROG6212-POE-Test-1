using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Tests.Helpers;

//requests that pass every check, each test changes just the one thing its testing
public static class TestRequests
{
    //a new participant, 26 on the test date, with an email no seed user has
    public static RegisterRequest ValidRegisterRequest()
    {
        return new RegisterRequest
        {
            FirstName = "Isaac",
            LastName = "Laforeze",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Email = "isaac.forgemaster@gmail.com",
            Password = "Password123",
            RoleName = RoleNames.Participant
        };
    }
}