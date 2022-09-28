## TODO List

 * General features:
   * Support LookupItems for Class="" attributes
   * Support LookupItems for things like <thoughtWorker> or <compClass>
   * If we know a bit of XML should be an enum like Gender, we should be able to autocomplete and highlight in red
   * Don't fail it there's no Rimworld Scope
   * Maybe bring out own Rimworld defs if there's no scope?
   * Handle Defs with custom classes instead of Rimworld classes
   * Packaging. It shouldn't be too difficult, but I haven't done it yet
   
 * Bugs
   * I don't know what's causing it, but I occasionally get an error in Rider when *using* this plugin, but it doesn't 
     appear to cause any kidn of real error so I've been ignoring it. Next time I see it, I'll post the details here and
     I need to investigate it so that I can add some polish before releasing this

 * \<li> handling
   * Can we look for, and try to handle, instances where it doesn't have a Type against it's List?
   * Reference **defNames**

 * Field list
   * Should we be checking that the field is public?

 * XML Autocomplete
   * Auto complete other XML Defs
   * Link to those other XML Defs
   
 * Refactoring
   * We're fetching symbol scopes a bit all over the place. Let's collect it into a SymbolScope helper class
   
 * Documentation
   * Add a **useful** README that shows how the different classes work together
   * Re-read and document References.RimworldXmlReference
   * Document the requirements for running the plugin in the first place (Having the Rimworld DLL as a C# Reference)
   * If you have an XML file open while Rider is still initializing, that file doesn't get autocompletion. Document that
   * Add some gifs showing this plugin in work
   * Add a Roadmap