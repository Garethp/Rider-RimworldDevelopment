using System;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Web.WebConfig;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using ReSharperPlugin.RimworldDev.SymbolScope;


namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

public class RimworldCSharpDefProvider
{
}

[ReferenceProviderFactory]
public class RimworldCSharpReferenceProvider : IReferenceProviderFactory
{
    public RimworldCSharpReferenceProvider(Lifetime lifetime) =>
        Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);

    public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
    {
        return sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>()
            // && sourceFile.GetExtensionWithDot().Equals(".xml", StringComparison.CurrentCultureIgnoreCase)
            ? new RimworldCSharpReferenceFactory()
            : null;
    }

    public ISignal<IReferenceProviderFactory> Changed { get; }
}

/**
 * The reference provider is just that, it provides a reference from XML into C# (And hopefully someday the other way
 * around with Find Usages), where you can Ctrl+Click into the C# for a property or class
 */
public class RimworldCSharpReferenceFactory : IReferenceFactory
{
    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
        if (element is ICSharpLiteralExpression expression &&
            expression.Parent?.Parent?.Parent is IInvocationExpression invocationExpression &&
            invocationExpression.GetText().Contains("DefDatabase") &&
            invocationExpression.Type() is IDeclaredType declaredType)
        {
            var defTypeName = declaredType.GetClrName().ShortName;
            var defName = element.GetText().Trim('"');
            
            var xmlSymbolTable = element.GetSolution().GetComponent<RimworldSymbolScope>();

            var defNameTag = new DefNameValue(defTypeName, defName);
            defNameTag = xmlSymbolTable.GetDefName(defNameTag);
            
            if (!xmlSymbolTable.HasTag(defNameTag)) return new ReferenceCollection();

            if (xmlSymbolTable.GetTagByDef(defNameTag) is not { } tag)
                return new ReferenceCollection();

            return new ReferenceCollection(new RimworldXmlDefReference(element, tag, defNameTag.DefType, defNameTag.DefName));
        }

        if (element.GetContainingTypeElement() is not IClass containingClass || containingClass
                .GetAttributeInstances(AttributesSource.Self)
                .FirstOrDefault(attribute => attribute.GetClrName().FullName == "RimWorld.DefOf") is null)
        {
            return new ReferenceCollection();
        }

        if (element is IFieldDeclaration fieldDeclaration && fieldDeclaration.Type is IDeclaredType fieldType)
        {
            var defTypeName = fieldType.GetClrName().ShortName;
            var defName = element.GetText();

            var xmlSymbolTable = element.GetSolution().GetComponent<RimworldSymbolScope>();

            var defNameTag = new DefNameValue(defTypeName, defName);
            defNameTag = xmlSymbolTable.GetDefName(defNameTag);
            
            if (!xmlSymbolTable.HasTag(defNameTag)) return new ReferenceCollection();

            if (xmlSymbolTable.GetTagByDef(defNameTag) is not { } tag)
                return new ReferenceCollection();

            return new ReferenceCollection(new RimworldXmlDefReference(element, tag, defNameTag.DefType, defNameTag.DefName));
        }

        return new ReferenceCollection();
    }

    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        if (element.NodeType.ToString() != "TEXT") return false;
        return !element.Parent.GetText().Contains("defName") && names.Contains(element.GetText());
    }
}