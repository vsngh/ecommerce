using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.Api.Dtos;
using Users.Api.Security;
using Users.Api.Services;

namespace Users.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUsersService usersService;

    public UsersController(IUsersService usersService)
    {
        this.usersService = usersService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var authResponse = await usersService.LoginAsync(request);

        return authResponse is null ? Unauthorized() : Ok(authResponse);
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicies.UsersManage)]
    public async Task<IActionResult> GetUsers() => Ok(await usersService.GetUsersAsync());

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = PermissionPolicies.UsersManage)]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var user = await usersService.GetUserByIdAsync(userId);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await usersService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUserById), new { userId = user.Id }, user);
    }

    [HttpPut("{userId:guid}")]
    [Authorize(Policy = PermissionPolicies.UsersManage)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        return await usersService.UpdateUserAsync(userId, request) ? NoContent() : NotFound();
    }

    [HttpPatch("{userId:guid}/role")]
    [Authorize(Policy = PermissionPolicies.UsersManage)]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
    {
        return await usersService.UpdateUserRoleAsync(userId, request.Role) ? NoContent() : NotFound();
    }

    [HttpPatch("{userId:guid}/password")]
    [Authorize(Policy = PermissionPolicies.UsersManage)]
    public async Task<IActionResult> UpdateUserPassword(Guid userId, [FromBody] UpdateUserPasswordRequest request)
    {
        return await usersService.UpdateUserPasswordAsync(userId, request.PasswordHash) ? NoContent() : NotFound();
    }

    [HttpDelete("{userId:guid}")]
    [Authorize(Policy = PermissionPolicies.UsersManage)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        return await usersService.DeleteUserAsync(userId) ? NoContent() : NotFound();
    }
}
