using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs.Requests;
using RaceDay.Api.DTOs.Responses;
using RaceDay.Api.Helpers;
using RaceDay.Api.Tests.Helpers;

namespace RaceDay.Api.Tests;

//the logged in users own profile, viewing it, updating it and uploading a profile picture
//none of the seed users have a profile picture yet
public class UsersControllerTests
{
    //the users controller with the given fake storage and a fake session, nobody logged in yet
    //the storage gets passed in so the tests can check what was uploaded and deleted
    private static UsersController CreateController(RaceDayDbContext context, FakeFileStorageService storage)
    {
        var controller = new UsersController(context, storage);
        ControllerTestHelper.AttachSession(controller);
        return controller;
    }

    //alucards current details with whatever phone number the test wants
    private static UpdateProfileRequest AlucardRequest(string? phoneNumber)
    {
        return new UpdateProfileRequest
        {
            FirstName = "Alucard",
            LastName = "Tepes",
            PhoneNumber = phoneNumber
        };
    }

    //viewing

    [Fact]
    public async Task GetMyProfile_LoggedIn_ReturnsOwnProfile()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context, new FakeFileStorageService());
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.GetMyProfile();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal(SeedIDs.Alucard, response.UserID);
        Assert.Equal("Alucard", response.FirstName);
        Assert.Equal("floatingsword@gmail.com", response.Email);
        Assert.Equal(RoleNames.Participant, response.RoleName);
        Assert.Null(response.ProfileImageURL);
    }

    //updating

    [Fact]
    public async Task UpdateMyProfile_ValidDetails_Returns200AndSavesChanges()
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context, new FakeFileStorageService());
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //spaces around the values get trimmed off before saving
        var request = new UpdateProfileRequest
        {
            FirstName = "  Adrian ",
            LastName = "Tepes",
            PhoneNumber = " +27 73 112 2334 "
        };

        var result = await controller.UpdateMyProfile(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal("Adrian", response.FirstName);

        //reads straight from the database to make sure the change was actually saved
        context.ChangeTracker.Clear();
        var saved = context.Users.Single(u => u.UserID == SeedIDs.Alucard);
        Assert.Equal("Adrian", saved.FirstName);
        Assert.Equal("+27 73 112 2334", saved.PhoneNumber);

        //email and date of birth cant be changed through the profile
        Assert.Equal("floatingsword@gmail.com", saved.Email);
        Assert.Equal(new DateOnly(1998, 5, 2), saved.DateOfBirth);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UpdateMyProfile_BlankPhoneNumber_SavesAsNull(string? phoneNumber)
    {
        using var context = TestDbContextFactory.Create();
        var controller = CreateController(context, new FakeFileStorageService());
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.UpdateMyProfile(AlucardRequest(phoneNumber));

        Assert.Equal(StatusCodes.Status200OK, ResultHelper.GetStatusCode(result));

        //alucard had a phone number in the seed data, now the column is empty
        context.ChangeTracker.Clear();
        Assert.Null(context.Users.Single(u => u.UserID == SeedIDs.Alucard).PhoneNumber);
    }

    [Fact]
    public void UpdateProfileRequest_PhoneNumberWithLetters_FailsValidation()
    {
        var results = ModelValidation.Validate(AlucardRequest("073 ABC 2334"));

        var error = Assert.Single(results);
        Assert.Equal("Phone number may only contain digits, spaces and an optional leading +.", error.ErrorMessage);
    }

    [Fact]
    public void UpdateProfileRequest_PlusInTheMiddle_FailsValidation()
    {
        //the + is only allowed right at the start
        var results = ModelValidation.Validate(AlucardRequest("073+112+2334"));

        Assert.Single(results);
    }

    //profile picture

    [Fact]
    public async Task UploadProfilePicture_ValidPng_Returns200AndSavesURL()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.UploadProfilePicture(new ImageUploadRequest { File = TestFiles.Png() });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserResponse>(ok.Value);

        //the URL storage gave back is the one sent back and saved
        var uploadedURL = Assert.Single(storage.UploadedURLs);
        Assert.Equal(uploadedURL, response.ProfileImageURL);
        Assert.StartsWith("https://storage.test/raceday-images/profile-pictures/", uploadedURL);
        Assert.EndsWith(".png", uploadedURL);

        context.ChangeTracker.Clear();
        Assert.Equal(uploadedURL, context.Users.Single(u => u.UserID == SeedIDs.Alucard).ProfileImageURL);

        //there was no old picture so nothing got deleted
        Assert.Empty(storage.DeletedURLs);
    }

    [Fact]
    public async Task UploadProfilePicture_ReplacingPicture_DeletesOldOne()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        await controller.UploadProfilePicture(new ImageUploadRequest { File = TestFiles.Png("first.png") });
        await controller.UploadProfilePicture(new ImageUploadRequest { File = TestFiles.Png("second.png") });

        //the first picture got deleted once the second one was saved
        Assert.Equal(2, storage.UploadedURLs.Count);
        var deletedURL = Assert.Single(storage.DeletedURLs);
        Assert.Equal(storage.UploadedURLs[0], deletedURL);

        context.ChangeTracker.Clear();
        Assert.Equal(storage.UploadedURLs[1], context.Users.Single(u => u.UserID == SeedIDs.Alucard).ProfileImageURL);
    }

    [Fact]
    public async Task UploadProfilePicture_NoFile_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.UploadProfilePicture(new ImageUploadRequest { File = null });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Please upload an image file.", ResultHelper.GetMessage(result));
        Assert.Empty(storage.UploadedURLs);
    }

    [Fact]
    public async Task UploadProfilePicture_WrongFileType_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        var result = await controller.UploadProfilePicture(new ImageUploadRequest { File = TestFiles.Text() });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Only JPG, PNG, or WEBP images are allowed.", ResultHelper.GetMessage(result));
        Assert.Empty(storage.UploadedURLs);
    }

    [Fact]
    public async Task UploadProfilePicture_LargerThan5MB_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //one byte over the limit
        var file = TestFiles.Png(size: ImageValidation.MaxFileSizeBytes + 1);

        var result = await controller.UploadProfilePicture(new ImageUploadRequest { File = file });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("Images can be no larger than 5 MB.", ResultHelper.GetMessage(result));
        Assert.Empty(storage.UploadedURLs);
    }

    [Fact]
    public async Task UploadProfilePicture_RenamedFileThatIsNotAnImage_Returns400()
    {
        using var context = TestDbContextFactory.Create();
        var storage = new FakeFileStorageService();
        var controller = CreateController(context, storage);
        ControllerTestHelper.LogInAs(controller, SeedIDs.Alucard, RoleNames.Participant);

        //ends in .png but the bytes inside arent a png
        var result = await controller.UploadProfilePicture(new ImageUploadRequest { File = TestFiles.FakePng() });

        Assert.Equal(StatusCodes.Status400BadRequest, ResultHelper.GetStatusCode(result));
        Assert.Equal("The file is not a valid JPG, PNG or WEBP image.", ResultHelper.GetMessage(result));
        Assert.Empty(storage.UploadedURLs);
    }
}