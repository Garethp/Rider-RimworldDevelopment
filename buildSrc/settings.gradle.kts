// Build logic for the root build: Kotlin in src/main/kotlin is compiled before the root build scripts are evaluated and is
// on their classpath (see RiderVersionsTask). Same mirrors as the root settings.gradle.kts.
pluginManagement {
    repositories {
        maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
        maven("https://cache-redirector.jetbrains.com/maven-central")
    }
}

dependencyResolutionManagement {
    repositories {
        maven("https://cache-redirector.jetbrains.com/maven-central")
        maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
    }
}
