using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Race results. Organisers capture, check and publish results for their own events, and Participants view their own once published.
/// </summary>
//results, organisers capture, check and publish them, participants view their own once theyre published
//no route on the class, the endpoints sit under /api/enrolments, /api/events and /api/results like the plan
[ApiController]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ISouthAfricanClock _clock;

    public ResultsController(RaceDayDbContext context, ISouthAfricanClock clock)
    {
        _context = context;
        _clock = clock;
    }

    /// <summary>
    /// Captures a result for a Confirmed enrolment on the logged-in Organiser's own event.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. Results can only be captured once the event has taken place (race day counts),
    /// only for Confirmed enrolments, only once per enrolment, and only until the event's results are published. The finish time is sent as hh:mm:ss.
    /// The finishing position must be at least 1 and no higher than the number of Confirmed enrolments for the event.
    /// Participants only see the result once the Organiser publishes the event's results.
    /// </remarks>
    /// <param name="enrolmentID">The enrolment's ID.</param>
    /// <param name="request">The finish time (hh:mm:ss) and the finishing position.</param>
    /// <response code="201">The result was captured.</response>
    /// <response code="400">The event has not taken place, the enrolment is not Confirmed, or the finish time or position is invalid.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No enrolment has this ID.</response>
    /// <response code="409">A result has already been captured for this enrolment, or the event's results have already been published.</response>
    [ProducesResponseType<EventResultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //POST /api/enrolments/{enrolmentID}/results
    //captures a result for a confirmed enrolment on the logged in organisers own event, once its taken place
    [HttpPost("api/enrolments/{enrolmentID:int}/results")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> CaptureResult(int enrolmentID, ResultRequest request)
    {
        //event for the owner and date checks, user and category for the response, result for the duplicate check
        var enrolment = await _context.Enrolments
            .Include(en => en.Event)
            .Include(en => en.User)
            .Include(en => en.Category)
            .Include(en => en.Result)
            .SingleOrDefaultAsync(en => en.EnrolmentID == enrolmentID);

        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Event.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return APIResponses.Forbidden("You can only capture results for your own events.");
        }

        //once results are published theyre final, nothing more gets added
        if (enrolment.Event.ResultsPublished)
        {
            return Conflict(new { message = "Results for this event have already been published." });
        }

        //only one result per enrolment, UQ_Results_Enrolment backs this up in the database
        if (enrolment.Result != null)
        {
            return Conflict(new { message = "A result has already been captured for this enrolment." });
        }

        //race day counts as taken place so results can go in on the day
        if (!_clock.HasTakenPlace(enrolment.Event.EventDate))
        {
            return BadRequest(new { message = "Results can only be captured once the event has taken place." });
        }

        if (enrolment.Status != EnrolmentStatuses.Confirmed)
        {
            return BadRequest(new { message = "Results can only be captured for confirmed enrolments." });
        }

        if (request.FinishingPosition!.Value < 1)
        {
            return BadRequest(new { message = "Finishing position must be 1 or higher." });
        }

        //nobody can finish lower than the number of confirmed participants, only they can get results
        //counted against confirmed enrolments not captured results, since results can go in in any order
        var confirmedParticipants = await _context.Enrolments
            .CountAsync(en => en.EventID == enrolment.EventID && en.Status == EnrolmentStatuses.Confirmed);

        if (request.FinishingPosition.Value > confirmedParticipants)
        {
            return BadRequest(new { message = $"Finishing position cannot be higher than the number of confirmed participants ({confirmedParticipants})." });
        }

        if (request.FinishTime!.Value == TimeOnly.MinValue)
        {
            return BadRequest(new { message = "Finish time must be more than 00:00:00." });
        }

        //setting the enrolment sets the EnrolmentID too
        var result = new Result
        {
            Enrolment = enrolment,
            FinishTime = request.FinishTime.Value,
            FinishingPosition = request.FinishingPosition.Value
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, EventResultResponse.FromResult(result));
    }

    /// <summary>
    /// Lists the results for the logged-in Organiser's own event, in finishing order.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. Every captured result is shown, including results that have not been published yet,
    /// so they can be checked before publishing.
    /// </remarks>
    /// <param name="eventID">The event's ID.</param>
    /// <response code="200">The event's results.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    [ProducesResponseType<IEnumerable<EventResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    //GET /api/events/{eventID}/results
    //all results for the logged in organisers own event, in finishing order, published or not so they can be checked first
    [HttpGet("api/events/{eventID:int}/results")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> GetEventResults(int eventID)
    {
        var ev = await _context.Events.FindAsync(eventID);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return APIResponses.Forbidden("You can only view results for your own events.");
        }

        //participant and category come through the enrolment
        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.User)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category)
            .Where(r => r.Enrolment.EventID == eventID)
            .OrderBy(r => r.FinishingPosition)
            .ThenBy(r => r.FinishTime)
            .ToListAsync();

        return Ok(results.Select(EventResultResponse.FromResult));
    }

    /// <summary>
    /// Publishes the results for the logged-in Organiser's own event, so Participants can see them.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. The event must have taken place and have at least one captured result.
    /// Published results are final: no more results can be captured for the event, and publishing cannot be undone.
    /// </remarks>
    /// <param name="eventID">The event's ID.</param>
    /// <response code="200">The results were published. The event is returned with resultsPublished set to true.</response>
    /// <response code="400">The event has not taken place, or no results have been captured for it.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    /// <response code="409">The event's results have already been published.</response>
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //POST /api/events/{eventID}/results/publish
    //publishes the logged in organisers results for an event, participants can only see them after this
    [HttpPost("api/events/{eventID:int}/results/publish")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> PublishResults(int eventID)
    {
        //type and organiser get loaded for the response
        var ev = await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.User)
            .SingleOrDefaultAsync(e => e.EventID == eventID);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return APIResponses.Forbidden("You can only publish results for your own events.");
        }

        //results only exist once the event has taken place, so theres nothing to publish before that
        if (!_clock.HasTakenPlace(ev.EventDate))
        {
            return BadRequest(new { message = "Results can only be published once the event has taken place." });
        }

        if (!await _context.Results.AnyAsync(r => r.Enrolment.EventID == eventID))
        {
            return BadRequest(new { message = "There are no results to publish for this event yet." });
        }

        //publishing is final, it cant be done twice or undone
        if (ev.ResultsPublished)
        {
            return Conflict(new { message = "Results for this event have already been published." });
        }

        ev.ResultsPublished = true;
        await _context.SaveChangesAsync();

        return Ok(EventResponse.FromEvent(ev, true));
    }

    /// <summary>
    /// Lists the logged-in Participant's own published results, most recent event first.
    /// </summary>
    /// <remarks>
    /// Participants only. Only results from events whose results the Organiser has published are shown.
    /// totalFinishers is the number of results for each event, so a result can be shown as, for example, 3rd out of 12 finishers.
    /// </remarks>
    /// <response code="200">The participant's results.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not a Participant.</response>
    [ProducesResponseType<IEnumerable<ParticipantResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    //GET /api/results/me
    //the logged in participants own race history, most recent event first, only once the results are published
    [HttpGet("api/results/me")]
    [SessionAuthorize(RoleNames.Participant)]
    public async Task<IActionResult> GetMyResults()
    {
        var userID = HttpContext.Session.GetCurrentUserID();

        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category)
            .Where(r => r.Enrolment.UserID == userID && r.Enrolment.Event.ResultsPublished)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .ToListAsync();

        //counts the finishers for each of those events in one query instead of loading every result
        var eventIDs = results.Select(r => r.Enrolment.EventID).Distinct().ToList();
        var finishersPerEvent = await _context.Results
            .Where(r => eventIDs.Contains(r.Enrolment.EventID))
            .GroupBy(r => r.Enrolment.EventID)
            .Select(g => new { EventID = g.Key, Total = g.Count() })
            .ToDictionaryAsync(g => g.EventID, g => g.Total);

        return Ok(results.Select(r => ParticipantResultResponse.FromResult(r, finishersPerEvent[r.Enrolment.EventID])));
    }
}