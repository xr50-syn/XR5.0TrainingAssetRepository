using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Data;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Controllers
{
    [Route("api/{tenantName}/[controller]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly IMaterialService _materialService;
        private readonly ILearningPathService _learningPathService;
        private readonly ILogger<MaterialsController> _logger;

        public MaterialsController(
            IMaterialService materialService,
            ILearningPathService learningPathService,
            ILogger<MaterialsController> logger)
        {
            _materialService = materialService;
            _learningPathService = learningPathService;
            _logger = logger;
        }

        #region Base Material Operations

        // GET: api/{tenantName}/materials
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterials(string tenantName)
        {
            _logger.LogInformation("Getting materials for tenant: {TenantName}", tenantName);
            
            var materials = await _materialService.GetAllMaterialsAsync();
            
            _logger.LogInformation("Found {MaterialCount} materials for tenant: {TenantName}", 
                materials.Count(), tenantName);
            
            return Ok(materials);
        }

        // GET: api/{tenantName}/materials/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Material>> GetMaterial(string tenantName, int id)
        {
            _logger.LogInformation("Getting material {Id} for tenant: {TenantName}", id, tenantName);
            
            var material = await _materialService.GetMaterialAsync(id);

            if (material == null)
            {
                _logger.LogWarning("Material {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            return material;
        }

        // POST: api/{tenantName}/materials - Generic material creation
        [HttpPost]
        public async Task<ActionResult<Material>> PostMaterial(string tenantName, [FromBody] JsonElement materialData)
        {
            try
            {
                // Parse the incoming JSON to determine material type
                var material = ParseMaterialFromJson(materialData);
                
                if (material == null)
                {
                    return BadRequest("Invalid material data or unsupported material type");
                }

                _logger.LogInformation("Creating material {Name} (Type: {Type}) for tenant: {TenantName}", 
                    material.Name, material.GetType().Name, tenantName);
                
                var createdMaterial = await _materialService.CreateMaterialAsync(material);

                _logger.LogInformation("Created material {Name} with ID {Id} for tenant: {TenantName}", 
                    createdMaterial.Name, createdMaterial.Id, tenantName);

                return CreatedAtAction(nameof(GetMaterial), 
                    new { tenantName, id = createdMaterial.Id }, 
                    createdMaterial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating material for tenant: {TenantName}", tenantName);
                return StatusCode(500, $"Error creating material: {ex.Message}");
            }
        }

        private Material? ParseMaterialFromJson(JsonElement jsonElement)
        {
            // Get the discriminator/type from the JSON
            string? discriminator = null;
            string? typeValue = null;
            
            if (jsonElement.TryGetProperty("discriminator", out var discProp))
            {
                discriminator = discProp.GetString();
            }
            else if (jsonElement.TryGetProperty("type", out var typeProp))
            {
                typeValue = typeProp.GetString();
            }
            else if (jsonElement.TryGetProperty("Type", out var typeEnumProp))
            {
                typeValue = typeEnumProp.GetString();
            }

            // Create the appropriate material type
            Material material = (discriminator?.ToLower(), typeValue?.ToLower()) switch
            {
                ("videomaterial", _) or (_, "video") => new VideoMaterial(),
                ("imagematerial", _) or (_, "image") => new ImageMaterial(),
                ("checklistmaterial", _) or (_, "checklist") => new ChecklistMaterial(),
                ("workflowmaterial", _) or (_, "workflow") => new WorkflowMaterial(),
                ("pdfmaterial", _) or (_, "pdf") => new PDFMaterial(),
                ("unitydemo", _) or (_, "unitydemo") => new UnityDemoMaterial(),
                ("chatbotmaterial", _) or (_, "chatbot") => new ChatbotMaterial(),
                ("questionnairematerial", _) or (_, "questionnaire") => new QuestionnaireMaterial(),
                ("mqtt_templatematerial", _) or (_, "mqtt_template") => new MQTT_TemplateMaterial(),
                ("defaultmaterial", _) or (_, "default") => new DefaultMaterial(),
                _ => new DefaultMaterial() // Default fallback
            };

            // Populate common properties
            if (jsonElement.TryGetProperty("name", out var nameProp))
                material.Name = nameProp.GetString();
            
            if (jsonElement.TryGetProperty("description", out var descProp))
                material.Description = descProp.GetString();

            // Populate type-specific properties
            PopulateTypeSpecificProperties(material, jsonElement);

            return material;
        }

        private void PopulateTypeSpecificProperties(Material material, JsonElement jsonElement)
        {
            switch (material)
            {
                case MQTT_TemplateMaterial mqtt:
                    if (jsonElement.TryGetProperty("message_type", out var msgType))
                        mqtt.message_type = msgType.GetString();
                    if (jsonElement.TryGetProperty("message_text", out var msgText))
                        mqtt.message_text = msgText.GetString();
                    break;
                    
                case UnityDemoMaterial unity:
                    if (jsonElement.TryGetProperty("assetId", out var unityAssetId))
                        unity.AssetId = unityAssetId.GetString();
                    if (jsonElement.TryGetProperty("unityVersion", out var version))
                        unity.UnityVersion = version.GetString();
                    if (jsonElement.TryGetProperty("unityBuildTarget", out var buildTarget))
                        unity.UnityBuildTarget = buildTarget.GetString();
                    if (jsonElement.TryGetProperty("unitySceneName", out var sceneName))
                        unity.UnitySceneName = sceneName.GetString();
                    break;
                    
                case DefaultMaterial defaultMat:
                    if (jsonElement.TryGetProperty("assetId", out var defaultAssetId))
                        defaultMat.AssetId = defaultAssetId.GetString();
                    break;
                    
                case VideoMaterial video:
                    if (jsonElement.TryGetProperty("assetId", out var videoAssetId))
                        video.AssetId = videoAssetId.GetString();
                    if (jsonElement.TryGetProperty("videoPath", out var videoPath))
                        video.VideoPath = videoPath.GetString();
                    if (jsonElement.TryGetProperty("videoDuration", out var duration))
                        video.VideoDuration = duration.GetInt32();
                    if (jsonElement.TryGetProperty("videoResolution", out var resolution))
                        video.VideoResolution = resolution.GetString();
                    break;
                    
                case ImageMaterial image:
                    if (jsonElement.TryGetProperty("assetId", out var imageAssetId))
                        image.AssetId = imageAssetId.GetString();
                    if (jsonElement.TryGetProperty("imagePath", out var imagePath))
                        image.ImagePath = imagePath.GetString();
                    if (jsonElement.TryGetProperty("imageWidth", out var width))
                        image.ImageWidth = width.GetInt32();
                    if (jsonElement.TryGetProperty("imageHeight", out var height))
                        image.ImageHeight = height.GetInt32();
                    if (jsonElement.TryGetProperty("imageFormat", out var format))
                        image.ImageFormat = format.GetString();
                    break;
                    
                case PDFMaterial pdf:
                    if (jsonElement.TryGetProperty("assetId", out var pdfAssetId))
                        pdf.AssetId = pdfAssetId.GetString();
                    if (jsonElement.TryGetProperty("pdfPath", out var pdfPath))
                        pdf.PdfPath = pdfPath.GetString();
                    if (jsonElement.TryGetProperty("pdfPageCount", out var pageCount))
                        pdf.PdfPageCount = pageCount.GetInt32();
                    if (jsonElement.TryGetProperty("pdfFileSize", out var fileSize))
                        pdf.PdfFileSize = fileSize.GetInt64();
                    break;
                    
                case ChatbotMaterial chatbot:
                    if (jsonElement.TryGetProperty("chatbotConfig", out var config))
                        chatbot.ChatbotConfig = config.GetString();
                    if (jsonElement.TryGetProperty("chatbotModel", out var model))
                        chatbot.ChatbotModel = model.GetString();
                    if (jsonElement.TryGetProperty("chatbotPrompt", out var prompt))
                        chatbot.ChatbotPrompt = prompt.GetString();
                    break;
                    
                case QuestionnaireMaterial questionnaire:
                    if (jsonElement.TryGetProperty("questionnaireConfig", out var qConfig))
                        questionnaire.QuestionnaireConfig = qConfig.GetString();
                    if (jsonElement.TryGetProperty("questionnaireType", out var qType))
                        questionnaire.QuestionnaireType = qType.GetString();
                    if (jsonElement.TryGetProperty("passingScore", out var score))
                        questionnaire.PassingScore = score.GetDecimal();
                    break;
            }
        }

        // PUT: api/{tenantName}/materials/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMaterial(string tenantName, int id, Material material)
        {
            if (id != material.Id)
            {
                return BadRequest("ID mismatch");
            }

            _logger.LogInformation("Updating material {Id} for tenant: {TenantName}", id, tenantName);
            
            try
            {
                await _materialService.UpdateMaterialAsync(material);
                _logger.LogInformation("Updated material {Id} for tenant: {TenantName}", id, tenantName);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _materialService.MaterialExistsAsync(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/{tenantName}/materials/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(string tenantName, int id)
        {
            _logger.LogInformation("Deleting material {Id} for tenant: {TenantName}", id, tenantName);
            
            var deleted = await _materialService.DeleteMaterialAsync(id);
            
            if (!deleted)
            {
                return NotFound();
            }

            _logger.LogInformation("Deleted material {Id} for tenant: {TenantName}", id, tenantName);

            return NoContent();
        }

        #endregion

        #region Material Type-Specific Endpoints

        // GET: api/{tenantName}/materials/videos
        [HttpGet("videos")]
        public async Task<ActionResult<IEnumerable<VideoMaterial>>> GetVideoMaterials(string tenantName)
        {
            _logger.LogInformation("🎥 Getting video materials for tenant: {TenantName}", tenantName);
            
            var videos = await _materialService.GetAllVideoMaterialsAsync();
            
            _logger.LogInformation("Found {Count} video materials for tenant: {TenantName}", 
                videos.Count(), tenantName);
            
            return Ok(videos);
        }

        // GET: api/{tenantName}/materials/checklists
        [HttpGet("checklists")]
        public async Task<ActionResult<IEnumerable<ChecklistMaterial>>> GetChecklistMaterials(string tenantName)
        {
            _logger.LogInformation("📋 Getting checklist materials for tenant: {TenantName}", tenantName);
            
            var checklists = await _materialService.GetAllChecklistMaterialsAsync();
            
            _logger.LogInformation("Found {Count} checklist materials for tenant: {TenantName}", 
                checklists.Count(), tenantName);
            
            return Ok(checklists);
        }

        // GET: api/{tenantName}/materials/workflows
        [HttpGet("workflows")]
        public async Task<ActionResult<IEnumerable<WorkflowMaterial>>> GetWorkflowMaterials(string tenantName)
        {
            _logger.LogInformation("⚙️ Getting workflow materials for tenant: {TenantName}", tenantName);
            
            var workflows = await _materialService.GetAllWorkflowMaterialsAsync();
            
            _logger.LogInformation("Found {Count} workflow materials for tenant: {TenantName}", 
                workflows.Count(), tenantName);
            
            return Ok(workflows);
        }

        // GET: api/{tenantName}/materials/images
        [HttpGet("images")]
        public async Task<ActionResult<IEnumerable<ImageMaterial>>> GetImageMaterials(string tenantName)
        {
            _logger.LogInformation("🖼️ Getting image materials for tenant: {TenantName}", tenantName);
            
            var images = await _materialService.GetAllImageMaterialsAsync();
            
            _logger.LogInformation("Found {Count} image materials for tenant: {TenantName}", 
                images.Count(), tenantName);
            
            return Ok(images);
        }

        // GET: api/{tenantName}/materials/pdfs
        [HttpGet("pdfs")]
        public async Task<ActionResult<IEnumerable<PDFMaterial>>> GetPDFMaterials(string tenantName)
        {
            _logger.LogInformation("📄 Getting PDF materials for tenant: {TenantName}", tenantName);
            
            var pdfs = await _materialService.GetAllPDFMaterialsAsync();
            
            _logger.LogInformation("Found {Count} PDF materials for tenant: {TenantName}", 
                pdfs.Count(), tenantName);
            
            return Ok(pdfs);
        }

        // GET: api/{tenantName}/materials/chatbots
        [HttpGet("chatbots")]
        public async Task<ActionResult<IEnumerable<ChatbotMaterial>>> GetChatbotMaterials(string tenantName)
        {
            _logger.LogInformation("🤖 Getting chatbot materials for tenant: {TenantName}", tenantName);
            
            var chatbots = await _materialService.GetAllChatbotMaterialsAsync();
            
            _logger.LogInformation("Found {Count} chatbot materials for tenant: {TenantName}", 
                chatbots.Count(), tenantName);
            
            return Ok(chatbots);
        }

        // GET: api/{tenantName}/materials/questionnaires
        [HttpGet("questionnaires")]
        public async Task<ActionResult<IEnumerable<QuestionnaireMaterial>>> GetQuestionnaireMaterials(string tenantName)
        {
            _logger.LogInformation("❓ Getting questionnaire materials for tenant: {TenantName}", tenantName);
            
            var questionnaires = await _materialService.GetAllQuestionnaireMaterialsAsync();
            
            _logger.LogInformation("Found {Count} questionnaire materials for tenant: {TenantName}", 
                questionnaires.Count(), tenantName);
            
            return Ok(questionnaires);
        }

        // GET: api/{tenantName}/materials/mqtt-templates
        [HttpGet("mqtt-templates")]
        public async Task<ActionResult<IEnumerable<MQTT_TemplateMaterial>>> GetMQTTTemplateMaterials(string tenantName)
        {
            _logger.LogInformation("📡 Getting MQTT template materials for tenant: {TenantName}", tenantName);
            
            var templates = await _materialService.GetAllMQTTTemplateMaterialsAsync();
            
            _logger.LogInformation("Found {Count} MQTT template materials for tenant: {TenantName}", 
                templates.Count(), tenantName);
            
            return Ok(templates);
        }

        // GET: api/{tenantName}/materials/unity-demos
        [HttpGet("unity-demos")]
        public async Task<ActionResult<IEnumerable<UnityDemoMaterial>>> GetUnityDemoMaterials(string tenantName)
        {
            _logger.LogInformation("🎮 Getting Unity demo materials for tenant: {TenantName}", tenantName);
            
            var unityDemos = await _materialService.GetAllUnityDemoMaterialsAsync();
            
            _logger.LogInformation("Found {Count} Unity demo materials for tenant: {TenantName}", 
                unityDemos.Count(), tenantName);
            
            return Ok(unityDemos);
        }

        #endregion

        #region Complex Material Creation Endpoints (One-shot creation with child entities)

        // POST: api/{tenantName}/materials/workflow-complete
        [HttpPost("workflow-complete")]
        public async Task<ActionResult<WorkflowMaterial>> CreateCompleteWorkflow(
            string tenantName, 
            [FromBody] CompleteWorkflowRequest request)
        {
            _logger.LogInformation("⚙️ Creating complete workflow {Name} with {StepCount} steps for tenant: {TenantName}", 
                request.Workflow.Name, request.Steps?.Count ?? 0, tenantName);
            
            try
            {
                var createdMaterial = await _materialService.CreateWorkflowWithStepsAsync(
                    request.Workflow, 
                    request.Steps);
                
                return CreatedAtAction(nameof(GetMaterial), 
                    new { tenantName, id = createdMaterial.Id }, 
                    createdMaterial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating complete workflow for tenant: {TenantName}", tenantName);
                return StatusCode(500, $"Error creating workflow: {ex.Message}");
            }
        }

        // POST: api/{tenantName}/materials/video-complete  
        [HttpPost("video-complete")]
        public async Task<ActionResult<VideoMaterial>> CreateCompleteVideo(
            string tenantName,
            [FromBody] CompleteVideoRequest request)
        {
            _logger.LogInformation("🎥 Creating complete video {Name} with {TimestampCount} timestamps for tenant: {TenantName}", 
                request.Video.Name, request.Timestamps?.Count ?? 0, tenantName);
            
            try
            {
                var createdMaterial = await _materialService.CreateVideoWithTimestampsAsync(
                    request.Video, 
                    request.Timestamps);
                
                return CreatedAtAction(nameof(GetMaterial), 
                    new { tenantName, id = createdMaterial.Id }, 
                    createdMaterial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating complete video for tenant: {TenantName}", tenantName);
                return StatusCode(500, $"Error creating video: {ex.Message}");
            }
        }

        // POST: api/{tenantName}/materials/checklist-complete
        [HttpPost("checklist-complete")]
        public async Task<ActionResult<ChecklistMaterial>> CreateCompleteChecklist(
            string tenantName,
            [FromBody] CompleteChecklistRequest request)
        {
            _logger.LogInformation("📋 Creating complete checklist {Name} with {EntryCount} entries for tenant: {TenantName}", 
                request.Checklist.Name, request.Entries?.Count ?? 0, tenantName);
            
            try
            {
                var createdMaterial = await _materialService.CreateChecklistWithEntriesAsync(
                    request.Checklist, 
                    request.Entries);
                
                return CreatedAtAction(nameof(GetMaterial), 
                    new { tenantName, id = createdMaterial.Id }, 
                    createdMaterial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating complete checklist for tenant: {TenantName}", tenantName);
                return StatusCode(500, $"Error creating checklist: {ex.Message}");
            }
        }

        #endregion

        #region Video Material Management

        // GET: api/{tenantName}/materials/videos/5/with-timestamps
        [HttpGet("videos/{id}/with-timestamps")]
        public async Task<ActionResult<VideoMaterial>> GetVideoWithTimestamps(string tenantName, int id)
        {
            _logger.LogInformation("🎥 Getting video material {Id} with timestamps for tenant: {TenantName}", 
                id, tenantName);
            
            var video = await _materialService.GetVideoMaterialWithTimestampsAsync(id);
            
            if (video == null)
            {
                _logger.LogWarning("Video material {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            _logger.LogInformation("Retrieved video material {Id} with {Count} timestamps for tenant: {TenantName}", 
                id, video.VideoTimestamps?.Count() ?? 0, tenantName);
            
            return Ok(video);
        }

        // POST: api/{tenantName}/materials/videos/5/timestamps
        [HttpPost("videos/{videoId}/timestamps")]
        public async Task<ActionResult<VideoMaterial>> AddTimestampToVideo(string tenantName, int videoId, VideoTimestamp timestamp)
        {
            _logger.LogInformation("⏱️ Adding timestamp '{Title}' to video {VideoId} for tenant: {TenantName}", 
                timestamp.Title, videoId, tenantName);
            
            try
            {
                var video = await _materialService.AddTimestampToVideoAsync(videoId, timestamp);
                
                _logger.LogInformation("Added timestamp to video {VideoId} for tenant: {TenantName}", 
                    videoId, tenantName);
                
                return Ok(video);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("{Message} for tenant: {TenantName}", ex.Message, tenantName);
                return NotFound(ex.Message);
            }
        }
        // DELETE: api/{tenantName}/materials/videos/5/timestamps/3
        [HttpDelete("videos/{videoId}/timestamps/{timestampId}")]
        public async Task<IActionResult> RemoveTimestampFromVideo(string tenantName, int videoId, int timestampId)
        {
            _logger.LogInformation("Removing timestamp {TimestampId} from video {VideoId} for tenant: {TenantName}", 
                timestampId, videoId, tenantName);
            
            var removed = await _materialService.RemoveTimestampFromVideoAsync(videoId, timestampId);
            
            if (!removed)
            {
                return NotFound("Timestamp not found");
            }

            _logger.LogInformation("Removed timestamp {TimestampId} from video {VideoId} for tenant: {TenantName}", 
                timestampId, videoId, tenantName);

            return NoContent();
        }

        #endregion

        #region Checklist Material Management

        // GET: api/{tenantName}/materials/checklists/5/with-entries
        [HttpGet("checklists/{id}/with-entries")]
        public async Task<ActionResult<ChecklistMaterial>> GetChecklistWithEntries(string tenantName, int id)
        {
            _logger.LogInformation("📋 Getting checklist material {Id} with entries for tenant: {TenantName}", 
                id, tenantName);
            
            var checklist = await _materialService.GetChecklistMaterialWithEntriesAsync(id);
            
            if (checklist == null)
            {
                _logger.LogWarning("Checklist material {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            _logger.LogInformation("Retrieved checklist material {Id} with {Count} entries for tenant: {TenantName}", 
                id, checklist.ChecklistEntries?.Count() ?? 0, tenantName);
            
            return Ok(checklist);
        }

        // POST: api/{tenantName}/materials/checklists/5/entries
        [HttpPost("checklists/{checklistId}/entries")]
        public async Task<ActionResult<ChecklistMaterial>> AddEntryToChecklist(string tenantName, int checklistId, ChecklistEntry entry)
        {
            _logger.LogInformation("✔️ Adding entry '{Text}' to checklist {ChecklistId} for tenant: {TenantName}", 
                entry.Text, checklistId, tenantName);
            
            try
            {
                var checklist = await _materialService.AddEntryToChecklistAsync(checklistId, entry);
                
                _logger.LogInformation("Added entry to checklist {ChecklistId} for tenant: {TenantName}", 
                    checklistId, tenantName);
                
                return Ok(checklist);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("{Message} for tenant: {TenantName}", ex.Message, tenantName);
                return NotFound(ex.Message);
            }
        }

        // DELETE: api/{tenantName}/materials/checklists/5/entries/3
        [HttpDelete("checklists/{checklistId}/entries/{entryId}")]
        public async Task<IActionResult> RemoveEntryFromChecklist(string tenantName, int checklistId, int entryId)
        {
            _logger.LogInformation("Removing entry {EntryId} from checklist {ChecklistId} for tenant: {TenantName}", 
                entryId, checklistId, tenantName);
            
            var removed = await _materialService.RemoveEntryFromChecklistAsync(checklistId, entryId);
            
            if (!removed)
            {
                return NotFound("Entry not found");
            }

            _logger.LogInformation("Removed entry {EntryId} from checklist {ChecklistId} for tenant: {TenantName}", 
                entryId, checklistId, tenantName);

            return NoContent();
        }

        #endregion

        #region Workflow Material Management

        // GET: api/{tenantName}/materials/workflows/5/with-steps
        [HttpGet("workflows/{id}/with-steps")]
        public async Task<ActionResult<WorkflowMaterial>> GetWorkflowWithSteps(string tenantName, int id)
        {
            _logger.LogInformation("⚙️ Getting workflow material {Id} with steps for tenant: {TenantName}", 
                id, tenantName);
            
            var workflow = await _materialService.GetWorkflowMaterialWithStepsAsync(id);
            
            if (workflow == null)
            {
                _logger.LogWarning("Workflow material {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            _logger.LogInformation("Retrieved workflow material {Id} with {Count} steps for tenant: {TenantName}", 
                id, workflow.WorkflowSteps?.Count() ?? 0, tenantName);
            
            return Ok(workflow);
        }

        // POST: api/{tenantName}/materials/workflows/5/steps
        [HttpPost("workflows/{workflowId}/steps")]
        public async Task<ActionResult<WorkflowMaterial>> AddStepToWorkflow(string tenantName, int workflowId, WorkflowStep step)
        {
            _logger.LogInformation("➕ Adding step '{Title}' to workflow {WorkflowId} for tenant: {TenantName}", 
                step.Title, workflowId, tenantName);
            
            try
            {
                var workflow = await _materialService.AddStepToWorkflowAsync(workflowId, step);
                
                _logger.LogInformation("Added step to workflow {WorkflowId} for tenant: {TenantName}", 
                    workflowId, tenantName);
                
                return Ok(workflow);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("{Message} for tenant: {TenantName}", ex.Message, tenantName);
                return NotFound(ex.Message);
            }
        }

        // DELETE: api/{tenantName}/materials/workflows/5/steps/3
        [HttpDelete("workflows/{workflowId}/steps/{stepId}")]
        public async Task<IActionResult> RemoveStepFromWorkflow(string tenantName, int workflowId, int stepId)
        {
            _logger.LogInformation("Removing step {StepId} from workflow {WorkflowId} for tenant: {TenantName}", 
                stepId, workflowId, tenantName);
            
            var removed = await _materialService.RemoveStepFromWorkflowAsync(workflowId, stepId);
            
            if (!removed)
            {
                return NotFound("Step not found");
            }

            _logger.LogInformation("Removed step {StepId} from workflow {WorkflowId} for tenant: {TenantName}", 
                stepId, workflowId, tenantName);

            return NoContent();
        }

        #endregion

        #region Asset Relationships

        // GET: api/{tenantName}/materials/by-asset/asset123
        [HttpGet("by-asset/{assetId}")]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterialsByAsset(string tenantName, string assetId)
        {
            _logger.LogInformation("Getting materials for asset {AssetId} in tenant: {TenantName}", 
                assetId, tenantName);
            
            var materials = await _materialService.GetMaterialsByAssetIdAsync(assetId);
            
            _logger.LogInformation("Found {Count} materials for asset {AssetId} in tenant: {TenantName}", 
                materials.Count(), assetId, tenantName);
            
            return Ok(materials);
        }

        // GET: api/{tenantName}/materials/5/asset
        [HttpGet("{materialId}/asset")]
        public async Task<ActionResult<object>> GetMaterialAsset(string tenantName, int materialId)
        {
            _logger.LogInformation("Getting asset for material {MaterialId} in tenant: {TenantName}", 
                materialId, tenantName);
            
            var assetId = await _materialService.GetMaterialAssetIdAsync(materialId);
            
            if (assetId == null)
            {
                return Ok(new { AssetId = (string?)null, Message = "Material does not support assets or has no asset assigned" });
            }

            _logger.LogInformation("Material {MaterialId} has asset {AssetId} in tenant: {TenantName}", 
                materialId, assetId, tenantName);
            
            return Ok(new { AssetId = assetId });
        }

        // POST: api/{tenantName}/materials/5/assign-asset/asset123
        [HttpPost("{materialId}/assign-asset/{assetId}")]
        public async Task<IActionResult> AssignAssetToMaterial(string tenantName, int materialId, string assetId)
        {
            _logger.LogInformation("🔗 Assigning asset {AssetId} to material {MaterialId} for tenant: {TenantName}", 
                assetId, materialId, tenantName);
            
            var success = await _materialService.AssignAssetToMaterialAsync(materialId, assetId);
            
            if (!success)
            {
                return BadRequest("Material not found or material type does not support assets");
            }

            _logger.LogInformation("Assigned asset {AssetId} to material {MaterialId} for tenant: {TenantName}", 
                assetId, materialId, tenantName);

            return Ok(new { Message = "Asset successfully assigned to material" });
        }

        // DELETE: api/{tenantName}/materials/5/remove-asset
        [HttpDelete("{materialId}/remove-asset")]
        public async Task<IActionResult> RemoveAssetFromMaterial(string tenantName, int materialId)
        {
            _logger.LogInformation("Removing asset from material {MaterialId} for tenant: {TenantName}", 
                materialId, tenantName);
            
            var success = await _materialService.RemoveAssetFromMaterialAsync(materialId);
            
            if (!success)
            {
                return BadRequest("Material not found or material type does not support assets");
            }

            _logger.LogInformation("Removed asset from material {MaterialId} for tenant: {TenantName}", 
                materialId, tenantName);

            return Ok(new { Message = "Asset successfully removed from material" });
        }

        #endregion

        #region Polymorphic Relationships Management

        // GET: api/{tenantName}/materials/5/relationships
        [HttpGet("{materialId}/relationships")]
        public async Task<ActionResult<IEnumerable<MaterialRelationship>>> GetMaterialRelationships(string tenantName, int materialId)
        {
            _logger.LogInformation("Getting relationships for material {MaterialId} in tenant: {TenantName}", 
                materialId, tenantName);
            
            var relationships = await _materialService.GetMaterialRelationshipsAsync(materialId);
            
            _logger.LogInformation("Found {Count} relationships for material {MaterialId} in tenant: {TenantName}", 
                relationships.Count(), materialId, tenantName);
            
            return Ok(relationships);
        }

        // POST: api/{tenantName}/materials/5/assign-learningpath/3
        [HttpPost("{materialId}/assign-learningpath/{learningPathId}")]
        public async Task<ActionResult<object>> AssignMaterialToLearningPath(
            string tenantName, int materialId, int learningPathId, [FromQuery] string relationshipType = "contains", [FromQuery] int? displayOrder = null)
        {
            _logger.LogInformation("🔗 Assigning material {MaterialId} to learning path {LearningPathId} for tenant: {TenantName}", 
                materialId, learningPathId, tenantName);
            
            var relationshipId = await _materialService.AssignMaterialToLearningPathAsync(materialId, learningPathId, relationshipType, displayOrder);

            _logger.LogInformation("Assigned material {MaterialId} to learning path {LearningPathId} (Relationship: {RelationshipId}) for tenant: {TenantName}", 
                materialId, learningPathId, relationshipId, tenantName);

            return Ok(new { 
                Message = "Material successfully assigned to learning path",
                RelationshipId = relationshipId,
                RelationshipType = relationshipType,
                DisplayOrder = displayOrder
            });
        }

        // DELETE: api/{tenantName}/materials/5/remove-learningpath/3
        [HttpDelete("{materialId}/remove-learningpath/{learningPathId}")]
        public async Task<IActionResult> RemoveMaterialFromLearningPath(string tenantName, int materialId, int learningPathId)
        {
            _logger.LogInformation("Removing material {MaterialId} from learning path {LearningPathId} for tenant: {TenantName}", 
                materialId, learningPathId, tenantName);
            
            var success = await _materialService.RemoveMaterialFromLearningPathAsync(materialId, learningPathId);
            
            if (!success)
            {
                return NotFound("Relationship not found");
            }

            _logger.LogInformation("Removed material {MaterialId} from learning path {LearningPathId} for tenant: {TenantName}", 
                materialId, learningPathId, tenantName);

            return Ok(new { Message = "Material successfully removed from learning path" });
        }

        #endregion

        #region Material Type Summary Endpoint

        // GET: api/{tenantName}/materials/summary
        [HttpGet("summary")]
        public async Task<ActionResult<MaterialTypeSummary>> GetMaterialTypeSummary(string tenantName)
        {
            _logger.LogInformation("📊 Getting material type summary for tenant: {TenantName}", tenantName);
            
            try
            {
                var summary = new MaterialTypeSummary
                {
                    TenantName = tenantName,
                    Videos = (await _materialService.GetAllVideoMaterialsAsync()).Count(),
                    Images = (await _materialService.GetAllImageMaterialsAsync()).Count(),
                    Checklists = (await _materialService.GetAllChecklistMaterialsAsync()).Count(),
                    Workflows = (await _materialService.GetAllWorkflowMaterialsAsync()).Count(),
                    PDFs = (await _materialService.GetAllPDFMaterialsAsync()).Count(),
                    Chatbots = (await _materialService.GetAllChatbotMaterialsAsync()).Count(),
                    Questionnaires = (await _materialService.GetAllQuestionnaireMaterialsAsync()).Count(),
                    MQTTTemplates = (await _materialService.GetAllMQTTTemplateMaterialsAsync()).Count(),
                    UnityDemos = (await _materialService.GetAllUnityDemoMaterialsAsync()).Count(),
                    Total = (await _materialService.GetAllMaterialsAsync()).Count()
                };

                _logger.LogInformation("Generated material summary for tenant: {TenantName} ({Total} total materials)", 
                    tenantName, summary.Total);
                
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating material summary for tenant: {TenantName}", tenantName);
                return StatusCode(500, $"Error generating summary: {ex.Message}");
            }
        }

        #endregion
                #region Material Relationship Query Endpoints

        /// <summary>
        /// Get all learning paths that contain this material
        /// </summary>
        [HttpGet("{materialId}/learning-paths")]
        public async Task<ActionResult<IEnumerable<LearningPath>>> GetMaterialLearningPaths(
            string tenantName,
            int materialId)
        {
            _logger.LogInformation("Getting learning paths containing material {MaterialId} for tenant: {TenantName}",
                materialId, tenantName);

            // Get relationships where this material is assigned to learning paths
            var relationships = await _materialService.GetRelationshipsByTypeAsync(materialId, "LearningPath");
            
            // Extract learning path IDs and fetch the actual learning paths
            var learningPathIds = relationships.Select(r => int.Parse(r.RelatedEntityId)).ToList();
            
            var learningPaths = new List<LearningPath>();
            foreach (var id in learningPathIds)
            {
                var path = await _learningPathService.GetLearningPathAsync(id);
                if (path != null)
                    learningPaths.Add(path);
            }

            _logger.LogInformation("Found {Count} learning paths containing material {MaterialId} for tenant: {TenantName}",
                learningPaths.Count, materialId, tenantName);

            return Ok(learningPaths);
        }

        /// <summary>
        /// Get all training programs that contain this material
        /// </summary>
      /*  [HttpGet("{materialId}/training-programs")]
        public async Task<ActionResult<IEnumerable<TrainingProgram>>> GetMaterialTrainingPrograms(
            string tenantName,
            int materialId)
        {
            _logger.LogInformation("Getting training programs containing material {MaterialId} for tenant: {TenantName}",
                materialId, tenantName);

            var programs = await _materialService.GetTrainingProgramsContainingMaterialAsync(materialId);

            _logger.LogInformation("Found {Count} training programs containing material {MaterialId} for tenant: {TenantName}",
                programs.Count(), materialId, tenantName);

            return Ok(programs);
        }*/

        /// <summary>
        /// Get all relationships for this material
        /// </summary>
        [HttpGet("{materialId}/all-relationships")]
        public async Task<ActionResult<object>> GetMaterialAllRelationships(
            string tenantName,
            int materialId)
        {
            _logger.LogInformation("Getting all relationships for material {MaterialId} for tenant: {TenantName}",
                materialId, tenantName);

            var relationships = await _materialService.GetMaterialRelationshipsAsync(materialId);

            var groupedRelationships = relationships
                .GroupBy(r => r.RelatedEntityType)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("Found {Count} total relationships for material {MaterialId} for tenant: {TenantName}",
                relationships.Count(), materialId, tenantName);

            return Ok(new
            {
                MaterialId = materialId,
                TotalRelationships = relationships.Count(),
                RelationshipsByType = groupedRelationships,
                RelationshipTypes = groupedRelationships.Keys.ToList()
            });
        }

        #endregion
    }

        // Supporting DTOs
        public class BulkMaterialAssignment
        {
            public int MaterialId { get; set; }
            public string? RelationshipType { get; set; }
            public int? DisplayOrder { get; set; }
            public string? Notes { get; set; }
        }

        public class BulkAssignmentResult
        {
            public int SuccessfulAssignments { get; set; }
            public int FailedAssignments { get; set; }
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();

            #region Request DTOs for Complex Creation
        }
        public class CompleteWorkflowRequest
        {
            public WorkflowMaterial Workflow { get; set; } = new();
            public List<WorkflowStep>? Steps { get; set; }
        }

        public class CompleteVideoRequest
        {
            public VideoMaterial Video { get; set; } = new();
            public List<VideoTimestamp>? Timestamps { get; set; }
        }

        public class CompleteChecklistRequest
        {
            public ChecklistMaterial Checklist { get; set; } = new();
            public List<ChecklistEntry>? Entries { get; set; }
        }

        public class MaterialTypeSummary
        {
            public string TenantName { get; set; } = "";
            public int Videos { get; set; }
            public int Images { get; set; }
            public int Checklists { get; set; }
            public int Workflows { get; set; }
            public int PDFs { get; set; }
            public int Chatbots { get; set; }
            public int Questionnaires { get; set; }
            public int MQTTTemplates { get; set; }
            public int UnityDemos { get; set; }
            public int Total { get; set; }
        }

        #endregion
    }
