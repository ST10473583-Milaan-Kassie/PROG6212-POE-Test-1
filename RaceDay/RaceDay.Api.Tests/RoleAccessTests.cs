using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Controllers;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests;

//checks the login and role rules on every endpoint
//[SessionAuthorize] runs before the endpoint, calling a controller directly skips it, so the filter gets run here on its own
public class RoleAccessTests
{
    private const string LoginMessage = "You must be logged in to access this endpoint.";
    private const string RoleMessage = "Your role does not have access to this endpoint.";

    //runs a filter the same way aspnet does before an endpoint
    //gives back whatever the filter decided, null means it let the request through
    private static async Task<IActionResult?> RunFilter(SessionAuthorizeAttribute filter, int? userID, string? roleName)
    {
        var session = new FakeSession();

        if (userID != null)
        {
            session.SetInt32(SessionKeys.UserID, userID.Value);
        }

        if (roleName != null)
        {
            session.SetString(SessionKeys.RoleName, roleName);
        }

        var httpContext = new DefaultHttpContext { Session = session };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var filterContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        await filter.OnAuthorizationAsync(filterContext);

        return filterContext.Result;
    }

    //finds the [SessionAuthorize] thats really on an endpoint, either on the method or on its whole controller
    private static SessionAuthorizeAttribute? GetEndpointFilter(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName)
            ?? throw new InvalidOperationException($"{controllerType.Name} has no method called {methodName}.");

        return method.GetCustomAttribute<SessionAuthorizeAttribute>()
            ?? controllerType.GetCustomAttribute<SessionAuthorizeAttribute>();
    }

    //the filter on its own

    [Fact]
    public async Task Filter_NotLoggedIn_Returns401()
    {
        var filter = new SessionAuthorizeAttribute();

        var result = await RunFilter(filter, null, null);

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, ResultHelper.GetStatusCode(result));
        Assert.Equal(LoginMessage, ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Filter_RoleButNoUserID_Returns401()
    {
        var filter = new SessionAuthorizeAttribute();

        //half a session isnt a login, both values have to be there
        var result = await RunFilter(filter, null, RoleNames.Organiser);

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, ResultHelper.GetStatusCode(result));
    }

    [Fact]
    public async Task Filter_WrongRole_Returns403()
    {
        var filter = new SessionAuthorizeAttribute(RoleNames.Organiser);

        //alucard is a participant trying to get into an organiser endpoint
        var result = await RunFilter(filter, SeedIDs.Alucard, RoleNames.Participant);

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(result));
        Assert.Equal(RoleMessage, ResultHelper.GetMessage(result));
    }

    [Fact]
    public async Task Filter_RightRole_LetsRequestThrough()
    {
        var filter = new SessionAuthorizeAttribute(RoleNames.Organiser);

        var result = await RunFilter(filter, SeedIDs.Trevor, RoleNames.Organiser);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(SeedIDs.Trevor, RoleNames.Organiser)]
    [InlineData(SeedIDs.Alucard, RoleNames.Participant)]
    public async Task Filter_NoRoleGiven_LetsAnyLoggedInUserThrough(int userID, string roleName)
    {
        var filter = new SessionAuthorizeAttribute();

        var result = await RunFilter(filter, userID, roleName);

        Assert.Null(result);
    }

    //every real endpoint

    [Theory]
    [InlineData(typeof(EventsController), nameof(EventsController.GetMyEvents))]
    [InlineData(typeof(EventsController), nameof(EventsController.CreateEvent))]
    [InlineData(typeof(EventsController), nameof(EventsController.UpdateEvent))]
    [InlineData(typeof(EventsController), nameof(EventsController.DeleteEvent))]
    [InlineData(typeof(EventsController), nameof(EventsController.UploadBanner))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.AddCategory))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.UpdateCategory))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.DeleteCategory))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetEventEnrolments))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.UpdateEnrolmentStatus))]
    [InlineData(typeof(ResultsController), nameof(ResultsController.CaptureResult))]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetEventResults))]
    [InlineData(typeof(ResultsController), nameof(ResultsController.PublishResults))]
    public async Task OrganiserEndpoints_RejectParticipants_AndAllowOrganisers(Type controllerType, string methodName)
    {
        var filter = GetEndpointFilter(controllerType, methodName);
        Assert.NotNull(filter);

        //a participant gets turned away
        var participantResult = await RunFilter(filter, SeedIDs.Alucard, RoleNames.Participant);
        Assert.NotNull(participantResult);
        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(participantResult));

        //an organiser gets through
        var organiserResult = await RunFilter(filter, SeedIDs.Trevor, RoleNames.Organiser);
        Assert.Null(organiserResult);
    }

    [Theory]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Withdraw))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetMyEnrolments))]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetMyResults))]
    public async Task ParticipantEndpoints_RejectOrganisers_AndAllowParticipants(Type controllerType, string methodName)
    {
        var filter = GetEndpointFilter(controllerType, methodName);
        Assert.NotNull(filter);

        //an organiser gets turned away
        var organiserResult = await RunFilter(filter, SeedIDs.Trevor, RoleNames.Organiser);
        Assert.NotNull(organiserResult);
        Assert.Equal(StatusCodes.Status403Forbidden, ResultHelper.GetStatusCode(organiserResult));

        //a participant gets through
        var participantResult = await RunFilter(filter, SeedIDs.Alucard, RoleNames.Participant);
        Assert.Null(participantResult);
    }

    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.Logout))]
    [InlineData(typeof(UsersController), nameof(UsersController.GetMyProfile))]
    [InlineData(typeof(UsersController), nameof(UsersController.UpdateMyProfile))]
    [InlineData(typeof(UsersController), nameof(UsersController.UploadProfilePicture))]
    public async Task AnyRoleEndpoints_AllowBothRoles(Type controllerType, string methodName)
    {
        var filter = GetEndpointFilter(controllerType, methodName);
        Assert.NotNull(filter);

        Assert.Null(await RunFilter(filter, SeedIDs.Trevor, RoleNames.Organiser));
        Assert.Null(await RunFilter(filter, SeedIDs.Alucard, RoleNames.Participant));
    }

    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.Logout))]
    [InlineData(typeof(UsersController), nameof(UsersController.GetMyProfile))]
    [InlineData(typeof(UsersController), nameof(UsersController.UpdateMyProfile))]
    [InlineData(typeof(UsersController), nameof(UsersController.UploadProfilePicture))]
    [InlineData(typeof(EventsController), nameof(EventsController.GetMyEvents))]
    [InlineData(typeof(EventsController), nameof(EventsController.CreateEvent))]
    [InlineData(typeof(EventsController), nameof(EventsController.UpdateEvent))]
    [InlineData(typeof(EventsController), nameof(EventsController.DeleteEvent))]
    [InlineData(typeof(EventsController), nameof(EventsController.UploadBanner))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.AddCategory))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.UpdateCategory))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.DeleteCategory))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Withdraw))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetMyEnrolments))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetEventEnrolments))]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.UpdateEnrolmentStatus))]
    [InlineData(typeof(ResultsController), nameof(ResultsController.CaptureResult))]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetEventResults))]
    [InlineData(typeof(ResultsController), nameof(ResultsController.PublishResults))]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetMyResults))]
    public async Task ProtectedEndpoints_RejectNotLoggedIn_With401(Type controllerType, string methodName)
    {
        var filter = GetEndpointFilter(controllerType, methodName);
        Assert.NotNull(filter);

        var result = await RunFilter(filter, null, null);

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, ResultHelper.GetStatusCode(result));
        Assert.Equal(LoginMessage, ResultHelper.GetMessage(result));
    }

    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.Register))]
    [InlineData(typeof(AuthController), nameof(AuthController.Login))]
    [InlineData(typeof(EventTypesController), nameof(EventTypesController.GetEventTypes))]
    [InlineData(typeof(EventsController), nameof(EventsController.GetEvents))]
    [InlineData(typeof(EventsController), nameof(EventsController.GetEvent))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.GetCategories))]
    public void PublicEndpoints_HaveNoLoginCheck(Type controllerType, string methodName)
    {
        //anyone can use these, logged in or not, so theres no filter on them at all
        Assert.Null(GetEndpointFilter(controllerType, methodName));
    }
}