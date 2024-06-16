using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace wpfSignalRClient
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private HubConnection connection;
        private bool _isEnableSendButton;

        public bool IsEnableSendButton
        {
            get { return _isEnableSendButton; }
            set { _isEnableSendButton = value; OnProepertyChanged(); }
        }

        private string _message;

        public string Message
        {
            get { return _message; }
            set { _message = value; OnProepertyChanged(); }
        }
        private string _userName;

        public string UserName
        {
            get { return _userName; }
            set { _userName = value; OnProepertyChanged(); }
        }
 


        private ObservableCollection<string> _chats;

        public ObservableCollection<string> Chats
        {
            get { return _chats; }
            set { _chats = value; }
        }

        public ICommand MessageSendCommand { get; set; }
        public ICommand ConnectCommand { get; set; }

        public MainViewModel()
        {
            _userName = string.Empty;
            _chats = new ObservableCollection<string>();
            _message = string.Empty;
            _isEnableSendButton = false;
            MessageSendCommand = new RelayCommand(Send);
            ConnectCommand = new RelayCommand(Connect);

            connection = new HubConnectionBuilder()
                        .WithUrl("https://localhost:7039/chatHub")
                        .Build();
            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
        }

        private async void Connect(object obj)
        {
            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                App.Current.Dispatcher.BeginInvoke(() =>
                {
                    var newMessage = $"{user}: {message}";
                    Chats.Add(newMessage);                    
                });
            });

            try
            {
                await connection.StartAsync();
                Chats.Add("Connection started");
                IsEnableSendButton = true;
            }
            catch (Exception ex)
            {
                Chats.Add(ex.ToString());
            }
        }

        private async void Send(object obj)
        {
            try
            {
                await connection.InvokeAsync("SendMEssage", UserName, Message);
            }
            catch (Exception ex)
            {
                Chats.Add(ex.ToString());                
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnProepertyChanged([CallerMemberName] string property = "")
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
