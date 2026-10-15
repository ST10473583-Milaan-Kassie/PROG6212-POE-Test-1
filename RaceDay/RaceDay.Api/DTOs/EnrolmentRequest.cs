using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

//what a participant sends to enter an event, matches the request body in the endpoint plan
//the event comes from the URL and the participant comes from the session
public class EnrolmentRequest
{
    //nullable so leaving it out fails [Required] instead of quietly becoming 0
    [Required]
    public int? CategoryID { get; set; }
}