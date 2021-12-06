using ApiBase.Data;
using ApiBase.Models;
using ApiBase.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApiBase.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApiDbContext _database;
        ILogger<TodoController> _logger;
        private readonly IHubContext<EventHub> _hub;

        public TodoController(UserManager<AppUser> userManager, ApiDbContext database, ILogger<TodoController> logger, IHubContext<EventHub> hub)
        {
            _userManager = userManager;
            _database = database;
            _logger = logger;
            _hub = hub;
        }

        /// <summary>
        /// visszaadja az adott user összes todo-ját
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public JsonResult GetMyTodos()
        {
            var myself = CurrentUser();

            return new JsonResult(_database.Todos.Where(t => t.Owner.Id == myself.Id));
        }

        /// <summary>
        /// visszaadja az adott user adott id-jű todoját
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [Authorize]
        public JsonResult GetMyTodo(string id)
        {
            var myself = CurrentUser();
            return new JsonResult(_database.Todos.FirstOrDefault(t => t.Owner.Id == myself.Id && t.Id == id));
        }

        /// <summary>
        /// visszaadja minden user összes todo-ját (csak adminoknak!)
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        [HttpPatch]
        public JsonResult GetAllTodo()
        {
            return new JsonResult(_database.Todos);
        }

        /// <summary>
        /// visszaadja valamely user konkrét todo-ját (csak adminoknak!)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}")]
        public JsonResult GetTodoFromUser(string id)
        {
            return new JsonResult(_database.Todos.FirstOrDefault(t => t.Id == id));
        }

        /// <summary>
        /// Todo létrehozása (kell: title stringként, hours intként)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> CreateTodo([FromBody] Todo value)
        {
            value.Owner = CurrentUser();
            _database.Todos.Add(value);
            _database.SaveChanges();

            await _hub.Clients.All.SendAsync("TodoAdded", value);

            return new JsonResult(_database.Todos.FirstOrDefault(t => t.Id == value.Id));
        }

        /// <summary>
        /// todo törlése id alapján, ha a miénk vagy adminok vagyunk
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<JsonResult> RemoveTodo(string id)
        {
            var myself = CurrentUser();
            var myselfRoles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(this.User));

            var todoToDelete = _database.Todos.FirstOrDefault(t => t.Id == id);

            if (todoToDelete.Owner.Id == myself.Id || myselfRoles.Contains("Admin"))
            {
                _database.Todos.Remove(todoToDelete);
                _database.SaveChanges();
                await _hub.Clients.All.SendAsync("TodoRemoved", todoToDelete);
                return new JsonResult(todoToDelete);
            }
            else
            {
                return new JsonResult(BadRequest());
            }
        }


        /// <summary>
        /// Todo módosítása, kell neki a teljes új todo object a régi id-jával
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPut]
        public async Task<JsonResult> UpdateTodo([FromBody] Todo value)
        {
            var myself = CurrentUser();
            var myselfRoles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(this.User));

            var todoToUpdate = _database.Todos.FirstOrDefault(t => t.Id == value.Id);

            if (todoToUpdate.Owner.Id == myself.Id || myselfRoles.Contains("Admin"))
            {
                todoToUpdate.Title = value.Title;
                todoToUpdate.Hours = value.Hours;
                _database.SaveChanges();
                await _hub.Clients.All.SendAsync("TodoUpdated", todoToUpdate);
                return new JsonResult(todoToUpdate);
            }
            else
            {
                return new JsonResult(BadRequest());
            }
        }





        //segédmetódus
        private AppUser CurrentUser()
        {
            //ki a jelenlegi user
            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var myself = _userManager.Users.FirstOrDefault(t => t.UserName == userId);
            return myself;
        }
    }
}
