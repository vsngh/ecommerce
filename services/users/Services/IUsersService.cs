using Users.Api.Dtos;
using Users.Api.Entities;

namespace Users.Api.Services;

public interface IUsersService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<IReadOnlyList<User>> GetUsersAsync();
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User> CreateUserAsync(CreateUserRequest request);
    Task<bool> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<bool> UpdateUserRoleAsync(Guid userId, string role);
    Task<bool> UpdateUserPasswordAsync(Guid userId, string passwordHash);
    Task<bool> DeleteUserAsync(Guid userId);
}
