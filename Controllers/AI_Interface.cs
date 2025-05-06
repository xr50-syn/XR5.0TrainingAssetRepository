using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Controllers
{
    [Route("/xr50/trainingAssetRepository/XR50AIAPI/[controller]")]
    [ApiController]
    public class AI_Interface : ControllerBase
    {
        private readonly XR50TrainingAssetRepoContext _context;
        private readonly HttpClient _httpClient;
        public AI_Interface(XR50TrainingAssetRepoContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }
             
    }
}
