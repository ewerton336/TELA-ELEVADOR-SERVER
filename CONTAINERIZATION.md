# TELA-ELEVADOR-SERVER - Containerização

Este projeto está estruturado em dois containers separados:

## Arquitetura

### 1. **API Container** (ASP.NET Core)

- Responsável por servir endpoints REST
- SEM background services/workers
- Acessa dados da base e os expõe via HTTP
- Comunica-se com o PostgreSQL

### 2. **Worker Container** (.NET Worker Service)

- Executa `ClimaWorker`: Atualiza previsão de clima a cada 4 horas via Open-Meteo API
- Executa `NoticiasWorker`: Atualiza notícias de RSS feeds a cada 2 horas
- Acessa dados da base e os persiste
- Comunica-se com o PostgreSQL
- Executa de forma independente e cíclica

### 3. **PostgreSQL Container**

- Banco de dados compartilhado entre API e Worker
- Persiste todas as entidades

## Como Executar

### Pré-requisitos

- Docker e Docker Compose instalados

### Comando para rodar

```bash
docker-compose up -d
```

Isso iniciará:

1. PostgreSQL (porta 5432)
2. API (porta 8080)
3. Worker (sem porta exposta, interno)

### Parar containers

```bash
docker-compose down
```

### Ver logs

```bash
docker-compose logs api       # Logs da API
docker-compose logs worker    # Logs do Worker
docker-compose logs postgres  # Logs do PostgreSQL
docker-compose logs -f        # Todos os logs em tempo real
```

## Fluxo de Dados

```
┌─────────────┐
│  PostgreSQL │
├─────────────┤
│  Cidades    │
│  Clima      │
│  Predios    │
│  Noticias   │
└──────┬──────┘
       │
       ├──────────────────┬──────────────────┐
       │                  │                  │
       ▼                  ▼                  ▼
   ┌────────┐        ┌──────────┐      ┌──────────┐
   │  API   │        │ ClimaWkr │      │ NotWkr   │
   │ (REST) │        │(Open-Met)│      │(RSS Feed)│
   └────────┘        └──────────┘      └──────────┘
       │
       ▼
  ┌─────────┐
  │Frontend │ (React/TypeScript)
  └─────────┘
```

## Detalhes de Configuração

- **API**: Aguarda conectar ao PostgreSQL antes de iniciar
- **Worker**: Aguarda conectar ao PostgreSQL antes de iniciar workers
- **Restart Policy**: Ambos têm `restart: unless-stopped` para reiniciar após falhas
- **Connection String**: Configurada automaticamente via docker-compose

## Variáveis de Ambiente

### API

- `ASPNETCORE_ENVIRONMENT`: Production
- `ConnectionStrings__DefaultConnection`: String de conexão ao PostgreSQL

### Worker

- `DOTNET_ENVIRONMENT`: Production
- `ConnectionStrings__DefaultConnection`: String de conexão ao PostgreSQL

## Build Manual

Se quiser fazer build dos containers manualmente:

```bash
# API
docker build -f Dockerfile -t elevador-api .

# Worker
docker build -f Dockerfile.worker -t elevador-worker .

# PostgreSQL (já vem do Docker Hub)
```

## Troubleshooting

### API não conecta ao banco

Verifique se o PostgreSQL iniciou corretamente:

```bash
docker-compose logs postgres
```

### Worker não está rodando

Verifique os logs do worker:

```bash
docker-compose logs worker
```

### Portas em conflito

Mude as portas no `docker-compose.yml`:

```yaml
ports:
  - "8081:8080" # API em 8081 em vez de 8080
```

## Observações

- ✅ API contém **APENAS** serviços HTTP e de persistência
- ✅ Worker contém **TODOS** os Background Services (Clima + Notícias)
- ✅ Cada container pode ser escalado independentemente
- ✅ Separação clara entre responsabilidades
