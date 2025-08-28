using Base.Domain;
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

    public async Task<PagedResult<User>> GetAllAsync(UserQueryCriteria criteria)
    {
        // Build the base query with filters
        IQueryable<User> filteredQuery = BuildFilteredQuery(_context.Users.AsQueryable(), criteria.EmailFilter, criteria.UserNameFilter);

        // Get total count for filtered results (optimized - no sorting needed for count)
        int totalItems = await filteredQuery.CountAsync();

        // Early return if no results
        if (totalItems == 0)
        {
            return PagedResult<User>.Empty(criteria.PagingParameters);
        }

        // Apply sorting and pagination
        IOrderedQueryable<User> sortedQuery = ApplySorting(filteredQuery, criteria.SortBy, criteria.Ascending);
        List<User> users = await sortedQuery
            .Skip(criteria.PagingParameters.Skip)
            .Take(criteria.PagingParameters.Limit)
            .ToListAsync();

        return PagedResult<User>.Create(users, totalItems, criteria.PagingParameters);
    }

    private static IQueryable<User> BuildFilteredQuery(IQueryable<User> query, string? emailFilter, string? userNameFilter)
    {
        if (!string.IsNullOrWhiteSpace(emailFilter))
        {
            query = query.Where(u => u.Email.Value.Address.Contains(emailFilter));
        }

        if (!string.IsNullOrWhiteSpace(userNameFilter))
        {
            query = query.Where(u => u.UserName.Value.Contains(userNameFilter));
        }

        return query;
    }

    private static IOrderedQueryable<User> ApplySorting(IQueryable<User> query, UsersSortBy sortBy, bool ascending)
    {
        return sortBy switch
        {
            UsersSortBy.CreatedAt => ascending
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt),
            UsersSortBy.Email => ascending
                ? query.OrderBy(u => u.Email.Value.Address)
                : query.OrderByDescending(u => u.Email.Value.Address),
            UsersSortBy.UserName => ascending
                ? query.OrderBy(u => u.UserName.Value)
                : query.OrderByDescending(u => u.UserName.Value),
            UsersSortBy.LastLoginAt => ascending
                ? query.OrderBy(u => u.LastLoginAt)
                : query.OrderByDescending(u => u.LastLoginAt),
            _ => query.OrderBy(u => u.CreatedAt)
        };
    }

    public Task UpdateAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _context.Users.Update(user);
        return Task.CompletedTask;
    }
}
