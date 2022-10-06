using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; 
using InstaRent.Shared.Hosting.Gateways;
using Ocelot.Middleware;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace InstaRent.WebGateway
{
    [DependsOn(
       typeof(InstaRentSharedHostingGatewaysModule)
   )]
    public class InstaRentWebGateWayModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostingEnvironment = context.Services.GetHostingEnvironment();

            SwaggerConfigurationHelper.Configure(
                context: context,
                apiTitle: "Web Gateway API"
            );

            context.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .WithOrigins(
                            configuration["App:CorsOrigins"]
                                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                                .Select(o => o.Trim().RemovePostFix("/"))
                                .ToArray()
                        )
                        .WithAbpExposedHeaders()
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var env = context.GetEnvironment();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCorrelationId();
            app.UseCors();
            app.UseSwagger();
            app.UseWebSockets();
            app.UseSwaggerUI(options =>
            {
                var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
                var routes = configuration.GetSection("Routes").Get<List<OcelotConfiguration>>();
                var routedServices = routes
                    .GroupBy(t => t.ServiceKey)
                    .Select(r => r.First())
                    .Distinct();

                var swaggerURLs = configuration.GetSection("SwaggersEndURLs").Get<List<SwaggerURLConfiguration>>();

                foreach (var config in routedServices.OrderBy(q => q.ServiceKey))
                {
                    var swaggerURL = swaggerURLs.Where(r => r.ServiceKey.Equals(config.ServiceKey)).FirstOrDefault();
                    var url = "";
                    if (swaggerURL != null)
                    {
                        url = $"{config.DownstreamScheme}://{swaggerURL.URL}:{config.DownstreamHostAndPorts.FirstOrDefault()?.Port}";
                        if (!env.IsDevelopment())
                        {
                            url = $"https://{swaggerURL.URL}";
                        }
                    }
                    else
                    {
                        url = $"{config.DownstreamScheme}://{config.DownstreamHostAndPorts.FirstOrDefault()?.Host}:{config.DownstreamHostAndPorts.FirstOrDefault()?.Port}";
                        if (!env.IsDevelopment())
                        {
                            url = $"https://{config.DownstreamHostAndPorts.FirstOrDefault()?.Host}";
                        }
                    }

                    

                    options.SwaggerEndpoint($"{url}/swagger/v1/swagger.json", $"{config.ServiceKey} API");
                     
                }
            });
            
            app.UseAbpSerilogEnrichers();
             
            app.UseRewriter(new RewriteOptions()
            // Regex for "", "/" and "" (whitespace)
            .AddRedirect("^(|\\|\\s+)$", "/swagger"));
            app.UseOcelot().Wait();
        }
    }
}
