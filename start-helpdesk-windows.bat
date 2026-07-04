@echo off
setlocal

cd /d "%~dp0"
set "APP_URL=http://localhost:5173"

echo Starting local helpdesk stack with Docker Compose...

where docker >nul 2>nul
if errorlevel 1 (
  echo Docker Desktop is required. Install and start Docker Desktop, then run this file again.
  pause
  exit /b 1
)

docker compose version >nul 2>nul
if errorlevel 1 (
  echo Docker Compose plugin is required. Update Docker Desktop, then run this file again.
  pause
  exit /b 1
)

docker compose up --build -d
if errorlevel 1 (
  echo Startup failed. Run "docker compose logs" from this folder for details.
  pause
  exit /b 1
)

echo Services are starting. Opening %APP_URL% ...
timeout /t 3 /nobreak >nul
start "" "%APP_URL%"

echo Done. To stop the stack later, run: docker compose down
pause
