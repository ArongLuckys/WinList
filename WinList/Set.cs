using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinList
{
	public partial class Set : Form
	{
		public Set()
		{
			InitializeComponent();
			textBox1.Text = Properties.Settings.Default.UserAttribute;
		}

		private void Set_FormClosing(object sender, FormClosingEventArgs e)
		{
			WinList.Main.UserAttribute = textBox1.Text;
			Properties.Settings.Default.UserAttribute = textBox1.Text;
			Properties.Settings.Default.Save();
		}
	}
}
