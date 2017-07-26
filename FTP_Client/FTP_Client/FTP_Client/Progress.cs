using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FTP_Client
{
    public partial class Progress : Form
    {
        private int numberOfFiles, numberCompleted = 0;

        public Progress()
        {
            InitializeComponent();
        }

        public Progress(int totalFiles)
        {
            numberOfFiles = totalFiles;
            InitializeComponent();

            this.progressBar1.Maximum = totalFiles;
            this.progressBar1.Minimum = 0;
            this.progressBar1.Step = 1;

            this.progressBar1.Visible = true;
        }

        public void IncrementProgress()
        {
            this.progressBar1.PerformStep();
        }

        public int NumberCompleted
        {
            get { return numberCompleted; }
            set { numberCompleted = value; }
        }
    }
}
