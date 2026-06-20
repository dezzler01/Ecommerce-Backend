FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything and restore projects
COPY . .
RUN dotnet restore "PicksAndMore.API/PicksAndMore.API.csproj"

# Build and publish the API in Release mode
RUN dotnet publish "PicksAndMore.API/PicksAndMore.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose standard port and trigger application startup
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PicksAndMore.API.dll"]
