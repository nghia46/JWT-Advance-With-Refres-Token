using JWT_Advance.Interfaces;
using JWT_Advance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace JWT_Advance.Controllers;
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IRepository<LoginModel> _repository;

    public AuthController(IRepository<LoginModel> repository, ITokenService tokenService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    [HttpGet]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Get()
    {
        IEnumerable<LoginModel> users = await _repository.Get();
        return Ok(users);
    }
    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] LoginView LoginView)
    {
        LoginModel register = new LoginModel
        {
            Id = Guid.NewGuid(),
            UserName = LoginView.UserName,
            Password = LoginView.Password
        };
        await _repository.AddAsync(register);
        return Ok();
    }
    [HttpPost, Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginView loginView)
    {
        if (loginView is null)
        {
            return BadRequest("Invalid client request");
        }

        var users = await _repository.Get(p => p.UserName == loginView.UserName && p.Password == loginView.Password);
        var user = users.FirstOrDefault();
        if (user == null)
        {
            return Unauthorized();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, loginView.UserName ?? throw new ArgumentNullException(nameof(loginView.UserName))),
            new Claim(ClaimTypes.Role, "Manager")
        };

        var accessToken = _tokenService.GenerateAccessToken(claims);
        var refreshToken = _tokenService.GenerateRefreshToken();


        try
        {
            var existingUsers = await _repository.Get(p => p.Id == user.Id);
            var existingUser = existingUsers.FirstOrDefault();
            if (existingUser == null)
            {
                return NotFound("User not found");
            }

            existingUser.RefreshToken = refreshToken;
            existingUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            // Update the user entity in the repository
            await _repository.UpdateAsync(existingUser);
        }
        catch (Exception ex)
        {
            // Handle exception
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the user: " + ex.Message);
        }

        // Return success response with access token and refresh token
        return Ok(new AuthenticatedResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken
        });
    }
}
