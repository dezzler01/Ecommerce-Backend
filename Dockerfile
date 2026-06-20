# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY PicksAndMore.slnx ./
COPY PicksAndMore.Domain/PicksAndMore.Domain.csproj PicksAndMore.Domain/
COPY PicksAndMore.Application/PicksAndMore.Application.csproj PicksAndMore.Application/
COPY PicksAndMore.Infrastructure/PicksAndMore.Infrastructure.csproj PicksAndMore.Infrastructure/
COPY PicksAndMore.API/PicksAndMore.API.csproj PicksAndMore.API/

RUN dotnet restore PicksAndMore.slnx

# Copy remaining source code and publish
COPY . .
RUN dotnet publish PicksAndMore.API/PicksAndMore.API.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render.com free tier uses port 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PicksAndMore.API.dll"]
