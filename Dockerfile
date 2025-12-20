FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LeaderBoardUsingCaching/LeaderBoardUsingCaching.csproj", "LeaderBoardUsingCaching/"]
RUN dotnet restore "./LeaderBoardUsingCaching/LeaderBoardUsingCaching.csproj"
COPY . .
WORKDIR "/src/LeaderBoardUsingCaching"
RUN dotnet build "./LeaderBoardUsingCaching.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LeaderBoardUsingCaching.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeaderBoardUsingCaching.dll"]
