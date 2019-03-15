using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageInfo
{
    public partial class MainForm : Form
    {
        List<FileInfo> Data { get; }
        BindingSource src = new BindingSource();

        public MainForm(List<FileInfo> data)
        {
            Data = data;
            InitializeComponent();
            InitializeDataGridView();
        }

        private void InitializeDataGridView()
        {
            dataGridView.DataSource = Data;
        }
    }
}
