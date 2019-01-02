@ECHO OFF

if "%NUGET_API_KEY%" == "" goto NO_KEY
if "%1" == "" goto MISSING_INPUT_FOLDER

for %%f in (%1\*.nupkg %1\*.snupkg) do dotnet nuget push %%f --api-key %NUGET_API_KEY% --source https://nuget.org/

GOTO END

:NO_KEY
ECHO Couldn't find 'NUGET_API_KEY' environment variable.
EXIT /B 1

:MISSING_INPUT_FOLDER

ECHO You must supply the folder in which the nuget packages live.
EXIT /B 2

:END
