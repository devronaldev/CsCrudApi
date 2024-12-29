using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.PostRelated.Requests;
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

        public DbSet<Area> Areas { get; set; }

        public DbSet<CampusOffer> CampusOffers { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<UserLikesPost> PostLikes { get; set; }

        public DbSet<PostHasCategory> PostHasCategories { get; set; }

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

            modelBuilder.Entity<CampusOffer>()
                .HasKey(c => new { c.IdCampus, c.IdCourse });

            modelBuilder.Entity<Category>()
                .ToTable("categorias");

            modelBuilder.Entity<UserLikesPost>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(ulp => ulp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserLikesPost>()
                .HasOne<Post>()
                .WithMany()
                .HasForeignKey(ulp => ulp.PostGuid)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserLikesPost>()
                .HasKey(ulp => new { ulp.PostGuid, ulp.UserId });

            modelBuilder.Entity<PostHasCategory>()
                .HasOne<Post>()
                .WithMany()
                .HasForeignKey(phc => phc.PostGUID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostHasCategory>()
                .HasOne<Category>()
                .WithMany()
                .HasForeignKey(phc => phc.CategoryID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostHasCategory>()
                .HasKey(phc => new { phc.PostGUID, phc.CategoryID });

            base.OnModelCreating(modelBuilder);
        }
    }
}
