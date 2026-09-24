# syntax=docker/dockerfile:1.25@sha256:0adf442eae370b6087e08edc7c50b552d80ddf261576f4ebd6421006b2461f12
# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:620e765fe18186c08399f7aa978f79f04b6bbf0ee1b3b8a91e2d5c9619e59da1 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:c4b29bf368004ad9076c1ab9bc91fb373561e3905b4345637e14e8b8c57e3be8
WORKDIR /app
RUN apk add --no-cache curl
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "MeldingerReceiver.App.dll"]
