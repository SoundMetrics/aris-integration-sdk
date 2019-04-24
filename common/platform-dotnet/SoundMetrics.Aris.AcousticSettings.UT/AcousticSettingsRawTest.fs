namespace SoundMetrics.Aris.AcousticSettings.UT

open Microsoft.VisualStudio.TestTools.UnitTesting
open SoundMetrics.Aris.AcousticSettings

module Details =

    let areEqual<'T> a b = Assert.AreEqual<'T>(a, b)
    let areNotEqual<'T> a b = Assert.AreNotEqual<'T>(a, b)

open Details

[<TestClass>]
type AcousticSettingsRawTest () =

    [<TestMethod>]
    member __.``Equality`` () =

        let a = AcousticSettingsRaw.Invalid
        let b = AcousticSettingsRaw.Invalid
        areEqual a b

    [<TestMethod>]
    member __.``Inequality`` () =

        // Not terribly complete
        let a = AcousticSettingsRaw.Invalid
        let b = { AcousticSettingsRaw.Invalid with SampleCount = a.SampleCount + 1 }
        areNotEqual a b
