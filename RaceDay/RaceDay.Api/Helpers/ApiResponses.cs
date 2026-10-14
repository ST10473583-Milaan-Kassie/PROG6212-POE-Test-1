using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Helpers;

//shared responses that aspnet doesnt have a simple built in method for
public static class ApiResponses
{
    //403 built by hand bcs Forbid() needs aspnet's built in auth which isnt used here
    public static ObjectResult Forbidden(string message)
    {
        return new ObjectResult(new { message })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}