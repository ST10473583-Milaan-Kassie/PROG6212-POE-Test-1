using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Tests.Helpers;

//pulls the status code and message out of whatever a controller sends back
public static class ResultHelper
{
    //Ok, BadRequest, Conflict etc all carry a status code, NoContent just doesnt have a body with it
    public static int? GetStatusCode(IActionResult result)
    {
        return result switch
        {
            ObjectResult objectResult => objectResult.StatusCode,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            _ => null
        };
    }

    //the error bodies are made as new { message = "..." }, theres no class to cast that to so the message gets read by its name
    public static string? GetMessage(IActionResult result)
    {
        var value = (result as ObjectResult)?.Value;
        return value?.GetType().GetProperty("message")?.GetValue(value) as string;
    }
}