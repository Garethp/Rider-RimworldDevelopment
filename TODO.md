## TODO List

 * General features:
   * Support LookupItems for Class="" attributes
   * Support LookupItems for things like <thoughtWorker> or <compClass>
   * If we know a bit of XML should be an enum like Gender, we should be able to check that it's a valid enum value
   * Handle Defs with custom classes instead of Rimworld classes
   * We need to be able to support "LoadAlias", such as "StorageSettings.priority"

 * Error Detection of special types
   * Enum
   * Rot4
   * Boolean
   * IntRange
   * int
   * Vector2
   * Vector3

 * \<li> handling
   * Can we look for, and try to handle, instances where it doesn't have a Type against it's List?

 * XML Autocomplete
   * Make auto completing to other XMLTags look nicer and work faster
   * When linking to other def (`<defaultDuty>DefNameHere</defaultDuty>`) also include defs where the tag is a custom class
     that extends from the def we're looking for
`   
 * Documentation
   * Re-read and document References.RimworldXmlReference
   * If you have an XML file open while Rider is still initializing, that file doesn't get autocompletion. Document that
   
 * Tests
   * It's not a serious project without Tests IMO. Let's at least aim to get one or two unit tests to start with

 * Project Structure
   * Add project references to allow mods to reference other mods
   * When the issue with the project location is fixed, move to 2023.3 to take advantage of that so that we don't need to build the structure ourselves
   * Refactor `RimworldProjectMark::GetModsList` to accept a list of desired ModIds so we can filter them down sooner, which should be more efficient

 * Investigate the possibility of tying a version number to our types in the SymbolScope so that we can don't cross reference between versions
    * Right now we only try to load the latest version of the XML, since we aren't built for loading multiple copies of the same def