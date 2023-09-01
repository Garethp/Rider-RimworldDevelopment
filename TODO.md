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

 * Adding custom project structure
   * Kotlin
     * RiderProjectTypesProvider
     * RiderProjectType
   * ReSharper
     * ProjectHost
       * WebSiteProjectHost
       * Implement Reload & Define Descriptors
     * Project Reference Descriptors
     * ProjectMark
       * The entry in SolutionFile
       * VirtualProjectMark
       * It's the entry point to our project model
 * Convert the RimworldXmlProjectMark into a VirtualProjectMark
 * Set the name of the XML Project to be loaded from the `About.xml`
 * Refactor `RimworldXmlProjectHost` to pull `Build`, `BuildInternal` and `Filter` into a `RimworldXmlProjectStructureBuilder` class
 * When the issue with the project location is fixed, move to 2023.3 to take advantage of that so that we don't need to build the structure ourselves