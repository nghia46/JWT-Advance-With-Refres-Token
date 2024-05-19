using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Threading.Tasks;

namespace JWT_Advance
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public AuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var allowAnonymousRefreshTokenAttribute = endpoint.Metadata.GetMetadata<AllowAnonymousRefreshTokenAttribute>();
                if (allowAnonymousRefreshTokenAttribute != null)
                {
                    await _next(context);
                    return;
                }
            }

            // Token validation logic...
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (!string.IsNullOrEmpty(token))
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    try
                    {
                        var jwtToken = tokenHandler.ReadJwtToken(token);
                        if (jwtToken.ValidTo < DateTime.UtcNow)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            await context.Response.WriteAsync("Token has expired");
                            return;
                        }
                    }
                    catch (SecurityTokenException)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        await context.Response.WriteAsync("Invalid token");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
