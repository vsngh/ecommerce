using Users.Api.Entities;

namespace Users.Api.Data;

public interface IUsersRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid userId);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
}