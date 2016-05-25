@echo OFF
cls

ECHO [INFO] Restoring the Paket packages
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

IF "%~1"=="" (
    "src\packages\FAKE\tools\Fake.exe" scripts\Release.fsx
) ELSE (
    "src\packages\FAKE\tools\Fake.exe" scripts\Release.fsx target=%*
)

