using GestionTareas.API.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GestionTareas.MVC.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UsuariosController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Usuarios/
        public async Task<IActionResult> Index()
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync("api/usuarios");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var usuarios = JsonSerializer.Deserialize<List<Usuarios>>(json);
                return View(usuarios.OrderBy(u => u.Nombre));
            }
            return View(new List<Usuarios>());
        }

        // GET: /Usuarios/TareasAsignadas/5
        public async Task<IActionResult> TareasAsignadas(int id)
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/tareas?asignadoId={id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tareas = JsonSerializer.Deserialize<List<Tareas>>(json);
                ViewBag.UsuarioId = id;
                return View(tareas.OrderByDescending(t => t.Prioridad).ThenBy(t => t.Titulo)); // Cambiado FechaVencimiento por Titulo
            }
            return View(new List<Tareas>());
        }

        private HttpClient CreateAuthenticatedClient()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }
    }
}
