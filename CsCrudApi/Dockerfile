# Usa a imagem SDK do .NET 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o arquivo de projeto
# Agora o SOURCE é relativo ao Root Directory (que é CsCrudApi/)
# Então, o CsCrudApi.csproj está na RAIZ desse contexto
# E você quer copiar para a RAIZ do WORKDIR (/src)
COPY ["CsCrudApi.csproj", "./"] 
# CUIDADO: Ajustado aqui!

# Restaura os pacotes
# O .csproj está agora em /src/CsCrudApi.csproj
RUN dotnet restore "./CsCrudApi.csproj"

# Copia o conteúdo do contexto (CsCrudApi/) para /src
COPY . .

# Define o WORKDIR para a raiz do projeto dentro do contêiner
# Se você copiou CsCrudApi.csproj para /src/CsCrudApi.csproj, então o WORKDIR principal é /src
WORKDIR "/src" 
# CUIDADO: Ajustado aqui!

# Agora, o dotnet build será executado de dentro da pasta do projeto
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CsCrudApi.dll"]