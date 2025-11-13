
using Microsoft.AspNetCore.Authentication.OAuth;
using ProxySharp.Services;

namespace ProxySharp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddSingleton<TokenStore>();
        builder.Services.AddSingleton<ITokenService, TokenService>();
        builder.Services.AddScoped<AuthHandler>();

        builder.Services.AddHttpClient("AuthClient")
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["BASE_URL"]);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

        builder.Services.AddHttpClient<TestServiceClient>()
            .AddHttpMessageHandler<AuthHandler>();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

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
    }
}
