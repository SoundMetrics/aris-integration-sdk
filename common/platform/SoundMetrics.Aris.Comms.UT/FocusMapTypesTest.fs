namespace SoundMetrics.Aris.Comms.UT

open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Aris.Comms.FocusMapTypes.NumericHelpers
open System

type TestResult<'T> = Value of 'T | Exception

[<TestClass>]
type FocusMapsTypeTest () =

    [<TestMethod>]
    member __.minmax_test() =
        let cases = [|
            // Inputs   Expected             
            (0, 0),     { Min = 0; Max = 0 }
            (0, 1),     { Min = 0; Max = 1 }
            (1, 0),     { Min = 0; Max = 1 }
        |]

        for ((left, right), expected) in cases do
            let description = sprintf "inputs: l=[%d]; r=[%d]" left right
            let actual = minmax left right
            Assert.AreEqual(expected, actual, description)

    [<TestMethod>]
    member __.inRange_test() =

        let cases = [|
            // Inputs       Expected
            0, (1, 2),      false
            1, (1, 2),      true
            2, (1, 2),      true
            3, (1, 2),      false
        |]

        for (t, (l, r), expected) in cases do
            let description = sprintf "inputs: t=[%d]; lr=[(%d, %d)]" t l r
            let actual = inRange t l r
            Assert.AreEqual(expected, actual, description)

    [<TestMethod>]
    member __.getEnclosingRange_test() =

        let ascending =  [| 4; 5; 6 |]
        let descending = [| 6; 5; 4 |]

        let cases = [|
            // Inputs           Expected
            (0, ascending),     Exception
            (4, ascending),     Value (struct (0, 1))
            (5, ascending),     Value (struct (0, 1))
            (6, ascending),     Value (struct (1, 2))
            (7, ascending),     Exception

            (7, descending),    Exception
            (6, descending),    Value (struct (0, 1))
            (5, descending),    Value (struct (0, 1))
            (4, descending),    Value (struct (1, 2))
            (3, descending),    Exception
        |]

        for (t, values), expected in cases do
            let description = sprintf "inputs: t=[%d]; values=[%A]" t values

            match expected with
            | Value expected ->
                let actual = getEnclosingRange t values
                Assert.AreEqual(expected, actual, description)
            | Exception ->
                try
                    getEnclosingRange t values |> ignore
                    Assert.Fail("should have thrown")
                with
                    _ -> Assert.IsTrue(true)

    [<TestMethod>]
    member __.getBracketIndex_test() =

        let ascending =  [| 4; 5; 6 |]
        let descending = [| 6; 5; 4 |]

        let cases = [|
            // Inputs           Expected
            (0, ascending),     (struct (0, 0))
            (4, ascending),     (struct (0, 1))
            (5, ascending),     (struct (0, 1))
            (6, ascending),     (struct (1, 2))
            (7, ascending),     (struct (2, 2))

            (7, descending),    (struct (0, 0))
            (6, descending),    (struct (0, 1))
            (5, descending),    (struct (0, 1))
            (4, descending),    (struct (1, 2))
            (3, descending),    (struct (2, 2))
        |]

        for (t, values), expected in cases do
            let description = sprintf "inputs: t=[%d]; values=[%A]" t values
            let actual = getBracketIndex t values
            Assert.AreEqual(expected, actual, description)

    [<TestMethod>]
    member __.interpolate_test() =

        let cases = [|
            // Inputs                                       // Expected
            (0.0f, struct (0.0f, 1.0f), struct (10.0f, 11.0f)),  10.0f
            (0.1f, struct (0.0f, 1.0f), struct (10.0f, 11.0f)),  10.1f
            (0.1f, struct (0.0f, 1.0f), struct (10.0f, 20.0f)),  11.0f

            (0.0f, struct (0.0f, 1.0f), struct (11.0f, 10.0f)),  11.0f
            (0.1f, struct (0.0f, 1.0f), struct (11.0f, 10.0f)),  10.9f
            (0.1f, struct (0.0f, 1.0f), struct (20.0f, 10.0f)),  19.0f
        |]

        for (input, inputRange, outputRange), expected in cases do
            let description = sprintf "inputs: t=[%A]; inputRange=[%A]; outputRange=[%A]"
                                input inputRange outputRange
            let actual = interpolate input inputRange outputRange
            Assert.AreEqual(expected, actual, description)

    [<TestMethod>]
    member __.closerBound_test() =

        let cases = [|
            // Inputs           Expected
            (0.0, 0.0, 1.0),    0.0
            (0.1, 0.0, 1.0),    0.0
            (0.4, 0.0, 1.0),    0.0
            (0.5, 0.0, 1.0),    1.0
            (0.9, 0.0, 1.0),    1.0
            (1.0, 0.0, 1.0),    1.0
        |]

        for (target, lb, ub), expected in cases do
            let description = sprintf "target=[%f]; lb=[%f]; ub=[%f]" target lb ub
            let actual = closerBound target lb ub
            Assert.AreEqual(expected, actual, description)

