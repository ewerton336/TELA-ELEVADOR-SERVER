@echo off
REM ========================================
REM Script para iniciar os 3 projetos
REM API, Worker e Frontend em abas separadas
REM ========================================

setlocal

REM Caminho base dos projetos (pasta onde o .bat esta)
set "BASE_PATH=%~dp0"
set "SERVER_PATH=%BASE_PATH%"
set "WEB_PATH=%BASE_PATH%..\TELA-ELEVADOR-WEB"

for %%I in ("%SERVER_PATH%") do set "SERVER_PATH=%%~fI"
for %%I in ("%WEB_PATH%") do set "WEB_PATH=%%~fI"

REM ========================================
REM 1. Inicia API em nova janela PowerShell
REM ========================================
echo Iniciando API (porta 3003)...
start "TELA-ELEVADOR-API" powershell -NoExit -Command "& { Set-Location '%SERVER_PATH%'; Write-Host '=== API iniciando ===' -ForegroundColor Green; Write-Host 'Porta: 3003' -ForegroundColor Cyan; Write-Host 'Logs abaixo:' -ForegroundColor Cyan; Write-Host '======================================' -ForegroundColor Green; dotnet run --project TELA-ELEVADOR-SERVER.Api }"

REM ========================================
REM 2. Inicia Worker em nova janela PowerShell
REM ========================================
echo Iniciando Worker...
start "TELA-ELEVADOR-WORKER" powershell -NoExit -Command "& { Set-Location '%SERVER_PATH%'; Write-Host '=== Worker iniciando ===' -ForegroundColor Yellow; Write-Host 'ClimaWorker + NoticiasWorker' -ForegroundColor Cyan; Write-Host 'Logs abaixo:' -ForegroundColor Cyan; Write-Host '======================================' -ForegroundColor Yellow; dotnet run --project TELA-ELEVADOR-SERVER.Worker }"

REM ========================================
REM 3. Inicia Frontend em nova janela PowerShell
REM ========================================
echo Iniciando Frontend (porta 3000)...
start "TELA-ELEVADOR-WEB" powershell -NoExit -Command "& { Set-Location '%WEB_PATH%'; Write-Host '=== Frontend iniciando ===' -ForegroundColor Magenta; Write-Host 'Porta: 3000' -ForegroundColor Cyan; Write-Host 'Dashboard: http://localhost:3000/gramado' -ForegroundColor Cyan; Write-Host 'Admin: http://localhost:3000/master' -ForegroundColor Cyan; Write-Host 'Logs abaixo:' -ForegroundColor Cyan; Write-Host '======================================' -ForegroundColor Magenta; npm run dev }"

REM Aguarda um pouco para visualizar as mensagens
timeout /t 2 /nobreak

echo.
echo ========================================
echo ✓ Todos os projetos foram iniciados!
echo ========================================
echo.
echo Janelas abertas:
echo  • TELA-ELEVADOR-API (porta 3003)
echo  • TELA-ELEVADOR-WORKER (background services)
echo  • TELA-ELEVADOR-WEB (porta 3000)
echo.
echo Você pode acompanhar os logs em cada janela.
echo.
pause
