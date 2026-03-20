using CommunityToolkit.Mvvm.Input;
using MonitorServer.Models;
using MonitorServer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MonitorServer.ViewModels
{
    public enum Vistas { Laboratorios, Computadoras, Historial }
    public class ServidorViewModel : INotifyPropertyChanged
    {
        public ServidorViewModel()
        {
            hiloUI = Application.Current.Dispatcher;
            servidor.IniciarServidor();
            SeleccionarLaboratorioCommand = new RelayCommand<LaboratorioModel>(SeleccionarLaboratorio);
            RegresarLaboratoriosCommand = new RelayCommand(RegresarLaboratorios);
            ComprobarEstadoCommand = new RelayCommand<ComputadoraModel>(ComprobarEstado);
            ApagarComputadoraCommand = new RelayCommand<ComputadoraModel>(ApagarComputadora);
            ComprobarEstadoTodasCommand = new RelayCommand(ComprobarEstadoTodas);
            DescubrirComputadorasCommand = new RelayCommand(DescubrirComputadoras);
            VerHistorialCommand = new RelayCommand(VerHistorial);
            servidor.ActualizarTablero += Servidor_ActualizarTablero;
        }

        private MonitorServidor servidor = new MonitorServidor();
        Dispatcher hiloUI;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand SeleccionarLaboratorioCommand { get; set; }
        public ICommand RegresarLaboratoriosCommand { get; set; }
        public ICommand ComprobarEstadoCommand { get; set; }
        public ICommand ApagarComputadoraCommand { get; set; }
        public ICommand ComprobarEstadoTodasCommand { get; set; }
        public ICommand VerHistorialCommand { get; set; }
        public ICommand DescubrirComputadorasCommand { get; set; }
        public Vistas Vista { get; set; }
        public string? LaboratorioSeleccionado { get; set; }
        private List<ComputadoraModel> ListaComputadoras { get; set; } = new List<ComputadoraModel>();
        public ObservableCollection<LaboratorioModel> Laboratorios { get; set; } = new ObservableCollection<LaboratorioModel>();
        public ObservableCollection<ComputadoraModel> Computadoras { get; set; } = new ObservableCollection<ComputadoraModel>();
        public ObservableCollection<ComputadoraHistorialModel> Historial { get; set; } = new ObservableCollection<ComputadoraHistorialModel>();

        private void Servidor_ActualizarTablero()
        {

            hiloUI.BeginInvoke(() =>
            {
                ListaComputadoras = servidor.Computadoras.ToList();
                ActualizarLaboratorios();
                ActualizarComputadoras();
                ActualizarHistorial();
            });
        }
        private void SeleccionarLaboratorio(LaboratorioModel? lab)
        {
            if (lab != null && lab.Nombre !=  null)
            {
                LaboratorioSeleccionado = lab.Nombre.ToUpper();
                Vista = Vistas.Computadoras;
                OnPropertyChanged(nameof(Vista));
                OnPropertyChanged(nameof(LaboratorioSeleccionado));
                ActualizarComputadoras();
            }
        }
        private void VerHistorial()
        {
            ActualizarHistorial();
            Vista = Vistas.Historial;
            OnPropertyChanged(nameof(Vista));
        }

        private void ActualizarHistorial()
        {
            Historial.Clear();
            var historial = servidor.GetHistorial();
            foreach (var pc in historial)
            {
                var pchistorial = new ComputadoraHistorialModel()
                {
                    IP = pc.IP,
                    Nombre = pc.Nombre,
                    Laboratorio = pc.Laboratorio,
                    UltimaConexion = pc.UltimaConexion,
                };
                Historial.Add(pchistorial);
            }
        }

        private void ComprobarEstado(ComputadoraModel? pc)
        {
            if (pc!= null)
            {
                servidor.ComprobarEstado(pc);
            }
        }

        public void ComprobarEstadoTodas()
        {
            servidor.ComprobarEstadoTodas();
        }

        private void ApagarComputadora(ComputadoraModel? pc)
        {
            if (pc != null)
            {
                servidor.ApagarComputadora(pc);
            }
        }

        public void ActualizarLaboratorios()
        {
            Laboratorios.Clear();
            var laboratorios = ListaComputadoras.Where(x=>x.Conectado == true).GroupBy(x => x.Laboratorio).Select(g => new LaboratorioModel { Nombre = g.Key.ToUpper(), Cantidad = g.Count() }).OrderBy(x=>x.Nombre);
            foreach (var lab in laboratorios)
            {
                Laboratorios.Add(lab);

            }
        }
        private void RegresarLaboratorios()
        {
            LaboratorioSeleccionado = "";
            Vista = Vistas.Laboratorios;
            OnPropertyChanged(nameof(LaboratorioSeleccionado));
            OnPropertyChanged(nameof(Vista));
        }
        private void ActualizarComputadoras()
        {
            if (!string.IsNullOrWhiteSpace(LaboratorioSeleccionado))
            {
                Computadoras.Clear();
                foreach (var pc in ListaComputadoras.Where(x => x.Laboratorio.ToUpper() == LaboratorioSeleccionado.ToUpper() && x.Conectado == true).OrderBy(x=>x.Nombre))
                {
                    Computadoras.Add(pc);
                }
            }
        }
        private void DescubrirComputadoras()
        {
            servidor.DescubrirComputadoras();
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
