import com.jetbrains.rd.generator.gradle.RdGenTask

plugins {
    id("org.jetbrains.kotlin.jvm")
    alias(libs.plugins.rdGen)
}

dependencies {
    implementation(libs.kotlinStdLib)
    implementation(libs.rdGen)
    implementation(
            project(
                    mapOf(
                            "path" to ":",
                            "configuration" to "riderModel"
                    )
            )
    )
}

val DotnetPluginId: String by rootProject
val RiderPluginId: String by rootProject

rdgen {
    val csOutput = File(rootDir, "src/dotnet/${DotnetPluginId}")
    val ktOutput = File(rootDir, "src/rider/main/kotlin/remodder")

    verbose = true
    packages = "model.rider"

    generator {
        language = "kotlin"
        transform = "asis"
        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
        namespace = "com.jetbrains.rider.model"
        directory = "$ktOutput"
    }

    generator {
        language = "csharp"
        transform = "reversed"
        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
        namespace = "JetBrains.Rider.Model"
        directory = "$csOutput"
    }
}

tasks.withType<RdGenTask> {
    // rd-gen's task reads Task.project while executing, which the configuration cache (on in gradle.properties)
    // rejects. Opting this one task out keeps :protocol:rdgen usable without turning the cache off for everything.
    notCompatibleWithConfigurationCache("RdGenTask accesses Task.project at execution time")

    val classPath = sourceSets["main"].runtimeClasspath
    dependsOn(classPath)
    classpath(classPath)
}