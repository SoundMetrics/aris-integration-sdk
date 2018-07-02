# Print the script name
"Script: $MyInvocation.MyCommand.Name"

# Add the build output nuget packages to our local package source.

$local_nuget_source = '\\build\SMC-Nuget-Packages'

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

Foreach ($pkg in $package_locations) {
    $paths = Resolve-Path $pkg

    Foreach ($path in $paths) {
        .\.nuget\nuget add $path -Source $local_nuget_source -NonInteractive
    }
}
