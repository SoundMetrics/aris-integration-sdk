Param(
    [string]$build_number = "55555"
)

# Print the script name
"Script: $MyInvocation.MyCommand.Name"

<#
@ECHO OFF
REM ---------------------------------------------------------------------------
REM Builds the file utility nuget packages.
REM ---------------------------------------------------------------------------

// /p:PackageVersion=1.0.13-alpha /p:Version=1.0.13.55555
#>

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
<#
SETLOCAL

SET BUILD_NUMBER=%1
IF "%BUILD_NUMBER%" == "" SET BUILD_NUMBER=55555
ECHO BUILD_NUMBER=%BUILD_NUMBER%

SET /p VERSION=<ver.platform.txt

ECHO Making packages for %VERSION%.%BUILD_NUMBER%

SET PACKCMD=dotnet pack -c Release /p:Version=%VERSION%
ECHO PACKCMD=%PACKCMD%

SET PROJECTS=SoundMetrics.Aris.Comms SoundMetrics.Aris.Config SoundMetrics.Aris.FrameHeaderInjection
SET PROJECTS=%PROJECTS% SoundMetrics.Aris.Messages SoundMetrics.Aris.ReorderCS SoundMetrics.NativeMemory

FOR %%p in (%PROJECTS%) do (
    ECHO ---------------------------------------------------------------------
    ECHO %%p
    ECHO ---------------------------------------------------------------------
    %PACKCMD% %%p
)

ENDLOCAL
#>
