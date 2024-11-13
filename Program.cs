using backend.Data;
using Microsoft.EntityFrameworkCore;
using backend.Middleware; // Import the namespace for the middleware



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));



// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/users/purchase"), appBuilder =>
{
    appBuilder.UseMiddleware<BasicAuthMiddleware>();
});

// app.UseMiddleware<BasicAuthMiddleware>(); // Register the custom middleware


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



// CRUD endpoints for testing

// Get all users
app.MapGet("/users", async (AppDbContext db) => await db.Users.ToListAsync());




// Get all courses
app.MapGet("/courses", async (AppDbContext db) => await db.Courses.ToListAsync());



// Get all admins
app.MapGet("/admin", async (AppDbContext db) => await db.Admins.ToListAsync());



// POST route to create a new user
app.MapPost("/users", async (AppDbContext db, User user) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", user);
});


// POST route to create a new course
app.MapPost("/courses", async (AppDbContext db, Course course) =>
{
    db.Courses.Add(course);
    await db.SaveChangesAsync();
    return Results.Created($"/courses/{course.Id}", course);
});

// PUT route to update a user's purchased courses
app.MapPut("/users/{userId}/purchase", async (AppDbContext db, int userId, List<int> courseIds) =>
{
    // Find the user by their ID
    var user = await db.Users.Include(u => u.PurchasedCourses).FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null)
    {
        return Results.NotFound($"User with ID {userId} not found.");
    }

    // Clear existing purchased courses
    // user.PurchasedCourses.Clear();

    // Add new purchased courses based on provided courseIds
    var courses = await db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync();

    if (courses.Count != courseIds.Count)
    {
        return Results.BadRequest("Some of the provided courses are invalid.");
    }


    // Add each course individually to the ICollection
    foreach (var course in courses)
    {
        if (!user.PurchasedCourses.Contains(course))
        {
            user.PurchasedCourses.Add(course);
        }
    }

    // Save changes to the database
    await db.SaveChangesAsync();

    return Results.Ok(user);
});





//  get all purchased course

app.MapGet("/users/purchase", async (HttpContext context,AppDbContext db) =>
{       
       // Check if user is authenticated (middleware will have added user to context)
    var user = context.Items["User"] as User;
    
    if (user == null)
    {
        return Results.Problem("Invalid credentials");
    }

    // Find the user by their ID and include the purchased courses
    var dbUser = await db.Users
        .Include(u => u.PurchasedCourses)
        .FirstOrDefaultAsync(u => u.Id == user.Id);

    if (dbUser == null)
    {
        return Results.NotFound($"User with ID {user.Id} not found.");
    }

    // Return the list of purchased courses for the user
    // return Results.Ok($"userinHeader:{user}");
    return Results.Ok(dbUser.PurchasedCourses);
});




// DELETE route to remove a course from the user's purchased courses
app.MapDelete("/users/{userId}/purchase/{courseId}", async (AppDbContext db, int userId, int courseId) =>
{
    // Find the user by their ID and include the purchased courses
    var user = await db.Users
        .Include(u => u.PurchasedCourses)
        .FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null)
    {
        return Results.NotFound($"User with ID {userId} not found.");
    }

    // Find the course to be removed from the user's purchased courses list
    var course = user.PurchasedCourses.FirstOrDefault(c => c.Id == courseId);

    if (course == null)
    {
        return Results.NotFound($"Course with ID {courseId} not found in the user's purchased courses.");
    }

    // Remove the course from the user's purchased courses
    user.PurchasedCourses.Remove(course);

    // Save changes to the database
    await db.SaveChangesAsync();

    return Results.Ok($"Course with ID {courseId} has been removed from the user's purchased courses.");
});



// POST route to create a new admin
app.MapPost("/admin", async (AppDbContext db, Admin admin) =>
{
    db.Admins.Add(admin);
    await db.SaveChangesAsync();
    return Results.Created($"/admin/{admin.Id}", admin);
});



// Add more endpoints as needed
app.Run();
