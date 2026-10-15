using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Registration, login and logout for both roles.
/// </summary>
//handles registering, logging in and logging out
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ISouthAfricanClock _clock;

    //the dbcontext and clock get passed in automatically bcs theyre registered in Program.cs
    public AuthController(RaceDayDbContext context, ISouthAfricanClock clock)
    {
        _context = context;
        _clock = clock;
    }

    /// <summary>
    /// Registers a new Organiser or Participant account.
    /// </summary>
    /// <remarks>
    /// No login is required, and registering does not log the user in.
    /// The role name must be Organiser or Participant, and the email must not already be in use.
    /// Users must be younger than 100 on the registration date. Organisers must be at least 18 and Participants at least 10.
    /// The password must be 8 to 72 characters and is only ever stored as a BCrypt hash.
    /// </remarks>
    /// <response code="201">The account was created. The new user's details are returned, without the password.</response>
    /// <response code="400">A field is missing or invalid, the role name is not recognised, or the date of birth breaks the age limits.</response>
    /// <response code="409">An account with this email already exists.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
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

        //dob has to be before today in south africa
        var dateOfBirth = request.DateOfBirth!.Value;
        if (dateOfBirth >= _clock.Today)
        {
            return BadRequest(new { message = "Date of birth must be in the past." });
        }

        //age limits, worked out on todays date in south africa
        //dob cant be changed later so a wrong one gets caught here
        var age = AgeHelper.GetAgeOn(dateOfBirth, _clock.Today);

        if (age >= AgeLimits.MaximumAge)
        {
            return BadRequest(new { message = $"Please enter a valid date of birth. Users must be younger than {AgeLimits.MaximumAge} years old." });
        }

        if (roleName == RoleNames.Organiser && age < AgeLimits.MinimumOrganiserAge)
        {
            return BadRequest(new { message = $"Organisers must be at least {AgeLimits.MinimumOrganiserAge} years old." });
        }

        if (roleName == RoleNames.Participant && age < AgeLimits.MinimumParticipantAge)
        {
            return BadRequest(new { message = $"Participants must be at least {AgeLimits.MinimumParticipantAge} years old." });
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

    /// <summary>
    /// Logs in and starts a server-side session.
    /// </summary>
    /// <remarks>
    /// No login is required. On success, the user's ID and role are stored in a server-side session,
    /// and the browser receives the RaceDay.Session cookie, which it sends with every later request.
    /// The same message is returned for an unknown email and a wrong password.
    /// </remarks>
    /// <response code="200">Login succeeded. The user's details are returned.</response>
    /// <response code="400">The email or password is missing, or the email is not valid.</response>
    /// <response code="401">The email or password is incorrect.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Logs out and ends the session.
    /// </summary>
    /// <remarks>
    /// Requires a logged-in user of either role. Clears the server-side session and removes the session cookie.
    /// </remarks>
    /// <response code="200">Logged out.</response>
    /// <response code="401">Nobody is logged in.</response>
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
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