rootProject.name = "rimworlddev"

pluginManagement {
    val rdVersion: String by settings
    val rdKotlinVersion: String by settings
    val intellijPlatformGradlePluginVersion: String by settings
    val gradleJvmWrapperVersion: String by settings
    val DotnetPluginId: String by settings
    val DotnetSolution: String by settings
    val RiderPluginId: String by settings
    val PluginVersion: String by settings
    val BuildConfiguration: String by settings
    val PublishToken: String by settings
    val ProductVersion: String by settings

    repositories {
        maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
        maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
        maven("https://cache-redirector.jetbrains.com/maven-central")
        maven("https://cache-redirector.jetbrains.com/dl.bintray.com/kotlin/kotlin-eap")
        maven("https://cache-redirector.jetbrains.com/myget.org.rd-snapshots.maven")
        maven("https://cache-redirector.jetbrains.com/intellij-dependencies")

        if (rdVersion == "SNAPSHOT") {
            mavenLocal()
        }
    }

    plugins {
        id("com.jetbrains.rdgen") version rdVersion
        id("org.jetbrains.kotlin.jvm") version rdKotlinVersion
        id("org.jetbrains.intellij.platform") version intellijPlatformGradlePluginVersion
        id("me.filippov.gradle.jvm.wrapper") version gradleJvmWrapperVersion
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