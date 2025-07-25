using Dapper;
using GestionTareas.API.models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GestionTareas.API.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
    public abstract class BaseRepository
    {
        protected readonly IDbConnectionFactory _connectionFactory;

        protected BaseRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        protected async Task<T> QuerySingleAsync<T>(string sql, object parameters = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);
        }

        protected async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<T>(sql, parameters);
        }

        protected async Task<int> ExecuteAsync(string sql, object parameters = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteAsync(sql, parameters);
        }
    }

    // 4. Repositorio de Usuarios
    public interface IUsuarioRepository
    {
        Task<Usuarios> GetByIdAsync(int id);
        Task<IEnumerable<Usuarios>> GetAllActiveAsync();
        Task<Usuarios> GetByEmailAsync(string email);
        Task<int> CreateAsync(Usuarios usuario);
        Task UpdateAsync(Usuarios usuario);
        Task DeactivateAsync(int id);
    }

    public class UsuarioRepository : BaseRepository, IUsuarioRepository
    {
        public UsuarioRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<Usuarios> GetByIdAsync(int id)
        {
            const string sql = @"
            SELECT Id, Email, Nombre, IsActive 
            FROM Usuarios 
            WHERE Id = @Id AND IsActive = 1";

            return await QuerySingleAsync<Usuarios>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Usuarios>> GetAllActiveAsync()
        {
            const string sql = @"
            SELECT Id, Email, Nombre, IsActive 
            FROM Usuarios 
            WHERE IsActive = 1
            ORDER BY Nombre";

            return await QueryAsync<Usuarios>(sql);
        }

        public async Task<Usuarios> GetByEmailAsync(string email)
        {
            const string sql = @"
            SELECT Id, Email, Nombre, IsActive 
            FROM Usuarios 
            WHERE Email = @Email AND IsActive = 1";

            return await QuerySingleAsync<Usuarios>(sql, new { Email = email });
        }

        public async Task<int> CreateAsync(Usuarios usuario)
        {
            const string sql = @"
            INSERT INTO Usuarios (Email, Nombre, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Email, @Nombre, @IsActive)";

            return await QuerySingleAsync<int>(sql, usuario);
        }

        public async Task UpdateAsync(Usuarios usuario)
        {
            const string sql = @"
            UPDATE Usuarios 
            SET Email = @Email, Nombre = @Nombre, IsActive = @IsActive
            WHERE Id = @Id";

            await ExecuteAsync(sql, usuario);
        }

        public async Task DeactivateAsync(int id)
        {
            const string sql = "UPDATE Usuarios SET IsActive = 0 WHERE Id = @Id";
            await ExecuteAsync(sql, new { Id = id });
        }
    }

    // 5. Repositorio de Proyectos
    public interface IProyectoRepository
    {
        Task<Proyectos> GetByIdAsync(int id);
        Task<Proyectos> GetByIdWithTareasAsync(int id);
        Task<IEnumerable<Proyectos>> GetAllActiveAsync();
        Task<IEnumerable<Proyectos>> GetByCreadorAsync(int creadorId);
        Task<int> CreateAsync(Proyectos proyecto);
        Task UpdateAsync(Proyectos proyecto);
        Task DeactivateAsync(int id);
    }

    public class ProyectoRepository : BaseRepository, IProyectoRepository
    {
        public ProyectoRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<Proyectos> GetByIdAsync(int id)
        {
            const string sql = @"
            SELECT p.Id, p.Nombre, p.Descripcion, p.CreacionUserId, p.IsActive,
                   u.Id, u.Email, u.Nombre, u.IsActive
            FROM Proyectos p
            LEFT JOIN Usuarios u ON p.CreacionUserId = u.Id
            WHERE p.Id = @Id AND p.IsActive = 1";

            using var connection = _connectionFactory.CreateConnection();
            var proyectos = await connection.QueryAsync<Proyectos, Usuarios, Proyectos>(
                sql,
                (proyecto, usuario) =>
                {
                    proyecto.Creacion = usuario;
                    return proyecto;
                },
                new { Id = id },
                splitOn: "Id"
            );

            return proyectos.FirstOrDefault();
        }

        public async Task<Proyectos> GetByIdWithTareasAsync(int id)
        {
            const string sql = @"
            SELECT p.Id, p.Nombre, p.Descripcion, p.CreacionUserId, p.IsActive,
                   u.Id, u.Email, u.Nombre, u.IsActive,
                   t.Id, t.Titulo, t.Descripcion, t.Status, t.Prioridad, t.ProjectoId, 
                   t.AsignacionUserId, t.CreacionUserId,
                   ua.Id, ua.Email, ua.Nombre, ua.IsActive,
                   uc.Id, uc.Email, uc.Nombre, uc.IsActive
            FROM Proyectos p
            LEFT JOIN Usuarios u ON p.CreacionUserId = u.Id
            LEFT JOIN Tareas t ON p.Id = t.ProjectoId
            LEFT JOIN Usuarios ua ON t.AsignacionUserId = ua.Id
            LEFT JOIN Usuarios uc ON t.CreacionUserId = uc.Id
            WHERE p.Id = @Id AND p.IsActive = 1";

            using var connection = _connectionFactory.CreateConnection();
            var proyectoDictionary = new Dictionary<int, Proyectos>();

            await connection.QueryAsync<Proyectos, Usuarios, Tareas, Usuarios, Usuarios, Proyectos>(
                sql,
                (proyecto, creador, tarea, asignado, creadorTarea) =>
                {
                    if (!proyectoDictionary.TryGetValue(proyecto.Id, out var proyectoEntry))
                    {
                        proyectoEntry = proyecto;
                        proyectoEntry.Creacion = creador;
                        proyectoEntry.Tareas = new List<Tareas>();
                        proyectoDictionary.Add(proyecto.Id, proyectoEntry);
                    }

                    if (tarea != null)
                    {
                        tarea.Asignacion = asignado;
                        tarea.Creacion = creadorTarea;
                        proyectoEntry.Tareas.Add(tarea);
                    }

                    return proyectoEntry;
                },
                new { Id = id },
                splitOn: "Id,Id,Id,Id"
            );

            return proyectoDictionary.Values.FirstOrDefault();
        }

        public async Task<IEnumerable<Proyectos>> GetAllActiveAsync()
        {
            const string sql = @"
            SELECT p.Id, p.Nombre, p.Descripcion, p.CreacionUserId, p.IsActive,
                   u.Id, u.Email, u.Nombre, u.IsActive
            FROM Proyectos p
            LEFT JOIN Usuarios u ON p.CreacionUserId = u.Id
            WHERE p.IsActive = 1
            ORDER BY p.Nombre";

            using var connection = _connectionFactory.CreateConnection();
            var proyectos = await connection.QueryAsync<Proyectos, Usuarios, Proyectos>(
                sql,
                (proyecto, usuario) =>
                {
                    proyecto.Creacion = usuario;
                    return proyecto;
                },
                splitOn: "Id"
            );

            return proyectos;
        }

        public async Task<IEnumerable<Proyectos>> GetByCreadorAsync(int creadorId)
        {
            const string sql = @"
            SELECT p.Id, p.Nombre, p.Descripcion, p.CreacionUserId, p.IsActive,
                   u.Id, u.Email, u.Nombre, u.IsActive
            FROM Proyectos p
            LEFT JOIN Usuarios u ON p.CreacionUserId = u.Id
            WHERE p.CreacionUserId = @CreadorId AND p.IsActive = 1
            ORDER BY p.Nombre";

            using var connection = _connectionFactory.CreateConnection();
            var proyectos = await connection.QueryAsync<Proyectos, Usuarios, Proyectos>(
                sql,
                (proyecto, usuario) =>
                {
                    proyecto.Creacion = usuario;
                    return proyecto;
                },
                new { CreadorId = creadorId },
                splitOn: "Id"
            );

            return proyectos;
        }

        public async Task<int> CreateAsync(Proyectos proyecto)
        {
            const string sql = @"
            INSERT INTO Proyectos (Nombre, Descripcion, CreacionUserId, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Descripcion, @CreacionUserId, @IsActive)";

            return await QuerySingleAsync<int>(sql, proyecto);
        }

        public async Task UpdateAsync(Proyectos proyecto)
        {
            const string sql = @"
            UPDATE Proyectos 
            SET Nombre = @Nombre, Descripcion = @Descripcion, 
                CreacionUserId = @CreacionUserId, IsActive = @IsActive
            WHERE Id = @Id";

            await ExecuteAsync(sql, proyecto);
        }

        public async Task DeactivateAsync(int id)
        {
            const string sql = "UPDATE Proyectos SET IsActive = 0 WHERE Id = @Id";
            await ExecuteAsync(sql, new { Id = id });
        }
    }

    // 6. Repositorio de Tareas
    public interface ITareaRepository
    {
        Task<Tareas> GetByIdAsync(int id);
        Task<IEnumerable<Tareas>> GetByProjectoIdAsync(int proyectoId);
        Task<IEnumerable<Tareas>> GetByAsignadoIdAsync(int usuarioId);
        Task<IEnumerable<Tareas>> GetByStatusAsync(TareaStatus status);
        Task<int> CreateAsync(Tareas tarea);
        Task UpdateAsync(Tareas tarea);
        Task DeleteAsync(int id);
    }

    public class TareaRepository : BaseRepository, ITareaRepository
    {
        public TareaRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

        public async Task<Tareas> GetByIdAsync(int id)
        {
            const string sql = @"
            SELECT t.Id, t.Titulo, t.Descripcion, t.Status, t.Prioridad, 
                   t.ProjectoId, t.AsignacionUserId, t.CreacionUserId,
                   p.Id, p.Nombre, p.Descripcion, p.CreacionUserId, p.IsActive,
                   ua.Id, ua.Email, ua.Nombre, ua.IsActive,
                   uc.Id, uc.Email, uc.Nombre, uc.IsActive
            FROM Tareas t
            LEFT JOIN Proyectos p ON t.ProjectoId = p.Id
            LEFT JOIN Usuarios ua ON t.AsignacionUserId = ua.Id
            LEFT JOIN Usuarios uc ON t.CreacionUserId = uc.Id
            WHERE t.Id = @Id";

            using var connection = _connectionFactory.CreateConnection();
            var tareas = await connection.QueryAsync<Tareas, Proyectos, Usuarios, Usuarios, Tareas>(
                sql,
                (tarea, proyecto, asignado, creador) =>
                {
                    tarea.Project = proyecto;
                    tarea.Asignacion = asignado;
                    tarea.Creacion = creador;
                    return tarea;
                },
                new { Id = id },
                splitOn: "Id,Id,Id"
            );

            return tareas.FirstOrDefault();
        }

        public async Task<IEnumerable<Tareas>> GetByProjectoIdAsync(int proyectoId)
        {
            const string sql = @"
            SELECT t.Id, t.Titulo, t.Descripcion, t.Status, t.Prioridad, 
                   t.ProjectoId, t.AsignacionUserId, t.CreacionUserId,
                   ua.Id, ua.Email, ua.Nombre, ua.IsActive,
                   uc.Id, uc.Email, uc.Nombre, uc.IsActive
            FROM Tareas t
            LEFT JOIN Usuarios ua ON t.AsignacionUserId = ua.Id
            LEFT JOIN Usuarios uc ON t.CreacionUserId = uc.Id
            WHERE t.ProjectoId = @ProyectoId
            ORDER BY t.Prioridad DESC, t.Id";

            using var connection = _connectionFactory.CreateConnection();
            var tareas = await connection.QueryAsync<Tareas, Usuarios, Usuarios, Tareas>(
                sql,
                (tarea, asignado, creador) =>
                {
                    tarea.Asignacion = asignado;
                    tarea.Creacion = creador;
                    return tarea;
                },
                new { ProyectoId = proyectoId },
                splitOn: "Id,Id"
            );

            return tareas;
        }

        public async Task<IEnumerable<Tareas>> GetByAsignadoIdAsync(int usuarioId)
        {
            const string sql = @"
            SELECT t.Id, t.Titulo, t.Descripcion, t.Status, t.Prioridad, 
                   t.ProjectoId, t.AsignacionUserId, t.CreacionUserId,
                   p.Id, p.Nombre, p.Descripcion, p.CreacionUserId, p.IsActive
            FROM Tareas t
            LEFT JOIN Proyectos p ON t.ProjectoId = p.Id
            WHERE t.AsignacionUserId = @UsuarioId
            ORDER BY t.Prioridad DESC, t.Status, t.Id";

            using var connection = _connectionFactory.CreateConnection();
            var tareas = await connection.QueryAsync<Tareas, Proyectos, Tareas>(
                sql,
                (tarea, proyecto) =>
                {
                    tarea.Project = proyecto;
                    return tarea;
                },
                new { UsuarioId = usuarioId },
                splitOn: "Id"
            );

            return tareas;
        }

        public async Task<IEnumerable<Tareas>> GetByStatusAsync(TareaStatus status)
        {
            const string sql = @"
            SELECT t.Id, t.Titulo, t.Descripcion, t.Status, t.Prioridad, 
                   t.ProjectoId, t.AsignacionUserId, t.CreacionUserId,
                   p.Id, p.Nombre, p.Descripcion, p.CreacionUserId, p.IsActive,
                   ua.Id, ua.Email, ua.Nombre, ua.IsActive
            FROM Tareas t
            LEFT JOIN Proyectos p ON t.ProjectoId = p.Id
            LEFT JOIN Usuarios ua ON t.AsignacionUserId = ua.Id
            WHERE t.Status = @Status
            ORDER BY t.Prioridad DESC, t.Id";

            using var connection = _connectionFactory.CreateConnection();
            var tareas = await connection.QueryAsync<Tareas, Proyectos, Usuarios, Tareas>(
                sql,
                (tarea, proyecto, asignado) =>
                {
                    tarea.Project = proyecto;
                    tarea.Asignacion = asignado;
                    return tarea;
                },
                new { Status = (int)status },
                splitOn: "Id,Id"
            );

            return tareas;
        }

        public async Task<int> CreateAsync(Tareas tarea)
        {
            const string sql = @"
            INSERT INTO Tareas (Titulo, Descripcion, Status, Prioridad, 
                              ProjectoId, AsignacionUserId, CreacionUserId)
            OUTPUT INSERTED.Id
            VALUES (@Titulo, @Descripcion, @Status, @Prioridad, 
                   @ProjectoId, @AsignacionUserId, @CreacionUserId)";

            return await QuerySingleAsync<int>(sql, new
            {
                tarea.Titulo,
                tarea.Descripcion,
                Status = (int)tarea.Status,
                Prioridad = (int)tarea.Prioridad,
                tarea.ProjectoId,
                tarea.AsignacionUserId,
                tarea.CreacionUserId
            });
        }

        public async Task UpdateAsync(Tareas tarea)
        {
            const string sql = @"
            UPDATE Tareas 
            SET Titulo = @Titulo, Descripcion = @Descripcion, 
                Status = @Status, Prioridad = @Prioridad,
                AsignacionUserId = @AsignacionUserId
            WHERE Id = @Id";

            await ExecuteAsync(sql, new
            {
                tarea.Id,
                tarea.Titulo,
                tarea.Descripcion,
                Status = (int)tarea.Status,
                Prioridad = (int)tarea.Prioridad,
                tarea.AsignacionUserId
            });
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Tareas WHERE Id = @Id";
            await ExecuteAsync(sql, new { Id = id });
        }
    }

    // 7. Configuración de Dependency Injection
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDapperServices(this IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IProyectoRepository, ProyectoRepository>();
            services.AddScoped<ITareaRepository, TareaRepository>();

            return services;
        }
    }

}
