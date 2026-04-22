using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SimsimiChat.Models;

public partial class SimsimiDbContext : DbContext
{
    public SimsimiDbContext()
    {
    }

    public SimsimiDbContext(DbContextOptions<SimsimiDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatSession> ChatSessions { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatSess__3214EC073D99A970");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.LastActiveAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.StartedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithMany(p => p.ChatSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ChatSessions_Users");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Messages__3214EC07C5831F15");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SequenceNumber);
            entity.Property(e => e.SenderType)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Session).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_Messages_ChatSessions");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC077C0593EC");

            entity.HasIndex(e => e.Token, "IX_RefreshTokens_Token");

            entity.HasIndex(e => e.Token, "UQ__RefreshT__1EB4F817688011B8").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.JwtId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Token)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0769844F23");

            entity.HasIndex(e => e.DeviceId, "UQ__Users__49E12310DEBED11E").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4528EC147").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DeviceId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("User");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
