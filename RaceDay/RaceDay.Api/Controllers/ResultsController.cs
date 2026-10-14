using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

//results, organisers capture and view them, participants view their own
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

    //POST /api/enrolments/{enrolmentId}/results
    //captures a result for a confirmed enrolment on the logged in organisers own event, once its taken place
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> CaptureResult(int enrolmentId, ResultRequest request)
    {
        //event for the owner and date checks, user and category for the response, result for the duplicate check
        var enrolment = await _context.Enrolments
            .Include(en => en.Event)
            .Include(en => en.User)
            .Include(en => en.Category)
            .Include(en => en.Result)
            .SingleOrDefaultAsync(en => en.EnrolmentID == enrolmentId);

        if (enrolment == null)
        {
            return NotFound(new { message = "Enrolment not found." });
        }

        if (enrolment.Event.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return ApiResponses.Forbidden("You can only capture results for your own events.");
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

        if (request.FinishTime!.Value == TimeOnly.MinValue)
        {
            return BadRequest(new { message = "Finish time must be more than 00:00:00." });
        }

        //setting the enrolment sets the enrolmentid too
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

    //GET /api/events/{eventId}/results
    //all results for the logged in organisers own event, in finishing order
    [HttpGet("api/events/{eventId:int}/results")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return ApiResponses.Forbidden("You can only view results for your own events.");
        }

        //participant and category come through the enrolment
        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.User)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category)
            .Where(r => r.Enrolment.EventID == eventId)
            .OrderBy(r => r.FinishingPosition)
            .ThenBy(r => r.FinishTime)
            .ToListAsync();

        return Ok(results.Select(EventResultResponse.FromResult));
    }

    //GET /api/results/me
    //the logged in participants own race history, most recent event first
    [HttpGet("api/results/me")]
    [SessionAuthorize(RoleNames.Participant)]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = HttpContext.Session.GetCurrentUserID();

        var results = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(en => en.Event)
            .Include(r => r.Enrolment).ThenInclude(en => en.Category)
            .Where(r => r.Enrolment.UserID == userId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .ToListAsync();

        //counts the finishers for each of those events in one query instead of loading every result
        var eventIds = results.Select(r => r.Enrolment.EventID).Distinct().ToList();
        var finishersPerEvent = await _context.Results
            .Where(r => eventIds.Contains(r.Enrolment.EventID))
            .GroupBy(r => r.Enrolment.EventID)
            .Select(g => new { EventID = g.Key, Total = g.Count() })
            .ToDictionaryAsync(g => g.EventID, g => g.Total);

        return Ok(results.Select(r => ParticipantResultResponse.FromResult(r, finishersPerEvent[r.Enrolment.EventID])));
    }
}