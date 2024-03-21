using System;
using System.Data;
using System.Data.Common;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paramore.Brighter.Inbox.Sqlite;
using Paramore.Brighter.Outbox.Sqlite;
using Polly;

namespace Transmogrification.Database
{
    public static class SchemaCreation
    {
        internal const string INBOX_TABLE_NAME = "Inbox";
        internal const string OUTBOX_TABLE_NAME = "Outbox";

        public static IHost CheckDbIsUp(this IHost host)
        {
            using var scope = host.Services.CreateScope();

            var services = scope.ServiceProvider;
            var env = services.GetService<IHostEnvironment>();
            var config = services.GetService<IConfiguration>();
            var connectionString = DbConnectionString();

            //We don't check db availability in development as we always use Sqlite which is a file not a server
            if (env.IsDevelopment()) return host;

            WaitToConnect(connectionString);
            CreateDatabaseIfNotExists((DbConnection)new SqliteConnection());

            return host;
        }

        public static IHost CreateInbox(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var env = services.GetService<IHostEnvironment>();
                var config = services.GetService<IConfiguration>();

                CreateInbox(config, env);
            }

            return host;
        }
        
        public static IHost CreateOutbox(this IHost webHost, bool hasBinaryMessagePayload)
        {
            using var scope = webHost.Services.CreateScope();
            var services = scope.ServiceProvider;
            var env = services.GetService<IHostEnvironment>();
            var config = services.GetService<IConfiguration>();

            CreateOutbox(config, env, hasBinaryMessagePayload);

            return webHost;
        }

        public static IHost MigrateDatabase(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var runner = services.GetRequiredService<IMigrationRunner>();
                runner.ListMigrations();
                runner.MigrateUp();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
                throw;
            }

            return host;
        }

        private static void CreateDatabaseIfNotExists(DbConnection conn)
        {
            //The migration does not create the Db, so we need to create it sot that it will add it
            conn.Open();
            using var command = conn.CreateCommand();

            command.CommandText = "CREATE DATABASE IF NOT EXISTS TransmogrificationMade";

            try
            {
                command.ExecuteScalar();
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Issue with creating TransmogrificationMade tables, {e.Message}");
                //Rethrow, if we can't create the Outbox, shut down
                throw;
            }
        }
        private static void CreateInbox(IConfiguration config, IHostEnvironment env)
        {
            try
            {
                var connectionString = DbConnectionString();

                CreateInboxSqlite(connectionString);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Issue with creating Inbox table, {e.Message}");
                throw;
            }
        }

        private static void CreateInboxSqlite(string connectionString)
        {
            using var sqlConnection = new SqliteConnection(connectionString);
            sqlConnection.Open();

            using var exists = sqlConnection.CreateCommand();
            exists.CommandText = SqliteInboxBuilder.GetExistsQuery(INBOX_TABLE_NAME);
            using var reader = exists.ExecuteReader(CommandBehavior.SingleRow);

            if (reader.HasRows) return;

            using var command = sqlConnection.CreateCommand();
            command.CommandText = SqliteInboxBuilder.GetDDL(INBOX_TABLE_NAME);
            command.ExecuteScalar();
        }


        private static void CreateOutbox(IConfiguration config, IHostEnvironment env, bool hasBinaryMessagePayload)
        {
            try
            {
                var connectionString = DbConnectionString();

                CreateOutboxSqlite(connectionString, hasBinaryMessagePayload);
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Issue with creating Outbox table, {e.Message}");
                //Rethrow, if we can't create the Outbox, shut down
                throw;
            }
        }

        private static void CreateOutboxSqlite(string connectionString, bool hasBinaryMessagePayload)
        {
            using var sqlConnection = new SqliteConnection(connectionString);
            sqlConnection.Open();

            using var exists = sqlConnection.CreateCommand();
            exists.CommandText = SqliteOutboxBuilder.GetExistsQuery(OUTBOX_TABLE_NAME);
            using var reader = exists.ExecuteReader(CommandBehavior.SingleRow);

            if (reader.HasRows) return;

            using var command = sqlConnection.CreateCommand();
            command.CommandText = SqliteOutboxBuilder.GetDDL(OUTBOX_TABLE_NAME, hasBinaryMessagePayload);
            command.ExecuteScalar();
        }

        private static string DbConnectionString()
        {
            //NOTE: Sqlite needs to use a shared cache to allow Db writes to the Outbox as well as entities
            return "Filename=TransmogrificationMade.sqlite;Cache=Shared" ; 
        }


        private static void WaitToConnect(string connectionString)
        {
            var policy = Policy.Handle<SqliteException>().WaitAndRetryForever(
                retryAttempt => TimeSpan.FromSeconds(2),
                (exception, timespan) =>
                {
                    Console.WriteLine($"Healthcheck: Waiting for the database {connectionString} to come online - {exception.Message}");
                });

            policy.Execute(() =>
            {
                using var conn = new SqliteConnection(connectionString);
                conn.Open();
            });
        }
    }
}
