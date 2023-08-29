package RimworldDev.Rider.XmlProject

import com.jetbrains.rider.projectView.projectTypes.RiderProjectType
import com.jetbrains.rider.projectView.projectTypes.RiderProjectTypesProvider

class RimworldProjectType: RiderProjectTypesProvider {
    companion object {
        val RimworldProjectType = RiderProjectType("xml", "{F2A71F9B-5D33-465A-A702-920D77279781}")
    }

    override fun getProjectType(): List<RiderProjectType> = listOf(RimworldProjectType)
}