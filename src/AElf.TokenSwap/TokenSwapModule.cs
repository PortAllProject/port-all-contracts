using AElf.Contracts.Bridge;
using AElf.Database;
using AElf.TokenSwap.Infrastructure;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Volo.Abp.AspNetCore;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Caching;
using Volo.Abp.Http.Modeling;
using Volo.Abp.Modularity;

namespace AElf.TokenSwap
{
    [DependsOn(typeof(AbpAspNetCoreModule),
        typeof(DatabaseAElfModule))]
    public class TokenSwapModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            var configuration = services.GetConfiguration();
            services.AddSingleton<IDistributedCacheSerializer, Utf8JsonDistributedCacheSerializer>();
            services.AddSingleton<IDistributedCacheKeyNormalizer, DistributedCacheKeyNormalizer>();
            services.AddSingleton(typeof(IDistributedCache<>), typeof(DistributedCache<>));
            services.AddTransient<IApiDescriptionModelProvider, AspNetCoreApiDescriptionModelProvider>();
            services.AddControllers();
            services.AddApiVersioning(options =>
            {
                options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new MediaTypeApiVersionReader();
                options.UseApiBehavior = false;
            });
            services.AddVersionedApiExplorer();
            services.AddDistributedMemoryCache();

            services.AddSwaggerGen(
                options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Rural Assets Platform API", Version = "v1" });
                    options.OperationFilter<SwaggerFileUploadFilter>();
                    options.DocInclusionPredicate((docName, description) => true);
                    options.CustomSchemaIds(type => type.FullName);
                }
            );

            services.AddTransient(typeof(IStoreKeyPrefixProvider<>), typeof(StoreKeyPrefixProvider<>));
            services.AddStoreKeyPrefixProvide<ReceiptInfo>("ri");
            services.AddTransient(typeof(ITokenSwapStore<>), typeof(TokenSwapStore<>));

            if (configuration.GetConnectionString("Default") == "InMemory")
            {
                services.AddKeyValueDbContext<TokenSwapKeyValueDbContext>(p => p.UseInMemoryDatabase());
            }
            else
            {
                services.AddKeyValueDbContext<TokenSwapKeyValueDbContext>(p => p.UseSsdbDatabase());
            }

            services.Configure<ConfigOptions>(configuration.GetSection("Config"));
        }
    }

    public static class StoreKeyPrefixProviderServiceCollectionExtensions
    {
        public static IServiceCollection AddStoreKeyPrefixProvide<T>(
            this IServiceCollection serviceCollection, string prefix)
            where T : IMessage<T>, new()
        {
            serviceCollection.AddTransient<IStoreKeyPrefixProvider<T>>(c =>
                new FastStoreKeyPrefixProvider<T>(prefix));

            return serviceCollection;
        }
    }
}