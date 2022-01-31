open System
open System.Threading.Tasks
open Ably.HealthCheck
open HealthChecks.UI.Client
open IO.Ably
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Diagnostics.HealthChecks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Diagnostics.HealthChecks
open Microsoft.Extensions.Hosting
open Ably.HealthCheck

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let ably = new AblyRealtime ("apikey")
    builder.Services.AddControllers() |> ignore
    builder.Services.AddHealthChecks()
        .AddCheck(
            "AblyPing",
            AblyPingHealthCheck(
                ably,
                TimeSpan.FromSeconds 1.
            )
        )        
        .AddCheck(
            "AblyChannel",
            AblyChannelHealthCheck(
                ably,
                "ServiceName",
                "ChannelName"
            )
        )
        .AddCheck(
            "AblyTimer",
            AblyTimerHealthCheck(
                ably,
                "serviceName",
                "ChannelName",
                TimeSpan.FromSeconds 1.,
                TimeSpan.FromSeconds 1.
            )
        )
    |> ignore
    
    
    let app = builder.Build()
    app.UseRouting() |> ignore
    app.UseEndpoints(fun endpoints ->
            endpoints.MapControllers() |> ignore
            endpoints.MapHealthChecksUI(fun setup ->
                setup.UIPath <- "/ui-health"
                setup.ApiPath <- "/api-ui-health"
            ) |> ignore
            endpoints.MapHealthChecks(
                "/health",
                HealthCheckOptions(
                    Predicate = (fun _ -> true),
                    ResponseWriter = Func<HttpContext, HealthReport, Task>(fun (context) (c: HealthReport) -> UIResponseWriter.WriteHealthCheckUIResponse(context, c))
                )
            ) |> ignore
        ) |> ignore
    
    app.MapGet("/", Func<string>(fun () -> "Hello World!")) |> ignore
    
    app.Run()
    
    0 // Exit code

