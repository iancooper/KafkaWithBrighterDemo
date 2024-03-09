using System;
using System.Data;
using System.Data.Common;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paramore.Brighter.Outbox.Sqlite;
using Polly;

namespace GreetingsWeb.Database
{
    public static class SchemaCreation
    {
        private const string OUTBOX_TABLE_NAME = "Outbox";

        public static IHost CheckDbIsUp(this IHost webHost)
        {
            using var scope = webHost.Services.CreateScope();

            var services = scope.ServiceProvider;
            var env = services.GetService<IWebHostEnvironment>();
            var config = services.GetService<IConfiguration>();
            var  connectionString = GetConnectionString();

            //We don't check db availability in development as we always use Sqlite which is a file not a server
            if (env.IsDevelopment()) return webHost;

            WaitToConnect(connectionString);
            CreateDatabaseIfNotExists(GetDbConnection(connectionString));

            return webHost;
        }
        
        public static IHost CreateOutbox(this IHost webHost, bool hasBinaryPayload)
        {
            using var scope = webHost.Services.CreateScope();
            var services = scope.ServiceProvider;
            var env = services.GetService<IWebHostEnvironment>();
            var config = services.GetService<IConfiguration>();

            CreateOutbox();

            return webHost;
        }

        public static IHost MigrateDatabase(this IHost webHost)
        {
            using (var scope = webHost.Services.CreateScope())
            {
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
            }

            return webHost;
        }


        private static void CreateDatabaseIfNotExists(DbConnection conn)
        {
            //The migration does not create the Db, so we need to create it sot that it will add it
            conn.Open();
            using var command = conn.CreateCommand();

            command.CommandText = "CREATE DATABASE IF NOT EXISTS Greetings";
            try
            {
                command.ExecuteScalar();
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Issue with creating Greetings tables, {e.Message}");
                //Rethrow, if we can't create the Outbox, shut down
                throw;
            }
        }

        private static void CreateOutbox()
        {
            try
            {
                using var sqlConnection = new SqliteConnection(GetConnectionString());
                sqlConnection.Open();

                using var exists = sqlConnection.CreateCommand();
                exists.CommandText = SqliteOutboxBuilder.GetExistsQuery(OUTBOX_TABLE_NAME);
                using var reader = exists.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                }
                else
                {
                    using var command = sqlConnection.CreateCommand();
                    command.CommandText = SqliteOutboxBuilder.GetDDL(OUTBOX_TABLE_NAME, true);
                    command.ExecuteScalar();
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Issue with creating Outbox table, {e.Message}");
                //Rethrow, if we can't create the Outbox, shut down
                throw;
            }
        }

        private static string GetConnectionString()
        {
            return "Filename=Greetings.db;Cache=Shared";
        }

        private static DbConnection GetDbConnection(string connectionString)
        {
            return  new SqliteConnection(connectionString);
        }
        
        private static void WaitToConnect(string connectionString)
        {
            var policy = Policy.Handle<DbException>().WaitAndRetryForever(
                retryAttempt => TimeSpan.FromSeconds(2),
                (exception, timespan) =>
                {
                    Console.WriteLine($"Healthcheck: Waiting for the database {connectionString} to come online - {exception.Message}");
                });

            policy.Execute(() =>
            {
                using var conn = GetDbConnection(connectionString);
                conn.Open();
            });
        }
 
    }
}
