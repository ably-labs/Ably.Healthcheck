# Ably.Heathcheck

* NuGet Status [![NuGet](https://buildstats.info/nuget/Ably.Healthcheck?includePreReleases=true)](https://www.nuget.org/packages/Ably.Healthcheck)

Library with Healtchecks to check a health of [Ably](https://ably.com/) services.
Two checks are available:

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
            "AblyChannel",
            AblyChannelHealthCheck(
                ably,
                "Name"
            )
        )
        .AddCheck(
            "AblyTimer",
            AblyTimerHealthCheck(
                ably,
                "Topic",
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
