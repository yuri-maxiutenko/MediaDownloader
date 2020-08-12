﻿// <auto-generated />
using System;
using MediaDownloader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MediaDownloader.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7");

            modelBuilder.Entity("MediaDownloader.Data.Models.DownloadFolder", b =>
                {
                    b.Property<int>("DownloadFolderId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("LastSelectionDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.HasKey("DownloadFolderId");

                    b.ToTable("DownloadFolders");
                });

            modelBuilder.Entity("MediaDownloader.Data.Models.HistoryRecord", b =>
                {
                    b.Property<int>("HistoryRecordId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("DownloadDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("DownloadFormat")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DownloadStatus")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .HasColumnType("TEXT");

                    b.HasKey("HistoryRecordId");

                    b.ToTable("History");
                });
#pragma warning restore 612, 618
        }
    }
}
