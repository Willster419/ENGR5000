using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace Threading_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread _thread;
        private BackgroundWorker _backgroundworker;
        private DispatcherTimer _dispatcherTimer;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += LoadUserCode;
        }

        private void LoadUserCode(object sender, RoutedEventArgs e)
        {
            //testing the backgroundworker
            _backgroundworker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _backgroundworker.DoWork += GetThread;
            _backgroundworker.RunWorkerCompleted += GottenThread;
            _backgroundworker.RunWorkerAsync();
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += Tick1;
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            _dispatcherTimer.IsEnabled = true;
            _dispatcherTimer.Start();
        }

        private void GottenThread(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        private void GetThread(object sender, DoWorkEventArgs e)
        {
            
            while (true) ;
        }

        private void Tick1(object sender, EventArgs e)
        {
            
        }

        private void nothing()
        {

        }
    }
}
