using ApiBase.Data;
using ApiBase.Models;
using ApiBase.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiBase.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppleController : GenericController<Apple>
    {
        public AppleController(UserManager<AppUser> userManager, ApiDbContext database, ILogger<AppleController> logger, IHubContext<EventHub> hub)
            :base(userManager, database, logger, hub)
        {
            
        }

        
    }
}
