using FirstBrick.Account.Api.Data;
using FirstBrick.Account.Api.Services;
using FirstBrick.Shared.Auth;
using FirstBrick.Shared.ErrorHandling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AccountDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddFirstBrickJwtAuth(builder.Configuration);
builder.Services.AddFirstBrickErrorHandling();
builder.Services.AddSingleton<TokenService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    db.Database.Migrate();
}

app.UseFirstBrickErrorHandling();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
