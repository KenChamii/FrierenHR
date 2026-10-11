# Build context is the repository root, because the API references four sibling projects.
#   docker build -f FrierenHR.WebAPI/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, on their own layer. Restore then re-runs only when a dependency
# changes, not on every source edit.
COPY FrierenHR.slnx ./
COPY FrierenHR.Core/FrierenHR.Core.csproj FrierenHR.Core/
COPY FrierenHR.Application/FrierenHR.Application.csproj FrierenHR.Application/
COPY FrierenHR.Infrastructure/FrierenHR.Infrastructure.csproj FrierenHR.Infrastructure/
COPY FrierenHR.WebAPI/FrierenHR.WebAPI.csproj FrierenHR.WebAPI/
COPY FrierenHR.Tests/FrierenHR.Tests.csproj FrierenHR.Tests/
RUN dotnet restore FrierenHR.WebAPI/FrierenHR.WebAPI.csproj

COPY . .
RUN dotnet publish FrierenHR.WebAPI/FrierenHR.WebAPI.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Runs as a non-root user. The published output is read-only to it, which is what we want.
USER $APP_UID

# Chat attachments are written here. Mount a volume at this path to keep them across deploys;
# without one they live and die with the container.
VOLUME /app/wwwroot/uploads

# Kestrel listens on 8080 rather than the default 5000/5001 pair, because TLS is terminated
# by the reverse proxy in front of this container, not here.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app .
ENTRYPOINT ["dotnet", "FrierenHR.WebAPI.dll"]
