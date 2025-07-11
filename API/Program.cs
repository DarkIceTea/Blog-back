
using API.Endpoints;
using API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.ConfigureApplicationServices();
            var app = builder.Build();

            AuthEndpoints.SetSender(builder.Services.BuildServiceProvider().GetRequiredService<ISender>());

            app.ConfigureMiddlewares();

            app.Run();
        }
    }
}
