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

    private static UsersService CreateService(UsersDbContext dbContext) => new(dbContext, CreateCache(), new FakeJwtTokenService());

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

    private static UsersService CreateService(UsersDbContext dbContext) => new(dbContext, CreateCache(), new FakeJwtTokenService());

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
