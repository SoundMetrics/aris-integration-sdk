SETLOCAL

SET SCRIPT=%~nx0
SET PREFIX=%SCRIPT%:

SET PROJECT_DIR=%1
SHIFT /1
ECHO %PREFIX% PROJECT_DIR=[%PROJECT_DIR%]

SET DEST=.\generated
ECHO %PREFIX% DEST=[%DEST%]
CALL :NORMALIZEPATH %DEST%
ECHO %PREFIX% DEST=[%RETVAL%]
IF NOT EXIST %DEST% MKDIR %DEST%

DEL/Q %DEST%\*.*

ECHO %PREFIX% Getting Google.Protobuf.Tools

SET LOCAL_NUGET=%PROJECT_DIR%local_nuget
ECHO LOCAL_NUGET=[%LOCAL_NUGET%]
IF NOT EXIST %LOCAL_NUGET% MKDIR %LOCAL_NUGET%

SET DESIRED_PROTOC_VERSION=3.23.2
%PROJECT_DIR%\..\.nuget\nuget.exe install Google.Protobuf.Tools -OutputDirectory %LOCAL_NUGET% -Version %DESIRED_PROTOC_VERSION%

ECHO %PREFIX% Generating protobuf output...

REM SET PROTOC=..\%PROJECT_DIR%\packages\Google.Protobuf.Tools.%PROTOBUF_TOOLS_VER%\tools\windows_x86\protoc.exe
SET PROTOC=%PROJECT_DIR%\local_nuget\Google.Protobuf.Tools.3.23.2\tools\windows_x86\protoc.exe

ECHO %PREFIX% PROTOC=[%PROTOC%]
CALL :NORMALIZEPATH %PROTOC%
ECHO %PREFIX% PROTOC=[%RETVAL%]

DIR %PROTOC%
%PROTOC% --version

set PGEN=%PROTOC% --csharp_out=%DEST% --error_format=msvs %1 %2
ECHO %PREFIX% PGEN=[%PGEN%]

set PBARIS=..\..\protobuf
ECHO %PREFIX% PBARIS=[%PBARIS%]
CALL :NORMALIZEPATH %PBARIS%
ECHO %PREFIX% PBARIS=[%RETVAL%]

ECHO %PREFIX% ### first=[%PBARIS%\availability.proto]
CALL :NORMALIZEPATH %PBARIS%\availability.proto
ECHO %PREFIX% ### first=[%RETVAL%]

%PGEN% --proto_path=%PBARIS% %PBARIS%\availability.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\commands.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\frame_stream.proto

%PGEN% --proto_path=%PBARIS% %PBARIS%\command_module_beacon.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\defender_availability.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\defender_settings.proto

dir %DEST%

ENDLOCAL

:: ========== FUNCTIONS ==========
EXIT /B

:NORMALIZEPATH
  SET RETVAL=%~f1
  EXIT /B
