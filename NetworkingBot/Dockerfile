FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
COPY . .
ENTRYPOINT ["dotnet", "NetworkingBot.dll"]
