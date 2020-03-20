using System.Collections.Generic;

namespace SimplifiedProtocolTestWpfCore
{
    internal sealed class IntegrationTestResult
    {
        public string TestName = "";
        public bool Success = false;
        public IEnumerable<string> Messages = new string[] { "Uninitialized test result" };
    }
}
