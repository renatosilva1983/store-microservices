using System.Security.Cryptography;
using System.Text;
using Identity.Api.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Identity.Api.Controllers;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);

public sealed record UserResponse(string Email, string Name, string Role);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IOptions<DemoUsersOptions> users, TokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login(LoginRequest request)
    {
        var user = users.Value.Users.SingleOrDefault(u =>
            string.Equals(u.Email, request.Email?.Trim(), StringComparison.OrdinalIgnoreCase));

        // Comparação em tempo constante: não revela pelo tempo de resposta quantos caracteres acertou.
        if (user is null || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.Password), Encoding.UTF8.GetBytes(request.Password ?? string.Empty)))
        {
            // Mesma mensagem para usuário inexistente e senha errada: não ajuda a descobrir e-mails válidos.
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Auth.InvalidCredentials",
                detail: "E-mail ou senha inválidos.");
        }

        var token = tokens.Create(user);
        return Ok(new LoginResponse(token.Token, token.ExpiresAtUtc, new UserResponse(user.Email, user.Name, user.Role)));
    }
}
