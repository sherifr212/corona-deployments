using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoronaDeployments.Core;
using CoronaDeployments.Core.Build;
using CoronaDeployments.Core.Deploy;
using CoronaDeployments.Core.HostedServices;
using CoronaDeployments.Core.Repositories;
using CoronaDeployments.Core.RepositoryImporter;
using Marten;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace CoronaDeployments
{
    public class Startup
    {
        private readonly TimeSpan SessionTimeout = TimeSpan.FromHours(1);

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureLogger();

            services.AddControllersWithViews()
#if DEBUG
                .AddRazorRuntimeCompilation()
#endif
                ;
            
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = Configuration.GetConnectionString("Redis");
            });

            services.AddSession(o =>
            {
                o.IdleTimeout = SessionTimeout;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/Home/Login";
                    options.ExpireTimeSpan = SessionTimeout;
                    options.SlidingExpiration = false;
                });

            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<CoreHostedService>();

            var store = DocumentStore
                .For((c) =>
                {
                    c.Connection(Configuration.GetConnectionString("Postgres"));

                    // TODO: Create indecies https://martendb.io/documentation/documents/customizing/computed_index/
                });

            services.AddSingleton<IDocumentStore>(store);

            services.AddSingleton<IProjectRepository, ProjectRepository>();
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<ISecurityRepository, SecurityRepository>();

            services.AddSingleton<IRepositoryImportStrategy, SvnRepositoryStrategy>();
            services.AddSingleton<IRepositoryImportStrategy, GitRepositoryStrategy>();

            services.AddSingleton<ISourceCodeBuilderStrategy, DotNetCoreSourceBuilderStrategy>();

            services.AddSingleton<IDeployStrategy, InternetInformationServerDeploymentStrategy>();

            // Add AppConfiguration
            var appConfig = Configuration["AppConfiguration:BaseDirctory"];
            services.AddSingleton(new AppConfiguration(appConfig));

            // Add Credentials
            var gitUsername = Configuration["GitAuthInfo:Username"];
            var gitPassword = Configuration["GitAuthInfo:Password"];
            services.AddSingleton<IRepositoryAuthenticationInfo>(new AuthInfo(gitUsername, gitPassword, SourceCodeRepositoryType.Git));

            var svnUsername = Configuration["SvnAuthInfo:Username"];
            var svnPassword = Configuration["SvnAuthInfo:Password"];
            services.AddSingleton<IRepositoryAuthenticationInfo>(new AuthInfo(svnUsername, svnPassword, SourceCodeRepositoryType.Svn));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static void ConfigureLogger()
        {
            LogEventLevel level = LogEventLevel.Verbose;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", level)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", $"corona-deployments_log_.txt"),
                    restrictedToMinimumLevel: level,
                    rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();

            Log.Error("Corona Deployments is starting...");
        }
    }
}
