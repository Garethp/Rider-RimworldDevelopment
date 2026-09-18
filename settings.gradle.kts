rootProject.name = "rimworlddev"

pluginManagement {
    repositories {
        maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
        maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
        maven("https://cache-redirector.jetbrains.com/maven-central")
        maven("https://cache-redirector.jetbrains.com/dl.bintray.com/kotlin/kotlin-eap")
        maven("https://cache-redirector.jetbrains.com/myget.org.rd-snapshots.maven")
        maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
    }

    resolutionStrategy {
        eachPlugin {
            // Gradle has to map a plugin dependency to Maven coordinates - '{groupId}:{artifactId}:{version}'. It tries
            // to do use '{plugin.id}:{plugin.id}.gradle.plugin:version'.
            // This doesn't work for rdgen, so we provide some help
            if (requested.id.id == "com.jetbrains.rdgen") {
                useModule("com.jetbrains.rd:rd-gen:${requested.version}")
            }
        }
    }
}
dependencyResolutionManagement {
    repositories {
        maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
        maven("https://cache-redirector.jetbrains.com/intellij-repository/releases")
        maven("https://cache-redirector.jetbrains.com/intellij-repository/snapshots")
        maven("https://cache-redirector.jetbrains.com/maven-central")
        maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
        maven("https://cache-redirector.jetbrains.com/dl.bintray.com/kotlin/kotlin-eap")
        maven("https://cache-redirector.jetbrains.com/myget.org.rd-snapshots.maven")
    }
}

include(":protocol")