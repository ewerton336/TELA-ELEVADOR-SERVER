# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["TELA-ELEVADOR-SERVER.Api/TELA-ELEVADOR-SERVER.Api.csproj", "TELA-ELEVADOR-SERVER.Api/"]
COPY ["TELA-ELEVADOR-SERVER.Application/TELA-ELEVADOR-SERVER.Application.csproj", "TELA-ELEVADOR-SERVER.Application/"]
COPY ["TELA-ELEVADOR-SERVER.Domain/TELA-ELEVADOR-SERVER.Domain.csproj", "TELA-ELEVADOR-SERVER.Domain/"]
COPY ["TELA-ELEVADOR-SERVER.EntityFrameworkCore/TELA-ELEVADOR-SERVER.EntityFrameworkCore.csproj", "TELA-ELEVADOR-SERVER.EntityFrameworkCore/"]
COPY ["TELA-ELEVADOR-SERVER.Infrastructure/TELA-ELEVADOR-SERVER.Infrastructure.csproj", "TELA-ELEVADOR-SERVER.Infrastructure/"]

RUN dotnet restore "TELA-ELEVADOR-SERVER.Api/TELA-ELEVADOR-SERVER.Api.csproj"

COPY . .
WORKDIR "/src/TELA-ELEVADOR-SERVER.Api"
RUN dotnet build "TELA-ELEVADOR-SERVER.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TELA-ELEVADOR-SERVER.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TELA-ELEVADOR-SERVER.Api.dll"]
