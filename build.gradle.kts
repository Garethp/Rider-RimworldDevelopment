import groovy.ant.FileNameFinder
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import org.apache.tools.ant.taskdefs.condition.Os
import org.jetbrains.intellij.platform.gradle.IntelliJPlatformType
import java.io.ByteArrayOutputStream
import java.net.URL
import java.nio.file.Files
import java.nio.file.Paths

plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    id("org.jetbrains.intellij.platform") version "2.5.0"     // https://github.com/JetBrains/gradle-intellij-plugin/releases
//    id("com.jetbrains.rdgen") version libs.versions.rdGen    // https://www.myget.org/feed/rd-snapshots/package/maven/com.jetbrains.rd/rd-gen
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

val runVisualStudio by tasks.registering {
    // TODO: Read this from config
    val hiveName = "RimworldDev"
    val sdkVersion = "2025.1"

    // TODO: Use vswhere to determine this
    val vsVersion = "17"

    val solutionLocation = "${rootDir}/src/dotnet/${DotnetPluginId}/${DotnetSolution}"

    // TODO: Check if things are already installed

    val releaseUrl = "https://data.services.jetbrains.com/products/releases?code=RSU&type=eap&type=release&majorVersion=$sdkVersion"
    val releasesInfo = Json.parseToJsonElement(URL(releaseUrl).readText()).jsonObject
    val version = releasesInfo["RSU"]!!.jsonArray[0].jsonObject
    val downloadLink = version["downloads"]!!.jsonObject["windows"]!!.jsonObject["link"]!!
        .toString()
        .trim('"')
        .replace(".exe", ".Checked.exe")

    val fileName = downloadLink.split("/").last()
    val installerLocation = "${rootDir}/build/installer/${fileName}"

    if (file(installerLocation).exists()) {
        println("Using cached installer from $installerLocation")
    } else {
        println("Downloading $fileName installer")
        Files.createDirectories(Paths.get(file(installerLocation).parent))
        URL(downloadLink).openStream().use { input ->
            file(installerLocation).outputStream().use { output ->
                input.copyTo(output)
            }
        }
    }

    println("Installing experimental hive")
    exec {
        executable(installerLocation)
        args("/Silent=true", "/SpecificProductNames=ReSharper", "/Hive=$hiveName", "/VsVersion=$vsVersion.0")
    }
}