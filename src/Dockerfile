# syntax=docker/dockerfile:1.23@sha256:2780b5c3bab67f1f76c781860de469442999ed1a0d7992a5efdf2cffc0e3d769
# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:5c559aa5d99337e400d39ab4fa1f6979d126c29b20939d53658ed38300571e74 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:1e37a8236c558ae31bd6bc8144e38e6036b73cf1b0616fe56d79e60babb9d93b
WORKDIR /app
RUN apk add --no-cache curl
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "MeldingerReceiver.App.dll"]
