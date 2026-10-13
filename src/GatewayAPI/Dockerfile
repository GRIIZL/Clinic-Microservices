FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/GatewayAPI/GatewayAPI.csproj", "src/GatewayAPI/"]
RUN dotnet restore "src/GatewayAPI/GatewayAPI.csproj"
COPY src/GatewayAPI/ src/GatewayAPI/
RUN dotnet publish "src/GatewayAPI/GatewayAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "GatewayAPI.dll"]
