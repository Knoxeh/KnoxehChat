using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCPServerCSharp
{
    class TcpServ
    {
        private TcpListener _server;
        private int userCount;
        
        private Boolean _isRunning;
        private Boolean clientConnected;
        private List<String> userList = new List<String>();
        private List<String> ChatLog = new List<String>();

        public TcpServ(int port)
        {
            _server = new TcpListener(IPAddress.Any, port);
            _server.Start();

            _isRunning = true;

            LoopClients();


        }

        public void LoopClients()
        {
            while (_isRunning)
            {
                // wait for client connection
                TcpClient newClient = _server.AcceptTcpClient();
                userCount++;
                // client found.
                // create a thread to handle communication
                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(newClient);
            }
        }
        public void HandleMessages(object obj)
        {
            TcpClient client = (TcpClient)obj;

            NetworkStream sStream = client.GetStream();

            int ChatLogSize = ChatLog.Count;
            int userListSize = userList.Count;
            while (clientConnected)
            {
                Thread.Sleep(100);
                if (ChatLog.Count > ChatLogSize && clientConnected)
                {
                    byte[] msg1 = System.Text.Encoding.ASCII.GetBytes(ChatLog[ChatLog.Count - 1]);
                    sStream.Write(msg1, 0, msg1.Length);
                    Console.WriteLine("Sending Message: {0}",ChatLog[ChatLog.Count - 1]);
                    ChatLogSize = ChatLog.Count;
                }
                /*if(userList.Count > ChatLogSize && clientConnected)
                {
                    String userListStr = "";
                    for (int x = 0; x < userList.Count; x++)
                    {
                        userListStr += userList[x];
                        if (x != (userCount - 1))
                            userListStr += ";";
                    }
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes("SRVSTATUS:USERLIST:" + userListStr);
                    sStream.Write(msg, 0, msg.Length);
                    userListSize = userList.Count;
                }*/
            }

        }
        public void HandleClient(object obj)
        {
            // retrieve client from parameter passed to thread
            TcpClient client = (TcpClient)obj;
            string clientIPAddress = "" + IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
            Console.WriteLine("NEW CLIENT: {0}", clientIPAddress);

            // sets two streams
            //StreamWriter sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
           // StreamReader sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
            NetworkStream sStream = client.GetStream();

            // you could use the NetworkStream to read and write, 
            // but there is no forcing flush, even when requested
            clientConnected = true;
            Byte[] bytes = new Byte[1024];
            String data = null;
            int i;
            

            Thread t = new Thread(new ParameterizedThreadStart(HandleMessages));
            t.Start(client);
            while (clientConnected)
            {
                try
                { 

                    // Loop to receive all the data sent by the client. 
                    while ((i = sStream.Read(bytes, 0, bytes.Length)) != 0)
                    {

                        Thread.Sleep(100);
                        // Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);

                        // Process the data sent by the client.
                        //data = data.ToUpper();
                       
                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                        string[] command = data.Split(':');

                       
                        if(command[1] == "MSG")
                        {
                            if(data.Substring(0,1) != "/")
                                ChatLog.Add(data);
                        }
                        else if(command[0] == "STATUS")
                        {
                            Thread.Sleep(10);
                            if(command[1] == "ChangeName")
                            {

                                ChatLog.Add("SRVMSG:ChangeName:"+command[2]+":"+command[3]);
                                userList[userList.IndexOf(command[2])] = command[3];
                            }
                            else if(command[1] == "onDisconnect")
                            {
                                ChatLog.Add("SRVMSG:userDisconnect:"+command[2]);

                                int userIndex = userList.IndexOf(command[2]);
                                userList.RemoveAt(userIndex);
                                Console.WriteLine("Removing User: {0}",userIndex);
                                clientConnected = false;
                               // sStream.
                            }
                            else if(command[1] == "onConnect")      
                            {
                                userList.Add(command[2]);
                                ChatLog.Add("SRVMSG:userConnect:"+command[2]);

                                String userListStr = "";
                                for (int x = 0; x < userList.Count; x++)
                                {
                                    userListStr += userList[x];
                                    if (x != (userCount - 1))
                                        userListStr += ";";
                                }
                                msg = System.Text.Encoding.ASCII.GetBytes("SRVMSG:userList:" + userListStr);
                                sStream.Write(msg, 0, msg.Length);
                            }
                            

                        
                        }
                        else if(command[0] == "GETDATA")
                        {
                            if(command[1] == "userList")
                            {
                                String userListStr = "";
                                for (int x = 0; x < userList.Count; x++)
                                {
                                    userListStr += userList[x];
                                    if (x != (userCount - 1))
                                        userListStr += ";";
                                }
                                msg = System.Text.Encoding.ASCII.GetBytes("SRVMSG:userList:"+userListStr);
                                sStream.Write(msg, 0, msg.Length);
                                Console.WriteLine("Sent: {0}", userListStr);
                                
                            }
                        }
                        else if(data == "GETDATA:USERCOUNT")
                        {
                            msg = System.Text.Encoding.ASCII.GetBytes(""+userCount);
                            sStream.Write(msg, 0, msg.Length);
                            Console.WriteLine("Sent: {0}", userCount);
                        }
                        
                        // Send back a response.
                        //sStream.Write(msg, 0, msg.Length);
                        //Console.WriteLine("Sent: {0}", data);


                        
                    }
                }
                catch (IOException exception)
                {
                    userCount--;
                    Console.WriteLine("Client Forcefully Close Stream.");
                    clientConnected = false;
                }            
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            TcpServ ts = new TcpServ(13000);
        }
    }
}

