using CommunityToolkit.Mvvm.Input;
using MonitorClient.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MonitorClient.ViewModels
{
    public enum Vistas { Cliente, ApagarComputadora }
    public class ClienteViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        //public string? DireccionIP { get; set; } = "127.0.0.1";
        public Vistas Vista { get; set; } = Vistas.Cliente;
        public string? Identificador { get; set; }
        public string? Mensaje { get; set; }
        public bool ConectarBoton { get; set; } = true;
        public int ContadorApagado { get; set; }
        private DispatcherTimer timerApagado;
        public ICommand ConectarCommand { get; set; }
        public ICommand CancelarApagarComputadoraCommand { get; set; }

        private MonitorCliente service = new MonitorCliente();
        public ClienteViewModel()
        {
            ConectarCommand = new RelayCommand(Conectar);
            CancelarApagarComputadoraCommand = new RelayCommand(CancelarApagarComputadora);
            timerApagado = new DispatcherTimer();
            timerApagado.Interval = TimeSpan.FromSeconds(1);
            service.ContadorIniciado += Service_ContadorIniciado;
            service.ClienteEscuchando += Service_ClienteEscuchando;
            timerApagado.Tick += TimerApagado_Tick;
            service.CargarIdentificador();
        }

        private void Service_ClienteEscuchando(string identificador)
        {
            Identificador = identificador;
            ConectarBoton = false;
            Mensaje = "Conectado";
            OnPropertyChanged(nameof(ConectarBoton));
            OnPropertyChanged(nameof(Identificador));
            OnPropertyChanged(nameof(Mensaje));
        }

        private void CancelarApagarComputadora()
        {
            timerApagado.Stop();
            Vista = Vistas.Cliente;
            OnPropertyChanged(nameof(Vista));
        }

        private void Service_ContadorIniciado()
        {
            ContadorApagado = 10;
            timerApagado.Start();
            Vista = Vistas.ApagarComputadora;
            OnPropertyChanged(nameof(Vista));
        }

        private void TimerApagado_Tick(object? sender, EventArgs e)
        {
            ContadorApagado--;
            OnPropertyChanged(nameof(ContadorApagado));

            if (ContadorApagado <= 0)
            {
                timerApagado.Stop();
                service.ApagarPC();
            }
        }

        private void Conectar()
        {
            Mensaje = "";
            try
            {
                //if (!IPAddress.TryParse(DireccionIP, out IPAddress? ip))
                //{
                //    Mensaje = "Escriba una dirección IP válida.";
                //    OnPropertyChanged(nameof(Mensaje));
                //    return;
                //}
                if (string.IsNullOrWhiteSpace(Identificador) || Identificador.Split("-").Length != 2)
                {
                    Mensaje = "Escriba el identificador con el formato LAB-PC (Ejemplo: LAB01-PC01).";
                    OnPropertyChanged(nameof(Mensaje));
                    return;
                }
                service.Conectar(Identificador);
                ConectarBoton = false;
                OnPropertyChanged(nameof(ConectarBoton));
                OnPropertyChanged(nameof(Mensaje));
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
                OnPropertyChanged(nameof(Mensaje));
            }
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
