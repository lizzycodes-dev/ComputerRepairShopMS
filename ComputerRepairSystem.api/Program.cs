using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Interfaces;
using ComputerRepairSystem.company.Repositories;
using ComputerRepairSystem.company.Services;
using ComputerRepairSystem.infrastructure.Entities;
using ComputerRepairSystem.domain.Entities;

using ComputerRepairSystem.infrastructure.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ComputerRepairSystem.infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);



// ==========================================
// MASTER ERP DATABASE
// ==========================================

builder.Services.AddDbContext<MasterErpDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MasterLocal")));


// ==========================================
// ASP.NET IDENTITY
// ==========================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<MasterErpDbContext>()
    .AddDefaultTokenProviders();


// ==========================================
// TENANT DATABASE
// ==========================================

builder.Services.AddDbContextFactory<TenantDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TenantLocal")));


builder.Services.AddScoped<
    ITenantDbContextFactory,
    TenantDbContextFactory>();

// ==========================================
// REPOSITORIES
// ==========================================

builder.Services.AddScoped<
    ICustomerRepository,
    CustomerRepository>();


// ==========================================
// SERVICES
// ==========================================

builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<UserService>();

// ==========================================
// CONTROLLERS
// ==========================================

builder.Services.AddControllers();


// ==========================================
// OPENAPI / SWAGGER
// ==========================================

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Computer Repair System API",
        Version = "v1"
    });
});


var app = builder.Build();


// ==========================================
// SWAGGER
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();

    app.MapOpenApi();
}


// ==========================================
// HTTP PIPELINE
// ==========================================

app.UseHttpsRedirection();


// IMPORTANT:
// Authentication must come BEFORE Authorization

app.UseAuthentication();

app.UseAuthorization();


// ==========================================
// COMPANY ENDPOINT
// ==========================================

app.MapPost(
    "/companies",

    async (
        Company company,
        MasterErpDbContext db) =>
    {
        db.Companies.Add(company);

        await db.SaveChangesAsync();

        return Results.Created(
            $"/companies/{company.CompanyId}",
            company);
    });


// ==========================================
// CONTROLLERS
// ==========================================


app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var userManager =
        scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

    var admin =
        await userManager.FindByNameAsync("admin");

    if (admin == null)
    {
        Console.WriteLine("ADMIN NOT FOUND");
    }
    else
    {
        Console.WriteLine(
            $"ADMIN FOUND: {admin.UserName}");

        Console.WriteLine(
            $"CURRENT HASH: {admin.PasswordHash}");

        if (string.IsNullOrEmpty(admin.PasswordHash))
        {
            var result =
                await userManager.AddPasswordAsync(
                    admin,
                    "Admin123!");

            Console.WriteLine(
                $"PASSWORD RESULT: {result.Succeeded}");

            foreach (var error in result.Errors)
            {
                Console.WriteLine(
                    $"{error.Code}: {error.Description}");
            }
        }
    }
}

app.Run();