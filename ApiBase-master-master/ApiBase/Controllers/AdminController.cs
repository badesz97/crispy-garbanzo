using ApiBase.Data;
using ApiBase.Services;
using ApiBase.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiBase.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IHubContext<EventHub> _hub;

        public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, IHubContext<EventHub> hub)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _hub = hub;
        }


        /// <summary>
        /// visszaadja az összes usert (username, id, email és roleok stringként)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> GetUsers()
        {
            List<UserViewModel> users = new List<UserViewModel>();
            foreach (var user in _userManager.Users)
            {
                users.Add(new UserViewModel()
                {
                    Email = user.Email,
                    Id = user.Id,
                    UserName = user.UserName,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }
            return new JsonResult(users);
        }

        /// <summary>
        /// visszaadja a konkrét usert (username, id, email és roleok stringként)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<JsonResult> GetUser(string id)
        {
            var user = _userManager.Users.FirstOrDefault(t => t.Id == id);
            if (user != null)
            {
                return new JsonResult(new UserViewModel()
                {
                    Email = user.Email,
                    Id = user.Id,
                    UserName = user.UserName,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }
            else
            {
                return new JsonResult(BadRequest());
            }
            
        }

        /// <summary>
        /// egy adott id-jű usert (id), adott nevű role-ba (role) helyezi
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<JsonResult> UserToRole([FromBody] UserRoleViewModel value)
        {
            var user = _userManager.Users.FirstOrDefault(t => t.Id == value.Id);
            if (user != null)
            {
                await _userManager.AddToRoleAsync(user, value.Role);

                var uvm = new UserViewModel()
                {
                    Email = user.Email,
                    Id = user.Id,
                    UserName = user.UserName,
                    Roles = await _userManager.GetRolesAsync(user)
                };

                await _hub.Clients.All.SendAsync("UserToRoleAdded", uvm);

                return new JsonResult(uvm);


            }
            else
            {
                return new JsonResult(BadRequest());
            }

        }

        /// <summary>
        /// egy adott id-jű usert (id), adott nevű role-ból (role) kiszed
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpDelete]
        public async Task<JsonResult> UserFromRole([FromBody] UserRoleViewModel value)
        {
            var user = _userManager.Users.FirstOrDefault(t => t.Id == value.Id);
            if (user != null)
            {
                await _userManager.RemoveFromRoleAsync(user, value.Role);

                var uvm = new UserViewModel()
                {
                    Email = user.Email,
                    Id = user.Id,
                    UserName = user.UserName,
                    Roles = await _userManager.GetRolesAsync(user)
                };

                await _hub.Clients.All.SendAsync("UserFromRoleDeleted", uvm);

                return new JsonResult(uvm);
            }
            else
            {
                return new JsonResult(BadRequest());
            }
            
        }

        /// <summary>
        /// Milyen roleok vannak a rendszerben?
        /// </summary>
        /// <returns></returns>
        [HttpPatch]
        public JsonResult RoleList()
        {
            return new JsonResult(_roleManager.Roles);
        }


        /// <summary>
        /// role létrehozása
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> CreateRole([FromBody] RoleViewModel value)
        {
            await _roleManager.CreateAsync(new IdentityRole()
            {
                Name = value.Name
            });

            var role = _roleManager.Roles.FirstOrDefault(t => t.Name == value.Name);
            await _hub.Clients.All.SendAsync("RoleCreated", role);

            return new JsonResult(role);
        }


    }
}
