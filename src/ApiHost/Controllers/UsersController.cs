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

        return HandleError(result.Error);

    }

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>User information.</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUser(string userId)
    {
        var query = new GetUserByIdQuery(userId);
        Result<UserDto> result = await _messageBus.InvokeAsync<Result<UserDto>>(query);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return HandleError(result.Error);
    }

    private IActionResult HandleError(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => CreateValidationProblem(error),
            ErrorType.NotFound => NotFound(CreateProblemDetails("Resource not found", error)),
            _ => Problem(
                title: "An error occurred while processing the request",
                detail: error.Description,
                statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private IActionResult CreateValidationProblem(Error error)
    {
        ModelState.AddModelError(string.Empty, error.Description);
        return ValidationProblem();
    }

    private ProblemDetails CreateProblemDetails(string title, Error error)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = error.Description,
            Status = GetStatusCodeForErrorType(error.Type)
        };
    }

    private static int GetStatusCodeForErrorType(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
