using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
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
        var response = new UserResponse
        {
            UserID = user.UserID,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RoleName = role.RoleName,
            ProfileImageURL = user.ProfileImageURL
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }
}