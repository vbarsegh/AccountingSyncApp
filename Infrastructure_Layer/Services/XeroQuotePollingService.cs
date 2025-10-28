using Application_Layer.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
namespace Infrastructure_Layer.Services
{
    //BackgroundService is a built-in .NET base class for long-running background tasks (like timers, workers, or schedulers).
    //This means your class will automatically run in the background when your API starts — no controller call needed.
    //this is your “daemon” that constantly wakes up every few minutes to check Xero for new quotes.
    public class XeroQuotePollingService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<XeroQuotePollingService> _logger;
        private readonly IConfiguration _config;
        private TimeSpan _interval;

        public XeroQuotePollingService(
            IServiceProvider services,
            ILogger<XeroQuotePollingService> logger,
            IConfiguration config)
        {
            _services = services;
            _logger = logger;
            _config = config;

            // ✅ Read from appsettings.json
            var minutes = _config.GetValue<int>("PollingSettings:XeroQuotePollingIntervalMinutes");//Microsoft.Extensions.Configuration.Binder add this nuGet Package for GetValue Extension method.
            if (minutes <= 0)
                minutes = 5; // default fallback

            _interval = TimeSpan.FromMinutes(minutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //This is the main loop of your background service.
            //.NET automatically calls this when your app starts.
            //The stoppingToken tells your loop to stop gracefully when the app shuts down(e.g.Ctrl+C).
            _logger.LogInformation("🚀 Xero Quote Polling Service started. Interval: {Minutes} min", _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var syncManager = scope.ServiceProvider.GetRequiredService<IAccountingSyncManager>();
                    await syncManager.SyncQuotesFromXeroPeriodicallyAsync();

                    _logger.LogInformation("✅ Quote polling completed at {Time}.", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error while polling quotes from Xero");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
