FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore NewsIntelligence.sln
RUN dotnet build NewsIntelligence.sln -c Release --no-restore --verbosity minimal
RUN dotnet publish src/NewsIntelligence.Api/NewsIntelligence.Api.csproj -c Release -o /app/publish --no-restore --no-build
RUN test -f /app/publish/Microsoft.EntityFrameworkCore.Relational.dll \
    && test -f /app/publish/Npgsql.EntityFrameworkCore.PostgreSQL.dll \
    && test -f /app/publish/NewsIntelligence.Infrastructure.dll \
    && test -f /app/publish/NewsIntelligence.Api.dll

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
LABEL org.opencontainers.image.title="Northstar News Intelligence" \
      org.opencontainers.image.version="1.4.0"
RUN apt-get update && apt-get install -y --no-install-recommends wget && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "NewsIntelligence.Api.dll"]
