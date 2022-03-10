namespace Tests

open System
open System.Collections.Concurrent
open Ably.HealthCheck
open Expecto
open IO.Ably

module TimerTests =
    let tests = testList "Timer" [
        testList "TimerHealtcheck validation" [
            testCase "sleep higher than acceptable diff" <| fun _ ->
                let create () =
                    let _ = AblyTimerHealthCheck(new AblyRealtime("empty"), "", "", TimeSpan.FromSeconds 2., TimeSpan.FromSeconds 10.)
                    ()
                    
                Expect.throwsT<SleepHigherThanAcceptableDiffException> create ""
                
            testCase "sleep lower than acceptable diff" <| fun _ ->
                let _ = AblyTimerHealthCheck(new AblyRealtime("empty"), "", "", TimeSpan.FromSeconds 20., TimeSpan.FromSeconds 10.)
                    
                Expect.isTrue true ""
        ]
        
        testList "Timer Check" [
            testCase "timespan not available" <| fun _ ->
                let bag = ConcurrentBag()
                let baseMsg = "some msg"
                let now = DateTimeOffset(DateTime(2020, 2, 10, 10, 10, 5))
                let mutable ablyMsg = Message(data = baseMsg)
                bag.Add ablyMsg
                
                let result = TimerCheck.doMatch bag baseMsg now (TimeSpan.FromSeconds 1.)

                Expect.equal result (Error "Ably timer check failed. `Timestamp` is empty") ""
                
            testCase "no msg" <| fun _ ->
                let bag = ConcurrentBag()
                let baseMsg = "some msg"
                let now = DateTimeOffset(DateTime(2020, 2, 10, 10, 10, 5))
                
                let result = TimerCheck.doMatch bag baseMsg now (TimeSpan.FromSeconds 1.)
                
                Expect.equal result (Error "Ably failed to receive message") ""
                
            testCase "timespan shifted too much in the future" <| fun _ ->
                let diff = TimeSpan(0, 0, 0, 0, 100)
                let bag = ConcurrentBag()
                let baseMsg = "some msg"
                let now = DateTimeOffset(DateTime(2020, 2, 10, 10, 10, 5))
                let mutable ablyMsg = Message(data = baseMsg)
                ablyMsg.Timestamp <- System.Nullable<DateTimeOffset>(now + diff)
                bag.Add ablyMsg
                
                let result = TimerCheck.doMatch bag baseMsg now (TimeSpan.FromMilliseconds 10.)
                
                Expect.equal result (Error "Ably timer check failed. Acceptable diff is: 00:00:00.0100000 while actual diff is: 00:00:00.1000000") ""
                
            testCase "timespan shifted too much in the past" <| fun _ ->
                let diff = TimeSpan(0, 0, 0, 0, 120)
                let bag = ConcurrentBag()
                let baseMsg = "some msg"
                let now = DateTimeOffset(DateTime(2020, 2, 10, 10, 10, 5))
                let mutable ablyMsg = Message(data = baseMsg)
                ablyMsg.Timestamp <- System.Nullable<DateTimeOffset>(now - diff)
                bag.Add ablyMsg
                
                let result = TimerCheck.doMatch bag baseMsg now (TimeSpan.FromSeconds 0.)
                
                Expect.equal result (Error "Ably timer check failed. Acceptable diff is: 00:00:00 while actual diff is: 00:00:00.1200000") ""
                
            testCase "diff in acceptable range" <| fun _ ->
                let diff = TimeSpan(0, 0, 0, 0, 100)
                let bag = ConcurrentBag()
                let baseMsg = "some msg"
                let now = DateTimeOffset(DateTime(2020, 2, 10, 10, 10, 5, 500))
                let mutable ablyMsg = Message(data = baseMsg)
                ablyMsg.Timestamp <- System.Nullable<DateTimeOffset>(now.Add diff)
                bag.Add ablyMsg
                
                let result = TimerCheck.doMatch bag baseMsg now (TimeSpan.FromSeconds 1.)
                
                Expect.equal result (Ok diff) ""
        ]
    ]