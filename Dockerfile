FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/BookingService.Api/BookingService.Api.csproj", "src/BookingService.Api/"]
COPY ["src/BookingService.Application/BookingService.Application.csproj", "src/BookingService.Application/"]
COPY ["src/BookingService.Domain/BookingService.Domain.csproj", "src/BookingService.Domain/"]
COPY ["src/BookingService.Infrastructure/BookingService.Infrastructure.csproj", "src/BookingService.Infrastructure/"]
RUN dotnet restore "src/BookingService.Api/BookingService.Api.csproj"

COPY . .
RUN dotnet publish "src/BookingService.Api/BookingService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookingService.Api.dll"]
