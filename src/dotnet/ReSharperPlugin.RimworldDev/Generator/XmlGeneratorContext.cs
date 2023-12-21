using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.Generator;

[Language(typeof (XmlLanguage), Instantiation.DemandAnyThread)]
public class XamlGeneratorContextFactory : IGeneratorContextFactory
{
  public IGeneratorContext TryCreate(string kind, IPsiDocumentRangeView psiDocumentRangeView)
  {
    return XmlGeneratorContext.CreateContext(kind, psiDocumentRangeView);
  }

  public IGeneratorContext TryCreate(string kind, IDeclaredElement contextElement)
  {
    return null;
  }

  public IGeneratorContext TryCreate(string kind, ITreeNode targetContext, ITreeNode anchor)
  {
    return null;
  }
}


  public class XmlGeneratorContext : GeneratorContextBase
  {
    private XmlGeneratorContext([NotNull] string kind, [NotNull] IXmlFile file, [CanBeNull] ITreeNode anchor)
      : base(kind)
    {
      XmlFile = file;
      Anchor = anchor;
      if (anchor != null)
      CodebehindDeclarations = EmptyList<IDeclaration>.InstanceList;
    }

    [CanBeNull]
    public static XmlGeneratorContext CreateContext(
      string kind,
      [NotNull] IPsiDocumentRangeView psiDocumentRangeView)
    {
      ITreeNode anchor;
      IXmlFile selectedTreeNode = psiDocumentRangeView.View<XmlLanguage>().GetSelectedTreeNode<IXmlFile>(out anchor);
      return selectedTreeNode == null ? null : new XmlGeneratorContext(kind, selectedTreeNode, anchor);
    }
    
    public IXmlFile XmlFile { get; set; }
    
    public IList<IDeclaration> CodebehindDeclarations { get; }

    public override sealed ITreeNode Anchor { get; set; }

    public override ITreeNode Root => XmlFile;

    public override ISolution Solution => XmlFile.GetSolution();

    public override IPsiModule PsiModule => XmlFile.GetPsiModule();

    public override PsiLanguageType Language => XmlFile.Language;

    public override PsiLanguageType PresentationLanguage
    {
      get
      {
        using (IEnumerator<IDeclaration> enumerator = CodebehindDeclarations.GetEnumerator())
        {
          if (enumerator.MoveNext())
            return enumerator.Current.Language;
        }
        return XmlFile.Language;
      }
    }

    public override TreeTextRange GetSelectionTreeRange() => TreeTextRange.InvalidRange;

    public override IGeneratorContextPointer CreatePointer()
    {
      return new XmlGeneratorWorkflowPointer(this);
    }

    private sealed class XmlGeneratorWorkflowPointer : GeneratorWorkflowPointer
    {
      [NotNull]
      private readonly ITreeNodePointer<IXmlFile> myClassPointer;
      [CanBeNull]
      private readonly ITreeNodePointer<ITreeNode> myAnchorPointer;

      public XmlGeneratorWorkflowPointer([NotNull] XmlGeneratorContext context)
        : base(context)
      {
        myClassPointer = context.XmlFile.CreateTreeElementPointer();
        ITreeNode anchor = context.Anchor;
        if (anchor == null)
          return;
        myAnchorPointer = anchor.CreateTreeElementPointer();
      }

      protected override GeneratorContextBase CreateWorkflow()
      {
        IXmlFile treeNode1 = myClassPointer.GetTreeNode();
        if (treeNode1 == null)
          return null;
        ITreeNode treeNode2 = myAnchorPointer != null ? myAnchorPointer.GetTreeNode() : null;
        return new XmlGeneratorContext(Kind, treeNode1, treeNode2);
      }
    }
  }
