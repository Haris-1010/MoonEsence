FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY src/Ecommerce.Web/*.csproj ./src/Ecommerce.Web/
COPY src/Ecommerce.Application/*.csproj ./src/Ecommerce.Application/
COPY src/Ecommerce.Infrastructure/*.csproj ./src/Ecommerce.Infrastructure/
COPY src/Ecommerce.Domain/*.csproj ./src/Ecommerce.Domain/
RUN dotnet restore src/Ecommerce.Web/Ecommerce.Web.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish src/Ecommerce.Web/Ecommerce.Web.csproj -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install required libraries for PostgreSQL
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/out .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "Ecommerce.Web.dll"]