# Parameterized multi-stage build for all FirstBrick services.
# Pass PROJECT=Account|Payment|Investment|Portfolio as a build arg.

ARG PROJECT

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src

# Copy csproj files first for better layer caching
COPY FirstBrick.slnx ./
COPY src/Shared/FirstBrick.Shared/FirstBrick.Shared.csproj src/Shared/FirstBrick.Shared/
COPY src/Account/FirstBrick.Account.Api/FirstBrick.Account.Api.csproj src/Account/FirstBrick.Account.Api/
COPY src/Payment/FirstBrick.Payment.Api/FirstBrick.Payment.Api.csproj src/Payment/FirstBrick.Payment.Api/
COPY src/Investment/FirstBrick.Investment.Api/FirstBrick.Investment.Api.csproj src/Investment/FirstBrick.Investment.Api/
COPY src/Portfolio/FirstBrick.Portfolio.Api/FirstBrick.Portfolio.Api.csproj src/Portfolio/FirstBrick.Portfolio.Api/

RUN dotnet restore src/${PROJECT}/FirstBrick.${PROJECT}.Api/FirstBrick.${PROJECT}.Api.csproj

# Copy the rest of the source and publish
COPY src/ src/
RUN dotnet publish src/${PROJECT}/FirstBrick.${PROJECT}.Api/FirstBrick.${PROJECT}.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG PROJECT
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DLL_NAME=FirstBrick.${PROJECT}.Api.dll

EXPOSE 8080

# Use shell form so $DLL_NAME expands; exec so signals propagate to the .NET process.
ENTRYPOINT ["sh", "-c", "exec dotnet $DLL_NAME"]
