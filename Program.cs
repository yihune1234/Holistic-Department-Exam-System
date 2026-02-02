using HolisticDepartmentExamSystem.Data;
using HolisticDepartmentExamSystem.Services;
using HolisticDepartmentExamSystem.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework Core with SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    // Suppress pending model changes warning for development
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Register Query Optimization Service
builder.Services.AddScoped<QueryOptimizationService>();

// Register Exam Mark Calculation Service
builder.Services.AddScoped<ExamMarkCalculationService>();

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    options.AddPolicy("CoordinatorOnly", policy => policy.RequireRole("Coordinator"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Add session state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Apply migrations automatically on startup (with error handling)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Create database if it doesn't exist (preserve existing data)
        dbContext.Database.EnsureCreated();
        
        // Seed admin user if it doesn't exist
        var adminUser = dbContext.Users.FirstOrDefault(u => u.Username == "admin");
        var adminPassword = "admin123";

        if (adminUser == null)
        {
            var adminRole = dbContext.Roles.FirstOrDefault(r => r.RoleName == "Admin");
            if (adminRole != null)
            {
                adminUser = new HolisticDepartmentExamSystem.Models.User
                {
                    Username = "admin",
                    PasswordHash = adminPassword,
                    RoleId = adminRole.RoleId,
                    Status = true,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Users.Add(adminUser);
                dbContext.SaveChanges();
            }
        }
        else if (adminUser.PasswordHash != adminPassword)
        {
            adminUser.PasswordHash = adminPassword;
            adminUser.Status = true;
            dbContext.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // Log error but continue
        Console.WriteLine($"Database initialization error: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
