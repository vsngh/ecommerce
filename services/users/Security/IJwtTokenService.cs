using Users.Api.Dtos;
using Users.Api.Entities;

namespace Users.Api.Security;

public interface IJwtTokenService
{
    AuthResponse CreateToken(User user);
}
