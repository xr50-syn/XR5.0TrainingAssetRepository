using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/training-repo/XR50AIAPI/[controller]")]
    [ApiController]
    public class AI_Interface : ControllerBase
    {
        private readonly XRAIInterfaceContext _context;
        private readonly HttpClient _httpClient;
        public AI_Interface(XRAIInterfaceContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }
             
    }
}
