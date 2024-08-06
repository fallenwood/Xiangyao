FROM mcr.microsoft.com/dotnet/aspnet:8.0-cbl-mariner2.0-distroless AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR "/src"
COPY . .
RUN dotnet restore "helloworld.csproj"
RUN dotnet build "helloworld.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "helloworld.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_gcServer=1

ENTRYPOINT ["dotnet", "helloworld.dll"]
