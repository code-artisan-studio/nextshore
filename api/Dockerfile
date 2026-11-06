# syntax=docker/dockerfile:1
# Build context: api/ (Render rootDir). Tests are excluded by .dockerignore, so restore the Api project, not the .sln.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/NextShore.Domain/NextShore.Domain.csproj src/NextShore.Domain/
COPY src/NextShore.Application/NextShore.Application.csproj src/NextShore.Application/
COPY src/NextShore.Infrastructure/NextShore.Infrastructure.csproj src/NextShore.Infrastructure/
COPY src/NextShore.Api/NextShore.Api.csproj src/NextShore.Api/
RUN dotnet restore src/NextShore.Api/NextShore.Api.csproj
COPY src/ src/
RUN dotnet publish src/NextShore.Api/NextShore.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Render injects PORT; 10000 is its default when unset.
ENV PORT=10000
EXPOSE 10000
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["sh", "-c", "exec dotnet NextShore.Api.dll --urls http://0.0.0.0:${PORT}"]
