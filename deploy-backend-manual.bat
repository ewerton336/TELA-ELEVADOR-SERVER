@echo off
setlocal EnableExtensions DisableDelayedExpansion

pushd "%~dp0"

REM Manual backend deploy from local PC to VPS.
REM Requirements: tar, ssh and scp available in PATH (Windows OpenSSH + bsdtar).

set "VPS_HOST=130.250.189.175"
set "VPS_USER=root"
set "VPS_DEPLOY_DIR=/opt/tela-elevador-server"
set "SERVER_API_PORT=8080"
set "DB_HOST=130.250.189.175"
set "DB_PORT=5432"
set "DB_NAME=TELA_ELEVADOR"
set "DB_USER=dba"
set "DB_PASSWORD=Bb3491818835!"
set "PACKAGE_FILE=deploy.tar.gz"
set "REMOTE_DEPLOY_SCRIPT_FILE=deploy-backend-remote.sh"
set "REMOTE_PACKAGE=/tmp/tela-elevador-server-deploy.tar.gz"
set "REMOTE_SCRIPT=/tmp/tela-elevador-server-deploy.sh"
set "LOCAL_ENV_FILE=%TEMP%\tela-elevador-server.env"
set "REMOTE_ENV_FILE=/tmp/tela-elevador-server.env"
set "SSH_OPTS=-o ServerAliveInterval=30 -o ServerAliveCountMax=10 -o ConnectTimeout=20 -o TCPKeepAlive=yes"

if "%VPS_HOST%"=="" (
  set /p VPS_HOST=Digite o VPS_HOST: 
)

if "%VPS_HOST%"=="" (
  echo [ERRO] VPS_HOST nao informado.
  popd
  exit /b 1
)

where tar >nul 2>&1
if errorlevel 1 (
  echo [ERRO] Comando tar nao encontrado no PATH.
  popd
  exit /b 1
)

where ssh >nul 2>&1
if errorlevel 1 (
  echo [ERRO] Comando ssh nao encontrado no PATH.
  popd
  exit /b 1
)

where scp >nul 2>&1
if errorlevel 1 (
  echo [ERRO] Comando scp nao encontrado no PATH.
  popd
  exit /b 1
)

echo === Criando pacote de deploy ===
if exist "%PACKAGE_FILE%" del /f /q "%PACKAGE_FILE%"
tar --exclude=.git --exclude=.github --exclude=.vscode --exclude=**/bin --exclude=**/obj --exclude=*.log --exclude=%PACKAGE_FILE% -czf "%PACKAGE_FILE%" .
if errorlevel 1 (
  echo [ERRO] Falha ao criar pacote %PACKAGE_FILE%.
  popd
  exit /b 1
)

if not exist "%REMOTE_DEPLOY_SCRIPT_FILE%" (
  echo [ERRO] Arquivo %REMOTE_DEPLOY_SCRIPT_FILE% nao encontrado.
  popd
  exit /b 1
)

echo === Gerando arquivo de variaveis de ambiente ===
(
  echo DB_HOST=%DB_HOST%
  echo DB_PORT=%DB_PORT%
  echo DB_NAME=%DB_NAME%
  echo DB_USER=%DB_USER%
  echo DB_PASSWORD=%DB_PASSWORD%
) > "%LOCAL_ENV_FILE%"

echo === Enviando pacote para %VPS_USER%@%VPS_HOST% ===
scp %SSH_OPTS% "%PACKAGE_FILE%" %VPS_USER%@%VPS_HOST%:%REMOTE_PACKAGE%
if errorlevel 1 (
  echo [ERRO] Falha no upload via scp.
  popd
  exit /b 1
)

echo === Enviando script de deploy remoto ===
scp %SSH_OPTS% "%REMOTE_DEPLOY_SCRIPT_FILE%" %VPS_USER%@%VPS_HOST%:%REMOTE_SCRIPT%
if errorlevel 1 (
  echo [ERRO] Falha no upload do script remoto.
  del /f /q "%LOCAL_ENV_FILE%" >nul 2>&1
  popd
  exit /b 1
)

echo === Enviando arquivo .env para o servidor ===
scp %SSH_OPTS% "%LOCAL_ENV_FILE%" %VPS_USER%@%VPS_HOST%:%REMOTE_ENV_FILE%
if errorlevel 1 (
  echo [ERRO] Falha no upload do arquivo .env.
  del /f /q "%LOCAL_ENV_FILE%" >nul 2>&1
  popd
  exit /b 1
)

ssh -T %SSH_OPTS% %VPS_USER%@%VPS_HOST% "mkdir -p %VPS_DEPLOY_DIR% && mv %REMOTE_ENV_FILE% %VPS_DEPLOY_DIR%/.env"
if errorlevel 1 (
  echo [ERRO] Falha ao posicionar .env no servidor.
  del /f /q "%LOCAL_ENV_FILE%" >nul 2>&1
  popd
  exit /b 1
)

echo === Executando deploy no servidor ===
ssh -T %SSH_OPTS% %VPS_USER%@%VPS_HOST% "chmod +x %REMOTE_SCRIPT% && %REMOTE_SCRIPT% '%VPS_DEPLOY_DIR%' '%SERVER_API_PORT%' '%REMOTE_PACKAGE%'"
set "SSH_EXIT=%ERRORLEVEL%"

ssh -T %SSH_OPTS% %VPS_USER%@%VPS_HOST% "rm -f %REMOTE_SCRIPT%" >nul 2>&1
del /f /q "%LOCAL_ENV_FILE%" >nul 2>&1

if not "%SSH_EXIT%"=="0" (
  echo [ERRO] Deploy remoto falhou com codigo %SSH_EXIT%.
  popd
  exit /b %SSH_EXIT%
)

echo === Deploy concluido com sucesso ===
popd
exit /b 0
