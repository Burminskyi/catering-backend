# ЭТАП 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем решения и проекты для кэширования слоев NuGet
COPY ["CateringSaaS.sln", "./"]
COPY ["CateringSaaS.WebAPI/CateringSaaS.WebAPI.csproj", "CateringSaaS.WebAPI/"]
COPY ["CateringSaaS.Shared/CateringSaaS.Shared.csproj", "CateringSaaS.Shared/"]
COPY ["Modules/CateringSaaS.Modules.Ordering/CateringSaaS.Modules.Ordering.csproj", "Modules/CateringSaaS.Modules.Ordering/"]
COPY ["Modules/CateringSaaS.Modules.Identity/CateringSaaS.Modules.Identity.csproj", "Modules/CateringSaaS.Modules.Identity/"]
COPY ["Modules/CateringSaaS.Modules.Tenants/CateringSaaS.Modules.Tenants.csproj", "Modules/CateringSaaS.Modules.Tenants/"]
COPY ["Modules/CateringSaaS.Modules.Inventory/CateringSaaS.Modules.Inventory.csproj", "Modules/CateringSaaS.Modules.Inventory/"]
COPY ["Modules/CateringSaaS.Modules.Menu/CateringSaaS.Modules.Menu.csproj", "Modules/CateringSaaS.Modules.Menu/"]
COPY ["Modules/CateringSaaS.Modules.Kitchen/CateringSaaS.Modules.Kitchen.csproj", "Modules/CateringSaaS.Modules.Kitchen/"]

# Восстанавливаем зависимости
RUN dotnet restore "CateringSaaS.sln"

# Копируем весь исходный код и собираем проект
COPY . .
WORKDIR "/src/CateringSaaS.WebAPI"
RUN dotnet publish "CateringSaaS.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ЭТАП 2: Запуск (Минимальный образ для быстрого старта)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CateringSaaS.WebAPI.dll"]