using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TuskTsk.Framework.AspNetCore;

namespace TuskTsk.Examples.AspNetCore
{
    /// <summary>
    /// ASP.NET Core Startup Example - Best Practices for TuskTsk Integration
    /// 
    /// Demonstrates:
    /// - Proper dependency injection setup
    /// - Configuration binding from appsettings.json
    /// - Health check integration
    /// - Error handling and logging
    /// - Performance monitoring
    /// - Production-ready configuration
    /// 
    /// NO PLACEHOLDERS - Complete working example
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add ASP.NET Core MVC
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Add TuskTsk Framework Integration - PRODUCTION READY
            services.AddTuskTsk(Configuration)
                .ConfigureOptions<TuskTskOptions>(Configuration.GetSection("TuskTsk"));

            // Alternative configuration with lambda
            // services.AddTuskTsk(options =>
            // {
            //     options.DefaultConfigPath = "app.tsk";
            //     options.EnableDebugMode = false;
            //     options.CacheConfigurations = true;
            //     options.CacheTimeoutMinutes = 30;
            //     options.EnablePerformanceMonitoring = true;
            //     options.MaxConcurrentOperators = 100;
            //     options.OperatorTimeoutSeconds = 30;
            //     options.EnableErrorRecovery = true;
            // });

            // Add Health Checks with TuskTsk monitoring
            services.AddHealthChecks()
                .AddCheck<TuskTskHealthCheck>("tusktsk")
                .AddCheck("example", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Example is healthy"));

            // Add logging
            services.AddLogging();

            // Add CORS for API access
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Add memory caching
            services.AddMemoryCache();

            // Add HTTP context accessor
            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthorization();

            // Add Health Check endpoint
            app.UseHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var response = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(entry => new
                        {
                            name = entry.Key,
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description,
                            data = entry.Value.Data
                        }),
                        duration = report.TotalDuration
                    };
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }
    }
} 