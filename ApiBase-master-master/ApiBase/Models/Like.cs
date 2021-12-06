using ApiBase.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiBase.Models
{
    public class Like
    {
        public AppUser SourceUser { get; set; }
        public string SourceUserId { get; set; }
        public AppUser LikedUser { get; set; }
        public string LikedUserId { get; set; }
    }
}
