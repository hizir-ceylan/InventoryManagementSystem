# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy solution file and project files
COPY *.sln ./
COPY Inventory.Api/Inventory.Api.csproj ./Inventory.Api/
COPY Inventory.Data/Inventory.Data.csproj ./Inventory.Data/
COPY Inventory.Domain/Inventory.Domain.csproj ./Inventory.Domain/
COPY Inventory.Shared/Inventory.Shared.csproj ./Inventory.Shared/
COPY Inventory.Agent.Windows/Inventory.Agent.Windows.csproj ./Inventory.Agent.Windows/

# Restore dependencies
RUN dotnet restore

# Copy the entire source code
COPY . .

# Build the API project
WORKDIR /src/Inventory.Api
RUN dotnet build -c Release -o /app/build

# Publish the API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Install necessary tools for network scanning and system monitoring
RUN apt-get update && apt-get install -y \
    iputils-ping \
    net-tools \
    iproute2 \
    dnsutils \
    curl \
    wget \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Copy the published app
COPY --from=build /app/publish .

# Create directories for logs and data
RUN mkdir -p /app/ApiLogs /app/Data

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5093

# Expose the port
EXPOSE 5093

# Set the entry point
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]