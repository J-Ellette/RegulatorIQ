FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/RegulatorIQ.BackgroundServices/RegulatorIQ.BackgroundServices.csproj", "src/RegulatorIQ.BackgroundServices/"]
COPY ["src/RegulatorIQ.API/RegulatorIQ.API.csproj", "src/RegulatorIQ.API/"]
RUN dotnet restore "src/RegulatorIQ.BackgroundServices/RegulatorIQ.BackgroundServices.csproj"
COPY . .
WORKDIR "/src/src/RegulatorIQ.BackgroundServices"
RUN dotnet build "RegulatorIQ.BackgroundServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RegulatorIQ.BackgroundServices.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RegulatorIQ.BackgroundServices.dll"]
