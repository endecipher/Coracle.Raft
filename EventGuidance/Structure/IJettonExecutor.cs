using EventGuidance.Responsibilities;
using System.Threading.Tasks;

namespace EventGuidance.Structure
{
    public interface IJettonExecutor
    {
        Task Perform(IActionJetton jetton);

        IActionJetton ReturnJetton();
    }
}
