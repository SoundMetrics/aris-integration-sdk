REM build-vc-programs.cmd

REM This script does the following:
REM     1) Bootstraps (builds) the VCPKG submodule; VCPKG is used to be the
REM        sample and tools programs that rely on Google Protocol Buffers.
REM     2) builds the Visual C++ samples and tools contained in this repository.
REM
REM PRECONDITIONS
REM -------------
REM     - You have recursively cloned this repo; if not, submodules/vcpkg will be empty.
REM     - You have installed Visual Studio 2017 C++ tools. 2015 may also work.
REM     - PowerShell is available on your computer.
REM     - You must run this script from within a Visual Studio Developer Command prompt.
REM     - You must have internet access.
REM
REM POSTCONDITIONS
REM --------------
REM On successful execution of this script the vcpkg tools will be built and
REM the projects built by this script will have been built successfully.

REM ---------------------------------------------------------------------------
REM Bootstrap and build vcpkg

IF "%VisualStudioVersion%" == "" GOTO VSCmdPrompt

pushd submodules\vcpkg
call .\bootstrap-vcpkg.bat
ECHO vcpkg build completed.

REM per https://github.com/Microsoft/vcpkg/issues/645
if not exist downloads mkdir downloads
echo "." > downloads\AlwaysAllowEverything
dir downloads\Always*

vcpkg install protobuf:x86-windows-static
vcpkg install protobuf:x64-windows-static
ECHO Installs completed.

popd

REM ---------------------------------------------------------------------------
REM Build the programs

REM Per https://github.com/NuGet/Home/issues/7386, /t:restore does not support
REM packages.config, and nuget.exe is therefore necessary. However, nuget 3.5.0,
REM as available in vcpkg\downloads, attempts to use the v14 build tools
REM ("Failed to load msbuild Toolset... Microsoft.Build, Version=14.0.0.0").
REM Don't use the old nuget.

REM SET NUGET_EXE=.\submodules\vcpkg\downloads\tools\nuget-4.6.2-windows\nuget.exe
REM 
REM %NUGET_EXE% restore tools\arislog\arislog.sln
REM 
REM REM pushd tools\arislog\arislog
REM REM msbuild /t:restore /p:Configuration="Release" /p:Platform="x86"
REM REM msbuild /t:restore /p:Configuration="Release" /p:Platform="x64"
REM REM popd
REM 
REM msbuild tools\arislog\arislog.sln /t:Rebuild /p:Configuration="Release" /p:Platform="x86"
REM IF %ERRORLEVEL% NEQ 0 EXIT /B 1
REM 
REM msbuild tools\arislog\arislog.sln /t:Rebuild /p:Configuration="Release" /p:Platform="x64"
REM IF %ERRORLEVEL% NEQ 0 EXIT /B 1
REM 
REM 
REM %NUGET_EXE% restore sample-code\vc-using-framestream\vc-using-framestream.sln
REM REM pushd sample-code\vc-using-framestream
REM REM msbuild /t:restore /p:Configuration="Release" /p:Platform="x86"
REM REM msbuild /t:restore /p:Configuration="Release" /p:Platform="x64"
REM REM popd
REM 
REM msbuild sample-code\vc-using-framestream\vc-using-framestream.sln /t:Rebuild /p:Configuration="Release" /p:Platform="x86"
REM IF %ERRORLEVEL% NEQ 0 EXIT /B 1
REM 
REM msbuild sample-code\vc-using-framestream\vc-using-framestream.sln /t:Rebuild /p:Configuration="Release" /p:Platform="x64"
REM IF %ERRORLEVEL% NEQ 0 EXIT /B 1

GOTO End

:VSCmdPrompt
ECHO Couldn't find environment variable 'VisualStudioVersion'.
ECHO Are you at a Visual Studio developer command prompt?
EXIT /B 1

:End
