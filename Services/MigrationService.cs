using XR50TrainingAssetRepo.Models;
// Core .NET namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// ASP.NET Core namespaces
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Entity Framework namespaces
using Microsoft.EntityFrameworkCore;

// OR if using modern Microsoft.Data.SqlClient:
using Microsoft.Data.SqlClient;


// XR50 Migration Service
public class XR50MigrationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<XR50MigrationService> _logger;

    public XR50MigrationService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<XR50MigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CreateTenantSchemaAsync(XR50Tenant tenant)
    {
        var connectionString = _configuration.GetConnectionString("XR50Database");
        var schemaName = $"tenant_{Regex.Replace(tenant.TenantName, @"[^a-zA-Z0-9_]", "_")}";

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Create the schema
            var createSchemaCommand = new SqlCommand($"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schemaName}') EXEC('CREATE SCHEMA [{schemaName}]')", connection, transaction);
            await createSchemaCommand.ExecuteNonQueryAsync();

            // 2. Create training tables in the new schema
            await CreateTrainingTablesInSchemaAsync(connection, transaction, schemaName);

            // 3. Store tenant metadata
            await StoreTenantMetadataAsync(connection, transaction, tenant, schemaName);

            // 4. Create tenant directory if specified
            if (!string.IsNullOrEmpty(tenant.TenantDirectory))
            {
                CreateTenantDirectory(tenant.TenantDirectory);
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Tenant schema {SchemaName} created successfully", schemaName);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create tenant schema {SchemaName}", schemaName);
            throw;
        }
    }

    private async Task CreateTrainingTablesInSchemaAsync(SqlConnection connection, SqlTransaction transaction, string schemaName)
    {
        var createTablesScript = $@"
            -- Programs table
            CREATE TABLE [{schemaName}].[Programs] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [Name] nvarchar(255) NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_{schemaName}_Programs] PRIMARY KEY ([Id])
            );
            
            CREATE INDEX [IX_{schemaName}_Programs_Name] ON [{schemaName}].[Programs] ([Name]);
            CREATE INDEX [IX_{schemaName}_Programs_CreatedAt] ON [{schemaName}].[Programs] ([CreatedAt]);

            -- Materials table
            CREATE TABLE [{schemaName}].[Materials] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [Name] nvarchar(255) NOT NULL,
                [Description] nvarchar(1000) NULL,
                [Type] int NOT NULL,
                [Value] int NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_{schemaName}_Materials] PRIMARY KEY ([Id])
            );
            
            CREATE INDEX [IX_{schemaName}_Materials_Type] ON [{schemaName}].[Materials] ([Type]);
            CREATE INDEX [IX_{schemaName}_Materials_Name] ON [{schemaName}].[Materials] ([Name]);

            -- Assets table
            CREATE TABLE [{schemaName}].[Assets] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [MaterialsId] int NOT NULL,
                [Src] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_{schemaName}_Assets] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_{schemaName}_Assets_Materials] FOREIGN KEY ([MaterialsId]) 
                    REFERENCES [{schemaName}].[Materials] ([Id]) ON DELETE CASCADE
            );
            
            CREATE INDEX [IX_{schemaName}_Assets_MaterialsId] ON [{schemaName}].[Assets] ([MaterialsId]);
        ";

        var command = new SqlCommand(createTablesScript, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private async Task StoreTenantMetadataAsync(SqlConnection connection, SqlTransaction transaction, XR50Tenant tenant, string schemaName)
    {
        // Store in central tenant registry
        var insertMetadataScript = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'XR50TenantRegistry' AND schema_id = SCHEMA_ID('dbo'))
            BEGIN
                CREATE TABLE [dbo].[XR50TenantRegistry] (
                    [TenantName] nvarchar(100) NOT NULL PRIMARY KEY,
                    [TenantGroup] nvarchar(100) NULL,
                    [Description] nvarchar(500) NULL,
                    [TenantDirectory] nvarchar(500) NULL,
                    [OwnerName] nvarchar(255) NULL,
                    [SchemaName] nvarchar(100) NOT NULL,
                    [TrainingProgramListJson] nvarchar(max) NULL,
                    [AdminListJson] nvarchar(max) NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [IsActive] bit NOT NULL DEFAULT 1
                );
            END

            INSERT INTO [dbo].[XR50TenantRegistry] 
                ([TenantName], [TenantGroup], [Description], [TenantDirectory], [OwnerName], [SchemaName], 
                 [TrainingProgramListJson], [AdminListJson], [CreatedAt], [IsActive])
            VALUES 
                (@tenantName, @tenantGroup, @description, @tenantDirectory, @ownerName, @schemaName,
                 @trainingProgramsJson, @adminListJson, @createdAt, 1);
        ";

        var command = new SqlCommand(insertMetadataScript, connection, transaction);
        command.Parameters.AddWithValue("@tenantName", tenant.TenantName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@tenantGroup", tenant.TenantGroup ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@description", tenant.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@tenantDirectory", tenant.TenantDirectory ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ownerName", tenant.OwnerName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@schemaName", schemaName);
        command.Parameters.AddWithValue("@trainingProgramsJson", JsonSerializer.Serialize(tenant.TrainingProgramList ?? new List<string>()));
        command.Parameters.AddWithValue("@adminListJson", JsonSerializer.Serialize(tenant.AdminList ?? new List<string>()));
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync();
    }

    private void CreateTenantDirectory(string tenantDirectory)
    {
        try
        {
            if (!Directory.Exists(tenantDirectory))
            {
                Directory.CreateDirectory(tenantDirectory);
                _logger.LogInformation("Created tenant directory: {TenantDirectory}", tenantDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tenant directory: {TenantDirectory}", tenantDirectory);
            // Don't throw - directory creation failure shouldn't fail tenant creation
        }
    }
}
