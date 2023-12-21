## TODO List

 * General features:
   * Support LookupItems for Class="" attributes
   * Support LookupItems for things like <thoughtWorker> or <compClass>
   * If we know a bit of XML should be an enum like Gender, we should be able to check that it's a valid enum value
   * Handle Defs with custom classes instead of Rimworld classes
   * We need to be able to support "LoadAlias", such as "StorageSettings.priority"

 * \<li> handling
   * Can we look for, and try to handle, instances where it doesn't have a Type against it's List?

 * XML Autocomplete
   * Make auto completing to other XMLTags look nicer and work faster
   * When linking to other def (`<defaultDuty>DefNameHere</defaultDuty>`) also include defs where the tag is a custom class
     that extends from the def we're looking for
`   
 * Documentation
   * Re-read and document References.RimworldXmlReference
   
 * Tests
   * It's not a serious project without Tests IMO. Let's at least aim to get one or two unit tests to start with

 * Project Structure
   * When the issue with the project location is fixed, move to 2023.3 to take advantage of that so that we don't need to build the structure ourselves

 * Investigate the possibility of tying a version number to our types in the SymbolScope so that we can don't cross reference between versions
    * Right now we only try to load the latest version of the XML, since we aren't built for loading multiple copies of the same def

 * Refactor all reading of `About.xml` into a single class that holds that information for that file
 * Look at refactoring RimworldSymbolScope to see if we can make it cleaner
 * Refactor the fetching of classes to a central location. We're all over the place, using different scopes from `ScopeHelper`
   when we really should just let `ScopeHelper` deal with it all internally as well as the scope swapping

 * Generate
   * Order based on number of references 
   * Auto-tick what's required
