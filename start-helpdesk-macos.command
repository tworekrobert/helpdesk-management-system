#!/bin/sh
set -u

cd "$(dirname "$0")" || exit 1

APP_URL="http://localhost:5173"

echo "Starting local helpdesk stack with Docker Compose..."

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker Desktop is required. Install and start Docker Desktop, then run this file again."
  printf "Press Enter to close..."
  read _input
  exit 1
fi

if ! docker compose version >/dev/null 2>&1; then
  echo "Docker Compose plugin is required. Update Docker Desktop, then run this file again."
  printf "Press Enter to close..."
  read _input
  exit 1
fi

docker compose up --build -d
status=$?

if [ "$status" -ne 0 ]; then
  echo "Startup failed. Run 'docker compose logs' from this folder for details."
  printf "Press Enter to close..."
  read _input
  exit "$status"
fi

echo "Services are starting. Opening $APP_URL ..."
sleep 3
open "$APP_URL" >/dev/null 2>&1 || true

echo "Done. To stop the stack later, run: docker compose down"
printf "Press Enter to close..."
read _input
