using GestionTareas.API.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GestionTareas.MVC.Controllers
{
    [Authorize]
    public class TareasController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TareasController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Tareas/Create
        public async Task<IActionResult> Create(int proyectoId)
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync("api/usuarios");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var usuarios = JsonSerializer.Deserialize<List<Usuarios>>(json);
                ViewBag.Usuarios = new SelectList(usuarios, "Id", "Nombre");
            }
            return View(new Tareas { ProjectoId = proyectoId });
        }

        // POST: /Tareas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tareas tarea)
        {
            if (ModelState.IsValid)
            {
                tarea.CreacionUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                var client = CreateAuthenticatedClient();
                var content = new StringContent(JsonSerializer.Serialize(tarea), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/tareas", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Details", "Proyectos", new { id = tarea.ProjectoId });
                }
                ModelState.AddModelError(string.Empty, "Error al crear la tarea.");
            }
            var usuariosResponse = await CreateAuthenticatedClient().GetAsync("api/usuarios");
            if (usuariosResponse.IsSuccessStatusCode)
            {
                var json = await usuariosResponse.Content.ReadAsStringAsync();
                var usuarios = JsonSerializer.Deserialize<List<Usuarios>>(json);
                ViewBag.Usuarios = new SelectList(usuarios, "Id", "Nombre");
            }
            return View(tarea);
        }

        // GET: /Tareas/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync($"api/tareas/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tarea = JsonSerializer.Deserialize<Tareas>(json);
                var usuariosResponse = await client.GetAsync("api/usuarios");
                if (usuariosResponse.IsSuccessStatusCode)
                {
                    var usuariosJson = await usuariosResponse.Content.ReadAsStringAsync();
                    var usuarios = JsonSerializer.Deserialize<List<Usuarios>>(usuariosJson);
                    ViewBag.Usuarios = new SelectList(usuarios, "Id", "Nombre");
                }
                return View(tarea);
            }
            return NotFound();
        }

        // POST: /Tareas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tareas tarea)
        {
            if (id != tarea.Id)
            {
                return BadRequest();
            }
            if (ModelState.IsValid)
            {
                tarea.CreacionUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                var client = CreateAuthenticatedClient();
                var content = new StringContent(JsonSerializer.Serialize(tarea), Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"api/tareas/{id}", content);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Details", "Proyectos", new { id = tarea.ProjectoId });
                }
                ModelState.AddModelError(string.Empty, "Error al actualizar la tarea.");
            }
            var usuariosResponse = await CreateAuthenticatedClient().GetAsync("api/usuarios");
            if (usuariosResponse.IsSuccessStatusCode)
            {
                var json = await usuariosResponse.Content.ReadAsStringAsync();
                var usuarios = JsonSerializer.Deserialize<List<Usuarios>>(json);
                ViewBag.Usuarios = new SelectList(usuarios, "Id", "Nombre");
            }
            return View(tarea);
        }

        // GET: /Tareas/Reporte
        public async Task<IActionResult> Reporte(string estado, string prioridad)
        {
            var client = CreateAuthenticatedClient();
            var response = await client.GetAsync("api/tareas");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tareas = JsonSerializer.Deserialize<List<Tareas>>(json);

                // Filtrar con LINQ
                var query = tareas.AsQueryable();
                if (!string.IsNullOrEmpty(estado) && Enum.TryParse<TareaStatus>(estado, out var status))
                    query = query.Where(t => t.Status == status);
                if (!string.IsNullOrEmpty(prioridad) && Enum.TryParse<TareaPrioridad>(prioridad, out var prio))
                    query = query.Where(t => t.Prioridad == prio);

                return View(query.OrderBy(t => t.Prioridad).ThenBy(t => t.Titulo).ToList()); // Cambiado FechaVencimiento por Titulo
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
