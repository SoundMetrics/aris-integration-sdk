Param(
    [string]$build_number = "55555",
    [string]$git_hash = ""
)

'Building packages...'

$dotnet_version = Invoke-Expression "dotnet --version"
'$dotnet_version=' + $dotnet_version

# The build agent has to pass in the git hash as it's not using git locally.
if ($git_hash -eq "") { $git_hash = Invoke-Expression "git rev-parse --short HEAD" }

# Don't let the version number get past 64 characters long (nuget limit)--
# use the short hash of what the build server provides.
$git_hash = $git_hash.Substring(0, 7)
'$git_hash=' + $git_hash

$file_date = Get-Date -Format FileDate

$package_version = (Get-Content "ver.platform.txt") `
                        + ".$build_number" `
                        + "+$file_date" `
                        + "-git-$git_hash"
'$package_version=' + $package_version

$split_version = $package_version.Split("-")[0]
'$split_version=' + $split_version

'$build_number=' + $build_number

$version_with_build_number = $split_version + "." + $build_number
'$version_with_build_number=' + $version_with_build_number

$output_directory = ".\built-nuget-packages"
'$output_directory=' + $output_directory
if (-not (Test-Path $output_directory)) { mkdir $output_directory }

$symbol_directory = $output_directory + "\symbols"
'$symbol_directory=' + $symbol_directory
if (-not (Test-Path $symbol_directory)) { mkdir $symbol_directory }

rm $output_directory\*.nupkg
rm $output_directory\*.snupkg
rm $output_directory\symbols\*.pdb

$dotnetStandardAssemblies = @(
    "SoundMetrics.Aris.AcousticSettings"
    "SoundMetrics.Aris.Comms"
    "SoundMetrics.Aris.FrameHeaderInjection"
    "SoundMetrics.Aris.Messages"
    "SoundMetrics.Aris.ReorderCS"
    "SoundMetrics.Common"
    "SoundMetrics.Data"
    "SoundMetrics.Dataflow"
    "SoundMetrics.NativeMemory"
    "SoundMetrics.Network"
    "SoundMetrics.Scripting"
)

'$dotnetStandardAssemblies: ' + $dotnetStandardAssemblies

Foreach ($el in $dotnetStandardAssemblies) {
    ''
    '---------------------------------------------------------------------'
    "Packing $el"
    '---------------------------------------------------------------------'

    # As of dotnet 2.2.1 the dotnet tools seems to be confused by the presence
    # of a *.?sproj.metaproj file. This file is generated by the MS Build tools
    # when compiling a solution with dependencies. dotnet pack reports that
    # there are multiple project files in this case. So... deleting the meta file.

    del $el/*.?sproj.metaproj

    # Now continue normally.

    # We prevent building again as that causes a mismatch in PDB file against the
    # assemblies produced by the build server. The build server is the canonical,
    # only source for published executables.
    #
    # # If only. I am fed up with these tools, trying to get the symbol packages
    # # published successfully is completely wedged by the fact that I cannot
    # # current prevent this command from rebuilding assemblies. The PDBs end up
    # # not matching checksum on the assembly and cannot be published on nuget.org.
    # # Removing the following:
    # #
    # #   --no-build --include-source -p:SymbolPackageFormat=snupkg
    # #
    dotnet pack -c Release `
                --output ../$output_directory `
                /p:Version=$split_version `
                /p:PackageVersion=$package_version `
                $el

    # Ship off the symbols with the package
    $pdb_name = $el + ".pdb"
    $pdb_src = $el + "\bin\Release\netstandard2.0\" + $pdb_name
    $pdb_dest = $symbol_directory + "\" + $pdb_name

    '$pdb_src: ' + $pdb_src
    '$pdb_dest: ' + $pdb_dest

    cp $pdb_src $pdb_dest
}

# .NET Desktop assemblies

$dotnetDesktopAssemblies = @(
    "SoundMetrics.HID.Windows"
    "SoundMetrics.Scripting.Desktop"
)

'$dotnetDesktopAssemblies: ' + $dotnetDesktopAssemblies

# We're using -NoPackageAnalysis to avoid nuget warning NU5105 (legacy compat).
#
# # Again, wedged, as above. Removing the following:
# #
# #   -Symbols -SymbolPackageFormat snupkg
# #
#.\.nuget\nuget.exe pack -Verbosity detailed `
#                        -NoPackageAnalysis `
#                        -Version $package_version `
#                        -Properties Configuration=Release `
#                        -OutputDirectory $output_directory `
#                        .\SoundMetrics.Scripting.Desktop\SoundMetrics.Scripting.Desktop.fsproj

#cp .\SoundMetrics.Scripting.Desktop\bin\Release\SoundMetrics.Scripting.Desktop.pdb $symbol_directory\SoundMetrics.Scripting.Desktop.pdb

Foreach ($el in $dotnetDesktopAssemblies) {
    ''
    '---------------------------------------------------------------------'
    "Packing $el"
    '---------------------------------------------------------------------'

    .\.nuget\nuget.exe pack -Verbosity detailed `
                            -NoPackageAnalysis `
                            -Version $package_version `
                            -Properties Configuration=Release `
                            -OutputDirectory $output_directory `
                            .\$el

    cp .\$el\bin\Release\$el.pdb $symbol_directory\$el.pdb
}

cp push-packages.cmd $output_directory
ls $output_directory
ls $symbol_directory
