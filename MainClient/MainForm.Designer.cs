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
            groupBox1 = new GroupBox();
            taskInfoListView = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();
            columnHeader6 = new ColumnHeader();
            groupBox2 = new GroupBox();
            label12 = new Label();
            numericUpDown_ChannelCapacity = new NumericUpDown();
            label25 = new Label();
            label27 = new Label();
            numericUpDown_IpValidityDuration = new NumericUpDown();
            checkBox_IsHiddenMode = new CheckBox();
            checkBox_DisableImage = new CheckBox();
            textBox_DevApiUrl = new TextBox();
            label24 = new Label();
            checkBox_CheckIpHealth = new CheckBox();
            linkLabel2 = new LinkLabel();
            linkLabel1 = new LinkLabel();
            checkBox_UsingIOSMAC = new CheckBox();
            checkBox_SendSms = new CheckBox();
            checkBox_UsingIOSIMEI = new CheckBox();
            checkBox_UsingSystemDevs = new CheckBox();
            checkBox_CheckIpRegion = new CheckBox();
            checkBox_NoneOS = new CheckBox();
            label19 = new Label();
            label18 = new Label();
            numericUpDown_ChildProcessResetIntervalMinutes = new NumericUpDown();
            label17 = new Label();
            numericUpDown_MainProcessResetIntervalMinutes = new NumericUpDown();
            label16 = new Label();
            label15 = new Label();
            checkBox_IsRealIp = new CheckBox();
            label11 = new Label();
            numericUpDown_Multiple = new NumericUpDown();
            checkBox_IsProxyMode = new CheckBox();
            button1 = new Button();
            textBox_TaskApiUrl = new TextBox();
            label10 = new Label();
            label7 = new Label();
            label5 = new Label();
            label13 = new Label();
            numericUpDown_TaskPullIntervalMs = new NumericUpDown();
            buttonStart = new Button();
            label9 = new Label();
            numericUpDown_UvExecutionIntervalMs = new NumericUpDown();
            label8 = new Label();
            numericUpDown_MaxConcurrency = new NumericUpDown();
            label6 = new Label();
            label4 = new Label();
            textBox_TaskName = new TextBox();
            label3 = new Label();
            label2 = new Label();
            textBox_ProxyIpUrl = new TextBox();
            label1 = new Label();
            groupBox5 = new GroupBox();
            button2 = new Button();
            label23 = new Label();
            textBox_SmsPhone = new TextBox();
            label22 = new Label();
            numericUpDown_SendSmsTimeout = new NumericUpDown();
            label21 = new Label();
            label20 = new Label();
            textBox_SmsName = new TextBox();
            LogTextBox = new TextBox();
            groupBox3 = new GroupBox();
            LogDetailTextBox = new TextBox();
            groupBox4 = new GroupBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_ChannelCapacity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_IpValidityDuration).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_ChildProcessResetIntervalMinutes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MainProcessResetIntervalMinutes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_Multiple).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_TaskPullIntervalMs).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_UvExecutionIntervalMs).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaxConcurrency).BeginInit();
            groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SendSmsTimeout).BeginInit();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(taskInfoListView);
            groupBox1.Dock = DockStyle.Left;
            groupBox1.Location = new Point(0, 462);
            groupBox1.Margin = new Padding(5, 2, 5, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(5, 2, 5, 2);
            groupBox1.Size = new Size(542, 355);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "任务列表";
            // 
            // taskInfoListView
            // 
            taskInfoListView.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4, columnHeader5, columnHeader6 });
            taskInfoListView.Dock = DockStyle.Fill;
            taskInfoListView.FullRowSelect = true;
            taskInfoListView.GridLines = true;
            taskInfoListView.Location = new Point(5, 25);
            taskInfoListView.Margin = new Padding(5, 2, 5, 2);
            taskInfoListView.Name = "taskInfoListView";
            taskInfoListView.Size = new Size(532, 328);
            taskInfoListView.TabIndex = 0;
            taskInfoListView.UseCompatibleStateImageBehavior = false;
            taskInfoListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "任务名称";
            columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "IP地址";
            columnHeader2.Width = 100;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "真实IP";
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "延迟";
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "归属地";
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "状态";
            columnHeader6.Width = 120;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label12);
            groupBox2.Controls.Add(numericUpDown_ChannelCapacity);
            groupBox2.Controls.Add(label25);
            groupBox2.Controls.Add(label27);
            groupBox2.Controls.Add(numericUpDown_IpValidityDuration);
            groupBox2.Controls.Add(checkBox_IsHiddenMode);
            groupBox2.Controls.Add(checkBox_DisableImage);
            groupBox2.Controls.Add(textBox_DevApiUrl);
            groupBox2.Controls.Add(label24);
            groupBox2.Controls.Add(checkBox_CheckIpHealth);
            groupBox2.Controls.Add(linkLabel2);
            groupBox2.Controls.Add(linkLabel1);
            groupBox2.Controls.Add(checkBox_UsingIOSMAC);
            groupBox2.Controls.Add(checkBox_SendSms);
            groupBox2.Controls.Add(checkBox_UsingIOSIMEI);
            groupBox2.Controls.Add(checkBox_UsingSystemDevs);
            groupBox2.Controls.Add(checkBox_CheckIpRegion);
            groupBox2.Controls.Add(checkBox_NoneOS);
            groupBox2.Controls.Add(label19);
            groupBox2.Controls.Add(label18);
            groupBox2.Controls.Add(numericUpDown_ChildProcessResetIntervalMinutes);
            groupBox2.Controls.Add(label17);
            groupBox2.Controls.Add(numericUpDown_MainProcessResetIntervalMinutes);
            groupBox2.Controls.Add(label16);
            groupBox2.Controls.Add(label15);
            groupBox2.Controls.Add(checkBox_IsRealIp);
            groupBox2.Controls.Add(label11);
            groupBox2.Controls.Add(numericUpDown_Multiple);
            groupBox2.Controls.Add(checkBox_IsProxyMode);
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(textBox_TaskApiUrl);
            groupBox2.Controls.Add(label10);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(label13);
            groupBox2.Controls.Add(numericUpDown_TaskPullIntervalMs);
            groupBox2.Controls.Add(buttonStart);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(numericUpDown_UvExecutionIntervalMs);
            groupBox2.Controls.Add(label8);
            groupBox2.Controls.Add(numericUpDown_MaxConcurrency);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(textBox_TaskName);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(textBox_ProxyIpUrl);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(groupBox5);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.Location = new Point(0, 0);
            groupBox2.Margin = new Padding(5, 2, 5, 2);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(5, 2, 5, 2);
            groupBox2.Size = new Size(1309, 462);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "设置";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(52, 314);
            label12.Margin = new Padding(5, 0, 5, 0);
            label12.Name = "label12";
            label12.Size = new Size(86, 24);
            label12.TabIndex = 101;
            label12.Text = "任务队列:";
            // 
            // numericUpDown_ChannelCapacity
            // 
            numericUpDown_ChannelCapacity.Location = new Point(149, 310);
            numericUpDown_ChannelCapacity.Margin = new Padding(5, 2, 5, 2);
            numericUpDown_ChannelCapacity.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            numericUpDown_ChannelCapacity.Name = "numericUpDown_ChannelCapacity";
            numericUpDown_ChannelCapacity.Size = new Size(92, 30);
            numericUpDown_ChannelCapacity.TabIndex = 102;
            numericUpDown_ChannelCapacity.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Location = new Point(529, 367);
            label25.Margin = new Padding(6, 0, 6, 0);
            label25.Name = "label25";
            label25.Size = new Size(28, 24);
            label25.TabIndex = 100;
            label25.Text = "秒";
            // 
            // label27
            // 
            label27.AutoSize = true;
            label27.Location = new Point(316, 367);
            label27.Margin = new Padding(6, 0, 6, 0);
            label27.Name = "label27";
            label27.Size = new Size(103, 24);
            label27.TabIndex = 98;
            label27.Text = "Ip有效时长:";
            // 
            // numericUpDown_IpValidityDuration
            // 
            numericUpDown_IpValidityDuration.Location = new Point(423, 362);
            numericUpDown_IpValidityDuration.Margin = new Padding(6, 5, 6, 5);
            numericUpDown_IpValidityDuration.Name = "numericUpDown_IpValidityDuration";
            numericUpDown_IpValidityDuration.Size = new Size(103, 30);
            numericUpDown_IpValidityDuration.TabIndex = 99;
            numericUpDown_IpValidityDuration.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // checkBox_IsHiddenMode
            // 
            checkBox_IsHiddenMode.AutoSize = true;
            checkBox_IsHiddenMode.Location = new Point(960, 41);
            checkBox_IsHiddenMode.Margin = new Padding(5);
            checkBox_IsHiddenMode.Name = "checkBox_IsHiddenMode";
            checkBox_IsHiddenMode.Size = new Size(108, 28);
            checkBox_IsHiddenMode.TabIndex = 77;
            checkBox_IsHiddenMode.Text = "隐藏模式";
            checkBox_IsHiddenMode.UseVisualStyleBackColor = true;
            // 
            // checkBox_DisableImage
            // 
            checkBox_DisableImage.AutoSize = true;
            checkBox_DisableImage.Location = new Point(1030, 284);
            checkBox_DisableImage.Margin = new Padding(5);
            checkBox_DisableImage.Name = "checkBox_DisableImage";
            checkBox_DisableImage.Size = new Size(108, 28);
            checkBox_DisableImage.TabIndex = 76;
            checkBox_DisableImage.Text = "禁止图片";
            checkBox_DisableImage.UseVisualStyleBackColor = true;
            // 
            // textBox_DevApiUrl
            // 
            textBox_DevApiUrl.Location = new Point(110, 101);
            textBox_DevApiUrl.Margin = new Padding(5, 2, 5, 2);
            textBox_DevApiUrl.Name = "textBox_DevApiUrl";
            textBox_DevApiUrl.Size = new Size(526, 30);
            textBox_DevApiUrl.TabIndex = 75;
            textBox_DevApiUrl.Text = "http://117.21.200.18:9000/api/getdev.php";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(24, 109);
            label24.Margin = new Padding(5, 0, 5, 0);
            label24.Name = "label24";
            label24.Size = new Size(86, 24);
            label24.TabIndex = 74;
            label24.Text = "设备接口:";
            // 
            // checkBox_CheckIpHealth
            // 
            checkBox_CheckIpHealth.AutoSize = true;
            checkBox_CheckIpHealth.Location = new Point(623, 310);
            checkBox_CheckIpHealth.Margin = new Padding(5);
            checkBox_CheckIpHealth.Name = "checkBox_CheckIpHealth";
            checkBox_CheckIpHealth.Size = new Size(124, 28);
            checkBox_CheckIpHealth.TabIndex = 73;
            checkBox_CheckIpHealth.Text = "IP有效校验";
            checkBox_CheckIpHealth.UseVisualStyleBackColor = true;
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(883, 250);
            linkLabel2.Margin = new Padding(5, 0, 5, 0);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(82, 24);
            linkLabel2.TabIndex = 72;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "应用目录";
            linkLabel2.LinkClicked += linkLabel2_LinkClicked;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(883, 211);
            linkLabel1.Margin = new Padding(5, 0, 5, 0);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(82, 24);
            linkLabel1.TabIndex = 71;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "开机启动";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // checkBox_UsingIOSMAC
            // 
            checkBox_UsingIOSMAC.AutoSize = true;
            checkBox_UsingIOSMAC.Location = new Point(725, 228);
            checkBox_UsingIOSMAC.Margin = new Padding(5);
            checkBox_UsingIOSMAC.Name = "checkBox_UsingIOSMAC";
            checkBox_UsingIOSMAC.Size = new Size(145, 28);
            checkBox_UsingIOSMAC.TabIndex = 52;
            checkBox_UsingIOSMAC.Text = "IOS使用MAC";
            checkBox_UsingIOSMAC.UseVisualStyleBackColor = true;
            // 
            // checkBox_SendSms
            // 
            checkBox_SendSms.AutoSize = true;
            checkBox_SendSms.Location = new Point(1122, 109);
            checkBox_SendSms.Margin = new Padding(5);
            checkBox_SendSms.Name = "checkBox_SendSms";
            checkBox_SendSms.Size = new Size(108, 28);
            checkBox_SendSms.TabIndex = 45;
            checkBox_SendSms.Text = "短信服务";
            checkBox_SendSms.UseVisualStyleBackColor = true;
            // 
            // checkBox_UsingIOSIMEI
            // 
            checkBox_UsingIOSIMEI.AutoSize = true;
            checkBox_UsingIOSIMEI.Location = new Point(725, 199);
            checkBox_UsingIOSIMEI.Margin = new Padding(5);
            checkBox_UsingIOSIMEI.Name = "checkBox_UsingIOSIMEI";
            checkBox_UsingIOSIMEI.Size = new Size(140, 28);
            checkBox_UsingIOSIMEI.TabIndex = 51;
            checkBox_UsingIOSIMEI.Text = "IOS使用IMEI";
            checkBox_UsingIOSIMEI.UseVisualStyleBackColor = true;
            // 
            // checkBox_UsingSystemDevs
            // 
            checkBox_UsingSystemDevs.AutoSize = true;
            checkBox_UsingSystemDevs.Location = new Point(725, 170);
            checkBox_UsingSystemDevs.Margin = new Padding(5);
            checkBox_UsingSystemDevs.Name = "checkBox_UsingSystemDevs";
            checkBox_UsingSystemDevs.Size = new Size(180, 28);
            checkBox_UsingSystemDevs.TabIndex = 50;
            checkBox_UsingSystemDevs.Text = "使用系统设备信息";
            checkBox_UsingSystemDevs.UseVisualStyleBackColor = true;
            // 
            // checkBox_CheckIpRegion
            // 
            checkBox_CheckIpRegion.AutoSize = true;
            checkBox_CheckIpRegion.Location = new Point(781, 310);
            checkBox_CheckIpRegion.Margin = new Padding(5);
            checkBox_CheckIpRegion.Name = "checkBox_CheckIpRegion";
            checkBox_CheckIpRegion.Size = new Size(124, 28);
            checkBox_CheckIpRegion.TabIndex = 47;
            checkBox_CheckIpRegion.Text = "IP地区校验";
            checkBox_CheckIpRegion.UseVisualStyleBackColor = true;
            // 
            // checkBox_NoneOS
            // 
            checkBox_NoneOS.AutoSize = true;
            checkBox_NoneOS.Location = new Point(725, 255);
            checkBox_NoneOS.Margin = new Padding(5);
            checkBox_NoneOS.Name = "checkBox_NoneOS";
            checkBox_NoneOS.Size = new Size(115, 28);
            checkBox_NoneOS.TabIndex = 45;
            checkBox_NoneOS.Text = "不回传OS";
            checkBox_NoneOS.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(605, 256);
            label19.Margin = new Padding(5, 0, 5, 0);
            label19.Name = "label19";
            label19.Size = new Size(99, 24);
            label19.TabIndex = 42;
            label19.Text = "分钟±30秒";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(605, 218);
            label18.Margin = new Padding(5, 0, 5, 0);
            label18.Name = "label18";
            label18.Size = new Size(99, 24);
            label18.TabIndex = 41;
            label18.Text = "分钟±30秒";
            // 
            // numericUpDown_ChildProcessResetIntervalMinutes
            // 
            numericUpDown_ChildProcessResetIntervalMinutes.Location = new Point(504, 248);
            numericUpDown_ChildProcessResetIntervalMinutes.Margin = new Padding(5, 2, 5, 2);
            numericUpDown_ChildProcessResetIntervalMinutes.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numericUpDown_ChildProcessResetIntervalMinutes.Name = "numericUpDown_ChildProcessResetIntervalMinutes";
            numericUpDown_ChildProcessResetIntervalMinutes.Size = new Size(92, 30);
            numericUpDown_ChildProcessResetIntervalMinutes.TabIndex = 40;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(347, 256);
            label17.Margin = new Padding(5, 0, 5, 0);
            label17.Name = "label17";
            label17.Size = new Size(140, 24);
            label17.TabIndex = 39;
            label17.Text = "子进程重置间隔:";
            // 
            // numericUpDown_MainProcessResetIntervalMinutes
            // 
            numericUpDown_MainProcessResetIntervalMinutes.Location = new Point(504, 211);
            numericUpDown_MainProcessResetIntervalMinutes.Margin = new Padding(5, 2, 5, 2);
            numericUpDown_MainProcessResetIntervalMinutes.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numericUpDown_MainProcessResetIntervalMinutes.Name = "numericUpDown_MainProcessResetIntervalMinutes";
            numericUpDown_MainProcessResetIntervalMinutes.Size = new Size(92, 30);
            numericUpDown_MainProcessResetIntervalMinutes.TabIndex = 38;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(347, 218);
            label16.Margin = new Padding(5, 0, 5, 0);
            label16.Name = "label16";
            label16.Size = new Size(140, 24);
            label16.TabIndex = 37;
            label16.Text = "主进程重置间隔:";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(812, 107);
            label15.Margin = new Padding(5, 0, 5, 0);
            label15.Name = "label15";
            label15.Size = new Size(97, 24);
            label15.TabIndex = 36;
            label15.Text = "进程数量:0";
            // 
            // checkBox_IsRealIp
            // 
            checkBox_IsRealIp.AutoSize = true;
            checkBox_IsRealIp.Location = new Point(508, 310);
            checkBox_IsRealIp.Margin = new Padding(5);
            checkBox_IsRealIp.Name = "checkBox_IsRealIp";
            checkBox_IsRealIp.Size = new Size(88, 28);
            checkBox_IsRealIp.TabIndex = 35;
            checkBox_IsRealIp.Text = "真实IP";
            checkBox_IsRealIp.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(423, 147);
            label11.Margin = new Padding(5, 0, 5, 0);
            label11.Name = "label11";
            label11.Size = new Size(86, 24);
            label11.TabIndex = 31;
            label11.Text = "任务倍速:";
            // 
            // numericUpDown_Multiple
            // 
            numericUpDown_Multiple.Location = new Point(523, 140);
            numericUpDown_Multiple.Margin = new Padding(5, 2, 5, 2);
            numericUpDown_Multiple.Name = "numericUpDown_Multiple";
            numericUpDown_Multiple.Size = new Size(92, 30);
            numericUpDown_Multiple.TabIndex = 32;
            numericUpDown_Multiple.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // checkBox_IsProxyMode
            // 
            checkBox_IsProxyMode.AutoSize = true;
            checkBox_IsProxyMode.Location = new Point(382, 310);
            checkBox_IsProxyMode.Margin = new Padding(5);
            checkBox_IsProxyMode.Name = "checkBox_IsProxyMode";
            checkBox_IsProxyMode.Size = new Size(108, 28);
            checkBox_IsProxyMode.TabIndex = 28;
            checkBox_IsProxyMode.Text = "代理模式";
            checkBox_IsProxyMode.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Location = new Point(647, 101);
            button1.Margin = new Padding(5);
            button1.Name = "button1";
            button1.Size = new Size(149, 55);
            button1.TabIndex = 22;
            button1.Text = "清除";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox_TaskApiUrl
            // 
            textBox_TaskApiUrl.Location = new Point(109, 61);
            textBox_TaskApiUrl.Margin = new Padding(5, 2, 5, 2);
            textBox_TaskApiUrl.Name = "textBox_TaskApiUrl";
            textBox_TaskApiUrl.Size = new Size(526, 30);
            textBox_TaskApiUrl.TabIndex = 21;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(23, 70);
            label10.Margin = new Padding(5, 0, 5, 0);
            label10.Name = "label10";
            label10.Size = new Size(82, 24);
            label10.TabIndex = 20;
            label10.Text = "任务接口";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(812, 79);
            label7.Margin = new Padding(5, 0, 5, 0);
            label7.Name = "label7";
            label7.Size = new Size(97, 24);
            label7.TabIndex = 19;
            label7.Text = "运行时间:0";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(812, 53);
            label5.Margin = new Padding(5, 0, 5, 0);
            label5.Name = "label5";
            label5.Size = new Size(97, 24);
            label5.TabIndex = 16;
            label5.Text = "提交数量:0";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(280, 280);
            label13.Margin = new Padding(5, 0, 5, 0);
            label13.Name = "label13";
            label13.Size = new Size(46, 24);
            label13.TabIndex = 15;
            label13.Text = "毫秒";
            // 
            // numericUpDown_TaskPullIntervalMs
            // 
            numericUpDown_TaskPullIntervalMs.Location = new Point(149, 185);
            numericUpDown_TaskPullIntervalMs.Margin = new Padding(5, 2, 5, 2);
            numericUpDown_TaskPullIntervalMs.Maximum = new decimal(new int[] { 30000, 0, 0, 0 });
            numericUpDown_TaskPullIntervalMs.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numericUpDown_TaskPullIntervalMs.Name = "numericUpDown_TaskPullIntervalMs";
            numericUpDown_TaskPullIntervalMs.Size = new Size(92, 30);
            numericUpDown_TaskPullIntervalMs.TabIndex = 14;
            numericUpDown_TaskPullIntervalMs.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // buttonStart
            // 
            buttonStart.Location = new Point(647, 24);
            buttonStart.Margin = new Padding(5);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(149, 70);
            buttonStart.TabIndex = 13;
            buttonStart.Text = "开始";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(27, 280);
            label9.Margin = new Padding(5, 0, 5, 0);
            label9.Name = "label9";
            label9.Size = new Size(111, 24);
            label9.TabIndex = 0;
            label9.Text = "UV执行间隔:";
            // 
            // numericUpDown_UvExecutionIntervalMs
            // 
            numericUpDown_UvExecutionIntervalMs.Location = new Point(149, 273);
            numericUpDown_UvExecutionIntervalMs.Margin = new Padding(5, 2, 5, 2);
            numericUpDown_UvExecutionIntervalMs.Maximum = new decimal(new int[] { 30000, 0, 0, 0 });
            numericUpDown_UvExecutionIntervalMs.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numericUpDown_UvExecutionIntervalMs.Name = "numericUpDown_UvExecutionIntervalMs";
            numericUpDown_UvExecutionIntervalMs.Size = new Size(92, 30);
            numericUpDown_UvExecutionIntervalMs.TabIndex = 3;
            numericUpDown_UvExecutionIntervalMs.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(52, 348);
            label8.Margin = new Padding(5, 0, 5, 0);
            label8.Name = "label8";
            label8.Size = new Size(86, 24);
            label8.TabIndex = 0;
            label8.Text = "并发数量:";
            // 
            // numericUpDown_MaxConcurrency
            // 
            numericUpDown_MaxConcurrency.Location = new Point(149, 344);
            numericUpDown_MaxConcurrency.Margin = new Padding(5, 2, 5, 2);
            numericUpDown_MaxConcurrency.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            numericUpDown_MaxConcurrency.Name = "numericUpDown_MaxConcurrency";
            numericUpDown_MaxConcurrency.Size = new Size(92, 30);
            numericUpDown_MaxConcurrency.TabIndex = 3;
            numericUpDown_MaxConcurrency.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(812, 26);
            label6.Margin = new Padding(5, 0, 5, 0);
            label6.Name = "label6";
            label6.Size = new Size(63, 24);
            label6.TabIndex = 4;
            label6.Text = "label6";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(52, 142);
            label4.Margin = new Padding(5, 0, 5, 0);
            label4.Name = "label4";
            label4.Size = new Size(86, 24);
            label4.TabIndex = 0;
            label4.Text = "任务标识:";
            // 
            // textBox_TaskName
            // 
            textBox_TaskName.Location = new Point(149, 135);
            textBox_TaskName.Margin = new Padding(5, 2, 5, 2);
            textBox_TaskName.Name = "textBox_TaskName";
            textBox_TaskName.Size = new Size(90, 30);
            textBox_TaskName.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(287, 191);
            label3.Margin = new Padding(5, 0, 5, 0);
            label3.Name = "label3";
            label3.Size = new Size(46, 24);
            label3.TabIndex = 0;
            label3.Text = "毫秒";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(16, 193);
            label2.Margin = new Padding(5, 0, 5, 0);
            label2.Name = "label2";
            label2.Size = new Size(122, 24);
            label2.TabIndex = 0;
            label2.Text = "任务获取间隔:";
            // 
            // textBox_ProxyIpUrl
            // 
            textBox_ProxyIpUrl.Location = new Point(109, 24);
            textBox_ProxyIpUrl.Margin = new Padding(5, 2, 5, 2);
            textBox_ProxyIpUrl.Name = "textBox_ProxyIpUrl";
            textBox_ProxyIpUrl.Size = new Size(526, 30);
            textBox_ProxyIpUrl.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(23, 31);
            label1.Margin = new Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new Size(86, 24);
            label1.TabIndex = 0;
            label1.Text = "代理接口:";
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(button2);
            groupBox5.Controls.Add(label23);
            groupBox5.Controls.Add(textBox_SmsPhone);
            groupBox5.Controls.Add(label22);
            groupBox5.Controls.Add(numericUpDown_SendSmsTimeout);
            groupBox5.Controls.Add(label21);
            groupBox5.Controls.Add(label20);
            groupBox5.Controls.Add(textBox_SmsName);
            groupBox5.Location = new Point(970, 131);
            groupBox5.Margin = new Padding(5);
            groupBox5.Name = "groupBox5";
            groupBox5.Padding = new Padding(5);
            groupBox5.Size = new Size(323, 143);
            groupBox5.TabIndex = 44;
            groupBox5.TabStop = false;
            // 
            // button2
            // 
            button2.Location = new Point(250, 26);
            button2.Margin = new Padding(5);
            button2.Name = "button2";
            button2.Size = new Size(62, 34);
            button2.TabIndex = 51;
            button2.Text = "测试";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(14, 71);
            label23.Margin = new Padding(5, 0, 5, 0);
            label23.Name = "label23";
            label23.Size = new Size(50, 24);
            label23.TabIndex = 50;
            label23.Text = "电话:";
            // 
            // textBox_SmsPhone
            // 
            textBox_SmsPhone.Location = new Point(76, 62);
            textBox_SmsPhone.Margin = new Padding(5);
            textBox_SmsPhone.Name = "textBox_SmsPhone";
            textBox_SmsPhone.Size = new Size(234, 30);
            textBox_SmsPhone.TabIndex = 49;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(152, 107);
            label22.Margin = new Padding(5, 0, 5, 0);
            label22.Name = "label22";
            label22.Size = new Size(122, 24);
            label22.TabIndex = 48;
            label22.Text = "分钟,发送短信";
            // 
            // numericUpDown_SendSmsTimeout
            // 
            numericUpDown_SendSmsTimeout.Location = new Point(76, 98);
            numericUpDown_SendSmsTimeout.Margin = new Padding(5, 2, 5, 2);
            numericUpDown_SendSmsTimeout.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            numericUpDown_SendSmsTimeout.Name = "numericUpDown_SendSmsTimeout";
            numericUpDown_SendSmsTimeout.Size = new Size(72, 30);
            numericUpDown_SendSmsTimeout.TabIndex = 47;
            numericUpDown_SendSmsTimeout.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(14, 107);
            label21.Margin = new Padding(5, 0, 5, 0);
            label21.Name = "label21";
            label21.Size = new Size(50, 24);
            label21.TabIndex = 46;
            label21.Text = "超时:";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(14, 35);
            label20.Margin = new Padding(5, 0, 5, 0);
            label20.Name = "label20";
            label20.Size = new Size(50, 24);
            label20.TabIndex = 44;
            label20.Text = "名称:";
            // 
            // textBox_SmsName
            // 
            textBox_SmsName.Location = new Point(76, 26);
            textBox_SmsName.Margin = new Padding(5);
            textBox_SmsName.Name = "textBox_SmsName";
            textBox_SmsName.Size = new Size(162, 30);
            textBox_SmsName.TabIndex = 43;
            // 
            // LogTextBox
            // 
            LogTextBox.Dock = DockStyle.Fill;
            LogTextBox.Location = new Point(5, 25);
            LogTextBox.Margin = new Padding(5, 2, 5, 2);
            LogTextBox.Multiline = true;
            LogTextBox.Name = "LogTextBox";
            LogTextBox.ScrollBars = ScrollBars.Both;
            LogTextBox.Size = new Size(757, 328);
            LogTextBox.TabIndex = 3;
            LogTextBox.WordWrap = false;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(LogTextBox);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Location = new Point(542, 462);
            groupBox3.Margin = new Padding(5, 2, 5, 2);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(5, 2, 5, 2);
            groupBox3.Size = new Size(767, 355);
            groupBox3.TabIndex = 4;
            groupBox3.TabStop = false;
            groupBox3.Text = "日志";
            // 
            // LogDetailTextBox
            // 
            LogDetailTextBox.Dock = DockStyle.Fill;
            LogDetailTextBox.Location = new Point(5, 25);
            LogDetailTextBox.Margin = new Padding(5, 2, 5, 2);
            LogDetailTextBox.Multiline = true;
            LogDetailTextBox.Name = "LogDetailTextBox";
            LogDetailTextBox.ScrollBars = ScrollBars.Both;
            LogDetailTextBox.Size = new Size(1299, 80);
            LogDetailTextBox.TabIndex = 3;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(LogDetailTextBox);
            groupBox4.Dock = DockStyle.Bottom;
            groupBox4.Location = new Point(0, 817);
            groupBox4.Margin = new Padding(5, 2, 5, 2);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new Padding(5, 2, 5, 2);
            groupBox4.Size = new Size(1309, 107);
            groupBox4.TabIndex = 4;
            groupBox4.TabStop = false;
            groupBox4.Text = "详细日志";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1309, 924);
            Controls.Add(groupBox3);
            Controls.Add(groupBox1);
            Controls.Add(groupBox2);
            Controls.Add(groupBox4);
            Margin = new Padding(5, 2, 5, 2);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "优化测试-曝光";
            FormClosed += MainForm_FormClosed;
            Load += MainForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_ChannelCapacity).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_IpValidityDuration).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_ChildProcessResetIntervalMinutes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MainProcessResetIntervalMinutes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_Multiple).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_TaskPullIntervalMs).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_UvExecutionIntervalMs).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaxConcurrency).EndInit();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SendSmsTimeout).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            ResumeLayout(false);

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
        private System.Windows.Forms.TextBox textBox_ProxyIpUrl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox LogTextBox;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_TaskName;
        private System.Windows.Forms.TextBox LogDetailTextBox;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numericUpDown_MaxConcurrency;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numericUpDown_UvExecutionIntervalMs;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.NumericUpDown numericUpDown_TaskPullIntervalMs;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_TaskApiUrl;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox_IsProxyMode;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown numericUpDown_Multiple;
        private System.Windows.Forms.CheckBox checkBox_IsRealIp;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.NumericUpDown numericUpDown_ChildProcessResetIntervalMinutes;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.NumericUpDown numericUpDown_MainProcessResetIntervalMinutes;
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
        private System.Windows.Forms.CheckBox checkBox_CheckIpRegion;
        private System.Windows.Forms.CheckBox checkBox_UsingSystemDevs;
        private System.Windows.Forms.CheckBox checkBox_UsingIOSIMEI;
        private System.Windows.Forms.CheckBox checkBox_UsingIOSMAC;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.CheckBox checkBox_CheckIpHealth;
        private System.Windows.Forms.TextBox textBox_DevApiUrl;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.CheckBox checkBox_DisableImage;
        private System.Windows.Forms.CheckBox checkBox_IsHiddenMode;
        private Label label25;
        private Label label27;
        private NumericUpDown numericUpDown_IpValidityDuration;
        private Label label12;
        private NumericUpDown numericUpDown_ChannelCapacity;
    }
}

