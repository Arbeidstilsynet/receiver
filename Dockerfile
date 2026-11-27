# syntax=docker/dockerfile:1.29@sha256:572cebbc2b15e6f84c077742952674889cb63cd5e9b80abdffcbe587b6f19183
# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:11.0.100-alpine3.24@sha256:52556c0fb9d0f609ae9aeab9efeeba96f4acc2f7038f7696f2235ccbdbb8e290 AS build
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

RUN dotnet publish ./App/src/App.csproj -c Release -f net11.0 -o /app --no-restore

# final stage/image
# Distroless, non-root by default. The -extra variant includes tzdata (RecoveryJob is scheduled in Europe/Oslo).
FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-resolute-chiseled-extra@sha256:4f2b9809c1974acd91dc04777bf464808fb5d22a3455c6657a97847077878936
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "MeldingerReceiver.App.dll"]
