FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AiAgentProject/AiAgentProject.csproj", "AiAgentProject/"]
RUN dotnet restore "AiAgentProject/AiAgentProject.csproj"
COPY . .
WORKDIR "/src/AiAgentProject"
RUN dotnet build "./AiAgentProject.csproj" -c $BUILD_CONFIGURATION -o /app/build
s
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AiAgentProject.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiAgentProject.dll"]
