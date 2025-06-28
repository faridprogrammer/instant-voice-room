using InstantVoiceRoom.Framework.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InstantVoiceRoom.CLI
{
    [HelpOption]  // --help, -h
    [VersionOption("1.0.0")]
    class Program
    {
        private static IServiceProvider _services;

        static async Task<int> Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddDataProtection().SetApplicationName("InstantVoiceRoom").Services
                .BuildServiceProvider();
            _services = services;

            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(_services);

            app.Command("add", cmd =>
            {
                cmd.Description = "Add a new user";
                var filepathArg = cmd.Argument("filepath", "Path to user store file")
                                     .IsRequired();
                var userArg    = cmd.Argument("username", "Username").IsRequired();
                var passArg    = cmd.Argument("password", "Password").IsRequired();

                cmd.OnExecute(() =>
                {
                    var dp   = _services.GetRequiredService<IDataProtectionProvider>();
                    var logger = _services.GetRequiredService<ILogger<FileUserStore>>();
                    var store = new FileUserStore(logger, dp, filepathArg.Value);
                    if (store.AddUser(userArg.Value, passArg.Value))
                        Console.WriteLine($"User '{userArg.Value}' added.");
                    else
                        Console.WriteLine($"User '{userArg.Value}' already exists.");
                    return 0;
                });
            });

            app.Command("delete", cmd =>
            {
                cmd.Description = "Delete an existing user";
                var filepathArg = cmd.Argument("filepath", "Path to user store file")
                                     .IsRequired();
                var userArg    = cmd.Argument("username", "Username").IsRequired();

                cmd.OnExecute(() =>
                {
                    var dp    = _services.GetRequiredService<IDataProtectionProvider>();
                    var logger = _services.GetRequiredService<ILogger<FileUserStore>>();
                    var store = new FileUserStore(logger, dp, filepathArg.Value);
                    if (store.DeleteUser(userArg.Value))
                        Console.WriteLine($"User '{userArg.Value}' deleted.");
                    else
                        Console.WriteLine($"User '{userArg.Value}' not found.");
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 1;
            });

            return await app.ExecuteAsync(args);
        }
    }
}