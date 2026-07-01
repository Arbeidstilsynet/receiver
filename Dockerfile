# syntax=docker/dockerfile:1.25@sha256:0adf442eae370b6087e08edc7c50b552d80ddf261576f4ebd6421006b2461f12
# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:940f919ae84dd92ccd4aab7686fa5b777870b006c9360351039e16bcaad73d89 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:57bd717ac18ff6c8a39cc0ee4a76c1f15adc46df50434c73eff0c3f1df4c88f0
WORKDIR /app
RUN apk add --no-cache curl
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "MeldingerReceiver.App.dll"]
