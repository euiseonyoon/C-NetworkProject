using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace ftp_server
{
    public partial class Form1 : Form
    {
        bool serverRunning;
        Thread serverThread;
        SocketListener listener;

        public Form1()
        {
            InitializeComponent();
            serverRunning = false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="password"></param>
        /// <param name="confirmPassword"></param>
        /// <returns></returns>
        private bool validateAddInput(string userID, string password, string confirmPassword)
        {
            bool error = false;

            // Use Regex here
            if (userID.Contains(" ") || password.Contains(" ") ||
                userID.Equals("") || password.Equals(""))
            {
                error = true;
            }

            if (password != confirmPassword)
            {
                error = true;
            }

            if (error == true)
            {
                MessageBox.Show("Invalid user ID and/or password, or your passwords did not match. Please try again.");
            }

            return error;
        }
        
        /// <summary>
        /// "Add User" button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click_AddUser(object sender, EventArgs e)
        {   
            string userID, password,confirmPassword;
            userID = this.textBox_AddUserID.Text;
            password = this.textBox_AddUserPassword.Text;
            confirmPassword = this.textBox_AddUserConfirm.Text;
            bool error = false;
            bool idFound = false;

            //Validate user inputs 
            error = validateAddInput(userID, password, confirmPassword);

            //Check if the ID is already taken or not.
            idFound = UserManager.searchID(userID);

            if (idFound)
            {
                MessageBox.Show("The ID is in use");
                clearTextBoxes();
                return;
            }
            
            //User adding part
            if(!error && !idFound)
            {
                //Adding user to the List
                UserManager.addUserToList(userID, password);

                //Create User's folder               
                UserManager.createUserDirectory(userID);
            }

            clearTextBoxes();
            return;            
        }
                
        /// <summary>
        /// If "Delete user" button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            string IDforDelete = textBox_DeleteUserID.Text;
            
            // Delete user info from userList.txt
            // Delete user's directory
            if (UserManager.deleteUserDirectory(IDforDelete) &&
                UserManager.deleteUserID(IDforDelete))
            {
                MessageBox.Show("User successfully deleted!");
            }
            else
            {
                MessageBox.Show("User could not be deleted. Try again.");
            }

            clearTextBoxes();
        }
        
        /// <summary>
        /// IF "Start the server" button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click_1(object sender, EventArgs e)
        {
            if (!serverRunning)
            {
                
                try
                {
                    listener = new SocketListener(int.Parse(textBox_Port.Text));
                    serverRunning = true;
                }
                catch (Exception ex) when
                    (ex is OverflowException || ex is FormatException || ex is ArgumentNullException)
                {
                    MessageBox.Show("An invalid port number was used. Please try again.");
                    return;
                }
                
                serverThread = new Thread(new ThreadStart(listener.StartListening));
                serverThread.Start();

                button_StopServer.Enabled = true; // Allow pressing "Stop the server"
                button_StartServer.Enabled = false; // Disallow pressing "Start the server"
            }
            else if (serverRunning)
            {
                MessageBox.Show("Server is already running");
            }
        }

        /// <summary>
        /// If "Stop the server" button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (serverRunning)
                {
                    listener.StopListening();

                    serverThread.Abort();
                    serverRunning = false;
                    MessageBox.Show("Server stopped running");

                    button_StopServer.Enabled = false; // Allow pressing "Stop the server"
                    button_StartServer.Enabled = true; // Disallow pressing "Start the server"
                }
                else if (!serverRunning)
                {
                    MessageBox.Show("Server already not running");
                }
            }
            catch (ObjectDisposedException ode)
            {
                Debug.WriteLine("The listener and serverThread objects have been disposed: " + ode.ToString());
            }
        }

        private void clearTextBoxes()
        {
            textBox_AddUserID.Text = "";
            textBox_AddUserPassword.Text = "";
            textBox_AddUserConfirm.Text = "";
            textBox_DeleteUserID.Text = "";
        }
    }   
}


