# ── Сборка ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Миграции ────────────────────────────────────────────────
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# ── Runtime ─────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="$PATH:/root/.dotnet/tools"

# Применяем миграции и запускаем API
ENTRYPOINT ["sh", "-c", "dotnet ef database update --no-build && dotnet WarehouseAPI.dll"]
