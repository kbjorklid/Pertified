using Base.Domain.Result;
using Users.Contracts;
using Users.Domain;

namespace Users.Application.Commands;

/// <summary>
/// Handles the AddUserCommand to create new users in the system.
/// </summary>
public static class AddUserCommandHandler
{
    /// <summary>
    /// Handles the command to add a new user using Wolverine's preferred static method pattern.
    /// </summary>
    /// <param name="command">The add user command containing email and username.</param>
    /// <param name="userRepository">The user repository injected by Wolverine.</param>
    /// <returns>A Result containing the AddUserResult if successful, or an error if validation fails.</returns>
    public static async Task<Result<AddUserResult>> Handle(AddUserCommand command, IUserRepository userRepository)
    {
        Result<User> userResult = User.Register(command.Email, command.UserName, DateTime.UtcNow);
        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        User user = userResult.Value;

        User? existingEmailUser = await userRepository.GetByEmailAsync(user.Email);
        if (existingEmailUser is not null)
        {
            return new Error(User.Codes.EmailAlreadyInUse, "A user with this email address already exists.", ErrorType.Validation);
        }

        User? existingUserNameUser = await userRepository.GetByUserNameAsync(user.UserName);
        if (existingUserNameUser is not null)
        {
            return new Error(User.Codes.UserNameAlreadyInUse, "A user with this username already exists.", ErrorType.Validation);
        }

        await userRepository.AddAsync(user);
        await userRepository.SaveChangesAsync();

        return new AddUserResult(
            user.Id.Value,
            user.Email.Value.Address,
            user.UserName.Value,
            user.CreatedAt
        );
    }
}
