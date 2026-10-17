using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Requests;
using RaceDay.Api.DTOs.Responses;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Viewing events, and managing events for Organisers.
/// </summary>
//events, the public viewing endpoints and the organiser management endpoints
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly ISouthAfricanClock _clock;
    private readonly IFileStorageService _storage;

    public EventsController(RaceDayDbContext context, ISouthAfricanClock clock, IFileStorageService storage)
    {
        _context = context;
        _clock = clock;
        _storage = storage;
    }

    /// <summary>
    /// Lists all events, with optional filters.
    /// </summary>
    /// <remarks>
    /// No login is required. Filters can be combined, and any filter left out is not applied.
    /// Events are returned soonest first, and hasTakenPlace is worked out in South African time.
    /// </remarks>
    /// <response code="200">The matching events.</response>
    /// <response code="400">The event type does not exist, fromDate is after toDate, or a value could not be read.</response>
    [ProducesResponseType<IEnumerable<EventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// Returns a single event.
    /// </summary>
    /// <remarks>
    /// No login is required.
    /// </remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">The event.</response>
    /// <response code="404">No event has this ID.</response>
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    //GET /api/events/{id}
    //one events details, anyone can see it
    //:int means only numbers match, so /api/events/mine doesnt get mixed up with this
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

    /// <summary>
    /// Lists the logged-in Organiser's own events, with the total and confirmed enrolments for each.
    /// </summary>
    /// <remarks>
    /// Organisers only. totalEnrolments counts both Pending and Confirmed enrolments, and confirmedEnrolments counts only the Confirmed ones.
    /// </remarks>
    /// <response code="200">The Organiser's events.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser.</response>
    [ProducesResponseType<IEnumerable<EventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    //GET /api/events/mine
    //the logged in organisers own events, with how many people have enrolled in each and how many are confirmed
    [HttpGet("mine")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> GetMyEvents()
    {
        var userID = HttpContext.Session.GetCurrentUserID();

        var events = await _context.Events
            .Include(ev => ev.EventType)
            .Include(ev => ev.User)
            .Where(ev => ev.UserID == userID)
            .OrderBy(ev => ev.EventDate)
            .ThenBy(ev => ev.EventID)
            .ToListAsync();

        //counts the enrolments per event in one query instead of loading every enrolment
        var eventIDs = events.Select(ev => ev.EventID).ToList();
        var enrolmentCounts = await _context.Enrolments
            .Where(en => eventIDs.Contains(en.EventID))
            .GroupBy(en => en.EventID)
            .Select(g => new
            {
                EventID = g.Key,
                Total = g.Count(),
                Confirmed = g.Count(en => en.Status == EnrolmentStatuses.Confirmed)
            })
            .ToDictionaryAsync(g => g.EventID);

        var response = events.Select(ev =>
        {
            var eventResponse = EventResponse.FromEvent(ev, _clock.HasTakenPlace(ev.EventDate));

            //events with no enrolments arent in the dictionary so they get 0
            var counts = enrolmentCounts.GetValueOrDefault(ev.EventID);
            eventResponse.TotalEnrolments = counts?.Total ?? 0;
            eventResponse.ConfirmedEnrolments = counts?.Confirmed ?? 0;
            return eventResponse;
        });

        return Ok(response);
    }

    /// <summary>
    /// Creates a new event owned by the logged-in Organiser.
    /// </summary>
    /// <remarks>
    /// Organisers only. The event date must be after today, measured in South African time.
    /// Distance must be between 0.01 and 9999.99 km with at most 2 decimal places, and the event type must exist.
    /// </remarks>
    /// <param name="request">The new event's details.</param>
    /// <response code="201">The event was created.</response>
    /// <response code="400">A field is missing or invalid, the date is not after today, or the event type does not exist.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser.</response>
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    //POST /api/events
    //creates a new event owned by the logged in organiser
    [HttpPost]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var validationError = ValidateEventDetails(request);
        if (validationError != null)
        {
            return validationError;
        }

        var eventType = await _context.EventTypes.FindAsync(request.EventTypeID!.Value);
        if (eventType == null)
        {
            return BadRequest(new { message = "Event type does not exist." });
        }

        var ev = new Event
        {
            EventName = request.EventName.Trim(),
            EventType = eventType,

            //the organiser is always whoever is logged in, never taken from the request
            UserID = HttpContext.Session.GetCurrentUserID(),

            //blank description gets saved as null
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            Distance = request.Distance!.Value
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        //loads the organiser so their name can go in the response
        await _context.Entry(ev).Reference(e => e.User).LoadAsync();

        var response = EventResponse.FromEvent(ev, _clock.HasTakenPlace(ev.EventDate));

        //201 with a link to GET /api/events/{id} for the new event
        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, response);
    }

    /// <summary>
    /// Updates the logged-in Organiser's own upcoming event.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. An event cannot be changed once it has taken place, and race day counts.
    /// The new date must be after today. If the date changes, every enrolled participant's age on the new date must still fit their category.
    /// The new distance cannot be shorter than any of the event's categories that have a distance.
    /// </remarks>
    /// <param name="id">The event's ID.</param>
    /// <param name="request">The event's details. Every field is sent, including any that have not changed.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="400">A field is missing or invalid, the date is not after today, or the event type does not exist.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    /// <response code="409">The event has already taken place, the new date would leave enrolments outside their category's age range, or the new distance is shorter than one of its categories.</response>
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //PUT /api/events/{id}
    //updates the logged in organisers own event, only while its still upcoming
    [HttpPut("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.User)
            .SingleOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        //organisers can only change their own events
        if (ev.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return APIResponses.Forbidden("You can only manage your own events.");
        }

        //once race day comes the event is locked
        if (_clock.HasTakenPlace(ev.EventDate))
        {
            return Conflict(new { message = "This event has already taken place and can no longer be changed." });
        }

        var validationError = ValidateEventDetails(request);
        if (validationError != null)
        {
            return validationError;
        }

        var eventType = await _context.EventTypes.FindAsync(request.EventTypeID!.Value);
        if (eventType == null)
        {
            return BadRequest(new { message = "Event type does not exist." });
        }

        //moving the date changes everyones age on race day, so enrolled participants have to still fit their category
        //skipped completely if the date isnt changing
        var newEventDate = request.EventDate!.Value;
        if (newEventDate != ev.EventDate)
        {
            var enrolments = await _context.Enrolments
                .Where(en => en.EventID == ev.EventID)
                .Select(en => new
                {
                    en.User.DateOfBirth,
                    en.Category.MinAge,
                    en.Category.MaxAge
                })
                .ToListAsync();

            var outsideRange = enrolments.Count(en =>
                !AgeHelper.IsAgeEligible(en.DateOfBirth, newEventDate, en.MinAge, en.MaxAge));

            if (outsideRange > 0)
            {
                var participants = outsideRange == 1 ? "participant" : "participants";
                return Conflict(new { message = $"This change would leave {outsideRange} enrolled {participants} outside their category's age range." });
            }
        }

        //a category cant be longer than its event, so the event cant be shortened below any of its categories
        //only checked when the distance is getting shorter, categories with no distance never count
        var newDistance = request.Distance!.Value;
        if (newDistance < ev.Distance)
        {
            var longerCategories = await _context.Categories
                .CountAsync(c => c.EventID == ev.EventID && c.Distance > newDistance);

            if (longerCategories > 0)
            {
                var categories = longerCategories == 1 ? "category" : "categories";
                return Conflict(new { message = $"This change would leave {longerCategories} {categories} longer than the event's distance." });
            }
        }

        ev.EventName = request.EventName.Trim();

        //setting the type itself keeps the ID and the name in the response in sync
        ev.EventType = eventType;
        ev.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.Distance = request.Distance!.Value;

        await _context.SaveChangesAsync();

        return Ok(EventResponse.FromEvent(ev, _clock.HasTakenPlace(ev.EventDate)));
    }

    /// <summary>
    /// Deletes the logged-in Organiser's own upcoming event, along with its categories and enrolments.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. The event, its categories and its enrolments are deleted together in one transaction.
    /// Events that have taken place are kept as history and cannot be deleted.
    /// </remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="204">The event was deleted.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    /// <response code="409">The event has already taken place.</response>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status409Conflict)]
    //DELETE /api/events/{id}
    //deletes the logged in organisers own upcoming event, along with its categories and enrolments
    [HttpDelete("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        //categories and enrolments get loaded so they can be deleted along with the event
        var ev = await _context.Events
            .Include(e => e.Categories)
            .Include(e => e.Enrolments)
            .SingleOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return APIResponses.Forbidden("You can only manage your own events.");
        }

        //events that have taken place are kept as history
        if (_clock.HasTakenPlace(ev.EventDate))
        {
            return Conflict(new { message = "This event has already taken place and can no longer be deleted." });
        }

        //all three go in one SaveChanges so its one transaction, if anything fails nothing gets deleted
        //EF works out the order itself, enrolments then categories then the event
        //no results to remove, results only exist for events that have taken place and those cant be deleted
        _context.Enrolments.RemoveRange(ev.Enrolments);
        _context.Categories.RemoveRange(ev.Categories);
        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Uploads or replaces the banner image for the logged-in Organiser's own event.
    /// </summary>
    /// <remarks>
    /// Organisers only, and only for their own events. Send the image as multipart/form-data in the File field.
    /// It must be a JPG, PNG or WEBP file no larger than 5 MB. Banners can still be uploaded after the event has taken place.
    /// Any previous banner is deleted from storage.
    /// </remarks>
    /// <param name="id">The event's ID.</param>
    /// <param name="request">The banner image, sent in the File field.</param>
    /// <response code="200">The updated event, with the new banner's URL.</response>
    /// <response code="400">The file is missing, larger than 5 MB, not a JPG, PNG or WEBP, or not a valid image.</response>
    /// <response code="401">Nobody is logged in.</response>
    /// <response code="403">The logged-in user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has this ID.</response>
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status404NotFound)]
    //POST /api/events/{id}/banner
    //uploads or replaces the banner on the logged in organisers own event
    //no taken place check, banners can still go up after race day like the plan says
    [HttpPost("{id:int}/banner")]
    [SessionAuthorize(RoleNames.Organiser)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBanner(int id, [FromForm] ImageUploadRequest request)
    {
        //type and organiser get loaded for the response
        var ev = await _context.Events
            .Include(e => e.EventType)
            .Include(e => e.User)
            .SingleOrDefaultAsync(e => e.EventID == id);

        if (ev == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        if (ev.UserID != HttpContext.Session.GetCurrentUserID())
        {
            return APIResponses.Forbidden("You can only manage your own events.");
        }

        //jpg, png or webp up to 5 mb
        var imageError = ImageValidation.Validate(request.File);
        if (imageError != null)
        {
            return BadRequest(new { message = imageError });
        }

        //remembers the old banner so it can be removed once the new one is saved
        var oldBannerURL = ev.BannerImageURL;

        ev.BannerImageURL = await _storage.UploadAsync(request.File!, "event-banners");
        await _context.SaveChangesAsync();

        //deleted after saving, so if the save failed the old banner would still work
        if (oldBannerURL != null)
        {
            await _storage.DeleteAsync(oldBannerURL);
        }

        return Ok(EventResponse.FromEvent(ev, _clock.HasTakenPlace(ev.EventDate)));
    }

    //checks the parts of an event request that the annotations cant, used by create and update
    private BadRequestObjectResult? ValidateEventDetails(EventRequest request)
    {
        //events have to be after today, tomorrow at the earliest
        //race day counts as taken place so an event dated today would be locked straight away
        if (request.EventDate!.Value <= _clock.Today)
        {
            return BadRequest(new { message = "Event date must be after today." });
        }

        var distance = request.Distance!.Value;

        //has to fit DECIMAL(6,2), above 0 and 9999.99 at most
        if (distance <= 0 || distance > 9999.99m)
        {
            return BadRequest(new { message = "Distance must be between 0.01 and 9999.99 km." });
        }

        //SQL would quietly round anything past 2 decimals so its rejected instead
        if (decimal.Round(distance, 2) != distance)
        {
            return BadRequest(new { message = "Distance can have at most 2 decimal places." });
        }

        return null;
    }
}