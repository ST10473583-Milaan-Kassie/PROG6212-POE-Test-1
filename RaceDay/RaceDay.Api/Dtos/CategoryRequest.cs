using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

//what an organiser sends to add or update a category, matches the request body in the endpoint plan
public class CategoryRequest
{
    [Required, MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    //nullable so leaving it out fails [Required] instead of quietly becoming 0
    [Required]
    public int? MinAge { get; set; }

    //optional, null means there is no maximum age
    public int? MaxAge { get; set; }
}