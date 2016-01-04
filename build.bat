@echo OFF
cls

IF "%1"=="api" (
	CALL :API
	GOTO EXIT
)

ECHO [INFO] ------------------------------------------------------------------------
ECHO [INFO] Running in Release mode
CALL :API
ECHO [INFO] ------------------------------------------------------------------------
ECHO [INFO] Running FAKE
"src\.nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "src\packages" "-ExcludeVersion"
"src\packages\FAKE\tools\Fake.exe" scripts\release.fsx
GOTO EXIT

:API
ECHO [INFO] ------------------------------------------------------------------------
ECHO [INFO] Running API Generation
scripts\tools\f#\fsi.exe scripts\GenerateAPI.fsx
EXIT /B 0

:EXIT
ECHO [INFO] ------------------------------------------------------------------------