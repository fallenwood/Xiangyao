FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR "/src"
COPY . .
RUN dotnet restore "src/Xiangyao/Xiangyao.csproj"
RUN dotnet build "src/Xiangyao/Xiangyao.csproj" -r net9.0 -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Xiangyao/Xiangyao.csproj" -r net9.0 -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_gcServer=1

ENTRYPOINT ["dotnet", "Xiangyao.dll"]
