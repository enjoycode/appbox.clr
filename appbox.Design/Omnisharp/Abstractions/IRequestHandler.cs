using System.Threading.Tasks;
using appbox.Data;
using appbox.Design;

namespace OmniSharp.Mef
{
    public interface IRequestHandler
    {

        Task<object> Handle(DesignHub hub, InvokeArgs args);

    }
}
