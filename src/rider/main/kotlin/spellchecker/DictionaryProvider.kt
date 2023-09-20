package RimworldDev.Rider.spellchecker

import com.intellij.spellchecker.BundledDictionaryProvider

class DictionaryProvider: BundledDictionaryProvider {
    override fun getBundledDictionaries(): Array<String> = arrayOf("/spellchecker/rimworld.dic")
}