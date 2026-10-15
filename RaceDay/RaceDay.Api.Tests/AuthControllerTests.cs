using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Helpers;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests;

//register, login and logout, the ones that should work and the ones that should get turned away
//the test date is 15 october 2026 (TestClock.DefaultToday), all the ages below are worked out from that
public class AuthControllerTests
{
    private const string AlucardEmail = "floatingsword@gmail.com";
    private const string TrevorEmail = "thelastbelmont@raceday.co.za";

    //the auth controller with the test clock and a fake session, nobody logged in yet
    private static AuthController CreateController(RaceDayDbContext context)
    {
        var controller = new AuthController(context, TestClock.Default());
        ControllerTestHelper.AttachSession(controller);
        return controller;
    }

    //register

    [Fact]
    public async Task Register_ValidParticipant_Returns201AndSavesUser()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();

        var result = await controller.Register(request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<UserResponse>(created.Value);
        Assert.Equal(request.Email, response.Email);
        Assert.Equal(RoleNames.Participant, response.RoleName);

        //the user is actually in the database now, under the participant role
        var savedUser = context.Users.Single(u => u.Email == request.Email);
        var savedRole = context.Roles.Single(r => r.RoleID == savedUser.RoleID);
        Assert.Equal(RoleNames.Participant, savedRole.RoleName);

        //registering doesnt log you in, the session should still be empty
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserID));
    }

    [Fact]
    public async Task Register_SavesHashedPassword_NotThePlainPassword()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();

        await controller.Register(request);

        var savedUser = context.Users.Single(u => u.Email == request.Email);

        //whats stored is a bcrypt hash, not the password itself
        Assert.NotEqual(request.Password, savedUser.PasswordHash);
        Assert.StartsWith("$2", savedUser.PasswordHash);

        //but the hash still matches the original password
        Assert.True(PasswordHelper.VerifyPassword(request.Password, savedUser.PasswordHash));
    }

    [Fact]
    public async Task Register_RoleAndEmailInAnyCasing_AreStoredProperly()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();
        request.RoleName = "organiser";
        request.Email = "Isaac.Forgemaster@Gmail.com";

        var result = await controller.Register(request);

        var created = Assert.IsType<ObjectResult>(result);
        var response = Assert.IsType<UserResponse>(created.Value);

        //role comes back spelt properly and the email comes back lowercase
        Assert.Equal(RoleNames.Organiser, response.RoleName);
        Assert.Equal("isaac.forgemaster@gmail.com", response.Email);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Organisers")]
    public async Task Register_UnknownRoleName_Returns400(string roleName)
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();
        request.RoleName = roleName;

        var result = await controller.Register(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Role name must be either Organiser or Participant.", ResultHelper.GetMessage(result));

        //nobody new got saved, still just the 4 seed users
        Assert.Equal(4, context.Users.Count());
    }

    [Fact]
    public async Task Register_EmailAlreadyUsed_Returns409()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();

        //alucards email with different casing, still counts as the same email
        request.Email = "FloatingSword@Gmail.com";

        var result = await controller.Register(request);

        Assert.Equal(StatusCodes.Status409Conflict, ResultHelper.GetStatusCode(result));
        Assert.Equal("An account with this email already exists.", ResultHelper.GetMessage(result));
        Assert.Equal(4, context.Users.Count());
    }

    [Fact]
    public async Task Register_DateOfBirthToday_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();
        request.DateOfBirth = TestClock.DefaultToday;

        var result = await controller.Register(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Date of birth must be in the past.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Register_ParticipantTurning10Today_Returns201()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();

        //turns 10 on the test date, exactly old enough
        request.DateOfBirth = new DateOnly(2016, 10, 15);

        var result = await controller.Register(request);

        Assert.Equal(StatusCodes.Status201Created, ResultHelper.GetStatusCode(result));
    }

    [Fact]
    public async Task Register_ParticipantAged9_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();

        //only turns 10 tomorrow, so still 9 on the test date
        request.DateOfBirth = new DateOnly(2016, 10, 16);

        var result = await controller.Register(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Participants must be at least 10 years old.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Register_OrganiserAged17_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();
        request.RoleName = RoleNames.Organiser;

        //17 on the test date, old enough to be a participant but not an organiser
        request.DateOfBirth = new DateOnly(2009, 10, 15);

        var result = await controller.Register(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Organisers must be at least 18 years old.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Register_Aged100_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var request = TestRequests.ValidRegisterRequest();

        //turns 100 on the test date, has to be younger than 100
        request.DateOfBirth = new DateOnly(1926, 10, 15);

        var result = await controller.Register(request);

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Please enter a valid date of birth. Users must be younger than 100 years old.", ResultHelper.GetMessage(result));
    }

    //login

    [Fact]
    public async Task Login_CorrectDetails_Returns200AndStartsSession()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.Login(new LoginRequest { Email = AlucardEmail, Password = SeedIds.Password });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal(SeedIds.Alucard, response.UserID);

        //the session now holds alucards id and role, the two things the role filter reads
        Assert.Equal(SeedIds.Alucard, controller.HttpContext.Session.GetInt32(SessionKeys.UserID));
        Assert.Equal(RoleNames.Participant, controller.HttpContext.Session.GetString(SessionKeys.RoleName));
    }

    [Fact]
    public async Task Login_EmailInDifferentCasing_Returns200()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.Login(new LoginRequest { Email = "FloatingSword@Gmail.com", Password = SeedIds.Password });

        Assert.Equal(StatusCodes.Status200OK, ResultHelper.GetStatusCode(result));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401AndNoSession()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.Login(new LoginRequest { Email = AlucardEmail, Password = "WrongPassword" });

        Assert.Equal(StatusCodes.Status401Unauthorized, ResultHelper.GetStatusCode(result));
        Assert.Equal("Invalid email or password.", ResultHelper.GetMessage(result));

        //nobody got logged in
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserID));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401WithSameMessage()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.Login(new LoginRequest { Email = "nobody@gmail.com", Password = SeedIds.Password });

        //same message as a wrong password, so it doesnt give away which emails have accounts
        Assert.Equal(StatusCodes.Status401Unauthorized, ResultHelper.GetStatusCode(result));
        Assert.Equal("Invalid email or password.", ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Login_WhileAlreadyLoggedIn_ReplacesPreviousUser()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        await controller.Login(new LoginRequest { Email = TrevorEmail, Password = SeedIds.Password });
        await controller.Login(new LoginRequest { Email = AlucardEmail, Password = SeedIds.Password });

        //trevor is gone from the session, only alucard is left
        Assert.Equal(SeedIds.Alucard, controller.HttpContext.Session.GetInt32(SessionKeys.UserID));
        Assert.Equal(RoleNames.Participant, controller.HttpContext.Session.GetString(SessionKeys.RoleName));
    }

    //logout

    [Fact]
    public void Logout_ClearsSessionAndRemovesCookie()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        ControllerTestHelper.LogInAs(controller, SeedIds.Alucard, RoleNames.Participant);

        var result = controller.Logout();

        Assert.Equal(StatusCodes.Status200OK, ResultHelper.GetStatusCode(result));
        Assert.Equal("Logged out successfully.", ResultHelper.GetMessage(result));

        //the user is wiped from the session
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserID));
        Assert.Null(controller.HttpContext.Session.GetString(SessionKeys.RoleName));

        //and the response tells the browser to delete the session cookie
        Assert.Contains(SessionKeys.CookieName, controller.Response.Headers.SetCookie.ToString());
    }
}