using backend.Data;
using Microsoft.EntityFrameworkCore;
using backend.Middleware; // Import the namespace for the middleware



var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

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

app.UseWhen(context => context.Request.Path.StartsWithSegments("/users/purchase/{courseId}"), appBuilder =>
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


// POST route to create a new admin
app.MapPost("/admin", async (AppDbContext db, Admin admin) =>
{
    db.Admins.Add(admin);
    await db.SaveChangesAsync();
    return Results.Created($"/admin/{admin.Id}", admin);
});

// Get all admins
app.MapGet("/admin", async (AppDbContext db) => await db.Admins.ToListAsync());

// Get all users
app.MapGet("/admin/users", async (AppDbContext db) => await db.Users.ToListAsync());

// POST route to create a new course
app.MapPost("/admin/courses", async (AppDbContext db, Course course) =>
{   
    var existingCourse = await db.Courses.FirstOrDefaultAsync(c => c.Title == course.Title);


    if (existingCourse != null)
    {
        return Results.BadRequest("A course with this title already exists.");
    }

      // 2. Validate required fields
    if (string.IsNullOrWhiteSpace(course.Title) || string.IsNullOrWhiteSpace(course.Description))
    {
        return Results.BadRequest("Title and Description are required.");
    }

    // 3. Validate positive price
    if (course.Price <= 0)
    {
        return Results.BadRequest("Price must be a positive value.");
    }


    db.Courses.Add(course);
    await db.SaveChangesAsync();
    return Results.Created($"/courses/{course.Id}", course);
});

// Delete All courses
app.MapDelete("/admin/courses",async(AppDbContext db)=>{

     var allCourses = await db.Courses.ToListAsync();

    foreach (var course in allCourses)
    {
        db.Courses.Remove(course);
    }

    await db.SaveChangesAsync();

    return Results.Ok("All users have been deleted.");

});

// Delete All users
app.MapDelete("/admin/user", async (AppDbContext db) =>
{
    var allUsers = await db.Users.ToListAsync();

    if (allUsers.Count == 0)
    {
        return Results.NotFound("No users found to delete.");
    }

    foreach (var user in allUsers)
    {
        db.Users.Remove(user);
    }

    await db.SaveChangesAsync();

    return Results.Ok("All users have been deleted.");
});


// POST route to create a new user
app.MapPost("/user/signup", async (AppDbContext db, User user) =>
{       
    
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
    if (existingUser != null)
    {
        return Results.BadRequest("Username already exists. Please log in.");
    }else{

    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/user/{user.Id}", user);
    }
});

// POST route to log in a user
app.MapPost("/user/login", async (AppDbContext db, User loginUser) =>
{
    // Find the user by their username
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == loginUser.Username);

    // Check if user exists and password matches
    if (user == null || user.Password != loginUser.Password)
    {
        return Results.BadRequest("Invalid username or password.");
    }

    // If login is successful, return a success message and user information (without password)
    return Results.Ok(new { message = "Login successful", username = user.Username });
});

app.MapPut("/users/purchase", async (AppDbContext db, HttpContext context, List<int> courseIds) =>
{
    // Retrieve authenticated user from the context
    var user = context.Items["User"] as User;
    if (user == null)
    {
        return Results.NotFound("User not found in context. Please ensure you are authenticated.");
    }

    // Fetch new courses from courseIds
    var courses = await db.Courses.Where(c => courseIds.Contains(c.Id)).ToListAsync();

    if (courses.Count != courseIds.Count)
    {
        return Results.BadRequest("Some of the provided courses are invalid.");
    }

    // Update user's purchased courses
    user.PurchasedCourses.Clear();
    foreach (var course in courses)
    {
        user.PurchasedCourses.Add(course);
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


// delete the purchased Courses
app.MapDelete("/users/purchase/{courseId}", async (AppDbContext db, HttpContext context, int courseId) =>
{
    // Retrieve the authenticated user from the context (set by middleware)
    var user = context.Items["User"] as User;

    if (user == null)
    {
        return Results.Unauthorized();
    }

    // Include the user's purchased courses to enable deletion
    var dbUser = await db.Users
        .Include(u => u.PurchasedCourses)
        .FirstOrDefaultAsync(u => u.Id == user.Id);

    if (dbUser == null)
    {
        return Results.NotFound("User not found.");
    }

    // Find the course to be removed from the user's purchased courses list
    var course = dbUser.PurchasedCourses.FirstOrDefault(c => c.Id == courseId);

    if (course == null)
    {
        return Results.NotFound($"Course with ID {courseId} not found in the user's purchased courses.");
    }

    // Remove the course from the user's purchased courses
    dbUser.PurchasedCourses.Remove(course);

    // Save changes to the database
    await db.SaveChangesAsync();

    return Results.Ok($"Course with ID {courseId} has been removed from the user's purchased courses.");
});






// Get all courses
app.MapGet("/courses", async (AppDbContext db) => await db.Courses.ToListAsync());








// Add more endpoints as needed
app.Run();
