'Adding packages...'

$package_version = Get-Content "ver.platform.txt"
'$package_version=' + $package_version

$output_directory = ".\built-nuget-packages"
'$output_directory=' + $output_directory

# Add the build output nuget packages to our local package source.

$local_nuget_source = '\\build\SMC-Nuget-Packages'

$package_names = @(
    "SoundMetrics.Aris.Comms"
    "SoundMetrics.Aris.Config"
    "SoundMetrics.Aris.FrameHeaderInjection"
    "SoundMetrics.Aris.Messages"
    "SoundMetrics.Aris.ReorderCS"
    "SoundMetrics.Common"
    "SoundMetrics.NativeMemory"
    "SoundMetrics.Network"
    "SoundMetrics.Scripting"
    "SoundMetrics.Scripting.Desktop"
)

$package_locations = @(
    '.\SoundMetrics.Aris.Comms\bin\Release\SoundMetrics.Aris.Comms*.nupkg'
    '.\SoundMetrics.Aris.Config\bin\Release\SoundMetrics.Aris.Config*.nupkg'
    '.\SoundMetrics.Aris.Messages\bin\Release\SoundMetrics.Aris.Messages*.nupkg'
    '.\SoundMetrics.Aris.FrameHeaderInjection\bin\Release\SoundMetrics.Aris.FrameHeaderInjection*.nupkg'
    '.\SoundMetrics.Aris.ReorderCS\bin\Release\SoundMetrics.Aris.ReorderCS*.nupkg'
    '.\SoundMetrics.NativeMemory\bin\Release\SoundMetrics.NativeMemory*.nupkg'
    '.\SoundMetrics.Scripting\bin\Release\SoundMetrics.Scripting*.nupkg'
    '.\SoundMetrics.Scripting.Desktop\bin\Release\SoundMetrics.Scripting.Desktop*.nupkg'
)

Foreach ($package_name in $package_names) {
    $package_path = ".\$package_name\bin\Release\$package_name.$package_version.nupkg"

    "----------------------------------------------------------------------------------------------------------------"
    $package_path
    "----------------------------------------------------------------------------------------------------------------"
    ""

    .\.nuget\nuget delete "$package_name" "$package_version" -NonInteractive -Source \\build\SMC-Nuget-Packages
    .\.nuget\nuget add $package_path -Source $local_nuget_source -NonInteractive
}
