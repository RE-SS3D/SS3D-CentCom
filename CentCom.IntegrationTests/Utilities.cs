using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using CentCom.Models;

namespace CentCom.IntegrationTests
{
    public static class Utilities
    {
        public static readonly Server[] SEED_SERVERS = new Server[] {
            new Server { Address = new IPEndPoint(100, 1), Name = "Server A", LastUpdate = DateTime.Now },
            new Server { Address = new IPEndPoint(200, 2), Name = "Server B", LastUpdate = DateTime.Now },
            new Server { Address = new IPEndPoint(300, 3), Name = "Server C", LastUpdate = DateTime.Now - TimeSpan.FromMinutes(10) },
            new Server { Address = new IPEndPoint(400, 4), Name = "Server D", LastUpdate = DateTime.Now }
        };

        /**
         * Setup the database with some initial items.
         */
        public static void InitializeDbForTests(DataContext db)
        {
            if (db is null)
                throw new ArgumentNullException(nameof(db));

            db.Servers.RemoveRange(db.Servers);
            db.Servers.AddRange(SEED_SERVERS);
            db.SaveChanges();
        }
    }
}
