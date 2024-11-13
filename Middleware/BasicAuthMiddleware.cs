using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Threading.Tasks;
using backend.Data; // Make sure this namespace matches where your AppDbContext is located
using Microsoft.EntityFrameworkCore;

namespace backend.Middleware  // Ensure the namespace matches your folder structure
{
    public class BasicAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public BasicAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            // Check for username and password in the request headers
            if (context.Request.Headers.TryGetValue("username", out StringValues username) &&
                context.Request.Headers.TryGetValue("password", out StringValues password))
            {
                // Find user by username
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username.ToString());

                if (user == null || user.Password != password.ToString())  // Simple password check (no hashing)
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Invalid credentials");
                    return;
                }

                // Add user to the request context if valid
                context.Items["User"] = user; // Store user in the context, accessible in later stages of pipeline
            }
            else
            {
                context.Response.StatusCode = 400; // Bad Request if username/password headers are missing
                await context.Response.WriteAsync("Please Login");
                return;
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}
