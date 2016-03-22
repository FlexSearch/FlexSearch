@echo OFF
cls

ECHO [INFO] Checking if FAKE was downloaded from Nuget
IF NOT EXIST %~dp0\src\packages\FAKE\tools\FAKE.exe (
	ECHO [INFO] Downloading FAKE from Nuget
	cd src
	.paket\paket.bootstrapper.exe
	if errorlevel 1 (
		exit /b %errorlevel%
	)

	.paket\paket.exe restore
	if errorlevel 1 (
		exit /b %errorlevel%
	)
	cd ../
)

IF "%1"=="api" (
	CALL :API
	GOTO EXIT
)

IF "%1"=="target" (
	SET _target=%2
	CALL :TARGET
	GOTO EXIT
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

:TARGET
ECHO [INFO] ------------------------------------------------------------------------
ECHO [INFO] Running the specified target from the Release script
"src\packages\FAKE\tools\Fake.exe" scripts\Release.fsx %_target% -st
EXIT /B 0

:EXIT
ECHO [INFO] ------------------------------------------------------------------------