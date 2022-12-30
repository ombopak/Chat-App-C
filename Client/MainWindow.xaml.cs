using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
        public ObservableCollection<string> Userlist { get; set; }
        public ObservableCollection<string> MessageList { get; set; }
        public ConcurrentDictionary<string, List<string>> chatHistories { get; set; }





        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            MessageList = new ObservableCollection<string>();
            Userlist = new ObservableCollection<string>();
            chatHistories = new ConcurrentDictionary<string, List<string>>();


            btnLogin.Content = "LOGIN";


        }



        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Connect to the server
                client = new TcpClient();
                await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 7891);

                string username = UsernameTextBox.Text;

                stream = client.GetStream();

                //TODO:GET PORT FOR IDENTIFIER UNIQUE
                byte[] dataToServer = Encoding.UTF8.GetBytes("USERNAME:" + username);
                await stream.WriteAsync(dataToServer, 0, dataToServer.Length);

                // Start a new thread to continuously read data from the server
                Thread thread = new Thread(new ThreadStart(ReceiveDataFromServer));
                thread.Start();
            }
            catch
            {
                MessageBox.Show("Could not connect to server.");
            }


        }

        private async void ReceiveDataFromServer()
        {
            // Continuously read data from the server
            byte[] dataFromServer = new byte[1024];
            while (true)
            {
                int dataRead = await stream.ReadAsync(dataFromServer, 0, dataFromServer.Length);
                string message = Encoding.UTF8.GetString(dataFromServer, 0, dataRead);
                string[] messagePart = message.Split(':');

                if (messagePart[0] == "USERNAME")
                {

                    string[] newUserList = messagePart[1].Split(',');
                    App.Current.Dispatcher.Invoke(() => Userlist.Clear());

                    string username;

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        username = UsernameTextBox.Text;
                        foreach (string user in newUserList)
                        {
                            if (user != username)
                            {
                                App.Current.Dispatcher.Invoke(() =>
                       Userlist.Add(user));
                            }

                        }
                    }
                   );

                }
                else if (messagePart[0] == "DISCONNECT")
                {
                    string[] newUserList = messagePart[1].Split(',');
                    App.Current.Dispatcher.Invoke(() => Userlist.Clear());
                    //Send updated user list after delete

                    string username;

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        username = UsernameTextBox.Text;
                        foreach (string user in newUserList)
                        {
                            if (user != username)
                            {
                                App.Current.Dispatcher.Invoke(() =>
                       Userlist.Add(user));
                            }

                        }
                    }
                  );
                }
                else if (messagePart[0] == "MESSAGE")
                {                   
                    //sender message username
                    string key = messagePart[1];
                    //receiver message username
                    string values = messagePart[2];


                    // Add the client to the list
                    App.Current.Dispatcher.Invoke(() =>
                    {
                            if (chatHistories.ContainsKey(key))
                            {
                                chatHistories[key].Add(values);
                            }
                            else
                            {
                                chatHistories.TryAdd(key, new List<string> { values });
                            }
                        
                    });

                    App.Current.Dispatcher.Invoke( () => MessageList.Clear());


                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (UserListBox.SelectedItem != null)
                        {
                            string recipientM = (string)UserListBox.SelectedItem;
                            string senderM = UsernameTextBox.Text;
                            string key;

                            if (string.Compare(senderM, recipientM) < 0)
                            {
                                key = senderM + "_" + recipientM;
                            }
                            else
                            {
                                key = recipientM + "_" + senderM;
                            }

                            List<string> msgs;
                            if (chatHistories.TryGetValue(key, out msgs))
                            {

                                foreach (var item in msgs)
                                {
                                   
                                        MessageList.Add(item);
                                    

                                }
                            }

                        }

                    });


                }

            }
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            await App.Current.Dispatcher.Invoke(async () =>
            {
                if (UserListBox.SelectedItem != null)
                {

                    string recipientM = (string)UserListBox.SelectedItem;
                    string senderM = UsernameTextBox.Text;
                    string key;

                    Label2.Content = recipientM;

                    if (string.Compare(senderM, recipientM) < 0)
                    {
                        key = senderM + "_" + recipientM;
                    }
                    else
                    {
                        key = recipientM + "_" + senderM;
                    }
                    
                    string message = senderM + " | " + MessageTextBox.Text;


                    byte[] dataToServer = Encoding.UTF8.GetBytes("MESSAGE:" + key + ":" + message);


                    await stream.WriteAsync(dataToServer, 0, dataToServer.Length);

                    MessageTextBox.Text = "";

                }
                else { MessageBox.Show("Pick user from user list"); }

            });




        }

        private async void Window_Closed(object sender, EventArgs e)
        {
            byte[] dataToServer = Encoding.UTF8.GetBytes("DISCONNECT:" + UsernameTextBox.Text);
            await stream.WriteAsync(dataToServer, 0, dataToServer.Length);
        }

     
        private async void UserListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await App.Current.Dispatcher.Invoke(async () => MessageList.Clear());

            await App.Current.Dispatcher.Invoke(async () =>
            {
                if (UserListBox.SelectedItem != null)
            {
                string recipientM = UserListBox.SelectedItem.ToString();
                string senderM = UsernameTextBox.Text;
                string key;

                Label2.Content = recipientM;

                if (string.Compare(senderM, recipientM) < 0)
                {
                    key = senderM + "_" + recipientM;
                }
                else
                {
                    key = recipientM + "_" + senderM;
                }

                List<string> msgs;
                if (chatHistories.TryGetValue(key, out msgs))
                {

                    foreach (var item in msgs)
                    {
                        
                            MessageList.Add(item);
                        

                    }
                }

            }
            });
        }
    }
}
