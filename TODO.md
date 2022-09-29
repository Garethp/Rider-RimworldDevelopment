## TODO List

 * General features:
   * Support LookupItems for Class="" attributes
   * Support LookupItems for things like <thoughtWorker> or <compClass>
   * If we know a bit of XML should be an enum like Gender, we should be able to autocomplete and highlight in red
   * Don't fail it there's no Rimworld Scope
   * Maybe bring out own Rimworld defs if there's no scope?
   * Handle Defs with custom classes instead of Rimworld classes
   * Packaging. It shouldn't be too difficult, but I haven't done it yet

 * \<li> handling
   * Can we look for, and try to handle, instances where it doesn't have a Type against it's List?
   * Reference **defNames**

 * XML Autocomplete
   * Auto complete other XML Defs
   * Link to those other XML Defs
   
 * Refactoring
   * We're fetching symbol scopes a bit all over the place. Let's collect it into a SymbolScope helper class
   
 * Documentation
   * Re-read and document References.RimworldXmlReference
   * If you have an XML file open while Rider is still initializing, that file doesn't get autocompletion. Document that
   
 * Tests
   * It's not a serious plugin project without Tests IMO. Let's at least aim to get one or two unit tests to start with