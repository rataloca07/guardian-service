# Usar una imagen oficial de .NET como base
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Construir la imagen
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GuardianService/GuardianService.csproj", "GuardianService/"]
RUN dotnet restore "GuardianService/GuardianService.csproj"
COPY . .
WORKDIR "/src/GuardianService"
RUN dotnet build "GuardianService.csproj" -c Release -o /app/build

# Publicar el proyecto
FROM build AS publish
RUN dotnet publish "GuardianService.csproj" -c Release -o /app/publish

# Configurar la imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GuardianService.dll"]