package model.rider

import com.jetbrains.rd.generator.nova.Ext
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rider.model.nova.ide.SolutionModel

object RemodderProtocolModel : Ext(SolutionModel.Solution) {
    init {
        setting(CSharp50Generator.Namespace, "ReSharperPlugin.RdProtocol")
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.plugins.rdprotocol")

        // Remote procedure on backend
        call("decompile", array(string), array(string)).async
    }
}