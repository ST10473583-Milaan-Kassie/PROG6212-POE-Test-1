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
/// Event enrolments. Participants enrol and withdraw, and Organisers view and confirm enrolments for their own events.
/// </summary>
//enrolments, participants enrol and withdraw, organisers view and confirm them
//no route on the class, the endpoints sit under both /api/events and /api/enrolments like the plan
[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ISouthAfricanClock _clock;

    public EnrolmentsController(RaceDayDbContext context, ISouthAfricanClock clock)
    {
        _context = context;
        _clock = clock;
    }

    /// <summary>
    /// Enrols the logged-in Participant in an upcoming event, under the chosen category.
    /// </summary>
    /// <remarks>
    /// Participants only. The category must belong to this event. When the category has an age range, the participant's age on the event date must fall within it.
    /// Categories with only a distance are open to every participant.
    /// Each participant can enrol only once per event. New enrolments start as Pending until the Organiser confirms them.
    /// </remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <param name="request">The ID of the category to enrol under.</param>
    /// <response code="201">The enrolment was recorded.</response>
    /// <response code="400">The event has already taken place, the category is not part of the event, or the participant's age is outside the category's age range.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not a Participant.</response>
    /// <response code="404">No event has this ID.</response>
    /// <response code="409">The participant is already enrolled in this event.</response>
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //POST /api/events/{eventId}/enrolments
    //enters the logged in participant into an upcoming event under the category they picked
    [HttpPost("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Participant)]
    public async Task<IActionResult> Enrol(int eventId, EnrolmentRequest request)
    {
        var ev = await _context.Events.FindAsync(eventId);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        //only upcoming events can be entered, the plan uses 400 for this one
        if (_clock.HasTakenPlace(ev.EventDate))
        {
            return BadRequest(new { message = "This event has already taken place, so it can no longer be entered." });
        }

        //the category has to belong to this event, an id from a different event counts as not part of it
        var category = await _context.Categories
            .SingleOrDefaultAsync(c => c.CategoryID == request.CategoryID!.Value && c.EventID == eventId);

        if (category == null)
        {
            return BadRequest(new { message = "The selected category is not part of this event." });
        }

        var userId = HttpContext.Session.GetCurrentUserID();

        //only once per event, UQ_Enrolments_UserEvent backs this up in the database
        if (await _context.Enrolments.AnyAsync(en => en.UserID == userId && en.EventID == eventId))
        {
            return Conflict(new { message = "You are already enrolled in this event." });
        }

        //age gets checked on race day, not today
        //distance only categories have no age range so everyone passes this
        var user = await _context.Users.SingleAsync(u => u.UserID == userId);
        if (!AgeHelper.IsAgeEligible(user.DateOfBirth, ev.EventDate, category.MinAge, category.MaxAge))
        {
            var ageOnEventDate = AgeHelper.GetAgeOn(user.DateOfBirth, ev.EventDate);
            return BadRequest(new { message = $"You will be {ageOnEventDate} on the event date, which is outside this category's age range." });
        }

        //new enrolments always start as pending, the organiser confirms them
        //setting the event and category sets eventid and categoryid too
        var enrolment = new Enrolment
        {
            UserID = userId,
            Event = ev,
            Category = category,
            Status = EnrolmentStatuses.Pending
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, EnrolmentResponse.FromEnrolment(enrolment, false));
    }

    /// <summary>
    /// Withdraws the logged-in Participant's own enrolment from an upcoming event.
    /// </summary>
    /// <remarks>
    /// Participants only, and only for their own enrolments.
    /// Enrolments for events that have taken place are kept as historical records and cannot be withdrawn.
    /// </remarks>
    /// <param name="id">The enrolment's ID.</param>
    /// <response code="204">The enrolment was withdrawn.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not a Participant, or the enrolment belongs to someone else.</response>
    /// <response code="404">No enrolment has this ID.</response>
    /// <response code="409">The event has already taken place.</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //DELETE /api/enrolments/{id}
    //withdraws the logged in participant from an upcoming event
    [HttpDelete("api/enrolments/{id:int}")]
    [SessionAuthorize(RoleNames.Participant)]
    public async Task<IActionResult> Withdraw(int id)
    {
        //the event gets loaded to check if its taken place
        var enrolment = await _context.Enrolments
            .Include(en => en.Event)
            .SingleOrDefaultAsync(en => en.EnrolmentID == id);

        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        //participants can only withdraw their own enrolments
        if (enrolment.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return ApiResponses.Forbidden("You can only withdraw your own enrolments.");
        }

        //enrolments for events that have taken place are kept as history
        if (_clock.HasTakenPlace(enrolment.Event.EventDate))
        {
            return Conflict(new { message = "This event has already taken place, so the enrolment is kept as a historical record." });
        }

        //no result to remove, results only exist once the event has taken place and then this is blocked
        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Lists the logged-in Participant's own enrolments.
    /// </summary>
    /// <remarks>
    /// Participants only. Events are returned soonest first. hasTakenPlace shows whether withdrawing is still allowed.
    /// </remarks>
    /// <response code="200">The participant's enrolments.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not a Participant.</response>
    [ProducesResponseType<IEnumerable<EnrolmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    //GET /api/enrolments/me
    //the logged in participants own enrolments, soonest events first
    [HttpGet("api/enrolments/me")]
    [SessionAuthorize(RoleNames.Participant)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = HttpContext.Session.GetCurrentUserID();

        var enrolments = await _context.Enrolments
            .Include(en => en.Event)
            .Include(en => en.Category)
            .Where(en => en.UserID == userId)
            .OrderBy(en => en.Event.EventDate)
            .ThenBy(en => en.EnrolmentID)
            .ToListAsync();

        return Ok(enrolments.Select(en => EnrolmentResponse.FromEnrolment(en, _clock.HasTakenPlace(en.Event.EventDate))));
    }

    /// <summary>
    /// Lists the enrolments for the logged-in Organiser's own event.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. Only each participant's name is returned, not their contact details or date of birth.
    /// </remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <param name="categoryID">Optional. Only show enrolments in this category.</param>
    /// <response code="200">The event's enrolments, grouped by category.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID, or the category is not part of this event.</response>
    [ProducesResponseType<IEnumerable<EventEnrolmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    //GET /api/events/{eventId}/enrolments
    //all enrolments for the logged in organisers own event, can be filtered by category
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> GetEventEnrolments(int eventId, [FromQuery] int? categoryID)
    {
        var ev = await _context.Events.FindAsync(eventId);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        //organisers can only see enrolments for their own events
        if (ev.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return ApiResponses.Forbidden("You can only view enrolments for your own events.");
        }

        //filtering by a category from a different event is a mistake, not just an empty list
        if (categoryID.HasValue &&
            !await _context.Categories.AnyAsync(c => c.CategoryID == categoryID.Value && c.EventID == eventId))
        {
            return NotFound(new { message = "Category not found in this event." });
        }

        //participant, category and result get loaded for the response
        IQueryable<Enrolment> query = _context.Enrolments
            .Include(en => en.User)
            .Include(en => en.Category)
            .Include(en => en.Result)
            .Where(en => en.EventID == eventId);

        if (categoryID.HasValue)
        {
            query = query.Where(en => en.CategoryID == categoryID.Value);
        }

        //grouped by category, youngest category first, then by surname
        var enrolments = await query
            .OrderBy(en => en.Category.MinAge)
            .ThenBy(en => en.Category.CategoryName)
            .ThenBy(en => en.User.LastName)
            .ThenBy(en => en.User.FirstName)
            .ToListAsync();

        return Ok(enrolments.Select(EventEnrolmentResponse.FromEnrolment));
    }

    /// <summary>
    /// Sets an enrolment on the logged-in Organiser's own event to Pending or Confirmed.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. The status can still be changed on race day, so participants can be confirmed as they check in.
    /// It cannot be changed once a result has been captured. A Confirmed enrolment also cannot be set back to Pending
    /// if that would leave fewer Confirmed enrolments than the highest finishing position already captured.
    /// </remarks>
    /// <param name="id">The enrolment's ID.</param>
    /// <param name="request">The new status, either Pending or Confirmed.</param>
    /// <response code="200">The updated enrolment.</response>
    /// <response code="400">The status is missing, or is not Pending or Confirmed.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No enrolment has this ID.</response>
    /// <response code="409">A result has already been captured, or the change would leave fewer Confirmed enrolments than the highest finishing position.</response>
    [ProducesResponseType<EventEnrolmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //PUT /api/enrolments/{id}/status
    //sets an enrolment on the logged in organisers own event to pending or confirmed
    [HttpPut("api/enrolments/{id:int}/status")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> UpdateEnrolmentStatus(int id, UpdateEnrolmentStatusRequest request)
    {
        //the event is loaded to check the owner, the rest is for the response and the result check
        var enrolment = await _context.Enrolments
            .Include(en => en.Event)
            .Include(en => en.User)
            .Include(en => en.Category)
            .Include(en => en.Result)
            .SingleOrDefaultAsync(en => en.EnrolmentID == id);

        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Event.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return ApiResponses.Forbidden("You can only manage enrolments for your own events.");
        }

        //once a result is captured the status is locked
        //not blocked on race day though, so organisers can still confirm people as they check in
        if (enrolment.Result != null)
        {
            return Conflict(new { message = "A result has already been captured for this enrolment, so its status can no longer be changed." });
        }

        //only pending or confirmed, any casing works but it gets stored the proper way to match CK_Enrolments_Status
        var validStatuses = new[] { EnrolmentStatuses.Pending, EnrolmentStatuses.Confirmed };
        var status = validStatuses.FirstOrDefault(s =>
            string.Equals(s, request.Status.Trim(), StringComparison.OrdinalIgnoreCase));

        if (status == null)
        {
            return BadRequest(new { message = "Status must be either Pending or Confirmed." });
        }

        //setting a confirmed enrolment back to pending lowers the number of confirmed participants
        //that number cant drop below the highest finishing position already captured for the event
        if (enrolment.Status == EnrolmentStatuses.Confirmed && status == EnrolmentStatuses.Pending)
        {
            var confirmedParticipants = await _context.Enrolments
                .CountAsync(en => en.EventID == enrolment.EventID && en.Status == EnrolmentStatuses.Confirmed);

            //null when no results have been captured yet, so it falls back to 0
            var highestPosition = await _context.Results
                .Where(r => r.Enrolment.EventID == enrolment.EventID)
                .MaxAsync(r => (int?)r.FinishingPosition) ?? 0;

            if (highestPosition > confirmedParticipants - 1)
            {
                return Conflict(new { message = $"This enrolment cannot be set back to Pending, a finishing position of {highestPosition} has already been captured for this event." });
            }
        }

        enrolment.Status = status;
        await _context.SaveChangesAsync();

        return Ok(EventEnrolmentResponse.FromEnrolment(enrolment));
    }
}