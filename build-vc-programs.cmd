@ECHO OFF
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

pushd tools\arislog
msbuild -t:restore
popd

msbuild tools\arislog\arislog.sln /t:Rebuild /p:Configuration="Release" /p:Platform="x86"
IF %ERRORLEVEL% NEQ 0 EXIT /B 1

msbuild tools\arislog\arislog.sln /t:Rebuild /p:Configuration="Release" /p:Platform="x64"
IF %ERRORLEVEL% NEQ 0 EXIT /B 1


pushd sample-code\vc-using-framestream
msbuild -t:restore
popd

msbuild sample-code\vc-using-framestream\vc-using-framestream.sln /t:Rebuild /p:Configuration="Release" /p:Platform="x86"
IF %ERRORLEVEL% NEQ 0 EXIT /B 1

msbuild sample-code\vc-using-framestream\vc-using-framestream.sln /t:Rebuild /p:Configuration="Release" /p:Platform="x64"
IF %ERRORLEVEL% NEQ 0 EXIT /B 1

GOTO End

:VSCmdPrompt
ECHO Couldn't find environment variable 'VisualStudioVersion'.
ECHO Are you at a Visual Studio developer command prompt?
EXIT /B 1

:End
