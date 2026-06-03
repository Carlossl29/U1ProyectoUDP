using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;

namespace MonitorClient.Services
{
    public class MonitorCliente
    {
        public string? Identificador { get; set; }
        public IPAddress? IpServidor { get; set; }

        UdpClient Cliente = new UdpClient(8888);

        int puertoServer = 7777;

        public event Action? ContadorIniciado;
        public event Action<string>? ClienteEscuchando;

        public MonitorCliente()
        {
            CargarIdentificador();
        }

        public void Conectar(string identificador)
        {
            identificador = identificador.Replace('|', '\0');
            var idSeparado = identificador.Split("-");
            bool internet = TieneInternet();
            Cliente.EnableBroadcast = true;
            //IPEndPoint remoto = new IPEndPoint(ipServer, puertoServer);


            if (string.IsNullOrWhiteSpace(Identificador))
            {
                Identificador = identificador;
                GuardarIdentificador();
            }
            else
            {
                ClienteEscuchando?.Invoke(Identificador);
            }

            Thread hilo = new(RecibirComandos);
            hilo.IsBackground = true;
            hilo.Start();

            //IPEndPoint remoto = new IPEndPoint(IPAddress.Broadcast, puertoServer);
            var comando = $"CONECTAR|{idSeparado[0].ToUpper()}|{idSeparado[1].ToUpper()}|{internet}";
            byte[] buffer = Encoding.UTF8.GetBytes(comando);

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var props = ni.GetIPProperties();

                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (ua.IPv4Mask == null)
                        continue;

                    byte[] ip = ua.Address.GetAddressBytes();
                    byte[] mask = ua.IPv4Mask.GetAddressBytes();
                    byte[] broadcast = new byte[4];

                    for (int i = 0; i < 4; i++)
                        broadcast[i] = (byte)(ip[i] | ~mask[i]);

                    var broadcastIp = new IPAddress(broadcast);

                    try
                    {
                        IPEndPoint remoto = new IPEndPoint(broadcastIp, puertoServer);
                        Cliente.Send(buffer, buffer.Length, remoto);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error enviando broadcast a {broadcastIp}: {ex.Message}");
                    }
                }
            }

            //Cliente.Send(buffer, buffer.Length, remoto);

            //IpServidor = remoto.Address;
        }
        public void RecibirComandos()
        {
            while (true)
            {
                try
                {
                    IPEndPoint remoto = new(IPAddress.None, 0);
                    byte[] buffer = Cliente.Receive(ref remoto);
                    var comando = Encoding.UTF8.GetString(buffer);

                    if (comando == "DESCUBRIR" || comando == "ESTADO")
                    {
                        if (Identificador != null)
                        {
                            var idSeparado = Identificador.Split("-");
                            bool internet = TieneInternet();
                            var respuesta = $"{(comando == "DESCUBRIR" ? "CONECTAR" : "ESTADO")}|{idSeparado[0].ToUpper()}|{idSeparado[1].ToUpper()}|{internet}";
                            byte[] buffer2 = Encoding.UTF8.GetBytes(respuesta);
                            Cliente.Send(buffer2, buffer2.Length, remoto);
                        }
                    }
                    if (comando == "APAGAR")
                    {
                        ContadorIniciado?.Invoke();
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private bool TieneInternet()
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send("8.8.8.8", 1000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        public void ApagarPC()
        {
            try
            {
                if (Identificador != null)
                {
                    var idSeparado = Identificador.Split("-");
                    //IPEndPoint remoto = new IPEndPoint(IPAddress.Broadcast, puertoServer);
                    var comando = $"APAGADO|{idSeparado[0].ToUpper()}|{idSeparado[1].ToUpper()}";
                    byte[] buffer = Encoding.UTF8.GetBytes(comando);

                    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                            continue;

                        var props = ni.GetIPProperties();

                        foreach (var ua in props.UnicastAddresses)
                        {
                            if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                                continue; 

                            if (ua.IPv4Mask == null)
                                continue;

                            byte[] ip = ua.Address.GetAddressBytes();
                            byte[] mask = ua.IPv4Mask.GetAddressBytes();
                            byte[] broadcast = new byte[4];

                            for (int i = 0; i < 4; i++)
                                broadcast[i] = (byte)(ip[i] | ~mask[i]);

                            var broadcastIp = new IPAddress(broadcast);

                            try
                            {
                                IPEndPoint remoto = new IPEndPoint(broadcastIp, puertoServer);
                                Cliente.Send(buffer, buffer.Length, remoto);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error enviando broadcast a {broadcastIp}: {ex.Message}");
                            }
                        }

                        //Cliente.Send(buffer, buffer.Length, remoto);
                    }

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/s /t 0",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al apagar: " + ex.Message);
            }
        }

        public void GuardarIdentificador()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Identificador))
                {
                    File.WriteAllText("identificador.json", Identificador);
                }
            }
            catch
            {

            }
        }

        public void CargarIdentificador()
        {
            try
            {
                if (File.Exists("identificador.json"))
                {
                    Identificador = File.ReadAllText("identificador.json");
                    Conectar(Identificador);
                }
            }
            catch
            {

            }
        }
    }
}
