using FirstBrick.Payment.Api.Consumers;
using FirstBrick.Payment.Api.Data;
using FirstBrick.Shared.Auth;
using FirstBrick.Shared.ErrorHandling;
using FirstBrick.Shared.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddFirstBrickJwtAuth(builder.Configuration);
builder.Services.AddFirstBrickErrorHandling();

var rabbit = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
             ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InvestmentRequestedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(rabbit.Host, rabbit.VirtualHost, h =>
        {
            h.Username(rabbit.Username);
            h.Password(rabbit.Password);
        });
        cfg.ReceiveEndpoint("payment.investment-requested", e =>
        {
            e.ConfigureConsumer<InvestmentRequestedConsumer>(ctx);
            e.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(2)));
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.UseFirstBrickErrorHandling();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
