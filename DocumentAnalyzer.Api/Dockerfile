FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["DocumentAnalyzer.Api/DocumentAnalyzer.Api.csproj", "DocumentAnalyzer.Api/"]
COPY ["DocumentAnalyzer.Services/DocumentAnalyzer.Services.csproj", "DocumentAnalyzer.Services/"]
RUN dotnet restore "DocumentAnalyzer.Api/DocumentAnalyzer.Api.csproj"
COPY . .
WORKDIR "/src/DocumentAnalyzer.Api"
RUN dotnet build "DocumentAnalyzer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocumentAnalyzer.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocumentAnalyzer.Api.dll"]