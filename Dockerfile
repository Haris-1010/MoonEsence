FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
 
COPY src/Ecommerce.Web/*.csproj ./src/Ecommerce.Web/
COPY src/Ecommerce.Application/*.csproj ./src/Ecommerce.Application/
COPY src/Ecommerce.Infrastructure/*.csproj ./src/Ecommerce.Infrastructure/
COPY src/Ecommerce.Domain/*.csproj ./src/Ecommerce.Domain/
RUN dotnet restore src/Ecommerce.Web/Ecommerce.Web.csproj
 
COPY . .
RUN dotnet publish src/Ecommerce.Web/Ecommerce.Web.csproj -c Release -o out
 
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
 
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
 
COPY --from=build /app/out .
 
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
 
EXPOSE 8080
 
ENTRYPOINT ["dotnet", "Ecommerce.Web.dll"]