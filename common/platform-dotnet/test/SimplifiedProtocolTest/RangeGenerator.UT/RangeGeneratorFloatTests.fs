namespace RangeGenerator.UT

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open RangeGenerator

[<TestClass>]
type RangeGeneratorFloatTests () =

    [<TestMethod>]
    member __.EqualStartAndEnd () =
        RangeGenerator<float>(1.0, 1.0, fun value -> value + 1.0)
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndGreaterThanStart () =
        RangeGenerator<float>(1.0, 2.0, fun value -> value + 1.0)
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndLessThanStart () =
        Assert.ThrowsException<ArgumentOutOfRangeException>(fun () ->
            let _ = RangeGenerator<float>(2.0, 1.0, fun value -> value + 1.0)
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
            RangeGenerator<float>(start, endInclusive, fun value -> value + increment)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.NextSmallestInterval () =
        let increment = 0.000001
        let start = 1.0
        let endInclusive = start + increment
        let expected = [| start; endInclusive |]
        let actual =
            RangeGenerator<float>(start, endInclusive, fun value -> value + increment)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.SecondNextSmallestInterval () =
        let increment = 0.000001
        let start = 1.0
        let endInclusive = start + increment + increment // don't multiply by 2, that's different than addition
        let expected = [| start; start + increment; endInclusive |]
        let actual =
            RangeGenerator<float>(start, endInclusive, fun value -> value + increment)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)
