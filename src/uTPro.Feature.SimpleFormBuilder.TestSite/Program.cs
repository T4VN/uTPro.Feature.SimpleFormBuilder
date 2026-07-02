using Our.Umbraco.PostgreSql;
using Umbraco.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddUmbracoPostgreSqlSupport()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

var app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
