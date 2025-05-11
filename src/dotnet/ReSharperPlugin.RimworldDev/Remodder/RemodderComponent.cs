using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Protocol;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using ReSharperPlugin.RdProtocol;

namespace ReSharperPlugin.RimworldDev.Remodder;

[SolutionComponent(InstantiationEx.LegacyDefault)]
public class RemodderComponent
{
    public RemodderComponent(ISolution solution)
    {
        var model = solution.GetProtocolSolution().GetRemodderProtocolModel();

        model.Decompile.SetAsync((_, args) =>
        {
            return Task.Run(() =>
            {
                using (ReadLockCookie.Create())
                {
                    var filePath = args[0];
                    var typeName = args[1];
                    var userAsms = args.Skip(2).ToArray();

                    var alc = new AssemblyLoadContext("Remodder ALC", true);

                    try
                    {
                        foreach (var userAsm in userAsms)
                            alc.LoadFromStream(PathToStream(userAsm));

                        var project = solution
                            .FindProjectItemsByLocation(VirtualFileSystemPath.Parse(filePath,
                                InteractionContext.Local))
                            .FirstOrDefault()?.GetProject();

                        if (project == null)
                            return ["ERROR: Can't find the file's project"];

                        LoadReferenceAssemblies(alc, solution, project,
                            userAsms.Select(path => AssemblyName.GetAssemblyName(path).Name!).ToArray());

                        var projectAssembly = project.GetOutputFilePath(project.TargetFrameworkIds[0]);
                        var a = alc.LoadFromStream(PathToStream(projectAssembly.FullPath));
                        var transpiler = Decompiler.GetTranspiler(a, typeName);

                        return transpiler != null
                            ? DecompileOrigAndTranspiled(transpiler, userAsms)
                            : ["ERROR: No transpiler"];
                    }
                    finally
                    {
                        alc.Unload();
                    }
                }
            });
        });
    }

    private static void LoadReferenceAssemblies(AssemblyLoadContext alc, ISolution solution, IProject project,
        string[] asmNamesToIgnore)
    {
        foreach (var r in project.GetModuleReferences(project.TargetFrameworkIds[0]))
            if (r is ProjectToAssemblyReference assemblyReference)
            {
                if (r.Name.Contains("System") ||
                    r.Name.Contains("mscorlib") ||
                    r.Name.Contains("Win32") ||
                    r.Name.Contains("netstandard") ||
                    r.Name.Contains("Microsoft") ||
                    r.Name.Contains("Harmony") ||
                    asmNamesToIgnore.Contains(r.Name))
                    continue;

                var assemblyFile = assemblyReference.ReferenceTarget.HintLocation?.FullPath;
                if (assemblyFile != null)
                {
                    var assemblyBytes =
                        AssemblyHelper.RemoveReferenceAssemblyAttribute(File.ReadAllBytes(assemblyFile));
                    alc.LoadFromStream(new MemoryStream(assemblyBytes));
                }
            }
            else if (r is GuidProjectReference guidProjectReference)
            {
                var referencedProject = solution.GetProjectByGuid(guidProjectReference.ReferencedProjectGuid);
                if (referencedProject != null)
                {
                    var referencedProjectOutput =
                        referencedProject.GetOutputFilePath(referencedProject.TargetFrameworkIds[0]);
                    alc.LoadFromStream(PathToStream(referencedProjectOutput.FullPath));
                }
            }
    }

    private static string[] DecompileOrigAndTranspiled(AttributePatch patch, string[] userAsms)
    {
        var origDecomp = Decompiler.Decompile(
            patch.info.GetOriginalMethod(),
            null,
            userAsms,
            null
        );

        var decomp = Decompiler.Decompile(
            patch.info.GetOriginalMethod(),
            patch.info.method,
            userAsms,
            null
        );

        return [origDecomp, decomp];
    }

    private static MemoryStream PathToStream(string path)
    {
        return new MemoryStream(File.ReadAllBytes(path));
    }
}