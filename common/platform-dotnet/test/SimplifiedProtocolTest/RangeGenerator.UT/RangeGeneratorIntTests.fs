namespace RangeGenerator.UT

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open RangeGenerator

[<TestClass>]
type RangeGeneratorIntTests () =

    [<TestMethod>]
    member __.EqualStartAndEnd () =
        RangeGenerator<int>(1, 1, fun value -> value + 1)
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndGreaterThanStart () =
        RangeGenerator<int>(1, 2, fun value -> value + 1)
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndLessThanStart () =
        Assert.ThrowsException<ArgumentOutOfRangeException>(fun () ->
            let _ = RangeGenerator<int>(2, 1, fun value -> value + 1)
            // An exception should ahve occurred
            Assert.IsTrue(false);
            )
        |> ignore

    [<TestMethod>]
    member __.SmallestInterval () =
        let start = 1
        let endInclusive = 1
        let expected = [| start |]
        let actual =
            RangeGenerator(start, endInclusive, fun value -> value + 1)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.NextSmallestInterval () =
        let start = 1
        let endInclusive = 2
        let expected = [| start; endInclusive |]
        let actual =
            RangeGenerator(start, endInclusive, fun value -> value + 1)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)
