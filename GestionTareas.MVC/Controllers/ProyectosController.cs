using GestionTareas.API.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GestionTareas.MVC.Controllers
{
    [Authorize]
    public class ProyectosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProyectosController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Proyectos/
        public async Task<IActionResult> Index()
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync("api/proyectos");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var proyectos = JsonSerializer.Deserialize<List<Proyectos>>(json);
                return View(proyectos.OrderBy(p => p.Nombre)); // Usar LINQ
            }
            return View(new List<Proyectos>());
        }

        // GET: /Proyectos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/proyectos/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var proyecto = JsonSerializer.Deserialize<Proyectos>(json);
                return View(proyecto);
            }
            return NotFound();
        }

        // GET: /Proyectos/Create
        public IActionResult Create()
        {
            return View(new Proyectos());
        }

        // POST: /Proyectos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proyectos proyecto)
        {
            if (ModelState.IsValid)
            {
                proyecto.CreacionUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                proyecto.IsActive = true;
                var client = CreateAuthenticatedClient();
                var content = new StringContent(JsonSerializer.Serialize(proyecto), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/proyectos", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, "Error al crear el proyecto.");
            }
            return View(proyecto);
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
