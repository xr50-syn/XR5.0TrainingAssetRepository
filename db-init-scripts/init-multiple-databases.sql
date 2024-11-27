-- Create the first database and user
CREATE DATABASE IF NOT EXISTS some_mariadb;
CREATE USER IF NOT EXISTS 'XR50user'@'%' IDENTIFIED BY 'my-secret-pw';
GRANT ALL PRIVILEGES ON some_mariadb.* TO 'XR50user'@'%';
GRANT ALL PRIVILEGES ON some_mariadb.* TO 'owncloud'@'%';

-- Apply changes
FLUSH PRIVILEGES;
