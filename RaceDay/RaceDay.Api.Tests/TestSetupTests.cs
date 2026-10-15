using Microsoft.AspNetCore.Http;
using RaceDay.Api.Controllers;
using RaceDay.Api.Helpers;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests;

//makes sure the test helpers actually work before the real tests start leaning on them
public class TestSetupTests
{
    [Fact]
    public void InMemoryDatabase_LoadsAllSeedData()
    {
        using var context = TestDbContextFactory.Create();

        //same row counts as SeedData.cs and the SQL script
        Assert.Equal(2, context.Roles.Count());
        Assert.Equal(4, context.Users.Count());
        Assert.Equal(3, context.EventTypes.Count());
        Assert.Equal(3, context.Events.Count());
        Assert.Equal(8, context.Categories.Count());
        Assert.Equal(4, context.Enrolments.Count());
        Assert.Equal(2, context.Results.Count());
    }

    [Fact]
    public void LogInAs_PutsUserAndRoleInSession()
    {
        using var context = TestDbContextFactory.Create();

        //any controller works here, event types is just the simplest one to make
        var controller = new EventTypesController(context);
        ControllerTestHelper.AttachSession(controller);

        ControllerTestHelper.LogInAs(controller, SeedIDs.Trevor, RoleNames.Organiser);

        Assert.Equal(SeedIDs.Trevor, controller.HttpContext.Session.GetCurrentUserID());
        Assert.Equal(RoleNames.Organiser, controller.HttpContext.Session.GetString(SessionKeys.RoleName));
    }
}