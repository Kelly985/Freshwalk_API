# ASP.NET Core on Render: native runtimes do not include .NET — use Docker.
# https://render.com/docs/docker

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Freshwalk.Domain/Freshwalk.Domain.csproj Freshwalk.Domain/
COPY Freshwalk.Application/Freshwalk.Application.csproj Freshwalk.Application/
COPY Freshwalk.Infrastructure/Freshwalk.Infrastructure.csproj Freshwalk.Infrastructure/
COPY Freshwalk.API/Freshwalk.API.csproj Freshwalk.API/

RUN dotnet restore Freshwalk.API/Freshwalk.API.csproj

COPY . .
RUN dotnet publish Freshwalk.API/Freshwalk.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Freshwalk.API.dll"]
