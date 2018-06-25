Param(
    [string]$build_number = "55555"
)

# Print the script name
"Script: $MyInvocation.MyCommand.Name"

$package_version = Get-Content "ver.platform.txt"
'$package_version=' + $package_version

$split_version = $package_version.Split("-")[0]
'$split_version=' + $split_version

'$build_number=' + $build_number

$version_with_build_number = $split_version + "." + $build_number
'$version_with_build_number=' + $version_with_build_number

$assemblies = @(
    "SoundMetrics.Aris.Comms"
    "SoundMetrics.Aris.Config"
    "SoundMetrics.Aris.FrameHeaderInjection"
    "SoundMetrics.Aris.Messages"
    "SoundMetrics.Aris.ReorderCS"
    "SoundMetrics.NativeMemory"
)

'$assemblies: ' + $assemblies

Foreach ($el in $assemblies) {
    ''
    '---------------------------------------------------------------------'
    "Packing $el"
    '---------------------------------------------------------------------'
    dotnet pack -c Release /p:Version=$split_version /p:PackageVersion=$package_version $el
}
