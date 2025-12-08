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
COPY ["src/StreamForge.Worker/StreamForge.Worker.csproj", "src/StreamForge.Worker/"]
COPY ["src/StreamForge.Application/StreamForge.Application.csproj", "src/StreamForge.Application/"]
COPY ["src/StreamForge.Domain/StreamForge.Domain.csproj", "src/StreamForge.Domain/"]
COPY ["src/StreamForge.Infrastructure/StreamForge.Infrastructure.csproj", "src/StreamForge.Infrastructure/"]
RUN dotnet restore "src/StreamForge.Worker/StreamForge.Worker.csproj"

# ------------------------------------------------------------------------------ 
# Build da Aplicação:
# Copia o restante do código fonte e compila.
# ------------------------------------------------------------------------------ 
COPY . .
WORKDIR "/app/src/StreamForge.Worker"
RUN dotnet build "StreamForge.Worker.csproj" -c Release -o /app/build

# ------------------------------------------------------------------------------ 
# Publish:
# Gera os binários finais otimizados para produção.
# ------------------------------------------------------------------------------ 
FROM build AS publish
RUN dotnet publish "StreamForge.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================================================== 
# Estágio 2: Runtime (Imagem Final)
# Usa uma imagem leve (ASP.NET Core Runtime) apenas para rodar o app.
# ============================================================================== 
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# ------------------------------------------------------------------------------ 
# Instalação de Ferramentas de Processamento de Mídia (FFmpeg)
# Necessário para análise de arquivos de vídeo.
# ------------------------------------------------------------------------------ 
RUN apt-get update && \
    apt-get install -y wget xz-utils ffmpeg && \
    rm -rf /var/lib/apt/lists/*     

# Copia os binários gerados no estágio de publish
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "StreamForge.Worker.dll"]
