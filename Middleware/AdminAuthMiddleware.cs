using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Threading.Tasks;
using backend.Data; // Make sure this namespace matches where your AppDbContext is located
using Microsoft.EntityFrameworkCore;

namespace backend.Middleware  // Ensure the namespace matches your folder structure
{
    public class AdminAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AdminAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            // Check for username and password in the request headers
            if (context.Request.Headers.TryGetValue("username", out StringValues username))
            {
                // Find user by username
                var user = await db.Admins.FirstOrDefaultAsync(u => u.Username == username.ToString());

                if (user == null )  // Simple password check (no hashing)
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Invalid credentials");
                    return;
                }
                // Call the next middleware in the pipeline
                Console.WriteLine($"Authenticated user: {user?.Username}");
                // Add user to the request context if valid
                context.Items["User"] = user; // Store user in the context, accessible in later stages of pipeline
            }
            else
            {
                context.Response.StatusCode = 400; // Bad Request if username/password headers are missing
                await context.Response.WriteAsync("Please Login as Admin");
                return;
            }



            await _next(context);
        }
    }
}
