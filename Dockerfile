FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the project files alone so the layer survives source-only changes.
COPY src/ExpenseTracker.API/ExpenseTracker.API.csproj src/ExpenseTracker.API/
COPY src/ExpenseTracker.Application/ExpenseTracker.Application.csproj src/ExpenseTracker.Application/
COPY src/ExpenseTracker.Domain/ExpenseTracker.Domain.csproj src/ExpenseTracker.Domain/
COPY src/ExpenseTracker.Infrastructure/ExpenseTracker.Infrastructure.csproj src/ExpenseTracker.Infrastructure/
RUN dotnet restore src/ExpenseTracker.API/ExpenseTracker.API.csproj

COPY src/ src/
RUN dotnet publish src/ExpenseTracker.API/ExpenseTracker.API.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ExpenseTracker.API.dll"]
