using ApiBase.Data;
using ApiBase.DTO;
using ApiBase.Models;
using ApiBase.Services;
using ApiBase.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ApiBase.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApiDbContext _database;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<EventHub> _hub;
        private readonly IMapper mapper;
        private readonly IPhotoService photoservice;

        public UsersController(UserManager<AppUser> userManager, ApiDbContext database, IConfiguration configuration, IHubContext<EventHub> hub, IMapper mapper, IPhotoService photoservice)
        {
            _userManager = userManager;
            _database = database;
            _configuration = configuration;
            _hub = hub;
            this.mapper = mapper;
            this.photoservice = photoservice;
        }

        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<MemberDto>> GetUsers()
        {
            var users = await _userManager.GetUsersInRoleAsync("Customer");
            var usersToReturn = mapper.Map<IEnumerable<MemberDto>>(users);

            return usersToReturn;

            /*
             var users = _userManager.Users
                .ProjectTo<MemberDto>(mapper.ConfigurationProvider)
                .AsNoTracking();
            var q = await PagedList<MemberDto>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);

            Response.AddPaginationHeader(q.CurrentPage, q.PageSize,
                q.TotalCount, q.TotalPages);

            return q;
            */
        }

        [HttpGet("{username}", Name = "GetUser")]
        [Authorize]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            return mapper.Map<MemberDto>(user);
        }

        [HttpPut]
        [Authorize]
        public async Task<ActionResult> UpdateUser([FromBody] MemberUpdateDto memberUpdateDto)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByNameAsync(username);

            mapper.Map(memberUpdateDto, user);
            var res = await _userManager.UpdateAsync(user);
            if (res != null)
                return NoContent();
            else
                return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByNameAsync(username);

            var res = await photoservice.AddPhotoAsync(file);

            if (res.Error != null)
            {
                return BadRequest(res.Error.Message);
            }

            var photo = new Photo
            {
                Url = res.SecureUrl.AbsoluteUri,
                PublicId = res.PublicId
            };

            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }

            ((AppUser)_database.Users.FirstOrDefault(x => x == user)).Photos.Add(photo);
            _database.SaveChanges();

            return CreatedAtRoute("GetUser", new { username = user.UserName }, mapper.Map<PhotoDto>(photo));
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByNameAsync(username);

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if (photo.IsMain)
                return BadRequest("This is already the users main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null)
                currentMain.IsMain = false;

            ((AppUser)_database.Users.FirstOrDefault(x => x == user)).Photos.Remove(photo);

            photo.IsMain = true;
            ((AppUser)_database.Users.FirstOrDefault(x => x == user)).Photos.Add(photo);
            _database.SaveChanges();
            return NoContent(); 
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByNameAsync(username);

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null)
                return NotFound();
            if (photo.IsMain)
                return BadRequest("Main photo cannot be deleted");
            if (photo.PublicId != null)
            {
                var res = await photoservice.DeletePhotoAsync(photo.PublicId);
                if (res.Error != null)
                    return BadRequest(res.Error.Message);
            }

            ((AppUser)_database.Users.FirstOrDefault(x => x == user)).Photos.Remove(photo);
            _database.SaveChanges();

            return Ok();
        }
    }
}
