using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Tests.Helpers;

//runs the [Required], [EmailAddress], [MinLength] etc rules on a request
//[ApiController] checks these before the endpoint runs, calling the controller directly skips that, so this checks them on their own
public static class ModelValidation
{
    public static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();

        //true means every attribute gets checked, not just [Required]
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results;
    }
}