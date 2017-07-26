using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FTP_Client
{
    public partial class ConnectionForm : Form
    {
        private string serverIpAddress;
        private int port;
        private string userName;
        private string password;

        public ConnectionForm()
        {
            InitializeComponent();            
        }

        /// <summary>
        /// Checks if the connection info is valid and ok to use.
        /// </summary>
        /// <param name="tempIp"></param>
        /// <param name="tempPort"></param>
        /// <param name="tempId"></param>
        /// <param name="tempPassword"></param>
        /// <returns></returns>
        public bool validateConnectionInfo(string tempIp, string tempPort, string tempId, string tempPassword)
        {
            bool isOK = true;

            //Validate IP address
            //if(tempIp == null || tempIp.Contains(" "))
            //{   
            //    isOK = false;
            //}
            //else
            //{
            //    string[] octet = tempIp.Split('.');

            //    //if IP doesn't have 4 octet, isOK = false
            //    if(octet.Length != 4)
            //    {
            //        isOK = false;
            //    }

            //    for(int i = 0; i < octet.Length; i++)
            //    {
            //        int octetNumber;

            //        //if a octet is not just inteter, isOK = false;
            //        if (!int.TryParse(octet[i],out octetNumber))
            //        {   
            //            isOK = false;
            //            break;
            //        }
            //        else
            //        {
            //            int.TryParse(octet[i], out octetNumber);

            //            //if a octet number is out of range, -> isOK = false;
            //            if(octetNumber < 0 || octetNumber > 255)
            //            {   
            //                isOK = false;
            //            }
            //        }
            //    }
            //}

            if (validateIP(tempIp) == true)
            {
                isOK = true;
            }
            else
            {
                isOK = false;
            }

            //Validating port number. if it's not just integer,
            //or if it's empty, or if it contains SPACES, -> isOK = false;
            //int n;
            //if(!int.TryParse(tempPort,out n) || tempPort == null || tempPort.Contains(" "))
            //{
            //    isOK = false;
            //}

            if (validatePort(tempPort) == true)
            {
                isOK = true;
            } 
            else
            {
                isOK = false;
            }

            //Validating userID
            if(tempId == null || tempId.Contains(" ") || tempId.Length > 32)
            {
                isOK = false;
            }

            //Validating password
            if(tempPassword == null || tempPassword.Contains(" "))
            {
                isOK = false;
            }

            return isOK;
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            string tempIp, tempPort, tempId, tempPassword;
            bool isOK = true;
            
            tempIp = this.textBox1.Text;
            tempPort = this.textBox2.Text;
            tempId = this.textBox3.Text;
            tempPassword = this.textBox4.Text;

            isOK = validateConnectionInfo(tempIp, tempPort, tempId, tempPassword);

            if(!isOK)
            {
                serverIpAddress = null;
                port = 0;
                userName = null;
                password = null;
            }
            else
            {
                serverIpAddress = tempIp;
                port = System.Convert.ToInt32(this.textBox2.Text);
                userName = tempId;
                password = tempPassword;
            }
            
            this.Close();
        }

        /// <summary>
        /// Validates an IP address in string format (IPV4).
        /// This can be changed to accept IPv4 AND IPv6 if necessary.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns></returns>
        private static Boolean validateIP(string ipAddress)
        {
            try
            {
                IPAddress address = IPAddress.Parse(ipAddress);

                // If it is an IPv4 address, return true, false otherwise.
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the port is within the valid range, and that it is a number.
        /// </summary>
        /// <param name="port">The port to check </param>
        /// <returns>True if valid port; false otherwise.</returns>
        private static Boolean validatePort(string port)
        {
            try
            {
                if (int.Parse(port) > IPEndPoint.MinPort &&
                    int.Parse(port) < IPEndPoint.MaxPort)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string getServerIP() { return serverIpAddress; }
        public int getPort() { return port; }
        public string getUserName() { return userName; }
        public string getPassword() { return password; }
    }
}
