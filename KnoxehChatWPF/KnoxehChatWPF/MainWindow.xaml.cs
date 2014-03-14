using Awesomium.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace KnoxehChatWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        String serveIP = "murphy.im";
        SoundPlayer notificationSound = new SoundPlayer(@"./Mira.wav");
               
        private static NetworkStream stream;
        private static TcpClient client;
        private static Boolean serverConnected = false;
        private static String userName = "Guest";

        List<chatMessage> chatLog = new List<chatMessage>();
        public static int chatAmount = 0;
        Thread inputThread;

       public delegate void updateChatBoxDelegate(String textBoxString); // delegate type 
        public static updateChatBoxDelegate updateChatBox; // delegate object
        void updateChatBox1(string str) { createChatMessage(userName, str); }
        
        public MainWindow()
        {
            InitializeComponent();

  
            
        }
        private void sendClientStatus(String status)
        {

            Byte[] data = System.Text.Encoding.ASCII.GetBytes("STATUS:" + status);
            stream.Write(data, 0, data.Length);

        }
        private void sendChatMessage(String message)
        {
            if (serverConnected)
            {
                message = message.Replace(":", "&#58;");
                string msgSend = "MSG:" + userName + ":" + message + "";
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(msgSend.Length+":"+msgSend);
                stream.Write(data, 0, data.Length);
                Debug.WriteLine("SENDING MESSAGE: " + message);
            }
        }
        private String getServerInfo(String type)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes("GETDATA:" + type);
            stream.Write(data, 0, data.Length);

            Int32 bytes = stream.Read(data, 0, data.Length);
            String responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

            return responseData;
        }
        public void handleChatMessage(string[] msg)
        {

            this.Dispatcher.Invoke((Action)(() =>
            {
             
                if(msg[1] == "MSG")
                {
                    createChatMessage(msg[2], msg[3]);
                }
                else if(msg[0] == "SRVMSG")
                {
                    if (msg[1] == "userConnect")
                    {
                        createServerMessage(msg[2] + " has connected");
                        if(msg[2] != userName)
                            userList.Items.Add(msg[2]);
                    }
                    else if(msg[1] == "userDisconnect")
                    {
                        createServerMessage(msg[2] + " has disconnected");
                        userList.Items.Remove(msg[2]);
                       
                    }
                    else if(msg[1] == "ChangeName")
                    {
                        int userLocation = userList.Items.IndexOf(msg[2]);
                        createServerMessage(msg[2] + " has changed names to " + msg[3]);
                        userList.Items.RemoveAt(userLocation);
                        userList.Items.Insert(userLocation, msg[3]);
                    }
                    else if(msg[1] == "userList")
                    {
                        String[] userListSplit = msg[2].Split(';');
                        foreach (String users in userListSplit)
                            userList.Items.Add(users);

                    }
                    
                }
              
            }));
        }
        public void createServerMessage(string msg)
        {
            chatLog.Add(new chatMessage("server","SERVER",msg));
            string json = JsonConvert.SerializeObject(chatLog, Formatting.None);

            System.IO.StreamWriter file = new System.IO.StreamWriter("chatLog.json");
            file.Write(json);
            file.Close();

            txtChat.AppendText("SERVER: " + msg + Environment.NewLine);
        }
        public void createChatMessage(string author, string message)
        {
            chatLog.Add(new chatMessage("user", author, message));

            string json = JsonConvert.SerializeObject(chatLog, Formatting.None);

            if(chkSound.IsChecked == true)
                  notificationSound.Play();

            System.IO.StreamWriter file = new System.IO.StreamWriter("chatLog.json");
            file.Write(json);
            file.Close();

            txtChat.AppendText(author + ": " + message + Environment.NewLine);

        }
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            String messageInput = txtMessage.Text;

            //Handle slash commands
            if (messageInput.Contains("/name"))
            {
                String newName = messageInput.Substring(6);
                sendClientStatus("ChangeName:" + userName + ":" + newName);
                userName = newName;
                txtUserName.Text = userName;
            }

            else//If there arent any slash commands
            {
                sendChatMessage(messageInput );
            }
            txtMessage.Text = "";
           
        }
        public void HandleUserList()
        {
            while(serverConnected)
            {
                Thread.Sleep(1000);
                


            }
            
        }
        public void HandleMessages()
        {
            Byte[] data = new Byte[1024];
            String responseData = String.Empty;
            Thread.Sleep(10);

            while (serverConnected)
            {
                Thread.Sleep(100);
               
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                

                string[] message = responseData.Split(':');
                handleChatMessage(message);

            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {

        }
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            btnConnect.IsEnabled = false;
            try
            {
                serverConnected = !serverConnected;
                if (serverConnected)
                {
                    Int32 port = 13000;
                    client = new TcpClient(serveIP, port);
                    stream = client.GetStream();
                    btnConnect.Content = "Disconnect from Server";
                    userName = txtUserName.Text;
                    sendClientStatus("onConnect:" + userName);

                    // client started
                    // create a thread to handle communication
                    inputThread = new Thread(new ThreadStart(HandleMessages));
                    inputThread.Start();


                    txtUserName.IsEnabled = false;

                    webChat.Source = new Uri("file:///./chatHandle.html",UriKind.Absolute);

                    Thread.Sleep(500);
                    btnConnect.IsEnabled = true;

                }
                else
                {
                    userList.Items.Clear();
                    txtUserName.IsEnabled = true;
                    btnConnect.Content = "Connect To Server";
                    sendClientStatus("onDisconnect:" + userName);
                    inputThread.Abort();
                    stream.Flush();
                    stream.Close();
                    client.Close();

                    Thread.Sleep(500);
                    btnConnect.IsEnabled = true;
                    //   timer1.Stop();
                }
            }
            catch(Exception ex)
            {
                if (ex.GetType().ToString() == "System.Net.Sockets.SocketException")
                {
                    MessageBox.Show(serveIP + " is not responding, please try connecting again later!","Connection Error");
                }

                
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(serverConnected)
            {

                btnConnect_Click(sender,null);
            }
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btnSend_Click(sender, e);
            }
            
        }

        private void webChat_LoadingFrame(object sender, Awesomium.Core.LoadingFrameEventArgs e)
        {

           
        }
        private void webChat_ShowJavascriptDialog(object sender, Awesomium.Core.JavascriptDialogEventArgs e)
        {
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void webChat_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void webChat_InitializeView(object sender, WebViewEventArgs e)
        {
            // Create a WebSession.
            WebSession session = WebCore.CreateWebSession(new WebPreferences()
            {
                SmoothScrolling = true,
                UniversalAccessFromFileURL = true
            });
            

            // Assign it to the control. This should only occur here, before the
            // the underlying view of the control is created.
            webChat.WebSession = session;
        }
    }
    class chatMessage
    {
        public string timestamp;
        public int id;
        public String messageType;
        public String author;
        public String message;
        public String messageHTML;
        public chatMessage( String type, String athr, String msg)
        {
            timestamp = DateTime.Now.ToString("MMM ddd d HH:mm yyyy");
            messageType = type;
            author = athr;
            message = msg;
            MainWindow.chatAmount++;
            id = MainWindow.chatAmount;
            switch(messageType)
            {
                case "server": messageHTML = "<div class='row'><div class='col-md-12'><div class='alert alert-success'><b>SERVER:</b> " + message + "</div></div></div>"; break;
                case "user": messageHTML = "<div class=\"row\"><div class=\"col-md-1\"><strong>" + author + "</strong></div><div class=\"col-md-11 well well-sm\">" + message + "</div></div>"; break;
            }
            
        }
    }
}
