import com.jetbrains.rd.generator.gradle.RdGenTask

plugins {
  // Version is configured in gradle.properties
  id("com.jetbrains.rdgen")
  id("org.jetbrains.kotlin.jvm")
//  id("org.jetbrains.intellij.platform.module")
}


val isMonorepo = rootProject.projectDir != projectDir.parentFile
val pluginRepoRoot: File = projectDir.parentFile

sourceSets {
  main {
    kotlin {
      srcDir(pluginRepoRoot.resolve("protocol/src/main/kotlin/model/rider"))
    }
    java {
      rootProject.tasks.named("resolvePlatformModel").get().outputs.files.singleFile
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
  classpath(rootProject.tasks.named("resolvePlatformModel").get().outputs.files.singleFile)

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
    val rdVersion: String by project
    val rdKotlinVersion: String by project
//    val riderModelJar: String by rootProject.ext


  implementation("com.jetbrains.rd:rd-gen:$rdVersion")
    implementation("org.jetbrains.kotlin:kotlin-stdlib:$rdKotlinVersion")
  println(rootProject.tasks.named("resolvePlatformModel").get().outputs.files.singleFile)
    implementation(files(rootProject.tasks.named("resolvePlatformModel").get().outputs.files.singleFile))
    implementation(
      project(
        mapOf(
          "path" to ":",
          "configuration" to "riderModel"
        )
      )
    )

}
