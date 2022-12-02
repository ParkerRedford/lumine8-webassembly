using lumine8_webassembly;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using static System.Net.WebRequestMethods;
using System.Reflection;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using static lumine8_GrpcService.MainProto;
using lumine8.Client.Pages;
using lumine8;
using lumine8.Client.Identity;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using lumine8_webassembly.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

builder.Services.AddScoped<Mobile>();
builder.Services.AddScoped<EnumToString>();

builder.Services.AddSingleton<AccountManager>();
builder.Services.AddSingleton<AuthenticationService>();

builder.Services.AddBlazoredLocalStorageAsSingleton();

builder.Services.AddSingleton<SingletonVariables>();

var sv = new SingletonVariables();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(sv.uri) });
builder.Services.AddSingleton(services =>
{
    var httpClient = new HttpClient(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()));
    var baseUri = services.GetRequiredService<NavigationManager>().BaseUri;
    var channel = GrpcChannel.ForAddress(baseUri, new GrpcChannelOptions { HttpClient = httpClient });

    return new MainProtoClient(channel);
});

await builder.Build().RunAsync();
