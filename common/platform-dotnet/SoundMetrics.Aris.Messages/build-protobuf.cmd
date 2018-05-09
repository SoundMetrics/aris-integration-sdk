SETLOCAL

SET DEST=.\generated
IF NOT EXIST %DEST% MKDIR %DEST%

DEL/Q %DEST%\*.*

ECHO Generating protobuf output...

SET PROTOC=..\packages\Google.Protobuf.Tools.3.5.1\tools\windows_x86\protoc.exe
%PROTOC% --version

set PGEN=%PROTOC% --csharp_out=%DEST% --error_format=msvs %1 %2

set PBARIS=..\..\protobuf

%PGEN% --proto_path=%PBARIS% %PBARIS%\availability.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\commands.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\frame_stream.proto

%PGEN% --proto_path=%PBARIS% %PBARIS%\command_module_beacon.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\defender_availability.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\defender_settings.proto

dir %DEST%

ENDLOCAL
