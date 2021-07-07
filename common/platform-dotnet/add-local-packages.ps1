'Adding packages...'

$package_version = Get-Content "ver.platform.txt"
'$package_version=' + $package_version

$id_terminator = "." + $package_version
'$id_terminator=' + $id_terminator

$package_folder = ".\built-nuget-packages"
'$package_folder=' + $package_folder
dir $package_folder

# Add the build output nuget packages to our local package source.

$local_nuget_source = '\\build\SMC-Nuget-Packages'

Get-ChildItem $package_folder -Filter *.nupkg |
Foreach-Object {

    '----'

    # $file_name = $_.Name
    # $idx_terminator = $file_name.IndexOf($id_terminator)
    # $package_id = $file_name.Substring(0, $idx_terminator)
    # '$package_id=' + $package_id
    #
    # .\.nuget\nuget delete $package_id "$package_version" -NonInteractive -Source $local_nuget_source

    $package_path = Join-Path -Path $package_folder -ChildPath $_
    '$package_path=' + $package_path

    .\.nuget\nuget add $package_path -Source $local_nuget_source -NonInteractive
}
