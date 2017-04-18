using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using App.Db;

namespace App.Migrations
{
    [DbContext(typeof(AppDb))]
    [Migration("20170418020144_initial")]
    partial class initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.1");

            modelBuilder.Entity("App.Api.Models.Pixel", b =>
                {
                    b.Property<int>("X");

                    b.Property<int>("Y");

                    b.Property<byte[]>("Color");

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Updated")
                        .ValueGeneratedOnAddOrUpdate();

                    b.HasKey("X", "Y");

                    b.ToTable("Pixels");
                });
        }
    }
}
