using DataObjects;

using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class LeiaContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<SessionData> Sessions { get; set; }
        public DbSet<TournamentData> TournamentsData { get; set; }
        public DbSet<TournamentSession> Tournaments { get; set; }
        public DbSet<TournamentType> TournamentTypes{ get; set; }
        public DbSet<Transactions>Transactions { get; set; }
        public DbSet<TransactionType> TransactionTypes { get; set; }
        public DbSet<Currencies> Currencies { get; set; }
        public DbSet<PlayerCurrencies> PlayerCurrencies { get; set; }

        public LeiaContext(DbContextOptions<LeiaContext> options):base (options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Player>() // this is needed to configure a MtM connection type with a payload (like we made in the PlayerTournamentSession class. With out this we get an error)
                .HasMany(p => p.TournamentSessions)
                .WithMany(t => t.Players)
                .UsingEntity<PlayerTournamentSession>(e =>   
                    e.Property(pt => pt.PlayerScore));

            modelBuilder.Entity<Player>()
                    .HasMany(p => p.Currencies)
                    .WithMany(c => c.Players)
                    .UsingEntity<PlayerCurrencies>
                        (e =>
                            e.Property(pc => pc.CurrencyBalance));
        
        }
    }
}
