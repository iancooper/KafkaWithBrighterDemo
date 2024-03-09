﻿using System;
using System.IO;
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
using Paramore.Brighter.Outbox.Sqlite;
using Paramore.Brighter.ServiceActivator.Extensions.DependencyInjection;
using Paramore.Brighter.ServiceActivator.Extensions.Hosting;
using Paramore.Brighter.Sqlite;
using SalutationAnalytics.Application.Ports.Driving;
using SalutationAnalytics.Database;
using SalutationAnalytics.Messaging;
using SalutationAnalytics.Policies;

var host = CreateHostBuilder(args).Build();
host.CheckDbIsUp();
host.MigrateDatabase();
host.CreateInbox();
host.CreateOutbox(HasBinaryMessagePayload());
await host.RunAsync();
return;

static void AddSchemaRegistryMaybe(IServiceCollection services, MessagingTransport messagingTransport)
{
    if (messagingTransport != MessagingTransport.Kafka) return;

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
        })
        .UseConsoleLifetime();

static void ConfigureBrighter(HostBuilderContext hostContext, IServiceCollection services)
{
    var messagingTransport = GetTransportType(hostContext.Configuration[MessagingGlobals.BRIGHTER_TRANSPORT]);

    AddSchemaRegistryMaybe(services, messagingTransport);

    Subscription[] subscriptions = GetSubscriptions(messagingTransport);

    var relationalDatabaseConfiguration = new RelationalDatabaseConfiguration(GetDevDbConnectionString());
    services.AddSingleton<IAmARelationalDatabaseConfiguration>(relationalDatabaseConfiguration);

    var outboxConfiguration = new RelationalDatabaseConfiguration(
        GetDevDbConnectionString(),
        binaryMessagePayload: messagingTransport == MessagingTransport.Kafka
    );
    services.AddSingleton<IAmARelationalDatabaseConfiguration>(outboxConfiguration);

    (IAmAnOutbox outbox, Type connectionProvider, Type transactionProvider) makeOutbox =
        (new SqliteOutbox(outboxConfiguration), typeof(SqliteConnectionProvider), typeof(SqliteUnitOfWork));

    services.AddServiceActivator(options =>
        {
            options.Subscriptions = subscriptions;
            options.ChannelFactory = GetChannelFactory(messagingTransport);
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
        .UseExternalBus((config) =>
        {
            config.ProducerRegistry = ConfigureProducerRegistry(messagingTransport);
            config.Outbox = makeOutbox.outbox;
            config.ConnectionProvider = makeOutbox.connectionProvider;
            config.TransactionProvider = makeOutbox.transactionProvider;
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
                .ScanIn(typeof(Salutations_Migrations.Migrations.SqlInitialMigrations).Assembly).For.Migrations();
        });
}

static void ConfigureDapperSqlite(IServiceCollection services)
{
    services.AddScoped<IAmARelationalDbConnectionProvider, SqliteConnectionProvider>();
    services.AddScoped<IAmATransactionConnectionProvider, SqliteUnitOfWork>();
}

static IAmAnInbox CreateInbox(HostBuilderContext hostContext, IAmARelationalDatabaseConfiguration configuration)
{
    return new SqliteInbox(configuration);
}

static IAmAProducerRegistry ConfigureProducerRegistry(MessagingTransport messagingTransport)
{
    var producerRegistry = new KafkaProducerRegistryFactory(
            new KafkaMessagingGatewayConfiguration
            {
                Name = "paramore.brighter.greetingsender", BootStrapServers = new[] { "localhost:9092" }
            },
            new KafkaPublication[]
            {
                new KafkaPublication
                {
                    Topic = new RoutingKey("SalutationReceived"),
                    MessageSendMaxRetries = 3,
                    MessageTimeoutMs = 1000,
                    MaxInFlightRequestsPerConnection = 1,
                    MakeChannels = OnMissingChannel.Create
                }
            })
        .Create();

    return producerRegistry;
}

static IAmAChannelFactory GetChannelFactory(MessagingTransport messagingTransport)
{
    return new Paramore.Brighter.MessagingGateway.Kafka.ChannelFactory(
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
    return "Filename=Salutations.db;Cache=Shared";
}

static IAmAChannelFactory GetKafkaChannelFactory()
{
    return new Paramore.Brighter.MessagingGateway.Kafka.ChannelFactory(
        new KafkaMessageConsumerFactory(
            new KafkaMessagingGatewayConfiguration
            {
                Name = "paramore.brighter", BootStrapServers = new[] { "localhost:9092" }
            }
        )
    );
}

static IAmAProducerRegistry GetKafkaProducerRegistry()
{
    var producerRegistry = new KafkaProducerRegistryFactory(
            new KafkaMessagingGatewayConfiguration
            {
                Name = "paramore.brighter.greetingsender", BootStrapServers = new[] { "localhost:9092" }
            },
            new KafkaPublication[]
            {
                new KafkaPublication
                {
                    Topic = new RoutingKey("SalutationReceived"),
                    MessageSendMaxRetries = 3,
                    MessageTimeoutMs = 1000,
                    MaxInFlightRequestsPerConnection = 1,
                    MakeChannels = OnMissingChannel.Create
                }
            })
        .Create();

    return producerRegistry;
}

static Subscription[] GetSubscriptions(MessagingTransport messagingTransport)
{
    var subscriptions = new KafkaSubscription[]
    {
        new KafkaSubscription<GreetingMade>(
            new SubscriptionName("paramore.sample.salutationanalytics"),
            channelName: new ChannelName("SalutationAnalytics"),
            routingKey: new RoutingKey("GreetingMade"),
            groupId: "kafka-GreetingsReceiverConsole-Sample",
            timeoutInMilliseconds: 100,
            offsetDefault: AutoOffsetReset.Earliest,
            commitBatchSize: 5,
            sweepUncommittedOffsetsIntervalMs: 10000,
            runAsync: true,
            makeChannels: OnMissingChannel.Create)
    };
    return subscriptions;
}

static MessagingTransport GetTransportType(string brighterTransport)
{
    return brighterTransport switch
    {
        MessagingGlobals.RMQ => MessagingTransport.Rmq,
        MessagingGlobals.KAFKA => MessagingTransport.Kafka,
        _ => throw new ArgumentOutOfRangeException(nameof(MessagingGlobals.BRIGHTER_TRANSPORT),
            "Messaging transport is not supported")
    };
}

static bool HasBinaryMessagePayload()
{
    return GetTransportType(Environment.GetEnvironmentVariable("BRIGHTER_TRANSPORT")) == MessagingTransport.Kafka;
}
