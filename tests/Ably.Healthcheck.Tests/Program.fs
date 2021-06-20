open Tests

module Program =
    open Expecto

    let tests = testList "Healthcheck" [
        TimerTests.tests
    ]
    
    [<EntryPoint>]
    let main argv =
        Tests.runTestsWithArgs defaultConfig argv tests
