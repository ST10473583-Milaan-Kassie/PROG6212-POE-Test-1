using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

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