using Microsoft.EntityFrameworkCore;
using Sonrisa.Notifier.Infrastructure;
using Microsoft.Data.Sqlite;
using Sonrisa.Notifier.Core.Senders;
using Sonrisa.Notifier.Core.Interfaces;
using Sonrisa.Notifier.Core.Dispatchers;
using Sonrisa.Notifier.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Resolve DB path inside the Infrastructure project Data folder so the DB file is part of the DAL
var dbRelativePath = Path.Combine("..", "Sonrisa.Notifier.Infrastructure", "Data", "sonrisa_notifier.db");
var dbFullPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, dbRelativePath));

// Ensure folder exists
Directory.CreateDirectory(Path.GetDirectoryName(dbFullPath)!);

// If DB file doesn't exist, attempt to create it from seed.sql located in the Infrastructure Data folder
var seedSqlPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "Sonrisa.Notifier.Infrastructure", "Data", "seed.sql.txt"));
if (!File.Exists(dbFullPath) && File.Exists(seedSqlPath))
{
    try
    {
        var connectionString = $"Data Source={dbFullPath}";
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        var sql = File.ReadAllText(seedSqlPath);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
        conn.Close();
    }
    catch (Exception ex)
    {
        // If seeding fails, remove partial DB if created and rethrow so developer sees the error
        try { if (File.Exists(dbFullPath)) File.Delete(dbFullPath); } catch { }
        throw new InvalidOperationException($"Failed to create DB from seed file: {seedSqlPath}", ex);
    }
}

var connection = $"Data Source={dbFullPath}";
builder.Services.AddDbContext<SonrisaNotifierDbContext>(options =>
    options.UseSqlite(connection)
);

// Infrastructure repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
builder.Services.AddScoped<IUsersChannelsRepository, UsersChannelsRepository>();

// Notification senders and dispatcher (moved to Core)
builder.Services.AddScoped<EmailNotificationSender>();
builder.Services.AddScoped<SlackNotificationSender>();
builder.Services.AddScoped<INotificationSenderFactory, NotificationSenderFactory>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => "Sonrisa Notifier Host");
app.MapControllers();

app.Run();
