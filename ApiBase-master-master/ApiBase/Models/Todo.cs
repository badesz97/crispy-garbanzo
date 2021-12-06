using ApiBase.Data;
using ApiBase.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ApiBase.Models
{
    public class Todo
    {
        [Key]
        public string Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public int Hours { get; set; }

        [NotMapped]
        public virtual AppUser Owner { get; set; }

        public Todo()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
