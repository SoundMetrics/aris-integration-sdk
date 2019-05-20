'Adding packages...'

$package_version = Get-Content "ver.platform.txt"
'$package_version=' + $package_version

$package_folder = ".\built-nuget-packages"
'$package_folder=' + $package_folder

# Add the build output nuget packages to our local package source.

$local_nuget_source = '\\build\SMC-Nuget-Packages'

Get-ChildItem $package_folder -Filter *.nupkg |
Foreach-Object {

    $package_path = $_.FullName
    '$package_path=' + $package_path

    .\.nuget\nuget delete "$package_name" "$package_version" -NonInteractive -Source $local_nuget_source
    .\.nuget\nuget add $package_path -Source $local_nuget_source -NonInteractive
}
