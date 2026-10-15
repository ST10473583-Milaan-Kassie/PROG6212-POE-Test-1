namespace RaceDay.Api.DTOs;

//the optional query parameters for GET /api/events, e.g. /api/events?eventTypeID=1&upcomingOnly=true
//anything left out just doesnt get filtered on
public class EventFilterQuery
{
    /// <summary>Only show events of this type (1 Run, 2 Walk, 3 Cycle).</summary>
    //only events of this type (run/walk/cycle)
    public int? EventTypeID { get; set; }

    /// <summary>Only show events whose location contains this text. Case does not matter.</summary>
    //location contains this text, case doesnt matter
    public string? Location { get; set; }

    /// <summary>Only show events on or after this date (yyyy-MM-dd).</summary>
    //events on or after this date
    public DateOnly? FromDate { get; set; }

    /// <summary>Only show events on or before this date (yyyy-MM-dd).</summary>
    //events on or before this date
    public DateOnly? ToDate { get; set; }

    /// <summary>Only show events that have not taken place yet, measured in South African time.</summary>
    //true only shows events that havent taken place yet
    public bool UpcomingOnly { get; set; }
}