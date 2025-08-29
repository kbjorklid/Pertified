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
    /// <summary>
    /// Helper method to create a user via REST API and return the created user's ID.
    /// </summary>
    private async Task<Guid> CreateUserAsync(AddUserCommand command)
    {
        HttpResponseMessage createResponse = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));
        AddUserResult createdUser = await FromJsonAsync<AddUserResult>(createResponse);
        return createdUser.UserId;
    }

    /// <summary>
    /// Helper method to create a user with default test data via REST API and return the created user's ID.
    /// </summary>
    private async Task<Guid> CreateUserAsync()
    {
        AddUserCommand command = new AddUserCommandBuilder().Build();
        return await CreateUserAsync(command);
    }
    [Fact]
    public async Task PostUsers_WithValidData_ReturnsCreatedWithUserId()
    {
        // Arrange
        var command = new AddUserCommand("john.doe@example.com", "johndoe");

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        AddUserResult result = await FromJsonAsync<AddUserResult>(response);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal("john.doe@example.com", result.Email);
        Assert.Equal("johndoe", result.UserName);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.True(result.CreatedAt >= DateTime.UtcNow.AddMinutes(-1)); // Ensure reasonable creation time

        // Verify Location header is set correctly
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(result.UserId.ToString(), response.Headers.Location.ToString());
    }

    [Fact]
    public async Task PostUsers_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var command = new AddUserCommand("invalid-email", "johndoe");

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_WithEmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var command = new AddUserCommand("", "johndoe");

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_WithEmptyUserName_ReturnsBadRequest()
    {
        // Arrange
        var command = new AddUserCommand("john.doe@example.com", "");

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsOkWithUserData()
    {
        // Arrange
        var addCommand = new AddUserCommand("john.doe@example.com", "johndoe");
        Guid userId = await CreateUserAsync(addCommand);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Parse the response JSON - single objects are returned directly per REST conventions
        UserDto userDto = await FromJsonAsync<UserDto>(response);

        // Verify user properties (camelCase in JSON, but deserialized to UserDto)
        Assert.Equal(userId, userDto.UserId);
        Assert.Equal("john.doe@example.com", userDto.Email);
        Assert.Equal("johndoe", userDto.UserName);

        // Verify CreatedAt is recent and reasonable
        Assert.True(userDto.CreatedAt <= DateTime.UtcNow);
        Assert.True(userDto.CreatedAt >= DateTime.UtcNow.AddMinutes(-1));

        // lastLoginAt should be null for new users
        Assert.Null(userDto.LastLoginAt);
    }

    [Fact]
    public async Task GetUser_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        string nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/v1/users/{nonExistentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WithInvalidUuidFormat_ReturnsBadRequest()
    {
        // Arrange
        string invalidUserId = "not-a-valid-uuid";

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/v1/users/{invalidUserId}");

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
        // Arrange
        string nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/v1/users/{nonExistentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithInvalidUuidFormat_ReturnsBadRequest()
    {
        // Arrange
        string invalidUserId = "not-a-valid-uuid";

        // Act
        HttpResponseMessage response = await HttpClient.DeleteAsync($"/api/v1/users/{invalidUserId}");

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
        Guid userId = await CreateUserAsync();

        // Act
        HttpResponseMessage firstDeleteResponse = await HttpClient.DeleteAsync($"/api/v1/users/{userId}");
        HttpResponseMessage secondDeleteResponse = await HttpClient.DeleteAsync($"/api/v1/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, firstDeleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondDeleteResponse.StatusCode);
    }
}
