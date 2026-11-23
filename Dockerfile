FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 6200

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["StarTalkSessions.csproj", "."]
RUN dotnet restore "./StarTalkSessions.csproj"

COPY . .
WORKDIR "/src"
RUN dotnet build "StarTalkSessions.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "StarTalkSessions.csproj" -c Release -o /app/publish

# Final image using the runtime
FROM base AS final
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
COPY --from=publish /app/publish .

# Start the application
ENTRYPOINT ["dotnet", "StarTalkSessions.dll"]