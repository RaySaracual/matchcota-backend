FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Matchcota.Api/Matchcota.Api.csproj", "Matchcota.Api/"]
COPY ["Matchcota.Services/Matchcota.Services.csproj", "Matchcota.Services/"]
COPY ["Matchcota.Infrastructure/Matchcota.Infrastructure.csproj", "Matchcota.Infrastructure/"]
COPY ["Matchcota.Core/Matchcota.Core.csproj", "Matchcota.Core/"]
RUN dotnet restore "Matchcota.Api/Matchcota.Api.csproj"

COPY . .
WORKDIR "/src/Matchcota.Api"
RUN dotnet publish "Matchcota.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Matchcota.Api.dll"]
