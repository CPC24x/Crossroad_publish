using System;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Plugin.Crossroad.Integration.Areas.Admin.Factories;
using Nop.Plugin.Crossroad.Integration.Domains.Onix;
using Nop.Plugin.Crossroad.Integration.Domains.OpenKm;
using Nop.Plugin.Crossroad.Integration.Infrastructure.Handlers;
using Nop.Plugin.Crossroad.Integration.Services.Manufacturer;
using Nop.Plugin.Crossroad.Integration.Services.Onix;
using Nop.Plugin.Crossroad.Integration.Services.Picture;
using Nop.Plugin.Crossroad.Integration.Services.Products;
using Nop.Plugin.Crossroad.Integration.Services.SpecificationAttributes;
using Nop.Services.Configuration;

namespace Nop.Plugin.Crossroad.Integration.Infrastructure
{
    public class PluginNopStartup : INopStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new ViewLocationExpander());
            });

            RegisterFactories(services);

            RegisterHandlers(services);

            RegisterHttpClients(services);
        }
        private void RegisterHandlers(IServiceCollection services) => services.AddTransient<TokenHandler>();

        private void RegisterFactories(IServiceCollection services)
        {
            services.AddScoped<IIntegrationModelFactory, IntegrationModelFactory>();
            services.AddScoped<IProductSpecificationAttributeService, ProductSpecificationAttributeService>();
            services.AddScoped<IProductExtendedService, ProductExtendedService>();
            services.AddScoped<IPictureExtendedService, PictureExtendedService>();
            services.AddScoped<IManufacturerExtendedService, ManufacturerExtendedService>();
            services.AddScoped<IPersistenceService, PersistenceService>();
        }

        private void RegisterHttpClients(IServiceCollection services)
        {
            var serviceProvided = services.BuildServiceProvider();

            var settingsService = serviceProvided.GetService<ISettingService>();

            var onixEditSettings = settingsService.LoadSetting<OnixEditSettings>();

            services.AddHttpClient<OnixLoginService>(client => client.BaseAddress = new Uri(onixEditSettings.Url));

            services.AddHttpClient<OnixEditService>(client => { client.BaseAddress = new Uri(onixEditSettings.Url); client.Timeout = TimeSpan.FromSeconds(600); })
                .AddHttpMessageHandler<TokenHandler>();

            var openKmSettings = settingsService.LoadSetting<OpenKMSettings>();

            services.AddHttpClient<OpenKMSettings>(client =>
            {
                client.BaseAddress = new Uri(openKmSettings.Url);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{openKmSettings.Username}:{openKmSettings.Password}")));
            });
        }

        public void Configure(IApplicationBuilder application) { }

        public int Order => 3500;
    }
}