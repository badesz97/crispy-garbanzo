using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiBase.DTO
{
    public class LikeDto
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string KnownAs { get; set; }
        public string PhotoUrl { get; set; }
        public string City { get; set; }
        public int Age { get; set; }
    }
}
