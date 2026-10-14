namespace RaceDay.Api.Dtos;

//the optional query parameters for GET /api/events, e.g. /api/events?eventTypeID=1&upcomingOnly=true
//anything left out just doesnt get filtered on
public class EventFilterQuery
{
    //only events of this type (run/walk/cycle)
    public int? EventTypeID { get; set; }

    //location contains this text, case doesnt matter
    public string? Location { get; set; }

    //events on or after this date
    public DateOnly? FromDate { get; set; }

    //events on or before this date
    public DateOnly? ToDate { get; set; }

    //true only shows events that havent taken place yet
    public bool UpcomingOnly { get; set; }
}