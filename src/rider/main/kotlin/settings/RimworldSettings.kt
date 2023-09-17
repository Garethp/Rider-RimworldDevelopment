package RimworldDev.Rider.settings

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class RimworldSettings: SimpleOptionsPage("Rimworld", "RimworldOptiosnPage") {
    override fun getId(): String {
        return "preferences.build.rimworldPlugin"
    }
}