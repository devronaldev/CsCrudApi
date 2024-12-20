﻿using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.UserRelated;
using CsCrudApi.Models.UserRelated.CollegeRelated;
using CsCrudApi.Models.UserRelated.Request;
using Microsoft.EntityFrameworkCore;

namespace CsCrudApi.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
        {

        }

        public DbSet<UserRelated.User> Users { get; set; }

        public DbSet<Cidade> Cidades { get; set; }

        public DbSet<Campus> Campi { get; set; }

        public DbSet<UserFollowingUser> UsersFollowing { get; set; }

        public DbSet<EmailVerification> EmailVerifications { get; set; }

        public DbSet<PostRelated.Post> Posts { get; set; }

        public DbSet<PostArea> PostsArea { get; set; }

        public DbSet<Area> Areas { get; set; }

        public DbSet<CampusOffer> CampusOffers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Definir a chave composta
            modelBuilder.Entity<UserFollowingUser>()
                .HasKey(uf => new { uf.CdFollower, uf.CdFollowed });

            // Definir as chaves estrangeiras (se aplicável)
            modelBuilder.Entity<UserFollowingUser>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(uf => uf.CdFollower)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollowingUser>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(uf => uf.CdFollowed)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Post>()
                .HasOne<User>() 
                .WithMany() 
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostArea>()
                .HasOne<Area>()
                .WithMany()
                .HasForeignKey(pa => pa.AreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostArea>()
                .HasOne<Post>()
                .WithMany()
                .HasForeignKey(pa => pa.GuidPost);

            modelBuilder.Entity<PostArea>()
                .HasKey(pa => new { pa.AreaId, pa.GuidPost });

            modelBuilder.Entity<CampusOffer>()
                .HasKey(c => new { c.IdCampus, c.IdCourse });

            base.OnModelCreating(modelBuilder);
        }
    }
}
