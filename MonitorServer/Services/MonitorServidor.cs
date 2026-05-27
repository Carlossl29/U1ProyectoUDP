using MonitorServer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Text.Json;

namespace MonitorServer.Services
{
    public class MonitorServidor
    {
        public List<ComputadoraModel> Computadoras { get; set; } = new List<ComputadoraModel>();
        int puerto = 7777;
        public UdpClient Server { get; set; } = new();

        public event Action? ActualizarTablero;

        public void IniciarServidor()
        {
            LeerHistorialJSON();
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, puerto);
            Server = new UdpClient(serverEP);
            Server.EnableBroadcast = true;
            Thread hilo = new(RecibirMensajes);
            hilo.IsBackground = true;
            hilo.Start();

            DescubrirComputadoras();
        }

        private void RecibirMensajes()
        {
            while (true)
            {
                try
                {
                    IPEndPoint clientEP = new IPEndPoint(IPAddress.None, 0);
                    byte[] buffer = Server.Receive(ref clientEP);
                    string comando = Encoding.UTF8.GetString(buffer);
                    string[] comandoSeparado = comando.Split("|");
                    if (comandoSeparado[0] == "CONECTAR" && comandoSeparado.Length > 1 && !string.IsNullOrWhiteSpace(comandoSeparado[1]) && !string.IsNullOrWhiteSpace(comandoSeparado[2]) && !string.IsNullOrWhiteSpace(comandoSeparado[3]))
                    {
                        if (Computadoras.Any(x => x.Laboratorio == comandoSeparado[1].ToUpper() && x.Nombre == comandoSeparado[2].ToUpper() && x.IP == clientEP.Address.ToString()))
                        {
                            var c = Computadoras.Where(x => x.Laboratorio == comandoSeparado[1].ToUpper() && x.Nombre == comandoSeparado[2].ToUpper() && x.IP == clientEP.Address.ToString()).FirstOrDefault();
                            if (c != null)
                            {
                                c.Conectado = true;
                                c.Encendida = true;
                                c.TieneInternet = bool.Parse(comandoSeparado[3]);
                                c.UltimaConexion = DateTime.Now;
                                c.Puerto = clientEP.Port; //ESTE POR SI ES NECESARIO CON PUERTOS DINAMICOS. SE ACTUALIZA EL PUERTO CON EL DEL ULTIMO CLIENTE QUE SE ABRIO CON EL MISMO LAB, NOMBRE E IP, AHORA ESA COMPUTADORA TIENE EL PUERTO DEL NUEVO CLIENTE ABIERTO Y NO DEL CLIENTE QUE LO ABRIÓ PRIMERO
                                IPEndPoint destino = new IPEndPoint(IPAddress.Parse(c.IP), c.Puerto);
                                ActualizarTablero?.Invoke();
                                /*EnviarComando("CONECTADO", destino);*/ //creo que no se va a usar
                            }
                        }
                        else
                        {
                            ComputadoraModel computadora = new ComputadoraModel()
                            {
                                IP = clientEP.Address.ToString(), 

                                Puerto = clientEP.Port, //este quizá no es necesario con puerto estático
                                Nombre = comandoSeparado[2].ToUpper(),
                                Laboratorio = comandoSeparado[1].ToUpper(),
                                TieneInternet = bool.Parse(comandoSeparado[3]),
                                Conectado = true,
                                Encendida = true,
                                UltimaConexion = DateTime.Now,
                            };
                            IPEndPoint destino = new IPEndPoint(IPAddress.Parse(computadora.IP), computadora.Puerto);
                            lock (Computadoras)
                            {
                                Computadoras.Add(computadora);
                            }
                            ActualizarTablero?.Invoke();
                            //EnviarComando("CONECTADO", destino);
                        }

                    }
                    if (comandoSeparado[0] == "ESTADO" && comandoSeparado.Length > 1 && !string.IsNullOrWhiteSpace(comandoSeparado[1]) && !string.IsNullOrWhiteSpace(comandoSeparado[2]) && !string.IsNullOrWhiteSpace(comandoSeparado[3]))
                    {
                        if (Computadoras.Any(x => x.Laboratorio == comandoSeparado[1] && x.Nombre == comandoSeparado[2] && x.IP == clientEP.Address.ToString()))
                        {
                            var c = Computadoras.Where(x => x.Laboratorio == comandoSeparado[1] && x.Nombre == comandoSeparado[2] && x.IP == clientEP.Address.ToString()).FirstOrDefault();
                            if (c != null)
                            {
                                c.Conectado = true;
                                c.Encendida = true;
                                c.TieneInternet = bool.Parse(comandoSeparado[3]);
                                c.UltimaConexion = DateTime.Now; //probablemente este sea el que hay que dejar
                                ActualizarTablero?.Invoke();
                            }
                        }
                    }
                    if (comandoSeparado[0] == "APAGADO" && comandoSeparado.Length > 1 && !string.IsNullOrWhiteSpace(comandoSeparado[1]) && !string.IsNullOrWhiteSpace(comandoSeparado[2]))
                    {
                        if (Computadoras.Any(x => x.Laboratorio == comandoSeparado[1] && x.Nombre == comandoSeparado[2] && x.IP == clientEP.Address.ToString()))
                        {
                            var c = Computadoras.Where(x => x.Laboratorio == comandoSeparado[1] && x.Nombre == comandoSeparado[2] && x.IP == clientEP.Address.ToString()).FirstOrDefault();
                            if (c != null)
                            {
                                c.Conectado = false;
                                c.Encendida = false;
                                c.TieneInternet = false;
                                c.UltimaConexion = DateTime.Now; //no sé si dejarlo para el apagado
                                ActualizarTablero?.Invoke();
                            }
                        }
                    }

                    GuardarHistorialJSON();
                }
                catch (Exception ex)
                {
                    ActualizarTablero?.Invoke();
                }
            }
        }

        public void EnviarComando(string comando, IPEndPoint cliente)
        {
            var mensaje = comando.ToUpper();
            byte[] buffer = Encoding.UTF8.GetBytes(mensaje);
            try
            {
                Server.Send(buffer, buffer.Length, cliente);
            }
            catch (Exception ex)
            {
                ActualizarTablero?.Invoke();
            }
        }
        public void ComprobarEstado(ComputadoraModel c)
        {
            IPEndPoint cliente = new IPEndPoint(IPAddress.Parse(c.IP), c.Puerto);
            var pc = Computadoras.Where(x => x.Laboratorio == c.Laboratorio && x.Nombre == c.Nombre && x.IP == c.IP).FirstOrDefault();
            if (pc != null)
            {
                pc.Encendida = false;
                pc.TieneInternet = false;
            }
            EnviarComando("ESTADO", cliente);

            Thread hiloActualizar = new Thread(() =>
            {
                Thread.Sleep(500);
                if (pc != null)
                {
                    //VERIFICAR SI ESTA APAGADA
                    if (pc.Encendida == false && pc.TieneInternet == false)
                    {
                        ActualizarTablero?.Invoke();
                    }
                }
            });
            hiloActualizar.IsBackground = true;
            hiloActualizar.Start();
        }

        public void ApagarComputadora(ComputadoraModel c)
        {
            IPEndPoint cliente = new IPEndPoint(IPAddress.Parse(c.IP), c.Puerto);
            var pc = Computadoras.Where(x => x.Laboratorio == c.Laboratorio && x.Nombre == c.Nombre && x.IP == c.IP).FirstOrDefault();
            if (pc != null)
            {
                //pc.Encendida = false;
                //pc.TieneInternet = false;
                EnviarComando("APAGAR", cliente);
                ActualizarTablero?.Invoke();
            }
        }

        public void EliminarComputadoraHistorial(ComputadoraHistorialModel c)
        {
            var pc = Computadoras.Where(x => x.Laboratorio == c.Laboratorio && x.Nombre == c.Nombre && x.IP == c.IP && x.UltimaConexion <= DateTime.Now.AddDays(-7)).FirstOrDefault();
            if (pc != null)
            {
                lock (Computadoras)
                {
                    Computadoras.Remove(pc);
                }
                GuardarHistorialJSON();
                ActualizarTablero?.Invoke();
            }
        }

        public void ComprobarEstadoTodas()
        {
            foreach (var computadora in Computadoras)
            {
                //if (computadora.Conectado == false)
                //{
                //    IPEndPoint cliente = new IPEndPoint(IPAddress.Parse(computadora.IP), computadora.Puerto);
                //    EnviarComando("DESCUBRIR", cliente);
                //}
                //else
                //{
                //    computadora.TieneInternet = false;
                //    computadora.Encendida = false;
                //    ComprobarEstado(computadora);
                //}

                computadora.TieneInternet = false;
                computadora.Encendida = false;
                ComprobarEstado(computadora);
            }

            Thread hiloActualizar = new Thread(() =>
            {
                Thread.Sleep(500);
                ActualizarTablero?.Invoke();
            });
            hiloActualizar.IsBackground = true;
            hiloActualizar.Start();
        }

        public void DescubrirComputadoras()
        {
            IPEndPoint broadcast = new IPEndPoint(IPAddress.Broadcast, 8888);  //aqui me quede
            var comando = "DESCUBRIR";
            byte[] buffer = Encoding.UTF8.GetBytes(comando);
            Server.Send(buffer, buffer.Length, broadcast);
        }

        public IEnumerable<ComputadoraModel> GetHistorial()
        {
            return Computadoras.Where(x => x.UltimaConexion <= DateTime.Now.AddDays(-7));
        }

        public void LeerHistorialJSON()
        {
            if (File.Exists("historial.json"))
            {
                string jsonLeido = File.ReadAllText("historial.json");
                var computadoras = JsonSerializer.Deserialize<List<ComputadoraModel>>(jsonLeido);
                if (computadoras != null)
                {
                    foreach (var pc in computadoras)
                    {
                        pc.Conectado = false;
                        pc.Encendida = false;
                        pc.TieneInternet = false;
                    }
                    Computadoras = computadoras;
                }
            }
        }
        public void GuardarHistorialJSON()
        {
            string json = JsonSerializer.Serialize(Computadoras);
            File.WriteAllText("historial.json", json);
        }

    }
}
