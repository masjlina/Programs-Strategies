FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "WebAPI/WebAPI.csproj"

COPY . .
WORKDIR /src/WebAPI
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
 && apt-get install -y libgssapi-krb5-2 \
 && rm -rf /var/lib/apt/lists/*

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebAPI.dll"]
