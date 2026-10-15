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
/// Categories for each event. Anyone can view them, and Organisers manage them for their own events.
/// </summary>
//categories for each event
//no route on the class, half the endpoints sit under /api/events and half under /api/categories like the plan
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ISouthAfricanClock _clock;

    public CategoriesController(RaceDayDbContext context, ISouthAfricanClock clock)
    {
        _context = context;
        _clock = clock;
    }

    /// <summary>
    /// Lists the categories for an event.
    /// </summary>
    /// <remarks>
    /// No login is required. Categories are returned youngest first. A maxAge of null means there is no maximum age.
    /// </remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">The event's categories.</response>
    /// <response code="404">No event has this ID.</response>
    [ProducesResponseType<IEnumerable<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    //GET /api/events/{eventId}/categories
    //lists an events categories, anyone can see them
    [HttpGet("api/events/{eventId:int}/categories")]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _context.Events.AnyAsync(ev => ev.EventID == eventId))
        {
            return NotFound(new { message = "Event not found." });
        }

        //youngest categories first
        var categories = await _context.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.MinAge)
            .ThenBy(c => c.CategoryID)
            .ToListAsync();

        return Ok(categories.Select(CategoryResponse.FromCategory));
    }

    /// <summary>
    /// Adds a category to the logged-in Organiser's own upcoming event.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. Categories cannot be added once the event has taken place.
    /// The minimum age must be between 10 and 99. The maximum age is optional, but when set it must be between the minimum age and 99.
    /// </remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <param name="request">The category's name, minimum age and optional maximum age.</param>
    /// <response code="201">The category was added.</response>
    /// <response code="400">A field is missing, or the ages are outside 10 to 99 or the wrong way round.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    /// <response code="409">The event has already taken place.</response>
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //POST /api/events/{eventId}/categories
    //adds a category to the logged in organisers own upcoming event
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> AddCategory(int eventId, CategoryRequest request)
    {
        var ev = await _context.Events.FindAsync(eventId);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        //organisers can only add categories to their own events
        if (ev.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return ApiResponses.Forbidden("You can only manage categories for your own events.");
        }

        //once race day comes the categories are locked with the event
        if (_clock.HasTakenPlace(ev.EventDate))
        {
            return Conflict(new { message = "This event has already taken place, so its categories can no longer be changed." });
        }

        var ageError = ValidateAges(request);
        if (ageError != null)
        {
            return ageError;
        }

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            MinAge = request.MinAge!.Value,
            MaxAge = request.MaxAge
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, CategoryResponse.FromCategory(category));
    }

    /// <summary>
    /// Updates a category on the logged-in Organiser's own upcoming event.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. The same age rules apply as when adding a category.
    /// The new age range must still include the age, on the event date, of every participant already enrolled in the category.
    /// </remarks>
    /// <param name="id">The category's ID.</param>
    /// <param name="request">The category's new name, minimum age and optional maximum age.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="400">A field is missing, or the ages are outside 10 to 99 or the wrong way round.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category has this ID.</response>
    /// <response code="409">The event has already taken place, or the new range would leave enrolments outside it.</response>
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //PUT /api/categories/{id}
    //updates a category on the logged in organisers own upcoming event
    [HttpPut("api/categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        //the event gets loaded to check who owns it and if its taken place
        var category = await _context.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryID == id);

        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return ApiResponses.Forbidden("You can only manage categories for your own events.");
        }

        if (_clock.HasTakenPlace(category.Event.EventDate))
        {
            return Conflict(new { message = "This event has already taken place, so its categories can no longer be changed." });
        }

        var ageError = ValidateAges(request);
        if (ageError != null)
        {
            return ageError;
        }

        //everyone already enrolled in this category has to still fit the new range on the event date
        var enrolledBirthDates = await _context.Enrolments
            .Where(en => en.CategoryID == category.CategoryID)
            .Select(en => en.User.DateOfBirth)
            .ToListAsync();

        var outsideRange = enrolledBirthDates.Count(dob =>
            !AgeHelper.IsAgeEligible(dob, category.Event.EventDate, request.MinAge!.Value, request.MaxAge));

        if (outsideRange > 0)
        {
            var participants = outsideRange == 1 ? "participant" : "participants";
            return Conflict(new { message = $"This change would leave {outsideRange} enrolled {participants} outside their category's age range." });
        }

        category.CategoryName = request.CategoryName.Trim();
        category.MinAge = request.MinAge!.Value;
        category.MaxAge = request.MaxAge;

        await _context.SaveChangesAsync();

        return Ok(CategoryResponse.FromCategory(category));
    }

    /// <summary>
    /// Deletes a category on the logged-in Organiser's own upcoming event, along with the enrolments that selected it.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. The category and its enrolments are deleted together in one transaction.
    /// Categories of events that have taken place cannot be deleted.
    /// </remarks>
    /// <param name="id">The category's ID.</param>
    /// <response code="204">The category was deleted.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category has this ID.</response>
    /// <response code="409">The event has already taken place.</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //DELETE /api/categories/{id}
    //deletes a category on the logged in organisers own upcoming event, along with the enrolments that picked it
    [HttpDelete("api/categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        //enrolments get loaded so they can be deleted along with the category
        var category = await _context.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .SingleOrDefaultAsync(c => c.CategoryID == id);

        if (category == null)
        {
            return NotFound(new { message = "Category not found." });
        }

        if (category.Event.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return ApiResponses.Forbidden("You can only manage categories for your own events.");
        }

        if (_clock.HasTakenPlace(category.Event.EventDate))
        {
            return Conflict(new { message = "This event has already taken place, so its categories can no longer be changed." });
        }

        //both go in one SaveChanges so its one transaction, if anything fails nothing gets deleted
        //no results to remove, results only exist once the event has taken place and then this is blocked
        _context.Enrolments.RemoveRange(category.Enrolments);
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    //category ages have to fit inside the registration age limits, 10 to 99
    private BadRequestObjectResult? ValidateAges(CategoryRequest request)
    {
        var minAge = request.MinAge!.Value;

        //maximumage is the age nobody can reach, so the oldest allowed is one less
        var oldestAllowed = AgeLimits.MaximumAge - 1;

        if (minAge < AgeLimits.MinimumParticipantAge || minAge > oldestAllowed)
        {
            return BadRequest(new { message = $"Minimum age must be between {AgeLimits.MinimumParticipantAge} and {oldestAllowed}." });
        }

        if (request.MaxAge.HasValue)
        {
            //same rule as CK_Categories_AgeRange in the sql, checked here so it comes back as a 400 instead of a database error
            if (request.MaxAge.Value < minAge)
            {
                return BadRequest(new { message = "Maximum age cannot be lower than the minimum age." });
            }

            if (request.MaxAge.Value > oldestAllowed)
            {
                return BadRequest(new { message = $"Maximum age cannot be more than {oldestAllowed}." });
            }
        }

        return null;
    }
}