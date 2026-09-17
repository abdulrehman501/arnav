@echo off
cd /d "%~dp0"

if not exist venv (
    echo Creating virtual environment, first run only...
    python -m venv venv
)

set PY=venv\Scripts\python.exe

echo Installing dependencies...
"%PY%" -m pip install -q -r requirements.txt

echo.
"%PY%" start_tunnel.py

pause
