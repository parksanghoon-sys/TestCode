using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wpf비동기연속스트림
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ChatStream _chatStream = null;
        ObservableCollection<string> ChatList;
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;
            ChatList = new ObservableCollection<string>();
            this.Loaded += MainWindow_Loaded;
            lsvData.ItemsSource = ChatList;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _chatStream = new ChatStream();
            await foreach(var chat in _chatStream.GetChatAsync())
            {
                ChatList.Add(chat);
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            // ChatStream의 채팅 데이터 큐에 메세지 보관
            _chatStream.Send(this.txtChat.Text);
            this.txtChat.Text = String.Empty;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            // ChatStream의 채팅 데이터 큐에 메세지 보관
            _chatStream.Stop();
        }
    }
}