using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.IO;

namespace ftp_server
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

   public class SocketListener
    {
        // The listener socket that accepts connections.
        Socket listener = null;

        // Max size of the buffer (divides the stream into "packets").
        const int MAX = 1024;
        
        // The buffer to hold messages that are to be sent.
        char[] charBuffer = new char[MAX];

        // Data buffer for incoming data.
        byte[] bytes = new Byte[MAX];

        // Max number of users the server socket can accept, and default port.
        int MAX_USERS = 10, 
            port = 6000;
        
        // The path the server is on.
        string serverPath = Directory.GetCurrentDirectory();
        
        public ManualResetEvent allDone = new ManualResetEvent(false);
        public ManualResetEvent receiveDone = new ManualResetEvent(false);
        Boolean acceptConnections = true;
        UserManager userManager = new UserManager();
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public SocketListener() { }

        /// <summary>
        /// Constructor allows custom port
        /// </summary>
        /// <param name="port">The port to run the server on.</param>
        public SocketListener(int port)
        {
            try
            {
                this.port = port;
            } 
            catch (Exception e)
            {
                Debug.WriteLine("Bad port: " + e.ToString());
                this.port = 6000;
            }
        }
        
        /// <summary>
        /// Sets the listener socket to begin listening for new connections 
        /// from clients. 
        /// </summary>
        public void StartListening()
        {
            // Setting up address structure 
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

            // Create a socket.
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(MAX_USERS);

                MessageBox.Show("Server has started running on port " + port.ToString());

                while (acceptConnections)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.                    
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    Debug.WriteLine("Waiting for new client connection to finish");

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();                    
                }

            }
            catch (AbandonedMutexException ame)
            {
                MessageBox.Show("SERVER ERROR! Abandoned mutex exception: \n\n" + ame.ToString());
                listener.Close();
                return;
            }
            catch (ObjectDisposedException od)
            {
                MessageBox.Show("SERVER ERROR! Abandoned object exception: \n\n" + od.ToString());
                listener.Close();
                return;
            }
            catch (Exception e)
            {
                MessageBox.Show("SERVER ERROR! Some error has occurred: \n\n" + e.ToString());
                listener.Close();
                return;
            }
        }
        
        /// <summary>
        /// Asynchronously accepts a connection from a client. 
        /// </summary>
        /// <param name="ar">The async result object</param>
        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            StateObject state = new StateObject();

            // Get the socket that handles the client request.
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                state.workSocket = listener.EndAccept(ar);

                //state.workSocket.ReceiveBufferSize = 1024 * 100;
                //state.workSocket.SendBufferSize = 1024 * 100;
                
                // Receives a message from the client
                if (!AuthenticateUser(state))
                {
                    return;
                }

                //ClientServiceloop
                ClientServiceLoop(state);
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine("Accept callback exception ode: " + ode.ToString());
            }
            catch (SocketException)
            {
                Debug.WriteLine("The client connection has been lost. Perhaps the client closed their connection?");
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine("The client attempted to do some illegal file access (Perhaps downloading a directory?)");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Accept callback exception e: " + e.ToString());
            }

            if (state.IsActive())
            {
                state.workSocket.Close();
            }

            return;
        }

        /// <summary>
        /// It authenticate user,if the user is in the list,
        /// it sends "in" message, 
        /// </summary>
        /// <param name="state"></param>
        public bool AuthenticateUser(StateObject state)
        {
            //bytes = byte buffer
            byte[] bytes = new byte[MAX];

            //Receive userID from Client;
            state.workSocket.Receive(bytes);

            //Convert and save received ID to local variable
            string assumedUserID = Encoding.UTF8.GetString(bytes);

            //Receive Password from Client
            state.workSocket.Receive(bytes);
            
            //Conver and save received Password to loacal variable
            string assumedPassword = Encoding.UTF8.GetString(bytes);
            
            try
            {
                if (UserManager.validateCredentials(assumedUserID, assumedPassword))
                {   //if user is authenticated, send user "in"
                    toCharBuffer("in");
                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(bytes);

                    //set currenPath(global) to user's dedicated folder
                    state.currentPath = Path.Combine(Directory.GetCurrentDirectory(), assumedUserID);

                    Debug.WriteLine("Current path for " + assumedUserID.Trim() + " is " + state.currentPath.Trim());

                    //After authentication, send Client server directory info
                    state.userID = assumedUserID;
                    sendServerDirectoryInfo(state);

                    return true;
                }
                else
                {
                    toCharBuffer("out");
                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(bytes);

                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not send verification message to user: " + e.ToString());

                return false;
            }
            
        }
        
        /// <summary>
        /// Handles flags sent by the client and performs the requested commands.
        /// </summary>
        /// <param name="state">The StateObject of the client to handle.</param>
        private void ClientServiceLoop(StateObject state)
        {
            Boolean continueServiceLoop = true;
            string flag;
            string[] supportedFlags = { "UPLOAD", "DOWNLOAD", "DELETE", "CHANGEDIR", "REFRESH", "NEWFOLDER", "END" };

            do
            {
                if (!state.IsActive())
                {
                    break;
                }
                
                // Ensures the client's socket is in blocking mode.
                state.workSocket.Blocking = true;

                do
                {
                    flag = null;

                    Debug.WriteLine("Waiting to receive command from client");
                    state.workSocket.Receive(state.buffer);

                    string dataWithSpaces = Encoding.UTF8.GetString(state.buffer);
                    flag = dataWithSpaces.Trim();

                    Debug.WriteLine("Flag: " + flag + "; Client connected: " + state.workSocket.Connected.ToString());
                    Debug.WriteLine("Flag is in array: " + Array.IndexOf(supportedFlags, flag));
                    
                } while (Array.IndexOf(supportedFlags, flag) < 0 && state.workSocket.Connected == true);
               
                switch (flag)
                {
                    case "UPLOAD":
                        continueServiceLoop = ReceiveUpload(state);
                        break;
                    case "DOWNLOAD":
                        continueServiceLoop = SendDownload(state);
                        break;
                    case "DELETE":
                        continueServiceLoop = DeleteFile(state);
                        break;
                    case "CHANGEDIR":
                        continueServiceLoop = ChangeDir(state);
                        break;
                    case "REFRESH":
                        continueServiceLoop = Refresh(state);
                        break;
                    case "NEWFOLDER":
                        continueServiceLoop = MakeNewFolder(state);
                        break;
                    case "END":
                        continueServiceLoop = false;
                        break;
                    default:
                        Debug.WriteLine("False command received");
                        continueServiceLoop = false;
                        break;
                }
            } while (continueServiceLoop == true && state.workSocket.Connected == true && acceptConnections);

            state.workSocket.Close();
        }

        /// <summary>
        /// Asynchronously reads from the client. 
        /// </summary>
        /// <param name="ar">The async object</param>
        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead = 0;

            // Read data from the client socket. 
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (SocketException se)
            {
                Debug.WriteLine("The remote host may have forcibly closed the connection: " + se.ToString());
                return;
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine("The object is being disposed here: " + ode.ToString());
                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("The handler object may have been disposed inappropriately: " + e.ToString());
                return;
            }

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    state.sb.Replace(state.sb.ToString(), state.sb.ToString().Substring(0, state.sb.Length - 5));
                    receiveDone.Set();
                    return;
                }
                else
                {
                    // Not all data received. Get more. (Recursively)
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
            }
        }

        /// <summary>
        /// Sends a file to the client that requests it. 
        /// </summary>
        /// <param name="state">The client StateObject that requests the download.</param>
        /// <returns>True if sending download was successful (sends true even if the client refused a download); false otherwise.</returns>
        private bool SendDownload(StateObject state)
        {
            try
            {
                //Receive fileName
                state.workSocket.Receive(state.buffer);
                string fileName = Encoding.UTF8.GetString(state.buffer).Trim();

                string filePath = Path.Combine(state.currentPath, fileName);
                byte[] fileData = File.ReadAllBytes(filePath);

                // Send the file size to the client
                Array.Clear(state.buffer, 0, state.buffer.Length);
                toCharBuffer(new FileInfo(filePath).Length.ToString());
                state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                state.workSocket.Send(state.buffer);

                // Receive acknowledgement
                Array.Clear(state.buffer, 0, state.buffer.Length);
                state.workSocket.Receive(state.buffer);
                string ack = Encoding.UTF8.GetString(state.buffer).Trim();

                if (ack.Equals("STOPSEND"))
                {
                    Debug.WriteLine("Error on client side; not sending file");
                    return true;
                }

                int offset = 0;
                int blockSize = 0;
                int fileSize = fileData.Length;
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
                        Array.Clear(state.buffer, 0, state.buffer.Length);
                        Buffer.BlockCopy(fileData, offset, state.buffer, 0, blockSize);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    state.workSocket.Send(state.buffer);

                    offset = offset + blockSize;
                    bytesLeft = bytesLeft - blockSize;
                }

                return true;
            }
            catch (Exception e) when
                (e is SocketException || e is ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// This method receive files that Client sends
        /// </summary>
        /// <param name="state"></param>
        private bool ReceiveUpload(StateObject state)
        {
            string tempData;

            try {
                //Receive fileName
                state.workSocket.Receive(state.buffer);
                tempData = Encoding.UTF8.GetString(state.buffer).Trim();
                string fileName = tempData;

                //Receive fileSize
                state.workSocket.Receive(state.buffer);
                tempData = Encoding.UTF8.GetString(state.buffer).Trim();
                int fileSize = int.Parse(tempData);

                //Open or Create the file on the user's directory
                string path = state.currentPath;
                path = Path.Combine(path, fileName);

                FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
                fs.Close();

                byte[] fileData = new byte[fileSize];

                int offset = 0;
                int blockSize = 0;
                int bytesLeft = fileSize;

                //Receive 1024B(MAX) chuks of data, and write it to the file
                for (int i = 0; i < (fileSize / MAX) + 1; i++)
                {
                    if (bytesLeft > MAX)
                    {
                        blockSize = MAX;
                    }
                    else
                    {
                        blockSize = bytesLeft;
                    }

                    state.workSocket.Receive(state.buffer);
                    Buffer.BlockCopy(state.buffer, 0, fileData, offset, blockSize);

                    offset = offset + state.buffer.Length;
                    bytesLeft = bytesLeft - blockSize;
                }

                File.WriteAllBytes(path, fileData);

                CleanSocketBuffer(state.workSocket);

                Array.Clear(state.buffer, 0, state.buffer.Length);
                toCharBuffer("SUCCESS");
                state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                state.workSocket.Send(state.buffer);

                return true;
            }
            catch (Exception e) when 
                (e is ObjectDisposedException || e is SocketException)
            {
                return false; 
            }
        }

        /// <summary>
        /// Sends the client's current server directory.
        /// </summary>
        /// <param name="state">The current client state object.</param>
        private bool Refresh(StateObject state)
        {
            return sendServerDirectoryInfo(state);
        }

        /// <summary>
        /// it creates and sends selected directory info to Client
        /// </summary>
        /// <param name="userID"></param>
        public bool sendServerDirectoryInfo(StateObject state)
        {
            string[] directories, files;

            try {
                // Get files and directories of server
                files = Directory.GetFiles(state.currentPath).Select(Path.GetFileName).ToArray();
                directories = Directory.GetDirectories(state.currentPath).Select(Path.GetFileName).ToArray();

                //Send the number of sub-directory in the current Directory
                Array.Clear(state.buffer, 0, state.buffer.Length);
                toCharBuffer(directories.GetLength(0).ToString());
                Debug.WriteLine("Number of directories: " + (new string(charBuffer).Trim()));
                state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                state.workSocket.Send(state.buffer);

                //Send names of directories in current directory
                for (int i = 0; i < directories.Length; i++)
                {
                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    toCharBuffer(directories[i]);
                    state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(state.buffer);
                }

                // Send directory modified times
                for (int i = 0; i < directories.Length; i++)
                {
                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    toCharBuffer(Directory.GetLastWriteTime(
                        Path.Combine(
                            state.currentPath, directories[i])).ToString());
                    state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(state.buffer);
                }

                //Send the number of files in the current directory
                Array.Clear(state.buffer, 0, state.buffer.Length);
                toCharBuffer(files.GetLength(0).ToString());
                Debug.WriteLine("Number of files: " + files.GetLength(0).ToString());
                state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                state.workSocket.Send(state.buffer);

                //Send the names of files in current directory
                for (int i = 0; i < files.Length; i++)
                {
                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    toCharBuffer(Path.GetFileName(files[i]));
                    state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(state.buffer);
                }

                // Get file size 
                for (int i = 0; i < files.Length; i++)
                {
                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    toCharBuffer(new FileInfo(state.currentPath + "\\" + files[i]).Length.ToString());
                    state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(state.buffer);
                }

                // Send file modified times
                for (int i = 0; i < files.Length; i++)
                {
                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    toCharBuffer(File.GetLastWriteTime(
                        Path.Combine(state.currentPath, files[i])).ToString());
                    state.buffer = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(state.buffer);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Changes the user's directory
        /// </summary>
        /// <param name="state">The client state object</param>
        private bool ChangeDir(StateObject state)
        {
            string directoryname;
            string tempCurrentPath = state.currentPath;

            try
            {
                // Get new directory
                state.workSocket.Receive(state.buffer);
                directoryname = Encoding.UTF8.GetString(state.buffer).Trim();

                // Check if the user is trying to use .. in their home dir
                state.currentPath = state.currentPath.Trim();
                
                if (state.currentPath.Equals( Path.Combine(serverPath,  state.userID.Trim())) && 
                    directoryname.Equals(".."))
                {
                    sendServerDirectoryInfo(state);
                    return true;
                }
                else if (directoryname.Equals(".."))
                {
                    state.currentPath = Directory.GetParent(state.currentPath).FullName;
                }
                else
                {
                    state.currentPath = Path.Combine(state.currentPath, directoryname);
                }

                Debug.WriteLine("New directory for client is " + state.currentPath);

                sendServerDirectoryInfo(state);

                return true;
            }
            catch (Exception e) when    
                (e is SocketException || e is ObjectDisposedException)
            {
                Debug.WriteLine("Could not change directory");
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                state.currentPath = tempCurrentPath;
                sendServerDirectoryInfo(state);
                return true;
            }
        }

        /// <summary>
        /// Deletes a file or directory in the client's directory. Directory
        /// deletion is recursive.
        /// </summary>
        /// <param name="state">The client state object</param>
        private bool DeleteFile(StateObject state)
        {
            string filename;

            try
            {
                // Get the file name
                state.workSocket.Receive(state.buffer);
                filename = Encoding.UTF8.GetString(state.buffer).Trim();

                // Get path of file
                string path = Path.Combine(state.currentPath, filename);

                // If the file is a directory
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }

                sendServerDirectoryInfo(state);

                return true;
            }
            catch (Exception)
            {
                Debug.WriteLine("Could not delete a file!");

                return true;
            }
        }

        /// <summary>
        /// Attempts to make a new folder on the server, then reports the status.
        /// </summary>
        /// <param name="state"></param>
        private bool MakeNewFolder(StateObject state)
        {
            string tempData;
            string newFolderName;
            string path = state.currentPath;

            //Receive name of the folder to be created
            state.workSocket.Receive(state.buffer);
            tempData = Encoding.UTF8.GetString(state.buffer);
            newFolderName = tempData.Trim();

            path = Path.Combine(path, newFolderName);

            try {
                if (Directory.Exists(path))
                {
                    //If the name was taken already.
                    toCharBuffer("EXIST");

                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(bytes);
                }
                else
                {
                    //If the name was not taken, create new folder
                    toCharBuffer("NOTEXIST");

                    bytes = Encoding.UTF8.GetBytes(charBuffer);
                    state.workSocket.Send(bytes);

                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception)
            {
                return false;
            }

            sendServerDirectoryInfo(state);

            return true;
        }

        /// <summary>
        /// Writes data into byte[] and sends it in a socket.
        /// </summary>
        /// <param name="handler">The socket to send data in.</param>
        /// <param name="data">The data to send.</param>
        private void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using UTF8 encoding.
            byte[] byteData = Encoding.UTF8.GetBytes(data); 

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }
        
        /// <summary>
        /// Completes the Send(Socket handler, String data) function asynchronously. 
        /// </summary>
        /// <param name="ar">The asyncResult object.</param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Debug.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
        
        /// <summary>
        /// convert string data into charBuffer
        /// </summary>
        /// <param name="stringData"></param>
        public void toCharBuffer(string stringData)
        {
            for (int i = 0; i < MAX; i++)
            {
                charBuffer[i] = ' ';
            }

            char[] stringArray = stringData.ToArray();
            stringArray.CopyTo(charBuffer, 0);
        }
        
        /// <summary>
        /// Stops the loop of the server from accepting connections,
        /// closes the accepting listener socket, and sets the thread
        /// to continue on past the "accept connection" loop.
        /// </summary>
        public void StopListening()
        {
            acceptConnections = false;
            listener.Close();
            allDone.Set();
        }

        /// <summary>
        /// Clears the data from the socket (sometimes junk data is in there).
        /// </summary>
        /// <param name="socket">The socket to clean.</param>
        public void CleanSocketBuffer(Socket socket)
        {
            byte[] trash = new byte[MAX];

            while (socket.Available > 0)
            {
                socket.Receive(trash);
                Array.Clear(trash, 0, trash.Length);
            }
        }

        /// <summary>
        /// The async object to carry data for each client on threads.
        /// Each client of the server receives their own StateObject object to keep track of their information.
        /// </summary>
        public class StateObject
        {
            // Client socket.
            public Socket workSocket = null;

            // The user's current path.
            public string currentPath;

            // Size of receive buffer.
            public const int BufferSize = 1024;

            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];

            // Received data string.
            public StringBuilder sb = new StringBuilder();

            //User Id
            public string userID;

            public StateObject() { }
            
            public bool IsActive()
            {
                try {
                    if (workSocket.Connected)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (NullReferenceException)
                {
                    return false;
                }
            }
        }
    }
}
