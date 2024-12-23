﻿// <auto-generated />
using System;
using CsCrudApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CsCrudApi.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241023195936_UpdateUserDate")]
    partial class UpdateUserDate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("CsCrudApi.Models.PostRelated.Post", b =>
                {
                    b.Property<string>("Guid")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)")
                        .HasColumnName("guid");

                    b.Property<DateTime>("PostDate")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("data_post");

                    b.Property<int>("QuantityLikes")
                        .HasColumnType("int")
                        .HasColumnName("qt_like");

                    b.Property<string>("TextPost")
                        .IsRequired()
                        .HasMaxLength(16000000)
                        .HasColumnType("longtext")
                        .HasColumnName("desc_post");

                    b.Property<int>("Type")
                        .HasColumnType("int")
                        .HasColumnName("tipo_post");

                    b.HasKey("Guid");

                    b.ToTable("post");
                });

            modelBuilder.Entity("CsCrudApi.Models.PostRelated.PostAuthors", b =>
                {
                    b.Property<int>("CdUser")
                        .HasColumnType("int")
                        .HasColumnName("cd_usuario");

                    b.Property<string>("GuidPost")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)")
                        .HasColumnName("guid_post");

                    b.HasKey("CdUser", "GuidPost");

                    b.HasIndex("GuidPost");

                    b.ToTable("post_autores");
                });

            modelBuilder.Entity("CsCrudApi.Models.UserRelated.Campus", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id_campus");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("CampusName")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("desc_campus");

                    b.Property<int>("CdCidade")
                        .HasColumnType("int")
                        .HasColumnName("cd_cidade");

                    b.Property<string>("EmailCampus")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("email_campus");

                    b.Property<string>("EndCampus")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("end_campus");

                    b.Property<string>("SgCampus")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("sg_campus");

                    b.HasKey("Id");

                    b.ToTable("campus");
                });

            modelBuilder.Entity("CsCrudApi.Models.UserRelated.Cidade", b =>
                {
                    b.Property<int>("IdCidade")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id_cidade");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("IdCidade"));

                    b.Property<int>("CodIBGE")
                        .HasColumnType("int")
                        .HasColumnName("cod_ibge");

                    b.Property<int>("Estado")
                        .HasColumnType("int")
                        .HasColumnName("estado");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("nome");

                    b.HasKey("IdCidade");

                    b.ToTable("cidade");
                });

            modelBuilder.Entity("CsCrudApi.Models.UserRelated.Request.EmailVerification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("created_at");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("expires_at");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_verified");

                    b.Property<string>("NewEmail")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("novo_email");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.Property<string>("VerificationToken")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("verification_token");

                    b.HasKey("Id");

                    b.ToTable("emailverification");
                });

            modelBuilder.Entity("CsCrudApi.Models.UserRelated.User", b =>
                {
                    b.Property<int>("IdUser")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id_usuario");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("IdUser"));

                    b.Property<int>("CdCampus")
                        .HasColumnType("int")
                        .HasColumnName("cd_campus");

                    b.Property<int>("CdCidade")
                        .HasColumnType("int")
                        .HasColumnName("cd_cidade");

                    b.Property<DateTime>("DtNasc")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("data_nascimento");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("email");

                    b.Property<int>("GrauEscolaridade")
                        .HasColumnType("int")
                        .HasColumnName("grau_escolaridade");

                    b.Property<bool>("IsEmailVerified")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("is_email_verified");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("nome");

                    b.Property<string>("NmSocial")
                        .HasMaxLength(150)
                        .HasColumnType("varchar(150)")
                        .HasColumnName("nome_social");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)")
                        .HasColumnName("senha");

                    b.Property<int>("TipoInteresse")
                        .HasColumnType("int")
                        .HasColumnName("tipo_interesse");

                    b.Property<int>("TpColor")
                        .HasColumnType("int")
                        .HasColumnName("tipo_cor");

                    b.HasKey("IdUser");

                    b.ToTable("usuario");
                });

            modelBuilder.Entity("CsCrudApi.Models.UserRelated.UserFollowingUser", b =>
                {
                    b.Property<int>("CdFollower")
                        .HasColumnType("int")
                        .HasColumnName("cd_usuario_seguidor");

                    b.Property<int>("CdFollowed")
                        .HasColumnType("int")
                        .HasColumnName("cd_usuario_seguido");

                    b.HasKey("CdFollower", "CdFollowed");

                    b.HasIndex("CdFollowed");

                    b.ToTable("usuario_seguindo_usuario");
                });

            modelBuilder.Entity("CsCrudApi.Models.PostRelated.PostAuthors", b =>
                {
                    b.HasOne("CsCrudApi.Models.UserRelated.User", null)
                        .WithMany()
                        .HasForeignKey("CdUser")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CsCrudApi.Models.PostRelated.Post", null)
                        .WithMany()
                        .HasForeignKey("GuidPost")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("CsCrudApi.Models.UserRelated.UserFollowingUser", b =>
                {
                    b.HasOne("CsCrudApi.Models.UserRelated.User", null)
                        .WithMany()
                        .HasForeignKey("CdFollowed")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CsCrudApi.Models.UserRelated.User", null)
                        .WithMany()
                        .HasForeignKey("CdFollower")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
