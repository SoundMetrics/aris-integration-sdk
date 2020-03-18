namespace SimplifiedProtocolTestWpfCore
{
    internal static partial class IntegrationTest
    {
        private static readonly IntegrationTestCase[] testCases =
        {
            new IntegrationTestCase
            {
                Name = "Dummy test",
                IntegrationTestRunner =
                    (frameObservable, name) => { return new IntegrationTestResult(); },
            },
        };
    }
}
