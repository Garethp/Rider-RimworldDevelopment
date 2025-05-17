@file:Suppress("EXPERIMENTAL_API_USAGE","EXPERIMENTAL_UNSIGNED_LITERALS","PackageDirectoryMismatch","UnusedImport","unused","LocalVariableName","CanBeVal","PropertyName","EnumEntryName","ClassName","ObjectPropertyName","UnnecessaryVariable","SpellCheckingInspection")
package com.jetbrains.rider.plugins.rdprotocol

import com.jetbrains.rd.framework.*
import com.jetbrains.rd.framework.base.*
import com.jetbrains.rd.framework.impl.*

import com.jetbrains.rd.util.lifetime.*
import com.jetbrains.rd.util.reactive.*
import com.jetbrains.rd.util.string.*
import com.jetbrains.rd.util.*
import kotlin.time.Duration
import kotlin.reflect.KClass
import kotlin.jvm.JvmStatic



/**
 * #### Generated from [Model.kt:10]
 */
class RemodderProtocolModel private constructor(
    private val _decompile: RdCall<Array<String>, Array<String>>
) : RdExtBase() {
    //companion
    
    companion object : ISerializersOwner {
        
        override fun registerSerializersCore(serializers: ISerializers)  {
        }
        
        
        
        
        private val __StringArraySerializer = FrameworkMarshallers.String.array()
        
        const val serializationHash = 2555462534816406283L
        
    }
    override val serializersOwner: ISerializersOwner get() = RemodderProtocolModel
    override val serializationHash: Long get() = RemodderProtocolModel.serializationHash
    
    //fields
    val decompile: IRdCall<Array<String>, Array<String>> get() = _decompile
    //methods
    //initializer
    init {
        _decompile.async = true
    }
    
    init {
        bindableChildren.add("decompile" to _decompile)
    }
    
    //secondary constructor
    internal constructor(
    ) : this(
        RdCall<Array<String>, Array<String>>(__StringArraySerializer, __StringArraySerializer)
    )
    
    //equals trait
    //hash code trait
    //pretty print
    override fun print(printer: PrettyPrinter)  {
        printer.println("RemodderProtocolModel (")
        printer.indent {
            print("decompile = "); _decompile.print(printer); println()
        }
        printer.print(")")
    }
    //deepClone
    override fun deepClone(): RemodderProtocolModel   {
        return RemodderProtocolModel(
            _decompile.deepClonePolymorphic()
        )
    }
    //contexts
    //threading
    override val extThreading: ExtThreadingKind get() = ExtThreadingKind.Default
}
val com.jetbrains.rd.ide.model.Solution.remodderProtocolModel get() = getOrCreateExtension("remodderProtocolModel", ::RemodderProtocolModel)

