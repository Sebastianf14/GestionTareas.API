using Dapper;
using GestionTareas.API.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GestionTareas.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProyectosController : ControllerBase
    {
        private readonly string _connectionString;

        public ProyectosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/proyectos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Proyectos>>> GetProyectos()
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT p.*, u.*
                FROM Proyectos p
                INNER JOIN Usuarios u ON p.CreacionUserId = u.Id";

            var proyectoDict = new Dictionary<int, Proyectos>();
            await connection.QueryAsync<Proyectos, Usuarios, Proyectos>(
                sql,
                (proyecto, usuario) =>
                {
                    if (!proyectoDict.TryGetValue(proyecto.Id, out var currentProyecto))
                    {
                        currentProyecto = proyecto;
                        currentProyecto.Creacion = usuario;
                        currentProyecto.Tareas = new List<Tareas>();
                        proyectoDict.Add(proyecto.Id, currentProyecto);
                    }
                    return currentProyecto;
                },
                splitOn: "Id");

            return Ok(proyectoDict.Values);
        }

        // GET: api/proyectos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Proyectos>> GetProyecto(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT p.*, u.*
                FROM Proyectos p
                INNER JOIN Usuarios u ON p.CreacionUserId = u.Id
                WHERE p.Id = @Id;
                SELECT t.*
                FROM Tareas t
                WHERE t.ProjectoId = @Id";

            using var multi = await connection.QueryMultipleAsync(sql, new { Id = id });

            // Replace the problematic line with the following code:
            var proyecto = await multi.ReadFirstOrDefaultAsync<Proyectos>();

            if (proyecto == null)
            {
                return NotFound();
            }

            // Mapear Tareas desde el segundo conjunto de resultados
            var tareas = await multi.ReadAsync<Tareas>();
            proyecto.Tareas = tareas.ToList();

            return Ok(proyecto);
        }

        // POST: api/proyectos
        [HttpPost]
        public async Task<ActionResult<Proyectos>> CreateProyecto(Proyectos proyecto)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"INSERT INTO Proyectos (Nombre, Descripcion, CreacionUserId, IsActive) 
                                VALUES (@Nombre, @Descripcion, @CreacionUserId, @IsActive);
                                SELECT CAST(SCOPE_IDENTITY() as int);";
            proyecto.Id = await connection.ExecuteScalarAsync<int>(sql, proyecto);
            return CreatedAtAction(nameof(GetProyecto), new { id = proyecto.Id }, proyecto);
        }

        // PUT: api/proyectos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProyecto(int id, Proyectos proyecto)
        {
            if (id != proyecto.Id)
            {
                return BadRequest();
            }

            using var connection = new SqlConnection(_connectionString);
            const string sql = @"UPDATE Proyectos 
                                SET Nombre = @Nombre, Descripcion = @Descripcion, 
                                    CreacionUserId = @CreacionUserId, IsActive = @IsActive 
                                WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, proyecto);
            if (affectedRows == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/proyectos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProyecto(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = "DELETE FROM Proyectos WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            if (affectedRows == 0)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
