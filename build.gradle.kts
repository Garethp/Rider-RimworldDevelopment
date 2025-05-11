import com.jetbrains.plugin.structure.base.utils.isFile
import groovy.ant.FileNameFinder
import org.apache.tools.ant.taskdefs.condition.Os
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.intellij.platform.gradle.IntelliJPlatformType
import java.io.ByteArrayOutputStream

plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    id("org.jetbrains.intellij.platform") version "2.5.0"     // https://github.com/JetBrains/gradle-intellij-plugin/releases
    id("me.filippov.gradle.jvm.wrapper") version "0.14.0"
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
    gradleVersion = "8.8"
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
    kotlinOptions { jvmTarget = "21" }
}

val setBuildTool by tasks.registering {
    doLast {
        extra["executable"] = "dotnet"
        var args = mutableListOf("msbuild")

        if (isWindows) {
            val stdout = ByteArrayOutputStream()
            exec {
                executable("${rootDir}\\tools\\vswhere.exe")
                args("-latest", "-property", "installationPath", "-products", "*")
                standardOutput = stdout
                workingDir(rootDir)
            }

            val vsWhereDirectory = stdout.toString().trim()
            if (vsWhereDirectory.isNotEmpty()) {
                val files = FileNameFinder().getFileNames("${vsWhereDirectory}\\MSBuild", "**/MSBuild.exe")
                extra["executable"] = files.get(0)
                args = mutableListOf("/v:minimal")
            } else {
                val directory = "${System.getProperty("user.home")}\\AppData\\Local\\Programs\\Rider\\tools\\MSBuild"
                if (directory.isNotEmpty()) {
                    val files = FileNameFinder().getFileNames(directory, "Current/Bin/MSBuild.exe")
                    extra["executable"] = files.get(0)
                    args = mutableListOf("/v:minimal")
                }
            }
        }

        args.add(DotnetSolution)
        args.add("/p:Configuration=${BuildConfiguration}")
        args.add("/p:HostFullIdentifier=")
        extra["args"] = args
    }
}

val compileDotNet by tasks.registering {
    dependsOn(setBuildTool)
    doLast {
        val executable: String by setBuildTool.get().extra
        val arguments = (setBuildTool.get().extra["args"] as List<String>).toMutableList()
        arguments.add("/t:Restore;Rebuild")
        exec {
            executable(executable)
            args(arguments)
            workingDir(rootDir)
        }
    }
}

val buildResharperPlugin by tasks.registering {
    dependsOn(setBuildTool)
    doLast {
        val executable: String by setBuildTool.get().extra
        val arguments = (setBuildTool.get().extra["args"] as List<String>).toMutableList()
        arguments.add("/t:Restore;Rebuild;Pack")
        arguments.add("/v:minimal")
        arguments.add("/p:PackageOutputPath=\"$rootDir/output\"")
        arguments.add("/p:PackageVersion=$PluginVersion")
        exec {
            executable(executable)
            args(arguments)
            workingDir(rootDir)
        }
    }
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
        rider(ProductVersion, useInstaller = false)
        jetbrainsRuntime()

        bundledPlugin("com.intellij.resharper.unity")
    }
}

intellijPlatform {
    pluginVerification {
        freeArgs = listOf("-mute", "TemplateWordInPluginId")

        ides {
            ide(IntelliJPlatformType.Rider, ProductVersion)
            recommended()
        }
    }
}

tasks.runIde {
    // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
    maxHeapSize = "1500m"

    // Rider's backend doesn't support dynamic plugins. It might be possible to work with auto-reload of the frontend
    // part of a plugin, but there are dangers about keeping plugins in sync
    autoReload = false
}

tasks.prepareSandbox {
    dependsOn(compileDotNet)

    val outputFolder = "${rootDir}/src/dotnet/${DotnetPluginId}/bin/${DotnetPluginId}.Rider/${BuildConfiguration}"
    val dllFiles = listOf(
        "$outputFolder/${DotnetPluginId}.dll",
        "$outputFolder/${DotnetPluginId}.pdb",
    )

    dllFiles.forEach({ f ->
        val file = file(f)
        from(file, { into("${rootProject.name}/dotnet") })
    })

    from("${rootDir}/src/dotnet/${DotnetPluginId}/projectTemplates", { into("${rootProject.name}/projectTemplates") })

    doLast {
        dllFiles.forEach({ f ->
            val file = file(f)
            if (!file.exists()) throw RuntimeException("File ${file} does not exist")
        })
    }
}

val testDotNet by tasks.registering {
    doLast {
        exec {
            executable("dotnet")
            args("test", DotnetSolution, "--logger", "GitHubActions")
            workingDir(rootDir)
        }
    }
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