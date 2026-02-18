# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:48b443d87fe7426ce6a20554e87fdd6189143dc4aa5f43b501acf5fef6d42740 AS build
WORKDIR /source

COPY MeldingerReceiver.sln ./
COPY nuget.config ./

# Copy project files first to maximize Docker layer caching for restore.
COPY API/API.Adapters/API.Adapters.csproj ./API/API.Adapters/
COPY API/API.Ports/API.Ports.csproj ./API/API.Ports/
COPY Domain/Domain.Data/Domain.Data.csproj ./Domain/Domain.Data/
COPY Domain/Domain.Logic/Domain.Logic.csproj ./Domain/Domain.Logic/
COPY Infrastructure/Infrastructure.Adapters/Infrastructure.Adapters.csproj ./Infrastructure/Infrastructure.Adapters/
COPY Infrastructure/Infrastructure.Ports/Infrastructure.Ports.csproj ./Infrastructure/Infrastructure.Ports/
COPY Publish/Receiver.Publish/Receiver.Publish.csproj ./Publish/Receiver.Publish/

RUN dotnet restore ./API/API.Adapters/API.Adapters.csproj

# Copy the remaining source code.
COPY Domain/ ./Domain/
COPY API/ ./API/
COPY Infrastructure/ ./Infrastructure/
COPY Publish/ ./Publish/

RUN dotnet publish ./API/API.Adapters/API.Adapters.csproj -c Release -f net10.0 -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:55fa85c0075d8b48b4854af502ffbb305470564e9125c227da28b52cfec6dc92
WORKDIR /build
COPY --from=build /app ./
RUN apk add --no-cache curl
ENTRYPOINT ["dotnet", "MeldingerReceiver.API.Adapters.dll"]
