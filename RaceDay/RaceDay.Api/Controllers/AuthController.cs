using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

//handles registering, logging in and logging out
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    //the dbcontext gets passed in automatically bcs its registered in Program.cs
    public AuthController(RaceDayDbContext context)
    {
        _context = context;
    }

    //POST /api/auth/register
    //creates a new organiser or participant account, doesnt log them in
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        //only organiser or participant allowed, any casing works but it gets stored the proper way
        var validRoles = new[] { RoleNames.Organiser, RoleNames.Participant };
        var roleName = validRoles.FirstOrDefault(r =>
            string.Equals(r, request.RoleName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (roleName == null)
        {
            return BadRequest(new { message = "Role name must be either Organiser or Participant." });
        }

        //dob has to be before today
        var dateOfBirth = request.DateOfBirth!.Value;
        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest(new { message = "Date of birth must be in the past." });
        }

        //emails get stored lowercase so the same email cant sign up twice with different casing
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var role = await _context.Roles.SingleAsync(r => r.RoleName == roleName);

        //only the hash gets saved, never the actual password
        var user = new User
        {
            RoleID = role.RoleID,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = dateOfBirth,
            Email = email,
            PasswordHash = PasswordHelper.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        //userid gets filled in by the database once its saved
        return StatusCode(StatusCodes.Status201Created, UserResponse.FromUser(user, role.RoleName));
    }

    //POST /api/auth/login
    //checks the email and password, then starts the session
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        //role gets loaded too so it can go into the session
        var user = await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Email == email);

        //same message whether the email or the password is wrong, so it doesnt give away which emails have accounts
        if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        //clears anything left over from a previous login before storing the new user
        await HttpContext.Session.LoadAsync();
        HttpContext.Session.Clear();

        //server side session, only the session id goes to the browser in the cookie
        HttpContext.Session.SetInt32(SessionKeys.UserID, user.UserID);
        HttpContext.Session.SetString(SessionKeys.RoleName, user.Role.RoleName);

        return Ok(UserResponse.FromUser(user, user.Role.RoleName));
    }

    //POST /api/auth/logout
    //ends the session, any logged in user can do this
    [HttpPost("logout")]
    [SessionAuthorize]
    public IActionResult Logout()
    {
        //wipes the user from the session on the server and removes the cookie from the browser
        HttpContext.Session.Clear();
        Response.Cookies.Delete(SessionKeys.CookieName);

        return Ok(new { message = "Logged out successfully." });
    }
}