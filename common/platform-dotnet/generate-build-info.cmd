SET SOLUTION_DIR=%1
SET CONFIG=%2
SET OUTPUT_TYPE=%3
SET NAMESPACE=%4
SET OUTPUT_PATH=%5

SET BUILD_NUMBER=%6
IF "%BUILD_NUMBER%" == "" set BUILD_NUMBER=55555

SET BUILD_VCS_NUMBER=%7
IF "%BUILD_VCS_NUMBER%" == "" SET BUILD_VCS_NUMBER=NA


ECHO SOLUTION_DIR=	[%SOLUTION_DIR%]
ECHO CONFIG=		[%CONFIG%]
ECHO OUTPUT_TYPE=	[%OUTPUT_TYPE%]
ECHO NAMESPACE=		[%NAMESPACE%]
ECHO OUTPUT_PATH=	[%OUTPUT_PATH%]
ECHO BUILD_NUMBER=	[%BUILD_NUMBER%]
ECHO BUILD_VCS_NUMBER=[%BUILD_VCS_NUMBER%]


dotnet run ^
  --no-build ^
  --no-restore ^
  --project "%SOLUTION_DIR%..\..\submodules\build-tools\BuildInfo\BuildInfo.fsproj" ^
  -c %CONFIG% ^
  -- ^
  --version-file "%SOLUTION_DIR%\ver.platform.txt" ^
  --output-type %OUTPUT_TYPE% ^
  --namespace %NAMESPACE% ^
  --output-path "%OUTPUT_PATH%" ^
  --build-number %BUILD_NUMBER% ^
  --thumbprint %BUILD_VCS_NUMBER%
