#!/bin/sh
set -e
# This script builds the plugin on Linux

export Version="2024.1-EAP"

cd "src/dotnet/ReSharperPlugin.RimworldDev"
FrameworkPathOverride=$(dirname $(which dotnet))/../../../../lib/dotnet/sdk/7.0.202/ dotnet build  ReSharperPlugin.RimworldDev.csproj /property:Configuration=Release
FrameworkPathOverride=$(dirname $(which dotnet))/../../../../lib/dotnet/sdk/7.0.202/ dotnet build  ReSharperPlugin.RimworldDev.Rider.csproj /property:Configuration=Release

cd -

./gradlew :buildPlugin -x compileDotNet