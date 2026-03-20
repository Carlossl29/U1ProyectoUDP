using System;
using System.Collections.Generic;
using System.Text;

namespace MonitorServer.Models
{
    public class ComputadoraHistorialModel
    {
        public string Laboratorio { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string IP { get; set; } = null!;
        public DateTime UltimaConexion { get; set; }
    }
}
