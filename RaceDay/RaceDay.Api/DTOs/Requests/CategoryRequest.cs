using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Requests;

//what an organiser sends to add or update a category, matches the request body in the endpoint plan
//age range, distance or both, the controller checks at least one was sent
public class CategoryRequest
{
    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    //optional, leave it out for a distance only category
    public int? MinAge { get; set; }

    //optional, null means there is no maximum age
    public int? MaxAge { get; set; }

    //optional, in km, leave it out for an age only category
    public decimal? Distance { get; set; }
}