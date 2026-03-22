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
        public DbSet<Player> Players { get; set; }
        public DbSet<Round> Rounds { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<HallOfFame> HallOfFames { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =========================
            // ENUMS
            // =========================
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Game>()
                .Property(g => g.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Score>()
                .Property(s => s.Type)
                .HasConversion<string>();

            // =========================
            // USER → PLAYERS
            // =========================
            modelBuilder.Entity<User>()
                .HasMany(u => u.Players)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade); // slet players når user slettes

            // =========================
            // GAME → GAMEMASTER / WINNERPLAYER
            // =========================
            modelBuilder.Entity<Game>()
                .HasOne(g => g.Gamemaster)
                .WithMany()
                .HasForeignKey(g => g.GamemasterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Game>()
                .HasOne(g => g.WinnerPlayer)
                .WithMany()
                .HasForeignKey(g => g.WinnerPlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // GAME → PLAYERS / ROUNDS
            // =========================
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Players)
                .WithOne(p => p.Game)
                .HasForeignKey(p => p.GameId)
                .OnDelete(DeleteBehavior.Cascade); // slet players når game slettes

            modelBuilder.Entity<Game>()
                .HasMany(g => g.Rounds)
                .WithOne(r => r.Game)
                .HasForeignKey(r => r.GameId)
                .OnDelete(DeleteBehavior.Cascade); // slet rounds når game slettes

            // =========================
            // PLAYER UNIQUE INDEX
            // =========================
            modelBuilder.Entity<Player>()
                .HasIndex(p => new { p.GameId, p.UserId })
                .IsUnique();

            // =========================
            // ROUND UNIQUE INDEX
            // =========================
            modelBuilder.Entity<Round>()
                .HasIndex(r => new { r.GameId, r.RoundNumber })
                .IsUnique();

            modelBuilder.Entity<Round>()
                .HasIndex(r => new { r.GameId, r.EndedAt })
                .HasFilter("[EndedAt] IS NULL")
                .IsUnique();

            // ROUND → SCORES
            modelBuilder.Entity<Round>()
                .HasMany(r => r.Scores)
                .WithOne(s => s.Round)
                .HasForeignKey(s => s.RoundId)
                .OnDelete(DeleteBehavior.Cascade); // slet scores når round slettes

            // =========================
            // SCORE → PLAYER / VICTIMPLAYER
            // =========================
            modelBuilder.Entity<Score>()
                .HasOne(s => s.Player)
                .WithMany()
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.NoAction); // undgå multiple cascade paths

            modelBuilder.Entity<Score>()
                .HasOne(s => s.VictimPlayer)
                .WithMany()
                .HasForeignKey(s => s.VictimPlayerId)
                .OnDelete(DeleteBehavior.NoAction);

            // =========================
            // HALL OF FAME
            // =========================
            modelBuilder.Entity<HallOfFame>()
                .HasOne(h => h.Game)
                .WithOne()
                .HasForeignKey<HallOfFame>(h => h.GameId)
                .OnDelete(DeleteBehavior.Cascade); // slet HoF hvis game slettes

            modelBuilder.Entity<HallOfFame>()
                .HasOne(h => h.Player)
                .WithMany()
                .HasForeignKey(h => h.PlayerId)
                .OnDelete(DeleteBehavior.Restrict); // slet ikke HoF entries hvis player slettes

            base.OnModelCreating(modelBuilder);
        }
    }
}