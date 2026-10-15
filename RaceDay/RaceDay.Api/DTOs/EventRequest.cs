using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

//what an organiser sends to create or update an event, matches the request body in the endpoint plan
//no UserID on purpose, the organiser is always whoever is logged in
public class EventRequest
{
    [Required, MaxLength(100)]
    public string EventName { get; set; } = string.Empty;

    //optional
    [MaxLength(500)]
    public string? Description { get; set; }

    //nullable so leaving it out fails [Required] instead of quietly becoming 0001-01-01
    [Required]
    public DateOnly? EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    //in km, the range and decimal places get checked in the controller
    [Required]
    public decimal? Distance { get; set; }

    //has to be an existing event type, checked in the controller
    [Required]
    public int? EventTypeID { get; set; }
}