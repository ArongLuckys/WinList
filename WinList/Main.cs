using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NXOpen;
using NXOpen.Features.ShipDesign;
using NXOpen.UF;
using static System.Net.WebRequestMethods;
using static NXOpen.CAE.Post;
using static NXOpen.UF.UFPart;

namespace WinList
{
	public partial class Main : Form
	{
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll")]
		private static extern int GetWindowRect(IntPtr hwnd, out Rect lpRect);

		[DllImport("user32.dll")]
		public static extern int EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32", SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		public struct Rect
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		const int HWND_TOPMOST = -1;
		const int SWP_NOMOVE = 0x0002;
		const int SWP_NOSIZE = 0x0001;


		public List<Tag> tags = new List<Tag>();
		public IntPtr nx;
		public int partsum; //打开的部件数量
		public System.Drawing.Point pointnx,poingform;
		public Rect rect, rectT;
		List<string> filespath1 = new List<string>();
		public Tag part1;
		public LoadStatus error_status1;
		int count;
		Tag[] part_list;
		int[] error_list;
		List<IntPtr> childWindows = new List<IntPtr>();

		public Main()
		{
			InitializeComponent();
			nx = UFSession.GetUFSession().Ui.GetDefaultParent();
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
			
			GetWindowRect(UFSession.GetUFSession().Ui.GetDefaultParent(),out rect);
			int fromx = rect.Right - this.Width - 10;
			int fromy = rect.Top + ((rect.Bottom - rect.Top) / 2) - (this.Height / 2);
			this.Location = new System.Drawing.Point(fromx, fromy);

			pointnx.X =  rect.Left;
			pointnx.Y = rect.Top;
			poingform.X = this.Location.X;
			poingform.Y = this.Location.Y;

		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (partsum != UFSession.GetUFSession().Part.AskNumParts())
			{
				partsum = UFSession.GetUFSession().Part.AskNumParts();
				textBox2.Text = "一共打开了 " + partsum + " 个部件";
				listBox1.Items.Clear();
				tags.Clear();
				for (int i = 0; i < UFSession.GetUFSession().Part.AskNumParts(); i++)
				{
					if (textBox1.Text != "")
					{
						string name;
						UFSession.GetUFSession().Part.AskPartName(UFSession.GetUFSession().Part.AskNthPart(i), out name);

						string text = textBox1.Text.ToLower();
						if (Path.GetFileNameWithoutExtension(name).ToLower().LastIndexOf(text) != -1)
						{
							listBox1.Items.Add(Path.GetFileNameWithoutExtension(name));
							tags.Add(UFSession.GetUFSession().Part.AskNthPart(i));
						}
					}
					else
					{
						string name;
						UFSession.GetUFSession().Part.AskPartName(UFSession.GetUFSession().Part.AskNthPart(i), out name);

						listBox1.Items.Add(Path.GetFileNameWithoutExtension(name));
						tags.Add(UFSession.GetUFSession().Part.AskNthPart(i));
					}
				}
			}
		}

		private void Main_Load(object sender, EventArgs e)
		{
			this.BringToFront();
			//bool ok = SetForegroundWindow(UFSession.GetUFSession().Ui.GetDefaultParent());
			//MessageBox.Show(ok+"");
			//SetWindowPos(UFSession.GetUFSession().Ui.GetDefaultParent(), (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1)
			{
				UFSession.GetUFSession().Part.SetDisplayPart(tags[listBox1.SelectedIndex]);
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			textBox1.Text = "";
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			partsum = 0;
		}

		private void timer2_Tick(object sender, EventArgs e)
		{
			if (nx != IntPtr.Zero)
			{
				//最小化事件处理
				if (IsIconic(nx))
				{
					this.Hide();
				}
				else
				{
					this.Show();
				}

				//移动窗口
				GetWindowRect(UFSession.GetUFSession().Ui.GetDefaultParent(),out rectT);
				if (rect.Left != rectT.Left || rect.Right != rectT.Right || rect.Top != rectT.Top || rect.Bottom != rectT.Bottom)
				{
					int fromx = rectT.Right - this.Width - 10;
					int fromy = rectT.Top + ((rectT.Bottom - rectT.Top) / 2) - (this.Height / 2);
					this.Location = new System.Drawing.Point(fromx, fromy);
					rect = rectT;
				}
			}
		}

		public bool NXTop()
		{
			childWindows.Clear();
			childWindows.Add(UFSession.GetUFSession().Ui.GetDefaultParent());
			EnumWindowsProc myDelegate = new EnumWindowsProc(EnumChildWindowsCallback);
			EnumChildWindows(UFSession.GetUFSession().Ui.GetDefaultParent(), myDelegate, IntPtr.Zero);

			bool result = childWindows.Exists(t => t == GetForegroundWindow());
			return result;
		}

		private bool EnumChildWindowsCallback(IntPtr hWnd, IntPtr lParam)
		{
			if (IsWindowVisible(hWnd))
			{
				childWindows.Add(hWnd);
			}
			return true;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			try
			{
				UFSession.GetUFSession().Part.SaveAll(out count, out part_list, out error_list);
			}
			catch
			{
				MessageBox.Show(error_list.Length + "个文件保存错误！");
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			try
			{
				UFSession.GetUFSession().Part.CloseAll();
			}
			catch 
			{
				MessageBox.Show("部分文件关闭失败！需要手动关闭");
			}
		}



		private void button4_Click(object sender, EventArgs e)
		{
			filespath1.Clear();

			openFileDialog1.Multiselect = true;
			openFileDialog1.Title = "选择你要点开的部件，可以使用【Ctrl+A 】全选该文件夹下的所有文件！高版本则不会打开！";
			openFileDialog1.Filter = "NX files (*.prt)|*.prt";

			if (openFileDialog1.ShowDialog() == DialogResult.OK)
			{
				filespath1.AddRange(openFileDialog1.SafeFileNames);
				for (int i = 0; i < openFileDialog1.FileNames.Length; i++)
				{
					try
					{
						UFSession.GetUFSession().Part.Open(openFileDialog1.FileNames[i], out part1, out error_status1);
					}
					catch
					{
						MessageBox.Show("含有打开失败的文件！该文件可能已损坏或高于该NX版本\n" + openFileDialog1.FileNames[i]);
					}
				}
			}
		}
	}
}
