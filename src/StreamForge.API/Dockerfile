# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar arquivos de projeto e restaurar dependências
COPY ["src/StreamForge.API/StreamForge.API.csproj", "src/StreamForge.API/"]
COPY ["src/StreamForge.Application/StreamForge.Application.csproj", "src/StreamForge.Application/"]
COPY ["src/StreamForge.Domain/StreamForge.Domain.csproj", "src/StreamForge.Domain/"]
COPY ["src/StreamForge.Infrastructure/StreamForge.Infrastructure.csproj", "src/StreamForge.Infrastructure/"]
RUN dotnet restore "src/StreamForge.API/StreamForge.API.csproj"

# Copiar o restante do código e compilar
COPY . .
WORKDIR "/app/src/StreamForge.API"
RUN dotnet build "StreamForge.API.csproj" -c Release -o /app/build

# Publicar a aplicação
FROM build AS publish
RUN dotnet publish "StreamForge.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio Final (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "StreamForge.API.dll"]
