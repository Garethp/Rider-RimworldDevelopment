# RimworldDev for Rider and ReSharper

![Example Gif](./example.gif)

## Intro

This is my current Work In Progress plugin for JetBrains Rider (and ReSharper?) to bring some intelligence to Rimworld
XML Definitions. Because all of the XML that you write for Rimworld is backed by C#, all the properties you define and the
values that are allowed are just properties in a C# class, we're able to fetch the auto-complete options from C# itself.
Expanding further from that, we can also link the XML back into the C# classes behind them, allowing you to Ctrl+Click
into the definitions on which the XML sits.

## Features so far

 * Enjoy autocompletion of DefTypes and their properties
 * Ctrl+Click into a tag in order to view the Class or Property that the tag is referring to
 * Autocomplete from classes defined in `Class=""` attributes, such as for comps.
 * When referring to other XML Defs (such as `<thought>ThoughtDefName</thought>`), auto complete and link to that defs XML
 * Autocomplete certain values for properties with fixed options (Such as Altitude Layer, boolean and directions)
 * A Rimworld Run Configuration with a Debug option
 * Rimworld Mod support in New Solution
 * Custom Rimworld XML Projects
 * Basic validation for some XML Values

## Roadmap

The major feature for the next version of this plugin (Version 1.1) is custom error reporting for your XML. For example
if you try to use `<facing>NorthSouth</facing>` it will point out that's an invalid option. Likewise, if you pass in 
`<visualSizeRange>2.5</visualSizeRange>` it will tell you that you need to pass in a range of numbers, not a single number.

Additionally, here are some other features I intend to include at some point. These features are not slated for the 1.1
release and are a list of things to do "at some point in time". In no particular order, they are:

 * Autocompleting and referencing classes in `Class=""` attributes in XML Tags
 * Autocompleting and providing references when referring to C# classes in XML Values like in `<thoughtClass>` or `<compClass>`
 * Handle Def tags with custom classes instead of just Rimworld classes (such as `<MyMod.CustomThingDef>`)
 * Supporting Patches


## Quick Architecture For Developers

If you're interested in understanding how this plugin works or want to contribute, this section will probably be of
interest to you.

To begin with, there are two "entry points" into this plugin from the IDE: 
The [`RimworldReferenceProvider`](./src/dotnet/ReSharperPlugin.RimworldDev/References/RimworldReferenceProvider.cs) and
the [`RimworldXMLItemProvider`](./src/dotnet/ReSharperPlugin.RimworldDev/RimworldXMLItemProvider.cs).

The [`RimworldReferenceProvider`](./src/dotnet/ReSharperPlugin.RimworldDev/References/RimworldReferenceProvider.cs) is a class
that tells Rider what references exist for a given XML Tag. In this context, a Reference is what the tag is attached to, so in
our case the C# classes and properties that lie behind the XML. This provider works by collecting the path of the XML tag
into a list (`['Defs', 'ThingDef', 'defName']` as an example) and then walking down that list and matching it up to C# classes
and properties in those classes to come up with a C# Reference, then return that reference (In our example, it would have a reference
to `ThingDef.defName`). This is the entry point if a user tries to Ctrl+Click on an XML Tag.

The second entry point is when a user starts entering a new tag name and we want to display the autocomplete list for
their options. This is the [`RimworldXMLItemProvider`](./src/dotnet/ReSharperPlugin.RimworldDev/RimworldXMLItemProvider.cs).
The ItemProvider gets asked if it can provide any lookup items with it's `IsAvailable` method, and if it return true
it'll then be asked to provide those items by having `AddLookupItems` called. 

Adding lookup items works in much the same way that adding references does: It creates a path list with 
`GetHierarchy` and then parses that list into a given C# Class with `GetContextFromHierarchy`. The next step 
is where it differs from ReferenceProvider: Rather than returning a reference to that class, it fetches all the fields
declared by that class or it's super classes and adds them to it's lookup list. This is the autocomplete list shown
to the user.

A few small extra things are needed to make it all work smoothly. The first is the 
[`RimworldXMLCompletionContextProvider`](./src/dotnet/ReSharperPlugin.RimworldDev/RimworlXMLCompletionContextProvider.cs).
By default the context that we're given in our Lookup Item Provider doesn't give us access to the full solution,
which we need to be able to look up the attached modules to the solution to find the Rimworld C# Module. So we have
our own custom context provider which grabs the solution from the XML File and adds it to the context.

The other is the [`RimworldXmlReference`](./src/dotnet/ReSharperPlugin.RimworldDev/References/RimworldXmlReference.cs)
which is just a class that we have to define to hold our reference from the XML to the C#.

## FAQ

**VS Code Plugin When?**  
I don't use VS Code, so likely not any time soon.