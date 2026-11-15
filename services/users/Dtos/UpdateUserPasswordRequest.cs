namespace Users.Api.Dtos;

public class UpdateUserPasswordRequest
{
    public string PasswordHash { get; set; } = string.Empty;
}
