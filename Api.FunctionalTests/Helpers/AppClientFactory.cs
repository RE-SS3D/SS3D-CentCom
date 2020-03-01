using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Api.Models;

namespace Api.SystemTests.Helpers
{
    public class AppClientFactory : IDisposable
    {
        public AppClientFactory()
        {
            Client = GetClient();
        }

        public HttpClient Client { get; private set; }

        // May be null
        public WebApplicationFactory<Startup> Factory { get; private set; }

        private HttpClient GetClient()
        {
            string url = Environment.GetEnvironmentVariable("CENTCOM_API_TEST_URL");
            bool isLocal = string.IsNullOrEmpty(url);

            if (isLocal)
            {
                Factory = CreateLocalClientInstance();
                return Factory.CreateClient();
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            return client;
        }

        private WebApplicationFactory<Startup> CreateLocalClientInstance()
        {
            /*
             * This code replaces the database connection with a locally running instance
             */

            return new WebApplicationFactory<Startup>().WithWebHostBuilder(builder =>
            {
                // Add our own app settings to the builder.
                builder.ConfigureAppConfiguration((context, builder) =>
                {
                    builder
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", false, true);
                });

                builder.ConfigureServices((context, services) =>
                {
                    // Remove the app's previous database connection
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DataContext>));

                    if (descriptor != null) {
                        services.Remove(descriptor);
                    }

                    // Add a LocalDB instance declared in Visual Studio
                    services.AddDbContext<DataContext>(options =>
                        options.UseSqlServer(context.Configuration.GetConnectionString("LocalTestConnection"))
                    );

                    // Build the service provider.
                    var sp = services.BuildServiceProvider();

                    // Create a scope to obtain a reference to the database
                    // context (DataContext).
                    using (var scope = sp.CreateScope()) {
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<DataContext>();
                        var logger = scopedServices.GetRequiredService<ILogger<AppClientFactory>>();

                        // Ensure the database is created.
                        // Don't bother about migrations as the database is remade every time.
                        db.Database.EnsureCreated();

                        try {
                            // Seed the database with test data.
                            DatabaseSeed.Initialize(db);
                        }
                        catch (Exception ex) {
                            logger.LogError(ex, "An error occurred seeding the " +
                                "database with test messages. Error: {Message}", ex.Message);
                        }
                    }
                });
                
            });
        }

        public void Dispose()
        {
            Factory?.Dispose();
        }
    }
}
