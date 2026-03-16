using Microsoft.EntityFrameworkCore;
using PokerProject.Models;

namespace PokerProject.Data
{
    public class PokerDbContext : DbContext
    {
        public PokerDbContext(DbContextOptions<PokerDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<HallOfFame> HallOfFames { get; set; }
        public DbSet<GameParticipant> GameParticipants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<GameParticipant>()
        .HasIndex(gp => new { gp.GameId, gp.UserId })
        .IsUnique();

            modelBuilder.Entity<GameParticipant>()
                .HasOne(gp => gp.Game)
                .WithMany(g => g.Participants)
                .HasForeignKey(gp => gp.GameId);

            modelBuilder.Entity<GameParticipant>()
                .HasOne(gp => gp.User)
                .WithMany(u => u.GameParticipants)
                .HasForeignKey(gp => gp.UserId);

            modelBuilder.Entity<Game>()
                .HasIndex(g => g.GameNumber)
                .IsUnique();

            // Relations User -> Scores
            modelBuilder.Entity<User>()
                .HasMany(u => u.Scores)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relations User -> HallOfFame
            modelBuilder.Entity<User>()
                .HasMany(u => u.HallOfFames)
                .WithOne(h => h.User)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relations Game -> Scores
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Scores)
                .WithOne(s => s.Game)
                .HasForeignKey(s => s.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relations Game -> HallOfFame (Winner)
            modelBuilder.Entity<Game>()
                .HasOne(g => g.Winner)
                .WithOne(h => h.Game)
                .HasForeignKey<HallOfFame>(h => h.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            //Convert role enum to string for better readability in the database
            modelBuilder.Entity<User>()
           .Property(u => u.Role)
           .HasConversion<string>();

            //Convert score type enum to string for better readability in the database
            modelBuilder.Entity<Score>()
       .Property(s => s.Type)
       .HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }
}