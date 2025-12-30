using System.Net;
using System.Diagnostics;
using EggLink.DanhengServer.Util;
using Microsoft.AspNetCore;

namespace EggLink.DanhengServer.WebServer;

public class WebProgram
{
    public static void Main(string[] args, int port, string address)
    {
        BuildWebHost(args, port, address).Start();
    }

    public static IWebHost BuildWebHost(string[] args, int port, string address)
    {
        var b = WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureLogging((hostingContext, logging) => { logging.ClearProviders(); })
            .UseUrls(address);

        if (ConfigManager.Config.HttpServer.UseSSL)
            b.UseKestrel(options =>
            {
                options.Listen(IPAddress.Any, port, listenOptions =>
                {
                    listenOptions.UseHttps(
                        ConfigManager.Config.KeyStore.KeyStorePath,
                        ConfigManager.Config.KeyStore.KeyStorePassword
                    );
                });
            });

        return b.Build();
    }
}

public class Startup
{
    private static readonly HashSet<string> TracePaths =
    [
        "/query_dispatch",
        "/query_gateway",
        "/hkrpg_global/mdk/shield/api/login",
        "/hkrpg_global/mdk/shield/api/verify",
        "/hkrpg_global/account/ma-passport/api/appLoginByPassword",
        "/hkrpg_global/combo/granter/login/v2/login"
    ];

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
        });

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        var httpTraceLogger = new Logger("HttpTrace");

        app.Use(async (context, next) =>
        {
            var shouldTrace = TracePaths.Contains(context.Request.Path.Value ?? "");
            var sw = shouldTrace ? Stopwatch.StartNew() : null;

            using var buffer = new MemoryStream();
            var request = context.Request;
            var response = context.Response;

            var bodyStream = response.Body;
            response.Body = buffer;

            try
            {
                await next.Invoke();
            }
            catch (Exception ex)
            {
                if (shouldTrace)
                {
                    sw?.Stop();
                    httpTraceLogger.Debug(
                        $"{request.Method} {request.Path} EX={ex.GetType().Name} {ex.Message} {(sw?.ElapsedMilliseconds ?? 0)}ms");
                }

                throw;
            }

            buffer.Position = 0;
            context.Response.Headers["Content-Length"] = (response.ContentLength ?? buffer.Length).ToString();
            context.Response.Headers.Remove("Transfer-Encoding");
            await buffer.CopyToAsync(bodyStream);

            if (shouldTrace)
            {
                sw?.Stop();
                var host = request.Headers.Host.ToString();
                var deviceId = request.Headers["x-rpc-device_id"].ToString();
                if (string.IsNullOrWhiteSpace(deviceId))
                    deviceId = request.Headers["x-rpc-device-id"].ToString();

                var version = request.Query["version"].ToString();
                if (version.Length > 64) version = version[..64] + "...";

                var status = response.StatusCode;
                var len = response.ContentLength ?? buffer.Length;
                httpTraceLogger.Debug(
                    $"{request.Method} {request.Path} status={status} len={len} t={sw?.ElapsedMilliseconds ?? 0}ms host={host} device={deviceId} version={version}");
            }
        });

        if (ConfigManager.Config.HttpServer.UseSSL)
            app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCors("AllowAll");

        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}
