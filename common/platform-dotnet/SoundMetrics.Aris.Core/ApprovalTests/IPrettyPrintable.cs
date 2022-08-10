using System.Text;

namespace SoundMetrics.Aris.Core.ApprovalTests
{
    /// <summary>
    /// Pretty-print facility used for approval test output generation.
    /// </summary>
    public interface IPrettyPrintable
    {
        PrettyPrintHelper PrettyPrint(PrettyPrintHelper helper, string label);
    }
}
