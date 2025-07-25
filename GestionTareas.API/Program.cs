
using Dapper;
using GestionTareas.API.models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GestionTareas.API
{
    public class Program
    {
        // Cadena de conexión a la base de datos
        private static readonly string connectionString = "Server=localhost;Database=GestionTareas;Trusted_Connection=True;";

        public static async Task Main(string[] args)
        {
            // Ejemplo de operaciones CRUD
            await CrearUsuario();
            await CrearProyecto();
            await CrearTarea();
            await ObtenerTareasConRelaciones();
            await ActualizarTarea();
            await EliminarUsuario();
        }

        // Crear un nuevo usuario
        private static async Task CrearUsuario()
        {
            using var connection = new SqlConnection(connectionString);
            var usuario = new Usuario
            {
                Email = "juan@example.com",
                Nombre = "Juan Pérez",
                IsActive = true
            };

            const string sql = @"INSERT INTO Usuarios (Email, Nombre, IsActive) 
                                VALUES (@Email, @Nombre, @IsActive);
                                SELECT CAST(SCOPE_IDENTITY() as int);";
            usuario.Id = await connection.ExecuteScalarAsync<int>(sql, usuario);
            Console.WriteLine($"Usuario creado con ID: {usuario.Id}");
        }

        // Crear un nuevo proyecto
        private static async Task CrearProyecto()
        {
            using var connection = new SqlConnection(connectionString);
            var proyecto = new Proyecto
            {
                Nombre = "Proyecto Alpha",
                Descripcion = "Descripción del proyecto Alpha",
                CreacionUserId = 1, // Suponiendo que el usuario con ID 1 existe
                IsActive = true
            };

            const string sql = @"INSERT INTO Proyectos (Nombre, Descripcion, CreacionUserId, IsActive) 
                                VALUES (@Nombre, @Descripcion, @CreacionUserId, @IsActive);
                                SELECT CAST(SCOPE_IDENTITY() as int);";
            proyecto.Id = await connection.ExecuteScalarAsync<int>(sql, proyecto);
            Console.WriteLine($"Proyecto creado con ID: {proyecto.Id}");
        }

        // Crear una nueva tarea
        private static async Task CrearTarea()
        {
            using var connection = new SqlConnection(connectionString);
            var tarea = new Tarea
            {
                Titulo = "Tarea inicial",
                Descripcion = "Descripción de la tarea inicial",
                Status = TareaStatus.Pendiente,
                Prioridad = TareaPrioridad.Mediano,
                ProjectoId = 1, // Suponiendo que el proyecto con ID 1 existe
                CreacionUserId = 1, // Suponiendo que el usuario con ID 1 existe
                AsignacionUserId = null // Sin asignar
            };

            const string sql = @"INSERT INTO Tareas (Titulo, Descripcion, Status, Prioridad, ProjectoId, AsignacionUserId, CreacionUserId) 
                                VALUES (@Titulo, @Descripcion, @Status, @Prioridad, @ProjectoId, @AsignacionUserId, @CreacionUserId);
                                SELECT CAST(SCOPE_IDENTITY() as int);";
            tarea.Id = await connection.ExecuteScalarAsync<int>(sql, tarea);
            Console.WriteLine($"Tarea creada con ID: {tarea.Id}");
        }

        // Obtener tareas con sus relaciones (proyecto y usuarios)
        private static async Task ObtenerTareasConRelaciones()
        {
            using var connection = new SqlConnection(connectionString);
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
                splitOn: "Id,Id,Id"); // Dividir los resultados en las columnas Id de cada entidad

            foreach (var tarea in tareas)
            {
                Console.WriteLine($"Tarea: {tarea.Titulo}, Proyecto: {tarea.Project?.Nombre}, Creador: {tarea.Creacion?.Nombre}, Asignado: {tarea.Asignacion?.Nombre ?? "No asignado"}");
            }
        }

        // Actualizar una tarea
        private static async Task ActualizarTarea()
        {
            using var connection = new SqlConnection(connectionString);
            var tarea = new Tarea
            {
                Id = 1, // Suponiendo que la tarea con ID 1 existe
                Titulo = "Tarea actualizada",
                Descripcion = "Descripción actualizada",
                Status = TareaStatus.EnProgreso,
                Prioridad = TareaPrioridad.alto,
                ProjectoId = 1,
                CreacionUserId = 1,
                AsignacionUserId = 1 // Asignar a un usuario
            };

            const string sql = @"UPDATE Tareas 
                                SET Titulo = @Titulo, Descripcion = @Descripcion, Status = @Status, 
                                    Prioridad = @Prioridad, ProjectoId = @ProjectoId, 
                                    AsignacionUserId = @AsignacionUserId, CreacionUserId = @CreacionUserId 
                                WHERE Id = @Id";
            await connection.ExecuteAsync(sql, tarea);
            Console.WriteLine($"Tarea con ID {tarea.Id} actualizada");
        }

        // Eliminar un usuario (ejemplo)
        private static async Task EliminarUsuario()
        {
            using var connection = new SqlConnection(connectionString);
            const string sql = "DELETE FROM Usuarios WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = 1 }); // Suponiendo que el usuario con ID 1 existe
            Console.WriteLine("Usuario eliminado");
        }
    }
}
