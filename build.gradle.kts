import com.jetbrains.plugin.structure.base.utils.isFile
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.intellij.platform.gradle.tasks.PrepareSandboxTask
import org.jetbrains.intellij.platform.gradle.tasks.RunIdeTask
import org.jetbrains.kotlin.daemon.common.toHexString
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile
import kotlin.io.path.absolutePathString

plugins {
  // Version is configured in gradle.properties
  id("me.filippov.gradle.jvm.wrapper")
  id("org.jetbrains.intellij.platform")
  kotlin("jvm")
}

apply {
  plugin("kotlin")
}

repositories {
  maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
  maven("https://cache-redirector.jetbrains.com/intellij-repository/releases")
  maven("https://cache-redirector.jetbrains.com/intellij-repository/snapshots")
  maven("https://cache-redirector.jetbrains.com/maven-central")
  intellijPlatform {
    defaultRepositories()
    jetbrainsRuntime()
  }
}

val riderBaseVersion: String by project
val buildCounter = ext.properties["build.number"] ?: "9999"
version = "$riderBaseVersion.$buildCounter"

dependencies {
  intellijPlatform {
    val dir = file("build/rider")
    if (dir.exists()) {
      logger.lifecycle("*** Using Rider SDK from local path " + dir.absolutePath)
      local(dir)
    } else {
      logger.lifecycle("*** Using Rider SDK from intellij-snapshots repository")
      rider("2024.3")
    }
    jetbrainsRuntime()
//    bundledPlugin("JavaScript")
//    bundledPlugin("com.intellij.css")
//    bundledPlugin("com.intellij.database")
//    bundledPlugin("org.intellij.intelliLang")
//    bundledPlugin("org.jetbrains.plugins.textmate")
    bundledPlugin("rider.intellij.plugin.appender")
    bundledPlugin("com.intellij.resharper.unity")
    instrumentationTools()
  }
}

val isMonorepo = rootProject.projectDir != projectDir
val repoRoot: File = projectDir
val pluginPath = repoRoot.resolve("src/dotnet")

val buildConfiguration = ext.properties["BuildConfiguration"] ?: "Debug"
val primaryTargetFramework = "net472"
val outputRelativePath = "bin/$buildConfiguration/$primaryTargetFramework"
val ktOutputRelativePath = "src/rider/main/kotlin/"

if (!isMonorepo) {
  sourceSets.getByName("main") {
    java {
      srcDir(repoRoot.resolve("src/rider/main/java"))
    }
    kotlin {
      srcDir(repoRoot.resolve("src/rider/main/kotlin"))
    }
  }
}

val nugetConfigPath = File(repoRoot, "NuGet.Config")
val dotNetSdkPathPropsPath = File("build", "DotNetSdkPath.generated.props")

val targetsGroup = "Rider-RimworldDevelopment"

fun File.writeTextIfChanged(content: String) {
  val bytes = content.toByteArray()

  if (!exists() || readBytes().toHexString() != bytes.toHexString()) {
    println("Writing $path")
    writeBytes(bytes)
  }
}

val riderModel: Configuration by configurations.creating {
  isCanBeConsumed = true
  isCanBeResolved = false
}

val platformLibConfiguration: Configuration by configurations.creating {
  isCanBeConsumed = true
  isCanBeResolved = false
}

val platformLibFile = project.layout.buildDirectory.file("platform.lib.txt")
val resolvePlatformLibPath = tasks.create("resolvePlatformLibPath") {
  dependsOn(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
  outputs.file(platformLibFile)
  doLast {
    platformLibFile.get().asFile.writeTextIfChanged(intellijPlatform.platformPath.resolve("lib").absolutePathString())
  }
}

artifacts {
  add(riderModel.name, provider {
    val sdkRoot = intellijPlatform.platformPath
    sdkRoot.resolve("lib/rd.jar").also {
      check(it.isFile) {
        "rider-model.jar is not found at $riderModel in $sdkRoot"
      }
    }
  }) {
    builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
  }

  add(platformLibConfiguration.name, provider { resolvePlatformLibPath.outputs.files.singleFile }) {
    builtBy(resolvePlatformLibPath)
  }
}

tasks {
//  val generateDisabledPluginsTxt by registering {
//    val out = layout.buildDirectory.file("disabled_plugins.txt")
//    outputs.file(out)
//    doLast {
//      file(out).writeText(
//        """
//          com.intellij.ml.llm
//          com.intellij.swagger
//        """.trimIndent()
//      )
//    }
//  }

  withType<PrepareSandboxTask> {
    dependsOn(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    var outputFolder = "${pluginPath}/ReSharperPlugin.RimworldDev/bin/ReSharperPlugin.RimworldDev.Rider/Release"
    var files = listOf(
      "$outputFolder/ReSharperPlugin.RimworldDev.dll",
      "$outputFolder/ReSharperPlugin.RimworldDev.pdb"
    )

    fun moveToPlugin(files: List<String>, destinationFolder: String) {
      files.forEach {
        from(it) { into("${intellijPlatform.projectName.get()}/$destinationFolder") }
      }
    }

    moveToPlugin(files, "dotnet")
    moveToPlugin(listOf("${pluginPath}/ReSharperPlugin.RimworldDev/projectTemplates"), "projectTemplates")

    doLast {
      fun validateFiles(files: List<String>, destinationFolder: String) {
        files.forEach {
          val file = file(it)
          if (!file.exists()) throw RuntimeException("File $file does not exist")
          logger.warn("$name: ${file.name} -> $destinationDir/${intellijPlatform.projectName.get()}/$destinationFolder")
        }
      }
    }
  }

  // Initially introduced in:
  // https://github.com/JetBrains/ForTea/blob/master/Frontend/build.gradle.kts
  withType<RunIdeTask> {
    // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
    maxHeapSize = "1500m"
  }

  withType<KotlinCompile> {
    kotlinOptions.jvmTarget = "17"
    dependsOn(":protocol:rdgen")
  }

//  val parserTest by register<Test>("parserTest") {
//    useJUnitPlatform()
//  }
//
//  named<Test>("test") {
//    dependsOn(parserTest)
//    useTestNG {
//      groupByInstances = true
//    }
//  }
//
//  withType<Test> {
//    testLogging {
//      showStandardStreams = true
//      exceptionFormat = TestExceptionFormat.FULL
//    }
//    val rerunSuccessfulTests = false
//    outputs.upToDateWhen { !rerunSuccessfulTests }
//    ignoreFailures = true
//  }

  val writeDotNetSdkPathProps = create("writeDotNetSdkPathProps") {
    dependsOn(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    group = targetsGroup
    inputs.property("platformPath") { intellijPlatform.platformPath.toString() }
    outputs.file(dotNetSdkPathPropsPath)
    doLast {
      dotNetSdkPathPropsPath.writeTextIfChanged(
        """<Project>
  <PropertyGroup>
    <DotNetSdkPath>${intellijPlatform.platformPath.resolve("lib").resolve("DotNetSdkForRdPlugins").absolutePathString()}</DotNetSdkPath>
  </PropertyGroup>
</Project>
"""
      )
    }

    getByName("buildSearchableOptions") {
      enabled = buildConfiguration == "Release"
    }
  }

  val writeNuGetConfig = create("writeNuGetConfig") {
    dependsOn(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    group = targetsGroup
    inputs.property("platformPath") { intellijPlatform.platformPath.toString() }
    outputs.file(nugetConfigPath)
    doLast {
      nugetConfigPath.writeTextIfChanged(
        """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="resharper-sdk" value="${intellijPlatform.platformPath.resolve("lib").resolve("DotNetSdkForRdPlugins").absolutePathString()}" />
  </packageSources>
</configuration>
"""
      )
    }
  }

  named("assemble") {
    doLast {
      logger.lifecycle("Plugin version: $version")
    }
  }

  val prepare = create("prepare") {
    group = targetsGroup
    dependsOn(":protocol:rdgen", writeNuGetConfig, writeDotNetSdkPathProps)
  }

  val buildReSharperPlugin by registering(Exec::class) {
    group = targetsGroup
    dependsOn(prepare)

    executable = "dotnet"
    args("build", "ReSharperPlugin.RimworldDev.sln")
  }

  wrapper {
    gradleVersion = "8.7"
    distributionUrl = "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-bin.zip"
  }

  defaultTasks(prepare)
}
