using System;
using Confluent.SchemaRegistry;
using FluentMigrator.Runner;
using Greetings_MySqlMigrations.Migrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.Extensions.Hosting;
using Paramore.Brighter.MessagingGateway.Kafka;
using Paramore.Darker.AspNetCore;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using Paramore.Brighter.Outbox.Sqlite;
using Paramore.Brighter.Sqlite;
using Transmogrifier.Application.Ports.Driving;
using Transmogrifier.Policies;

namespace Transmogrifier
{
    public class Startup
    {
        
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        private void AddSchemaRegistry(IServiceCollection services)
        {
            var schemaRegistryConfig = new SchemaRegistryConfig { Url = "http://localhost:8081" };
            var cachedSchemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);
            services.AddSingleton<ISchemaRegistryClient>(cachedSchemaRegistryClient);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GreetingsAPI v1"));
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore().AddApiExplorer();
            services.AddControllers(options =>
                {
                    options.RespectBrowserAcceptHeader = true;
                })
                .AddXmlSerializerFormatters();
            services.AddProblemDetails();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "GreetingsAPI", Version = "v1" });
            });

            services.AddOpenTelemetry()
                .WithTracing(builder => builder
                    .AddAspNetCoreInstrumentation()
                    .AddConsoleExporter())
                .WithMetrics(builder => builder
                    .AddAspNetCoreInstrumentation()
                    );

            ConfigureSqlite(services);
            ConfigureBrighter(services);
            ConfigureDarker(services);
        }

        private void ConfigureSqlite(IServiceCollection services)
        {
            services
                .AddFluentMigratorCore()
                .ConfigureRunner(c => c.AddSQLite()
                    .WithGlobalConnectionString(DbConnectionString())
                    .ScanIn(typeof(SqlInitialCreate).Assembly).For.Migrations()
                );
        }

        private void ConfigureBrighter(IServiceCollection services)
        {
            AddSchemaRegistry(services);

            var outboxConfiguration = new RelationalDatabaseConfiguration(
                DbConnectionString(),
                binaryMessagePayload: true
            );
            services.AddSingleton<IAmARelationalDatabaseConfiguration>(outboxConfiguration);

            services.AddBrighter(options =>
                {
                    //we want to use scoped, so make sure everything understands that which needs to
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.CommandProcessorLifetime = ServiceLifetime.Scoped;
                    options.MapperLifetime = ServiceLifetime.Singleton;
                    options.PolicyRegistry = new GreetingsPolicy();
                })
                .UseExternalBus((configure) =>
                {
                    configure.ProducerRegistry = ConfigureProducerRegistry();
                    configure.Outbox = new SqliteOutbox(outboxConfiguration);
                    configure.TransactionProvider = typeof(SqliteUnitOfWork);
                    configure.ConnectionProvider =  typeof(SqliteConnectionProvider);
                })
                .UseOutboxSweeper(options => {
                    options.TimerInterval = 5;
                    options.MinimumMessageAge = 5000;
                 })
                .AutoFromAssemblies(typeof(AddPersonHandlerAsync).Assembly);
        }

        private void ConfigureDarker(IServiceCollection services)
        {
            services.AddDarker(options =>
                {
                    options.HandlerLifetime = ServiceLifetime.Scoped;
                    options.QueryProcessorLifetime = ServiceLifetime.Scoped;
                })
                .AddHandlersFromAssemblies(typeof(FindPersonByNameHandlerAsync).Assembly)
                .AddJsonQueryLogging()
                .AddPolicies(new GreetingsPolicy());
        }

        private static IAmAProducerRegistry ConfigureProducerRegistry()
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
                            Topic = new RoutingKey("TransmogrificationMade"),
                            MessageSendMaxRetries = 3,
                            MessageTimeoutMs = 1000,
                            MaxInFlightRequestsPerConnection = 1,
                            MakeChannels = OnMissingChannel.Create
                        }
                    })
                .Create();
            
            return producerRegistry;
        }

        private string DbConnectionString()
        {
            return "Filename=Transmogrifications.sqlite;Cache=Shared";
        }
    }
}
