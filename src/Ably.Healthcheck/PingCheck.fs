namespace Ably.HealthCheck

open System
open IO.Ably
open Microsoft.Extensions.Diagnostics.HealthChecks

type AblyPingHealthCheck (ably: AblyRealtime, ?acceptableDiff: TimeSpan) =
    let acceptableTimeDiff =
        match acceptableDiff with
        | Some a -> a
        | None -> TimeSpan.FromSeconds 1.
        
    interface IHealthCheck with
        member __.CheckHealthAsync (context, ct) =
            async {
                if ct.IsCancellationRequested then
                    return HealthCheckResult(context.Registration.FailureStatus, $"Cancellation requested")
                else
                    let! res = ably.Connection.PingAsync () |> Async.AwaitTask
                    if res.IsSuccess then
                        if res.Value.HasValue then
                            let toCheck = res.Value.Value <= acceptableTimeDiff
                            if toCheck then
                                return HealthCheckResult.Healthy("Ably is healthy")
                            else
                               return HealthCheckResult(context.Registration.FailureStatus, $"Ably failed to ping connection in acceptable time. Ping received after: {res.Value.Value}, acceptable time: {acceptableTimeDiff}")
                        else
                          return HealthCheckResult(context.Registration.FailureStatus, "Ably Connection failed to receive ping.")  
                    else
                        return HealthCheckResult(context.Registration.FailureStatus, $"Ably Connection failed to perform ping. Error info: {res.Error.ToString()}")
            } |> Async.StartAsTask
