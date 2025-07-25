﻿using Microsoft.CodeAnalysis;

namespace GestionTareas.API.models
{
    public enum TareaStatus
    {
        Pendiente = 0,
        EnProgreso = 1,
        Completado = 2
    }

    public enum TareaPrioridad
    {
        corto = 0,
        Mediano = 1,
        alto = 2,
        Critico = 3
    }
    public class Tareas
    {
            public int Id { get; set; }
            public string Titulo { get; set; }
            public string Descripcion { get; set; }
            public TareaStatus Status { get; set; }
            public TareaPrioridad Prioridad { get; set; }
            public int ProjectoId { get; set; }
            public int? AsignacionUserId { get; set; }
            public int CreacionUserId { get; set; }

            // Navigation properties
            public Proyectos? Project { get; set; }
            public Usuarios? Asignacion { get; set; }
            public Usuarios? Creacion { get; set; }
    }
}