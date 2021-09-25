#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/EarthLat.Backend.SyncService/EarthLat.Backend.SyncService.csproj", "src/EarthLat.Backend.SyncService/"]
RUN dotnet restore "src/EarthLat.Backend.SyncService/EarthLat.Backend.SyncService.csproj"
COPY . .
WORKDIR "/src/src/EarthLat.Backend.SyncService"
RUN dotnet build "EarthLat.Backend.SyncService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EarthLat.Backend.SyncService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EarthLat.Backend.SyncService.dll"]