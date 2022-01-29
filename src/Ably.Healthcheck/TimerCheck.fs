namespace Ably.HealthCheck

open System
open System.Collections.Concurrent
open IO.Ably
open Microsoft.Extensions.Diagnostics.HealthChecks

module internal TimerCheck =
    let doMatch (bag: ConcurrentBag<Message>) (msg: string) (expectedTime: DateTimeOffset) (acceptableTimeDiff: TimeSpan) =
        let matchingMsg (other: Message) = (string other.Data) = msg
        let possibleMsg =
            bag
            |> Seq.tryFind matchingMsg
        
        match possibleMsg with
        | Some m when m.Timestamp.HasValue ->
            let diff =
                (m.Timestamp.Value - expectedTime).Ticks
                |> Math.Abs
                |> TimeSpan
            let isAcceptable = diff < acceptableTimeDiff
            if isAcceptable then
                Ok diff
            else
                Error $"Ably timer check failed. Acceptable diff is: {acceptableTimeDiff} while actual diff is: {diff}"
        | Some m ->
            Error $"Ably timer check failed. `{nameof(m.Timestamp)}` is empty"
        | None -> Error "Ably failed to receive message"

type AblyTimerHealthCheck (ably: AblyRealtime, serviceName: string, ?channelName: string, ?acceptableDiff: TimeSpan, ?sleepTimeForMsg: TimeSpan) =
    let messageTypeName = $"timer-{serviceName}"
    let acceptableTimeDiff =
        match acceptableDiff with
        | Some a -> a
        | None -> TimeSpan.FromSeconds 1.
    let sleepToGatherMsg =
        match sleepTimeForMsg with
        | Some stfm -> stfm
        | None -> TimeSpan.FromSeconds 0.5
            
    interface IHealthCheck with
        member __.CheckHealthAsync (context, ct) =
            async {
                if ct.IsCancellationRequested then
                    return HealthCheckResult(context.Registration.FailureStatus, $"Cancellation requested")
                else
                    let msgs = ConcurrentBag()
                    let channelName = channelName |> Option.defaultValue "healthcheck"
                    let channel = ably.Channels.Get channelName
                    channel.Subscribe(messageTypeName, fun msg -> msgs.Add msg)

                    let now = DateTimeOffset.UtcNow
                    let msgData = $"{now} - {serviceName}"
                    let! msg =
                        channel.PublishAsync (messageTypeName, msgData)
                        |> Async.AwaitTask
                    if msg.IsFailure then
                        return HealthCheckResult(context.Registration.FailureStatus, $"Ably failed to push message. Error code: {msg.Error.Code}, Status: {msg.Error.StatusCode} Message: {msg.Error.Message}")
                    else
                        do! Async.Sleep sleepToGatherMsg
                        match TimerCheck.doMatch msgs msgData now acceptableTimeDiff with
                        | Ok diff -> return HealthCheckResult.Healthy($"Ably is healthy, diff is: {diff}")
                        | Error errorMsg -> return HealthCheckResult(context.Registration.FailureStatus, errorMsg)
            } |> Async.StartAsTask
