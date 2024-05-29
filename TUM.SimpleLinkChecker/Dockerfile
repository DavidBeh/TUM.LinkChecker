FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TUM.SimpleLinkChecker/TUM.SimpleLinkChecker.csproj", "TUM.SimpleLinkChecker/"]
COPY ["TUM.LinkChecker/TUM.LinkChecker.csproj", "TUM.LinkChecker/"]
RUN dotnet restore "TUM.SimpleLinkChecker/TUM.SimpleLinkChecker.csproj"
COPY . .
WORKDIR "/src/TUM.SimpleLinkChecker"
RUN dotnet build "TUM.SimpleLinkChecker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TUM.SimpleLinkChecker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TUM.SimpleLinkChecker.dll"]
