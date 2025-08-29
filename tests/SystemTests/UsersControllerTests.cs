using System.Net;
using System.Text.Json;
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

    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsOkWithUserData()
    {
        // Arrange
        await ClearDatabaseAsync();

        // Create a user first via the POST endpoint
        var addCommand = new AddUserCommand("john.doe@example.com", "johndoe");
        HttpResponseMessage createResponse = await HttpClient.PostAsync("/api/v1/users", ToJsonContent(addCommand));
        AddUserResult createdUser = await FromJsonAsync<AddUserResult>(createResponse);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/v1/users/{createdUser.UserId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Parse the response JSON to verify the data wrapper structure
        string responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        JsonElement root = doc.RootElement;

        // Verify response has "data" wrapper
        Assert.True(root.TryGetProperty("data", out JsonElement dataElement));

        // Verify user properties within the data element (camelCase)
        Assert.Equal(createdUser.UserId.ToString(), dataElement.GetProperty("userId").GetString());
        Assert.Equal("john.doe@example.com", dataElement.GetProperty("email").GetString());
        Assert.Equal("johndoe", dataElement.GetProperty("userName").GetString());

        // Compare dates with some tolerance for precision differences
        DateTime expectedCreatedAt = createdUser.CreatedAt;
        DateTime actualCreatedAt = dataElement.GetProperty("createdAt").GetDateTime();
        Assert.True(Math.Abs((expectedCreatedAt - actualCreatedAt).TotalSeconds) < 1,
            $"CreatedAt dates differ by more than 1 second. Expected: {expectedCreatedAt}, Actual: {actualCreatedAt}");

        // lastLoginAt should be null for new users
        JsonElement lastLoginElement = dataElement.GetProperty("lastLoginAt");
        Assert.Equal(JsonValueKind.Null, lastLoginElement.ValueKind);
    }

    [Fact]
    public async Task GetUser_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();
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
        await ClearDatabaseAsync();
        string invalidUserId = "not-a-valid-uuid";

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"/api/v1/users/{invalidUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WithEmptyUserId_ReturnsMethodNotAllowed()
    {
        // Arrange
        await ClearDatabaseAsync();

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync("/api/v1/users/");

        // Assert
        // When the route doesn't match (empty userId), ASP.NET returns MethodNotAllowed
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
