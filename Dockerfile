# syntax=docker/dockerfile:1.29@sha256:572cebbc2b15e6f84c077742952674889cb63cd5e9b80abdffcbe587b6f19183
# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0.404-alpine3.24@sha256:0294f285d062afacc8b5f6d7febb0cd4ee879320e5585fa15f2f4ebf1feb9ba7 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-alpine3.24@sha256:21c3f9798416cba4c0576a947c286231cb71ea2884896cb968253f5c9668f930
WORKDIR /app
# tzdata: RecoveryJob is scheduled in Europe/Oslo
RUN apk add --no-cache curl tzdata
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "MeldingerReceiver.App.dll"]
