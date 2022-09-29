```
com.jetbrains.rdclient.util.BackendException: Sequence contains no matching element

--- EXCEPTION #1/2 [InvalidOperationException]
Message = “Sequence contains no matching element”
ExceptionPath = Root.InnerException
ClassName = System.InvalidOperationException
HResult = COR_E_INVALIDOPERATION=80131509
Source = System.Core
StackTraceString = “
at System.Linq.Enumerable.First[TSource](IEnumerable`1 source, Func`2 predicate)
at ReSharperPlugin.RimworldDev.RimworldXMLItemProvider.GetContextFromHierachy(List`1 hierarchy, ISymbolScope symbolScope, List`1 allSymbolScopes) in C:\Users\Gareth\development\Rider-RimworldDevelopment\src\dotnet\ReSharperPlugin.RimworldDev\RimworldXMLItemProvider.cs:line 289
at ReSharperPlugin.RimworldDev.References.RimworldReferenceFactory.GetReferences(ITreeNode element, ReferenceCollection oldReferences) in C:\Users\Gareth\development\Rider-RimworldDevelopment\src\dotnet\ReSharperPlugin.RimworldDev\References\RimworldReferenceProvider.cs:line 60
at JetBrains.ReSharper.Psi.Files.ReferenceProviderFactory.CachingReferenceProvider.GetReferences(ITreeNode element, IReferenceNameContainer names)
at JetBrains.ReSharper.Psi.Tree.TreeNodeExtensions.GetReferencesImpl(ITreeNode element, IReferenceProvider referenceProvider, IReferenceNameContainer names)
at JetBrains.ReSharper.Psi.Tree.TreeNodeExtensions.GetReferences(ITreeNode element, IReferenceProvider referenceProvider, IReferenceNameContainer names)
at JetBrains.ReSharper.Daemon.UsageChecking.UsageAnalyzer.ProcessElement(ITreeNode treeNode, IParameters parameters)
at JetBrains.ReSharper.Daemon.UsageChecking.ScopeProcessor.ProcessElement(ITreeNode element)
at JetBrains.ReSharper.Psi.RecursiveElementProcessorExtensions.ProcessDescendants[TContext](ITreeNode root, IRecursiveElementProcessor`1 processor, TContext context)
at JetBrains.ReSharper.Daemon.UsageChecking.CommonCollectUsagesPsiFileProcessor.ProcessFile(IDaemonProcess daemonProcess, IFile psiFile, IScopeProcessor topLevelScopeProcessor)
at JetBrains.ReSharper.Daemon.UsageChecking.CollectUsagesStageProcess.Execute(Action`1 committer)
at JetBrains.ReSharper.Feature.Services.Daemon.DaemonProcessBase.RunStage(IDaemonStage stage, DaemonProcessKind processKind, Action`2 commiter, IContextBoundSettingsStore contextBoundSettingsStore, JetHashSet`1 disabledStages)
”

--- Outer ---
```