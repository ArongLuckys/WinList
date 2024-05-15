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
using System.Xml.Linq;
using NXOpen;
using NXOpen.Assemblies;
using NXOpen.CAE;
using NXOpen.Features.ShipDesign;
using NXOpen.UF;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static NXOpen.CAE.Post;
using static NXOpen.UF.UFPart;
using static WinList.Main;

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


		public struct Rect
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		public struct WinListData
		{
			public Tag Tag;
			public string Name;
			public string FileName;
			public bool Attr;
		}

		const int HWND_TOPMOST = -1;
		const int SWP_NOMOVE = 0x0002;
		const int SWP_NOSIZE = 0x0001;

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
		List<WinListData> winListDatas = new List<WinListData>();
		System.Drawing.Point win1, win2;
		double x1, x2, x3, y1, y2, y3;
		public static string UserAttribute;

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

			ColumnHeader columnHeader = new ColumnHeader { Text = "文件名", Width = 150, TextAlign = HorizontalAlignment.Left };
			ColumnHeader columnHeader2 = new ColumnHeader { Text = "中文名", Width = 100, TextAlign = HorizontalAlignment.Left };
			listView1.Columns.Add(columnHeader2);
			listView1.Columns.Add(columnHeader);

			UserAttribute = Properties.Settings.Default.UserAttribute;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (partsum != UFSession.GetUFSession().Part.AskNumParts())
			{
				partsum = UFSession.GetUFSession().Part.AskNumParts();
				textBox2.Text = "一共打开了 " + partsum + " 个部件";
				listView1.Items.Clear();
				winListDatas.Clear();
				listView1.BeginUpdate();
				for (int i = 0; i < UFSession.GetUFSession().Part.AskNumParts(); i++)
				{
					string name;
					UFSession.GetUFSession().Part.AskPartName(UFSession.GetUFSession().Part.AskNthPart(i), out name);
					WinListData winListData = new WinListData();
					winListData.Tag = UFSession.GetUFSession().Part.AskNthPart(i);
					NXOpen.NXObject nXObject = (NXOpen.NXObject)NXOpen.Utilities.NXObjectManager.Get(UFSession.GetUFSession().Part.AskNthPart(i));
					string names;
					try
					{
						 names = nXObject.GetStringUserAttribute(UserAttribute, 0);
					}
					catch
					{
						//没有这个标题或者内容
						names = "null";
					}
					winListData.Name = names;
					winListData.FileName = Path.GetFileNameWithoutExtension(name);
					winListData.Attr = false;
					winListDatas.Add(winListData);
				}

				for (int i =0;i<winListDatas.Count;i++)
				{
					if (textBox1.Text == "")
					{
						ListViewItem listViewItem = new ListViewItem();
						listViewItem.Text = winListDatas[i].Name;
						listViewItem.SubItems.Add(winListDatas[i].FileName + "");
						listView1.Items.Add(listViewItem);
					}
					else
					{
						string text = textBox1.Text.ToLower();
						if (winListDatas[i].FileName.ToLower().LastIndexOf(text) != -1)
						{
							ListViewItem listViewItem = new ListViewItem();
							listViewItem.Text = winListDatas[i].Name;
							listViewItem.SubItems.Add(winListDatas[i].FileName + "");
							listView1.Items.Add(listViewItem);
						}
					}
				}
				listView1.EndUpdate();
				listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
			}
		}

		private void Main_Load(object sender, EventArgs e)
		{
			
		}

		private void button1_Click(object sender, EventArgs e)
		{
			textBox1.Text = "";
		}

		private void button5_Click(object sender, EventArgs e)
		{
			Set set = new Set();
			set.ShowDialog();
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

				//渐入透明
				win1 = this.Location;
				win2 = new System.Drawing.Point(this.Location.X + this.Size.Width, this.Location.Y + this.Size.Height);
				if (MousePosition.X > win1.X - 80 && MousePosition.Y > win1.Y - 80 && MousePosition.X < win2.X + 80 && MousePosition.Y < win2.Y + 80)
				{
					this.Opacity = 1;
				}
				else
				{
					this.Opacity = 0.5;
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

		private void listView1_Click(object sender, EventArgs e)
		{
			if (listView1.Items.Count != 0)
			{
				foreach (WinListData wd in winListDatas)
				{
					if (wd.FileName == listView1.SelectedItems[0].SubItems[1].Text)
					{
						UFSession.GetUFSession().Part.SetDisplayPart(wd.Tag);
					}
				}
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
						UFSession.GetUFSession().Ui.OpenListingWindow();
						UFSession.GetUFSession().Ui.WriteListingWindow("含有打开失败的文件！1.该文件可能已损坏或高于该NX版本  2.该文件已经打开了   ->   " + openFileDialog1.FileNames[i] + "\n");
					}
				}
			}
		}
	}
}
