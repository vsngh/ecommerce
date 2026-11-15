using Microsoft.EntityFrameworkCore;
using Users.Api.Entities;

namespace Users.Api.Data;

public class UsersRepository : IUsersRepository
{
    private readonly UsersDbContext dbContext;

    public UsersRepository(UsersDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string email) => dbContext.Users.FirstOrDefaultAsync(user => user.Email == email);

    public async Task<IReadOnlyList<User>> GetAllAsync() => await dbContext.Users.ToListAsync();

    public Task<User?> GetByIdAsync(Guid userId) => dbContext.Users.FindAsync(userId).AsTask();

    public async Task AddAsync(User user)
    {
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
    }
}