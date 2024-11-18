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
//    id("org.jetbrains.grammarkit") version grammarKitVersion
    id("me.filippov.gradle.jvm.wrapper") version gradleJvmWrapperVersion
  }

  resolutionStrategy {
    eachPlugin {
      when (requested.id.name) {
        // This required to correctly rd-gen plugin resolution. May be we should switch our naming to match Gradle plugin naming convention.
        "rdgen" -> {
          useModule("com.jetbrains.rd:rd-gen:${rdVersion}")
        }
      }
    }
  }
}
dependencyResolutionManagement {
  repositories {
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
    maven("https://cache-redirector.jetbrains.com/maven-central")
    maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
    maven("https://cache-redirector.jetbrains.com/dl.bintray.com/kotlin/kotlin-eap")
    maven("https://cache-redirector.jetbrains.com/myget.org.rd-snapshots.maven")
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
  }
}

include(":protocol")
