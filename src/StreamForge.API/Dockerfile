# ==============================================================================
# Estágio 1: Build & Publish
# Usa a imagem do SDK completa para compilar o código.
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# ------------------------------------------------------------------------------
# Otimização de Cache:
# Copiamos apenas os arquivos .csproj primeiro e rodamos 'dotnet restore'.
# Isso permite que o Docker faça cache das dependências NuGet se os arquivos de projeto não mudarem.
# ------------------------------------------------------------------------------
COPY ["src/StreamForge.API/StreamForge.API.csproj", "src/StreamForge.API/"]
COPY ["src/StreamForge.Application/StreamForge.Application.csproj", "src/StreamForge.Application/"]
COPY ["src/StreamForge.Domain/StreamForge.Domain.csproj", "src/StreamForge.Domain/"]
COPY ["src/StreamForge.Infrastructure/StreamForge.Infrastructure.csproj", "src/StreamForge.Infrastructure/"]
RUN dotnet restore "src/StreamForge.API/StreamForge.API.csproj"

# ------------------------------------------------------------------------------
# Build da Aplicação:
# Copia o restante do código fonte e compila.
# ------------------------------------------------------------------------------
COPY . .
WORKDIR "/app/src/StreamForge.API"
RUN dotnet build "StreamForge.API.csproj" -c Release -o /app/build

# ------------------------------------------------------------------------------
# Publish:
# Gera os binários finais otimizados para produção.
# ------------------------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "StreamForge.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Estágio 2: Runtime (Imagem Final)
# Usa uma imagem leve (ASP.NET Core Runtime) apenas para rodar o app.
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copia os binários gerados no estágio de publish
COPY --from=publish /app/publish .

# Configurações de Rede
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "StreamForge.API.dll"]