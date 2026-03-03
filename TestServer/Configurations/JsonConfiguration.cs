using Microsoft.AspNetCore.Http.Json;
using TestShared;

namespace TestServer.Configurations;

public static class JsonConfiguration
{
    public static IServiceCollection ConfigureJsonOptions(this IServiceCollection services) =>
        services.Configure<JsonOptions>(o => o.SerializerOptions.TypeInfoResolver = AppJsonSrcGenContext.Default);
}