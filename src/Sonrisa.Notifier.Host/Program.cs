using Microsoft.EntityFrameworkCore;
using Sonrisa.Notifier.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var connection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=sonrisa_notifier.db";
builder.Services.AddDbContext<SonrisaNotifierDbContext>(options =>
    options.UseSqlite(connection)
);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => "Sonrisa Notifier Host");
app.MapControllers();

app.Run();
