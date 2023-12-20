## Find Usages

This is a trail of thoughts/possible solutions and references that I went down for Find Usages. They mostly exist to
document what I went through, and as a reference if I want to try and understand my thought process later down the line
when I try to expand the Find Usages functionality to finding XML Defs from C# (think DefOf usage)

### Reference Points
 * `SearchProcessorBase` appears to be where the we can look to see if nodes are being visited
 * `NamedThingsSearchSourceFileProcessor::GetReferences` is where I think our reference is being filtered out
 * `FomderSearchDomainVisitor::Add` and `AsyncSearchResult::Add` are where the references are being added to the result set
 * `CSharpReferenceSearcher::ProcessElement` is where we're seeing some of the C# references being processed?

### Thoughts/Notes
So far we've been abusing the `ShortName` of our `XmlTagDeclaredElement` to store both the defName and the defType, but
that appears to be one of the things limiting us from being able to find usages. `ShortName` should probably refer only
to the `defName` of our def, but trying to do that quickly just results in references not existing, presumably because
we can't grab them from the symbol table.

In this branch I've done work to actually move over to a singleton Symbol Table, which is good work, however since I think
it's almost a pre-requisite for the rest of our work, we really need to separate it from this branch and put it into main.
This branch is getting pretty messy because we're trying to do a whole number of things. Likewise, the ShortName migration
should probably be done separately as well so that we're not just throwing a bunch of shit against the wall to see what sticks.

I'm not sure we need/want to implement a separate `IFindUsagesContextSearch` ourselves. I think what we want to be looking
at is possibly just our own `SearcherFactory` and `DomainSearcher`. I think what we've got so far is a start however the
ShortName issue has caused us to make hacky workarounds to swap the ShortName on demand rather than just construct it properly.
With that kind of approach, there's no real way to say what we do or do not need. Still, with a propper ShortName we were
actually hitting `RimworldReferenceProvider::hasReference` for the first time ever. We were visiting nodes, collecting references
it's just that those references were being filtered out at some point, so we need to investigate that.

One problem though is that we're still trying to do a "Find Usages" on an actual usage of the def. If we try to do it on the
defName itself, we don't get what we need. Is that because we've attached the declaredElement to the xmlTag instead of the
string? No clue. Could be something else. I think once we get the ShortName sorted and the SymbolTable sorted, we can either
reference the Unity/YAML plugin to see what they've got or we can post to slack for help. Once we've cleaned up that is.

-----

It looks like we may be looking in the wrong place. We implemented a SearchFactory for LateBoundReferences, but we're
not serving up a LateBoundReference (I think), we're serving up a regular reference.

-----

That was definitely the wrong place. Implementing a DeclaredElementReferenceSearcher works well, but we need to do a 
`SwapName` on the `RimworldXmlDefReference` object in the middle of the call inside Rider, which obviously isn't going to
work for us. This just goes back to the idea of having to rework the ShortName to be the defName and not the defType.