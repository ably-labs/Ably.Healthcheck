namespace Ably.HealthCheck

open System
open IO.Ably
open IO.Ably.Realtime
open Microsoft.Extensions.Diagnostics.HealthChecks

type AblyChannelHealthCheck (ably: AblyRealtime, serviceName: string) =
    interface IHealthCheck with
        member __.CheckHealthAsync (context, ct) =
            async {
                if ct.IsCancellationRequested then
                    return HealthCheckResult(context.Registration.FailureStatus, $"Cancellation requested")
                else
                    let channel = ably.Channels.Get "healthcheck"
                    match channel.State with
                    | ChannelState.Attached | ChannelState.Initialized ->
                        let! msg =
                            channel.PublishAsync ($"channel-{serviceName}", $"{DateTimeOffset.UtcNow} - {serviceName}")
                            |> Async.AwaitTask
                        if msg.IsFailure then
                            return HealthCheckResult(context.Registration.FailureStatus, $"Ably failed to push message. Error code: {msg.Error.Code}, Status: {msg.Error.StatusCode} Message: {msg.Error.Message}")
                        else
                            return HealthCheckResult.Healthy("Ably is healthy")
                    | state ->
                        return HealthCheckResult(context.Registration.FailureStatus, $"Ably channel is in invalid state: {state}")
            } |> Async.StartAsTask
