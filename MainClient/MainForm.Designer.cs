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
            checkBox_IsHiddenMode = new CheckBox();
            checkBox_DisableImage = new CheckBox();
            textBox_DevApiUrl = new TextBox();
            label24 = new Label();
            checkBox_CheckIp = new CheckBox();
            linkLabel2 = new LinkLabel();
            linkLabel1 = new LinkLabel();
            checkBox_UsingIOSMAC = new CheckBox();
            checkBox_UsingIOSIMEI = new CheckBox();
            checkBox_UsingSystemDevs = new CheckBox();
            checkBox_IPAreaCheck = new CheckBox();
            checkBox_NoneOS = new CheckBox();
            label19 = new Label();
            label18 = new Label();
            numericUpDown_ChildProcessResetIntervalMinutes = new NumericUpDown();
            label17 = new Label();
            numericUpDown_MainProcessResetIntervalMinutes = new NumericUpDown();
            label16 = new Label();
            label15 = new Label();
            checkBox_RealIp = new CheckBox();
            label11 = new Label();
            numericUpDown_Multiple = new NumericUpDown();
            checkBox_IsProxyMode = new CheckBox();
            button1 = new Button();
            textBox_TaskApiUrl = new TextBox();
            label10 = new Label();
            label7 = new Label();
            label5 = new Label();
            label13 = new Label();
            numericUpDown_GetTaskInterval = new NumericUpDown();
            buttonStart = new Button();
            label12 = new Label();
            numericUpDown_MaximumParallel = new NumericUpDown();
            label9 = new Label();
            numericUpDown_UVInterval = new NumericUpDown();
            label8 = new Label();
            numericUpDown_MaximumLimitedConcurrency = new NumericUpDown();
            label6 = new Label();
            label4 = new Label();
            textBox_TaskIdentify = new TextBox();
            label3 = new Label();
            label2 = new Label();
            textBox_AllIpApiUrl = new TextBox();
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
            checkBox_SendSms = new CheckBox();
            LogTextBox = new TextBox();
            groupBox3 = new GroupBox();
            LogDetailTextBox = new TextBox();
            groupBox4 = new GroupBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_ChildProcessResetIntervalMinutes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MainProcessResetIntervalMinutes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_Multiple).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_GetTaskInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumParallel).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_UVInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumLimitedConcurrency).BeginInit();
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
            groupBox1.Location = new Point(0, 302);
            groupBox1.Margin = new Padding(4, 2, 4, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 2, 4, 2);
            groupBox1.Size = new Size(452, 379);
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
            taskInfoListView.Location = new Point(4, 22);
            taskInfoListView.Margin = new Padding(4, 2, 4, 2);
            taskInfoListView.Name = "taskInfoListView";
            taskInfoListView.Size = new Size(444, 355);
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
            groupBox2.Controls.Add(checkBox_IsHiddenMode);
            groupBox2.Controls.Add(checkBox_DisableImage);
            groupBox2.Controls.Add(textBox_DevApiUrl);
            groupBox2.Controls.Add(label24);
            groupBox2.Controls.Add(checkBox_CheckIp);
            groupBox2.Controls.Add(linkLabel2);
            groupBox2.Controls.Add(linkLabel1);
            groupBox2.Controls.Add(checkBox_UsingIOSMAC);
            groupBox2.Controls.Add(checkBox_SendSms);
            groupBox2.Controls.Add(checkBox_UsingIOSIMEI);
            groupBox2.Controls.Add(checkBox_UsingSystemDevs);
            groupBox2.Controls.Add(checkBox_IPAreaCheck);
            groupBox2.Controls.Add(checkBox_NoneOS);
            groupBox2.Controls.Add(label19);
            groupBox2.Controls.Add(label18);
            groupBox2.Controls.Add(numericUpDown_ChildProcessResetIntervalMinutes);
            groupBox2.Controls.Add(label17);
            groupBox2.Controls.Add(numericUpDown_MainProcessResetIntervalMinutes);
            groupBox2.Controls.Add(label16);
            groupBox2.Controls.Add(label15);
            groupBox2.Controls.Add(checkBox_RealIp);
            groupBox2.Controls.Add(label11);
            groupBox2.Controls.Add(numericUpDown_Multiple);
            groupBox2.Controls.Add(checkBox_IsProxyMode);
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(textBox_TaskApiUrl);
            groupBox2.Controls.Add(label10);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(label13);
            groupBox2.Controls.Add(numericUpDown_GetTaskInterval);
            groupBox2.Controls.Add(buttonStart);
            groupBox2.Controls.Add(label12);
            groupBox2.Controls.Add(numericUpDown_MaximumParallel);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(numericUpDown_UVInterval);
            groupBox2.Controls.Add(label8);
            groupBox2.Controls.Add(numericUpDown_MaximumLimitedConcurrency);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(textBox_TaskIdentify);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(textBox_AllIpApiUrl);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(groupBox5);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.Location = new Point(0, 0);
            groupBox2.Margin = new Padding(4, 2, 4, 2);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(4, 2, 4, 2);
            groupBox2.Size = new Size(1091, 302);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "设置";
            // 
            // checkBox_IsHiddenMode
            // 
            checkBox_IsHiddenMode.AutoSize = true;
            checkBox_IsHiddenMode.Location = new Point(777, 258);
            checkBox_IsHiddenMode.Margin = new Padding(4);
            checkBox_IsHiddenMode.Name = "checkBox_IsHiddenMode";
            checkBox_IsHiddenMode.Size = new Size(91, 24);
            checkBox_IsHiddenMode.TabIndex = 77;
            checkBox_IsHiddenMode.Text = "隐藏模式";
            checkBox_IsHiddenMode.UseVisualStyleBackColor = true;
            // 
            // checkBox_DisableImage
            // 
            checkBox_DisableImage.AutoSize = true;
            checkBox_DisableImage.Location = new Point(604, 217);
            checkBox_DisableImage.Margin = new Padding(4);
            checkBox_DisableImage.Name = "checkBox_DisableImage";
            checkBox_DisableImage.Size = new Size(91, 24);
            checkBox_DisableImage.TabIndex = 76;
            checkBox_DisableImage.Text = "禁止图片";
            checkBox_DisableImage.UseVisualStyleBackColor = true;
            // 
            // textBox_DevApiUrl
            // 
            textBox_DevApiUrl.Location = new Point(92, 84);
            textBox_DevApiUrl.Margin = new Padding(4, 2, 4, 2);
            textBox_DevApiUrl.Name = "textBox_DevApiUrl";
            textBox_DevApiUrl.Size = new Size(439, 27);
            textBox_DevApiUrl.TabIndex = 75;
            textBox_DevApiUrl.Text = "http://117.21.200.18:9000/api/getdev.php";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(20, 91);
            label24.Margin = new Padding(4, 0, 4, 0);
            label24.Name = "label24";
            label24.Size = new Size(73, 20);
            label24.TabIndex = 74;
            label24.Text = "设备接口:";
            // 
            // checkBox_CheckIp
            // 
            checkBox_CheckIp.AutoSize = true;
            checkBox_CheckIp.Location = new Point(816, 40);
            checkBox_CheckIp.Margin = new Padding(4);
            checkBox_CheckIp.Name = "checkBox_CheckIp";
            checkBox_CheckIp.Size = new Size(119, 24);
            checkBox_CheckIp.TabIndex = 73;
            checkBox_CheckIp.Text = "检测IP有效性";
            checkBox_CheckIp.UseVisualStyleBackColor = true;
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(736, 208);
            linkLabel2.Margin = new Padding(4, 0, 4, 0);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(69, 20);
            linkLabel2.TabIndex = 72;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "应用目录";
            linkLabel2.LinkClicked += linkLabel2_LinkClicked;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(736, 176);
            linkLabel1.Margin = new Padding(4, 0, 4, 0);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(69, 20);
            linkLabel1.TabIndex = 71;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "开机启动";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // checkBox_UsingIOSMAC
            // 
            checkBox_UsingIOSMAC.AutoSize = true;
            checkBox_UsingIOSMAC.Location = new Point(604, 190);
            checkBox_UsingIOSMAC.Margin = new Padding(4);
            checkBox_UsingIOSMAC.Name = "checkBox_UsingIOSMAC";
            checkBox_UsingIOSMAC.Size = new Size(122, 24);
            checkBox_UsingIOSMAC.TabIndex = 52;
            checkBox_UsingIOSMAC.Text = "IOS使用MAC";
            checkBox_UsingIOSMAC.UseVisualStyleBackColor = true;
            // 
            // checkBox_UsingIOSIMEI
            // 
            checkBox_UsingIOSIMEI.AutoSize = true;
            checkBox_UsingIOSIMEI.Location = new Point(604, 166);
            checkBox_UsingIOSIMEI.Margin = new Padding(4);
            checkBox_UsingIOSIMEI.Name = "checkBox_UsingIOSIMEI";
            checkBox_UsingIOSIMEI.Size = new Size(117, 24);
            checkBox_UsingIOSIMEI.TabIndex = 51;
            checkBox_UsingIOSIMEI.Text = "IOS使用IMEI";
            checkBox_UsingIOSIMEI.UseVisualStyleBackColor = true;
            // 
            // checkBox_UsingSystemDevs
            // 
            checkBox_UsingSystemDevs.AutoSize = true;
            checkBox_UsingSystemDevs.Location = new Point(604, 142);
            checkBox_UsingSystemDevs.Margin = new Padding(4);
            checkBox_UsingSystemDevs.Name = "checkBox_UsingSystemDevs";
            checkBox_UsingSystemDevs.Size = new Size(151, 24);
            checkBox_UsingSystemDevs.TabIndex = 50;
            checkBox_UsingSystemDevs.Text = "使用系统设备信息";
            checkBox_UsingSystemDevs.UseVisualStyleBackColor = true;
            // 
            // checkBox_IPAreaCheck
            // 
            checkBox_IPAreaCheck.AutoSize = true;
            checkBox_IPAreaCheck.Location = new Point(935, 258);
            checkBox_IPAreaCheck.Margin = new Padding(4);
            checkBox_IPAreaCheck.Name = "checkBox_IPAreaCheck";
            checkBox_IPAreaCheck.Size = new Size(104, 24);
            checkBox_IPAreaCheck.TabIndex = 47;
            checkBox_IPAreaCheck.Text = "IP地区校验";
            checkBox_IPAreaCheck.UseVisualStyleBackColor = true;
            // 
            // checkBox_NoneOS
            // 
            checkBox_NoneOS.AutoSize = true;
            checkBox_NoneOS.Location = new Point(959, 39);
            checkBox_NoneOS.Margin = new Padding(4);
            checkBox_NoneOS.Name = "checkBox_NoneOS";
            checkBox_NoneOS.Size = new Size(97, 24);
            checkBox_NoneOS.TabIndex = 45;
            checkBox_NoneOS.Text = "不回传OS";
            checkBox_NoneOS.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(504, 213);
            label19.Margin = new Padding(4, 0, 4, 0);
            label19.Name = "label19";
            label19.Size = new Size(83, 20);
            label19.TabIndex = 42;
            label19.Text = "分钟±30秒";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(504, 182);
            label18.Margin = new Padding(4, 0, 4, 0);
            label18.Name = "label18";
            label18.Size = new Size(83, 20);
            label18.TabIndex = 41;
            label18.Text = "分钟±30秒";
            // 
            // numericUpDown_ChildProcessResetIntervalMinutes
            // 
            numericUpDown_ChildProcessResetIntervalMinutes.Location = new Point(420, 207);
            numericUpDown_ChildProcessResetIntervalMinutes.Margin = new Padding(4, 2, 4, 2);
            numericUpDown_ChildProcessResetIntervalMinutes.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numericUpDown_ChildProcessResetIntervalMinutes.Name = "numericUpDown_ChildProcessResetIntervalMinutes";
            numericUpDown_ChildProcessResetIntervalMinutes.Size = new Size(77, 27);
            numericUpDown_ChildProcessResetIntervalMinutes.TabIndex = 40;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(289, 213);
            label17.Margin = new Padding(4, 0, 4, 0);
            label17.Name = "label17";
            label17.Size = new Size(118, 20);
            label17.TabIndex = 39;
            label17.Text = "子进程重置间隔:";
            // 
            // numericUpDown_MainProcessResetIntervalMinutes
            // 
            numericUpDown_MainProcessResetIntervalMinutes.Location = new Point(420, 176);
            numericUpDown_MainProcessResetIntervalMinutes.Margin = new Padding(4, 2, 4, 2);
            numericUpDown_MainProcessResetIntervalMinutes.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numericUpDown_MainProcessResetIntervalMinutes.Name = "numericUpDown_MainProcessResetIntervalMinutes";
            numericUpDown_MainProcessResetIntervalMinutes.Size = new Size(77, 27);
            numericUpDown_MainProcessResetIntervalMinutes.TabIndex = 38;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(289, 182);
            label16.Margin = new Padding(4, 0, 4, 0);
            label16.Name = "label16";
            label16.Size = new Size(118, 20);
            label16.TabIndex = 37;
            label16.Text = "主进程重置间隔:";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(677, 89);
            label15.Margin = new Padding(4, 0, 4, 0);
            label15.Name = "label15";
            label15.Size = new Size(82, 20);
            label15.TabIndex = 36;
            label15.Text = "进程数量:0";
            // 
            // checkBox_RealIp
            // 
            checkBox_RealIp.AutoSize = true;
            checkBox_RealIp.Location = new Point(816, 18);
            checkBox_RealIp.Margin = new Padding(4);
            checkBox_RealIp.Name = "checkBox_RealIp";
            checkBox_RealIp.Size = new Size(74, 24);
            checkBox_RealIp.TabIndex = 35;
            checkBox_RealIp.Text = "真实IP";
            checkBox_RealIp.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(337, 151);
            label11.Margin = new Padding(4, 0, 4, 0);
            label11.Name = "label11";
            label11.Size = new Size(73, 20);
            label11.TabIndex = 31;
            label11.Text = "任务倍速:";
            // 
            // numericUpDown_Multiple
            // 
            numericUpDown_Multiple.Location = new Point(420, 145);
            numericUpDown_Multiple.Margin = new Padding(4, 2, 4, 2);
            numericUpDown_Multiple.Name = "numericUpDown_Multiple";
            numericUpDown_Multiple.Size = new Size(77, 27);
            numericUpDown_Multiple.TabIndex = 32;
            numericUpDown_Multiple.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // checkBox_IsProxyMode
            // 
            checkBox_IsProxyMode.AutoSize = true;
            checkBox_IsProxyMode.Location = new Point(680, 258);
            checkBox_IsProxyMode.Margin = new Padding(4);
            checkBox_IsProxyMode.Name = "checkBox_IsProxyMode";
            checkBox_IsProxyMode.Size = new Size(91, 24);
            checkBox_IsProxyMode.TabIndex = 28;
            checkBox_IsProxyMode.Text = "代理模式";
            checkBox_IsProxyMode.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Location = new Point(539, 84);
            button1.Margin = new Padding(4);
            button1.Name = "button1";
            button1.Size = new Size(124, 46);
            button1.TabIndex = 22;
            button1.Text = "清除";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox_TaskApiUrl
            // 
            textBox_TaskApiUrl.Location = new Point(91, 51);
            textBox_TaskApiUrl.Margin = new Padding(4, 2, 4, 2);
            textBox_TaskApiUrl.Name = "textBox_TaskApiUrl";
            textBox_TaskApiUrl.Size = new Size(439, 27);
            textBox_TaskApiUrl.TabIndex = 21;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(19, 58);
            label10.Margin = new Padding(4, 0, 4, 0);
            label10.Name = "label10";
            label10.Size = new Size(67, 20);
            label10.TabIndex = 20;
            label10.Text = "任务API:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(677, 66);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(82, 20);
            label7.TabIndex = 19;
            label7.Text = "运行时间:0";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(677, 44);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(82, 20);
            label5.TabIndex = 16;
            label5.Text = "提交数量:0";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(237, 151);
            label13.Margin = new Padding(4, 0, 4, 0);
            label13.Name = "label13";
            label13.Size = new Size(39, 20);
            label13.TabIndex = 15;
            label13.Text = "毫秒";
            // 
            // numericUpDown_GetTaskInterval
            // 
            numericUpDown_GetTaskInterval.Location = new Point(155, 113);
            numericUpDown_GetTaskInterval.Margin = new Padding(4, 2, 4, 2);
            numericUpDown_GetTaskInterval.Maximum = new decimal(new int[] { 30000, 0, 0, 0 });
            numericUpDown_GetTaskInterval.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numericUpDown_GetTaskInterval.Name = "numericUpDown_GetTaskInterval";
            numericUpDown_GetTaskInterval.Size = new Size(77, 27);
            numericUpDown_GetTaskInterval.TabIndex = 14;
            numericUpDown_GetTaskInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // buttonStart
            // 
            buttonStart.Location = new Point(539, 20);
            buttonStart.Margin = new Padding(4);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(124, 58);
            buttonStart.TabIndex = 13;
            buttonStart.Text = "开始";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(41, 182);
            label12.Margin = new Padding(4, 0, 4, 0);
            label12.Name = "label12";
            label12.Size = new Size(103, 20);
            label12.TabIndex = 9;
            label12.Text = "任务进程数量:";
            // 
            // numericUpDown_MaximumParallel
            // 
            numericUpDown_MaximumParallel.Location = new Point(155, 176);
            numericUpDown_MaximumParallel.Margin = new Padding(4, 2, 4, 2);
            numericUpDown_MaximumParallel.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown_MaximumParallel.Name = "numericUpDown_MaximumParallel";
            numericUpDown_MaximumParallel.Size = new Size(77, 27);
            numericUpDown_MaximumParallel.TabIndex = 10;
            numericUpDown_MaximumParallel.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(73, 151);
            label9.Margin = new Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new Size(79, 20);
            label9.TabIndex = 0;
            label9.Text = "单UV间隔:";
            // 
            // numericUpDown_UVInterval
            // 
            numericUpDown_UVInterval.Location = new Point(155, 145);
            numericUpDown_UVInterval.Margin = new Padding(4, 2, 4, 2);
            numericUpDown_UVInterval.Maximum = new decimal(new int[] { 30000, 0, 0, 0 });
            numericUpDown_UVInterval.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numericUpDown_UVInterval.Name = "numericUpDown_UVInterval";
            numericUpDown_UVInterval.Size = new Size(77, 27);
            numericUpDown_UVInterval.TabIndex = 3;
            numericUpDown_UVInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(41, 213);
            label8.Margin = new Padding(4, 0, 4, 0);
            label8.Name = "label8";
            label8.Size = new Size(103, 20);
            label8.TabIndex = 0;
            label8.Text = "工作线程数量:";
            // 
            // numericUpDown_MaximumLimitedConcurrency
            // 
            numericUpDown_MaximumLimitedConcurrency.Location = new Point(155, 207);
            numericUpDown_MaximumLimitedConcurrency.Margin = new Padding(4, 2, 4, 2);
            numericUpDown_MaximumLimitedConcurrency.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown_MaximumLimitedConcurrency.Name = "numericUpDown_MaximumLimitedConcurrency";
            numericUpDown_MaximumLimitedConcurrency.Size = new Size(77, 27);
            numericUpDown_MaximumLimitedConcurrency.TabIndex = 3;
            numericUpDown_MaximumLimitedConcurrency.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(677, 22);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(53, 20);
            label6.TabIndex = 4;
            label6.Text = "label6";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(305, 119);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(103, 20);
            label4.TabIndex = 0;
            label4.Text = "独立任务标识:";
            // 
            // textBox_TaskIdentify
            // 
            textBox_TaskIdentify.Location = new Point(420, 113);
            textBox_TaskIdentify.Margin = new Padding(4, 2, 4, 2);
            textBox_TaskIdentify.Name = "textBox_TaskIdentify";
            textBox_TaskIdentify.Size = new Size(76, 27);
            textBox_TaskIdentify.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(240, 118);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(39, 20);
            label3.TabIndex = 0;
            label3.Text = "毫秒";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(41, 120);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(103, 20);
            label2.TabIndex = 0;
            label2.Text = "获取任务间隔:";
            // 
            // textBox_AllIpApiUrl
            // 
            textBox_AllIpApiUrl.Location = new Point(91, 20);
            textBox_AllIpApiUrl.Margin = new Padding(4, 2, 4, 2);
            textBox_AllIpApiUrl.Name = "textBox_AllIpApiUrl";
            textBox_AllIpApiUrl.Size = new Size(439, 27);
            textBox_AllIpApiUrl.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(19, 26);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(67, 20);
            label1.TabIndex = 0;
            label1.Text = "代理API:";
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
            groupBox5.Location = new Point(808, 109);
            groupBox5.Margin = new Padding(4);
            groupBox5.Name = "groupBox5";
            groupBox5.Padding = new Padding(4);
            groupBox5.Size = new Size(269, 119);
            groupBox5.TabIndex = 44;
            groupBox5.TabStop = false;
            // 
            // button2
            // 
            button2.Location = new Point(208, 22);
            button2.Margin = new Padding(4);
            button2.Name = "button2";
            button2.Size = new Size(52, 28);
            button2.TabIndex = 51;
            button2.Text = "测试";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(12, 59);
            label23.Margin = new Padding(4, 0, 4, 0);
            label23.Name = "label23";
            label23.Size = new Size(43, 20);
            label23.TabIndex = 50;
            label23.Text = "电话:";
            // 
            // textBox_SmsPhone
            // 
            textBox_SmsPhone.Location = new Point(63, 52);
            textBox_SmsPhone.Margin = new Padding(4);
            textBox_SmsPhone.Name = "textBox_SmsPhone";
            textBox_SmsPhone.Size = new Size(196, 27);
            textBox_SmsPhone.TabIndex = 49;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(127, 89);
            label22.Margin = new Padding(4, 0, 4, 0);
            label22.Name = "label22";
            label22.Size = new Size(103, 20);
            label22.TabIndex = 48;
            label22.Text = "分钟,发送短信";
            // 
            // numericUpDown_SendSmsTimeout
            // 
            numericUpDown_SendSmsTimeout.Location = new Point(63, 82);
            numericUpDown_SendSmsTimeout.Margin = new Padding(4, 2, 4, 2);
            numericUpDown_SendSmsTimeout.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            numericUpDown_SendSmsTimeout.Name = "numericUpDown_SendSmsTimeout";
            numericUpDown_SendSmsTimeout.Size = new Size(60, 27);
            numericUpDown_SendSmsTimeout.TabIndex = 47;
            numericUpDown_SendSmsTimeout.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(12, 89);
            label21.Margin = new Padding(4, 0, 4, 0);
            label21.Name = "label21";
            label21.Size = new Size(43, 20);
            label21.TabIndex = 46;
            label21.Text = "超时:";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(12, 29);
            label20.Margin = new Padding(4, 0, 4, 0);
            label20.Name = "label20";
            label20.Size = new Size(43, 20);
            label20.TabIndex = 44;
            label20.Text = "名称:";
            // 
            // textBox_SmsName
            // 
            textBox_SmsName.Location = new Point(63, 22);
            textBox_SmsName.Margin = new Padding(4);
            textBox_SmsName.Name = "textBox_SmsName";
            textBox_SmsName.Size = new Size(136, 27);
            textBox_SmsName.TabIndex = 43;
            // 
            // checkBox_SendSms
            // 
            checkBox_SendSms.AutoSize = true;
            checkBox_SendSms.Location = new Point(816, 66);
            checkBox_SendSms.Margin = new Padding(4);
            checkBox_SendSms.Name = "checkBox_SendSms";
            checkBox_SendSms.Size = new Size(91, 24);
            checkBox_SendSms.TabIndex = 45;
            checkBox_SendSms.Text = "短信服务";
            checkBox_SendSms.UseVisualStyleBackColor = true;
            // 
            // LogTextBox
            // 
            LogTextBox.Dock = DockStyle.Fill;
            LogTextBox.Location = new Point(4, 22);
            LogTextBox.Margin = new Padding(4, 2, 4, 2);
            LogTextBox.Multiline = true;
            LogTextBox.Name = "LogTextBox";
            LogTextBox.ScrollBars = ScrollBars.Both;
            LogTextBox.Size = new Size(631, 355);
            LogTextBox.TabIndex = 3;
            LogTextBox.WordWrap = false;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(LogTextBox);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Location = new Point(452, 302);
            groupBox3.Margin = new Padding(4, 2, 4, 2);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(4, 2, 4, 2);
            groupBox3.Size = new Size(639, 379);
            groupBox3.TabIndex = 4;
            groupBox3.TabStop = false;
            groupBox3.Text = "日志";
            // 
            // LogDetailTextBox
            // 
            LogDetailTextBox.Dock = DockStyle.Fill;
            LogDetailTextBox.Location = new Point(4, 22);
            LogDetailTextBox.Margin = new Padding(4, 2, 4, 2);
            LogDetailTextBox.Multiline = true;
            LogDetailTextBox.Name = "LogDetailTextBox";
            LogDetailTextBox.ScrollBars = ScrollBars.Both;
            LogDetailTextBox.Size = new Size(1083, 65);
            LogDetailTextBox.TabIndex = 3;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(LogDetailTextBox);
            groupBox4.Dock = DockStyle.Bottom;
            groupBox4.Location = new Point(0, 681);
            groupBox4.Margin = new Padding(4, 2, 4, 2);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new Padding(4, 2, 4, 2);
            groupBox4.Size = new Size(1091, 89);
            groupBox4.TabIndex = 4;
            groupBox4.TabStop = false;
            groupBox4.Text = "详细日志";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1091, 770);
            Controls.Add(groupBox3);
            Controls.Add(groupBox1);
            Controls.Add(groupBox2);
            Controls.Add(groupBox4);
            Margin = new Padding(4, 2, 4, 2);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "优化测试-曝光";
            FormClosing += MainForm_FormClosing;
            FormClosed += MainForm_FormClosed;
            Load += MainForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_ChildProcessResetIntervalMinutes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MainProcessResetIntervalMinutes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_Multiple).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_GetTaskInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumParallel).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_UVInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumLimitedConcurrency).EndInit();
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
        private System.Windows.Forms.CheckBox checkBox_IsProxyMode;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown numericUpDown_Multiple;
        private System.Windows.Forms.CheckBox checkBox_RealIp;
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
        private System.Windows.Forms.CheckBox checkBox_IsHiddenMode;
    }
}

