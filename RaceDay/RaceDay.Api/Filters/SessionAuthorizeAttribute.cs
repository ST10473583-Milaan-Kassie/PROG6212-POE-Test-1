using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Filters;

//put on a controller or endpoint to lock it behind the session login
//[SessionAuthorize] lets in any logged in user, [SessionAuthorize(RoleNames.Organiser)] only lets in organisers
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SessionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    //the roles allowed in, empty means any logged in user
    private readonly string[] _allowedRoles;

    public SessionAuthorizeAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    //runs before the endpoint, if it sets a result the endpoint never runs
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        //loads the session data before reading from it
        await session.LoadAsync();

        var userId = session.GetInt32(SessionKeys.UserID);
        var roleName = session.GetString(SessionKeys.RoleName);

        //nobody logged in, 401
        if (userId == null || string.IsNullOrEmpty(roleName))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "You must be logged in to access this endpoint."
            });
            return;
        }

        //logged in but the wrong role, 403
        //built by hand bcs Forbid() needs aspnet's built in auth which isnt used here
        if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(roleName))
        {
            context.Result = new ObjectResult(new
            {
                message = "Your role does not have access to this endpoint."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}