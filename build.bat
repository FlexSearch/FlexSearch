@echo OFF
cls

IF "%1"=="api" (
	CALL :API
	GOTO EXIT
)

ECHO [INFO] Checking if FAKE was downloaded from Nuget
IF NOT EXIST %~dp0\src\packages\FAKE\tools\FAKE.exe (
	ECHO [INFO] Downloading FAKE from Nuget
	"src\.nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "src\packages" "-ExcludeVersion"
)

ECHO [INFO] ------------------------------------------------------------------------
ECHO [INFO] Running in Release mode
CALL :API
ECHO [INFO] ------------------------------------------------------------------------
ECHO [INFO] Building the application
"src\packages\FAKE\tools\Fake.exe" scripts\release.fsx
GOTO EXIT

:API
ECHO [INFO] ------------------------------------------------------------------------
ECHO [INFO] Running API Generation
"src\packages\FAKE\tools\Fake.exe" scripts\GenerateAPI.fsx
EXIT /B 0

:EXIT
ECHO [INFO] ------------------------------------------------------------------------