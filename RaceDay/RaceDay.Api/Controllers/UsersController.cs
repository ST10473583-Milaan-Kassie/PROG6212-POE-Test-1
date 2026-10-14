using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

//the logged in user's own profile, works for both roles
//[SessionAuthorize] on the class covers every endpoint in here
[ApiController]
[Route("api/users")]
[SessionAuthorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public UsersController(RaceDayDbContext context)
    {
        _context = context;
    }

    //GET /api/users/me
    //shows the logged in user's own profile
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        //the id comes from the session, never from the url, so nobody can look at someone elses profile
        var userId = HttpContext.Session.GetCurrentUserID();

        var user = await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserID == userId);

        //only happens if the account is gone but the session isnt
        if (user == null)
        {
            return Unauthorized(new { message = "You must be logged in to access this endpoint." });
        }

        return Ok(UserResponse.FromUser(user, user.Role.RoleName));
    }

    //PUT /api/users/me
    //updates the logged in user's name and phone number, dob and email cant be changed here
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest request)
    {
        var userId = HttpContext.Session.GetCurrentUserID();

        var user = await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserID == userId);

        if (user == null)
        {
            return Unauthorized(new { message = "You must be logged in to access this endpoint." });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        //blank phone number gets saved as null so the column stays empty
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        await _context.SaveChangesAsync();

        return Ok(UserResponse.FromUser(user, user.Role.RoleName));
    }
}