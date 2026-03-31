# ─────────────────────────────────────────────────────────────────────────────
# Stage 1: Build
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copia o arquivo de projeto e restaura as dependências
COPY *.csproj ./
RUN dotnet restore

# Copia o restante do código e publica em modo Release
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime
# Imagem final menor, apenas com o runtime (sem SDK)
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copia o build publicado
COPY --from=build /app/publish .

EXPOSE 8080

# CMD com shell para que ${PORT} seja expandido em runtime pelo Railway
CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet EconomyBackPortifolio.dll
