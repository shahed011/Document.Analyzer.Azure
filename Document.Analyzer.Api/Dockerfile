FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Document.Analyzer.Api/Document.Analyzer.Api.csproj", "Document.Analyzer.Api/"]
COPY ["Document.Analyzer.Services/Document.Analyzer.Services.csproj", "Document.Analyzer.Services/"]
RUN dotnet restore "Document.Analyzer.Api/Document.Analyzer.Api.csproj"
COPY . .
WORKDIR "/src/Document.Analyzer.Api"
RUN dotnet build "Document.Analyzer.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Document.Analyzer.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Document.Analyzer.Api.dll"]