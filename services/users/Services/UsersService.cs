using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Users.Api.Data;
using Users.Api.Dtos;
using Users.Api.Entities;
using Users.Api.Security;

namespace Users.Api.Services;

public class UsersService : IUsersService
{
    private readonly UsersDbContext dbContext;
    private readonly IDistributedCache cache;
    private readonly IJwtTokenService jwtTokenService;
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public UsersService(UsersDbContext dbContext, IDistributedCache cache, IJwtTokenService jwtTokenService)
    {
        this.dbContext = dbContext;
        this.cache = cache;
        this.jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == request.Email);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return jwtTokenService.CreateToken(user);
    }

    public async Task<IReadOnlyList<User>> GetUsersAsync()
    {
        const string cacheKey = "users:all";
        var cachedUsers = await cache.GetStringAsync(cacheKey);
        if (cachedUsers is not null) return JsonSerializer.Deserialize<List<User>>(cachedUsers) ?? [];

        var users = await dbContext.Users.ToListAsync();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(users), CacheOptions);
        return users;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        var cacheKey = $"users:{userId}";
        var cachedUser = await cache.GetStringAsync(cacheKey);
        if (cachedUser is not null) return JsonSerializer.Deserialize<User>(cachedUser);

        var user = await dbContext.Users.FindAsync(userId);
        if (user is not null) await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), CacheOptions);
        return user;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.PasswordHash),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        await RemoveUserCache(user.Id);
        return user;
    }

    public async Task<bool> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user is null) return false;

        user.Name = request.Name;
        user.Email = request.Email;
        user.PasswordHash = PasswordHasher.Hash(request.PasswordHash);
        user.Role = request.Role;
        await dbContext.SaveChangesAsync();
        await RemoveUserCache(userId);
        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(Guid userId, string role)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user is null) return false;

        user.Role = role;
        await dbContext.SaveChangesAsync();
        await RemoveUserCache(userId);
        return true;
    }

    public async Task<bool> UpdateUserPasswordAsync(Guid userId, string passwordHash)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user is null) return false;

        user.PasswordHash = PasswordHasher.Hash(passwordHash);
        await dbContext.SaveChangesAsync();
        await RemoveUserCache(userId);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user is null) return false;

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
        await RemoveUserCache(userId);
        return true;
    }

    private async Task RemoveUserCache(Guid userId)
    {
        await cache.RemoveAsync("users:all");
        await cache.RemoveAsync($"users:{userId}");
    }
}
