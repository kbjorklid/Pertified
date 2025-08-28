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
        try
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
        catch (Exception ex)
        {
            return Problem(
                title: "An unexpected error occurred",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Placeholder for getting a user by ID (referenced in CreatedAtAction).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>User information (not implemented yet).</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetUser(string userId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, "Get user endpoint not yet implemented");
    }
}
