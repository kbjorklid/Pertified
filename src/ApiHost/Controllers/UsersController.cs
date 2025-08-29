using Base.Domain.Result;
using Microsoft.AspNetCore.Mvc;
using Users.Contracts;
using Wolverine;

namespace ApiHost.Controllers;

/// <summary>
/// REST API controller for user management operations.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IMessageBus _messageBus;

    public UsersController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    /// <summary>
    /// Creates a new user in the system.
    /// </summary>
    /// <param name="command">The add user command containing email and username.</param>
    /// <returns>The created user information.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddUserResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddUser([FromBody] AddUserCommand command)
    {

        Result<AddUserResult> result = await _messageBus.InvokeAsync<Result<AddUserResult>>(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetUser),
                new { userId = result.Value.UserId },
                result.Value);
        }

        // Handle validation errors
        if (result.Error.Type == ErrorType.Validation)
        {
            ModelState.AddModelError(string.Empty, result.Error.Description);
            return ValidationProblem();
        }

        // Handle other errors
        return Problem(
            title: "An error occurred while creating the user",
            detail: result.Error.Description,
            statusCode: StatusCodes.Status500InternalServerError);

    }

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>User information.</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUser(string userId)
    {
        var query = new GetUserByIdQuery(userId);
        Result<UserDto> result = await _messageBus.InvokeAsync<Result<UserDto>>(query);

        if (result.IsSuccess)
        {
            UserDto userResult = result.Value;
            var response = new
            {
                data = new
                {
                    userId = userResult.UserId,
                    email = userResult.Email,
                    userName = userResult.UserName,
                    createdAt = userResult.CreatedAt,
                    lastLoginAt = userResult.LastLoginAt
                }
            };
            return Ok(response);
        }

        switch (result.Error.Type)
        {
            case ErrorType.Validation:
                ModelState.AddModelError(nameof(userId), result.Error.Description);
                return ValidationProblem();
            case ErrorType.NotFound:
                return NotFound(new ProblemDetails
                {
                    Title = "User not found",
                    Detail = result.Error.Description,
                    Status = StatusCodes.Status404NotFound
                });
            default:
                return Problem(
                    title: "An error occurred while retrieving the user",
                    detail: result.Error.Description,
                    statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
