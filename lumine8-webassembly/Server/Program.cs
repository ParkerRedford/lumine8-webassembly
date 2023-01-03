using Blazored.LocalStorage;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System;
using lumine8_GrpcService;
using lumine8.Server.Data;
using lumine8.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddGrpc();

if (OperatingSystem.IsLinux())
{
    builder.WebHost.ConfigureKestrel((context, options) =>
    {
        options.ListenAnyIP(5001, p =>
        {
            p.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
            p.UseHttps();
        });
    });
}

builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    o.MaximumReceiveMessageSize = null;
});

builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
    o.MaximumReceiveMessageSize = null;
});

//builder.Services.AddControllersWithViews();
//builder.Services.AddRazorPages();

builder.Services.AddCors(o =>
{
    o.AddPolicy(name: "MyCors", builder =>
    {
        builder
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin();
    });
});

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddTransient<ApplicationDbContext>();

builder.Services.AddControllersWithViews().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
);

builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

var app = builder.Build();

//app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors("MyCors");

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseGrpcWeb();

app.UseEndpoints(e => e.MapGrpcService<MainService>().EnableGrpcWeb());

app.MapControllerRoute(name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapBlazorHub();
app.MapHub<PostRealTime>("/postreal");
app.MapHub<Notify>("/notify");
app.MapHub<MainHub>("/mainhub");
app.MapHub<Chat>("/chathub");

app.Run();
