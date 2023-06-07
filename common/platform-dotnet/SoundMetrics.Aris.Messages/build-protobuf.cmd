SETLOCAL

SET SCRIPT=%~nx0
SET PREFIX=%SCRIPT%:

ECHO %PREFIX% ProjectDir=[%1]
ECHO %PREFIX% SolutionDir=[%2]
SHIFT /1
SHIFT /1

SET DEST=.\generated
ECHO %PREFIX% DEST=[%DEST%]
CALL :NORMALIZEPATH %DEST%
ECHO %PREFIX% DEST=[%RETVAL%]
IF NOT EXIST %DEST% MKDIR %DEST%

DEL/Q %DEST%\*.*

ECHO %PREFIX% Generating protobuf output...

SET PROTOC=..\packages\Google.Protobuf.Tools.3.5.1\tools\windows_x86\protoc.exe
ECHO %PREFIX% PROTOC=[%PROTOC%]
CALL :NORMALIZEPATH %PROTOC%
ECHO %PREFIX% PROTOC=[%RETVAL%]
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
