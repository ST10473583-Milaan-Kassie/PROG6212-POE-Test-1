using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// The logged-in user's own profile. Available to both roles.
/// </summary>
//the logged in user's own profile, works for both roles
//[SessionAuthorize] on the class covers every endpoint in here
[ApiController]
[Route("api/users")]
[SessionAuthorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IFileStorageService _storage;

    public UsersController(RaceDayDbContext context, IFileStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    /// <summary>
    /// Returns the logged-in user's own profile.
    /// </summary>
    /// <remarks>
    /// Requires a logged-in user of either role. The user is identified by their session, not by an ID in the URL.
    /// </remarks>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Nobody is logged in.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Updates the logged-in user's name and phone number.
    /// </summary>
    /// <remarks>
    /// Requires a logged-in user of either role. Only the first name, last name and phone number can be changed.
    /// The date of birth and email cannot be changed through the profile, and are ignored if sent.
    /// A blank phone number clears it. Sending details that have not changed still succeeds.
    /// </remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="401">Nobody is logged in.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Uploads or replaces the logged-in user's profile picture.
    /// </summary>
    /// <remarks>
    /// Requires a logged-in user of either role. Send the image as multipart/form-data in the File field.
    /// It must be a JPG, PNG or WEBP file no larger than 5 MB, and its contents are checked to confirm it is a real image.
    /// Any previous profile picture is deleted from storage.
    /// </remarks>
    /// <response code="200">The updated profile, with the new picture's URL.</response>
    /// <response code="400">The file is missing, larger than 5 MB, not a JPG, PNG or WEBP, or not a valid image.</response>
    /// <response code="401">Nobody is logged in.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    //POST /api/users/me/profile-picture
    //uploads or replaces the logged in users profile picture
    [HttpPost("me/profile-picture")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture([FromForm] ImageUploadRequest request)
    {
        //jpg, png or webp up to 5 mb
        var imageError = ImageValidation.Validate(request.File);
        if (imageError != null)
        {
            return BadRequest(new { message = imageError });
        }

        var userId = HttpContext.Session.GetCurrentUserID();

        var user = await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserID == userId);

        if (user == null)
        {
            return Unauthorized(new { message = "You must be logged in to access this endpoint." });
        }

        //remembers the old picture so it can be removed once the new one is saved
        var oldImageUrl = user.ProfileImageURL;

        user.ProfileImageURL = await _storage.UploadAsync(request.File!, "profile-pictures");
        await _context.SaveChangesAsync();

        //deleted after saving, so if the save failed the old picture would still work
        if (oldImageUrl != null)
        {
            await _storage.DeleteAsync(oldImageUrl);
        }

        return Ok(UserResponse.FromUser(user, user.Role.RoleName));
    }
}