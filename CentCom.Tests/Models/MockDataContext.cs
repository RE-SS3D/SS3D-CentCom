using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CentCom.Models;

namespace CentCom.Tests.Models
{
    /**
     * <summary>
     * This sets up the DataContext to use a mock in-memory version appropriate for unit testing.
     * <see href="https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/sqlite">Microsoft has more documentation on this process.</see>
     * </summary>
     * <remarks>
     * Uses an in-memory SQL database. If this takes too long we could switch to plain in-memory,
     * although this does not verify relational data.
     * </remarks>
     */
    static class MockDataContext
    {
        public static DataContext GetContext() => new DataContext(options);

        /**
         * Clears the database and resets it to standard.
         */
        public static void Reset()
        {
            // For now we completely recreate the in-memory connection. If this is too costly we should
            // Find a way of just clearing the current db connection instead

            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite(connection)
                .Options;

            // Create the schema in the database
            using (var context = new DataContext(options))
            {
                context.Database.EnsureCreated();
            }
        }

        private static SqliteConnection connection;
        private static DbContextOptions<DataContext> options;
    }
}
