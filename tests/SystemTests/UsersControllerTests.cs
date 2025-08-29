using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SystemTests.TestObjectBuilders;
using Users.Contracts;
using Users.Domain;
using Users.Infrastructure;

namespace SystemTests;

/// <summary>
/// System tests for Users API endpoints.
/// </summary>
public class UsersControllerTests : BaseSystemTest
{
    private async Task<Guid> CreateUserAsync(AddUserCommand command)
    {
        HttpResponseMessage createResponse = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));
        AddUserResult createdUser = await FromJsonAsync<AddUserResult>(createResponse);
        return createdUser.UserId;
    }

    private async Task<Guid> CreateUserAsync()
    {
        AddUserCommand command = new AddUserCommandBuilder().Build();
        return await CreateUserAsync(command);
    }
    [Fact]
    public async Task PostUsers_WithValidData_ReturnsCreatedWithUserId()
    {
        // Arrange
        AddUserCommand command = new AddUserCommandBuilder()
            .WithEmail("john.doe@example.com")
            .WithUserName("johndoe")
            .Build();

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        AddUserResult result = await FromJsonAsync<AddUserResult>(response);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal("john.doe@example.com", result.Email);
        Assert.Equal("johndoe", result.UserName);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.True(result.CreatedAt >= DateTime.UtcNow.AddMinutes(-1));

        // Verify Location header is set correctly
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(result.UserId.ToString(), response.Headers.Location.ToString());
    }

    [Fact]
    public async Task PostUsers_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        AddUserCommand command = new AddUserCommandBuilder()
            .WithEmail("invalid-email")
            .Build();

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        AddUserCommand command = new AddUserCommandBuilder()
            .WithEmail("")
            .Build();

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_WithEmptyUserName_ReturnsBadRequest()
    {
        // Arrange
        AddUserCommand command = new AddUserCommandBuilder()
            .WithUserName("")
            .Build();

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsOkWithUserData()
    {
        // Arrange
        AddUserCommand addCommand = new AddUserCommandBuilder()
            .WithEmail("john.doe@example.com")
            .WithUserName("johndoe")
            .Build();
        HttpResponseMessage createResponse = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(addCommand));
        AddUserResult createdUser = await FromJsonAsync<AddUserResult>(createResponse);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/v1/users/{createdUser.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UserDto userDto = await FromJsonAsync<UserDto>(response);
        Assert.Equal(createdUser.UserId, userDto.UserId);
        Assert.Equal("john.doe@example.com", userDto.Email);
        Assert.Equal("johndoe", userDto.UserName);
        Assert.True(userDto.CreatedAt <= DateTime.UtcNow);
        Assert.True(userDto.CreatedAt >= DateTime.UtcNow.AddMinutes(-1));
        Assert.Null(userDto.LastLoginAt);
    }

    [Fact]
    public async Task GetUser_WhenUserNotFound_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WithInvalidUuidFormat_ReturnsBadRequest()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/api/v1/users/not-a-valid-uuid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WithEmptyUserId_ReturnsMethodNotAllowed()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/api/v1/users/");

        // Assert
        // When the route doesn't match (empty userId), ASP.NET returns MethodNotAllowed
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_UserExists_ReturnsNoContent()
    {
        // Arrange
        Guid userId = await CreateUserAsync();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_UserExists_RemovesUserFromDatabase()
    {
        // Arrange
        Guid userId = await CreateUserAsync();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify user is actually removed from database
        using IServiceScope scope = WebAppFactory.Services.CreateScope();
        UsersDbContext dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        User? userInDb = await dbContext.Users.FindAsync(new UserId(userId));
        Assert.Null(userInDb);
    }

    [Fact]
    public async Task DeleteUser_UserExists_VerifyUserCannotBeRetrievedAfterDeletion()
    {
        // Arrange
        Guid userId = await CreateUserAsync();

        // Act
        HttpResponseMessage deleteResponse = await HttpClient.DeleteAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify subsequent GET request returns NotFound
        HttpResponseMessage getResponse = await HttpClient.GetAsync($"/api/v1/users/{userId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_UserNotFound_ReturnsNotFound()
    {
        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/v1/users/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithInvalidUuidFormat_ReturnsBadRequest()
    {
        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync("/api/v1/users/not-a-valid-uuid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithEmptyUserId_ReturnsMethodNotAllowed()
    {
        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync("/api/v1/users/");

        // Assert
        // When the route doesn't match (empty userId), ASP.NET returns MethodNotAllowed
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_SameUserTwice_SecondDeleteReturnsNotFound()
    {
        // Arrange
        AddUserCommand command = new AddUserCommandBuilder().Build();
        HttpResponseMessage createResponse = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));
        AddUserResult createdUser = await FromJsonAsync<AddUserResult>(createResponse);

        // Act
        HttpResponseMessage firstDeleteResponse = await HttpClient.DeleteAsync($"/api/v1/users/{createdUser.UserId}");
        HttpResponseMessage secondDeleteResponse = await HttpClient.DeleteAsync($"/api/v1/users/{createdUser.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, firstDeleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);
    }
}
