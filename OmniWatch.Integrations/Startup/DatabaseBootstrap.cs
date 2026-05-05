using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniWatch.Integrations.Persistence;

namespace OmniWatch.Integrations.Startup
{
    public class DatabaseBootstrap
    {
        private readonly IServiceProvider _sp;

        public DatabaseBootstrap(IServiceProvider sp)
        {
            _sp = sp;
        }

        public void Initialize()
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            bool recreate = false;

            try
            {
                db.Database.EnsureCreated();

                if (!TableExists(db, "StormTracks")) recreate = true;
                if (!TableExists(db, "StormPoints")) recreate = true;
                if (!TableExists(db, "Metadata")) recreate = true;
            }
            catch
            {
                recreate = true;
            }

            if (recreate)
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
        }

        private static bool TableExists(NoaaCacheContext db, string tableName)
        {
            using var conn = db.Database.GetDbConnection();

            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT 1
                FROM sqlite_master
                WHERE type='table' AND name=$name
                LIMIT 1;";

            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = tableName;
            cmd.Parameters.Add(p);

            return cmd.ExecuteScalar() != null;
        }
    }
}