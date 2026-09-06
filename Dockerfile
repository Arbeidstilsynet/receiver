# syntax=docker/dockerfile:1.27@sha256:bde3983e9c939224420ddaf6b784cc30e09b035a4dea01f581230c50809f372e
# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c AS build
WORKDIR /source

# Improves restore speed by skipping XML docs.
ENV NUGET_XMLDOC_MODE=skip

COPY src/MeldingerReceiver.slnx .
COPY src/global.json .

# Copy project files first to maximize Docker layer caching for restore.
COPY src/App/src/App.csproj ./App/src/
COPY src/Domain/Ports/App/Domain.Ports.App.csproj ./Domain/Ports/App/
COPY src/Domain/Data/Domain.Data.csproj ./Domain/Data/
COPY src/Domain/Logic/src/Domain.Logic.csproj ./Domain/Logic/src/
COPY src/Domain/Ports/Infrastructure/Domain.Ports.Infrastructure.csproj ./Domain/Ports/Infrastructure/
COPY src/Infrastructure/src/Infrastructure.csproj ./Infrastructure/src/
COPY src/Publish/Receiver.Publish/Receiver.Publish.csproj ./Publish/Receiver.Publish/

RUN dotnet restore ./App/src/App.csproj

# Copy the remaining source code.
COPY src/Domain/ ./Domain/
COPY src/App/ ./App/
COPY src/Infrastructure/ ./Infrastructure/
COPY src/Publish/ ./Publish/

RUN dotnet publish ./App/src/App.csproj -c Release -f net10.0 -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:27b6b84beeede74fd16886177d360799c8e4299ceadfbd64eef57bafead7878a
WORKDIR /app
RUN apk add --no-cache curl
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "MeldingerReceiver.App.dll"]
