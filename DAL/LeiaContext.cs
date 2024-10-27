using DataObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;

namespace DAL
{
    public class LeiaContext : DbContext
    {
        //private readonly string? _connectionString;

        public DbSet<Player> Players { get; set; }
        public DbSet<SessionData> Sessions { get; set; }
        public DbSet<TournamentData> TournamentsData { get; set; }
        public DbSet<TournamentSession> Tournaments { get; set; }
        public DbSet<TournamentType> TournamentTypes { get; set; }
        public DbSet<Transactions> Transactions { get; set; }
        public DbSet<TransactionType> TransactionTypes { get; set; }
        public DbSet<Currencies> Currencies { get; set; }
        public DbSet<PlayerCurrencies> PlayerCurrencies { get; set; }
        public DbSet<PlayerTournamentSession> PlayerTournamentSession { get; set; }

        public LeiaContext(DbContextOptions<LeiaContext> options) : base(options) {}
        public LeiaContext(/* string? connectionString*/)
        {
            //_connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.EnableSensitiveDataLogging();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .Build();
            var connectionString = configuration["ConnectionStrings:SuikaDb"];

            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Player>() // this is needed to configure a MtM connection type with a payload (like we made in the PlayerTournamentSession class. With out this we get an error)
                .HasMany(p => p.TournamentSessions)
                .WithMany(t => t.Players)
                .UsingEntity<PlayerTournamentSession>(e =>
                    e.Property(pt => pt.PlayerScore));

            #region Configure MtM for players and currencies
            modelBuilder.Entity<Player>()
                    .HasMany(p => p.Currencies)
                    .WithMany(c => c.Players)
                    .UsingEntity<PlayerCurrencies>
                        (e =>
                            e.Property(pc => pc.CurrencyBalance));

            modelBuilder.Entity<PlayerCurrencies>()
                .HasKey(pc => new { pc.CurrenciesId, pc.PlayerId });

            modelBuilder.Entity<PlayerCurrencies>()
                .HasOne(pc => pc.Player)
                .WithMany(p => p.PlayerCurrencies)
                .HasForeignKey(pc => pc.PlayerId);

            modelBuilder.Entity<PlayerCurrencies>()
                .HasOne(pc => pc.Currencies)
                .WithMany(c => c.PlayerCurrencies)
                .HasForeignKey(pc => pc.CurrenciesId);
            #endregion
        }
    }
}
