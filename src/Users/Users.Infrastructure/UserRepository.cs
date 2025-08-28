using Microsoft.EntityFrameworkCore;
using Users.Domain;

namespace Users.Infrastructure;

/// <summary>
/// EF Core implementation of the User repository for persistence and retrieval operations.
/// </summary>
internal sealed class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByIdAsync(UserId userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUserNameAsync(UserName userName)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task AddAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _context.Users.AddAsync(user);
    }

    public Task UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _context.Users.Update(user);
        return Task.CompletedTask;
    }
}
