using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Users.Api.Controllers;
using Users.Api.Data;
using Users.Api.Dtos;
using Users.Api.Entities;
using Users.Api.Security;
using Users.Api.Services;
using Xunit;

namespace Users.Api.Tests;

public class UsersServiceTests
{
    [Theory]
    [InlineData("Admin", "users.manage", true)]
    [InlineData("Manager", "users.manage", false)]
    [InlineData("Customer", "products.read", true)]
    [InlineData("Customer", "inventory.manage", false)]
    [InlineData("Customer", "payments.order.cancel", false)]
    public void PermissionCatalog_ReturnsRoleSpecificPermissions(string role, string permission, bool expected)
    {
        var permissions = PermissionCatalog.ForRole(role);

        Assert.Equal(expected, permissions.Contains(permission));
    }

    [Fact]
    public async Task CreateUserAsync_HashesPasswordAndPersistsUser()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var user = await service.CreateUserAsync(new CreateUserRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "password123",
            Role = "Customer"
        });

        Assert.NotEqual("password123", user.PasswordHash);
        Assert.StartsWith("sha256:", user.PasswordHash);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenForValidCredentials()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        await service.CreateUserAsync(new CreateUserRequest
        {
            Name = "Admin",
            Email = "admin@example.com",
            PasswordHash = "admin123",
            Role = "Admin"
        });

        var auth = await service.LoginAsync(new LoginRequest { Email = "admin@example.com", Password = "admin123" });

        Assert.NotNull(auth);
        Assert.Equal("Admin", auth!.Role);
        Assert.Contains("users.manage", auth.Permissions);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNullForInvalidPassword()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        await service.CreateUserAsync(new CreateUserRequest
        {
            Name = "User",
            Email = "user@example.com",
            PasswordHash = "right",
            Role = "Customer"
        });

        var auth = await service.LoginAsync(new LoginRequest { Email = "user@example.com", Password = "wrong" });

        Assert.Null(auth);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsPersistedUsers()
    {
        using var dbContext = CreateDbContext();
        dbContext.Users.Add(new User { Name = "Test User", Email = "test@example.com", PasswordHash = "hash" });
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var users = await service.GetUsersAsync();

        Assert.Single(users);
        Assert.Equal("test@example.com", users[0].Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNullWhenUserDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var user = await service.GetUserByIdAsync(Guid.NewGuid());

        Assert.Null(user);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesUserAndHashesNewPassword()
    {
        using var dbContext = CreateDbContext();
        var user = new User { Name = "Old", Email = "old@example.com", PasswordHash = "old-hash", Role = "Customer" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateUserAsync(user.Id, new UpdateUserRequest
        {
            Name = "New",
            Email = "new@example.com",
            PasswordHash = "new-password",
            Role = "Admin"
        });

        Assert.True(updated);
        var savedUser = await dbContext.Users.FindAsync(user.Id);
        Assert.Equal("New", savedUser!.Name);
        Assert.Equal("new@example.com", savedUser.Email);
        Assert.Equal("Admin", savedUser.Role);
        Assert.True(PasswordHasher.Verify("new-password", savedUser.PasswordHash));
    }

    [Fact]
    public async Task UpdateUserRoleAsync_UpdatesExistingUser()
    {
        using var dbContext = CreateDbContext();
        var user = new User { Name = "Test", Email = "test@example.com", PasswordHash = "hash", Role = "Customer" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateUserRoleAsync(user.Id, "Manager");

        Assert.True(updated);
        Assert.Equal("Manager", (await dbContext.Users.FindAsync(user.Id))!.Role);
    }

    [Fact]
    public async Task UpdateUserPasswordAsync_HashesNewPassword()
    {
        using var dbContext = CreateDbContext();
        var user = new User { Name = "Test", Email = "test@example.com", PasswordHash = "old-hash" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var updated = await service.UpdateUserPasswordAsync(user.Id, "new-password");

        Assert.True(updated);
        Assert.True(PasswordHasher.Verify("new-password", (await dbContext.Users.FindAsync(user.Id))!.PasswordHash));
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesExistingUser()
    {
        using var dbContext = CreateDbContext();
        var user = new User { Name = "Test", Email = "test@example.com", PasswordHash = "hash" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var deleted = await service.DeleteUserAsync(user.Id);

        Assert.True(deleted);
        Assert.Null(await dbContext.Users.FindAsync(user.Id));
    }

    private static UsersService CreateService(UsersDbContext dbContext) => new(new UsersRepository(dbContext), CreateCache(), new FakeJwtTokenService());

    private static UsersDbContext CreateDbContext()
    {
        return new UsersDbContext(new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class UsersControllerIntegrationTests
{
    [Fact]
    public async Task Login_ReturnsOkForValidCredentials()
    {
        using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        await service.CreateUserAsync(new CreateUserRequest
        {
            Name = "Customer",
            Email = "customer@example.com",
            PasswordHash = "secret",
            Role = "Customer"
        });
        var controller = new UsersController(service);

        var result = await controller.Login(new LoginRequest { Email = "customer@example.com", Password = "secret" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorizedForMissingUser()
    {
        using var dbContext = CreateDbContext();
        var controller = new UsersController(CreateService(dbContext));

        var result = await controller.Login(new LoginRequest { Email = "missing@example.com", Password = "secret" });

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static UsersService CreateService(UsersDbContext dbContext) => new(new UsersRepository(dbContext), CreateCache(), new FakeJwtTokenService());

    private static UsersDbContext CreateDbContext()
    {
        return new UsersDbContext(new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}

public class FakeJwtTokenService : IJwtTokenService
{
    public AuthResponse CreateToken(User user)
    {
        return new AuthResponse
        {
            AccessToken = "test-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Permissions = PermissionCatalog.ForRole(user.Role)
        };
    }
}
