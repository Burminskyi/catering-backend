using CateringSaaS.Modules.Identity;
using CateringSaaS.Modules.Inventory;
using CateringSaaS.Modules.Kitchen;
using CateringSaaS.Modules.Menu;
using CateringSaaS.Modules.Ordering;
using CateringSaaS.Modules.Tenants;
using CateringSaaS.Shared;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CateringSaaS API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token from /api/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddSharedPersistence(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddTenantModule();
builder.Services.AddInventoryModule();
builder.Services.AddMenuModule();
builder.Services.AddOrderingModule();
builder.Services.AddKitchenModule();

var app = builder.Build();

// Enabled globally so Swagger UI works on Render/production for team testing
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CateringSaaS API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }))
    .WithTags("Health")
    .AllowAnonymous();

app.MapIdentityEndpoints();
app.MapTenantEndpoints();
app.MapInventoryEndpoints();
app.MapMenuEndpoints();
app.MapOrderingEndpoints();
app.MapKitchenEndpoints();

try
{
    await app.UseIdentityModuleAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogCritical(ex, "Startup seeding/migration failed. API will still listen.");
}

app.Run();
