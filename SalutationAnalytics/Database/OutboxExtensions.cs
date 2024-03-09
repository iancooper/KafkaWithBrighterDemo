using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paramore.Brighter;
using Paramore.Brighter.Outbox.Sqlite;
using Paramore.Brighter.Sqlite;

namespace SalutationAnalytics.Database
{

    public class OutboxExtensions
    {
        public static (IAmAnOutbox, Type, Type) MakeOutbox(
            HostBuilderContext hostContext,
            RelationalDatabaseConfiguration configuration,
            IServiceCollection services)
        {
            return (new SqliteOutbox(configuration), typeof(SqliteConnectionProvider), typeof(SqliteUnitOfWork));
        }
    }
}
