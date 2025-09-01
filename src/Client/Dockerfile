# Use the official .NET 9.0 SDK image to build the Blazor WASM application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["src/Client/Client.csproj", "src/Client/"]
RUN dotnet restore "src/Client/Client.csproj"

# Copy the source code
COPY ["src/Client/", "src/Client/"]

# Build the Blazor WASM application
WORKDIR "/src/src/Client"
RUN dotnet build "Client.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Client.csproj" -c Release -o /app/publish

# Use nginx to serve the static files
FROM nginx:alpine AS final
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]