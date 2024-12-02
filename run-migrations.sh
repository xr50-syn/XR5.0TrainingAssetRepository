#!/bin/bash
set -e

echo "Waiting for database to be ready..."

# Wait for MySQL to be ready
until mysql -h mariadb -u${XR50_REPO_DB_USER} -p${XR50_REPO_DB_PASSWORD} -e "SELECT 1;" > /dev/null 2>&1; do
    echo "MySQL is unavailable - sleeping"
    sleep 5
done

echo "MySQL is up - running migrations..."

# Generate migrations if not already generated (not recommended for production)
if [ ! -d "./Migrations" ]; then #TODO FIND A BETTER WAY!!!!!!
  echo "Migrations directory not found. Generating initial migration..."
  dotnet ef migrations add InitialCreate
  # Apply migrations
  dotnet ef database update
else
  echo "Migrations already exist. Skipping migration generation."
fi




# Start the application
echo "Starting the application..."
dotnet run
