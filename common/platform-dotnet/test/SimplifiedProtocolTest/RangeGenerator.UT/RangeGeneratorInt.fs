namespace RangeGenerator.UT

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open RangeGenerator

[<TestClass>]
type RangeGeneratorIntTests () =

    [<TestMethod>]
    member __.EqualStartAndEnd () =
        RangeGenerator<int>(1, 1, fun i -> + 1)
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndGreaterThanStart () =
        RangeGenerator<int>(1, 2, fun i -> + 1)
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndLessThanStart () =
        Assert.ThrowsException<ArgumentOutOfRangeException>(fun () ->
            let _ = RangeGenerator<int>(2, 1, fun i -> + 1)
            // An exception should ahve occurred
            Assert.IsTrue(false);
            )
        |> ignore
