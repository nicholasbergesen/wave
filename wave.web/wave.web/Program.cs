
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using wave.web.Models;
using wave.web.Services;

namespace wave.web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpClient();
            builder.Services.Configure<GoogleSearchOptions>(builder.Configuration.GetSection(GoogleSearchOptions.SectionName));
            builder.Services.AddSingleton<DocumentService>();
            builder.Services.AddSingleton<RagSearchService>();
            builder.Services.AddSingleton<IGoogleSearchService, GoogleSearchService>();
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.MapStaticAssets();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            };

            app.UseHttpsRedirection();

            // change
            app.UseAuthorization();

            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            using (var scope = app.Services.CreateScope())
            {
                var ragService = scope.ServiceProvider.GetRequiredService<RagSearchService>();
                await ragService.InitializeAsync();
            }

            app.Run();
        }
    }
}
