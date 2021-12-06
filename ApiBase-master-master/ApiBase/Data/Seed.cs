using ApiBase.Controllers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiBase.Data
{
    public class Seed
    {
        public static async Task<IEnumerable<AppUser>> SeedUsers()
        {
            var userData = await System.IO.File.ReadAllTextAsync("Data/UserSeedData.json");
            var users = JsonSerializer.Deserialize<List<AppUser>>(userData);
            return users;
        }
    }
}
