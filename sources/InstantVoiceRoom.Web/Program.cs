using InstantVoiceRoom;
using InstantVoiceRoom.Framework.Services;
using InstantVoiceRoom.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
);

builder.Services.Configure<WebRTCSettings>(builder.Configuration.GetSection("WebRTCSettings"));

builder.Services.AddScoped((provider) =>
{
    var hostingEnv = provider.GetRequiredService<IWebHostEnvironment>();
    var logger = provider.GetRequiredService<ILogger<FileUserStore>>();
    return new FileUserStore(logger, Path.Combine(hostingEnv.ContentRootPath, "file_store.dat"));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(options =>
       {
           options.ExpireTimeSpan = TimeSpan.FromDays(1);
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
