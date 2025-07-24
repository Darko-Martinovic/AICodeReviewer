FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 7001
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AICodeReviewer.csproj", "."]
RUN dotnet restore "./AICodeReviewer.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "AICodeReviewer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AICodeReviewer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy workflow configurations
COPY Configuration/ Configuration/

ENTRYPOINT ["dotnet", "AICodeReviewer.dll"]
