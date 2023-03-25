## TODO List

 * General features:
   * Support LookupItems for Class="" attributes
   * Support LookupItems for things like <thoughtWorker> or <compClass>
   * If we know a bit of XML should be an enum like Gender, we should be able to check that it's a valid enum value
`  * Maybe bring out own Rimworld defs if there's no scope?
   * Handle Defs with custom classes instead of Rimworld classes
`   * Packaging. It shouldn't be too difficult, but I haven't done it yet
   * We need to be able to support "LoadAlias", such as "StorageSettings.priority"

 * Autocomplete of special types
   * Boolean

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
   
` * Refactoring
   * We're fetching symbol scopes a bit all over the place. Let's collect it into a SymbolScope helper class
`   
 * Documentation
   * Re-read and document References.RimworldXmlReference
   * If you have an XML file open while Rider is still initializing, that file doesn't get autocompletion. Document that
   
 * Tests
   * It's not a serious plugin project without Tests IMO. Let's at least aim to get one or two unit tests to start with