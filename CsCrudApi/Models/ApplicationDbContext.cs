using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.UserRelated;
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

        public DbSet<PostRelated.PostAuthors> PostAuthors { get; set; }

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

            modelBuilder.Entity<PostAuthors>()
                .HasKey(pa => new { pa.CdUser, pa.GuidPost });

            modelBuilder.Entity<PostAuthors>()
                .HasOne<Post>()
                .WithMany()
                .HasForeignKey(pa => pa.GuidPost)
                .HasPrincipalKey(p => p.Guid);

            modelBuilder.Entity<PostAuthors>()
               .HasOne<User>()
               .WithMany()
               .HasForeignKey(pa => pa.CdUser)
               .HasPrincipalKey(u => u.IdUser);

            base.OnModelCreating(modelBuilder);
        }
    }
}
