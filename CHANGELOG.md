# Changelog

## 2024.2
 * Adds support for [Parent=""] attributes
 * Adds the ability to use Refactoring to Rename a Def, changing all references to that def in XML and C#

## 2024.1
 * Built for 2024.1
 * Adding defName auto-completion in C# for DefDatabase and `[DefOf]`
 * When defining a RunConfig, you can now select a ModList config and Save file to be loaded for just this one launch
 * Added support for `[LoadAlias]`
 * New Mod Template now adds a PublisherPlus configuration file

## 2023.4
 * Added support for Custom Def Classes
 * Implemented XML Def Find Usages, so you can just Right-Click a defName and see all usages of that def in XML
 * Add a new "Generate" menu feature to add props to your XML
 * Fixed an issue where some classes (like `ThingDef`) wouldn't resolve correctly, meaning that references weren't made to them
 * Fixed an issue where some elements in lists weren't resolved correctly


## 2023.3.3
 * Fixed some missed API Changes in Rider 2023.3.3

## 2023.3.2
 * Fix to work with Rider 2023.3

## 2023.3.1
 * Fixed a rare crash when the plugin was completely unable to find Rimworld
 * Fixed automatic Rimworld detection when Rimworld is installed through Steam on the C: drive
 * Fixed some false positives on error checking in XML
 * Added more mod folders to the XML Project that gets added automatically

## 2023.3
 * Rimworld XML will now exist as it's own project
 * Added some new problem analyzers to catch simple typing errors in Rimworld XML
 * Enabled spell checking for XML
 * Add a new Settings Page to allow a user to manually configure their Rimworld location
 * Enables automatic smart completion for XML in Visual Studio, although it's broken entirely in ReSharper 2023.2

## 2023.2.1
 * Fixed an issue for non-Rimworld projects that caused a hang on solution wide analysis

## 2023.2
 * Add a Run Configuration for Rimworld
 * On Windows, running the above configuration in Debug Mode will automatically copy in files from UnityDebug and attach the debugger to the Rimworld process
 * Add a new Rimworld Mod template to the New Solution screen

## 2023.1.1
* Taking some advice from the Jetbrains team, I've pushed the XML Def detection/storage into a `SimpleICache`, massively improving performance

## 2023.1
* The first release of this plugin.
* On top of the features from the Alphas, it also includes automatically detecting and using Rimworlds `Assembly-CSharp.dll` if it's not already part of your project

## 2023.1-Alpha-4
* Added support for auto-completing booleans. Not strictly useful, but why not

## 2023.1-Alpha-3
* Fixed some instances of certain classes not being queried for their properties
* If you've got a project without the Assembly-CSharp.dll referenced (If you're on Linux for example), it should now be
  able to find the DLL file on disk and reference that directly as a fall-back

## 2023.1-Alpha-2
* Minimum Rider version is now 2023.1
* Allowed XML -> XML lookups and References for more projects
* Fixed an issue of some classes not registering as classes
* Fixed an issue of modules that don't have Rimworld directly not being able to find if a class inherits Verse.Def

## 2023.1-Alpha-1
* Initial version
