using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Tests.Helpers;

//sets a controller up like its handling a real request
public static class ControllerTestHelper
{
    //gives the controller a pretend request with a fake session on it, nobody is logged in yet
    public static FakeSession AttachSession(ControllerBase controller)
    {
        var session = new FakeSession();
        var httpContext = new DefaultHttpContext { Session = session };

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        return session;
    }

    //logs in by putting the users ID and role in the session, the same two values login stores
    public static void LogInAs(ControllerBase controller, int userID, string roleName)
    {
        controller.HttpContext.Session.SetInt32(SessionKeys.UserID, userID);
        controller.HttpContext.Session.SetString(SessionKeys.RoleName, roleName);
    }
}