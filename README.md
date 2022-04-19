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

## Required capabilities and restrictions

When configuring Ably Healthcheck's we need to provide a valid `ApiKey`. Beyond of that some additional [Capabilities and Restrictions](https://ably.com/docs/core-features/authentication#capability-operations) could be required to make the `HealthCheck's` work properly.

- `Channel Healthcheck`
  - requires `Publish` Capability, restricted at most to concrete channel (part of configuration),
    
- `Timer Healthcheck`
  - requires `Publish` and `Subscribe` Capabilities, restricted at most to a concrete channel (part of configuration).

## Contributing

Do you want to contribute to this project? Have a look at our [contributing guide](./CONTRIBUTING.md).

## Issues

Did you find a bug? Do you want to suggest a feature? Please file an issue [here](https://github.com/ably-labs/Ably.Healthcheck/issues/new/choose).

## More info

- [Join our Discord server](https://discord.gg/q89gDHZcBK)
- [Follow us on Twitter](https://twitter.com/ablyrealtime)
- [Use our SDKs](https://github.com/ably/)
- [Visit our website](https://ably.com)

---
[![Ably logo](https://static.ably.dev/badge-black.svg?dotnet-healthcheck)](https://ably.com)