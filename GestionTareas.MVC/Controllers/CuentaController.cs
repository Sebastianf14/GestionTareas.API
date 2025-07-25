using Dapper;
using GestionTareas.API.models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using BCrypt.Net;



namespace GestionTareas.MVC.Controllers
{
    public class CuentaController : Controller
    {
        private readonly string _connectionString;

        public CuentaController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            var usuario = await connection.QuerySingleOrDefaultAsync<Usuarios>(
                "SELECT * FROM Usuarios WHERE Email = @Email",
                new { Email = email });

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))

            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos");
                return View();
            }

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email)
        };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string email, string nombre, string password)
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "INSERT INTO Usuarios (Email, Nombre, PasswordHash, IsActive) VALUES (@Email, @Nombre, @Hash, 1)",
                new { Email = email, Nombre = nombre, Hash = hash });

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
