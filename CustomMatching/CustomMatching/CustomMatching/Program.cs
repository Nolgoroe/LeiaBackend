using Services;
using DAL;

using Microsoft.EntityFrameworkCore;
using Services.NuveiPayment;
using Services.Emailer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
//THIS 👇🏻 IS TO PREVENT JSON ENDLESS CYCLES
.AddJsonOptions(jo => jo.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<LeiaContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SuikaDb"))
);

builder.Services.AddScoped<ISuikaDbService, SuikaDbService>();
builder.Services.AddScoped<IPostTournamentService, PostTournamentService>();
builder.Services.AddSingleton<ITournamentService, TournamentService>();
builder.Services.AddSingleton<INuveiPaymentService, NuveiPaymentService>();
builder.Services.AddSingleton<IEmailService>(sp =>
{
    var smtpFromAddress = builder.Configuration.GetConnectionString("SmtpFromAddress");
    var smtpServer = builder.Configuration.GetConnectionString("SmtpServer");
    var smtpUsername = builder.Configuration.GetConnectionString("SmtpUsername");
    var smtpPassword = builder.Configuration.GetConnectionString("SmtpPassword");

    return new EmailService(smtpFromAddress, smtpServer, smtpUsername, smtpPassword);
});

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
