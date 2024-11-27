-- Create the first database and user
CREATE DATABASE IF NOT EXISTS magical_library;
CREATE USER IF NOT EXISTS 'amy'@'%' IDENTIFIED BY '3mm13';
GRANT ALL PRIVILEGES ON magical_library.* TO 'amy'@'%';

-- Apply changes
FLUSH PRIVILEGES;
