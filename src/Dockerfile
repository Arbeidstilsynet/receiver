# syntax=docker/dockerfile:1.23@sha256:2780b5c3bab67f1f76c781860de469442999ed1a0d7992a5efdf2cffc0e3d769
# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:732cd42c6f659814c9804ad7b05c7f761e83ef8379c5b2fdc3af673353caff73 AS build
WORKDIR /source

# Improves restore speed by skipping XML docs.
ENV NUGET_XMLDOC_MODE=skip

COPY MeldingerReceiver.slnx .
COPY nuget.config .

# Copy project files first to maximize Docker layer caching for restore.
COPY App/src/App.csproj ./App/src/
COPY Domain/Ports/App/Domain.Ports.App.csproj ./Domain/Ports/App/
COPY Domain/Data/Domain.Data.csproj ./Domain/Data/
COPY Domain/Logic/src/Domain.Logic.csproj ./Domain/Logic/src/
COPY Domain/Ports/Infrastructure/Domain.Ports.Infrastructure.csproj ./Domain/Ports/Infrastructure/
COPY Infrastructure/src/Infrastructure.csproj ./Infrastructure/src/
COPY Publish/Receiver.Publish/Receiver.Publish.csproj ./Publish/Receiver.Publish/

RUN dotnet restore ./App/src/App.csproj

# Copy the remaining source code.
COPY Domain/ ./Domain/
COPY App/ ./App/
COPY Infrastructure/ ./Infrastructure/
COPY Publish/ ./Publish/

RUN dotnet publish ./App/src/App.csproj -c Release -f net10.0 -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:1201dde897ab436b7c6b386f6dbd4f9a3ca0245f9c5a8aac8f8bcdccb4c7d484
WORKDIR /app
RUN apk add --no-cache curl
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "MeldingerReceiver.App.dll"]
