using JWT_Advance.Context;
using JWT_Advance.Interfaces;
using JWT_Advance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace JWT_Advance.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TokenController : ControllerBase
{
    private readonly AppDbContext _appDbContext;
    private readonly ITokenService _tokenService;
    public TokenController(AppDbContext appDbContext, ITokenService tokenService)
    {
        _appDbContext = appDbContext ?? throw new ArgumentNullException(nameof(appDbContext));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    [AllowAnonymousRefreshToken]
    [HttpPost]
    [Route("refresh")]
    public IActionResult Refresh(TokenApiModel tokenApiModel)
    {
        if (tokenApiModel is null)
            return BadRequest("Invalid client request");

        string accessToken = tokenApiModel.AccessToken ?? string.Empty;
        string refreshToken = tokenApiModel.RefreshToken ?? string.Empty;

        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        var username = principal.Identity?.Name; //this is mapped to the Name claim by default

        var user = _appDbContext.LoginModels.SingleOrDefault(u => u.UserName == username);

        if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            return BadRequest("Invalid client request");

        var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        _appDbContext.SaveChanges();

        return Ok(new AuthenticatedResponse()
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }

    [HttpPost("revoke")]
    [Authorize]
    public IActionResult Revoke()
    {
        var username = User.Identity?.Name;

        var user = _appDbContext.LoginModels.SingleOrDefault(u => u.UserName == username);
        if (user == null) return BadRequest();

        user.RefreshToken = null;

        _appDbContext.SaveChanges();

        return NoContent();
    }
}