using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using EPC.ReferenceIntegration.Helpers;

namespace EPC.ReferenceIntegration
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appSettings = Configuration.GetSection("AppSettings");

            services.Configure<AppSettings>(appSettings);

            services.AddCors(o => o.AddPolicy("EnableCORSPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            // Add framework services.
            services.AddMvc();

            services.AddScoped<IHttpContextAccessor, HttpContextAccessor>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddFile("Logs/myapp-{Date}.txt");
            loggerFactory.AddDebug();

            //call ConfigureLogger in a centralized place in the code
            LogHelper.ConfigureLogger(loggerFactory);
            //set it as the primary LoggerFactory to use everywhere
            LogHelper.LoggerFactory = loggerFactory;

            app.UseMiddleware<RequestLoggingMiddleware>();

            // this has been added temporarily
            app.UseCors("EnableCORSPolicy");

            app.UseMvc();
        }
    }
}
