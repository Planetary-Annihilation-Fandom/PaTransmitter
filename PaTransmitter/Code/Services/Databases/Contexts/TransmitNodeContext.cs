using Microsoft.EntityFrameworkCore;
using PaTransmitter.Code.Models;

namespace PaTransmitter.Code.Services.Databases.Contexts
{
    /// <summary>
    /// Interface to use Sqlite database.
    /// </summary>
    public class TransmitNodeContext : DbContext
    {
        public DbSet<TransmitNode> Nodes { get; set; }

        public TransmitNodeContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var root = FileManager.Instance.Content;
            var path = Path.Combine(root, "/databases/sqlite/");
            var dbName = TransmitNodeConsts.DatabaseName;

            FileManager.Instance.InitializeFolders(path);

            // we can use DataSource or Data Source instead, cause keywords are aliases
            // root has ending slash c:/dir/.
            optionsBuilder.UseSqlite($"Filename={root}databases//sqlite//{dbName}.db");
        }
    }
}
