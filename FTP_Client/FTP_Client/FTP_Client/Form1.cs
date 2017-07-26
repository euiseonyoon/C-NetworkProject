using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Threading;

namespace FTP_Client
{
    public partial class Form1 : Form
    {
        // Path to begin on
        string currentPath = System.IO.Directory.GetCurrentDirectory();
        string[,] fileForTransfer;
        string[] serverFileList, serverDirectoryList;
        int fileTransmitNum;
        bool connected;
        bool authenticated;
        SynchronousClient syncClient = null;

        string serverIpAddress, userName, password;
        int port;

        bool dragFromServer = false;
        bool dragFromClient = false;
        bool currentlyTransferring = false;

        public Form1()
        {
            InitializeComponent();

            this.listView1.AllowDrop =  true;
            this.listView1.DragEnter += new DragEventHandler(listView1_DragEnter);
            this.listView1.ItemDrag += new ItemDragEventHandler(listView1_ItemDrag);

            this.listView2.AllowDrop = true;
            this.listView2.DragEnter += new DragEventHandler(listView2_DragEnter);
            

            PopulateLocalListView(currentPath); // Initialise local listview1
            AddToClientConsole("Use the \"Connection\" tool strip menu item to begin a new connection.");
        }

        /// <summary>
        /// Executes when an item of listView1 (or the client) is clicked. (Change directory)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (!currentlyTransferring)
            {
                try
                {
                    string fileString = currentPath + "\\" + listView1.SelectedItems[0].Text;
                    FileAttributes attr = File.GetAttributes(fileString);

                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        // If it is the .. directory, try to get the parent directory
                        if (String.Compare(listView1.SelectedItems[0].Text, "..") == 0)
                        {
                            try
                            {
                                currentPath = Directory.GetParent(currentPath).FullName;
                            }
                            catch (Exception e1)
                            {
                                Debug.WriteLine("Error getting parent directory: " + e1.ToString());
                            }
                        }
                        else // It was not the .. directory, so use the new path string as the current path
                        {
                            currentPath = fileString;
                        }

                        PopulateLocalListView(currentPath); // Update local listview
                    }
                }
                catch (Exception ex) when (ex is FileNotFoundException || ex is ArgumentException)
                {
                    Debug.WriteLine("Client: File not found: " + ex.ToString());
                    return;
                }
            }
        }

        /// <summary>
        /// Executes when an item of listView2 (or the server) is clicked. (Change directory)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView2_ItemActivate(object sender, EventArgs e)
        {
            if (!currentlyTransferring && listView2.SelectedItems[0].ImageKey == "folder")
            {
                string directory = listView2.SelectedItems[0].Text;

                syncClient.ChangeDir(directory);

                PopulateServerListView();
            }
        }
        
        /// <summary>
        /// Gathers the selected items into an array to prepare for upload transfer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if ( listView1.SelectedItems[0].Text == ".." )
            {
                AddToClientConsole("Cannot upload .. directory!");
                return;
            }

            if (connected && authenticated)
            {
                // Get how many files are selected for transmittion 
                fileTransmitNum = listView1.SelectedItems.Count;

                // Decrement size of fileForTransfer if there are directories in the selected items.
                foreach (ListViewItem selectedItem in listView2.SelectedItems)
                {
                    if (selectedItem.ImageKey.Equals("folder"))
                    {
                        fileTransmitNum--;
                    }
                }
                
                fileForTransfer = new string[fileTransmitNum, 2];

                for (int i = 0, j = 0; i < listView1.SelectedItems.Count; i++)
                {
                    try
                    {
                        // If the selected item is a directory, skip adding it to the fileForTransfer
                        if (listView1.SelectedItems[i].ImageKey.Equals("folder"))
                        {
                            j++;
                            continue;
                        }
                        
                        fileForTransfer[i - j, 0] = listView1.SelectedItems[i].Text;
                        fileForTransfer[i - j, 1] = (new FileInfo(currentPath + "\\" + listView1.SelectedItems[i].Text)).Length.ToString();
                        string temp = listView1.SelectedItems[i].Text;
                        string path = Path.Combine(currentPath, temp);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                dragFromClient = true;

                //Start dragging to destination
                listView1.DoDragDrop(listView1.SelectedItems, DragDropEffects.None);
                //listView1.DoDragDrop(listView1.SelectedItems, DragDropEffects.Copy);
            }
        }

        /// <summary>
        /// Gathers the selected items into an array to prepare for download transfer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void listView2_ItemDrag(object sender, ItemDragEventArgs e)
        { // DOWNLOADING FROM SERVER

            if (listView2.SelectedItems[0].Text == "..")
            {
                AddToClientConsole("Cannot download .. directory!");
                return;
            }

            if (connected && authenticated)
            { 
                //Get how many files are selected for transmittion 
                fileTransmitNum = listView2.SelectedItems.Count;

                // Decrement size of fileForTransfer if there are directories in the selected items.
                foreach (ListViewItem selectedItem in listView2.SelectedItems)
                {
                    if (selectedItem.ImageKey.Equals("folder"))
                    {
                        fileTransmitNum--;
                    }
                }
                
                fileForTransfer = new string[fileTransmitNum, 2];

                // i = Current selected item
                // j = Number of directories 
                // i - j = Position to place 
                for (int i = 0, j = 0; i < listView2.SelectedItems.Count; i++)
                {
                    try
                    {
                        // If the selected item is a directory, skip adding it to the fileForTransfer
                        if (listView2.SelectedItems[i].ImageKey.Equals("folder"))
                        {
                            j++;
                            continue;
                        }
                        
                        fileForTransfer[i - j, 0] = listView2.SelectedItems[i].Text;
                    }
                    catch (Exception)
                    {

                    }
                }

                dragFromServer = true;

                //Start dragging to destination
                listView2.DoDragDrop(listView2.SelectedItems, DragDropEffects.None);
            }
        }

        /// <summary>
        /// Begins the download transfer using the selected items gathered by listview2_itemdrag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if(connected && authenticated && dragFromServer && !currentlyTransferring)
            {
                ToggleClientTools();

                AddToClientConsole("===== Downloading " + fileTransmitNum + " files from the server =====");

                foreach (ListViewItem item in listView2.SelectedItems)
                {
                    AddToClientConsole("<----- Downloading " + item.Text + " from the server. " +
                            item.SubItems[1].Text + "ytes downloading from the server.");
                }

                Thread thread = new Thread(downloadInitiate);
                thread.Start(syncClient);
                syncClient.threadDone.WaitOne();
                
                PopulateLocalListView(currentPath);

                ToggleClientTools();
            }

            dragFromServer = false;
        }
        
        /// <summary>
        /// Begins the upload transfer using the selected items gathered by listview1_itemdrag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void listView2_DragEnter(object sender, DragEventArgs e)
        {
            try {
                if (connected && authenticated && dragFromClient && !currentlyTransferring)
                {
                    ToggleClientTools();

                    AddToClientConsole("===== Uploading " + fileTransmitNum + " files to the server =====");

                    foreach (ListViewItem item in listView1.SelectedItems)
                    {
                        AddToClientConsole("-----> Uploading " + item.Text + " to the server. " +
                                item.SubItems[1].Text + "ytes uploading to the server.");
                    }

                    string filePath = Path.Combine(currentPath, fileForTransfer[0, 0]);
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    int fileSize = int.Parse(fileForTransfer[0, 1]);

                    //PopulateServerListView();
                    Thread thread = new Thread(uploadInitiate);
                    thread.Start(syncClient);
                    syncClient.threadDone.WaitOne();
                    
                    //Populate listView2(server side)
                    serverDirectoryList = syncClient.serverDirectoryNames;
                    serverFileList = syncClient.serverFileNames;
                    PopulateServerListView();

                    ToggleClientTools();
                }

                dragFromClient = false;
            }
            catch (Exception ex) when 
                (ex is UnauthorizedAccessException || ex is ArgumentNullException || ex is IndexOutOfRangeException)
            {
                AddToClientConsole("You do not have permission to upload that file/directory.");
                dragFromClient = false;
            }
        }
 
        /// <summary>
        /// Begins the upload process.
        /// </summary>
        /// <param name="obj">The Synchronous Client object to use.</param>
        private void uploadInitiate(Object obj)
        {
            if (!currentlyTransferring)
            {
                Console.WriteLine("Uploading to server");
                currentlyTransferring = true;

                SynchronousClient syncClient = (SynchronousClient)obj;

                syncClient.threadDone.Set();
                syncClient.SendUpload(fileForTransfer, currentPath);

                currentlyTransferring = false;
            }
        }

        /// <summary>
        /// Begins the download process.
        /// </summary>
        /// <param name="obj">The SynchronousClient object to use.</param>
        private void downloadInitiate(Object obj)
        {
            if (!currentlyTransferring)
            {
                Console.WriteLine("Downloading from server");
                currentlyTransferring = true;
                
                SynchronousClient syncClient = (SynchronousClient)obj;

                syncClient.threadDone.Set();
                syncClient.ReceiveDownload(fileForTransfer, currentPath);
                
                currentlyTransferring = false;
            }
        }

        /// <summary>
        /// Populates the local listview (listView1) given a local path.
        /// </summary>
        /// <param name="path">The path to use for populating the listview.</param>
        private void PopulateLocalListView(string path)
        {
            string[] files, directories;

            try
            {
                // Try to get files/directories
                files = Directory.GetFiles(path);
                directories = Directory.GetDirectories(path);
            }
            catch (Exception e) when (e is UnauthorizedAccessException || e is FileNotFoundException)
            {
                // Display appropriate errors
                Debug.WriteLine("CLIENT EXCEPTION: " + e.ToString());
                consoleTextBox.AppendText(Environment.NewLine + "[Client]: Either the file was not found or there are not enough permissions to open this directory.");

                // Reset current path to the parent directory (undoing the action that led to the error)
                DirectoryInfo parentDirectory = Directory.GetParent(path.EndsWith("\\") ? path : string.Concat(path, "\\"));
                currentPath = parentDirectory.Parent.FullName; // Sets current path to path of parent

                // Retry getting the file and directories 
                files = Directory.GetFiles(currentPath);
                directories = Directory.GetDirectories(currentPath);
            }
            
            // Clear listview1 
            /* TO BE CHANGED:
             *      THE FOLLOWING BLOCK OF CODE IS ONLY UPDATING THE LOCAL LISTVIEW1.
             *      TO MAKE IT MODULAR, WE MUST BE ABLE TO UPDATE EITHER LISTVIEW1 OR 
             *      LISTVIEW2 (THE SERVER VIEW). THIS SECTION WILL BE CHANGED LATER.
             */
            listView1.Clear();
            listView1.Columns.Add("File name", 250);
            listView1.Columns.Add("File size", 90, HorizontalAlignment.Right);
            listView1.Columns.Add("Last modified", 175);

            AddUpLevelDirectory(listView1); // Add .. directory

            AddDirectoriesToListView(listView1, directories); // Add directories to listview1

            AddFilesToListView(listView1, files); // Add files to listview1
        }

        /// <summary>
        /// Clears, initialises, then repopulates the server listview with data that has been received already.
        /// </summary>
        private void PopulateServerListView()
        {
            serverFileList = syncClient.serverFileNames;
            serverDirectoryList = syncClient.serverDirectoryNames;

            try
            {
                listView2.Clear();
                listView2.Columns.Add("File name", 250);
                listView2.Columns.Add("File size", 90, HorizontalAlignment.Right);
                listView2.Columns.Add("Last modified", 175);
            }
            catch(Exception e) { Debug.WriteLine(e.Message);  }
            
            AddUpLevelDirectory(listView2); // Add .. directory

            AddDirectoriesToListView(listView2, serverDirectoryList); // Add directories to listview1

            AddFilesToListView(listView2, serverFileList); // Add files to listview1
        }

        /// <summary>
        /// Adds all directory entries from a string[] to a specified ListView.
        /// </summary>
        /// <param name="listView">The ListView to add the directory entries to.</param>
        /// <param name="directories">The string[] containing directory names.</param>
        private void AddDirectoriesToListView(ListView listView, string[] directories)
        {
            if (directories.Length == 0) { return; }
            
            try
            {
                for (int i = 0; i < directories.Length; i++)
                {
                    ListViewItem listItem;

                    if (listView.Equals(listView1))
                    {
                        listItem = new ListViewItem(new[] {
                            Path.GetFileName(directories[i]), // Get directory name; using GetDirectoryName() returns absolute path of parent dir
                            null, // No directory size 
                            (new FileInfo(directories[i])).LastWriteTime.ToString() // Get last modified time
                        });
                    }
                    else
                    {
                        listItem = new ListViewItem(new[] {
                            directories[i], // Get directory name; using GetDirectoryName() returns absolute path of parent dir
                            null, // No directory size 
                            syncClient.serverDirectoryModifiedTimes[i] // Get last modified time
                        });
                    }

                    listItem.ImageKey = "folder"; // Assign folder image to item

                    listView.Items.Add(listItem); // Add item to local listview
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Could not add directories to listview!");
            }
        }

        /// <summary>
        /// Adds all file entries from a string[] to a specified ListView.
        /// </summary>
        /// <param name="listView">The ListView to add the file entries to.</param>
        /// <param name="files">The string[] containing directory names.</param>
        private void AddFilesToListView(ListView listView, string[] files)
        {
            if (files.Length == 0) { return; }
            
            ListViewItem listItem;

            for (int i = 0; i < files.Length; i++)
            {
                if (listView.Equals(listView1))
                {
                    listItem = new ListViewItem(new[] {
                        Path.GetFileName(files[i]),
                        (new FileInfo(files[i])).Length.ToString() + " B",
                        (new FileInfo(files[i])).LastWriteTime.ToString() // Get last modified time
                    });

                    //AddToClientConsole("Receiving " + Path.GetFileNamefiles[i] + " with " + syncClient.serverFileSizes[i] + " bytes of data.");
                }
                else
                {
                    listItem = new ListViewItem(new[] {
                        files[i],
                        syncClient.serverFileSizes[i] + " B",
                        syncClient.serverFileModifiedTimes[i]
                    });
                    
                    //AddToClientConsole("Adding " + files[i] + " with " + syncClient.serverFileSizes[i] + " bytes of data.");
                }

                listItem.ImageKey = "file"; // Assign file image to item

                listView.Items.Add(listItem); // Add item to local listview
            }
        }

        /// <summary>
        /// Adds the "up level" (what is this?) directory.
        /// </summary>
        /// <param name="listView">The ListView to add the "up level" directory to.</param>
        private void AddUpLevelDirectory(ListView listView)
        {
            ListViewItem listItem = new ListViewItem(new[] {
                    "..", // Get directory name
                    null, // No directory size 
                    null // Get last modified time
                });

            listItem.ImageKey = "folder"; // Assign folder image to item

            listView.Items.Add(listItem); // Add item to local listview
        }
        
        /// <summary>
        /// Starts the client and tries to connect.
        /// </summary>
        /// <param name="obj"></param>
        public static void threadCallback(object obj)
        {
            SynchronousClient syncClient = (SynchronousClient)obj;

            Exception success = syncClient.StartClient();

            try
            {
                if (!(success == null))
                {
                    MessageBox.Show("Could not connect!");
                }
            }
            catch (NullReferenceException)
            {
                syncClient.threadDone.Set();
            }
            finally
            {
                syncClient.threadDone.Set();
            }
            
            return;
        }

        /// <summary>
        /// Attempts to authenticate the user.
        /// </summary>
        public bool userAuthentication()
        {
            if (syncClient.isConnected)
            {
                AddToClientConsole("Connected to server at " + serverIpAddress + ":" + port);

                try
                {
                    //Send "userName" to server
                    syncClient.sendUserName(userName);

                    //Send password
                    syncClient.sendPassword(password);

                    //If password matches, client side will receive "in"
                    authenticated = syncClient.receiveAuthentication();

                    if (authenticated)
                    {
                        //Receive directory list from server
                        syncClient.receiveServerDirectoryInfo();

                        //save the received lists to Global lists
                        serverDirectoryList = syncClient.serverDirectoryNames;
                        serverFileList = syncClient.serverFileNames;

                        //Populate listView2(server side)
                        PopulateServerListView();

                        AddToClientConsole("Authentication was a success! Getting your server directory.");

                        return true;
                    }
                    else
                    {
                        syncClient.client.Disconnect(false); // Close socket, but allow reuse of it
                        AddToClientConsole("Authentication with server failed. Did you use the correct credentials?");

                        return false;
                    }
                        
                }
                catch(NullReferenceException ex)
                {   
                    MessageBox.Show(ex.Message);
                    return false;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }
            }
            else
            {
                AddToClientConsole("Uh-oh. The client is not currently connected to the server.");
                return false;
            }
        }

        /// <summary>
        /// This event handles when the Refresh button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!currentlyTransferring)
            {
                RefreshDirectories();
            }
        }

        /// <summary>
        /// This executes when the Connection>New connection... button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open connection form as a modal (dialog) so that the user MUST interact with it.
            ConnectionForm connectionForm = new ConnectionForm();

            connectionForm.ShowDialog();

            if (connectionForm.getServerIP() == null || connectionForm.getPort() == 0
                || connectionForm.getUserName() == null || connectionForm.getPassword() == null)
            {
                AddToClientConsole("Connection to server failed or quit.");
            }
            else
            {
                serverIpAddress = connectionForm.getServerIP();
                port = connectionForm.getPort();
                userName = connectionForm.getUserName();
                password = connectionForm.getPassword();

                //NOW HERE IS WHERE YOU START CONNECTION
                try
                {
                    if (syncClient != null)
                        syncClient = null;

                    syncClient = new SynchronousClient(serverIpAddress, port, userName, password, this);

                    Thread thread = new Thread(threadCallback);
                    thread.Start(syncClient);
                    syncClient.threadDone.WaitOne();

                    connected = true;

                    //Authentication Process
                    if (userAuthentication()) // Authentication was successful!
                    {
                        connectionToolStripMenuItem.Enabled = false;
                    }
                    else // Authentication was a complete, utter failure.
                    {
                        connectionToolStripMenuItem.Enabled = true;
                    }
                }
                catch (NullReferenceException ex)
                {
                    Console.WriteLine("new connection menu button error: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("New connection menu button error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// "Delete" button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!currentlyTransferring)
            {
                int numberOfSelectedItems = 0;

                // If there are items selected in the local listview...
                if ((numberOfSelectedItems = listView1.SelectedItems.Count) > 0)
                {

                    string[] name = new string[numberOfSelectedItems];

                    FileAttributes attr;
                    bool isOKtoDelete = true;

                    for (int i = 0; i < numberOfSelectedItems; i++)
                    {
                        name[i] = listView1.SelectedItems[i].Text;

                        string temp = currentPath;
                        temp = Path.Combine(currentPath, name[i]);
                        attr = File.GetAttributes(temp);

                        //IF selected item is DIRECTORY, check if the directory is empty 
                        //If it is empty, it is ok to delete the directory
                        if (attr.HasFlag(FileAttributes.Directory))
                        {
                            string[] filesTemp = Directory.GetFiles(temp);
                            string[] directoryTemp = Directory.GetDirectories(temp);

                            if ((filesTemp.Length != 0) || (directoryTemp.Length != 0))
                            {
                                isOKtoDelete = true;
                            }
                        }
                    }

                    if (!isOKtoDelete)
                    {
                        AddToClientConsole("This version currently does not support deleting directories with ");
                    }
                    else if (isOKtoDelete)
                    {
                        deleteSelectedItem(currentPath, name, numberOfSelectedItems);
                    }

                    PopulateLocalListView(currentPath);
                }
                // Check if items selected on server side...
                else if ((numberOfSelectedItems = listView2.SelectedItems.Count) > 0)
                {
                    string[] listOfItems = new string[numberOfSelectedItems];
                    string list = null;

                    for (int i = 0; i < numberOfSelectedItems; i++)
                    {
                        list = list + listView2.SelectedItems[i].SubItems[0].Text + Environment.NewLine;
                        listOfItems[i] = listView2.SelectedItems[i].Text;
                    }
                    
                    DialogResult result = MessageBox.Show("Are you sure you wish to delete the following items?\n" + list + Environment.NewLine, " ", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

                    if (result == DialogResult.OK)
                    {
                        for (int i = 0; i < numberOfSelectedItems; i++)
                        {
                            Debug.WriteLine("Filename to delete: " + listOfItems[i]);
                            syncClient.DeleteProcess(listOfItems[i]);

                            syncClient.receiveServerDirectoryInfo();
                            PopulateServerListView();
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// "New Folder" button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newFoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!currentlyTransferring)
            { 
                string path = currentPath;
                // Show dialog box for creating new folder
                makeNewDirectory newDir = new makeNewDirectory();
                newDir.ShowDialog();

                //Populate the listview after the folder creating process
                PopulateLocalListView(path);

                if (listView1.Focused)
                {
                    if (newDir.newFolderName != null)
                    {
                        string pathForNew = Path.Combine(path, newDir.newFolderName);

                        if (!Directory.Exists(pathForNew))
                        {
                            Directory.CreateDirectory(pathForNew);
                        }
                        else
                        {
                            AddToClientConsole("[ERROR]: An error occurred while making a new folder.");
                        }

                        //Populate the listview after the folder creating process
                        PopulateLocalListView(path);
                    }
                }

                //if listView2 is selected
                if (listView2.Focused)
                {
                    if (!connected || !authenticated)
                    {
                        AddToClientConsole("Client is not currently connected or authenticated to a server. Creating a new directory failed.");
                    }
                    else
                    {
                        if (newDir.newFolderName != null)
                        {
                            bool error = syncClient.makeNewFolderOnServer(newDir.newFolderName);

                            if (error)
                            {
                                AddToClientConsole("[ERROR]: An error occurred while making a new folder on the server.");
                            }
                            else
                            {
                                syncClient.receiveServerDirectoryInfo();
                                PopulateServerListView();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method delete selected Item (it only deletes files and empty directory)
        /// </summary>
        /// <param name="currentpath"></param>
        /// <param name="selectedItem"></param>
        /// <param name="numberOfSelectedItems"></param>
        public void deleteSelectedItem(string currentPath, string[] selectedItem, int numberOfSelectedItems)
        {
            string list = null;
            for (int i =0; i<numberOfSelectedItems; i++)
            {
                list = list + selectedItem[i] + Environment.NewLine;
            }

            DialogResult result = MessageBox.Show("Are you sure you wish to delete the following items?\n" + list +"\n", " ", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

            if (result == DialogResult.OK)
            {
                FileAttributes attr;

                for (int i = 0; i < numberOfSelectedItems; i++)
                {
                    string temp = currentPath;
                    temp = Path.Combine(currentPath, selectedItem[i]);

                    attr = File.GetAttributes(temp);

                    //IF selected item is DIRECTORY, check if the directory is empty 
                    //If it is empty, it is ok to delete the directory
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        //Deleting Empty Directory
                        try
                        {
                            Directory.Delete(temp, true);
                        }
                        catch (Exception)
                        {
                            AddToClientConsole("Uh-oh. Could not delete the directory \"" + selectedItem[i] + "\"!");
                        }
                    }
                    else
                    {
                        //Deleting files
                        try
                        {
                            File.Delete(temp);
                        }
                        catch (Exception)
                        {
                            AddToClientConsole("Uh-oh. Could not delete the file \"" + selectedItem[i] + "\"!");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds a string to the client console window. Used for update messages.
        /// </summary>
        /// <param name="text">The text to be added. Datetime and new line are automatically added.</param>
        private void AddToClientConsole(string text)
        {
            this.consoleTextBox.AppendText("[" + DateTime.Now.ToLocalTime() + "]: " + text + Environment.NewLine);

            this.consoleTextBox.SelectionStart = this.consoleTextBox.Text.Length;
            this.consoleTextBox.ScrollToCaret();
        }
        
        /// <summary>
        /// Attempts to populate both server and local directories. If it fails to 
        /// populate server directory, it clears it (because no connection is likely established)
        /// </summary>
        public void RefreshDirectories()
        {
            try
            {
                Debug.WriteLine("POPULATING LOCAL");
                PopulateLocalListView(currentPath);

                try
                {
                    syncClient.CleanSocketBuffer(syncClient.client);

                    Debug.WriteLine("REQUESTING SERVER DIR");
                    syncClient.RequestServerDirectory();
                    Debug.WriteLine("RECEIVING SERVER DIR");
                    syncClient.receiveServerDirectoryInfo();

                    Debug.WriteLine("POPULATING SERVER");
                    PopulateServerListView();

                    AddToClientConsole("Client and server directories have been refreshed.");
                }
                catch (Exception)
                {
                    AddToClientConsole("The local directory view has been updated.");
                    if (syncClient != null)
                    {
                        syncClient.client.Close();
                        syncClient = null;
                        connected = false;
                        authenticated = false;
                        currentlyTransferring = false;
                        connectionToolStripMenuItem.Enabled = true;
                    }
                    listView2.Clear();
                }
            }
            catch (Exception)
            {
                AddToClientConsole("Could not refresh the local or server directory views.");
            }
        }

        /// <summary>
        /// Toggles (en/disables) the client tools.
        /// </summary>
        public void ToggleClientTools()
        {
            refreshToolStripMenuItem.Enabled = !refreshToolStripMenuItem.Enabled;
            deleteToolStripMenuItem.Enabled = !deleteToolStripMenuItem.Enabled;
            newFoToolStripMenuItem.Enabled = !newFoToolStripMenuItem.Enabled;
        }
    }
}