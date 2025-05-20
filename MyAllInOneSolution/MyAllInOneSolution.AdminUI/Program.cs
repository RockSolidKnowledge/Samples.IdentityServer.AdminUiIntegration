using IdentityExpress.Manager.BusinessLogic.Configuration;
using IdentityExpress.Manager.UI.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;

builder.Configuration
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

builder.Services.AddAdminUI(options =>
{
    options.IdentityType = IdentityType.DefaultIdentity;
    options.MigrationOptions = MigrationOptions.All;
});

var app = builder.Build();

app.UseAdminUI();

app.Run();