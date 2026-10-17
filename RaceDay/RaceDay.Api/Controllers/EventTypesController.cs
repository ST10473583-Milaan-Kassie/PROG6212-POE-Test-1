using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Responses;

namespace RaceDay.Api.Controllers;

/// <summary>
/// The event type lookup: Run, Walk and Cycle.
/// </summary>
//the event types lookup, no login needed
[ApiController]
[Route("api/event-types")]
public class EventTypesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventTypesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lists all event types.
    /// </summary>
    /// <remarks>
    /// No login is required. Used by the front end for its event forms and filters.
    /// </remarks>
    /// <response code="200">The event types.</response>
    [ProducesResponseType<IEnumerable<EventTypeResponse>>(StatusCodes.Status200OK)]
    //GET /api/event-types
    //lists run, walk and cycle for the front end's forms and filters
    [HttpGet]
    public async Task<IActionResult> GetEventTypes()
    {
        var eventTypes = await _context.EventTypes
            .OrderBy(et => et.EventTypeID)
            .Select(et => new EventTypeResponse
            {
                EventTypeID = et.EventTypeID,
                EventTypeName = et.EventTypeName
            })
            .ToListAsync();

        return Ok(eventTypes);
    }
}