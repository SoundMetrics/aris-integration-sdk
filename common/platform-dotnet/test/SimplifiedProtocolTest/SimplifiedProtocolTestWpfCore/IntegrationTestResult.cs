using System.Collections.Generic;

namespace SimplifiedProtocolTestWpfCore
{
    internal sealed class IntegrationTestResult
    {
        public string TestName = "";
        public bool Success = false;
        public List<string> Messages = new List<string> { "Uninitialized test result" };
    }
}
