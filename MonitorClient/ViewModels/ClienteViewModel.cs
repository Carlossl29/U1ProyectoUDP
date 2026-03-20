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

namespace MonitorClient.ViewModels
{
    public class ClienteViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string? DireccionIP { get; set; } = "127.0.0.1";
        public string? Identificador { get; set; }
        public string? Mensaje { get; set; }
        public bool ConectarBoton { get; set; } = true;
        public ICommand ConectarCommand { get; set; }

        private MonitorCliente service = new MonitorCliente();
        public ClienteViewModel()
        {
            ConectarCommand = new RelayCommand(Conectar);
        }
        private void Conectar()
        {
            Mensaje = "";
            try
            {
                if (!IPAddress.TryParse(DireccionIP, out IPAddress? ip))
                {
                    Mensaje = "Escriba una dirección IP válida.";
                    OnPropertyChanged(nameof(Mensaje));
                    return;
                }
                if (string.IsNullOrWhiteSpace(Identificador) || Identificador.Split("-").Length != 2)
                {
                    Mensaje = "Escriba el identificador con el formato LAB-PC (Ejemplo: LAB01-PC01).";
                    OnPropertyChanged(nameof(Mensaje));
                    return;
                }
                service.Conectar(ip, Identificador);
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
