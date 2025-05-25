# Usa a imagem SDK do .NET 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Define o diretório de trabalho dentro do contêiner como /src
WORKDIR /src 

# Copia o arquivo de projeto para a pasta correspondente dentro do WORKDIR
# O SOURCE (primeiro argumento) é relativo ao contexto de build (sua pasta raiz do repo)
# O DESTINATION (segundo argumento) é relativo ao WORKDIR (/src)
COPY ["CsCrudApi/CsCrudApi.csproj", "CsCrudApi/"]

# O comando restore precisa do caminho COMPLETO ou relativo ao WORKDIR atual
# Como você copiou para /src/CsCrudApi/, o caminho para restore é ./CsCrudApi/CsCrudApi.csproj
RUN dotnet restore "./CsCrudApi/CsCrudApi.csproj" # CUIDADO: Este era o ponto de erro no seu Dockerfile anterior.

# Limpa o cache de pacotes NuGet (boa prática, mas não é a causa do erro de "not found")
RUN dotnet nuget locals all --clear

# Copia o conteúdo do seu contexto de build (sua raiz do repo) para o WORKDIR (/src)
# Isso trará todas as outras pastas (Controllers, Models, etc.) e o Dockerfile em si.
COPY . .

# Define o WORKDIR para a pasta do projeto dentro do contêiner
# Isso é crucial para que o dotnet build funcione corretamente
WORKDIR "/src/CsCrudApi"

# Agora, o dotnet build será executado de dentro da pasta do projeto
RUN dotnet build -c Release -o /app/build

FROM build AS publish
# Aqui, como o build foi para /app/build, o publish também deve usar esse caminho
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# Copia os arquivos publicados da fase 'publish' para o diretório final
COPY --from=publish /app/publish .
# Define o ponto de entrada da aplicação
ENTRYPOINT ["dotnet", "CsCrudApi.dll"]