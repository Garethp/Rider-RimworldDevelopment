import com.jetbrains.rd.generator.gradle.RdGenTask

plugins {
  // Version is configured in gradle.properties
  id("com.jetbrains.rdgen")
  id("org.jetbrains.kotlin.jvm")
  id("org.jetbrains.intellij.platform.module")
}

repositories {
  maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
  maven("https://cache-redirector.jetbrains.com/maven-central")
}

val isMonorepo = rootProject.projectDir != projectDir.parentFile
val pluginRepoRoot: File = projectDir.parentFile

sourceSets {
  main {
    kotlin {
      srcDir(pluginRepoRoot.resolve("protocol/src/kotlin/model"))
    }
  }
}

data class RimworldGeneratorSettings(
  val csOutput: File,
  val ktOutput: File,
  val suffix: String)

val rimworldGeneratorSettings = RimworldGeneratorSettings (
    pluginRepoRoot.resolve("src/dotnet/ReSharperPlugin.RimworldDev/Rider"),
    pluginRepoRoot.resolve("src/rider/main/kotlin/"),
    "",
  )

rdgen {
  verbose = true
  packages = "model"

  generator {
    language = "kotlin"
    transform = "asis"
    root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
    namespace = "com.jetbrains.rider.model"
    directory = rimworldGeneratorSettings.ktOutput.absolutePath
    generatedFileSuffix = rimworldGeneratorSettings.suffix
  }

  generator {
    language = "csharp"
    transform = "reversed"
    root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
    namespace = "JetBrains.Rider.Model"
    directory = rimworldGeneratorSettings.csOutput.absolutePath
    generatedFileSuffix = rimworldGeneratorSettings.suffix
  }
}

tasks.withType<RdGenTask> {
  dependsOn(sourceSets["main"].runtimeClasspath)
  classpath(sourceSets["main"].runtimeClasspath)
}

dependencies {
  if (isMonorepo) {
    implementation(project(":rider-model"))
  } else {
    val rdVersion: String by project
    val rdKotlinVersion: String by project

    implementation("com.jetbrains.rd:rd-gen:$rdVersion")
    implementation("org.jetbrains.kotlin:kotlin-stdlib:$rdKotlinVersion")
    implementation(
      project(
        mapOf(
          "path" to ":",
          "configuration" to "riderModel"
        )
      )
    )
  }
}
