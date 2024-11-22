# Stage 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy project files and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy the rest of the application and publish
COPY . .
RUN dotnet restore XR5_0TrainingRepo.csproj
RUN dotnet publish XR5_0TrainingRepo.csproj --no-restore -c Release -o /source/app

# Stage 2: Run the app
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /source/app .

# Run the app
ENTRYPOINT ["dotnet", "XR5_0TrainingRepo.dll"]
