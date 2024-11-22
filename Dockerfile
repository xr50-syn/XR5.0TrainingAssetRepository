FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

ENV ASPNETCORE_ENVIRONMENT Development

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out
RUN dotnet tool install  -g dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations add InitialCreate
RUN dotnet ef database update

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .

ENTRYPOINT ["dotnet", "XR5_0TrainingRepo.dll"]