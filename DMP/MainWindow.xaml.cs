using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace DMP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, Core.Interfaces.ISimpleMediaPlayer
    {
        private Core.Controllers.Scheduler scheduler;
        public MainWindow()
        {
            InitializeComponent();
            mediaPlayer.MediaEnded += (sender, e) => OnPlaybackCompleted?.Invoke(sender, e);
            scheduler = new Core.Controllers.Scheduler(this);
        }

        public event EventHandler OnPlaybackCompleted;

        private void Button_LoadSchedule_Click(object sender, RoutedEventArgs e)
        {
            var file = new OpenFileDialog();
            if(file.ShowDialog().Value) scheduler.LoadSchedule(new FileInfo( file.FileName));
        }


        private void mediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException.Message + Environment.NewLine + e.ErrorException.StackTrace, "Media failed");
        }


        public bool OpenFile(FileInfo file)
        {
            this.Dispatcher.Invoke(() => mediaPlayer.Source = new Uri(file.FullName));
            return true;
        }
        public void DisplayError(string message)
        {
            this.Dispatcher.Invoke(() => MessageBox.Show(message, "ERROR"));
        }
        public bool Pause()
        {
            try
            {
                this.Dispatcher.Invoke(() => mediaPlayer.Pause());
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool Play()
        {
            try
            {
                this.Dispatcher.Invoke(() => mediaPlayer.Play());
            }
            catch(Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool StopPlayback()
        {
            try
            {
                this.Dispatcher.Invoke(() => 
                { 
                    mediaPlayer.Stop();
                    mediaPlayer.Close();
                });
            }
            catch(Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool SetMediaPosition(TimeSpan time)
        {
            try
            {
                mediaPlayer.Position = time;
            }
            catch(Exception ex)
            {
                return false;
            }
            return true;
        }

        public TimeSpan GetMediaPosition()
        {
            return this.Dispatcher.Invoke(() => mediaPlayer.Position);
             
        }
    }
}
