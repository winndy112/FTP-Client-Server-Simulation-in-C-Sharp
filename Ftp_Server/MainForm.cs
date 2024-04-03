﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using MongoDB.Driver;
using MongoDB.Bson;
namespace Ftp_Server
{
    public partial class MainForm : Form
    {
        private string host;
        private int port;
        private string password;
        private int control_port = 30;
        private TcpListener listener;
        private Thread listenThread;
        public MainForm()
        {
            InitializeComponent();
        }

        private void connection_Click(object sender, EventArgs e)
        {
            using (var connectionForm = new ConnectForm())
            {
                if (connectionForm.ShowDialog() == DialogResult.OK)
                {   
                    host = connectionForm.host;
                    port = connectionForm.port;
                    password = connectionForm.password;
                    StartListening();
                }
            }
        }
        private void CreateHeaders_viewLog()
        {
            // Clear existing columns
            ViewLog.Columns.Clear();

            // Add new columns
            ViewLog.Columns.Add("Date/Time");
            ViewLog.Columns.Add("Type");
            ViewLog.Columns.Add("Message");
            

            // Set column widths
            ViewLog.Columns[0].Width = 200;
            ViewLog.Columns[1].Width = 200;
            ViewLog.Columns[2].Width = 500;
            
            ViewLog.View = View.Details;
        }
        private void CreateHeaders_viewSession()
        {
            // Clear existing columns
            viewSession.Columns.Clear();
            // Add new columns
            viewSession.Columns.Add("Date/Time");
            viewSession.Columns.Add("SessionID");
            viewSession.Columns.Add("Host");
            viewSession.Columns.Add("User");
            // Set column widths
            viewSession.Columns[0].Width = 200;
            viewSession.Columns[1].Width = 200;
            viewSession.Columns[2].Width = 200;
            viewSession.Columns[2].Width = 200;

            viewSession.View = View.Details;
        }

        private void StartListening()
        {
            try
            {
                listener = new TcpListener(System.Net.IPAddress.Parse("0.0.0.0"), control_port);
                listener.Start();
                MessageBox.Show("FTP server started listening on port " + control_port);
                connection.Visible = false;
                label1.Text = "Connected to " + host;
                CreateHeaders_viewLog();
                CreateHeaders_viewSession();
                listenThread = new Thread(new ThreadStart(AcceptConnections));
                listenThread.Start();

                

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting FTP server: " + ex.Message);
            }
        }
        private void AcceptConnections()
        {
            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    // Start a new thread to handle the connection
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleConnection));
                    clientThread.Start(client);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error accepting connection: " + ex.Message);
                }
            }
        }
        public bool verifyUser(string username, string password)
        {
            var client = new MongoClient("mongodb+srv://22521168:TO82PIYRxNeYBd18@cluster0.x3ovogy.mongodb.net/?retryWrites=true&w=majority&appName=Cluster0");
            var database = client.GetDatabase("users");
            var collection = database.GetCollection<BsonDocument>("account");
            // Xây dựng truy vấn
            var filter = Builders<BsonDocument>.Filter.Eq("username", username);

            // Thực hiện truy vấn
            var result = collection.Find(filter).FirstOrDefault();
            if (result != null)
            {
                if (password == result["password"])
                {
                    Console.WriteLine("Valid password.");
                    return true;
                }
                else
                {
                    Console.WriteLine("Invalid password.");
                }

            }
            else
            {
                Console.WriteLine("User not found.");
            }
            return false;
        }

        private void handleUser(object obj, string cmd)
        {
            NetworkStream stream = (NetworkStream)obj;

            byte[] buffer = new byte[1024];

            // Get username after user cmd
            string username = cmd.Substring(5);
            Console.WriteLine("Username of client: " + username);

            // Send response for user cmd
            byte[] response = Encoding.ASCII.GetBytes("331 Please enter your password. Password: ");
            stream.Write(response, 0, response.Length);

            // Receive password from client
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            password = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Password of client: " + password);

            // Verify password
            bool ok = verifyUser(username, password);
            if (ok)
            {
                // Send response
                response = Encoding.ASCII.GetBytes("230 You are logged in.");
                stream.Write(response, 0, response.Length);
            }
            else
            {
                // Send response
                response = Encoding.ASCII.GetBytes("530 Login incorrect.");
                stream.Write(response, 0, response.Length);
            }

        }

        private void HandleConnection(object obj)
        {
            
            TcpClient client = (TcpClient)obj;
            try
            {
                string username_client, password_client; 
                // Lấy dữ liệu luồng mạng của client
                NetworkStream stream = client.GetStream();
                host = Dns.GetHostName();
                
                // Chuỗi chào mừng
                string welcomeMessage = "Connected to" + Dns.GetHostByName(host).AddressList[0].ToString()+  "\r\n";

                // Chuyển chuỗi chào mừng thành mảng byte
                byte[] welcomeBytes = Encoding.ASCII.GetBytes(welcomeMessage);

                // Gửi chuỗi chào mừng đến client
                stream.Write(welcomeBytes, 0, welcomeBytes.Length);

                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Xử lý dữ liệu đọc được từ client ở đây
                    
                    string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    if (receivedData.StartsWith("REGISTER"))
                    {
                        string[] _receivedData = receivedData.Split(' ');
                        username_client = _receivedData[1];
                        password_client = _receivedData[2];


                    }
                    else if (receivedData.StartsWith("USER"))
                    {
                        handleUser(stream, receivedData);
                    }
                    else if (receivedData.StartsWith("LIST"))
                    {

                    }
                    else if (receivedData.StartsWith("QUIT"))
                    {

                    }
                    else if (receivedData.StartsWith("GET"))
                    {

                    }
                    else if (receivedData.StartsWith("PUT"))
                    {

                    }
                    else if (receivedData.StartsWith("CD"))
                    {

                    }
                    else
                    {
                        MessageBox.Show("Command not found.");
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error handling connection: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
