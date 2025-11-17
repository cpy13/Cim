using System;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using OMRON.Compolet.Variable;

namespace VariableCompoletSample
{
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ColumnHeader Type;
		private System.Windows.Forms.ColumnHeader VarName;
		private System.Windows.Forms.CheckBox chk_Active;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tab_Information;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Button btn_ClearEvent;
		private System.Windows.Forms.Button btn_ClearAllEvent;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.GroupBox groupBox2;       
        private System.Windows.Forms.Button button_ReadVariable;
        private System.Windows.Forms.Button button_TestSD;
        private System.Windows.Forms.TextBox textBox_Name;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button button_WriteValue;
		private System.Windows.Forms.Button button_Close;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBox_WriteValue;
		private System.Windows.Forms.TextBox textBox_ReadValue;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox textBox_NameEvent;
		private System.Windows.Forms.Button button_SetEvent;
		public System.Windows.Forms.ListView listView_Event;
		private System.Windows.Forms.Button btn_VariableNames;
		public System.Windows.Forms.ListView listView_Variables;
		private System.Windows.Forms.Button button_ReceiveEvent;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		public System.Windows.Forms.ListView listView_EventData;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.Button button_writeReadValue;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ComboBox comboBox_Encoding;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label_count;
		internal System.Windows.Forms.Label label7;
		internal System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private Label label11;
		private Label label_VariableCount;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.CheckBox checkBox_WindowHandle;
		private System.Windows.Forms.Button button_IsSetEvent;
		private OMRON.Compolet.Variable.VariableCompolet variableCompolet1;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ColumnHeader Value;
        

        public Form1()
		{
			InitializeComponent();

			chk_Active.Checked = variableCompolet1.Active;
			variableCompolet1.PlcEncoding = System.Text.Encoding.GetEncoding(comboBox_Encoding.SelectedItem.ToString());
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(Form1));
			this.btn_VariableNames = new System.Windows.Forms.Button();
			this.listView_Variables = new System.Windows.Forms.ListView();
			this.VarName = new System.Windows.Forms.ColumnHeader();
			this.Value = new System.Windows.Forms.ColumnHeader();
			this.Type = new System.Windows.Forms.ColumnHeader();
			this.textBox_WriteValue = new System.Windows.Forms.TextBox();
			this.chk_Active = new System.Windows.Forms.CheckBox();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tab_Information = new System.Windows.Forms.TabPage();
			this.panel3 = new System.Windows.Forms.Panel();
			this.checkBox_WindowHandle = new System.Windows.Forms.CheckBox();
			this.comboBox_Encoding = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label_VariableCount = new System.Windows.Forms.Label();
			this.label_count = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.button_SetEvent = new System.Windows.Forms.Button();
			this.btn_ClearEvent = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.textBox_NameEvent = new System.Windows.Forms.TextBox();
			this.btn_ClearAllEvent = new System.Windows.Forms.Button();
			this.listView_Event = new System.Windows.Forms.ListView();
			this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
			this.button_ReceiveEvent = new System.Windows.Forms.Button();
			this.listView_EventData = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.button_IsSetEvent = new System.Windows.Forms.Button();
			this.textBox_ReadValue = new System.Windows.Forms.TextBox();
			this.button_ReadVariable = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.textBox_Name = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.panel2 = new System.Windows.Forms.Panel();
			this.label9 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.button_writeReadValue = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.button_WriteValue = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.button_Close = new System.Windows.Forms.Button();
			this.variableCompolet1 = new OMRON.Compolet.Variable.VariableCompolet(this.components);
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.tabControl1.SuspendLayout();
			this.tab_Information.SuspendLayout();
			this.panel3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// btn_VariableNames
			// 
			this.btn_VariableNames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_VariableNames.Location = new System.Drawing.Point(192, 384);
			this.btn_VariableNames.Name = "btn_VariableNames";
			this.btn_VariableNames.Size = new System.Drawing.Size(96, 24);
			this.btn_VariableNames.TabIndex = 0;
			this.btn_VariableNames.Text = "Variable Names";
			this.btn_VariableNames.Click += new System.EventHandler(this.btn_VariableNames_Click);
			// 
			// listView_Variables
			// 
			this.listView_Variables.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.listView_Variables.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																								 this.VarName,
																								 this.Type,
																								 this.Value});
			this.listView_Variables.FullRowSelect = true;
			this.listView_Variables.GridLines = true;
			this.listView_Variables.HideSelection = false;
			this.listView_Variables.Location = new System.Drawing.Point(12, 56);
			this.listView_Variables.Name = "listView_Variables";
			this.listView_Variables.Size = new System.Drawing.Size(280, 320);
			this.listView_Variables.TabIndex = 1;
			this.listView_Variables.View = System.Windows.Forms.View.Details;
			this.listView_Variables.SelectedIndexChanged += new System.EventHandler(this.listView_Variables_SelectedIndexChanged);
			// 
			// VarName
			// 
			this.VarName.Text = "Name";
			this.VarName.Width = 100;
			// 
			// Value
			// 
			this.Value.Text = "Value";
			this.Value.Width = 120;
			// 
			// Type
			// 
			this.Type.Text = "Type";
			this.Type.Width = 80;
			// 
			// textBox_WriteValue
			// 
			this.textBox_WriteValue.Location = new System.Drawing.Point(60, 104);
			this.textBox_WriteValue.Name = "textBox_WriteValue";
			this.textBox_WriteValue.Size = new System.Drawing.Size(228, 19);
			this.textBox_WriteValue.TabIndex = 2;
			this.textBox_WriteValue.Text = "0";
			// 
			// chk_Active
			// 
			this.chk_Active.Location = new System.Drawing.Point(12, 12);
			this.chk_Active.Name = "chk_Active";
			this.chk_Active.Size = new System.Drawing.Size(64, 20);
			this.chk_Active.TabIndex = 3;
			this.chk_Active.Text = "Active";
			this.chk_Active.CheckedChanged += new System.EventHandler(this.chk_Active_CheckedChanged);
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tab_Information);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(308, 446);
			this.tabControl1.TabIndex = 4;
			// 
			// tab_Information
			// 
			this.tab_Information.Controls.Add(this.panel3);
			this.tab_Information.Location = new System.Drawing.Point(4, 21);
			this.tab_Information.Name = "tab_Information";
			this.tab_Information.Size = new System.Drawing.Size(300, 421);
			this.tab_Information.TabIndex = 0;
			this.tab_Information.Text = "Information";
			// 
			// panel3
			// 
			this.panel3.Controls.Add(this.checkBox_WindowHandle);
			this.panel3.Controls.Add(this.btn_VariableNames);
			this.panel3.Controls.Add(this.listView_Variables);
			this.panel3.Controls.Add(this.chk_Active);
			this.panel3.Controls.Add(this.comboBox_Encoding);
			this.panel3.Controls.Add(this.label5);
			this.panel3.Controls.Add(this.label10);
			this.panel3.Controls.Add(this.label6);
			this.panel3.Controls.Add(this.label_VariableCount);
			this.panel3.Controls.Add(this.label_count);
			this.panel3.Controls.Add(this.label11);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(300, 421);
			this.panel3.TabIndex = 9;
			// 
			// checkBox_WindowHandle
			// 
			this.checkBox_WindowHandle.Location = new System.Drawing.Point(116, 32);
			this.checkBox_WindowHandle.Name = "checkBox_WindowHandle";
			this.checkBox_WindowHandle.TabIndex = 9;
			this.checkBox_WindowHandle.Text = "WindowHandle";
			this.checkBox_WindowHandle.CheckedChanged += new System.EventHandler(this.checkBox_WindowHandle_CheckedChanged);
			// 
			// comboBox_Encoding
			// 
			this.comboBox_Encoding.Items.AddRange(new object[] {
																   "utf-8",
																   "shift_jis",
																   "iso-2022-jp"});
			this.comboBox_Encoding.Location = new System.Drawing.Point(152, 12);
			this.comboBox_Encoding.Name = "comboBox_Encoding";
			this.comboBox_Encoding.Size = new System.Drawing.Size(121, 20);
			this.comboBox_Encoding.TabIndex = 7;
			this.comboBox_Encoding.Text = "utf-8";
			this.comboBox_Encoding.SelectedIndexChanged += new System.EventHandler(this.comboBox_Encoding_SelectedIndexChanged);
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(92, 16);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(56, 16);
			this.label5.TabIndex = 6;
			this.label5.Text = "Encoding:";
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(12, 40);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(100, 16);
			this.label10.TabIndex = 8;
			this.label10.Text = "Variable List";
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label6.Location = new System.Drawing.Point(20, 397);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(70, 16);
			this.label6.TabIndex = 2;
			this.label6.Text = "List Items :";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label_VariableCount
			// 
			this.label_VariableCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label_VariableCount.Location = new System.Drawing.Point(88, 381);
			this.label_VariableCount.Name = "label_VariableCount";
			this.label_VariableCount.Size = new System.Drawing.Size(56, 16);
			this.label_VariableCount.TabIndex = 2;
			this.label_VariableCount.Text = "0";
			this.label_VariableCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label_count
			// 
			this.label_count.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label_count.Location = new System.Drawing.Point(88, 397);
			this.label_count.Name = "label_count";
			this.label_count.Size = new System.Drawing.Size(56, 16);
			this.label_count.TabIndex = 2;
			this.label_count.Text = "0";
			this.label_count.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label11
			// 
			this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label11.Location = new System.Drawing.Point(20, 381);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(71, 16);
			this.label11.TabIndex = 2;
			this.label11.Text = "Variables :";
			this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// groupBox4
			// 
			this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox4.Controls.Add(this.label7);
			this.groupBox4.Controls.Add(this.label8);
			this.groupBox4.Controls.Add(this.button_SetEvent);
			this.groupBox4.Controls.Add(this.btn_ClearEvent);
			this.groupBox4.Controls.Add(this.label4);
			this.groupBox4.Controls.Add(this.textBox_NameEvent);
			this.groupBox4.Controls.Add(this.btn_ClearAllEvent);
			this.groupBox4.Controls.Add(this.listView_Event);
			this.groupBox4.Controls.Add(this.button_ReceiveEvent);
			this.groupBox4.Controls.Add(this.listView_EventData);
			this.groupBox4.Controls.Add(this.button_IsSetEvent);
			this.groupBox4.Location = new System.Drawing.Point(8, 160);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(404, 244);
			this.groupBox4.TabIndex = 7;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Event";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(12, 56);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(40, 28);
			this.label7.TabIndex = 10;
			this.label7.Text = "Event List:";
			// 
			// label8
			// 
			this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label8.Location = new System.Drawing.Point(12, 148);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(48, 32);
			this.label8.TabIndex = 9;
			this.label8.Text = "Event && value:";
			// 
			// button_SetEvent
			// 
			this.button_SetEvent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button_SetEvent.Location = new System.Drawing.Point(296, 52);
			this.button_SetEvent.Name = "button_SetEvent";
			this.button_SetEvent.Size = new System.Drawing.Size(96, 23);
			this.button_SetEvent.TabIndex = 0;
			this.button_SetEvent.Text = "Set Event";
			this.button_SetEvent.Click += new System.EventHandler(this.button_SetEvent_Click);
			// 
			// btn_ClearEvent
			// 
			this.btn_ClearEvent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_ClearEvent.Location = new System.Drawing.Point(296, 84);
			this.btn_ClearEvent.Name = "btn_ClearEvent";
			this.btn_ClearEvent.Size = new System.Drawing.Size(96, 23);
			this.btn_ClearEvent.TabIndex = 3;
			this.btn_ClearEvent.Text = "Clear Event";
			this.btn_ClearEvent.Click += new System.EventHandler(this.btn_ClearEvent_Click);
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(12, 24);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(40, 16);
			this.label4.TabIndex = 7;
			this.label4.Text = "Name:";
			// 
			// textBox_NameEvent
			// 
			this.textBox_NameEvent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox_NameEvent.Location = new System.Drawing.Point(60, 24);
			this.textBox_NameEvent.Name = "textBox_NameEvent";
			this.textBox_NameEvent.Size = new System.Drawing.Size(332, 19);
			this.textBox_NameEvent.TabIndex = 4;
			this.textBox_NameEvent.Text = "";
			// 
			// btn_ClearAllEvent
			// 
			this.btn_ClearAllEvent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_ClearAllEvent.Location = new System.Drawing.Point(296, 116);
			this.btn_ClearAllEvent.Name = "btn_ClearAllEvent";
			this.btn_ClearAllEvent.Size = new System.Drawing.Size(96, 23);
			this.btn_ClearAllEvent.TabIndex = 3;
			this.btn_ClearAllEvent.Text = "Clear All Event";
			this.btn_ClearAllEvent.Click += new System.EventHandler(this.btn_ClearAllEvent_Click);
			// 
			// listView_Event
			// 
			this.listView_Event.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.listView_Event.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																							 this.columnHeader3});
			this.listView_Event.Font = new System.Drawing.Font("MS UI Gothic", 9F);
			this.listView_Event.FullRowSelect = true;
			this.listView_Event.GridLines = true;
			this.listView_Event.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listView_Event.HideSelection = false;
			this.listView_Event.Location = new System.Drawing.Point(60, 52);
			this.listView_Event.Name = "listView_Event";
			this.listView_Event.Size = new System.Drawing.Size(228, 84);
			this.listView_Event.TabIndex = 1;
			this.listView_Event.View = System.Windows.Forms.View.Details;
			this.listView_Event.SelectedIndexChanged += new System.EventHandler(this.listView_Event_SelectedIndexChanged);
			// 
			// columnHeader3
			// 
			this.columnHeader3.Width = 228;
			// 
			// button_ReceiveEvent
			// 
			this.button_ReceiveEvent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button_ReceiveEvent.Location = new System.Drawing.Point(296, 148);
			this.button_ReceiveEvent.Name = "button_ReceiveEvent";
			this.button_ReceiveEvent.Size = new System.Drawing.Size(96, 23);
			this.button_ReceiveEvent.TabIndex = 3;
			this.button_ReceiveEvent.Text = "Receive Event";
			this.button_ReceiveEvent.Click += new System.EventHandler(this.button_ReceiveEvent_Click);
			// 
			// listView_EventData
			// 
			this.listView_EventData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.listView_EventData.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																								 this.columnHeader1,
																								 this.columnHeader2});
			this.listView_EventData.Font = new System.Drawing.Font("MS UI Gothic", 9F);
			this.listView_EventData.FullRowSelect = true;
			this.listView_EventData.GridLines = true;
			this.listView_EventData.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listView_EventData.HideSelection = false;
			this.listView_EventData.Location = new System.Drawing.Point(60, 144);
			this.listView_EventData.Name = "listView_EventData";
			this.listView_EventData.Size = new System.Drawing.Size(228, 84);
			this.listView_EventData.TabIndex = 1;
			this.listView_EventData.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 138;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Value";
			this.columnHeader2.Width = 520;
			// 
			// button_IsSetEvent
			// 
			this.button_IsSetEvent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button_IsSetEvent.Location = new System.Drawing.Point(296, 176);
			this.button_IsSetEvent.Name = "button_IsSetEvent";
			this.button_IsSetEvent.Size = new System.Drawing.Size(96, 23);
			this.button_IsSetEvent.TabIndex = 0;
			this.button_IsSetEvent.Text = "Is Set Event";
			this.button_IsSetEvent.Click += new System.EventHandler(this.button_IsSetEvent_Click);
			// 
			// textBox_ReadValue
			// 
			this.textBox_ReadValue.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox_ReadValue.Location = new System.Drawing.Point(60, 44);
			this.textBox_ReadValue.Multiline = true;
			this.textBox_ReadValue.Name = "textBox_ReadValue";
			this.textBox_ReadValue.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBox_ReadValue.Size = new System.Drawing.Size(228, 44);
			this.textBox_ReadValue.TabIndex = 5;
			this.textBox_ReadValue.Text = "";
			// 
			// button_ReadVariable
			// 
			this.button_ReadVariable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button_ReadVariable.Location = new System.Drawing.Point(296, 40);
			this.button_ReadVariable.Name = "button_ReadVariable";
			this.button_ReadVariable.Size = new System.Drawing.Size(96, 23);
			this.button_ReadVariable.TabIndex = 6;
			this.button_ReadVariable.Text = "Read Variable";
			this.button_ReadVariable.Click += new System.EventHandler(this.button_ReadVariable_Click);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(16, 20);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(40, 16);
			this.label3.TabIndex = 7;
			this.label3.Text = "Name:";
			// 
			// textBox_Name
			// 
			this.textBox_Name.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox_Name.Location = new System.Drawing.Point(60, 16);
			this.textBox_Name.Name = "textBox_Name";
			this.textBox_Name.Size = new System.Drawing.Size(332, 19);
			this.textBox_Name.TabIndex = 4;
			this.textBox_Name.Text = "";
			this.textBox_Name.TextChanged += new System.EventHandler(this.textBox_Name_TextChanged);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.tabControl1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(308, 446);
			this.panel1.TabIndex = 8;
			// 
			// splitter1
			// 
			this.splitter1.Location = new System.Drawing.Point(308, 0);
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(3, 446);
			this.splitter1.TabIndex = 9;
			this.splitter1.TabStop = false;
          

            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label9);
			this.panel2.Controls.Add(this.groupBox4);
			this.panel2.Controls.Add(this.groupBox2);
            // ★ Add Test SD button near Close
            this.button_TestSD = new System.Windows.Forms.Button();
            this.button_TestSD.Location = new System.Drawing.Point(210, 416);
            this.button_TestSD.Size = new System.Drawing.Size(96, 24);
            this.button_TestSD.Text = "Test SD";
            this.button_TestSD.UseVisualStyleBackColor = true;
            this.button_TestSD.Click += new System.EventHandler(this.button_TestSD_Click);

            this.panel2.Controls.Add(this.button_TestSD);
            this.panel2.Controls.Add(this.button_Close);

			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.DockPadding.All = 10;
			this.panel2.Location = new System.Drawing.Point(311, 0);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(421, 446);
			this.panel2.TabIndex = 10;
			// 
			// label9
			// 
			this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label9.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(0)), ((System.Byte)(0)), ((System.Byte)(192)));
			this.label9.Location = new System.Drawing.Point(152, 8);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(260, 12);
			this.label9.TabIndex = 12;
			this.label9.Text = "Example: Var1, Var2[2] Var3[2][3], Var4[2].mem1";
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.button_writeReadValue);
			this.groupBox2.Controls.Add(this.textBox_Name);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.button_ReadVariable);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.button_WriteValue);
			this.groupBox2.Controls.Add(this.textBox_ReadValue);
			this.groupBox2.Controls.Add(this.textBox_WriteValue);
			this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(8, 24);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(404, 132);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Variable";
           
          

            // 
            // button_writeReadValue
            // 
            this.button_writeReadValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button_writeReadValue.Enabled = false;
			this.button_writeReadValue.Location = new System.Drawing.Point(296, 72);
			this.button_writeReadValue.Name = "button_writeReadValue";
			this.button_writeReadValue.Size = new System.Drawing.Size(96, 23);
			this.button_writeReadValue.TabIndex = 8;
			this.button_writeReadValue.Text = "Write read value";
			this.button_writeReadValue.Click += new System.EventHandler(this.button_writeReadValue_Click);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(16, 48);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(40, 28);
			this.label1.TabIndex = 7;
			this.label1.Text = "Read Value:";
			// 
			// button_WriteValue
			// 
			this.button_WriteValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button_WriteValue.Location = new System.Drawing.Point(296, 104);
			this.button_WriteValue.Name = "button_WriteValue";
			this.button_WriteValue.Size = new System.Drawing.Size(96, 23);
			this.button_WriteValue.TabIndex = 6;
			this.button_WriteValue.Text = "Write Value";
			this.button_WriteValue.Click += new System.EventHandler(this.button_WriteValue_Click);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(16, 100);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(40, 28);
			this.label2.TabIndex = 7;
			this.label2.Text = "Write Value:";
			// 
			// button_Close
			// 
			this.button_Close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button_Close.Location = new System.Drawing.Point(312, 416);
			this.button_Close.Name = "button_Close";
			this.button_Close.Size = new System.Drawing.Size(96, 24);
			this.button_Close.TabIndex = 0;
			this.button_Close.Text = "Close"; 
            this.button_Close.Click += new System.EventHandler(this.button_Close_Click);
          
            // 
            // variableCompolet1
            // 
            this.variableCompolet1.PlcEncoding = ((System.Text.Encoding)(resources.GetObject("variableCompolet1.PlcEncoding")));
			this.variableCompolet1.Changed += new System.EventHandler(this.variableCompolet1_Changed);
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
			this.ClientSize = new System.Drawing.Size(732, 446);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.splitter1);
			this.Controls.Add(this.panel1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form1";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "VariableCompolet Sample(C#)";
			this.tabControl1.ResumeLayout(false);
			this.tab_Information.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.groupBox4.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main()
		{
			Application.Run(new Form1());
		}


		private void chk_Active_CheckedChanged(object sender, System.EventArgs e)
		{
			receiveCount = 0;
			try
			{
				this.variableCompolet1.Active = this.chk_Active.Checked;
				this.checkBox_WindowHandle.Checked = this.chk_Active.Checked;
				if (this.chk_Active.Checked)
				{
					UpdateVariableNames();
				}
				else
				{
					listView_Variables.Items.Clear();
					label_count.Text = "0";
					label_VariableCount.Text = "0";
				}
			}
			catch (Exception ex)
			{
				this.chk_Active.Checked = false;
				MessageBox.Show(ex.Message);
			}

		}


		private void btn_VariableNames_Click(object sender, System.EventArgs e)
		{
			UpdateVariableNames();
		}

		private void UpdateVariableNames()
		{
			try
			{
				this.listView_Variables.Items.Clear();

				string[] varNames = variableCompolet1.VariableNames;
				foreach (string varName in varNames)
				{
					VariableInfo vi = variableCompolet1.GetVariableInfo(varName);
					this.AddVariableToList(string.Empty, vi);
				}

				label_count.Text = listView_Variables.Items.Count.ToString();
				label_VariableCount.Text = varNames.Length.ToString();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void AddVariableToList(string parent, VariableInfo vi)
		{
			String type = vi.Type.ToString();
			if (vi.IsArray)
			{
				for (byte rank = 0; rank < vi.Dimension; rank++)
				{
					type += "[" + vi.NumberOfElements[rank] + "]";
				}
			}

			ListViewItem item = this.listView_Variables.Items.Add(vi.Name);
			item.SubItems.Add(type);
			item.SubItems.Add(string.Empty);
		}

		private string getValueString(object obj)
		{
			// Read written results
			string str = "";
			if (obj == null) return "";

			if (obj.GetType().IsArray)
			{
				foreach (object o in (Array)obj)
				{
					str += o.ToString() + ",";
				}
				str = str.TrimEnd(',');
			}
			else
			{
				str = obj.ToString();//string.Format("[0x{0:X16}]" ,obj);
			}

			return str;
		}

		//
		private void listView_Variables_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (this.listView_Variables.SelectedItems.Count > 0)
			{
				string name = this.listView_Variables.SelectedItems[0].Text;
				this.textBox_Name.Text = name;
				textBox_NameEvent.Text = name;
				VariableInfo varInfo = variableCompolet1.GetVariableInfo(name);
			}

		}

		private string ToStringVariableInfo(VariableInfo varInfo)
		{
			StringBuilder result = new StringBuilder();
			result.Append(varInfo.Name);
			result.Append("\r\nType is ");
			result.Append(varInfo.Type.ToString());
			return result.ToString();
		}

		// If get change notification for Variable, get changed TagName and EventID, using ReciveEvent.
		private void variableCompolet1_Changed(object sender, System.EventArgs e)
		{
			ReceiveEventEx();
		}

		private void button_Close_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}


		private object readvalue;
		private void button_ReadVariable_Click(object sender, System.EventArgs e)
		{
			try
			{
				object obj = this.variableCompolet1.ReadVariable(this.textBox_Name.Text);
				this.textBox_ReadValue.Text = string.Empty;
				this.textBox_ReadValue.AppendText(this.getValueString(obj));
				this.readvalue = obj;
				this.button_writeReadValue.Enabled = true;

				foreach (ListViewItem item in this.listView_Variables.SelectedItems)
				{
					obj = this.variableCompolet1.ReadVariable(item.Text);
					item.SubItems[2].Text = this.getValueString(obj);
				}
			}
			catch (Exception exp)
			{
				MessageBox.Show(exp.Message);
			}
		}

		private void button_WriteValue_Click(object sender, System.EventArgs e)
		{
			try
			{
				string varName = this.textBox_Name.Text;
				object val = null;
				if (this.textBox_WriteValue.Text.IndexOf(',') > -1)
				{
					val = this.textBox_WriteValue.Text.Split(',');
				}
				else
				{
					val = this.textBox_WriteValue.Text;
				}

				if (this.variableCompolet1.GetVariableInfo(varName).Type == VariableType.STRUCT)
				{
					// structure type's input must be byte[]
					String[] arr = val as String[];
					Byte[] bin = new Byte[arr.Length];
					for (int i = 0; i < arr.Length; i++)
					{
						bin[i] = Byte.Parse(arr[i]);
					}
					val = bin;
				}

				this.variableCompolet1.WriteVariable(varName, val);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void button_writeReadValue_Click(object sender, System.EventArgs e)
		{
			try
			{
				this.variableCompolet1.WriteVariable(textBox_Name.Text, readvalue);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void button_SetEvent_Click(object sender, System.EventArgs e)
		{
			try
			{
				variableCompolet1.SetEvent(this.textBox_NameEvent.Text, 1);
				listView_Event.Items.Add(this.textBox_NameEvent.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

		}

		private void btn_ClearEvent_Click(object sender, System.EventArgs e)
		{
			try
			{
				if (this.listView_Event.SelectedItems.Count <= 0)
				{
					return;
				}

				ListViewItem item = this.listView_Event.SelectedItems[0];
				variableCompolet1.ClearEvent(item.Text);
				listView_Event.Items.Remove(item);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void btn_ClearAllEvent_Click(object sender, System.EventArgs e)
		{
			try
			{
				variableCompolet1.ClearAllEvents();
				listView_Event.Items.Clear();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void listView_Event_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (this.listView_Event.SelectedItems.Count > 0)
			{
				// Variable name
				this.textBox_NameEvent.Text = this.listView_Event.SelectedItems[0].Text;
			}
		}

		private UInt32 receiveCount = 0;
		void ReceiveEvent()
		{
			string variableName;
			int eventID;

			try
			{
				this.variableCompolet1.ReciveEvent(out variableName, out eventID, 1000);
				if (variableName != null && variableName != "")
				{
					receiveCount++;
					this.listView_EventData.Items.Clear();
					string[] values = new string[2];
					values[0] = "Variable name";
					values[1] = variableName;
					ListViewItem item = new ListViewItem(values);
					listView_EventData.Items.Add(item);

					values[0] = "Event ID";
					values[1] = eventID.ToString();
					item = new ListViewItem(values);
					listView_EventData.Items.Add(item);

					values[0] = "Value";
					Object obj = variableCompolet1.ReadVariable(variableName);
					values[1] = getValueString(obj);
					item = new ListViewItem(values);
					listView_EventData.Items.Add(item);

					values[0] = "Count";
					values[1] = receiveCount.ToString();
					item = new ListViewItem(values);
					listView_EventData.Items.Add(item);
				}
			}
			catch (Exception exp)
			{
				MessageBox.Show(exp.Message);
			}

		}
		void ReceiveEventEx()
		{
			string variableName;
			int eventID;
			try
			{
				while (true)
				{
					this.variableCompolet1.ReciveEvent(out variableName, out eventID, 0);
					if (variableName == null || variableName == "")
					{
						return;
					}
					else
					{
						receiveCount++;
						this.listView_EventData.Items.Clear();
						string[] values = new string[2];
						values[0] = "Variable name";
						values[1] = variableName;
						ListViewItem item = new ListViewItem(values);
						listView_EventData.Items.Add(item);

						values[0] = "Event ID";
						values[1] = eventID.ToString();
						item = new ListViewItem(values);
						listView_EventData.Items.Add(item);

						values[0] = "Value";
						Object obj = variableCompolet1.ReadVariable(variableName);
						values[1] = getValueString(obj);
						item = new ListViewItem(values);
						listView_EventData.Items.Add(item);

						values[0] = "Count";
						values[1] = receiveCount.ToString();
						item = new ListViewItem(values);
						listView_EventData.Items.Add(item);
					}
				}

			}
			//catch (OMRON.FinsGateway.EventMemory.EventMemoryException evtexp)
			//{
			//	if (evtexp.ErrorCode != Convert.ToInt32(0x2000000b))
			//	{
			//		MessageBox.Show(evtexp.Message);
			//	}
			//	return;
			//}

			catch (Exception exp)
			{
				MessageBox.Show(exp.Message);
				return;
			}

		}
		private void button_ReceiveEvent_Click(object sender, System.EventArgs e)
		{
			ReceiveEvent();
		}

		private void textBox_Name_TextChanged(object sender, System.EventArgs e)
		{
			button_writeReadValue.Enabled = false;
		}

		private void comboBox_Encoding_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			variableCompolet1.PlcEncoding = System.Text.Encoding.GetEncoding(comboBox_Encoding.SelectedItem.ToString());
		}

		private void checkBox_WindowHandle_CheckedChanged(object sender, System.EventArgs e)
		{
			if (checkBox_WindowHandle.Checked)
			{
				variableCompolet1.WindowHandle = this.Handle;
			}
			else
			{
				variableCompolet1.WindowHandle = IntPtr.Zero;
			}
		}

		private void button_IsSetEvent_Click(object sender, System.EventArgs e)
		{
			try
			{
				string name = this.textBox_NameEvent.Text;
				bool ret = variableCompolet1.IsSetEvent(name);
				MessageBox.Show("IsSetEvent = " + ret.ToString(), "Variable " + name);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
        private void button_TestSD_Click(object sender, EventArgs e)
        {
            try
            {
                string tag = "SD_EQToCIM_Data01_03_01_00";

                ushort[] mock = BuildMock_SD_EQToCIM_Data01_WORD();

                // 写入
                variableCompolet1.WriteVariable(tag, mock);

                // 读取（WORD 会被转换成 Int32[]）
                object obj = variableCompolet1.ReadVariable(tag);
                int[] readback = (int[])obj;

                MessageBox.Show($"PLC 返回长度 = {readback.Length}");

                textBox_ReadValue.Text = "";
                int safeCount = Math.Min(20, readback.Length);
                for (int i = 0; i < safeCount; i++)
                {
                    textBox_ReadValue.AppendText($"W[{i}] = {readback[i]}\r\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("WriteVariable 出错：\r\n" + ex.Message);
            }
        }

        private ushort[] BuildMock_SD_EQToCIM_Data01_WORD()
        {
            ushort[] w = new ushort[331];

            w[0] = 0b0001_1111;
            w[1] = 0;

            for (int i = 0; i < 41; i++) w[3 + i] = (ushort)(100 + i);

            for (int i = 0; i < 15; i++) w[44 + i] = (ushort)(200 + i);

            for (int i = 0; i < 6; i++) w[59 + i] = (ushort)(300 + i);

            w[65] = 1;

            EncodeASCII_WORD("OPERATOR01", w, 66, 10);
            w[76] = 1;
            w[77] = 0;

            for (int i = 0; i < 35; i++) w[98 + i] = (ushort)(400 + i);

            for (int i = 0; i < 40; i++) w[133 + i] = (ushort)(500 + i);

            EncodeASCII_WORD("JOBTEST001", w, 173, 20);
            w[193] = 3;
            w[194] = 5;
            w[195] = 1;
            w[196] = 10;
            EncodeASCII_WORD("OK", w, 197, 1);
            EncodeASCII_WORD("A", w, 198, 1);
            w[199] = 7;

            for (int i = 0; i < 45; i++) w[203 + i] = (ushort)(600 + i);

            EncodeASCII_WORD("REQ0001", w, 248, 20);

            w[276] = w[277] = w[278] = w[279] = 0;
            w[280] = 5;

            for (int i = 0; i < 50; i++)
                w[281 + i] = (ushort)(700 + i);

            return w;
        }

        private void EncodeASCII_WORD(string text, ushort[] w, int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (i < text.Length)
                    w[start + i] = (ushort)text[i];  // ASCII 字符
                else
                    w[start + i] = 0;
            }
        }


        private void EncodeASCII(string text, short[] w, int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (i < text.Length)
                    w[start + i] = (short)text[i];
                else
                    w[start + i] = 0;
            }
        }
    }
}
