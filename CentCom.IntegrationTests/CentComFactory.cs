using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CentCom.Models;

namespace CentCom.IntegrationTests
{
    public class CentComFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        /**
         * Modifies the centcom configuration, specifically using an inline database instead.
         */
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            builder.ConfigureServices(services =>
            {
                // Remove the app's ApplicationDbContext registration.
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DataContext>));
                if (descriptor != null) {
                    services.Remove(descriptor);
                }

                // Keep the connection open until shutdown.
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                // Add ApplicationDbContext using an in-memory database for testing.
                services.AddDbContext<DataContext>(options => options.UseSqlite(connection));

                // Build the service provider.
                var sp = services.BuildServiceProvider();

                // Create a scope to obtain a reference to the database context (DataContext).
                using (var scope = sp.CreateScope()) {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<DataContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<CentComFactory<TStartup>>>();

                    // Ensure the database is created.
                    db.Database.EnsureCreated();

                    try {
                        // Seed the database with test data.
                        Utilities.InitializeDbForTests(db);
                    }
                    catch (Exception ex) {
                        logger.LogError(ex, "An error occurred seeding the database with test messages. Error: {Message}", ex.Message);
                    }
                }
            });
        }
    }
}
