using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorRss.App.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorRss.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = BuildWebHost(args);

            using (var scope = host.Services.CreateScope())
                await ExecuteAditionalServiceSetupAsync(scope);

            host.Run();
        }

        private static async Task ExecuteAditionalServiceSetupAsync(IServiceScope scope)
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            // await SeedWithSampleDataAsync(services.GetRequiredService<ApplicationDbContext>(), logger);
            await InitializeDatabase(services.GetRequiredService<ApplicationDbContext>());
        }

        private static async Task InitializeDatabase(ApplicationDbContext context)
            => await context.Database.EnsureCreatedAsync();

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .AddJsonFile("appsettings.json", false)
                    // .AddJsonFile("appsettings.json", true)
                    .Build())
                .UseStartup<Startup>()
                .UseUrls("http://*:5000/")
                .Build();
    }
}
