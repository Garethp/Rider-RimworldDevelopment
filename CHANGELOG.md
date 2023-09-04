# Changelog

## 2023.3
 * Rimworld XML will now exist as it's own project

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
