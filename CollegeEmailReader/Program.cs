using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

internal class Program
{
    public static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddUserSecrets<Program>()
            .Build();

        IServiceCollection collection = new ServiceCollection()
            .AddSingleton<IConfiguration>(x => configuration)
            .AddTransient<ClickLinks>()
            .AddSingleton<EmailService>();

        collection.AddHttpClient<ClickLinks>().AddResilienceHandler("exponential_backoff",
            builder => builder.AddRetry<HttpResponseMessage>(
                new RetryStrategyOptions<HttpResponseMessage> { 
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(3)
                })
            );

        IServiceProvider serviceProvider = collection.BuildServiceProvider();
        await serviceProvider.GetRequiredService<EmailService>().DoDemonstratedInterestAsync();
    }
}