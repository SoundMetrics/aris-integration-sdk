REM @ECHO OFF
REM ---------------------------------------------------------------------------
REM Builds packages for the ARIS protocols
REM ---------------------------------------------------------------------------

SETLOCAL

REM ---------------------------------------------------------------------------
REM Check usage
REM ---------------------------------------------------------------------------

IF "%1" == "" GOTO Usage


SET VERSION_STRING=%1

ECHO VERSION_STRING=%VERSION_STRING%


REM ---------------------------------------------------------------------------
REM Do the work
REM ---------------------------------------------------------------------------


SET NUGETPKGDIR=.\NugetPkg
SET NUGET=%NUGETPKGDIR%\nuget.exe

%NUGET% pack -Version "%VERSION_STRING%" %NUGETPKGDIR%\Aris.Proto.Native.nuspec  -OutputDirectory %NUGETPKGDIR%

ENDLOCAL

GOTO :EOF

REM ---------------------------------------------------------------------------
REM Show usage
REM ---------------------------------------------------------------------------

:Usage
ECHO USAGE: build_packages.cmd (version)
ECHO          e.g., build_packages 1.2.1.8127

GOTO :EOF
