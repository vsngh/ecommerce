using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Users.Api.Data;
using Users.Api.Dtos;
using Users.Api.Entities;
using Users.Api.Security;

namespace Users.Api.Services;

public class UsersService : IUsersService
{
    private readonly IUsersRepository usersRepository;
    private readonly IDistributedCache cache;
    private readonly IJwtTokenService jwtTokenService;
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public UsersService(IUsersRepository usersRepository, IDistributedCache cache, IJwtTokenService jwtTokenService)
    {
        this.usersRepository = usersRepository;
        this.cache = cache;
        this.jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await usersRepository.GetByEmailAsync(request.Email);

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

        var users = await usersRepository.GetAllAsync();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(users), CacheOptions);
        return users;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        var cacheKey = $"users:{userId}";
        var cachedUser = await cache.GetStringAsync(cacheKey);
        if (cachedUser is not null) return JsonSerializer.Deserialize<User>(cachedUser);

        var user = await usersRepository.GetByIdAsync(userId);
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

        await usersRepository.AddAsync(user);
        await RemoveUserCache(user.Id);
        return user;
    }

    public async Task<bool> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await usersRepository.GetByIdAsync(userId);
        if (user is null) return false;

        user.Name = request.Name;
        user.Email = request.Email;
        user.PasswordHash = PasswordHasher.Hash(request.PasswordHash);
        user.Role = request.Role;
        await usersRepository.UpdateAsync(user);
        await RemoveUserCache(userId);
        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(Guid userId, string role)
    {
        var user = await usersRepository.GetByIdAsync(userId);
        if (user is null) return false;

        user.Role = role;
        await usersRepository.UpdateAsync(user);
        await RemoveUserCache(userId);
        return true;
    }

    public async Task<bool> UpdateUserPasswordAsync(Guid userId, string passwordHash)
    {
        var user = await usersRepository.GetByIdAsync(userId);
        if (user is null) return false;

        user.PasswordHash = PasswordHasher.Hash(passwordHash);
        await usersRepository.UpdateAsync(user);
        await RemoveUserCache(userId);
        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await usersRepository.GetByIdAsync(userId);
        if (user is null) return false;

        await usersRepository.DeleteAsync(user);
        await RemoveUserCache(userId);
        return true;
    }

    private async Task RemoveUserCache(Guid userId)
    {
        await cache.RemoveAsync("users:all");
        await cache.RemoveAsync($"users:{userId}");
    }
}
