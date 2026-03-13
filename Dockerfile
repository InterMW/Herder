FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish Application -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "Application.dll"]

RUN apt-get update \ 
  && apt-get install -y wget
LABEL deunhealth.restart.on.unhealthy "true"


HEALTHCHECK  --interval=30s --timeout=3s --start-period=10s\
  CMD wget --no-verbose --tries=1 -O /dev/null http://localhost:8080/health
