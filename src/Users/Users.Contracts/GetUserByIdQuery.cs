namespace Users.Contracts;

/// <summary>
/// Query to retrieve a user by their unique identifier.
/// </summary>
/// <param name="UserId">The unique identifier of the user to retrieve.</param>
public sealed record GetUserByIdQuery(string UserId);

/// <summary>
/// Result of successfully retrieving a user by their identifier.
/// </summary>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="UserName">The user's username.</param>
/// <param name="CreatedAt">The date and time when the user was created.</param>
/// <param name="LastLoginAt">The date and time when the user last logged in, if applicable.</param>
public sealed record GetUserByIdResult(string UserId, string Email, string UserName, DateTime CreatedAt, DateTime? LastLoginAt);
