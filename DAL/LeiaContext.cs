using DataObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DAL
{
    public class LeiaContext : DbContext
    {
        //private readonly string? _connectionString;

        public DbSet<Player> Players { get; set; }
        public DbSet<SessionData> Sessions { get; set; }
        public DbSet<TournamentSession> Tournaments { get; set; }
        public DbSet<TournamentType> TournamentTypes { get; set; }
        public DbSet<Transactions> Transactions { get; set; }
        public DbSet<TransactionType> TransactionTypes { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<Currencies> Currencies { get; set; }
        public DbSet<PlayerCurrencies> PlayerCurrencies { get; set; }
        public DbSet<PlayerTournamentSession> PlayerTournamentSession { get; set; }
        public DbSet<PlayerActiveTournament> PlayerActiveTournaments { get; set; }
        public DbSet<BackendLog> BackendLogs { get; set; }
        public DbSet<League> League { get; set; }
        public DbSet<ConfigurationData> ConfigurationsData { get; set; }
        public DbSet<PlayerAuthToken> PlayerAuthToken { get; set; }
        public DbSet<PlayerGameRating> PlayerGameRatings { get; set; }

        public DbSet<GameType> GameType { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<AchievementElement> AchievementElements { get; set; }

        public DbSet<DailyReward> DailyRewards { get; set; }
        public DbSet<PlayerDailyReward> PlayerDailyRewards { get; set; }

        public DbSet<PlayerHourlyReward> PlayerHourlyRewards { get; set; }

        public DbSet<EggReward> EggRewards { get; set; }

        public DbSet<PlayerEggReward> PlayerEggRewards { get; set; }

        public DbSet<GivenPlayerEggReward> GivenPlayerEggRewards { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<PlayerFeature> PlayerFeatures { get; set; }

        public DbSet<FTUE> FTUEs { get; set; }
        public DbSet<PlayerFtue> PlayerFtues { get; set; }
        public DbSet<PlayerExpByGameType> GameTypeToExp { get; set; }
        public DbSet<LevelReward> LevelRewards { get; set; }
        public DbSet<GivenPlayerLevelReward> GivenPlayerLevelRewards { get; set; }
        public DbSet<PlayerProfileData> PlayerProfileData { get; set; }
        public DbSet<UserMainProgression> UserMainProgression { get; set; }

        public LeiaContext(DbContextOptions<LeiaContext> options) : base(options) { }
        public LeiaContext(/* string? connectionString*/)
        {
            //_connectionString = connectionString;
        }

        private static IConfigurationRoot _configurationBuild;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.EnableSensitiveDataLogging();

            if (_configurationBuild == null)
            {
                _configurationBuild = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, false)
                .Build();
            }
            var connectionString = _configurationBuild["ConnectionStrings:SuikaDb"];

            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Player>() // this is needed to configure a MtM connection type with a payload (like we made in the PlayerTournamentSession class. With out this we get an error)
                .HasMany(p => p.TournamentSessions)
                .WithMany(t => t.Players)

                .UsingEntity<PlayerTournamentSession>
                (
                    configureJoinEntityType: configurePlayerTournamentSession =>
                    {
                        configurePlayerTournamentSession.Property(pt => pt.PlayerScore);
                        configurePlayerTournamentSession.Property(pt => pt.DidClaim);
                        configurePlayerTournamentSession.Property(pt => pt.Position);
                        configurePlayerTournamentSession.Property(pt => pt.JoinTime);
                        configurePlayerTournamentSession.Property(pt => pt.SubmitScoreTime);
                    },
                    configureRight: configureTournaments => configureTournaments
                        .HasOne(pt => pt.TournamentSession)
                        .WithMany(t => t.PlayerTournamentSessions)
                        .HasForeignKey(pt => pt.TournamentSessionId)
                        .OnDelete(DeleteBehavior.NoAction),

                    configureLeft: configurePlayers => configurePlayers
                        .HasOne(pt => pt.Player)
                        .WithMany(p => p.PlayerTournamentSessions)
                        .HasForeignKey(pt => pt.PlayerId)

                );

            #region Configure PlayerAuthToken
            // Make sure auth tokens are unique
            modelBuilder.Entity<PlayerAuthToken>()
                .HasIndex(p => p.Token).IsUnique();
            modelBuilder.Entity<PlayerAuthToken>()
                .HasIndex(p => p.Secret).IsUnique();
            #endregion

            #region Configure PlayerActiveTournament and BackendLog

            modelBuilder.Entity<PlayerActiveTournament>()
                .HasIndex(p => p.PlayerId).IsUnique();
            // This index is used for getting players still in the queue
            modelBuilder.Entity<PlayerActiveTournament>()
                .HasIndex(p => p.TournamentId);
            // This index is used to sort players by their waiting time in the queue
            modelBuilder.Entity<PlayerActiveTournament>()
                .HasIndex(p => p.MatchmakeStartTime);

            modelBuilder.Entity<BackendLog>()
                .HasIndex(p => p.Timestamp);
            #endregion

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

            #region Game Type
            modelBuilder.Entity<GameType>().HasData(
                new GameType { Id = 1, Name = "Object Match" }
                );
            modelBuilder.Entity<TournamentSession>()
                .HasIndex(p => p.GameTypeId);
            modelBuilder.Entity<PlayerActiveTournament>()
                .HasIndex(p => p.GameTypeId);
            #endregion
        }
    }
}
