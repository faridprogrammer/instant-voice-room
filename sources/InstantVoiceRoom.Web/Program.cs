using InstantVoiceRoom;
using InstantVoiceRoom.Framework.Services;
using InstantVoiceRoom.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
);

builder.Services.Configure<WebRTCSettings>(builder.Configuration.GetSection("WebRTCSettings"));
builder.Services.AddDataProtection().SetApplicationName("InstantVoiceRoom");
builder.Services.AddSingleton<FileUserStore>((provider) =>
{
    var hostingEnv = provider.GetRequiredService<IWebHostEnvironment>();
    var logger = provider.GetRequiredService<ILogger<FileUserStore>>();
    return new FileUserStore(logger, provider.GetRequiredService<IDataProtectionProvider>(), Path.Combine(hostingEnv.ContentRootPath, "file_store.dat"));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(options =>
       {
           options.LoginPath  = "/Account/Login";
           options.LogoutPath = "/Account/Login";
       });


builder.Services.AddSignalR();
builder.Services.AddRazorPages(options =>
{
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapHub<SignalHub>("/signalHub");
app.Run();
