# CI/CD - TELA-ELEVADOR-SERVER

Este projeto agora possui deploy automatico via GitHub Actions.

## Pipeline

Arquivo:

- `.github/workflows/deploy-server.yml`

Trigger:

- `push` na branch `main`
- `pull_request` fechado com merge em `main`

Fluxo:

1. Checkout do codigo.
2. Empacotamento `tar.gz` sem artefatos de build locais.
3. Upload para VPS via SCP.
4. Extracao em diretorio de deploy.
5. `docker compose up -d --build`.
6. Validacao de conectividade da API e status do worker.

## Secrets necessarios

Configurar no repositório em `Settings > Secrets and variables > Actions`:

- `VPS_SSH_KEY`: chave privada SSH de deploy.
- `VPS_HOST`: host da VPS (ex: `130.250.189.175`).
- `VPS_USER`: usuario SSH (ex: `root`).
- `VPS_DEPLOY_DIR_SERVER`: pasta de deploy (ex: `/opt/tela-elevador-server`).
- `SERVER_API_PORT`: porta local de bind da API (default no workflow: `8080`).

## Observacoes operacionais

- O workflow assume Docker + Docker Compose instalados no servidor.
- O `docker-compose.yml` atual expoe `5432` no host; se houver conflito, ajuste a porta antes do primeiro deploy automatico.
- Se a API nao tiver rota de health dedicada, o pipeline valida apenas resposta HTTP na porta configurada.

## Checklist antes de habilitar em producao

1. Confirmar pasta final do deploy na VPS.
2. Confirmar portas livres na VPS para `8080` e `5432`.
3. Validar compose manualmente uma vez no servidor.
4. Configurar todos os secrets do workflow.
5. Executar primeiro deploy com monitoramento de logs:

```bash
cd /opt/tela-elevador-server
docker compose ps
docker compose logs -f api
```
