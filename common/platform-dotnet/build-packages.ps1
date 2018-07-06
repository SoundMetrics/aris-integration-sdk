Param(
    [string]$build_number = "55555"
)

'Building packages...'

$package_version = Get-Content "ver.platform.txt"
'$package_version=' + $package_version

$split_version = $package_version.Split("-")[0]
'$split_version=' + $split_version

'$build_number=' + $build_number

$version_with_build_number = $split_version + "." + $build_number
'$version_with_build_number=' + $version_with_build_number

$dotnetStandardAssemblies = @(
    "SoundMetrics.Aris.Comms"
    "SoundMetrics.Aris.Config"
    "SoundMetrics.Aris.FrameHeaderInjection"
    "SoundMetrics.Aris.Messages"
    "SoundMetrics.Aris.ReorderCS"
    "SoundMetrics.NativeMemory"
    "SoundMetrics.Scripting"
)

'$assemblies: ' + $assemblies

Foreach ($el in $dotnetStandardAssemblies) {
    ''
    '---------------------------------------------------------------------'
    "Packing $el"
    '---------------------------------------------------------------------'
    dotnet pack -c Release /p:Version=$split_version /p:PackageVersion=$package_version $el
}

# .NET Desktop assemblies

.\.nuget\nuget.exe pack -Verbosity detailed -Version $package_version -Properties Configuration=Release -OutputDirectory .\SoundMetrics.Scripting.Desktop\bin\Release\ .\SoundMetrics.Scripting.Desktop\SoundMetrics.Scripting.Desktop.fsproj
