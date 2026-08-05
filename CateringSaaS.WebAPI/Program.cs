using CateringSaaS.Modules.Identity;
using CateringSaaS.Modules.Tenants;
using CateringSaaS.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSharedPersistence(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddTenantModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();
app.MapTenantEndpoints();

await app.UseIdentityModuleAsync();

app.Run();
