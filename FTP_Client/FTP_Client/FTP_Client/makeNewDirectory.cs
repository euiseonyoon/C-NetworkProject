using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace FTP_Client
{
    public partial class makeNewDirectory : Form
    {
        //string currentpath;
        public string newFolderName;

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="currentPath"></param>
        public makeNewDirectory()
        {
            InitializeComponent();
        }

        /// <summary>
        /// "Create" button is clicked to create new folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            bool nameValid = true;
            string folderName = this.textBox1.Text;

            //Get char array of invalid characters for file name
            char[] notOkValue = Path.GetInvalidFileNameChars();

            //If fileName was empty
            if (folderName.Length == 0)
            {
                folderName = null;
                nameValid = false;
            }
            else
            {
                for (int i = 0; i < notOkValue.Length; i++)
                {
                    //If fileName from user contains any invalid character
                    if (folderName.Contains(notOkValue[i].ToString()))
                    {
                        nameValid = false;
                    }
                }
            }



            //If fileName contains any of non-valid characters 
            if (!nameValid)
            {
                MessageBox.Show("Invalid file name!");
                this.textBox1.Text = "";
                folderName = null;
            }//If fileName is valid 
            else if (nameValid)
            {
                newFolderName = folderName;
                this.Close();
            }
        }

        /// <summary>
        /// "Cancel" button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            newFolderName = null;
            this.Close();
        }
    }
}
