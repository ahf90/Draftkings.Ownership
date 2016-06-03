using System;
using Hangfire.SqlServer;
using Microsoft.Owin;
using Owin;
using Hangfire;

[assembly: OwinStartupAttribute(typeof(Draftkings.Ownership.Startup))]
namespace Draftkings.Ownership
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage(
                "HangfireDb",
                new SqlServerStorageOptions { QueuePollInterval = TimeSpan.FromSeconds(1) });

            var options = new BackgroundJobServerOptions
            {
                Queues = new[] { "default", "contestload", "externaldk" },
                WorkerCount = 1
            };

            app.UseHangfireServer(options);

            var InternalOptions = new BackgroundJobServerOptions
            {
                WorkerCount = 20,
                Queues = new[] { "default", "playercreate" }
            };

            app.UseHangfireServer(InternalOptions);

            var ExternalOptions = new BackgroundJobServerOptions
            {
                WorkerCount = 1,
                Queues = new[] { "default", "entryids", "ownership" }
            };

            app.UseHangfireServer(ExternalOptions);


            app.UseHangfireDashboard();
        }
    }
}
