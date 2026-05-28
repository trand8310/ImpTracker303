namespace MainClient
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.taskInfoListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBox_DevApiUrl = new System.Windows.Forms.TextBox();
            this.label24 = new System.Windows.Forms.Label();
            this.checkBox_CheckIp = new System.Windows.Forms.CheckBox();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.checkBox_UsingIOSMAC = new System.Windows.Forms.CheckBox();
            this.checkBox_UsingIOSIMEI = new System.Windows.Forms.CheckBox();
            this.checkBox_UsingSystemDevs = new System.Windows.Forms.CheckBox();
            this.checkBox_IPAreaCheck = new System.Windows.Forms.CheckBox();
            this.checkBox_NoneOS = new System.Windows.Forms.CheckBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.numericUpDown_SubResetInterval = new System.Windows.Forms.NumericUpDown();
            this.label17 = new System.Windows.Forms.Label();
            this.numericUpDown_MainResetInterval = new System.Windows.Forms.NumericUpDown();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.checkBox_RealIp = new System.Windows.Forms.CheckBox();
            this.textBox_UpdateApiUrl = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.numericUpDown_Multiple = new System.Windows.Forms.NumericUpDown();
            this.checkBox_NoProxy = new System.Windows.Forms.CheckBox();
            this.checkBox_ShowWeb = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox_TaskApiUrl = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.numericUpDown_GetTaskInterval = new System.Windows.Forms.NumericUpDown();
            this.buttonStart = new System.Windows.Forms.Button();
            this.label12 = new System.Windows.Forms.Label();
            this.numericUpDown_MaximumParallel = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.numericUpDown_UVInterval = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.numericUpDown_MaximumLimitedConcurrency = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_TaskIdentify = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox_AllIpApiUrl = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label23 = new System.Windows.Forms.Label();
            this.textBox_SmsPhone = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.numericUpDown_SendSmsTimeout = new System.Windows.Forms.NumericUpDown();
            this.label21 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.textBox_SmsName = new System.Windows.Forms.TextBox();
            this.checkBox_SendSms = new System.Windows.Forms.CheckBox();
            this.LogTextBox = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.LogDetailTextBox = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkBox_DisableImage = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_SubResetInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MainResetInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Multiple)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_GetTaskInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaximumParallel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_UVInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaximumLimitedConcurrency)).BeginInit();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_SendSmsTimeout)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.taskInfoListView);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(0, 302);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.groupBox1.Size = new System.Drawing.Size(452, 379);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "任务列表";
            // 
            // taskInfoListView
            // 
            this.taskInfoListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.taskInfoListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.taskInfoListView.FullRowSelect = true;
            this.taskInfoListView.GridLines = true;
            this.taskInfoListView.HideSelection = false;
            this.taskInfoListView.Location = new System.Drawing.Point(4, 20);
            this.taskInfoListView.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.taskInfoListView.Name = "taskInfoListView";
            this.taskInfoListView.Size = new System.Drawing.Size(444, 357);
            this.taskInfoListView.TabIndex = 0;
            this.taskInfoListView.UseCompatibleStateImageBehavior = false;
            this.taskInfoListView.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "任务名称";
            this.columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "IP地址";
            this.columnHeader2.Width = 100;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "真实IP";
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "延迟";
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "归属地";
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "状态";
            this.columnHeader6.Width = 120;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkBox_DisableImage);
            this.groupBox2.Controls.Add(this.textBox_DevApiUrl);
            this.groupBox2.Controls.Add(this.label24);
            this.groupBox2.Controls.Add(this.checkBox_CheckIp);
            this.groupBox2.Controls.Add(this.linkLabel2);
            this.groupBox2.Controls.Add(this.linkLabel1);
            this.groupBox2.Controls.Add(this.checkBox_UsingIOSMAC);
            this.groupBox2.Controls.Add(this.checkBox_UsingIOSIMEI);
            this.groupBox2.Controls.Add(this.checkBox_UsingSystemDevs);
            this.groupBox2.Controls.Add(this.checkBox_IPAreaCheck);
            this.groupBox2.Controls.Add(this.checkBox_NoneOS);
            this.groupBox2.Controls.Add(this.label19);
            this.groupBox2.Controls.Add(this.label18);
            this.groupBox2.Controls.Add(this.numericUpDown_SubResetInterval);
            this.groupBox2.Controls.Add(this.label17);
            this.groupBox2.Controls.Add(this.numericUpDown_MainResetInterval);
            this.groupBox2.Controls.Add(this.label16);
            this.groupBox2.Controls.Add(this.label15);
            this.groupBox2.Controls.Add(this.checkBox_RealIp);
            this.groupBox2.Controls.Add(this.textBox_UpdateApiUrl);
            this.groupBox2.Controls.Add(this.label14);
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.numericUpDown_Multiple);
            this.groupBox2.Controls.Add(this.checkBox_NoProxy);
            this.groupBox2.Controls.Add(this.checkBox_ShowWeb);
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.textBox_TaskApiUrl);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label13);
            this.groupBox2.Controls.Add(this.numericUpDown_GetTaskInterval);
            this.groupBox2.Controls.Add(this.buttonStart);
            this.groupBox2.Controls.Add(this.label12);
            this.groupBox2.Controls.Add(this.numericUpDown_MaximumParallel);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.numericUpDown_UVInterval);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.numericUpDown_MaximumLimitedConcurrency);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.textBox_TaskIdentify);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.textBox_AllIpApiUrl);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.groupBox5);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.groupBox2.Size = new System.Drawing.Size(1091, 302);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "设置";
            // 
            // textBox_DevApiUrl
            // 
            this.textBox_DevApiUrl.Location = new System.Drawing.Point(92, 118);
            this.textBox_DevApiUrl.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.textBox_DevApiUrl.Name = "textBox_DevApiUrl";
            this.textBox_DevApiUrl.Size = new System.Drawing.Size(439, 25);
            this.textBox_DevApiUrl.TabIndex = 75;
            this.textBox_DevApiUrl.Text = "http://117.21.200.18:9000/api/getdev.php";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Location = new System.Drawing.Point(20, 125);
            this.label24.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(75, 15);
            this.label24.TabIndex = 74;
            this.label24.Text = "设备接口:";
            // 
            // checkBox_CheckIp
            // 
            this.checkBox_CheckIp.AutoSize = true;
            this.checkBox_CheckIp.Location = new System.Drawing.Point(816, 40);
            this.checkBox_CheckIp.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_CheckIp.Name = "checkBox_CheckIp";
            this.checkBox_CheckIp.Size = new System.Drawing.Size(120, 19);
            this.checkBox_CheckIp.TabIndex = 73;
            this.checkBox_CheckIp.Text = "检测IP有效性";
            this.checkBox_CheckIp.UseVisualStyleBackColor = true;
            // 
            // linkLabel2
            // 
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.Location = new System.Drawing.Point(736, 266);
            this.linkLabel2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(67, 15);
            this.linkLabel2.TabIndex = 72;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "应用目录";
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(736, 234);
            this.linkLabel1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(67, 15);
            this.linkLabel1.TabIndex = 71;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "开机启动";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // checkBox_UsingIOSMAC
            // 
            this.checkBox_UsingIOSMAC.AutoSize = true;
            this.checkBox_UsingIOSMAC.Location = new System.Drawing.Point(604, 225);
            this.checkBox_UsingIOSMAC.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_UsingIOSMAC.Name = "checkBox_UsingIOSMAC";
            this.checkBox_UsingIOSMAC.Size = new System.Drawing.Size(107, 19);
            this.checkBox_UsingIOSMAC.TabIndex = 52;
            this.checkBox_UsingIOSMAC.Text = "IOS使用MAC";
            this.checkBox_UsingIOSMAC.UseVisualStyleBackColor = true;
            // 
            // checkBox_UsingIOSIMEI
            // 
            this.checkBox_UsingIOSIMEI.AutoSize = true;
            this.checkBox_UsingIOSIMEI.Location = new System.Drawing.Point(604, 201);
            this.checkBox_UsingIOSIMEI.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_UsingIOSIMEI.Name = "checkBox_UsingIOSIMEI";
            this.checkBox_UsingIOSIMEI.Size = new System.Drawing.Size(115, 19);
            this.checkBox_UsingIOSIMEI.TabIndex = 51;
            this.checkBox_UsingIOSIMEI.Text = "IOS使用IMEI";
            this.checkBox_UsingIOSIMEI.UseVisualStyleBackColor = true;
            // 
            // checkBox_UsingSystemDevs
            // 
            this.checkBox_UsingSystemDevs.AutoSize = true;
            this.checkBox_UsingSystemDevs.Location = new System.Drawing.Point(604, 177);
            this.checkBox_UsingSystemDevs.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_UsingSystemDevs.Name = "checkBox_UsingSystemDevs";
            this.checkBox_UsingSystemDevs.Size = new System.Drawing.Size(149, 19);
            this.checkBox_UsingSystemDevs.TabIndex = 50;
            this.checkBox_UsingSystemDevs.Text = "使用系统设备信息";
            this.checkBox_UsingSystemDevs.UseVisualStyleBackColor = true;
            // 
            // checkBox_IPAreaCheck
            // 
            this.checkBox_IPAreaCheck.AutoSize = true;
            this.checkBox_IPAreaCheck.Location = new System.Drawing.Point(959, 18);
            this.checkBox_IPAreaCheck.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_IPAreaCheck.Name = "checkBox_IPAreaCheck";
            this.checkBox_IPAreaCheck.Size = new System.Drawing.Size(105, 19);
            this.checkBox_IPAreaCheck.TabIndex = 47;
            this.checkBox_IPAreaCheck.Text = "IP地区校验";
            this.checkBox_IPAreaCheck.UseVisualStyleBackColor = true;
            // 
            // checkBox_NoneOS
            // 
            this.checkBox_NoneOS.AutoSize = true;
            this.checkBox_NoneOS.Location = new System.Drawing.Point(959, 39);
            this.checkBox_NoneOS.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_NoneOS.Name = "checkBox_NoneOS";
            this.checkBox_NoneOS.Size = new System.Drawing.Size(90, 19);
            this.checkBox_NoneOS.TabIndex = 45;
            this.checkBox_NoneOS.Text = "不回传OS";
            this.checkBox_NoneOS.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(504, 247);
            this.label19.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(83, 15);
            this.label19.TabIndex = 42;
            this.label19.Text = "分钟±30秒";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(504, 216);
            this.label18.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(83, 15);
            this.label18.TabIndex = 41;
            this.label18.Text = "分钟±30秒";
            // 
            // numericUpDown_SubResetInterval
            // 
            this.numericUpDown_SubResetInterval.Location = new System.Drawing.Point(420, 241);
            this.numericUpDown_SubResetInterval.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.numericUpDown_SubResetInterval.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown_SubResetInterval.Name = "numericUpDown_SubResetInterval";
            this.numericUpDown_SubResetInterval.Size = new System.Drawing.Size(77, 25);
            this.numericUpDown_SubResetInterval.TabIndex = 40;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(289, 247);
            this.label17.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(120, 15);
            this.label17.TabIndex = 39;
            this.label17.Text = "子进程重置间隔:";
            // 
            // numericUpDown_MainResetInterval
            // 
            this.numericUpDown_MainResetInterval.Location = new System.Drawing.Point(420, 210);
            this.numericUpDown_MainResetInterval.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.numericUpDown_MainResetInterval.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown_MainResetInterval.Name = "numericUpDown_MainResetInterval";
            this.numericUpDown_MainResetInterval.Size = new System.Drawing.Size(77, 25);
            this.numericUpDown_MainResetInterval.TabIndex = 38;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(289, 216);
            this.label16.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(120, 15);
            this.label16.TabIndex = 37;
            this.label16.Text = "主进程重置间隔:";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(677, 89);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(83, 15);
            this.label15.TabIndex = 36;
            this.label15.Text = "进程数量:0";
            // 
            // checkBox_RealIp
            // 
            this.checkBox_RealIp.AutoSize = true;
            this.checkBox_RealIp.Location = new System.Drawing.Point(816, 18);
            this.checkBox_RealIp.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_RealIp.Name = "checkBox_RealIp";
            this.checkBox_RealIp.Size = new System.Drawing.Size(75, 19);
            this.checkBox_RealIp.TabIndex = 35;
            this.checkBox_RealIp.Text = "真实IP";
            this.checkBox_RealIp.UseVisualStyleBackColor = true;
            // 
            // textBox_UpdateApiUrl
            // 
            this.textBox_UpdateApiUrl.Location = new System.Drawing.Point(91, 22);
            this.textBox_UpdateApiUrl.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.textBox_UpdateApiUrl.Name = "textBox_UpdateApiUrl";
            this.textBox_UpdateApiUrl.Size = new System.Drawing.Size(439, 25);
            this.textBox_UpdateApiUrl.TabIndex = 34;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(19, 29);
            this.label14.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(69, 15);
            this.label14.TabIndex = 33;
            this.label14.Text = "更新API:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(337, 185);
            this.label11.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(75, 15);
            this.label11.TabIndex = 31;
            this.label11.Text = "任务倍速:";
            // 
            // numericUpDown_Multiple
            // 
            this.numericUpDown_Multiple.Location = new System.Drawing.Point(420, 179);
            this.numericUpDown_Multiple.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.numericUpDown_Multiple.Name = "numericUpDown_Multiple";
            this.numericUpDown_Multiple.Size = new System.Drawing.Size(77, 25);
            this.numericUpDown_Multiple.TabIndex = 32;
            this.numericUpDown_Multiple.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // checkBox_NoProxy
            // 
            this.checkBox_NoProxy.AutoSize = true;
            this.checkBox_NoProxy.Location = new System.Drawing.Point(959, 61);
            this.checkBox_NoProxy.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_NoProxy.Name = "checkBox_NoProxy";
            this.checkBox_NoProxy.Size = new System.Drawing.Size(104, 19);
            this.checkBox_NoProxy.TabIndex = 28;
            this.checkBox_NoProxy.Text = "不使用代理";
            this.checkBox_NoProxy.UseVisualStyleBackColor = true;
            // 
            // checkBox_ShowWeb
            // 
            this.checkBox_ShowWeb.AutoSize = true;
            this.checkBox_ShowWeb.Location = new System.Drawing.Point(816, 81);
            this.checkBox_ShowWeb.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_ShowWeb.Name = "checkBox_ShowWeb";
            this.checkBox_ShowWeb.Size = new System.Drawing.Size(89, 19);
            this.checkBox_ShowWeb.TabIndex = 27;
            this.checkBox_ShowWeb.Text = "查看效果";
            this.checkBox_ShowWeb.UseVisualStyleBackColor = true;
            this.checkBox_ShowWeb.Click += new System.EventHandler(this.checkBox_ShowWeb_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(539, 92);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(124, 46);
            this.button1.TabIndex = 22;
            this.button1.Text = "清除";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox_TaskApiUrl
            // 
            this.textBox_TaskApiUrl.Location = new System.Drawing.Point(91, 85);
            this.textBox_TaskApiUrl.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.textBox_TaskApiUrl.Name = "textBox_TaskApiUrl";
            this.textBox_TaskApiUrl.Size = new System.Drawing.Size(439, 25);
            this.textBox_TaskApiUrl.TabIndex = 21;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(19, 92);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(69, 15);
            this.label10.TabIndex = 20;
            this.label10.Text = "任务API:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(677, 66);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(83, 15);
            this.label7.TabIndex = 19;
            this.label7.Text = "运行时间:0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(677, 44);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(83, 15);
            this.label5.TabIndex = 16;
            this.label5.Text = "提交数量:0";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(237, 185);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(37, 15);
            this.label13.TabIndex = 15;
            this.label13.Text = "毫秒";
            // 
            // numericUpDown_GetTaskInterval
            // 
            this.numericUpDown_GetTaskInterval.Location = new System.Drawing.Point(155, 147);
            this.numericUpDown_GetTaskInterval.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.numericUpDown_GetTaskInterval.Maximum = new decimal(new int[] {
            30000,
            0,
            0,
            0});
            this.numericUpDown_GetTaskInterval.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown_GetTaskInterval.Name = "numericUpDown_GetTaskInterval";
            this.numericUpDown_GetTaskInterval.Size = new System.Drawing.Size(77, 25);
            this.numericUpDown_GetTaskInterval.TabIndex = 14;
            this.numericUpDown_GetTaskInterval.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(539, 22);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(124, 65);
            this.buttonStart.TabIndex = 13;
            this.buttonStart.Text = "开始";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(41, 216);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(105, 15);
            this.label12.TabIndex = 9;
            this.label12.Text = "任务进程数量:";
            // 
            // numericUpDown_MaximumParallel
            // 
            this.numericUpDown_MaximumParallel.Location = new System.Drawing.Point(155, 210);
            this.numericUpDown_MaximumParallel.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.numericUpDown_MaximumParallel.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_MaximumParallel.Name = "numericUpDown_MaximumParallel";
            this.numericUpDown_MaximumParallel.Size = new System.Drawing.Size(77, 25);
            this.numericUpDown_MaximumParallel.TabIndex = 10;
            this.numericUpDown_MaximumParallel.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(73, 185);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(76, 15);
            this.label9.TabIndex = 0;
            this.label9.Text = "单UV间隔:";
            // 
            // numericUpDown_UVInterval
            // 
            this.numericUpDown_UVInterval.Location = new System.Drawing.Point(155, 179);
            this.numericUpDown_UVInterval.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.numericUpDown_UVInterval.Maximum = new decimal(new int[] {
            30000,
            0,
            0,
            0});
            this.numericUpDown_UVInterval.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown_UVInterval.Name = "numericUpDown_UVInterval";
            this.numericUpDown_UVInterval.Size = new System.Drawing.Size(77, 25);
            this.numericUpDown_UVInterval.TabIndex = 3;
            this.numericUpDown_UVInterval.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(41, 247);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(105, 15);
            this.label8.TabIndex = 0;
            this.label8.Text = "工作线程数量:";
            // 
            // numericUpDown_MaximumLimitedConcurrency
            // 
            this.numericUpDown_MaximumLimitedConcurrency.Location = new System.Drawing.Point(155, 241);
            this.numericUpDown_MaximumLimitedConcurrency.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.numericUpDown_MaximumLimitedConcurrency.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_MaximumLimitedConcurrency.Name = "numericUpDown_MaximumLimitedConcurrency";
            this.numericUpDown_MaximumLimitedConcurrency.Size = new System.Drawing.Size(77, 25);
            this.numericUpDown_MaximumLimitedConcurrency.TabIndex = 3;
            this.numericUpDown_MaximumLimitedConcurrency.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(677, 22);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(55, 15);
            this.label6.TabIndex = 4;
            this.label6.Text = "label6";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(305, 153);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(105, 15);
            this.label4.TabIndex = 0;
            this.label4.Text = "独立任务标识:";
            // 
            // textBox_TaskIdentify
            // 
            this.textBox_TaskIdentify.Location = new System.Drawing.Point(420, 147);
            this.textBox_TaskIdentify.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.textBox_TaskIdentify.Name = "textBox_TaskIdentify";
            this.textBox_TaskIdentify.Size = new System.Drawing.Size(76, 25);
            this.textBox_TaskIdentify.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(240, 152);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 15);
            this.label3.TabIndex = 0;
            this.label3.Text = "毫秒";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(41, 154);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 15);
            this.label2.TabIndex = 0;
            this.label2.Text = "获取任务间隔:";
            // 
            // textBox_AllIpApiUrl
            // 
            this.textBox_AllIpApiUrl.Location = new System.Drawing.Point(91, 54);
            this.textBox_AllIpApiUrl.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.textBox_AllIpApiUrl.Name = "textBox_AllIpApiUrl";
            this.textBox_AllIpApiUrl.Size = new System.Drawing.Size(439, 25);
            this.textBox_AllIpApiUrl.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(19, 60);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "代理API:";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.button2);
            this.groupBox5.Controls.Add(this.label23);
            this.groupBox5.Controls.Add(this.textBox_SmsPhone);
            this.groupBox5.Controls.Add(this.label22);
            this.groupBox5.Controls.Add(this.numericUpDown_SendSmsTimeout);
            this.groupBox5.Controls.Add(this.label21);
            this.groupBox5.Controls.Add(this.label20);
            this.groupBox5.Controls.Add(this.textBox_SmsName);
            this.groupBox5.Controls.Add(this.checkBox_SendSms);
            this.groupBox5.Location = new System.Drawing.Point(808, 109);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox5.Size = new System.Drawing.Size(269, 119);
            this.groupBox5.TabIndex = 44;
            this.groupBox5.TabStop = false;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(208, 22);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(52, 28);
            this.button2.TabIndex = 51;
            this.button2.Text = "测试";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label23
            // 
            this.label23.AutoSize = true;
            this.label23.Location = new System.Drawing.Point(12, 59);
            this.label23.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label23.Name = "label23";
            this.label23.Size = new System.Drawing.Size(45, 15);
            this.label23.TabIndex = 50;
            this.label23.Text = "电话:";
            // 
            // textBox_SmsPhone
            // 
            this.textBox_SmsPhone.Location = new System.Drawing.Point(63, 52);
            this.textBox_SmsPhone.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_SmsPhone.Name = "textBox_SmsPhone";
            this.textBox_SmsPhone.Size = new System.Drawing.Size(196, 25);
            this.textBox_SmsPhone.TabIndex = 49;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(127, 89);
            this.label22.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(105, 15);
            this.label22.TabIndex = 48;
            this.label22.Text = "分钟,发送短信";
            // 
            // numericUpDown_SendSmsTimeout
            // 
            this.numericUpDown_SendSmsTimeout.Location = new System.Drawing.Point(63, 82);
            this.numericUpDown_SendSmsTimeout.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.numericUpDown_SendSmsTimeout.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numericUpDown_SendSmsTimeout.Name = "numericUpDown_SendSmsTimeout";
            this.numericUpDown_SendSmsTimeout.Size = new System.Drawing.Size(60, 25);
            this.numericUpDown_SendSmsTimeout.TabIndex = 47;
            this.numericUpDown_SendSmsTimeout.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(12, 89);
            this.label21.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(45, 15);
            this.label21.TabIndex = 46;
            this.label21.Text = "超时:";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(12, 29);
            this.label20.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(45, 15);
            this.label20.TabIndex = 44;
            this.label20.Text = "名称:";
            // 
            // textBox_SmsName
            // 
            this.textBox_SmsName.Location = new System.Drawing.Point(63, 22);
            this.textBox_SmsName.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBox_SmsName.Name = "textBox_SmsName";
            this.textBox_SmsName.Size = new System.Drawing.Size(136, 25);
            this.textBox_SmsName.TabIndex = 43;
            // 
            // checkBox_SendSms
            // 
            this.checkBox_SendSms.AutoSize = true;
            this.checkBox_SendSms.Location = new System.Drawing.Point(8, -1);
            this.checkBox_SendSms.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.checkBox_SendSms.Name = "checkBox_SendSms";
            this.checkBox_SendSms.Size = new System.Drawing.Size(89, 19);
            this.checkBox_SendSms.TabIndex = 45;
            this.checkBox_SendSms.Text = "短信服务";
            this.checkBox_SendSms.UseVisualStyleBackColor = true;
            // 
            // LogTextBox
            // 
            this.LogTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogTextBox.Location = new System.Drawing.Point(4, 20);
            this.LogTextBox.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.LogTextBox.Multiline = true;
            this.LogTextBox.Name = "LogTextBox";
            this.LogTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.LogTextBox.Size = new System.Drawing.Size(631, 357);
            this.LogTextBox.TabIndex = 3;
            this.LogTextBox.WordWrap = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.LogTextBox);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(452, 302);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.groupBox3.Size = new System.Drawing.Size(639, 379);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "日志";
            // 
            // LogDetailTextBox
            // 
            this.LogDetailTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogDetailTextBox.Location = new System.Drawing.Point(4, 20);
            this.LogDetailTextBox.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.LogDetailTextBox.Multiline = true;
            this.LogDetailTextBox.Name = "LogDetailTextBox";
            this.LogDetailTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.LogDetailTextBox.Size = new System.Drawing.Size(1083, 67);
            this.LogDetailTextBox.TabIndex = 3;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.LogDetailTextBox);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBox4.Location = new System.Drawing.Point(0, 681);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.groupBox4.Size = new System.Drawing.Size(1091, 89);
            this.groupBox4.TabIndex = 4;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "详细日志";
            // 
            // checkBox_DisableImage
            // 
            this.checkBox_DisableImage.AutoSize = true;
            this.checkBox_DisableImage.Location = new System.Drawing.Point(604, 252);
            this.checkBox_DisableImage.Margin = new System.Windows.Forms.Padding(4);
            this.checkBox_DisableImage.Name = "checkBox_DisableImage";
            this.checkBox_DisableImage.Size = new System.Drawing.Size(89, 19);
            this.checkBox_DisableImage.TabIndex = 76;
            this.checkBox_DisableImage.Text = "禁止图片";
            this.checkBox_DisableImage.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1091, 770);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox4);
            this.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "优化测试-曝光";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_SubResetInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MainResetInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Multiple)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_GetTaskInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaximumParallel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_UVInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_MaximumLimitedConcurrency)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_SendSmsTimeout)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListView taskInfoListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBox_AllIpApiUrl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox LogTextBox;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_TaskIdentify;
        private System.Windows.Forms.TextBox LogDetailTextBox;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numericUpDown_MaximumLimitedConcurrency;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numericUpDown_UVInterval;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.NumericUpDown numericUpDown_MaximumParallel;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.NumericUpDown numericUpDown_GetTaskInterval;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_TaskApiUrl;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox_NoProxy;
        private System.Windows.Forms.CheckBox checkBox_ShowWeb;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown numericUpDown_Multiple;
        private System.Windows.Forms.TextBox textBox_UpdateApiUrl;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox checkBox_RealIp;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.NumericUpDown numericUpDown_SubResetInterval;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.NumericUpDown numericUpDown_MainResetInterval;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox textBox_SmsName;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.NumericUpDown numericUpDown_SendSmsTimeout;
        private System.Windows.Forms.CheckBox checkBox_SendSms;
        private System.Windows.Forms.TextBox textBox_SmsPhone;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBox_NoneOS;
        private System.Windows.Forms.CheckBox checkBox_IPAreaCheck;
        private System.Windows.Forms.CheckBox checkBox_UsingSystemDevs;
        private System.Windows.Forms.CheckBox checkBox_UsingIOSIMEI;
        private System.Windows.Forms.CheckBox checkBox_UsingIOSMAC;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.CheckBox checkBox_CheckIp;
        private System.Windows.Forms.TextBox textBox_DevApiUrl;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.CheckBox checkBox_DisableImage;
    }
}

