@echo off
REM ========================================
REM Script para iniciar os 3 projetos
REM API, Worker e Frontend em abas separadas
REM ========================================

setlocal enabledelayedexpansion

REM Caminho base dos projetos (pasta onde o .bat esta)
set "BASE_PATH=%~dp0"
set "SERVER_PATH=%BASE_PATH%"
set "WEB_PATH=%BASE_PATH%..\TELA-ELEVADOR-WEB"

for %%I in ("%SERVER_PATH%") do set "SERVER_PATH=%%~fI"
for %%I in ("%WEB_PATH%") do set "WEB_PATH=%%~fI"

cls
echo.
echo ========================================
echo   INICIALIZANDO TELA-ELEVADOR
echo ========================================
echo.

REM ========================================
REM 0. Build completo do solution
REM ========================================
echo [1/4] Compilando solucao completa (DEBUG)...
echo Isso pode levar 30-60 segundos...
cd /d "%SERVER_PATH%"
call dotnet build TELA-ELEVADOR-SERVER.sln -c Debug -v minimal

if errorlevel 1 (
    echo.
    echo [ERRO] Build falhou!
    echo.
    pause
    exit /b 1
)

echo.
echo [OK] Build completado com sucesso!
echo.

REM ========================================
REM 1. Inicia API em nova janela PowerShell
REM ========================================
echo [2/4] Iniciando API (porta 3003)...
cd /d "%SERVER_PATH%"
start "" powershell -NoExit -Command "cd '%SERVER_PATH%' ; dotnet run --project TELA-ELEVADOR-SERVER.Api --no-build"
timeout /t 3 /nobreak

REM ========================================
REM 2. Inicia Worker em nova janela PowerShell
REM ========================================
echo [3/4] Iniciando Worker...
cd /d "%SERVER_PATH%"
start "" powershell -NoExit -Command "cd '%SERVER_PATH%' ; dotnet run --project TELA-ELEVADOR-SERVER.Worker --no-build"
timeout /t 3 /nobreak

REM ========================================
REM 3. Inicia Frontend em nova janela PowerShell
REM ========================================
echo [4/4] Iniciando Frontend (porta 3000)...
cd /d "%WEB_PATH%"
start "" powershell -NoExit -Command "cd '%WEB_PATH%' ; npm run dev"
timeout /t 2 /nobreak

echo.
echo ========================================
echo SUCESSO! 3 abas foram abertas:
echo ========================================
echo.
echo  Aba 1 - API           (porta 3003)
echo  Aba 2 - Worker        (background)
echo  Aba 3 - Frontend      (porta 3000)
echo.
echo URLs:
echo  • Dashboard: http://localhost:3000/gramado
echo  • Admin:     http://localhost:3000/master
echo.
pause
