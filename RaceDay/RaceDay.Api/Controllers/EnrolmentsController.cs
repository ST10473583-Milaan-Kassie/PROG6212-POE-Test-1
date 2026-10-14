using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

//enrolments, the participant endpoints are in here for now
//the organiser endpoints get added in the next commit
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
}