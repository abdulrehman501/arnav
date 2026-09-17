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
echo Starting server on port 8000...
echo Leave this window open. Open start_tunnel.bat in a SECOND window next.
echo.
"%PY%" -m uvicorn main:app --host 0.0.0.0 --port 8000

pause
