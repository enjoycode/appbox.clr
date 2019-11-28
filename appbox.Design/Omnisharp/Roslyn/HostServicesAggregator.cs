using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace OmniSharp
{
    public class HostServicesAggregator
    {
        readonly ImmutableArray<Assembly> _assemblies;

        public HostServicesAggregator(/*IEnumerable<IHostServicesProvider> hostServicesProviders*/)
        {
            var builder = ImmutableHashSet.CreateBuilder<Assembly>();

            // We always include the default Roslyn assemblies, which includes:
            //
            //   * Microsoft.CodeAnalysis.Workspaces
            //   * Microsoft.CodeAnalysis.CSharp.Workspaces
            //   * Microsoft.CodeAnalysis.VisualBasic.Workspaces
            foreach (var assembly in MefHostServices.DefaultAssemblies)
            {
                builder.Add(assembly);
            }

            //TODO: 暂在这里加入CSharp所需的Assemblies
            var csharpAsms = new string[] {
                "Microsoft.CodeAnalysis.CSharp.Workspaces.dll",
                "Microsoft.CodeAnalysis.CSharp.Features.dll"
            };
            for (int i = 0; i < csharpAsms.Length; i++)
            {
                var path = System.IO.Path.Combine(appbox.Design.MetadataReferences.LibPath, csharpAsms[i]);
                var asm = Assembly.LoadFrom(path);
                builder.Add(asm);
            }

            // foreach (var provider in hostServicesProviders)
            // {
            //     foreach (var assembly in provider.Assemblies)
            //     {
            //         builder.Add(assembly);
            //     }
            // }

            _assemblies = builder.ToImmutableArray();
        }

        public HostServices CreateHostServices()
        {
            return MefHostServices.Create(_assemblies);
        }
    }
}
