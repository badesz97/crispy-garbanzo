using ApiBase.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiBase.Data
{
    public class ApiDbContext : IdentityDbContext<IdentityUser>
    {
        //DbSets here!
        public virtual DbSet<Todo> Todos { get; set; }
        public virtual DbSet<Apple> Apples { get; set; }
        public virtual DbSet<AppUser> AppUsers { get; set; }
        public virtual DbSet<Person> People { get; set; }
        public virtual DbSet<Photo> Photos { get; set; }
        public virtual DbSet<Like> Likes { get; set; }
        public virtual DbSet<Message> Messages { get; set; }

        public ApiDbContext(DbContextOptions<ApiDbContext> opt) : base(opt)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);



            builder.Entity<IdentityRole>().HasData(
                new { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
                new { Id = "2", Name = "Customer", NormalizedName = "CUSTOMER" }
            );

            var user = new AppUser()
            {
                Email = "akom@bakom.com",
                UserName = "akombakom",
                PasswordHash = "AQAAAAEAACcQAAAAEH/1o9on40LsRptJddE2jKSj2vJCBDeZuxMKnKxfCaHi0ISbWvlIwsnjayQUuQ3SqQ=="
            };

            builder.Entity<AppUser>(b =>
            {
                b.HasData(user);
                b.OwnsMany(e => e.Photos).HasData(new Photo
                {
                    Id = 999,
                    AppUserId = user.Id,
                    IsMain = true,
                    Url = "www.fsztudja.com",
                    PublicId = "valamiId"
                });
            });

            builder.Entity<Like>()
                .HasKey(k => new { k.SourceUserId, k.LikedUserId });

            builder.Entity<Like>()
                .HasOne(s => s.SourceUser)
                .WithMany(l => l.LikedUsers)
                .HasForeignKey(s => s.SourceUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Like>()
                .HasOne(s => s.LikedUser)
                .WithMany(l => l.LikedByUsers)
                .HasForeignKey(s => s.LikedUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Message>()
                .HasOne(u => u.Recipient)
                .WithMany(m => m.MessagesReceived)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Message>()
                .HasOne(u => u.Sender)
                .WithMany(m => m.MessagesSent)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
