FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CpuLoader/CpuLoader.csproj", "CpusLoader/"]
RUN ["dotnet", "restore", "CpuLoader/CpuLoader.csproj"]
COPY . .
WORKDIR "/src/CpuLoader"
RUN ["dotnet", "build", "CpuLoader.csproj", "-c Release", "-o /app/build"]

FROM build AS publish
RUN ["dotnet", "publish", "CpuLoader.csproj", "-c Release", "-o /app/publish"]

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CpuLoader.dll"]
