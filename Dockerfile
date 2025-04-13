FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 52975

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS with-node
RUN apt-get update
RUN apt-get install -y curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get -y install nodejs

FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WeatherThreading.Server/WeatherThreading.Server.csproj", "WeatherThreading.Server/"]
COPY ["weatherthreading.client/weatherthreading.client.esproj", "weatherthreading.client/"]
RUN dotnet restore "./WeatherThreading.Server/WeatherThreading.Server.csproj"
COPY . .
WORKDIR "/src/WeatherThreading.Server"
RUN dotnet build "./WeatherThreading.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WeatherThreading.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Build the client app
WORKDIR "/src/weatherthreading.client"
RUN npm ci
RUN npm run build
RUN mkdir -p /app/publish/wwwroot
RUN cp -R dist/* /app/publish/wwwroot/

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherThreading.Server.dll"]