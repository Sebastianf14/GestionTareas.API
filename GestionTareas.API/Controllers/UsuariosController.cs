using Dapper;
using GestionTareas.API.models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GestionTareas.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly string _connectionString;

        public UsuariosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuarios>>> GetUsuarios()
        {
            using var connection = new SqlConnection(_connectionString);
            var usuarios = await connection.QueryAsync<Usuarios>("SELECT * FROM Usuarios");
            return Ok(usuarios);
        }

        // GET: api/usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuarios>> GetUsuario(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var usuario = await connection.QueryFirstOrDefaultAsync<Usuarios>("SELECT * FROM Usuarios WHERE Id = @Id", new { Id = id });
            if (usuario == null)
            {
                return NotFound();
            }
            return Ok(usuario);
        }

        // POST: api/usuarios
        [HttpPost]
        public async Task<ActionResult<Usuarios>> CreateUsuario(Usuarios usuario)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = @"INSERT INTO Usuarios (Email, Nombre, IsActive) 
                                VALUES (@Email, @Nombre, @IsActive);
                                SELECT CAST(SCOPE_IDENTITY() as int);";
            usuario.Id = await connection.ExecuteScalarAsync<int>(sql, usuario);
            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
        }

        // PUT: api/usuarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, Usuarios usuario)
        {
            if (id != usuario.Id)
            {
                return BadRequest();
            }

            using var connection = new SqlConnection(_connectionString);
            const string sql = @"UPDATE Usuarios 
                                SET Email = @Email, Nombre = @Nombre, IsActive = @IsActive 
                                WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, usuario);
            if (affectedRows == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        // DELETE: api/usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            const string sql = "DELETE FROM Usuarios WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });
            if (affectedRows == 0)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
