using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Requests;

//what an organiser sends to change an enrolments status, matches the request body in the endpoint plan
public class UpdateEnrolmentStatusRequest
{
    //either pending or confirmed, checked in the controller
    [Required]
    public string Status { get; set; } = string.Empty;
}