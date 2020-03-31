namespace RangeGenerator.UT

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Data.RangeGenerator

[<TestClass>]
type RangeGeneratorIntOptionTests () =

    let advance (increment: int) = fun value -> value + increment

    [<TestMethod>]
    member __.EqualStartAndEnd () =
        makeOptionalRange 1 1 (advance 1)
        |> Seq.toArray // make it concrete
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndGreaterThanStart () =
        makeOptionalRange 1 2 (advance 1)
        |> ignore

        // No exception occurred
        Assert.IsTrue(true);

    [<TestMethod>]
    member __.EndLessThanStart () =
        Assert.ThrowsException<ArgumentOutOfRangeException>(fun () ->
            makeOptionalRange 2 1 (advance 1)
            |> ignore

            // An exception should ahve occurred
            Assert.IsTrue(false);
            )
        |> ignore

    [<TestMethod>]
    member __.SmallestInterval () =
        let start = 1
        let endInclusive = 1
        let expected = [| None; Some start |]
        let actual =
            makeOptionalRange start endInclusive (advance 1)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.NextSmallestInterval () =
        let start = 1
        let endInclusive = 2
        let expected = [| None; Some start; Some endInclusive |]
        let actual =
            makeOptionalRange start endInclusive (advance 1)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)

    [<TestMethod>]
    member __.SecondNextSmallestInterval () =
        let increment = 1
        let start = 1
        let endInclusive = start + 2
        let expected = [| None; Some start; Some (start + increment); Some endInclusive |]
        let actual =
            makeOptionalRange start endInclusive (advance increment)
            |> Seq.toArray

        CollectionAssert.AreEqual(expected, actual)
