using Dapper;
using GestionTareas.API.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GestionTareas.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TareasController : ControllerBase
    {
        private readonly string _connectionString;

        public TareasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/tareas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tarea>>> GetTareas()
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT t.*, p.*, uc.*, ua.*
                FROM Tareas t
                INNER JOIN Proyectos p ON t.ProjectoId = p.Id
                INNER JOIN Usuarios uc ON t.CreacionUserId = uc.Id
                LEFT JOIN Usuarios ua ON t.AsignacionUserId = ua.Id";

            var tareas = new List<Tarea>();
            await connection.QueryAsync<Tarea, Proyecto, Usuario, Usuario, Tarea>(
                sql,
                (tarea, proyecto, creacion, asignacion) =>
                {
                    tarea.Project = proyecto;
                    tarea.Creacion = creacion;
                    tarea.Asignacion = asignacion;
                    tareas.Add(tarea);
                    return tarea;
                },
                splitOn: "Id,Id,Id");

            return Ok(tareas);
        }

        // GET: api/tareas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Tarea>> GetTarea(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT t.*, p.*, uc.*, ua.*
                FROM Tareas t
                INNER JOIN Proyectos p ON t.ProjectoId = p.Id
                INNER JOIN Usuarios uc ON t.CreacionUserId = uc.Id
                LEFT JOIN Usuarios ua ON t.AsignacionUserId = ua.Id
                WHERE t.Id = @Id";

            Tarea tarea = null;
            await connection.QueryAsync<Tarea, Proyecto, Usuario, Usuario, Tarea>(
                sql,
                (t, p, uc, ua) =>
                {
                    tarea = t;
                    tarea.Project = p;
                    tarea.Creacion = uc;
                    tarea.Asignacion = ua;
                    return tarea;
                },
                new { Id = id },
                splitOn: "Id,Id,Id");

            if (tarea == null)
            {
                return NotFound();
            }

            return Ok(tarea);
        }

        // POST: api/tareas
        [HttpPost]
        public async Task<ActionResult<Tarea>> CreateTarea(Tarea tarea)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"INSERT INTO Tareas (Titulo, Descripcion, Status, Prioridad, ProjectoId, AsignacionUserId, CreacionUserId) 
                                VALUES (@Titulo, @Descripcion, @Status, @Prioridad, @ProjectoId, @AsignacionUserId, @CreacionUserId);
                                SELECT CAST(SCOPE_IDENTITY() as int);";
            tarea.Id = await connection.ExecuteScalarAsync<int>(sql, tarea);
            return CreatedAtAction(nameof(GetTarea), new { id = tarea.Id }, tarea);
        }

        // PUT: api/tareas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTarea(int id, Tarea tarea)
        {
            if (id != tarea.Id)
            {
                return BadRequest();
            }

            using var connection = new SqlConnection(_connectionString);
            const string sql = @"UPDATE Tareas 
                                SET Titulo = @Titulo, Descripcion = @Descripcion, Status = @Status, 
                                    Prioridad = @Prioridad, ProjectoId = @ProjectoId, 
                                    AsignacionUserId = @AsignacionUserId, CreacionUserId = @CreacionUserId 
                                WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, tarea);
            if (affectedRows == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/tareas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTarea(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = "DELETE FROM Tareas WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            if (affectedRows == 0)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
