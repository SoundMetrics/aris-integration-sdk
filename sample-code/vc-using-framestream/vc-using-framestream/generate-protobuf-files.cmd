@ECHO OFF

ECHO Generating protobuf source files...
IF NOT EXIST generated mkdir generated
del/q generated\*.pb.*

set PROTO_FILES_DIR=..\..\..\common\protobuf
set PROTOC_DIR=..\..\..\submodules\vcpkg\packages\protobuf_x86-windows-static\tools\protobuf

REM wildcard doesn't seem to work any more, so loop through the files.

for %%P in (%PROTO_FILES_DIR%\*.proto) do %PROTOC_DIR%\protoc --cpp_out=.\generated --proto_path=%PROTO_FILES_DIR% %%P

dir /b generated\*
