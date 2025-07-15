using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Models.DTOs;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Services
{
    public interface ITrainingProgramService
    {
        Task<IEnumerable<TrainingProgram>> GetAllTrainingProgramsAsync();
        Task<TrainingProgram?> GetTrainingProgramAsync(int id);
        Task<TrainingProgram> CreateTrainingProgramAsync(TrainingProgram program);
        Task<TrainingProgram> UpdateTrainingProgramAsync(TrainingProgram program);
        Task<bool> DeleteTrainingProgramAsync(int id);
        Task<bool> TrainingProgramExistsAsync(int id);
        Task<bool> AssignMaterialToTrainingProgramAsync(int trainingProgramId, int materialId);
        Task<bool> RemoveMaterialFromTrainingProgramAsync(int trainingProgramId, int materialId);
        Task<IEnumerable<Material>> GetMaterialsByTrainingProgramAsync(int trainingProgramId);
        Task<CompleteTrainingProgramResponse> CreateCompleteTrainingProgramAsync(CompleteTrainingProgramRequest request);
        Task<CompleteTrainingProgramResponse?> GetCompleteTrainingProgramAsync(int id);
        Task<IEnumerable<CompleteTrainingProgramResponse>> GetAllCompleteTrainingProgramsAsync();
    }

    public class TrainingProgramService : ITrainingProgramService
    {
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly ILogger<TrainingProgramService> _logger;

        public TrainingProgramService(
            IXR50TenantDbContextFactory dbContextFactory,
            ILogger<TrainingProgramService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<TrainingProgram>> GetAllTrainingProgramsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.TrainingPrograms
                .Include(tp => tp.ProgramLearningPaths)  // Show learning path associations
                .Include(tp => tp.ProgramMaterials)      // Show material associations (if you have this)
                .ToListAsync();
        }

        public async Task<TrainingProgram?> GetTrainingProgramAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.TrainingPrograms
                .Include(tp => tp.ProgramLearningPaths)  // Show learning path associations
                .Include(tp => tp.ProgramMaterials)      // Show material associations (if you have this)
                .FirstOrDefaultAsync(tp => tp.Id == id);
        }
        public async Task<TrainingProgram> CreateTrainingProgramAsync(TrainingProgram program)
        {
            using var context = _dbContextFactory.CreateDbContext();

            // Set creation timestamp if not already set
            if (string.IsNullOrEmpty(program.Created_at))
            {
                program.Created_at = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            }

            context.TrainingPrograms.Add(program);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created training program: {Name} with ID: {Id}", program.Name, program.Id);

            return program;
        }

        public async Task<TrainingProgram> UpdateTrainingProgramAsync(TrainingProgram program)
        {
            using var context = _dbContextFactory.CreateDbContext();

            context.Entry(program).State = EntityState.Modified;
            await context.SaveChangesAsync();

            _logger.LogInformation("Updated training program: {Id}", program.Id);

            return program;
        }

        public async Task<bool> DeleteTrainingProgramAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var program = await context.TrainingPrograms.FindAsync(id);
            if (program == null)
            {
                return false;
            }

            context.TrainingPrograms.Remove(program);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted training program: {Id}", id);

            return true;
        }

        public async Task<bool> TrainingProgramExistsAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.TrainingPrograms.AnyAsync(e => e.Id == id);
        }
        // Add these methods to TrainingProgramService class:

        #region Simple Material Assignment (ProgramMaterial Junction Table)

        /// <summary>
        /// Assign a material to training program using simple junction table
        /// </summary>
        public async Task<bool> AssignMaterialToTrainingProgramAsync(int trainingProgramId, int materialId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            // Check if relationship already exists
            var exists = await context.ProgramMaterials
                .AnyAsync(pm => pm.MaterialId == materialId && pm.TrainingProgramId == trainingProgramId);

            if (exists)
            {
                _logger.LogWarning("Material {MaterialId} already assigned to training program {ProgramId}",
                    materialId, trainingProgramId);
                return false; // Already exists
            }

            // Verify both entities exist
            var materialExists = await context.Materials.AnyAsync(m => m.Id == materialId);
            var programExists = await context.TrainingPrograms.AnyAsync(tp => tp.Id == trainingProgramId);

            if (!materialExists)
            {
                _logger.LogError("Material {MaterialId} not found", materialId);
                throw new ArgumentException($"Material with ID {materialId} not found");
            }

            if (!programExists)
            {
                _logger.LogError("Training program {ProgramId} not found", trainingProgramId);
                throw new ArgumentException($"Training program with ID {trainingProgramId} not found");
            }

            // Create the relationship
            var programMaterial = new ProgramMaterial
            {
                MaterialId = materialId,
                TrainingProgramId = trainingProgramId
            };

            context.ProgramMaterials.Add(programMaterial);
            await context.SaveChangesAsync();

            _logger.LogInformation("Successfully assigned material {MaterialId} to training program {ProgramId}",
                materialId, trainingProgramId);

            return true;
        }

        /// <summary>
        /// Remove a material from training program using simple junction table
        /// </summary>
        public async Task<bool> RemoveMaterialFromTrainingProgramAsync(int trainingProgramId, int materialId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var programMaterial = await context.ProgramMaterials
                .FirstOrDefaultAsync(pm => pm.MaterialId == materialId && pm.TrainingProgramId == trainingProgramId);

            if (programMaterial == null)
            {
                _logger.LogWarning("Material {MaterialId} not assigned to training program {ProgramId}",
                    materialId, trainingProgramId);
                return false;
            }

            context.ProgramMaterials.Remove(programMaterial);
            await context.SaveChangesAsync();

            _logger.LogInformation("Successfully removed material {MaterialId} from training program {ProgramId}",
                materialId, trainingProgramId);

            return true;
        }

        /// <summary>
        /// Get all materials assigned to training program via simple junction table
        /// </summary>
        public async Task<IEnumerable<Material>> GetMaterialsByTrainingProgramAsync(int trainingProgramId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var materials = await context.ProgramMaterials
                .Where(pm => pm.TrainingProgramId == trainingProgramId)
                .Include(pm => pm.Material)
                .Select(pm => pm.Material)
                .ToListAsync();

            _logger.LogInformation("Found {Count} materials for training program {ProgramId}",
                materials.Count, trainingProgramId);

            return materials;
        }

        #endregion
        #region Complete Training Program Operations

        /// <summary>
        /// Create a complete training program with materials and learning paths in one transaction
        /// </summary>
        public async Task<CompleteTrainingProgramResponse> CreateCompleteTrainingProgramAsync(CompleteTrainingProgramRequest request)
        {
            using var context = _dbContextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 1. Create the training program
                var program = new TrainingProgram
                {
                    Name = request.Name,
                    Created_at = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                context.TrainingPrograms.Add(program);
                await context.SaveChangesAsync(); // Save to get the ID

                _logger.LogInformation("Created training program: {Name} with ID: {Id}", program.Name, program.Id);

                // 2. Create new materials if specified
                var createdMaterialIds = new List<int>();
                if (request.MaterialsToCreate != null && request.MaterialsToCreate.Any())
                {
                    foreach (var materialRequest in request.MaterialsToCreate)
                    {
                        var material = CreateMaterialFromRequest(materialRequest);
                        context.Materials.Add(material);
                        await context.SaveChangesAsync(); // Save to get ID
                        
                        createdMaterialIds.Add(material.Id);
                        _logger.LogInformation("Created material: {Name} with ID: {Id}", material.Name, material.Id);
                    }
                }

                // 3. Combine existing material IDs with newly created ones
                var allMaterialIds = request.MaterialIds.Concat(createdMaterialIds).Distinct().ToList();

                // 4. Assign materials to the program
                if (allMaterialIds.Any())
                {
                    var assignments = allMaterialIds.Select(materialId => new ProgramMaterial
                    {
                        TrainingProgramId = program.Id,
                        MaterialId = materialId
                    }).ToList();

                    context.ProgramMaterials.AddRange(assignments);
                    _logger.LogInformation("Assigning {Count} materials to program {ProgramId}", 
                        allMaterialIds.Count, program.Id);
                }

                // 5. Assign learning paths to the program
                if (request.LearningPathIds.Any())
                {
                    var pathAssignments = request.LearningPathIds.Select(pathId => new ProgramLearningPath
                    {
                        TrainingProgramId = program.Id,
                        LearningPathId = pathId
                    }).ToList();

                    context.ProgramLearningPaths.AddRange(pathAssignments);
                    _logger.LogInformation("Assigning {Count} learning paths to program {ProgramId}", 
                        request.LearningPathIds.Count, program.Id);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully created complete training program {ProgramId} with {MaterialCount} materials and {PathCount} learning paths",
                    program.Id, allMaterialIds.Count, request.LearningPathIds.Count);

                // 6. Return the complete response
                return await GetCompleteTrainingProgramAsync(program.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create complete training program: {Name}", request.Name);
                throw;
            }
        }

        /// <summary>
        /// Get a complete training program with all materials and learning paths
        /// </summary>
        public async Task<CompleteTrainingProgramResponse?> GetCompleteTrainingProgramAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var program = await context.TrainingPrograms
                .Include(tp => tp.ProgramMaterials)
                    .ThenInclude(pm => pm.Material)
                .Include(tp => tp.ProgramLearningPaths)
                    .ThenInclude(plp => plp.LearningPath)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (program == null)
            {
                return null;
            }

            // Get materials with their complete information
            var materials = new List<MaterialResponse>();
            foreach (var pm in program.ProgramMaterials)
            {
                var materialResponse = await BuildMaterialResponse(pm.Material);
                materials.Add(materialResponse);
            }

            // Get learning paths
            var learningPaths = program.ProgramLearningPaths.Select(plp => new LearningPathResponse
            {
                Id = plp.LearningPath.Id,
                LearningPathName = plp.LearningPath.LearningPathName,
                Description = plp.LearningPath.Description
            }).ToList();

            // Build summary
            var summary = new TrainingProgramSummary
            {
                TotalMaterials = materials.Count,
                TotalLearningPaths = learningPaths.Count,
                MaterialsByType = materials.GroupBy(m => m.Type).ToDictionary(g => g.Key, g => g.Count()),
                LastUpdated = materials.Any() ? materials.Max(m => m.Updated_at) : program.Created_at != null ? DateTime.Parse(program.Created_at) : null
            };

            _logger.LogInformation("Retrieved complete training program {Id}: {MaterialCount} materials, {PathCount} learning paths",
                id, materials.Count, learningPaths.Count);

            return new CompleteTrainingProgramResponse
            {
                Id = program.Id,
                Name = program.Name,
                Created_at = program.Created_at,
                Materials = materials,
                LearningPaths = learningPaths,
                Summary = summary
            };
        }

        /// <summary>
        /// Get all training programs with complete information
        /// </summary>
        public async Task<IEnumerable<CompleteTrainingProgramResponse>> GetAllCompleteTrainingProgramsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();

            var programs = await context.TrainingPrograms.ToListAsync();
            var results = new List<CompleteTrainingProgramResponse>();

            foreach (var program in programs)
            {
                var completeProgram = await GetCompleteTrainingProgramAsync(program.Id);
                if (completeProgram != null)
                {
                    results.Add(completeProgram);
                }
            }

            _logger.LogInformation("Retrieved {Count} complete training programs", results.Count);
            return results;
        }

        #endregion

        #region Helper Methods

        private Material CreateMaterialFromRequest(MaterialCreationRequest request)
        {
            Material material = request.MaterialType.ToLower() switch
            {
                "video" => new VideoMaterial
                {
                    AssetId = request.AssetId,
                    VideoPath = request.VideoPath,
                    VideoDuration = request.VideoDuration,
                    VideoResolution = request.VideoResolution
                },
                "image" => new ImageMaterial
                {
                    AssetId = request.AssetId,
                    ImagePath = request.ImagePath
                },
                "chatbot" => new ChatbotMaterial
                {
                    ChatbotConfig = request.ChatbotConfig
                },
                "mqtt_template" => new MQTT_TemplateMaterial
                {
                    message_type = request.MessageType,
                    message_text = request.MessageText
                },
                "checklist" => new ChecklistMaterial(),
                "workflow" => new WorkflowMaterial(),
                "pdf" => new PDFMaterial { AssetId = request.AssetId },
                "unitydemo" => new UnityDemoMaterial { AssetId = request.AssetId },
                "questionnaire" => new QuestionnaireMaterial(),
                _ => new DefaultMaterial { AssetId = request.AssetId }
            };

            material.Name = request.Name;
            material.Description = request.Description;
            material.Created_at = DateTime.UtcNow;
            material.Updated_at = DateTime.UtcNow;

            return material;
        }

        private async Task<MaterialResponse> BuildMaterialResponse(Material material)
        {
            var response = new MaterialResponse
            {
                Id = material.Id,
                Name = material.Name,
                Description = material.Description,
                Type = material.Type.ToString(),
                Created_at = material.Created_at,
                Updated_at = material.Updated_at,
                Assignment = new AssignmentMetadata { AssignmentType = "Simple" }
            };

            // Add type-specific properties
            switch (material)
            {
                case VideoMaterial video:
                    response.AssetId = video.AssetId;
                    response.TypeSpecificProperties = new Dictionary<string, object?>
                    {
                        ["VideoPath"] = video.VideoPath,
                        ["VideoDuration"] = video.VideoDuration,
                        ["VideoResolution"] = video.VideoResolution
                    };
                    break;

                case ImageMaterial image:
                    response.AssetId = image.AssetId;
                    response.TypeSpecificProperties = new Dictionary<string, object?>
                    {
                        ["ImagePath"] = image.ImagePath,
                        ["ImageWidth"] = image.ImageWidth,
                        ["ImageHeight"] = image.ImageHeight,
                        ["ImageFormat"] = image.ImageFormat
                    };
                    break;

                case PDFMaterial pdf:
                    response.AssetId = pdf.AssetId;
                    response.TypeSpecificProperties = new Dictionary<string, object?>
                    {
                        ["PdfPath"] = pdf.PdfPath,
                        ["PdfPageCount"] = pdf.PdfPageCount,
                        ["PdfFileSize"] = pdf.PdfFileSize
                    };
                    break;

                case ChatbotMaterial chatbot:
                    response.TypeSpecificProperties = new Dictionary<string, object?>
                    {
                        ["ChatbotConfig"] = chatbot.ChatbotConfig,
                        ["ChatbotModel"] = chatbot.ChatbotModel,
                        ["ChatbotPrompt"] = chatbot.ChatbotPrompt
                    };
                    break;

                case MQTT_TemplateMaterial mqtt:
                    response.TypeSpecificProperties = new Dictionary<string, object?>
                    {
                        ["MessageType"] = mqtt.message_type,
                        ["MessageText"] = mqtt.message_text
                    };
                    break;

                case UnityDemoMaterial unity:
                    response.AssetId = unity.AssetId;
                    response.TypeSpecificProperties = new Dictionary<string, object?>
                    {
                        ["UnityVersion"] = unity.UnityVersion,
                        ["UnityBuildTarget"] = unity.UnityBuildTarget,
                        ["UnitySceneName"] = unity.UnitySceneName
                    };
                    break;

                case DefaultMaterial defaultMat:
                    response.AssetId = defaultMat.AssetId;
                    break;
            }

            return response;
        }

        #endregion
    }
    
}