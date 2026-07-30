FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# This is here solely for ECS 'host' network internal health checks that run on the container 
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY publish/ ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "soteltestaws.dll"]