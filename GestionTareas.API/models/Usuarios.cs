﻿namespace GestionTareas.API.models
{
    public class Usuarios
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public bool IsActive { get; set; }
        public String PasswordHash { get; set; }
    }
}
