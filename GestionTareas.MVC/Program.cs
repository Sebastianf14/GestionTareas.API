using GestionTareas.MVC.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionTareas.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configurar el contexto de Identity
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection")));

            // Configurar Identity
            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configurar autenticación con cookies
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Identity/Account/Login";
                    options.LogoutPath = "/Identity/Account/Logout";
                    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                });

            // Agregar servicios para MVC y HttpClient
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:5001/"); // URL de la API
            });

            var app = builder.Build();

            // Configurar el pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages(); // Para Identity Pages

            app.Run();
        }
    }
}
