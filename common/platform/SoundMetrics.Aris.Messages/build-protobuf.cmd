SETLOCAL

SET DEST=.\generated
IF NOT EXIST %DEST% MKDIR %DEST%

ECHO Generating protobuf output...

SET PROTOC=..\packages\Google.Protobuf.Tools.3.5.1\tools\windows_x86\protoc.exe
%PROTOC% --version

set PGEN=%PROTOC% --csharp_out=%DEST% --error_format=msvs %1 %2

set PBARIS=..\..\protobuf

REM %PGEN% --proto_path=%PBARIS% -namespace=SoundMetrics.Aris.Messages.Availability %PBARIS%\availability.proto
REM %PGEN% --proto_path=%PBARIS% -namespace=SoundMetrics.Aris.Messages.Commands     %PBARIS%\commands.proto
REM %PGEN% --proto_path=%PBARIS% -namespace=SoundMetrics.Aris.Messages.FrameStream  %PBARIS%\frame_stream.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\availability.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\commands.proto
%PGEN% --proto_path=%PBARIS% %PBARIS%\frame_stream.proto

dir %DEST%

ENDLOCAL
