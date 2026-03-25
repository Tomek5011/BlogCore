using BlogCore.DAL.Data;
using BlogCore.DAL.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace BlogCore.DAL.Tests
{
    [TestClass]
    public abstract class IntegrationTestBase
    {
        protected static readonly MsSqlContainer _dbContainer = new
        MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("StrongPassword123!")
        .Build();
        protected BlogContext _context = null!;
        protected BlogRepository _repository = null!;
        private Respawner _respawner = null!;
        [AssemblyInitialize]
        public static async Task AssemblyInit(TestContext context)
        {
            // Uruchomienie kontenera raz dla wszystkich testów w projekcie 
            await _dbContainer.StartAsync();
        }
        [TestInitialize]
        public async Task Setup()
        {
            var connectionString = _dbContainer.GetConnectionString();

            // 1. Konfiguracja EF Core 
            var options = new DbContextOptionsBuilder<BlogContext>()
                .UseSqlServer(connectionString)
                .Options;

            _context = new BlogContext(options);
            await _context.Database.EnsureCreatedAsync(); // Tworzy schemat 
            _repository = new BlogRepository(_context);

            // 2. Inicjalizacja Respawn przy u¿yciu AKTYWNEGO po³¹czenia 
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
                {
                    TablesToIgnore = new Table[]
                    {
                        new Table("__EFMigrationsHistory")
                    }
                });
            }

            // 3. Pierwszy reset bazy  
            await ResetDatabaseAsync();
        }

        // Metoda resetuj¹ca - czyœci tabele bazy danych 
        protected async Task ResetDatabaseAsync()
        {
            if (_respawner != null)
            {
                var connectionString = _dbContainer.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // Resetowanie danych przy u¿yciu obiektu po³¹czenia 
                    await _respawner.ResetAsync(connection);
                }
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Zwalnianie zasobów po ka¿dym teœcie 
            _context.Dispose();
        }
        [AssemblyCleanup]
        public static async Task AssemblyCleanup()
        {
            // Zatrzymanie kontenera po zakoñczeniu wszystkich testów 
            await _dbContainer.StopAsync();
        }
    }
}