# Ably.Heathcheck

* NuGet Status [![NuGet](https://buildstats.info/nuget/Ably.Healthcheck?includePreReleases=true)](https://www.nuget.org/packages/Ably.Healthcheck)

Library with Healtchecks to check a health of [Ably](https://ably.com/) services.
Three checks are available:

- Ping check,
- Channel check,
- Timer check.

# How to add healthchecks

```fsharp
...
member this.ConfigureServices(services: IServiceCollection) =
    ...
    let ably = new AblyRealtime ("apiKey")
    ...
    services.AddHealthChecks()
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
                "ServiceName",
                "ChannelName",
                TimeSpan.FromSeconds1.,
                TimeSpan.FromSeconds1.
            )
        )
    |> ignore
    ...
...
```

To get Healthchecks UI

```fsharp
member this.ConfigureServices(services: IServiceCollection) =
    ...
    services
        .AddHealthChecksUI(fun s ->
            s
                .SetEvaluationTimeInSeconds(60)
                .AddHealthCheckEndpoint("Self", $"http://{Dns.GetHostName()}/health")
            |> ignore)
        .AddInMemoryStorage() |> ignore
    ...

member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
    ...
    ...
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
    ...
```
