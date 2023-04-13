using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

using GameWave.Api.Services;
using System;
using System.Configuration;

namespace GameWave.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        private const string ApplicationName = "GameWave API";

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = GetConnectionString();

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithClientIp()
                .Enrich.WithClientAgent();

            loggerConfiguration.WriteTo.MSSqlServer(connectionString, new MSSqlServerSinkOptions
            {
                AutoCreateSqlTable = true,
                TableName = "Logs",
                SchemaName = "dbo"
            }, restrictedToMinimumLevel: LogEventLevel.Information);

            if (_environment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console();
            }

            Log.Logger = loggerConfiguration.CreateLogger();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString, database =>
                {
                    database.MigrationsAssembly(typeof(ApplicationDbContext).GetTypeInfo().Assembly.GetName().Name);
                    database.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            });

            services.AddScoped<IAgeRatingService, AgeRatingService>();
            services.AddScoped<IAgeRatingContentDescriptorService, AgeRatingContentDescriptorService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IGameService, GameService>();
            services.AddScoped<IGenreService, GenreService>();
            services.AddScoped<IPlatformService, PlatformService>();
            services.AddScoped<IReleaseService, ReleaseService>();

            services.AddSingleton(_environment.WebRootFileProvider);

            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            services.AddCors(options =>
            {
                options.AddPolicy("Open", policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod();
                    policy.AllowAnyOrigin();
                });
            });

            services.AddControllers();
            services.AddHealthChecks();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = ApplicationName,
                    Version = "v1"
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("Open");
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", ApplicationName);
                options.RoutePrefix = string.Empty;
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSerilogRequestLogging();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/status");
                endpoints.MapControllers();
            });
        }

        private string GetConnectionString()
        {
            if (_environment.IsDevelopment())
            {
                return _configuration.GetConnectionString("Development");
            }
            else if (_environment.IsProduction() && IsRunningOnAzure())
            {
                return _configuration.GetConnectionString("Production");
            }
            else if (IsRunningOnGitHubActions())
            {
                return Environment.GetEnvironmentVariable("CONNECTION_STRING");
            }
            else
            {
                return _configuration.GetConnectionString("Default");
            }
        }

        private static bool IsRunningOnAzure()
        {
            string azureEnvironment = Environment.GetEnvironmentVariable("GAMEWAVE");

            return !string.IsNullOrEmpty(azureEnvironment);
        }

        private static bool IsRunningOnGitHubActions()
        {
            string githubAction = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");

            return !string.IsNullOrEmpty(githubAction);
        }
    }
}