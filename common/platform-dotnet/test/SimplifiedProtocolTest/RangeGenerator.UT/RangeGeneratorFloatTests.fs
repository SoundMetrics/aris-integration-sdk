namespace RangeGenerator.UT

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Data.RangeGenerator

[<TestClass>]
type RangeGeneratorFloatTests () =

    let advance (increment: float) = fun value -> value + increment

    [<TestMethod>]
    member __.EqualStartAndEnd () =
        makeRange 1.0 1.0 (advance 1.0)
        |> Seq.toArray // make it concrete
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndGreaterThanStart () =
        makeRange 1.0 2.0 (advance 1.0)
        |> Seq.toArray // make it concrete
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndLessThanStart () =
        Assert.ThrowsException<ArgumentOutOfRangeException>(fun () ->
            makeRange 2.0 1.0 (advance 1.0)
            |> Seq.toArray // make it concrete
            |> ignore

            // An exception should ahve occurred
            Assert.IsTrue(false);
            )
        |> ignore

    [<TestMethod>]
    member __.SmallestInterval () =
        let increment = 0.000001
        let start = 1.0
        let endInclusive = start
        let expected = [| start |]
        let actual =
            makeRange start endInclusive (advance increment)
            |> Seq.toArray // make it concrete

        CollectionAssert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.NextSmallestInterval () =
        let increment = 0.000001
        let start = 1.0
        let endInclusive = start + increment
        let expected = [| start; endInclusive |]
        let actual =
            makeRange start endInclusive (advance increment)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.SecondNextSmallestInterval () =
        let increment = 0.000001
        let start = 1.0
        let endInclusive = start + increment + increment // don't multiply by 2, that's different than addition
        let expected = [| start; start + increment; endInclusive |]
        let actual =
            makeRange start endInclusive (advance increment)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)
