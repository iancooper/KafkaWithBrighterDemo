using System;
using System.IO;
using System.Threading;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.Inbox;
using Paramore.Brighter.Inbox.Sqlite;
using Paramore.Brighter.MessagingGateway.Kafka;
using Paramore.Brighter.ServiceActivator.Extensions.DependencyInjection;
using Paramore.Brighter.ServiceActivator.Extensions.Hosting;
using Paramore.Brighter.Sqlite;
using Transmogrification.Adapters.Driven;
using Transmogrification.Application.Ports.Driving;
using Transmogrification.Database;
using Transmogrification.Policies;

CancellationTokenSource cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {
    e.Cancel = true; //terminate the pump
    cts.Cancel();
};

var host = CreateHostBuilder(args).Build();
host.CheckDbIsUp();
host.MigrateDatabase();
host.CreateInbox();

var box = new Box();
if (box.StarTransformation())
{
    box.BeginTransforming();

    await host.RunAsync(cts.Token);

    box.EndTransforming();
}

return;

static void AddSchemaRegistry(IServiceCollection services)
{
    var schemaRegistryConfig = new SchemaRegistryConfig { Url = "http://localhost:8081" };
    var cachedSchemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);
    services.AddSingleton<ISchemaRegistryClient>(cachedSchemaRegistryClient);
}

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());
            configurationBuilder.AddJsonFile("appsettings.json", optional: true);
            configurationBuilder.AddJsonFile($"appsettings.{GetEnvironment()}.json", optional: true);
            configurationBuilder
                .AddEnvironmentVariables(
                    prefix: "ASPNETCORE_"); //NOTE: Although not web, we use this to grab the environment
            configurationBuilder.AddEnvironmentVariables(prefix: "BRIGHTER_");
            configurationBuilder.AddCommandLine(args);
        })
        .ConfigureLogging((context, builder) =>
        {
            builder.AddConsole();
            builder.AddDebug();
        })
        .ConfigureServices((hostContext, services) =>
        {
            ConfigureSqlite(hostContext, services);
            ConfigureDapperSqlite(services);
            ConfigureBrighter(hostContext, services);
            ConfigureBox(services);
        })
        .UseConsoleLifetime();

static void ConfigureBrighter(HostBuilderContext hostContext, IServiceCollection services)
{
    AddSchemaRegistry(services);

    Subscription[] subscriptions = GetSubscriptions();

    var relationalDatabaseConfiguration = new RelationalDatabaseConfiguration(GetDevDbConnectionString());
    services.AddSingleton<IAmARelationalDatabaseConfiguration>(relationalDatabaseConfiguration);

    services.AddServiceActivator(options =>
        {
            options.Subscriptions = subscriptions;
            options.ChannelFactory = GetChannelFactory();
            options.UseScoped = true;
            options.HandlerLifetime = ServiceLifetime.Scoped;
            options.MapperLifetime = ServiceLifetime.Singleton;
            options.CommandProcessorLifetime = ServiceLifetime.Scoped;
            options.PolicyRegistry = new SalutationPolicy();
            options.InboxConfiguration = new InboxConfiguration(
                CreateInbox(hostContext, relationalDatabaseConfiguration),
                scope: InboxScope.Commands,
                onceOnly: true,
                actionOnExists: OnceOnlyAction.Throw
            );
        })
        .ConfigureJsonSerialisation((options) =>
        {
            //We don't strictly need this, but added as an example
            options.PropertyNameCaseInsensitive = true;
        })
        .AutoFromAssemblies();

    services.AddHostedService<ServiceActivatorHostedService>();
}

static void ConfigureSqlite(HostBuilderContext hostBuilderContext, IServiceCollection services)
{
    services
        .AddFluentMigratorCore()
        .ConfigureRunner(c =>
        {
            c.AddSQLite()
                .WithGlobalConnectionString(GetDevDbConnectionString())
                .ScanIn(typeof(Transmogrification_Migrations.Migrations.SqlInitialMigrations).Assembly).For.Migrations();
        });
}

static void ConfigureDapperSqlite(IServiceCollection services)
{
    services.AddScoped<IAmARelationalDbConnectionProvider, SqliteConnectionProvider>();
    services.AddScoped<IAmATransactionConnectionProvider, SqliteUnitOfWork>();
}

static void ConfigureBox(IServiceCollection services)
{
    services.AddSingleton<IBox>(new Box());
}

static IAmAnInbox CreateInbox(HostBuilderContext hostContext, IAmARelationalDatabaseConfiguration configuration)
{
    return new SqliteInbox(configuration);
}

static IAmAChannelFactory GetChannelFactory()
{
    return new ChannelFactory(
        new KafkaMessageConsumerFactory(
            new KafkaMessagingGatewayConfiguration
            {
                Name = "paramore.brighter", BootStrapServers = new[] { "localhost:9092" }
            }
        )
    );
}

static string GetEnvironment()
{
    //NOTE: Hosting Context will always return Production outside of ASPNET_CORE at this point, so grab it directly
    return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
}

static string GetDevDbConnectionString()
{
    return "Filename=TransmogrificationMade.sqlite;Cache=Shared";
}

static KafkaSubscription[] GetSubscriptions()
{
    var subscriptions = new KafkaSubscription[]
    {
        new KafkaSubscription<TransmogrificationMade>(
            new SubscriptionName("paramore.sample.transmogrification"),
            channelName: new ChannelName("TransmogrificationMade"),
            routingKey: new RoutingKey("TransmogrificationMade"),
            groupId: "Transmogrification-History",
            timeoutInMilliseconds: 100,
            offsetDefault: AutoOffsetReset.Earliest,
            commitBatchSize: 5,
            sweepUncommittedOffsetsIntervalMs: 10000,
            runAsync: true,
            makeChannels: OnMissingChannel.Create)
    };
    return subscriptions;
}