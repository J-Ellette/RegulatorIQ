FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/RegulatorIQ.API/RegulatorIQ.API.csproj", "src/RegulatorIQ.API/"]
RUN dotnet restore "src/RegulatorIQ.API/RegulatorIQ.API.csproj"
COPY . .
WORKDIR "/src/src/RegulatorIQ.API"
RUN dotnet build "RegulatorIQ.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RegulatorIQ.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RegulatorIQ.API.dll"]
