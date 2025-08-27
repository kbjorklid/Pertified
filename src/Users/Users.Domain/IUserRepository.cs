namespace Users.Domain;

/// <summary>
/// Defines the contract for User aggregate persistence and retrieval operations following repository pattern principles.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId userId);

    Task<User?> GetByEmailAsync(Email email);

    Task<User?> GetByUserNameAsync(UserName userName);

    Task AddAsync(User user);

    Task UpdateAsync(User user);
}
