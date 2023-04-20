# Changelog

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
