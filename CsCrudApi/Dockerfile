# Usa a imagem SDK do .NET 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CsCrudApi/CsCrudApi.csproj", "CsCrudApi/"]
RUN dotnet nuget locals all --clear
RUN dotnet restore "./CsCrudApi/CsCrudApi.csproj"

# Limpa o cache de pacotes NuGet
RUN dotnet nuget locals all --clear

# Tenta restaurar os pacotes
RUN dotnet restore "./CsCrudApi/CsCrudApi.csproj"

COPY . .
WORKDIR "/src/CsCrudApi"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CsCrudApi.dll"]