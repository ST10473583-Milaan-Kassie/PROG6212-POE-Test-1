using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

//what an organiser sends to capture a result, matches the request body in the endpoint plan
public class ResultRequest
{
    //sent as hh:mm:ss e.g. "07:45:12", nullable so leaving it out fails [Required]
    [Required]
    public TimeOnly? FinishTime { get; set; }

    //overall position in the event, checked in the controller
    [Required]
    public int? FinishingPosition { get; set; }
}