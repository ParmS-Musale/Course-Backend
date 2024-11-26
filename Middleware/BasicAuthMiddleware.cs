using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using backend.Data; // Make sure this namespace matches where your AppDbContext is located
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;

namespace backend.Middleware  // Ensure the namespace matches your folder structure
{
    public class BasicAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _secretKey = "ThisIsA256BitLongSecretKeyForHS256Algorithm12345";  // Replace with your actual secret key

        public BasicAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            // Look for the Authorization header
            var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authorizationHeader != null && authorizationHeader.StartsWith("Bearer "))
            {
                // Extract the token (after "Bearer ")
                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                try
                {
                    // Validate the token and extract claims
                    var principal = ValidateToken(token);

                    if (principal != null)
                    {
                        // Find the user by username from the claims
                        var usernameClaim = principal.FindFirst(ClaimTypes.Name);
                        if (usernameClaim != null)
                        {
                            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == usernameClaim.Value);

                            if (user == null)
                            {
                                context.Response.StatusCode = 401; // Unauthorized
                                await context.Response.WriteAsync("Invalid token or user not found.");
                                return;
                            }

                            // Add user to the request context if token is valid
                            context.Items["User"] = user; // Store user in the context, accessible in later stages of the pipeline
                            Console.WriteLine($"Authenticated user: {user?.Username}");
                        }
                        else
                        {
                            context.Response.StatusCode = 401; // Unauthorized
                            await context.Response.WriteAsync("Invalid token - Username not found.");
                            return;
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 401; // Unauthorized
                        await context.Response.WriteAsync("Invalid token.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception (optional)
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Invalid token or error while validating.");
                    Console.WriteLine($"Error while validating token: {ex.Message}");
                    return;
                }
            }
            else
            {
                context.Response.StatusCode = 400; // Bad Request if Authorization header is missing or malformed
                await context.Response.WriteAsync("Authorization header missing or invalid.");
                return;
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }

        private ClaimsPrincipal ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
                var tokenHandler = new JwtSecurityTokenHandler();

                // Token validation parameters
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false, // Change to true if you want to validate the issuer
                    ValidateAudience = false, // Change to true if you want to validate the audience
                    ValidateLifetime = true,
                    IssuerSigningKey = key
                };

                // Validate the token and return the claims principal
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception)
            {
                // If validation fails, return null
                return null;
            }
        }
    }
}
