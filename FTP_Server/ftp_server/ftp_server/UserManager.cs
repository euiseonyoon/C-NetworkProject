using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ftp_server
{
    class UserManager
    {
        private string userID = null;
        private string password = null;
        private Boolean authenticated = false;

        public UserManager() { }
        
        public UserManager(string userID, string password)
        {
            this.userID = userID;
            this.password = password;
        }

        /// <summary>
        /// Attempts to search for the user ID in userList.txt
        /// </summary>
        /// <param name="userID">The user ID to look for</param>
        /// <returns>True if the ID is found; false otherwise.</returns>
        public static bool searchID(string userID)
        {
            string infoLine;

            try
            {
                if (userID.Equals("") || userID.Equals(null))
                {
                    return false;
                }

                StreamReader sr = File.OpenText("userList.txt");

                // Iterate over file until a match is found
                while ((infoLine = sr.ReadLine()) != null)
                {
                    string[] subString = infoLine.Split(' ');

                    if (userID == subString[0])
                    {
                        return true; // ID has been found
                    }
                }

                sr.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not search for user ID: " + e.ToString());
                return false; // ID has not been found or some error occurred
            }

            return false; // ID has not been found
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool validateCredentials(string userID, string password)
        {
            string infoLine;
            string ID = userID.Trim();
            String PW = password.Trim();

            try
            {
                StreamReader sr = File.OpenText("userList.txt");

                // Iterate over file until a match is found
                while ((infoLine = sr.ReadLine()) != null)
                {
                    string[] subString = infoLine.Split(' ');

                    //Console.WriteLine("ID length : "+ID.Length + "\n PW length : " + PW.Length);
                    
                    if (ID == subString[0] && PW == subString[1])
                    {
                        return true; // They are valid credentials
                    }
                }

                sr.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Could not validate user credentials: " + e.ToString());
                return false; // They are invalid credentials
            }

            return false; // They are not valid credentials
        }

        /// <summary>
        /// Attempts to create a directory for the given user ID.
        /// </summary>
        /// <param name="userID">The user ID to create a directory for.</param>
        public static void createUserDirectory(string userID)
        {
            string temp = Directory.GetCurrentDirectory();
            string path = Path.Combine(temp, userID);

            try
            {
                if (userID.Equals("") || userID.Equals(null))
                {
                    MessageBox.Show("Invalid user ID. Please try again.");
                    return;
                }

                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                    MessageBox.Show("That user already exists.");
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);
                MessageBox.Show("The user was successfully created!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("User creation error:\n\n" + ex.Message);
            }

            return;
        }

        /// <summary>
        /// Adds a user to the user list.
        /// </summary>
        /// <param name="userID">The user ID of the new user.</param>
        /// <param name="password">The password of the new user.</param>
        public static void addUserToList(string userID, string password)
        {
            try
            {
                string line = userID + " " + password;

                StreamWriter w = File.AppendText("userList.txt");
                w.WriteLine(line);
                
                w.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Adding user failed:\n\n" + ex.Message);
            }
            return;
        }

        /// <summary>
        /// Deletes a user from the user list.
        /// </summary>
        /// <param name="userID">The user ID to delete.</param>
        public static bool deleteUserID(string userID)
        {
            bool found = false;
            
            try
            {
                string infoLine;
                string tempFile = Path.GetTempFileName();
                StreamReader sr = File.OpenText("userList.txt");
                StreamWriter sw = new StreamWriter(tempFile);

                infoLine = sr.ReadLine();
                int i = 1;

                while (infoLine != null)
                {
                    if (!infoLine.Contains(userID))
                    {
                        sw.WriteLine(infoLine);
                    }
                    else
                    {
                        found = true;
                    }

                    infoLine = sr.ReadLine();
                    i++;
                }

                sw.Close();
                sr.Close();

                File.Delete("userList.txt");
                File.Move(tempFile, "userList.txt");
            }
            catch (Exception)
            {
                found = false;
            }

            Debug.WriteLine("FOUND = " + found);

            return found; 
        }

        /// <summary>
        /// Deleting user's directory
        /// </summary>
        /// <param name="userID">The user of which the directory will be deleted.</param>
        public static bool deleteUserDirectory(string userID)
        {
            string temp = Directory.GetCurrentDirectory();
            string path = Path.Combine(temp, userID);

            try
            {
                // Determine whether the directory exists.
                if (!Directory.Exists(path))
                {
                    //MessageBox.Show("That path does not exist.");
                    return false;
                }

                // Try to delete the directory.
                Directory.Delete(path);
                //MessageBox.Show("The user was deleted successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not delete the directory: \n\n" + ex.Message);
                //MessageBox.Show("Could not delete the directory: \n\n" + ex.Message);
                return false;
            }

            return true;
        }

        public string UserID
        {
            get
            {
                return this.userID;
            }
        }

        public string Password
        {
            get
            {
                return this.password;
            }
        }

        public Boolean Authenticated
        {
            get
            {
                return this.authenticated;
            }
            set
            {
                this.authenticated = !this.authenticated;
            }
        }
    }
}
