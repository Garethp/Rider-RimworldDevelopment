using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Conflicts;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.Util;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev.Refactoring;

[ShellFeaturePart]
public class DefElementRenameFactory : AtomicRenamesFactory
{
    public override bool IsApplicable(IDeclaredElement declaredElement)
    {
        return declaredElement.PresentationLanguage.Is<XmlLanguage>();
    }

    public override IEnumerable<AtomicRenameBase> CreateAtomicRenames(IDeclaredElement declaredElement, string newName,
        bool doNotAddBindingConflicts)
    {
        yield return new XmlDefAtomicRename(declaredElement, newName, doNotAddBindingConflicts);
    }

    public override RenameAvailabilityCheckResult CheckRenameAvailability(
        IDeclaredElement declaredElement)
    {
        return RenameAvailabilityCheckResult.CanBeRenamed;
        // RenameAvailabilityCheckResult availabilityCheckResult = base.CheckRenameAvailability(declaredElement);
        // if ((EnumPattern) availabilityCheckResult != (EnumPattern) RenameAvailabilityCheckResult.CanBeRenamed)
        //     return availabilityCheckResult;
        // switch (declaredElement)
        // {
        //     case ITypeMember _:
        //         return RenameAvailabilityCheckResult.CanBeRenamed;
        //     case ITypeElement _:
        //         return RenameAvailabilityCheckResult.CanBeRenamed;
        //     case IXamlNamespaceAlias _:
        //         return declaredElement.ShortName != "x" ? RenameAvailabilityCheckResult.CanBeRenamed : XamlRenameAvailabilityCheckResult.NotSupportedInXaml;
        //     case IXamlResource _:
        //         if (declaredElement.GetDeclarations().Count > 0)
        //             return RenameAvailabilityCheckResult.CanBeRenamed;
        //         break;
        // }
        // return XamlRenameAvailabilityCheckResult.NotSupportedInXaml;
    }
}

[PublicAPI]
public class XmlDefAtomicRename : AtomicRenameBase
{
    [NotNull] private readonly IDeclaredElementPointer<IDeclaredElement> myOriginalElementPointer;
    private readonly bool myDoNotShowBindingConflicts;
    [NotNull] private readonly List<IReference> myNewReferences = new();
    [NotNull] private readonly List<IDeclaration> myDeclarations = new();
    [CanBeNull] private readonly List<IDeclaredElementPointer<IDeclaredElement>> mySecondaryElements;
    [CanBeNull] private DeclaredElementEnvoy<IDeclaredElement> myDeclaredElementEnvoy;
    [CanBeNull] private IDeclaredElementPointer<IDeclaredElement> myNewElementPointer;

    public override IDeclaredElement NewDeclaredElement => myNewElementPointer.NotNull().FindDeclaredElement();

    public override IDeclaredElement PrimaryDeclaredElement
    {
        get
        {
            IDeclaredElement declaredElement = myOriginalElementPointer.FindDeclaredElement();
            if (declaredElement != null)
                return declaredElement;
            return myDeclaredElementEnvoy?.GetValidDeclaredElement();
        }
    }

    [NotNull]
    public override IList<IDeclaredElement> SecondaryDeclaredElements
    {
        get
        {
            return mySecondaryElements == null
                ? EmptyList<IDeclaredElement>.Instance
                : mySecondaryElements
                    .SelectNotNull(
                        (Func<IDeclaredElementPointer<IDeclaredElement>, IDeclaredElement>)
                        (x => x.FindDeclaredElement())).ToList();
        }
    }

    public override string NewName { get; }

    public override string OldName { get; }


    public XmlDefAtomicRename(
        [NotNull] IDeclaredElement declaredElement,
        [NotNull] string newName,
        bool doNotShowBindingConflicts)
    {
        myOriginalElementPointer = declaredElement.CreateElementPointer();
        NewName = newName;
        OldName = declaredElement.ShortName.Split('/').Last();
        myDoNotShowBindingConflicts = doNotShowBindingConflicts;
        mySecondaryElements = RenameRefactoringService.Instance.GetRenameService(declaredElement.PresentationLanguage)
            .GetSecondaryElements(declaredElement, newName).ToList(x => x.CreateElementPointer());
        BuildDeclarations();
    }

    public override void Rename(IRenameRefactoring executer, IProgressIndicator progress,
        bool hasConflictsWithDeclarations,
        IRefactoringDriver driver)
    {
        BuildDeclarations();
        var declaredElement = myOriginalElementPointer.FindDeclaredElement();
        if (declaredElement == null)
            return;
        var psiServices = declaredElement.GetPsiServices();
        var elementReferences = executer.Workflow.GetElementReferences(PrimaryDeclaredElement);
        progress.Start(myDeclarations.Count + elementReferences.Count);
        foreach (var declaration in myDeclarations)
        {
            InterruptableActivityCookie.CheckAndThrow(progress);
            executer.Workflow.LanguageSpecific[declaration.Language].SetName(declaration, NewName, executer);
            progress.Advance();
        }

        psiServices.Caches.Update();
        var newDeclaredElement = myDeclarations[0].DeclaredElement;
        myNewElementPointer = newDeclaredElement.CreateElementPointer();
        myNewReferences.Clear();
        PsiLanguageType key1;
        ISet<IReference> referenceSet;

        
        foreach (var reference in elementReferences)
        {
            if (reference is not RimworldXmlDefReference defReference) continue;
            var element = defReference.GetElement();
            
            // @TODO: Here we should attempt to handle [DefOf] references
            if (element is IFieldDeclaration field)
            {
                continue;
            }
            
            if (element is ICSharpLiteralExpression defDatabaseLiteral)
            {
                if (defDatabaseLiteral.FirstChild is not CSharpGenericToken textItem) continue;
                
                var substitution =
                    new CSharpGenericToken(textItem.GetTokenType(), textItem.GetText().Replace(OldName, NewName));

                ModificationUtil.ReplaceChild(textItem, substitution);
                progress.Advance();
                continue;
            }

            if (element is not XmlFloatingTextToken) return;
            if (element.GetTokenType() is not XmlTokenNodeType tokenType) return;

            ModificationUtil.ReplaceChild(element, new XmlFloatingTextToken(tokenType, NewName));
            progress.Advance();
        }
        
        // foreach (var referencesWithLanguage in LanguageUtil
        //              .SortReferencesWithLanguages(elementReferences.Where(x => x.IsValid()), psiServices))
        // {
        //     referencesWithLanguage.Deconstruct(out key1, out referenceSet);
        //     var language = key1;
        //     var references = referenceSet;
        //     var rename = executer.Workflow.LanguageSpecific[language];
            
        //     foreach (IGrouping<IPsiSourceFile, IReference> grouping1 in references.GetSortedReferences()
        //                  .GroupBy(
        //                      it => it.GetTreeNode().GetSourceFile()))
        //     {
        //         IPsiSourceFile key2 = grouping1.Key;
        //         if (key2 != null)
        //         {
        //             IGrouping<IPsiSourceFile, IReference> grouping = grouping1;
        //             psiServices.Caches.WithSyncUpdateFiltered(key2, () =>
        //             {
        //                 foreach (IReference reference1 in grouping)
        //                 {
        //                     // @TODO: Is this where we're meant to change the text?!
        //                     IReference oldReferenceForConflict = reference1;
        //                     InterruptableActivityCookie.CheckAndThrow(progress);
        //                     if (reference1.IsValid())
        //                     {
        //                         IReference reference2 = rename.TransformProjectedInitializer(reference1);
        // DeclaredElementInstance subst = GetSubst(newDeclaredElement, executer);
        //                         IReference reference3 = !(subst != null)
        //                             ? rename.BindReference(reference2, newDeclaredElement)
        //                             : (subst.Substitution.Domain.IsEmpty()
        //                                 ? rename.BindReference(reference2, subst.Element)
        //                                 : rename.BindReference(reference2, subst.Element, subst.Substitution));
        //                         if (!(reference3 is IImplicitReference) && !hasConflictsWithDeclarations &&
        //                             !myDoNotShowBindingConflicts && !rename.IsAlias(newDeclaredElement) &&
        //                             !rename.IsCheckResolvedTo(reference3, newDeclaredElement))
        //                             driver.AddLateConflict(
        //                                 () => Conflict.Create(oldReferenceForConflict,
        //                                     ConflictType.CANNOT_UPDATE_USAGE_CONFLICT), "late bound");
        //                         myNewReferences.Insert(0, reference3);
        //                         rename.AdditionalReferenceProcessing(newDeclaredElement, reference3,
        //                             myNewReferences);
        //                     }
        //
        //                     progress.Advance();
        //                 }
        //             });
        //         }
        //     }
        // }

        // foreach ((IDeclaredElement element, IList<IReference> source) in SecondaryDeclaredElements
        //              .ToList(
        //                  (Func<IDeclaredElement, (IDeclaredElement, IList<IReference>)>)(secondaryElement =>
        //                      (secondaryElement, executer.Workflow.GetElementReferences(secondaryElement)))))
        // {
        //     IDeclaredElement updatedElement =
        //         UpdateSecondaryElement(element, newDeclaredElement, executer) ?? element;
        //     foreach (KeyValuePair<PsiLanguageType, ISet<IReference>> referencesWithLanguage in
        //              LanguageUtil.SortReferencesWithLanguages(
        //                  source.Where(x => x.IsValid()), psiServices))
        //     {
        //         referencesWithLanguage.Deconstruct(out key1, out referenceSet);
        //         PsiLanguageType language = key1;
        //         ISet<IReference> references = referenceSet;
        //         RenameHelperBase rename = executer.Workflow.LanguageSpecific[language];
        //         foreach (IGrouping<IPsiSourceFile, IReference> grouping2 in references.GetSortedReferences()
        //                      .GroupBy(
        //                          it => it.GetTreeNode().GetSourceFile()))
        //         {
        //             IGrouping<IPsiSourceFile, IReference> grouping = grouping2;
        //             psiServices.Caches.WithSyncUpdateFiltered(grouping.Key, () =>
        //             {
        //                 foreach (IReference reference in grouping)
        //                 {
        //                     InterruptableActivityCookie.CheckAndThrow(progress);
        //                     if (reference.IsValid())
        //                         rename.TransformProjectedInitializer(reference).BindTo(updatedElement);
        //                 }
        //             });
        //         }
        //     }
        // }
    }

    private static DeclaredElementInstance GetSubst(
        IDeclaredElement element,
        IRenameRefactoring executer)
    {
        return executer.Workflow.LanguageSpecific[element.PresentationLanguage].GetSubst(element);
    }

    [CanBeNull]
    private static IDeclaredElement UpdateSecondaryElement(
        IDeclaredElement element,
        IDeclaredElement newDeclaredElement,
        IRenameRefactoring executor)
    {
        return executor.Workflow.LanguageSpecific[element.PresentationLanguage]
            .UpdateSecondaryElement(element, newDeclaredElement);
    }

    private void BuildDeclarations()
    {
        myDeclarations.Clear();
        IDeclaredElement declaredElement = myOriginalElementPointer.FindDeclaredElement();
        if (declaredElement == null)
            return;
        IList<IDeclaration> allDeclarations = new MultyPsiDeclarations(declaredElement).AllDeclarations;
        if (allDeclarations.Count == 0)
        {
            Assertion.Fail("Element of type '{0}' has no declarations.", declaredElement.GetElementType());
        }
        else
        {
            foreach (IDeclaration declaration in allDeclarations)
                myDeclarations.Add(declaration);
        }
    }
}