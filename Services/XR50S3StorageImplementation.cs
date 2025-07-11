using Amazon.S3;
using Amazon.S3.Model;
using System.Text.RegularExpressions;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Services
{
    public class S3StorageServiceImplementation : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<S3StorageServiceImplementation> _logger;
        private readonly string _baseBucketPrefix;

        public S3StorageServiceImplementation(
            IAmazonS3 s3Client,
            IConfiguration configuration,
            ILogger<S3StorageServiceImplementation> logger)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _logger = logger;
            _baseBucketPrefix = _configuration.GetValue<string>("S3Settings:BaseBucketPrefix") ?? "xr50";
        }

        public string GetStorageType() => "S3";

        private string GetTenantBucketName(string tenantName)
        {
            var sanitized = Regex.Replace(tenantName.ToLowerInvariant(), @"[^a-z0-9\-\.]", "-");
            return $"{_baseBucketPrefix}-tenant-{sanitized}";
        }

        public async Task<bool> CreateTenantStorageAsync(string tenantName, XR50Tenant tenant)
        {
            try
            {
                _logger.LogInformation("Creating S3 storage for tenant: {TenantName}", tenantName);

                var bucketName = GetTenantBucketName(tenantName);

                // Check if bucket already exists
                var bucketExists = await DoesBucketExistAsync(bucketName);
                if (bucketExists)
                {
                    _logger.LogInformation("S3 bucket already exists: {BucketName}", bucketName);
                    return true;
                }

                var request = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                };

                var response = await _s3Client.PutBucketAsync(request);

                _logger.LogInformation("Created S3 bucket: {BucketName}", bucketName);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create S3 storage for tenant: {TenantName}", tenantName);
                return false;
            }
        }

        public async Task<bool> DeleteTenantStorageAsync(string tenantName)
        {
            try
            {
                _logger.LogWarning("Deleting S3 storage for tenant: {TenantName}", tenantName);

                var bucketName = GetTenantBucketName(tenantName);

                // Check if bucket exists before trying to delete
                var bucketExists = await DoesBucketExistAsync(bucketName);
                if (!bucketExists)
                {
                    _logger.LogInformation("S3 bucket does not exist: {BucketName}", bucketName);
                    return true; // Consider it successful if bucket doesn't exist
                }

                // First, delete all objects in the bucket
                await DeleteAllObjectsInBucketAsync(bucketName);

                // Then delete the bucket
                var deleteRequest = new DeleteBucketRequest
                {
                    BucketName = bucketName
                };

                var response = await _s3Client.DeleteBucketAsync(deleteRequest);
                
                _logger.LogInformation("Deleted S3 bucket: {BucketName}", bucketName);
                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete S3 storage for tenant: {TenantName}", tenantName);
                return false;
            }
        }

        public async Task<bool> TenantStorageExistsAsync(string tenantName)
        {
            try
            {
                var bucketName = GetTenantBucketName(tenantName);
                return await DoesBucketExistAsync(bucketName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if tenant storage exists: {TenantName}", tenantName);
                return false;
            }
        }

        public async Task<string> UploadFileAsync(string tenantName, string fileName, Stream fileStream, string contentType = "application/octet-stream")
        {
            try
            {
                var bucketName = GetTenantBucketName(tenantName);
                var key = $"assets/{fileName}";

                _logger.LogInformation("Uploading file to S3: {BucketName}/{Key}", bucketName, key);

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = contentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                var response = await _s3Client.PutObjectAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var url = $"s3://{bucketName}/{key}";
                    _logger.LogInformation("Successfully uploaded file to S3: {Url}", url);
                    return url;
                }

                throw new InvalidOperationException($"Upload failed with status: {response.HttpStatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to S3: {TenantName}/{FileName}", tenantName, fileName);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string tenantName, string fileName)
        {
            try
            {
                var bucketName = GetTenantBucketName(tenantName);
                var key = $"assets/{fileName}";

                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectAsync(request);
                return response.ResponseStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from S3: {TenantName}/{FileName}", tenantName, fileName);
                throw;
            }
        }

        public async Task<string> GetDownloadUrlAsync(string tenantName, string fileName, TimeSpan? expiration = null)
        {
            try
            {
                var bucketName = GetTenantBucketName(tenantName);
                var key = $"assets/{fileName}";
                var expires = expiration ?? TimeSpan.FromHours(1);

                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.Add(expires)
                };

                var url = await _s3Client.GetPreSignedURLAsync(request);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate download URL for S3: {TenantName}/{FileName}", tenantName, fileName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string tenantName, string fileName)
        {
            try
            {
                var bucketName = GetTenantBucketName(tenantName);
                var key = $"assets/{fileName}";

                var request = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await _s3Client.DeleteObjectAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file from S3: {TenantName}/{FileName}", tenantName, fileName);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string tenantName, string fileName)
        {
            try
            {
                var bucketName = GetTenantBucketName(tenantName);
                var key = $"assets/{fileName}";

                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists in S3: {TenantName}/{FileName}", tenantName, fileName);
                return false;
            }
        }

        public async Task<long> GetFileSizeAsync(string tenantName, string fileName)
        {
            try
            {
                var bucketName = GetTenantBucketName(tenantName);
                var key = $"assets/{fileName}";

                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectMetadataAsync(request);
                return response.ContentLength;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file size from S3: {TenantName}/{FileName}", tenantName, fileName);
                return 0;
            }
        }

        public async Task<StorageStatistics> GetStorageStatisticsAsync(string tenantName)
        {
            try
            {
                var bucketName = GetTenantBucketName(tenantName);
                var request = new ListObjectsV2Request { BucketName = bucketName };

                long totalFiles = 0;
                long totalSize = 0;

                ListObjectsV2Response response;
                do
                {
                    response = await _s3Client.ListObjectsV2Async(request);
                    totalFiles += response.S3Objects?.Count ?? 0;
                    totalSize += response.S3Objects?.Sum(obj => obj.Size) ?? 0;
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated == true);

                return new StorageStatistics
                {
                    TenantName = tenantName,
                    StorageType = "S3",
                    TotalFiles = totalFiles,
                    TotalSizeBytes = totalSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage statistics for tenant: {TenantName}", tenantName);
                return new StorageStatistics { TenantName = tenantName, StorageType = "S3" };
            }
        }

        #region Private Helper Methods

        private async Task<bool> DoesBucketExistAsync(string bucketName)
        {
            try
            {
                await _s3Client.GetBucketLocationAsync(bucketName);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if bucket exists: {BucketName}", bucketName);
                return false;
            }
        }

        private async Task DeleteAllObjectsInBucketAsync(string bucketName)
        {
            try
            {
                var request = new ListObjectsV2Request { BucketName = bucketName };
                ListObjectsV2Response response;

                do
                {
                    response = await _s3Client.ListObjectsV2Async(request);
                    if (response.S3Objects.Count > 0)
                    {
                        var deleteRequest = new DeleteObjectsRequest
                        {
                            BucketName = bucketName,
                            Objects = response.S3Objects.Select(obj => new KeyVersion { Key = obj.Key }).ToList()
                        };

                        await _s3Client.DeleteObjectsAsync(deleteRequest);
                    }
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete all objects in bucket: {BucketName}", bucketName);
                throw;
            }
        }

        #endregion
    }
}