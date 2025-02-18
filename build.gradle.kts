// Reference https://www.jetbrains.org/intellij/sdk/docs/tutorials/build_system/gradle_guide.html
import com.jetbrains.plugin.structure.base.utils.isFile
import groovy.ant.FileNameFinder
import org.apache.tools.ant.taskdefs.condition.Os
import org.jetbrains.intellij.platform.gradle.Constants
import java.io.ByteArrayOutputStream

plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    id("org.jetbrains.intellij.platform") version "2.2.1"     // https://github.com/JetBrains/gradle-intellij-plugin/releases
    id("com.jetbrains.rdgen") version libs.versions.rdGen    // https://www.myget.org/feed/rd-snapshots/package/maven/com.jetbrains.rd/rd-gen
    id("me.filippov.gradle.jvm.wrapper") version "0.14.0"
}

val isWindows = Os.isFamily(Os.FAMILY_WINDOWS)
extra["isWindows"] = isWindows

val DotnetSolution: String by project
val BuildConfiguration: String by project
val ProductVersion: String by project
val DotnetPluginId: String by project
val RiderPluginId: String by project
val PublishToken: String by project

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
    distributionUrl = "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-all.zip"
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
    kotlinOptions { jvmTarget = "17" }
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

            val directory = "${System.getProperty("user.home")}\\.dotnet"
            if (directory.isNotEmpty()) {
                val files = FileNameFinder().getFileNames(directory, "**/dotnet.exe")
                extra["executable"] = files.get(0)
//                args = mutableListOf("/v:minimal")
            }
        }

        args.add("${DotnetSolution}")
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

        // TODO: See also org.jetbrains.changelog: https://github.com/JetBrains/gradle-changelog-plugin
        val changelogText = file("${rootDir}/CHANGELOG.md").readText()
        val changelogMatches = Regex("(?s)(-.+?)(?=##|$)").findAll(changelogText)
        val changeNotes = changelogMatches.map {
            it.groups[1]!!.value.replace("(?s)- ".toRegex(), "\u2022 ").replace("`", "").replace(",", "%2C").replace(";", "%3B")
        }.take(1).joinToString()

//        val executable: String by setBuildTool.get().extra
//        val arguments = (setBuildTool.get().extra["args"] as List<String>).toMutableList()
//        arguments.add("/t:Pack")
//        arguments.add("/p:PackageOutputPath=${rootDir}/output")
//        arguments.add("/p:PackageReleaseNotes=${changeNotes}")
//        arguments.add("/p:PackageVersion=${version}")
//        exec {
//            executable(executable)
//            args(arguments)
//            workingDir(rootDir)
//        }
    }
}

dependencies {
    intellijPlatform {
        rider(ProductVersion)
        jetbrainsRuntime()
//        instrumentationTools()

//        bundledModule("intellij.rider")
        bundledPlugin("com.intellij.resharper.unity")
        // TODO: add plugins
        // bundledPlugin("uml")
        // bundledPlugin("com.jetbrains.ChooseRuntime:1.0.9")
    }
}

//intellij {
//    type = 'RD'
//    version = "${ProductVersion}"
//    downloadSources = false
//    instrumentCode = false
//    // TODO: add plugins
//     plugins = ["rider-unity"]
//}

tasks.runIde {
    // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
    maxHeapSize = "1500m"

    // Rider's backend doesn't support dynamic plugins. It might be possible to work with auto-reload of the frontend
    // part of a plugin, but there are dangers about keeping plugins in sync
//    autoReloadPlugins = false

    // gradle-intellij-plugin will download the default version of the JBR for the snapshot. Update if required
    // jbrVersion = "jbr_jcef-11_0_6b765.40" // https://confluence.jetbrains.com/display/JBR/Release+notes
}

//rdgen {
//    def modelDir = new File(rootDir, "protocol/src/main/kotlin/model")
//    def csOutput = new File(rootDir, "src/dotnet/${DotnetPluginId}/Rider")
//    def ktOutput = new File(rootDir, "src/rider/main/kotlin/")
//
//    verbose = true
//    classpath {
//        "${rdLibDirectory()}/rider-model.jar"
//    }
//    sources "${modelDir}/rider"
//    hashFolder = "${buildDir}"
//    packages = "model.rider"
//
//    generator {
//        language = "kotlin"
//        transform = "asis"
//        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
//        namespace = "com.jetbrains.rider.model"
//        directory = "$ktOutput"
//    }
//
//    generator {
//        language = "csharp"
//        transform = "reversed"
//        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
//        namespace = "JetBrains.Rider.Model"
//        directory = "$csOutput"
//    }
//}


tasks.prepareSandbox {
    dependsOn(compileDotNet)

    val outputFolder = "${rootDir}/src/dotnet/${DotnetPluginId}/bin/${DotnetPluginId}.Rider/${BuildConfiguration}"
    val dllFiles = listOf(
        "$outputFolder/${DotnetPluginId}.dll",
        "$outputFolder/${DotnetPluginId}.pdb",

        // TODO: add additional assemblies
    )

    dllFiles.forEach({ f ->
        val file = file(f)
        from(file, { into("${rootProject.name}/dotnet") })
    })

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
            args("test","${DotnetSolution}","--logger","GitHubActions")
            workingDir(rootDir)
        }
    }
}

tasks.publishPlugin {
    dependsOn(testDotNet)
    dependsOn(tasks.buildPlugin)
    token.set("${PublishToken}")

    doLast {
        exec {
            executable("dotnet")
            args("nuget","push","output/${DotnetPluginId}.${version}.nupkg","--api-key","${PublishToken}","--source","https://plugins.jetbrains.com")
            workingDir(rootDir)
        }
    }
}

tasks.patchPluginXml  {
    untilBuild.set(provider { null })
}