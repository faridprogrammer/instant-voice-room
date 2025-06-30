using InstantVoiceRoom.Framework;
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
            
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            serviceCollection.AddScoped(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FileUserStore>>();
                return new FileUserStore(logger, GetCurrentDirectoryFileStorePath());
            });

            // Build the service provider once all services are registered.
            _services = serviceCollection.BuildServiceProvider();


            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(_services);

            app.Command("add", cmd =>
            {
                cmd.Description = "Add a new user";
                var userArg = cmd.Argument("username", "Username").IsRequired();
                var passArg = cmd.Argument("password", "Password").IsRequired();

                cmd.OnExecute(() =>
                {
                    var logger = _services.GetRequiredService<ILogger<FileUserStore>>();
                    var store = new FileUserStore(logger, GetCurrentDirectoryFileStorePath());
                    if (store.AddUser(userArg.Value, passArg.Value))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"User '{userArg.Value}' added.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"User '{userArg.Value}' already exists.");
                        Console.ResetColor();
                    }
                    return 0;
                });
            });

            app.Command("delete", cmd =>
            {
                cmd.Description = "Delete an existing user";
                var userArg = cmd.Argument("username", "Username").IsRequired();

                cmd.OnExecute(() =>
                {
                    var logger = _services.GetRequiredService<ILogger<FileUserStore>>();
                    var store = new FileUserStore(logger, GetCurrentDirectoryFileStorePath());
                    if (store.DeleteUser(userArg.Value))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"User '{userArg.Value}' deleted.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"User '{userArg.Value}' not found.");
                        Console.ResetColor();
                    }
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 1;
            });

            try
            {
                return await app.ExecuteAsync(args);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
                return 1;
            }
        }

        private static string GetCurrentDirectoryFileStorePath()
        {
            const string dbName = "file_store.dat";
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(currentDirectory, dbName);
        }
    }
}