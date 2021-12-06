using ApiBase.Data;
using ApiBase.Services;
using ApiBase.ViewModels;
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
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IHubContext<EventHub> _hub;

        public AuthController(UserManager<AppUser> userManager, IConfiguration configuration, IHubContext<EventHub> hub)
        {
            _userManager = userManager;
            _configuration = configuration;
            _hub = hub;
        }

        /// <summary>
        /// Felhasználó regisztrálása a rendszerbe
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<JsonResult> InsertUser([FromBody] RegisterViewModel model)
        {
            var users = _userManager.Users.ToList();

            var user = new AppUser
            {
                Email = model.Email,
                UserName = model.UserName,
                SecurityStamp = Guid.NewGuid().ToString(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Created = DateTime.Now,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                KnownAs = model.KnownAs,
                City = model.City,
                Country = model.Country,
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                if (users.Count == 0)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
            }

            var uwm = new UserViewModel()
            {
                Email = user.Email,
                UserName = user.UserName,
                Id = user.Id,
                Roles = await _userManager.GetRolesAsync(user),
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            await _hub.Clients.All.SendAsync("UserAdded", uwm);

            return new JsonResult(uwm);
        }

        /// <summary>
        /// Felhasználó bejelentkeztetése a rendszerbe
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> Login([FromBody] LoginViewModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var claim = new List<Claim> {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName)
                };

                foreach (var role in await _userManager.GetRolesAsync(user))
                {
                    claim.Add(new Claim(ClaimTypes.Role, role));
                }

                var signinKey = new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"]));

                int expiryInMinutes = Convert.ToInt32(_configuration["Jwt:ExpiryInMinutes"]);

                var token = new JwtSecurityToken(
                  issuer: _configuration["Jwt:Site"],
                  audience: _configuration["Jwt:Site"],
                  claims: claim.ToArray(),
                  expires: DateTime.Now.AddMinutes(60),
                  signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
                );

                user.LastActive = DateTime.Now;
                await _userManager.UpdateAsync(user);

                UserViewModel uvm = new UserViewModel()
                {
                    Email = user.Email,
                    UserName = user.UserName,
                    Id = user.Id,
                    Roles = await _userManager.GetRolesAsync(user),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                };

                await _hub.Clients.All.SendAsync("UserLoggedIn", uvm);

                return new JsonResult(
                  new
                  {
                      username = uvm.UserName,
                      token = new JwtSecurityTokenHandler().WriteToken(token),
                      expiration = token.ValidTo,
                      photoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
                  }); ;

                

            }
            return new JsonResult(Unauthorized("Invalid credentials."));
        }
    }
}
