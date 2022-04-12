using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace ReSharperPlugin.RimworldDev
{
    [IntellisensePart]
    public class RimworlXMLCompletionContextProvider: XmlCodeCompletionContextProvider
    {
        public override bool IsApplicable(CodeCompletionContext context)
        {
            return true;
        }

        public override ISpecificCodeCompletionContext GetCompletionContext(CodeCompletionContext context)
        {
            if (context.File is not IXmlFile xmlFile) return null;
            
            XmlReparsedCodeCompletionContext unterminatedContext = this.CreateUnterminatedContext(xmlFile, context);
            if (unterminatedContext == null)
                return (ISpecificCodeCompletionContext) null;
            unterminatedContext.Init();
            IReference reference = unterminatedContext.Reference;
            ITreeNode treeNode = unterminatedContext.TreeNode;
            if (treeNode == null)
                return (ISpecificCodeCompletionContext) null;
            TreeTextRange treeRange = reference == null ? XmlCodeCompletionContextProvider.GetElementRange(treeNode) : reference.GetTreeTextRange();
            DocumentRange documentRange = unterminatedContext.ToDocumentRange(treeRange);
            if (!documentRange.IsValid())
                return (ISpecificCodeCompletionContext) null;
            ref DocumentRange local1 = ref documentRange;
            DocumentOffset caretDocumentOffset = context.EffectiveCaretDocumentOffset;
            ref DocumentOffset local2 = ref caretDocumentOffset;
            if (!local1.Contains(in local2))
                return (ISpecificCodeCompletionContext) null;

            var symbolTable = new SymbolTable(context.Solution);
            
            TextLookupRanges textLookupRanges = CodeCompletionContextProviderBase.GetTextLookupRanges(context, documentRange);
            return this.CreateSpecificCompletionContext(context, textLookupRanges, unterminatedContext);
        }
    }
}