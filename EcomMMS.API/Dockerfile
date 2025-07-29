FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EcomMMS.API/EcomMMS.API.csproj", "EcomMMS.API/"]
COPY ["EcomMMS.Application/EcomMMS.Application.csproj", "EcomMMS.Application/"]
COPY ["EcomMMS.Domain/EcomMMS.Domain.csproj", "EcomMMS.Domain/"]
COPY ["EcomMMS.Infrastructure/EcomMMS.Infrastructure.csproj", "EcomMMS.Infrastructure/"]
COPY ["EcomMMS.Persistence/EcomMMS.Persistence.csproj", "EcomMMS.Persistence/"]
RUN dotnet restore "EcomMMS.API/EcomMMS.API.csproj"
COPY . .
WORKDIR "/src/EcomMMS.API"
RUN dotnet build "EcomMMS.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EcomMMS.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EcomMMS.API.dll"] 