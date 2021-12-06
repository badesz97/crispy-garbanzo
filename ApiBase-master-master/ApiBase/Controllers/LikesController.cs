using ApiBase.Data;
using ApiBase.DTO;
using ApiBase.Helpers;
using ApiBase.Models;
using ApiBase.Services;
using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApiBase.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LikesController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApiDbContext _database;

        public LikesController(UserManager<AppUser> userManager, ApiDbContext database)
        {
            _userManager = userManager;
            _database = database;
        }

        [HttpPost("{username}")]
        [Authorize]
        public async Task<ActionResult> AddLike(string username)
        {
            var un = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sourceUser = await _userManager.FindByNameAsync(un);

            var likedUser = await _userManager.FindByNameAsync(username);

            if (likedUser == null)
                return NotFound();
            if (sourceUser.UserName == username)
                return BadRequest("Liking user cannot like themselves.");

            var like = _database.Likes.FirstOrDefault(x => x.SourceUserId == sourceUser.Id && x.LikedUserId == likedUser.Id);

            if (like != null)
                return BadRequest("User already liked specified user.");

            like = new Like()
            {
                SourceUserId = sourceUser.Id,
                LikedUserId = likedUser.Id
            };

            _database.Likes.Add(like);
            _database.SaveChanges();

            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDto>>> GetLikedUsers(string predicate)
        {
            var un = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUser = await _userManager.FindByNameAsync(un);

            if (predicate == "likedUsers")
            {
                var likes = _database.Likes.AsQueryable().Where(x => x.SourceUserId == currentUser.Id);
                List<AppUser> likedUsers = new List<AppUser>();
                foreach (var like in likes)
                {
                    foreach (var user in _userManager.Users)
                    {
                        if (user.Id == like.LikedUserId)
                        {
                            likedUsers.Add(user);
                        }
                    }
                }

                var likeDtos = likedUsers.Select(user => new LikeDto
                {
                    Username = user.UserName,
                    KnownAs = user.KnownAs,
                    Age = user.DateOfBirth.CalculateAge(),
                    PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain).Url,
                    City = user.City,
                    UserId = user.Id

                }).ToList();
                return Ok(likeDtos);
            }

            if (predicate == "likedBy")
            {
                var likes = _database.Likes.AsQueryable().Where(x => x.LikedUserId == currentUser.Id);
                List<AppUser> usersLikedThisUser = new List<AppUser>();
                foreach (var like in likes)
                {
                    foreach (var user in _userManager.Users)
                    {
                        if (user.Id == like.SourceUserId)
                        {
                            usersLikedThisUser.Add(user);
                        }
                    }
                }

                var likeDtos = usersLikedThisUser.Select(user => new LikeDto
                {
                    Username = user.UserName,
                    KnownAs = user.KnownAs,
                    Age = user.DateOfBirth.CalculateAge(),
                    PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain).Url,
                    City = user.City,
                    UserId = user.Id

                }).ToList();
                return Ok(likeDtos);
            }

            return BadRequest("Wrong predicate");

        }
    }
}
