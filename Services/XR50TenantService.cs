public string GetCurrentTenant()
        {
            var context = _httpContextAccessor.HttpContext;
            
            // Log the incoming request for debugging
            var path = context?.Request.Path.Value;
            _logger.LogDebug("🔍 Resolving tenant for path: {Path}", path);
            
            if (!string.IsNullOrEmpty(path))
            {
                var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                _logger.LogDebug("Path segments: [{Segments}]", string.Join(", ", pathSegments));
                
                if (pathSegments.Length >= 2)
                {
                    // Pattern: /api/{tenant}/...
                    if (pathSegments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
                    {
                        var tenant = pathSegments[1];
                        _logger.LogInformation("🎯 Resolved tenant from /api/{tenant} pattern: {TenantName}", tenant);
                        return tenant;
                    }
                    
                    // Pattern: /xr50/{tenant}/... or /xr50/trainingAssetRepository/tenants (admin endpoint)
                    if (pathSegments[0].Equals("xr50", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pathSegments.Length >= 3 && 
                            !pathSegments[1].Equals("trainingAssetRepository", StringComparison.OrdinalIgnoreCase))
                        {
                            var tenant = pathSegments[1];
                            _logger.LogInformation("🎯 Resolved tenant from /xr50/{tenant} pattern: {TenantName}", tenant);
                            return tenant; // /xr50/{tenant}/...
                        }
                        else
                        {
                            _logger.LogDebug("Admin endpoint detected: /xr50/trainingAssetRepository/...");
                        }
                    }
                    
                    // Pattern: /{tenant}/api/... (tenant-first routing)
                    if (pathSegments.Length >= 3 && 
                        pathSegments[1].Equals("api", StringComparison.OrdinalIgnoreCase))
                    {
                        var tenant = pathSegments[0];
                        _logger.LogInformation("🎯 Resolved tenant from /{tenant}/api pattern: {TenantName}", tenant);
                        return tenant;
                    }
                }
            }
            
            // Fallback: From header (useful for testing/admin operations)
            if (context?.Request.Headers.TryGetValue("X-Tenant-Name", out var tenantHeader) == true)
            {
                var tenant = tenantHeader.FirstOrDefault();
                _logger.LogInformation("🎯 Resolved tenant from header: {TenantName}", tenant);
                return tenant;
            }
            
            // From JWT claims
            var tenantClaim = context?.User?.FindFirst("tenantName")?.Value;
            if (!string.IsNullOrEmpty(tenantClaim))
            {
                _logger.LogInformation("🎯 Resolved tenant from JWT claim: {TenantName}", tenantClaim);
                return tenantClaim;
            }
            
            _logger.LogInformation("🔄 No tenant resolved, using default");
            return "default";
        }