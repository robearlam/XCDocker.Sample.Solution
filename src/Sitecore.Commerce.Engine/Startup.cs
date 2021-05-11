// © 2015 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using IdentityModel.Client;
using IdentityServer4.AccessTokenValidation;
using Microsoft.ApplicationInsights;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Plugin.Sample.Payments.Braintree;
using Serilog;
using Serilog.Events;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Logging;
using Sitecore.Commerce.Plugin.Rules;
using Sitecore.Commerce.Plugin.SQL;
using Sitecore.Framework.Caching;
using Sitecore.Framework.Common;
using Sitecore.Framework.Common.MatchingOptions;
using Sitecore.Framework.Diagnostics;
using Sitecore.Framework.Rules;
using Sitecore.Framework.Rules.Serialization;
using Sitecore.Framework.TransientFaultHandling;
using Sitecore.Framework.TransientFaultHandling.EntLib;
using Sitecore.Framework.TransientFaultHandling.Redis;
using Sitecore.Framework.TransientFaultHandling.Sql;
using SqlExponentialRetryOptions = Sitecore.Framework.TransientFaultHandling.Sql.ExponentialRetryOptions;
using RedisExponentialRetryOptions = Sitecore.Framework.TransientFaultHandling.Redis.ExponentialRetryOptions;

#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
namespace Sitecore.Commerce.Engine
{
    /// <summary>
    /// Defines the commerce engine startup.
    /// </summary>
    public class Startup
    {
        private readonly string _nodeInstanceId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        private readonly IWebHostEnvironment _hostEnv;
        private volatile CommerceEnvironment _environment;
        private volatile NodeContext _nodeContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="hostEnv">The hosting environment.</param>
        /// <param name="configuration">The configuration.</param>
        public Startup(
            IWebHostEnvironment hostEnv,
            IConfiguration configuration)
        {
            _hostEnv = hostEnv;

            Configuration = configuration;

            if (!bool.TryParse(Configuration.GetSection("Logging:SerilogLoggingEnabled")?.Value, out bool serilogEnabled))
            {
                return;
            }

            if (!serilogEnabled)
            {
                return;
            }

            if (!long.TryParse(Configuration.GetSection("Serilog:FileSizeLimitBytes").Value, out long fileSize))
            {
                fileSize = 100000000;
            }

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
                .Enrich.With(new ScLogEnricher())
                .WriteTo.Async(a => a.File(
                    $@"{Path.Combine(_hostEnv.WebRootPath, "logs")}\SCF.{DateTimeOffset.UtcNow:yyyyMMdd}.log.{_nodeInstanceId}.txt",
                    GetSerilogLogLevel(),
                    "{ThreadId:D5} {Timestamp:HH:mm:ss} {ScLevel} {Message}{NewLine}{Exception}",
                    fileSizeLimitBytes: fileSize,
                    rollOnFileSizeLimit: true), bufferSize: 500)
                .CreateLogger();
        }

        /// <summary>
        /// Gets or sets the Initial Startup Environment. This will tell the Node how to behave
        /// This will be overloaded by the Environment stored in configuration.
        /// </summary>
        /// <value>
        /// The startup environment.
        /// </value>
        public CommerceEnvironment StartupEnvironment
        {
            get => _environment ?? (_environment = new CommerceEnvironment
            {
                Name = "Bootstrap"
            });
            set => _environment = value;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<LoggingSettings>(options => Configuration.GetSection("Logging").Bind(options));
            services.Configure<ApplicationInsightsSettings>(options => Configuration.GetSection("ApplicationInsights").Bind(options));
            services.Configure<CommerceConnectorSettings>(Configuration.GetSection(CommerceConnectorSettings.Section));

            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddControllers().AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new CommerceContractResolver());
            services.AddOData();
            services.AddCors();
            services.AddHttpContextAccessor();
            services.AddWebEncoders();

            // Identity Server settings 
            string authority = Configuration.GetSection("AppSettings:SitecoreIdentityServerUrl").Value;
            string internalAuthority = Configuration.GetSection("AppSettings:InternalSitecoreIdentityServerUrl").Value;
            const string apiName = "EngineAPI";
            const string apiSecret = "secret";
            const string nameClaimType = "name";
            const string roleClaimType = "role";
            static string InternalTokenRetriever(HttpRequest request) => request.HttpContext.Items["idsrv4:tokenvalidation:token"] as string;

            services
                .AddAuthentication(
                    options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                .AddIdentityServerAuthentication(
                    IdentityServerAuthenticationDefaults.AuthenticationScheme,
                    jwtBearerOptions =>
                    {
                        jwtBearerOptions.Authority = authority;
                        jwtBearerOptions.RequireHttpsMetadata = false;
                        jwtBearerOptions.RefreshOnIssuerKeyNotFound = true;
                        jwtBearerOptions.SaveToken = true;
                        jwtBearerOptions.Audience = apiName;
                        jwtBearerOptions.TokenValidationParameters.NameClaimType = nameClaimType;
                        jwtBearerOptions.TokenValidationParameters.RoleClaimType = roleClaimType;

                        if (!string.IsNullOrEmpty(internalAuthority))
                        {
                            var httpDocumentRetriever =
                                new HttpDocumentRetrieverAuthorityReplacer(false, internalAuthority);

                            jwtBearerOptions.ConfigurationManager =
                                new ConfigurationManager<OpenIdConnectConfiguration>(
                                    internalAuthority.EnsureSuffix("/") + ".well-known/openid-configuration",
                                    new OpenIdConnectConfigurationRetriever(), httpDocumentRetriever);
                        }

                        var handler = new JwtSecurityTokenHandler
                        {
                            MapInboundClaims = false
                        };

                        jwtBearerOptions.SecurityTokenValidators.Clear();
                        jwtBearerOptions.SecurityTokenValidators.Add(handler);
                    },
                    introspectionOptions =>
                    {
                        introspectionOptions.Authority = authority;
                        introspectionOptions.EnableCaching = false;
                        introspectionOptions.ClientId = apiName;
                        introspectionOptions.ClientSecret = apiSecret;
                        introspectionOptions.DiscoveryPolicy.RequireHttps = false;
                        introspectionOptions.NameClaimType = nameClaimType;
                        introspectionOptions.RoleClaimType = roleClaimType;
                        introspectionOptions.SaveToken = true;
                        introspectionOptions.DiscoveryPolicy = new DiscoveryPolicy();
                        introspectionOptions.TokenRetriever = InternalTokenRetriever;
                    });
            services.AddAuthorization();

            if (Configuration.GetSection("HealthCheck:Enabled").Get<bool>())
            {
                services.AddHealthChecks();
            }

            Log.Information("Bootstrapping Application ...");
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<Startup>>();
            _nodeContext = new NodeContext(logger, serviceProvider.GetService<TelemetryClient>())
            {
                CorrelationId = _nodeInstanceId,
                ConnectionId = "Node_Global",
                ContactId = "Node_Global",
                GlobalEnvironment = StartupEnvironment,
                Environment = StartupEnvironment,
                WebRootPath = _hostEnv.WebRootPath,
                LoggingPath = _hostEnv.WebRootPath + @"\logs\"
            };

            StartupEnvironment = GetGlobalEnvironment();
            services.AddSingleton(StartupEnvironment);

            _nodeContext.Environment = StartupEnvironment;
            _nodeContext.GlobalEnvironment = StartupEnvironment;
            _nodeContext.CommerceEngineConnectClientId = Configuration.GetSection("CommerceConnector:ClientId").Value;
            services.AddSingleton(_nodeContext);

            AddAntiforgery(services);
            AddCompression(services);

            services.Sitecore()
                .Eventing()
                .Rules()
                .BootstrapProduction(serviceProvider)
                .ConfigureCommercePipelines();
            services.Add(new ServiceDescriptor(typeof(IRuleBuilderInit), typeof(RuleBuilder), ServiceLifetime.Transient));
            services.Replace(new ServiceDescriptor(typeof(IRuleMetadataMapper), typeof(CommerceRuleMetadataMapper), ServiceLifetime.Singleton));
            AddCaching(services);
            AddTransientFaultHandling(services, logger);
            SetupDataProtection(services, logger);

            _nodeContext.AddObject(services);
        }

        /// <summary>
        /// Configures the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="configureServiceApiPipeline">The context pipeline.</param>
        /// <param name="startNodePipeline">The start node pipeline.</param>
        /// <param name="configureOpsServiceApiPipeline">The context ops service API pipeline.</param>
        /// <param name="startEnvironmentPipeline">The start environment pipeline.</param>
        /// <param name="loggingSettings">The logging settings.</param>
        /// <param name="commerceConnectorSettings">The commerce connector settings</param>
        /// <param name="getDatabaseVersionCommand">Command to get DB version</param>
        public void Configure(
            IApplicationBuilder app,
            IConfigureServiceApiPipeline configureServiceApiPipeline,
            IStartNodePipeline startNodePipeline,
            IConfigureOpsServiceApiPipeline configureOpsServiceApiPipeline,
            IStartEnvironmentPipeline startEnvironmentPipeline,
            IOptions<LoggingSettings> loggingSettings,
            IOptions<CommerceConnectorSettings> commerceConnectorSettings,
            GetDatabaseVersionCommand getDatabaseVersionCommand)
        {
            ValidateDatabaseVersion(getDatabaseVersionCommand);

            _nodeContext.PipelineTraceLoggingEnabled = loggingSettings.Value.PipelineTraceLoggingEnabled;

            if (Configuration.GetSection("Compression:Enabled").Get<bool>())
            {
                app.UseResponseCompression();
            }

            if (_hostEnv.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePages();
            }

            app.UseDiagnostics();
            app.UseRouting();
            var allowedOrigins = Configuration.GetSection("AppSettings:AllowedOrigins").Value.ToLowerInvariant().Split('|');
            app.UseCors(builder =>
                builder.WithOrigins(allowedOrigins)
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCommerceRoleValidationMiddleware(commerceConnectorSettings);

            Task.Run(() => startNodePipeline.RunAsync(_nodeContext, _nodeContext.PipelineContextOptions)).Wait();

            string environmentName = Configuration.GetSection("AppSettings:EnvironmentName").Value;
            if (!string.IsNullOrEmpty(environmentName))
            {
                _nodeContext.AddDataMessage("EnvironmentStartup", $"StartEnvironment={environmentName}");
                Task.Run(() => startEnvironmentPipeline.RunAsync(environmentName, _nodeContext.PipelineContextOptions)).Wait();
            }

            // Run the pipeline to configure the plugins OData context
            var contextResult = Task.Run(() => configureServiceApiPipeline.RunAsync(new ODataConventionModelBuilder(), _nodeContext.PipelineContextOptions)).Result;
            contextResult.Namespace = "Sitecore.Commerce.Engine";
            // Get the model and register the ODataRoute
            var apiModel = contextResult.GetEdmModel();

            // Register the bootstrap context for the engine
            var contextOpsResult = Task.Run(() => configureOpsServiceApiPipeline.RunAsync(new ODataConventionModelBuilder(), _nodeContext.PipelineContextOptions)).Result;
            contextOpsResult.Namespace = "Sitecore.Commerce.Engine";
            // Get the model and register the ODataRoute
            var opsModel = contextOpsResult.GetEdmModel();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.EnableDependencyInjection();
                endpoints.Expand().Select().OrderBy().Filter().Count();
                endpoints.MapODataRoute(CoreConstants.CommerceApi, CoreConstants.CommerceApi, apiModel);
                endpoints.MapODataRoute(CoreConstants.CommerceOpsApi, CoreConstants.CommerceOpsApi, opsModel);

                if (Configuration.GetSection("HealthCheck:Enabled").Get<bool>())
                {
                    endpoints.MapHealthChecks(Configuration.GetSection("HealthCheck:URL").Value);
                }

                endpoints.MapFallback(context =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                });
            });
        }

        private static void AddTransientFaultHandling(IServiceCollection services, Microsoft.Extensions.Logging.ILogger logger)
        {
            services.AddTransientFaultHandling(config => config
                .AddTransientFaultHandlingForBraintree(
                    new ExponentialBackoff(
                        "BraintreeExponentialRetryPolicy",
                        10,
                        TimeSpan.FromMilliseconds(50),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromMilliseconds(100),
                        false))

                .AddCommerceSqlRetryer(SqlConstants.StartupSqlRetryerName, logger, new SqlPolicyRetryerOptions
                {
                    ExponentialRetry = new SqlExponentialRetryOptions
                    {
                        MaxAttempts = 20,
                        MinBackoff = TimeSpan.FromSeconds(30),
                        MaxBackoff = TimeSpan.FromMinutes(5),
                        DeltaBackoff = TimeSpan.FromSeconds(30)
                    },
                    CustomErrorCodes = new List<int>
                    {
                        2 // Could not open a connection to SQL Server
                    }
                })

                .AddCommerceRedisRetryer(CoreConstants.StartupRedisRetryerName, logger, new RedisPolicyRetryerOptions
                {
                    ExponentialRetry = new RedisExponentialRetryOptions
                    {
                        MaxAttempts = 20,
                        MinBackoff = TimeSpan.FromSeconds(30),
                        MaxBackoff = TimeSpan.FromMinutes(5),
                        DeltaBackoff = TimeSpan.FromSeconds(30)
                    }
                })
            );
        }

        private void AddCaching(IServiceCollection services)
        {
            var cachingSettings = new CachingSettings();
            Configuration.GetSection("Caching").Bind(cachingSettings);
            var memoryCacheSettings = cachingSettings.Memory;
            var redisCacheSettings = cachingSettings.Redis;
            if (memoryCacheSettings.Enabled && redisCacheSettings.Enabled)
            {
                Log.Error("Only one cache provider can be enable at the same time, please choose Memory or Redis.");
                return;
            }

            if (!memoryCacheSettings.Enabled && !redisCacheSettings.Enabled)
            {
                Log.Warning("There is not cache provider configured, a default memory cache will be use.");
                return;
            }

            services.AddSingleton(cachingSettings);

            services.Sitecore()
                .Caching(
                    config =>
                    {
                        if (memoryCacheSettings.Enabled)
                        {
                            config
                                .AddMemoryStore(memoryCacheSettings.CacheStoreName, options => options = memoryCacheSettings.Options)
                                .ConfigureCaches(WildcardMatch.All(), memoryCacheSettings.CacheStoreName);

                            services.Replace(
                                new ServiceDescriptor(
                                    typeof(ICacheStore),
                                    sp => ActivatorUtilities.CreateInstance<CommerceMemoryCacheStore>(sp, memoryCacheSettings.CacheStoreName),
                                    ServiceLifetime.Singleton));
                        }

                        if (redisCacheSettings.Enabled)
                        {
                            _nodeContext.IsRedisCachingEnabled = true;
                            config
                                .AddRedisStore(redisCacheSettings.CacheStoreName, redisCacheSettings.Options.Configuration, redisCacheSettings.Options.InstanceName)
                                .ConfigureCaches(WildcardMatch.All(), redisCacheSettings.CacheStoreName);

                            services.Replace(
                                new ServiceDescriptor(
                                    typeof(ICacheStore),
                                    sp => ActivatorUtilities.CreateInstance<CommerceRedisCacheStore>(sp, redisCacheSettings.CacheStoreName, sp.GetService<CachingSettings>()),
                                    ServiceLifetime.Singleton));
                        }

                        services.Replace(ServiceDescriptor.Transient<IConfigureMatchingOptions<CacheOptions>, CommerceCacheOptionsSetup>());
                    });
        }

        private void AddCompression(IServiceCollection services)
        {
            if (!Configuration.GetSection("Compression:Enabled").Get<bool>())
            {
                return;
            }

            var responseCompressionOptions = Configuration.GetSection("Compression:ResponseCompressionOptions")
                .Get<ResponseCompressionOptions>();

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = responseCompressionOptions?.EnableForHttps ?? true;
                options.MimeTypes = responseCompressionOptions?.MimeTypes ?? ResponseCompressionDefaults.MimeTypes;
                options.Providers.Add<GzipCompressionProvider>();
            });

            var gzipCompressionProviderOptions = Configuration.GetSection("Compression:GzipCompressionProviderOptions")
                .Get<GzipCompressionProviderOptions>();

            services.Configure<GzipCompressionProviderOptions>(options => { options.Level = gzipCompressionProviderOptions?.Level ?? CompressionLevel.Fastest; });
        }

        private void AddAntiforgery(IServiceCollection services)
        {
            string antiForgeryEnabledSetting = Configuration.GetSection("AppSettings:AntiForgeryEnabled").Value;
            _nodeContext.AntiForgeryEnabled = !string.IsNullOrWhiteSpace(antiForgeryEnabledSetting) && Convert.ToBoolean(antiForgeryEnabledSetting, CultureInfo.InvariantCulture);
            _nodeContext.CommerceServicesHostPostfix = Configuration.GetSection("AppSettings:CommerceServicesHostPostfix").Value;
            if (string.IsNullOrEmpty(_nodeContext.CommerceServicesHostPostfix))
            {
                if (_nodeContext.AntiForgeryEnabled)
                {
                    services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
                }
            }
            else
            {
                if (_nodeContext.AntiForgeryEnabled)
                {
                    services.AddAntiforgery(
                        options =>
                        {
                            options.HeaderName = "X-XSRF-TOKEN";
                            options.Cookie.SameSite = SameSiteMode.Lax;
                            options.Cookie.Domain = string.Concat(".", _nodeContext.CommerceServicesHostPostfix);
                            options.Cookie.HttpOnly = false;
                        });
                }
            }
        }

        private static CommerceEnvironment LoadGlobalEnvironmentFile(CommerceContext context, string path, string id)
        {
            var commander = new CommerceCommander();
            string filename = $"{path}{id}.json";
            if (!File.Exists(filename))
            {
                return commander.Deserialize<CommerceEnvironment>(context, string.Empty);
            }

            string jsonFromFile = commander.ReplaceEnvironmentPlaceHolders(File.ReadAllText($"{path}{id}.json"));
            var inflatedEntity = commander.Deserialize<CommerceEnvironment>(context, jsonFromFile);

            return inflatedEntity;
        }

        private LogEventLevel GetSerilogLogLevel()
        {
            var level = LogEventLevel.Verbose;
            string configuredLevel = Configuration.GetSection("Serilog:MinimumLevel:Default").Value;
            if (string.IsNullOrEmpty(configuredLevel))
            {
                return level;
            }

            if (configuredLevel.Equals(LogEventLevel.Debug.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Debug;
            }
            else if (configuredLevel.Equals(LogEventLevel.Information.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Information;
            }
            else if (configuredLevel.Equals(LogEventLevel.Warning.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Warning;
            }
            else if (configuredLevel.Equals(LogEventLevel.Error.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Error;
            }
            else if (configuredLevel.Equals(LogEventLevel.Fatal.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                level = LogEventLevel.Fatal;
            }

            return level;
        }

        private void SetupDataProtection(IServiceCollection services, Microsoft.Extensions.Logging.ILogger logger)
        {
            var builder = services.AddDataProtection();
            string pathToKeyStorage = Configuration.GetSection("AppSettings:EncryptionKeyStorageLocation").Value;

            string protectionType = Configuration.GetSection("AppSettings:EncryptionProtectionType").Value.ToUpperInvariant();

            switch (protectionType)
            {
                case "DPAPI-SID":
                    string storageSid = Configuration.GetSection("AppSettings:EncryptionSID").Value.ToUpperInvariant();
                    //// Uses the descriptor rule "SID=S-1-5-21-..." to encrypt with domain joined user
                    builder.PersistKeysToFileSystem(new DirectoryInfo(pathToKeyStorage));
                    builder.ProtectKeysWithDpapiNG($"SID={storageSid}", flags: DpapiNGProtectionDescriptorFlags.None);
                    break;
                case "DPAPI-CERT":
                    string storageCertificateHash = Configuration.GetSection("AppSettings:EncryptionCertificateHash").Value.ToUpperInvariant();
                    //// Searches the cert store for the cert with this thumbprint
                    builder.PersistKeysToFileSystem(new DirectoryInfo(pathToKeyStorage));
                    builder.ProtectKeysWithDpapiNG(
                        $"CERTIFICATE=HashId:{storageCertificateHash}",
                        DpapiNGProtectionDescriptorFlags.None);
                    break;
                case "LOCAL":
                    //// Only the local user account can decrypt the keys
                    builder.PersistKeysToFileSystem(new DirectoryInfo(pathToKeyStorage));
                    builder.ProtectKeysWithDpapiNG();
                    break;
                case "MACHINE":
                    //// All user accounts on the machine can decrypt the keys
                    builder.PersistKeysToFileSystem(new DirectoryInfo(pathToKeyStorage));
                    builder.ProtectKeysWithDpapi(true);
                    break;
                case "REDIS":
                    //// Cache keys are stored in the Redis cache for distributed applications
                    var cachingSettings = new CachingSettings();
                    Configuration.GetSection("Caching").Bind(cachingSettings);
                    var redisCacheSettings = cachingSettings.Redis;
                    if (!redisCacheSettings.Enabled)
                    {
                        Log.Error("The 'Redis' EncryptionProtectionType cannot be used unless Redis caching is enabled.");
                        break;
                    }

                    var redisFactory = new CommerceRedisPolicyRetryerFactory(
                        new RedisPolicyRetryerOptions
                        {
                            ExponentialRetry = new RedisExponentialRetryOptions
                            {
                                MaxAttempts = 20,
                                MinBackoff = TimeSpan.FromSeconds(30),
                                MaxBackoff = TimeSpan.FromMinutes(5),
                                DeltaBackoff = TimeSpan.FromSeconds(30)
                            }
                        },
                        logger);

                    var retryer = redisFactory.Create();
                    var connectTask = RedisRetryHelper.ConnectAsync(redisCacheSettings.Options.Configuration, retryer);
                    var redisConnection = connectTask.GetAwaiter().GetResult();
                    services.AddDataProtection().PersistKeysToStackExchangeRedis(redisConnection, "DataProtection-Keys");
                    break;
                default:
                    //// All user accounts on the machine can decrypt the keys
                    builder.ProtectKeysWithDpapi(true);
                    break;
            }
        }

        private CommerceEnvironment GetGlobalEnvironment()
        {
            CommerceEnvironment environment;
            string bootstrapProviderFolderPath = string.Concat(Path.Combine(_hostEnv.WebRootPath, "Bootstrap"), Path.DirectorySeparatorChar);

            Log.Information($"Loading Global Environment from: {bootstrapProviderFolderPath}");

            // Use the default File System provider to setup the environment
            string bootstrapFile = Configuration.GetSection("AppSettings:BootStrapFile").Value;

            if (!string.IsNullOrEmpty(bootstrapFile))
            {
                _nodeContext.BootstrapEnvironmentPath = bootstrapFile;

                _nodeContext.AddDataMessage("NodeStartup", $"GlobalEnvironmentFrom='Configuration: {bootstrapFile}'");
                environment = LoadGlobalEnvironmentFile(_nodeContext, bootstrapProviderFolderPath, bootstrapFile);
            }
            else
            {
                // Load the _nodeContext default
                bootstrapFile = "Global";
                _nodeContext.BootstrapEnvironmentPath = bootstrapFile;
                _nodeContext.AddDataMessage("NodeStartup", $"GlobalEnvironmentFrom='{bootstrapFile}.json'");
                environment = LoadGlobalEnvironmentFile(_nodeContext, bootstrapProviderFolderPath, bootstrapFile);
            }

            _nodeContext.GlobalEnvironmentName = environment.Name;
            _nodeContext.AddDataMessage("NodeStartup", $"Status='Started',GlobalEnvironmentName='{_nodeContext.GlobalEnvironmentName}'");

            if (Configuration.GetSection("AppSettings:BootStrapFile").Value != null)
            {
                _nodeContext.ContactId = Configuration.GetSection("AppSettings:NodeId").Value;
            }

            if (!string.IsNullOrEmpty(environment.GetPolicy<DeploymentPolicy>().DeploymentId))
            {
                _nodeContext.ContactId = $"{environment.GetPolicy<DeploymentPolicy>().DeploymentId}_{_nodeInstanceId}";
            }

            return environment;
        }

        private void ValidateDatabaseVersion(GetDatabaseVersionCommand getDatabaseVersionCommand)
        {
            // Get the core required database version from config policy
            var coreRequiredDbVersion = string.Empty;
            if (StartupEnvironment.HasPolicy<EntityStoreSqlPolicy>())
            {
                coreRequiredDbVersion = StartupEnvironment.GetPolicy<EntityStoreSqlPolicy>().Version;
            }

            // Get the db version
            string dbVersion = Task.Run(() => getDatabaseVersionCommand.Process(_nodeContext)).Result;

            // Check versions
            if (string.IsNullOrEmpty(dbVersion) || string.IsNullOrEmpty(coreRequiredDbVersion) || !string.Equals(coreRequiredDbVersion, dbVersion, StringComparison.Ordinal))
            {
                throw new CommerceException($"Core required DB Version [{coreRequiredDbVersion}] and DB Version [{dbVersion}]");
            }

            Log.Information($"Core required DB Version [{coreRequiredDbVersion}] and DB Version [{dbVersion}]");
        }
    }
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
}
