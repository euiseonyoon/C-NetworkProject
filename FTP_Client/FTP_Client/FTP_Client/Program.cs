using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FTP_Client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class SynchronousClient
    {
        public const int MAX = 1024;
        private char[] charBuffer = new char[MAX];
        public Socket client;
        public bool isConnected = false;
        public ManualResetEvent threadDone = new ManualResetEvent(false);
        private Form1 ClientForm;

        //Names of subDirectory from server
        public string[] serverDirectoryNames;
        public string[] serverDirectoryModifiedTimes;
        //Names of Files from server
        public string[] serverFileNames;
        public string[] serverFileSizes;
        public string[] serverFileModifiedTimes;
        
        // The port number for the remote device.
        private int port;
        private string serverIP, userID, password;
        // Data buffer for incoming data.
        private byte[] bytes = new byte[MAX];

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="serverIP">The IP to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="userID">The user ID to use when connecting to the server.</param>
        /// <param name="password">The password to use when connecting to the server.</param>
        public SynchronousClient(string serverIP, int port, string userID, string password, Form1 obj)
        {
            this.serverIP = serverIP;
            this.port = port;
            this.userID = userID;
            this.password = password;
            this.ClientForm = obj;
        }

        /// <summary>
        /// Starts the client and initiates the connection.
        /// </summary>
        /// <returns>An exception if something happened.</returns>
        public Exception StartClient()
        {
            // Connect to a remote device.
            try
            {
                IPAddress ipAddress = IPAddress.Parse(serverIP);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP  socket.
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    client.Connect(remoteEP);

                    if (client.Connected)
                    {
                        isConnected = true;
                    }

                    return null;
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    return ane;
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                    return se;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    return e;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not establish client or IP endpoint: " + e.ToString());
                return e;
            }
        }

        /// <summary>
        /// Converts a string to the charBuffer so that it can be sent as bytedata on the socket.
        /// </summary>
        /// <param name="stringData">The string to convert to a char buffer.</param>
        public void toCharBuffer(string stringData)
        {
            for (int i = 0; i < MAX; i++)
            {
                charBuffer[i] = ' ';
            }

            Debug.WriteLine("String data in toCharBuffer is " + stringData);

            try
            {
                char[] stringArray = stringData.ToArray();
                stringArray.CopyTo(charBuffer, 0);
            }
            catch (ArgumentNullException)
            {
                Debug.WriteLine("ToCharBuffer: Cannot parse null");
            }
        }

        /// <summary>
        /// Send userName info to server
        /// </summary>
        /// <param name="userID">The userID of the user</param>
        public void sendUserName(string userID)
        {
            if (client != null)
            {
                toCharBuffer(userID);
                bytes = Encoding.UTF8.GetBytes(charBuffer);
                client.Send(bytes);
            }
            else
            {
                Debug.WriteLine("Client socket is null on sendUserName()");
            }
        }

        /// <summary>
        /// Sends the user's password info to server
        /// </summary>
        /// <param name="encryptedPassword"></param>
        public void sendPassword(string password)
        {
            if (client != null)
            {
                toCharBuffer(password);
                bytes = Encoding.UTF8.GetBytes(charBuffer);
                client.Send(bytes);
            }
            else
            {
                MessageBox.Show("Client Socket is null on sendPassword function");
            }
        }

        /// <summary>
        /// Sends a "REFRESH" flag to the server, in which the server responds with the user's directory data.
        /// </summary>
        public void RequestServerDirectory()
        {
            CleanSocketBuffer(client);

            toCharBuffer("REFRESH");
            bytes = Encoding.UTF8.GetBytes(charBuffer);
            client.Send(bytes);
        }

        /// <summary>
        /// Receives current directory from server.
        /// </summary>
        public void receiveServerDirectoryInfo()
        {
            string tempData;
            
            try
            {
                //Receive number of sub-directories from Server
                Array.Clear(bytes, 0, bytes.Length);
                client.Receive(bytes);
                tempData = Encoding.UTF8.GetString(bytes).Trim();
                Debug.WriteLine("Number of directories on server: " + tempData);
                int numberOfDir = int.Parse(tempData);

                //Receive names of sub-directories
                serverDirectoryNames = new string[numberOfDir];
                for (int i = 0; i < numberOfDir; i++)
                {
                    Array.Clear(bytes, 0, bytes.Length);
                    Debug.WriteLine("Getting directory names");
                    client.Receive(bytes);
                    tempData = Encoding.UTF8.GetString(bytes);
                    serverDirectoryNames[i] = tempData.Trim();
                }

                // Receive modified times of directories
                serverDirectoryModifiedTimes = new string[numberOfDir];
                for (int i = 0; i < numberOfDir; i++)
                {
                    Array.Clear(bytes, 0, bytes.Length);
                    Debug.WriteLine("Getting modified time for dir");
                    client.Receive(bytes);
                    tempData = Encoding.UTF8.GetString(bytes);
                    serverDirectoryModifiedTimes[i] = tempData.Trim();
                }

                //Receive number of files from server
                Array.Clear(bytes, 0, bytes.Length);
                client.Receive(bytes);
                tempData = Encoding.UTF8.GetString(bytes);
                Debug.WriteLine("Number of files on server: " + tempData.Trim());
                int numberOfFile = int.Parse(tempData.Trim());

                //Receive names of files from server
                serverFileNames = new string[numberOfFile];
                for (int i = 0; i < numberOfFile; i++)
                {
                    Array.Clear(bytes, 0, bytes.Length);
                    client.Receive(bytes);
                    tempData = Encoding.UTF8.GetString(bytes);
                    serverFileNames[i] = tempData.Trim();
                }

                // Get file sizes
                serverFileSizes = new string[numberOfFile];
                for (int i = 0; i < numberOfFile; i++)
                {
                    Array.Clear(bytes, 0, bytes.Length);
                    client.Receive(bytes);
                    tempData = Encoding.UTF8.GetString(bytes);
                    serverFileSizes[i] = tempData.Trim();
                }

                // Get file modified times
                serverFileModifiedTimes = new string[numberOfFile];
                for (int i = 0; i < numberOfFile; i++)
                {
                    Array.Clear(bytes, 0, bytes.Length);
                    client.Receive(bytes);
                    tempData = Encoding.UTF8.GetString(bytes);
                    serverFileModifiedTimes[i] = tempData.Trim();
                }
            }
            catch (Exception)
            {
                //client.Close();
            }

        }

        /// <summary>
        /// Receives authentication response from the server. 
        /// </summary>
        /// <returns>True if authentication was a success; false otherwise.</returns>
        public bool receiveAuthentication()
        {
            bool isLoggedIn = false;

            if (client != null)
            {
                Array.Clear(bytes, 0, bytes.Length);

                if (client.Receive(bytes) > 0)
                {
                    string result = Encoding.UTF8.GetString(bytes);

                    if (result.Contains("in"))
                    {
                        isLoggedIn = true;
                    } 
                    else
                    {
                        isLoggedIn = false;
                    }
                }
            }

            return isLoggedIn;
        }

        /// <summary>
        /// Requests the server to change the client's current directory.
        /// </summary>
        /// <param name="directory">The directory to change the client to. Not full path; only directory name.</param>
        public void ChangeDir(string directory)
        {
            try
            {
                toCharBuffer("CHANGEDIR");
                bytes = Encoding.UTF8.GetBytes(CharBuffer);
                client.Send(bytes);

                Debug.WriteLine("Changing directory to " + directory);

                toCharBuffer(directory);
                bytes = Encoding.UTF8.GetBytes(CharBuffer);
                client.Send(bytes);

                receiveServerDirectoryInfo();
            }
            catch (Exception)
            {
                MessageBox.Show("Error: Could not change directory!");
            }
        }

        /// <summary>
        /// Requests the server to delete a file from the useräs directory
        /// </summary>
        /// <param name="filename">The name of the file to request the server to delete.</param>
        public void DeleteProcess(string filename)
        {
            try
            {
                toCharBuffer("DELETE");
                bytes = Encoding.UTF8.GetBytes(CharBuffer);
                client.Send(bytes);

                toCharBuffer(filename);
                bytes = Encoding.UTF8.GetBytes(CharBuffer);
                client.Send(bytes);
            }
            catch (Exception)
            {
                MessageBox.Show("Error: The server may have disconnected.");
            }
        }
        
        /// <summary>
        /// Uploads file(s) to the server.
        /// </summary>
        /// <param name="fileForTransfer"></param>
        /// <param name="currentPath"></param>
        public void SendUpload(string[,] fileForTransfer, string currentPath)
        {
            int numberOfFiles = fileForTransfer.GetLength(0);
            string fileName;

            try
            {
                Progress progressWindow = new Progress(numberOfFiles);
                progressWindow.Show();

                for (int i = 0; i < numberOfFiles; i++)
                {
                    byte[] fileData;

                    try
                    {
                        //Get the file Open
                        string filePath = Path.Combine(currentPath, fileForTransfer[i, 0]);

                        fileData = File.ReadAllBytes(filePath);
                    }
                    catch (IOException)
                    {
                        Debug.WriteLine("Couldn't upload " + fileForTransfer[i, 0]);
                        continue;
                    }

                    CleanSocketBuffer(client);

                    //Send file transmittion("UPLOAD") to Server
                    Array.Clear(bytes, 0, bytes.Length);
                    toCharBuffer("UPLOAD");
                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    client.Send(bytes);

                    //Send fileName to server
                    Array.Clear(bytes, 0, bytes.Length);
                    fileName = fileForTransfer[i, 0];
                    toCharBuffer(fileName);
                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    client.Send(bytes);

                    //Send fileSize to Server
                    Array.Clear(bytes, 0, bytes.Length);
                    toCharBuffer(fileForTransfer[i, 1]);
                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    client.Send(bytes);
                    
                    int offset = 0;
                    int blockSize = 0;
                    int fileSize = int.Parse(fileForTransfer[i, 1]);
                    int bytesLeft = fileSize;
                    
                    //Send the file by 1024B(MAX) chunks
                    while (bytesLeft > 0)
                    {
                        if (bytesLeft >= MAX)
                        {
                            blockSize = MAX;
                        }
                        else
                        {
                            blockSize = bytesLeft;
                        }

                        try
                        {
                            Buffer.BlockCopy(fileData, offset, bytes, 0, blockSize);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        
                        while (!client.Poll(1000, SelectMode.SelectWrite))
                            Debug.WriteLine("Cannot write to socket yet");
                        
                        client.Send(bytes);

                        offset = offset + blockSize;
                        bytesLeft = bytesLeft - blockSize;
                    }
                    
                    Array.Clear(bytes, 0, bytes.Length);
                    client.Receive(bytes);
                    String message = Encoding.UTF8.GetString(bytes).Trim();

                    progressWindow.IncrementProgress();

                    if (!message.Equals("SUCCESS"))
                    {
                        return;
                    }
                }

                progressWindow.Dispose();
            }
            catch (Exception)
            {
                Debug.WriteLine("SOME ERROR HAPPENED WHEN UPLOADING");
            }
        }

        public void ReceiveDownload(string[,] fileForTransfer, string currentPath)
        {
            int numberOfFiles = fileForTransfer.GetLength(0);
            string fileName;
            
            try
            {
                Progress progressWindow = new Progress(numberOfFiles);
                progressWindow.Show();

                for (int i = 0; i < numberOfFiles; i++)
                {
                    string path;

                    try
                    {
                        fileName = fileForTransfer[i, 0];
                        path = Path.Combine(currentPath, fileName);

                        FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
                        fs.Close();
                    }
                    catch (IOException)
                    {
                        continue;
                    }

                    CleanSocketBuffer(client);

                    //Send file transmittion("DOWNLOAD") to Server
                    Array.Clear(bytes, 0, bytes.Length);
                    toCharBuffer("DOWNLOAD");
                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    client.Send(bytes);

                    //Send fileName to server
                    Array.Clear(bytes, 0, bytes.Length);
                    toCharBuffer(fileName);
                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    client.Send(bytes);

                    int fileSize = 0;

                    try
                    {
                        //Receive file size from server
                        Array.Clear(bytes, 0, bytes.Length);
                        client.Receive(bytes);
                        String test = Encoding.UTF8.GetString(bytes).Trim();
                        fileSize = int.Parse(test);

                        // Acknowledge
                        Array.Clear(bytes, 0, bytes.Length);
                        toCharBuffer("CONTINUEESEND");
                        bytes = Encoding.UTF8.GetBytes(charBuffer);
                        client.Send(bytes);
                    }
                    catch (FormatException)
                    {
                        // Acknowledge
                        Array.Clear(bytes, 0, bytes.Length);
                        toCharBuffer("STOPSEND");
                        bytes = Encoding.UTF8.GetBytes(charBuffer);
                        client.Send(bytes);

                        File.Delete(path);

                        continue;
                    }
                    
                    //Receive file
                    byte[] fileData = new byte[fileSize];

                    int offset = 0;
                    int blockSize = 0;
                    int bytesLeft = fileSize;

                    //Receive 1024B(MAX) chuks of data, and write it to the file
                    for (int j = 0; j < (fileSize / MAX) + 1; j++)
                    {
                        if (bytesLeft > MAX)
                        {
                            blockSize = MAX;
                        }
                        else
                        {
                            blockSize = bytesLeft;
                        }

                        client.Receive(bytes);
                        Buffer.BlockCopy(bytes, 0, fileData, offset, blockSize);

                        offset = offset + bytes.Length;
                        bytesLeft = bytesLeft - blockSize;
                    }

                    progressWindow.IncrementProgress();

                    File.WriteAllBytes(path, fileData);
                }

                progressWindow.Dispose();
            }
            catch (Exception e) when 
                (e is SocketException || e is ObjectDisposedException)
            {
                if (client.Connected)
                {
                    client.Close();
                }
            }
        }

        public bool makeNewFolderOnServer(string newFolderName)
        {
            bool error = true;
            string tempData;

            //Send NEWFOLDER flag to server
            toCharBuffer("NEWFOLDER");
            bytes = Encoding.UTF8.GetBytes(charBuffer);
            client.Send(bytes);

            //Send the name of the folder to be made
            toCharBuffer(newFolderName);
            bytes = Encoding.UTF8.GetBytes(charBuffer);
            client.Send(bytes);
            
            //Receive if the folder exists or not on server
            bool isOK = false;
            do
            {
                client.Receive(bytes);
                tempData = Encoding.UTF8.GetString(bytes);

                if (tempData.Trim() == "NOTEXIST" || tempData.Trim() == "EXIST")
                {
                    isOK = true;
                }
            } while (!isOK);
            
            if (tempData.Contains("NOTEXIST"))
            {
                error = false;
            }
            else if (tempData.Contains("EXIST"))
            {
                error = true;
            }

            return error;
        }

        public void CleanSocketBuffer(Socket socket)
        {
            byte[] trash = new byte[MAX];

            while (socket.Available > 0)
            {
                socket.Receive(trash);
                Array.Clear(trash, 0, trash.Length);
            }
        }

        public char[] CharBuffer
        {
            get { return charBuffer; }
            set { charBuffer = value; }
        }

        public byte[] Bytes
        {
            get
            {
                return bytes;
            }

            set
            {
                bytes = value;
            }
        }

        public int Max
        {
            get { return MAX; }
        }
    }
}
