using InstantVoiceRoom.Framework.Data;
using InstantVoiceRoom.Framework.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

            if (!IsInWebApplicationPath())
            {
                Console.WriteLine("CLI should be in web application path");
                return 1;
            }

            var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            serviceCollection.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("InstantVoiceRoom.Framework")));


            serviceCollection.AddScoped<IUserService, UserService>();

            // Build the service provider once all services are registered.
            _services = serviceCollection.BuildServiceProvider();


            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }


            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(_services);

            app.Command("user-add", cmd =>
            {
                cmd.Description = "Add a new user";
                var userArg = cmd.Argument("username", "Username").IsRequired();
                var passArg = cmd.Argument("password", "Password").IsRequired();

                cmd.OnExecuteAsync(async (CancellationToken c) =>
                {
                    var service = _services.GetRequiredService<IUserService>();
                    if (await service.AddUser(userArg.Value, passArg.Value))
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

            app.Command("user-delete", cmd =>
            {
                cmd.Description = "Delete an existing user";
                var userArg = cmd.Argument("username", "Username").IsRequired();

                cmd.OnExecuteAsync(async (CancellationToken c) =>
                {
                    var service = _services.GetRequiredService<IUserService>(); ;
                    if (await service.DeleteUser(userArg.Value))
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

        private static string GetCurrentDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static bool IsInWebApplicationPath()
        {
            var foundWebDlls = Directory.GetFiles(GetCurrentDirectory()).Any(dd => dd.Contains("InstantVoiceRoom.Web"));
            return foundWebDlls;
        }
    }
}