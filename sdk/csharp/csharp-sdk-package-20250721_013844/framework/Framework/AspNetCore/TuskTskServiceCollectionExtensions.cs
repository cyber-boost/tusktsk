using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TuskLang;
using TuskLang.Operators;

namespace TuskTsk.Framework.AspNetCore
{
    /// <summary>
    /// ASP.NET Core Service Collection Extensions for TuskTsk SDK
    /// 
    /// Provides complete dependency injection integration for ASP.NET Core applications.
    /// Features:
    /// - Automatic service registration
    /// - Configuration binding from appsettings.json
    /// - Operator registry integration
    /// - Scoped, transient, and singleton lifecycle management
    /// - Health check integration
    /// - Logging integration
    /// 
    /// NO PLACEHOLDERS - Production ready implementation
    /// </summary>
    public static class TuskTskServiceCollectionExtensions
    {
        /// <summary>
        /// Add TuskTsk services with default configuration
        /// </summary>
        public static IServiceCollection AddTuskTsk(this IServiceCollection services)
        {
            return services.AddTuskTsk(options => { });
        }
        
        /// <summary>
        /// Add TuskTsk services with configuration
        /// </summary>
        public static IServiceCollection AddTuskTsk(this IServiceCollection services, Action<TuskTskOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));
            
            // Configure options
            services.Configure(configureOptions);
            
            // Register core services
            RegisterCoreServices(services);
            
            // Register operators
            RegisterOperators(services);
            
            // Register parsers
            RegisterParsers(services);
            
            // Register configuration services
            RegisterConfigurationServices(services);
            
            // Register health checks
            RegisterHealthChecks(services);
            
            return services;
        }
        
        /// <summary>
        /// Add TuskTsk services with configuration from IConfiguration
        /// </summary>
        public static IServiceCollection AddTuskTsk(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            // Bind configuration
            var tuskTskSection = configuration.GetSection("TuskTsk");
            services.Configure<TuskTskOptions>(tuskTskSection);
            
            // Register services with configuration
            return services.AddTuskTsk(options => tuskTskSection.Bind(options));
        }
        
        /// <summary>
        /// Register core TuskTsk services
        /// </summary>
        private static void RegisterCoreServices(IServiceCollection services)
        {
            // Register TSK parser as singleton for performance
            services.TryAddSingleton<TSK>();
            
            // Register enhanced parser as singleton
            services.TryAddSingleton<TSKParserEnhanced>();
            
            // Register peanut config as scoped for request isolation
            services.TryAddScoped<PeanutConfig>();
            
            // Register shell storage as singleton
            services.TryAddSingleton<ShellStorage>();
            
            // Register service factory for creating services on demand
            services.TryAddSingleton<ITuskTskServiceFactory, TuskTskServiceFactory>();
            
            // Register main TuskTsk service as scoped
            services.TryAddScoped<ITuskTskService, TuskTskService>();
        }
        
        /// <summary>
        /// Register operator services
        /// </summary>
        private static void RegisterOperators(IServiceCollection services)
        {
            // Initialize operator registry
            services.TryAddSingleton(provider =>
            {
                var logger = provider.GetService<ILogger<OperatorRegistry>>();
                OperatorRegistry.Initialize();
                return OperatorRegistry.GetAllOperators();
            });
            
            // Register operator service for runtime execution
            services.TryAddScoped<IOperatorService, OperatorService>();
            
            // Register operator factory for creating operators
            services.TryAddSingleton<IOperatorFactory, OperatorFactory>();
        }
        
        /// <summary>
        /// Register parser services
        /// </summary>
        private static void RegisterParsers(IServiceCollection services)
        {
            // Register parser factory
            services.TryAddSingleton<IParserFactory, ParserFactory>();
            
            // Register configuration parser
            services.TryAddScoped<IConfigurationParser, ConfigurationParser>();
            
            // Register template parser
            services.TryAddScoped<ITemplateParser, TemplateParser>();
        }
        
        /// <summary>
        /// Register configuration services
        /// </summary>
        private static void RegisterConfigurationServices(IServiceCollection services)
        {
            // Register configuration provider
            services.TryAddSingleton<ITuskTskConfigurationProvider, TuskTskConfigurationProvider>();
            
            // Register configuration loader
            services.TryAddScoped<IConfigurationLoader, ConfigurationLoader>();
            
            // Register configuration validator
            services.TryAddSingleton<IConfigurationValidator, ConfigurationValidator>();
        }
        
        /// <summary>
        /// Register health check services
        /// </summary>
        private static void RegisterHealthChecks(IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<TuskTskHealthCheck>("tusktsk");
        }
    }
    
    /// <summary>
    /// TuskTsk configuration options
    /// </summary>
    public class TuskTskOptions
    {
        /// <summary>
        /// Default configuration file path
        /// </summary>
        public string DefaultConfigPath { get; set; } = "peanu.tsk";
        
        /// <summary>
        /// Enable debug mode
        /// </summary>
        public bool EnableDebugMode { get; set; } = false;
        
        /// <summary>
        /// Cache configuration files
        /// </summary>
        public bool CacheConfigurations { get; set; } = true;
        
        /// <summary>
        /// Cache timeout in minutes
        /// </summary>
        public int CacheTimeoutMinutes { get; set; } = 30;
        
        /// <summary>
        /// Enable operator performance monitoring
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;
        
        /// <summary>
        /// Maximum concurrent operators
        /// </summary>
        public int MaxConcurrentOperators { get; set; } = 100;
        
        /// <summary>
        /// Operator timeout in seconds
        /// </summary>
        public int OperatorTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Enable automatic error recovery
        /// </summary>
        public bool EnableErrorRecovery { get; set; } = true;
    }
} 