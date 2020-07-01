using System;
using System.Net.Http;
using System.Threading.Tasks;
using Kubernetes.Test.API.Server.Subscriptions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;

namespace Kubernetes.Test.API.Server
{
    public sealed class TestFramework : IAsyncDisposable
    {
        private readonly IHostBuilder _hostBuilder;
        private IHost _host;

        private TestServer GetTestServer()
        {
            var testServer = _host.GetTestServer();
            testServer.PreserveExecutionContext = true;
            return testServer;
        }

        private TestFramework(
            IHostBuilder hostBuilder)
        {
            _hostBuilder = hostBuilder;
        }

        public Uri BaseAddress => GetTestServer()
            .BaseAddress;

        internal void Start()
        {
            _host = _hostBuilder.ConfigureServices(
                    collection => collection.AddSingleton(this))
                .Build();
            _host.StartAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public static TestFramework Start(
            params string[] args)
        {
            IConfiguration configuration = null;
            var hostBuilder = Program.CreateHostBuilder(args)
                .ConfigureAppConfiguration(
                    (
                        context,
                        configurationBuilder) =>
                    {
                        configuration = context.Configuration;
                    })
                .ConfigureServices(
                    collection =>
                    {
                        collection.AddLogging(
                            builder =>
                            {
                                builder.ClearProviders();
                                builder.AddNLog(configuration);
                            });
                        collection.AddControllers(
                            options =>
                                options.Filters.Add(
                                    new HttpResponseExceptionFilter()));
                    })
                .ConfigureWebHost(builder => builder.UseTestServer())
                .UseNLog();

            var testFramework = new TestFramework(hostBuilder);
            testFramework.Start();
            return testFramework;
        }

        public HttpMessageHandler CreateHttpMessageHandler()
        {
            return GetTestServer()
                .CreateHandler();
        }

        public WebSocketClient CreateWebSocketClient()
        {
            return GetTestServer()
                .CreateWebSocketClient();
        }

        public PodSubscriptions Pod { get; } =
            new PodSubscriptions();

        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}