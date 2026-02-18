# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

COPY Server/*.slnx .
COPY Server/Server/*.csproj ./Server/

RUN dotnet restore

COPY . .
WORKDIR /app/Server

RUN dotnet publish -c Release -o out /p:GenerateOpenApiDocuments=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app/Server/out .

ENV ASPNETCORE_URLS=http://+:3001
EXPOSE 3001

ENTRYPOINT ["dotnet", "Server.dll"]
