using System.Net;
using Users.Contracts;

namespace SystemTests;

/// <summary>
/// System tests for Users API endpoints.
/// </summary>
public class UsersControllerTests : BaseSystemTest
{
    [Fact]
    public async Task PostUsers_WithValidData_ReturnsCreatedWithUserId()
    {
        // Arrange
        await ClearDatabaseAsync();
        var command = new AddUserCommand("john.doe@example.com", "johndoe");

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        AddUserResult result = await FromJsonAsync<AddUserResult>(response);
        Assert.NotNull(result.UserId);
        Assert.NotEmpty(result.UserId);
        Assert.Equal("john.doe@example.com", result.Email);
        Assert.Equal("johndoe", result.UserName);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.True(result.CreatedAt >= DateTime.UtcNow.AddMinutes(-1)); // Ensure reasonable creation time

        // Verify Location header is set correctly
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(result.UserId, response.Headers.Location.ToString());
    }

    [Fact]
    public async Task PostUsers_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        await ClearDatabaseAsync();
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
        await ClearDatabaseAsync();
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
        await ClearDatabaseAsync();
        var command = new AddUserCommand("john.doe@example.com", "");

        // Act
        HttpResponseMessage response = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(command));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
