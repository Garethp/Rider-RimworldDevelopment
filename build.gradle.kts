import com.jetbrains.plugin.structure.base.utils.isFile
import org.apache.tools.ant.taskdefs.condition.Os
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.kotlin.gradle.dsl.JvmTarget

plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    id("org.jetbrains.intellij.platform") version "2.15.0"     // https://github.com/JetBrains/gradle-intellij-plugin/releases
    id("me.filippov.gradle.jvm.wrapper") version "0.16.0"
}


java {
    sourceCompatibility = JavaVersion.VERSION_21

    toolchain {
        languageVersion = JavaLanguageVersion.of(21)
    }
}

val isWindows = Os.isFamily(Os.FAMILY_WINDOWS)
extra["isWindows"] = isWindows

val DotnetSolution: String by project
val BuildConfiguration: String by project
val ProductVersion: String by project
val DotnetPluginId: String by project
val RiderPluginId: String by project
val PublishToken: String by project
val PluginVersion: String by project

allprojects {
    repositories {
        maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
        maven("https://cache-redirector.jetbrains.com/intellij-repository/releases")
        maven("https://cache-redirector.jetbrains.com/intellij-repository/snapshots")
        maven("https://cache-redirector.jetbrains.com/maven-central")
    }
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

apply {
    plugin("kotlin")
}

tasks.wrapper {
    gradleVersion = "9.4.1"
    distributionType = Wrapper.DistributionType.ALL
    distributionUrl =
        "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-all.zip"
}

version = extra["PluginVersion"] as String

sourceSets {
    main {
        java.srcDir("src/rider/main/java")
        kotlin.srcDir("src/rider/main/kotlin")
        resources.srcDir("src/rider/main/resources")
    }
}

tasks.instrumentCode {
    enabled = false
}

tasks.instrumentTestCode {
    enabled = false
}

tasks.compileKotlin {
    compilerOptions { jvmTarget.set(JvmTarget.JVM_21) }
}

val compileDotNet by tasks.registering(Exec::class) {
    executable("dotnet")
    workingDir(rootDir)
    args("build", "--consoleLoggerParameters:ErrorsOnly", "--configuration", "Release")
}

val buildResharperPlugin by tasks.registering(Exec::class) {
    val arguments = mutableListOf<String>()
    arguments.add("msbuild")
    arguments.add(DotnetSolution)

    arguments.add("/t:Restore;Rebuild;Pack")
    arguments.add("/v:minimal")
    arguments.add("/p:PackageOutputPath=\"$rootDir/output\"")
    arguments.add("/p:PackageVersion=$PluginVersion")

    executable("dotnet")
    args(arguments)
    workingDir(rootDir)
}

tasks.buildPlugin {
    doLast {
        copy {
            from("${buildDir}/distributions/${rootProject.name}-${version}.zip")
            into("${rootDir}/output")
        }
    }
}

dependencies {
    intellijPlatform {
        rider(ProductVersion)
        {
            useInstaller = false
        }

        jetbrainsRuntime()

        bundledPlugin("com.intellij.resharper.unity")
        bundledModule("intellij.spellchecker")
    }
}

intellijPlatform {
    pluginVerification {
        freeArgs = listOf("-mute", "TemplateWordInPluginId")

        ides {
//            ide(IntelliJPlatformType.Rider, ProductVersion)
            recommended()
        }
    }
}

tasks.runIde {
    dependsOn(compileDotNet)

    // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
    maxHeapSize = "1500m"

    // Rider's backend doesn't support dynamic plugins. It might be possible to work with auto-reload of the frontend
    // part of a plugin, but there are dangers about keeping plugins in sync
    autoReload = false

    val exampleModSolution = layout.projectDirectory.file("example-mod/AshAndDust.sln").asFile.absolutePath

    argumentProviders += CommandLineArgumentProvider {
        listOf(exampleModSolution)
    }
}

if (!isWindows) {
    tasks.register("copyRiderDlls") {
        notCompatibleWithConfigurationCache("Uses local Rider install")

        // The Rider SDK archive omits certain DLLs that are present in a full Rider installation.
        // Copy the missing Unity plugin DotFiles DLL from the local Rider installation so the sandbox can load it.
        if (!isWindows) {
            val riderInstallCandidates = if (Os.isFamily(Os.FAMILY_MAC)) {
                listOf(file("/Applications/Rider.app/Contents"))
            } else {
                // Linux: check JetBrains Toolbox and common standalone install paths
                val toolboxBase = file("${System.getProperty("user.home")}/.local/share/JetBrains/Toolbox/apps/Rider")
                val toolboxInstalls = if (toolboxBase.exists()) {
                    toolboxBase.walkTopDown()
                        .filter { it.name == "plugins" && it.parentFile?.name?.startsWith("2") == true }
                        .map { it.parentFile }
                        .toList()
                } else emptyList()
                toolboxInstalls + listOf(file("/opt/rider"), file("/usr/share/rider"))
            }

            val missingDotFileDlls = listOf(
                "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.PausePoint.Helper.dll",
                "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.dll",
            )

            val destDir = intellijPlatform.platformPath.resolve("plugins/rider-unity/DotFiles").toFile()
            destDir.mkdirs()

            for (dllName in missingDotFileDlls) {
                val dllRelPath = "plugins/rider-unity/DotFiles/$dllName"
                val srcDll = riderInstallCandidates
                    .map { file("${it}/${dllRelPath}") }
                    .firstOrNull { it.exists() }

                if (srcDll != null) {
                    // Copy into the extracted SDK location (platformPath) — that's where Rider loads plugins from at runtime
                    srcDll.copyTo(file("${destDir}/${srcDll.name}"), overwrite = true)
                }
            }
        }
    }

    tasks.named("prepareSandbox") {
        dependsOn("copyRiderDlls")
    }
}

tasks.prepareSandbox {
    dependsOn(compileDotNet)

    val outputFolder = layout.projectDirectory.dir("/src/dotnet/${DotnetPluginId}/bin/${DotnetPluginId}.Rider/${BuildConfiguration}")

    val dllFiles = listOf(
        "$outputFolder/${DotnetPluginId}.dll",
        "$outputFolder/${DotnetPluginId}.pdb",

        // Not 100% sure why, but we manually need to include these dependencies for Remodder to work
        "$outputFolder/0Harmony.dll",
        "$outputFolder/AsmResolver.dll",
        "$outputFolder/AsmResolver.DotNet.dll",
        "$outputFolder/AsmResolver.PE.dll",
        "$outputFolder/AsmResolver.PE.File.dll",
        "$outputFolder/ICSharpCode.Decompiler.dll"
    ).map { outputFolder.file(it) }

    dllFiles.forEach { provider ->
        from(provider) {
            into("${rootProject.name}/dotnet")
        }
    }

    from(
        layout.projectDirectory.dir("src/dotnet/$DotnetPluginId/ProjectTemplates")
    ) {
        into("${rootProject.name}/ProjectTemplates")
    }

    doLast {
        dllFiles.forEach { f ->
            val file = f.asFile
            if (!file.exists()) {
                throw RuntimeException("File $file does not exist")
            }
        }
    }
}

val testDotNet by tasks.registering(Exec::class) {
    executable("dotnet")
    args("test", DotnetSolution, "--logger", "GitHubActions")
    workingDir(rootDir)
}

tasks.publishPlugin {
    dependsOn(testDotNet)
    dependsOn(tasks.buildPlugin)
    token.set(PublishToken)
}

tasks.patchPluginXml {
    val changelogText = file("${rootDir}/CHANGELOG.md").readText()
        .lines()
        .dropWhile { !it.trim().startsWith("##") }
        .drop(1)
        .takeWhile { !it.trim().startsWith("##") }
        .filter { it.trim().isNotEmpty() }
        .joinToString("\r\n") {
            "<li>${it.trim().replace(Regex("^\\*\\s+?"), "")}</li>"
        }.trim()

    pluginVersion.set(PluginVersion)
    changeNotes.set("<ul>\r\n$changelogText\r\n</ul>");
    untilBuild.set(provider { null })
}

val riderModel: Configuration by configurations.creating {
    isCanBeConsumed = true
    isCanBeResolved = false
}

artifacts {
    add(riderModel.name, provider {
        intellijPlatform.platformPath.resolve("lib/rd/rider-model.jar").also {
            check(it.isFile) {
                "rider-model.jar is not found at $riderModel"
            }
        }
    }) {
        builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    }
}