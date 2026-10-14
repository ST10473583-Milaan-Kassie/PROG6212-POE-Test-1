using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

//events, the public viewing endpoints are in here for now
//the organiser management endpoints get added in the next commit
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ISouthAfricanClock _clock;

    public EventsController(RaceDayDbContext context, ISouthAfricanClock clock)
    {
        _context = context;
        _clock = clock;
    }

    //GET /api/events
    //lists all events, anyone can see them, filters are all optional
    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] EventFilterQuery filter)
    {
        //a date range that ends before it starts cant match anything
        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
        {
            return BadRequest(new { message = "fromDate cannot be after toDate." });
        }

        //filtering by a type that doesnt exist is a mistake, not just an empty list
        if (filter.EventTypeID.HasValue &&
            !await _context.EventTypes.AnyAsync(et => et.EventTypeID == filter.EventTypeID.Value))
        {
            return BadRequest(new { message = "Event type does not exist." });
        }

        //type and organiser get loaded so their names can go in the response
        IQueryable<Event> query = _context.Events
            .Include(ev => ev.EventType)
            .Include(ev => ev.User);

        //each filter only gets added if it was sent
        if (filter.EventTypeID.HasValue)
        {
            query = query.Where(ev => ev.EventTypeID == filter.EventTypeID.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            //lowercase on both sides so case doesnt matter
            var location = filter.Location.Trim().ToLower();
            query = query.Where(ev => ev.Location.ToLower().Contains(location));
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(ev => ev.EventDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(ev => ev.EventDate <= filter.ToDate.Value);
        }

        if (filter.UpcomingOnly)
        {
            //after today in south africa, race day already counts as taken place
            var today = _clock.Today;
            query = query.Where(ev => ev.EventDate > today);
        }

        //soonest events first
        var events = await query
            .OrderBy(ev => ev.EventDate)
            .ThenBy(ev => ev.EventID)
            .ToListAsync();

        return Ok(events.Select(ev => EventResponse.FromEvent(ev, _clock.HasTakenPlace(ev.EventDate))));
    }

    //GET /api/events/{id}
    //one events details, anyone can see it
    //:int means only numbers match, so /api/events/mine wont get mixed up with this later
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.User)
            .SingleOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(EventResponse.FromEvent(ev, _clock.HasTakenPlace(ev.EventDate)));
    }
}