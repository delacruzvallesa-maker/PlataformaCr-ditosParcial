using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ✅ CONFIGURACIÓN CORRECTA DE IDENTITY CON ROLES
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

// ================= REDIS / CACHE =================
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");

if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);

        builder.Services.AddDataProtection();

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "PlataformaCreditos";
        });

        Console.WriteLine("Redis configurado correctamente.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al conectar con Redis: {ex.Message}. Usando MemoryCache.");

        builder.Services.AddMemoryCache();
        builder.Services.AddDistributedMemoryCache();
    }
}
else
{
    builder.Services.AddMemoryCache();
    builder.Services.AddDistributedMemoryCache();

    Console.WriteLine("Usando MemoryCache (Redis no configurado).");
}

// ================= SESIONES =================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ================= PIPELINE =================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();

// ✅ IMPORTANTE (ANTES DE Authorization)
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// ================= SEED DATA =================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    context.Database.EnsureCreated();

    await SeedData.InitializeAsync(context, userManager, roleManager);
}

app.Run();