using Microsoft.CodeAnalysis;

namespace GestionTareas.MVC.models
{
    public class Proyectos
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int CreacionUserId { get; set; }
        public bool IsActive { get; set; }
        // Navigation properties
        public Usuarios? Creacion { get; set; }
        public List<Tareas> Tareas { get; set; } = new();

    }
}
