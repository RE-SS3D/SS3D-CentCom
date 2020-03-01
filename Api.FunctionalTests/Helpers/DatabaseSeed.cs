using System;
using System.Collections.Generic;
using System.Text;
using Api.Models;

namespace Api.SystemTests.Helpers
{
    public static class DatabaseSeed
    {
        public static void Initialize(DataContext context)
        {
            context.Servers.RemoveRange(context.Servers);

            context.SaveChanges();
        }
    }
}
