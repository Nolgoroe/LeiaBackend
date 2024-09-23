using Services;
using DAL;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<LeiaContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SuikaDb"))
);

builder.Services.AddScoped<ISuikaDbService, SuikaDbService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var leiaContext = scope.ServiceProvider.GetRequiredService<LeiaContext>();
    //leiaContext.Database.EnsureDeleted();
    //leiaContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
