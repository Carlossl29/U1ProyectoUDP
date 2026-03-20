using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MonitorServer.Models
{
    public class ComputadoraModel
    {
        public string IP { get; set; } = null!;
        public int Puerto { get; set; }
        public string Nombre { get; set; } = null!;
        public string Laboratorio { get; set; } = null!;
        public bool TieneInternet { get; set; }
        public bool Conectado { get; set;  }
        public bool Encendida { get; set; }
        public DateTime UltimaConexion { get; set; }

    }
}
