using RaceDay.Api.DTOs.Requests;

namespace RaceDay.Api.Tests;

//the [Required], [EmailAddress] and length rules on the register request
//[ApiController] turns these into a 400 before the endpoint runs, so theyre tested here on their own
public class RegisterRequestValidationTests
{
    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var request = TestRequests.ValidRegisterRequest();

        var errors = ModelValidation.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void PasswordShorterThan8_FailsOnPassword()
    {
        var request = TestRequests.ValidRegisterRequest();
        request.Password = "Pass12";

        var errors = ModelValidation.Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void InvalidEmail_FailsOnEmail()
    {
        var request = TestRequests.ValidRegisterRequest();
        request.Email = "not-an-email";

        var errors = ModelValidation.Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterRequest.Email)));
    }

    [Fact]
    public void MissingDateOfBirth_FailsOnDateOfBirth()
    {
        var request = TestRequests.ValidRegisterRequest();
        request.DateOfBirth = null;

        var errors = ModelValidation.Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterRequest.DateOfBirth)));
    }
}