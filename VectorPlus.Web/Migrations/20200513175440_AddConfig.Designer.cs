﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VectorPlus.Web.Service.Db;

namespace VectorPlus.Web.Migrations
{
    [DbContext(typeof(VectorPlusBackgroundServiceDbContext))]
    [Migration("20200513175440_AddConfig")]
    partial class AddConfig
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.3");

            modelBuilder.Entity("VectorPlus.Web.Service.ModuleConfig", b =>
                {
                    b.Property<Guid>("ModuleConfigId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Added")
                        .HasColumnType("TEXT");

                    b.Property<string>("Author")
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthorEmail")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("DLL")
                        .HasColumnType("BLOB");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<string>("ModuleWebsite")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("Release")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UniqueReference")
                        .HasColumnType("TEXT");

                    b.Property<bool>("UserEnabled")
                        .HasColumnType("INTEGER");

                    b.HasKey("ModuleConfigId");

                    b.ToTable("Modules");
                });

            modelBuilder.Entity("VectorPlus.Web.Service.RoboConfig", b =>
                {
                    b.Property<Guid>("RoboConfigId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasColumnType("TEXT");

                    b.Property<string>("IpOverrideStr")
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .HasColumnType("TEXT");

                    b.Property<string>("RobotCert")
                        .HasColumnType("TEXT");

                    b.Property<string>("RobotGuid")
                        .HasColumnType("TEXT");

                    b.Property<string>("RobotName")
                        .HasColumnType("TEXT");

                    b.Property<string>("RobotSerial")
                        .HasColumnType("TEXT");

                    b.Property<bool>("UseEnvironment")
                        .HasColumnType("INTEGER");

                    b.HasKey("RoboConfigId");

                    b.ToTable("RoboConfig");
                });
#pragma warning restore 612, 618
        }
    }
}