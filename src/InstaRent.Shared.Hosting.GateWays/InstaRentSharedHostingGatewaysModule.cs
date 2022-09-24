using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Polly;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.Modularity;
using Volo.Abp.Swashbuckle;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;

namespace InstaRent.Shared.Hosting.Gateways
{
    [DependsOn(
        typeof(AbpAspNetCoreSerilogModule),
        typeof(AbpSwashbuckleModule),
        typeof(AbpAspNetCoreMvcUiMultiTenancyModule) ,
        typeof(AbpAutofacModule)
    )]
    public class InstaRentSharedHostingGatewaysModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var env = context.Services.GetHostingEnvironment();

            var ocelotBuilder = context.Services.AddOcelot(configuration)
                .AddPolly();

            if (!env.IsProduction())
            {
                ocelotBuilder.AddDelegatingHandler<AbpRemoveCsrfCookieHandler>(true);
            }
        }
    }
}