using OMRON.Compolet.Variable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace EQModeChangeSimulator
{
    public class Form1 : Form, IEqContext
    {
        public class CmdMap
        {
            public string CmdName;
            public string CimTag;     // CIM→EQ 标签
            public int CimWord;
            public int CimBit;

            public string ReplyTag;   // EQ→CIM 标签
            public int ReplyWord;
            public int ReplyBit;
        }

        public class CmdState
        {
            public bool Last = false;
            public bool WaitingOff = false;
            public DateTime Deadline;
        }
        Dictionary<string, CmdMap> CmdMapping = new Dictionary<string, CmdMap>();
        Dictionary<string, CmdState> CmdStates = new Dictionary<string, CmdState>();

        // 用来保存 Tag → 命令列表（监控时通过 tag 检索）
        Dictionary<string, List<string>> CmdGroups = new Dictionary<string, List<string>>();
       
        public class EventMap
        {
            public enum ReplyBlockType
            {
                None,
                JobDataRequest
            }

            public string Name;      // 事件名称
            public string EQTag;     // EQ → CIM 的标签（如 EQToCIM_Data01, EQToCIM_JudgeData）
            public int EQWord;       // Word
            public int EQBit;        // Bit
            public string ReplyTag;  // CIM → EQ 的标签（如 CIMToEQ_Data、CIMToEQ_PanelManagement）
            public int ReplyWord;
            public int ReplyBit;
            public ReplyBlockType BlockType = ReplyBlockType.None;
            public int ReplyBlockWord = -1;
        } 
        public class EQEventState
        {
            public bool Sent = false;
            public bool WaitingReply = false;
            public DateTime Deadline;
        }
        Dictionary<string, EventMap> EventMapping = new Dictionary<string, EventMap>();
        Dictionary<string, EQEventState> EventStates = new Dictionary<string, EQEventState>();

        private const int CimToEqDataWords = 273;
        private const int JobDataRequestReplyBitWord = 3;
        private const int JobDataRequestReplyBit = 13;
        private const int JobDataRequestReplyBlockOffset = 72;
        private const int JobDataRequestReplyJobWords = 150;
        private const int JobDataRequestReplyAckOffset = 222;
        private const int JobDataRequestReplyMinWords = 243;
        private const int JobDataRequestReplyBlockWords = 171;
        private bool _jobDataRequestFromAoi;
        private string _jobDataRequestJobId = "";

        private enum PeriodicDataReportStage
        {
            Idle,
            CvPending,
            UtPending
        }

        private PeriodicDataReportStage _periodicDataReportStage =
            PeriodicDataReportStage.Idle;

        // ===============================
        // EQ↔EQ LinkSignal 映射
        // ===============================
        public class LinkMap
        {
            public string Name;   // 变量名，如 ReceiveAble
            public string Tag;    // RV 或 SD 标签名
            public int Word;
            public int Bit;
        }

        Dictionary<string, LinkMap> LinkMappings = new();

        // 保存当前 LinkSignal 状态
        Dictionary<string, bool> LinkStates = new();

        // 监控开关
        public static Form1 Instance { get; private set; }
        private bool eq2eqEnabled = true;
 
        public bool Eq2EqEnabled => eq2eqEnabled;

        private bool cimModeEnabled = true; // 默认 ON
        public bool CimModeEnabled => cimModeEnabled;



        // 超时
        private int T1 = 10;
        private int T2 = 10;
        private int T3 = 10;
        private enum EqState { Idle, ReplyOnWaitingOff }
        private EqState _state = EqState.Idle;

        // ===============================
        // UI + Compolet
        // ===============================
        public enum EQStatus
        {
            Idle = 1,
            Run = 2,
            Down = 3,
            PM = 4,
            ETime = 5,
            JC = 6,
            MC = 7
        }
        public enum MachineMode
        {
            Normal = 1,   // 正常模式
            Bypass = 4    // 直通模式
        }
        public enum RequestOptionType
        {
            JobID = 1,                       // 按 Job ID 请求
            SequenceNumber = 2,              // 按 Slot/CST 顺序请求
            VcrMissMatchJobID = 3,           // VCR Mismatch 时用
            VcrReadJobID = 4                 // 单片请账 (VCR Read Job ID)
        }
        private string LastReason = "NW";   // 默认无 WIP，可被 CPP 覆盖
        private string LastSub = "NW";
        private int LastAlarmId = 0;
        private IContainer components;
        private Button btnStart, btnStop;
        //private Button btnTrigger;
        private CheckBox chkActive;
        private Label lblState;
        private TextBox txtLog;
        private Timer timer1;
        private Timer timer2;
        private VariableCompolet variableCompolet1;
        private CommandHandler cmdHandler;
        private EqTcpServer _tcpServer;
        private CppCommandRouter _cppRouter;
        private LinkSignalHandler _linkHandler;
        private Timer timerHeartbeat;   // ★ 心跳计时器
        private bool heartbeatFlag = false;   // ON/OFF 翻转
        private CheckBox chkCIMMode;
        public EQStatus LastEQStatus = EQStatus.Idle;
        private DateTime lastRunTime = DateTime.MinValue;
        private Timer statusTimer;
        // 手动 Job Request UI
        private Panel panelJobRequest;
        private TextBox txtJobID;
        private TextBox txtCSTSeq;
        private TextBox txtSlotSeq;
        private ComboBox cmbReqOption;
        private Button btnSendJobRequest;
        private Panel panelMove;
        private Button btnManualMove;
        private int CVIntervalHours = 1;                     // 默认 1 小时上报一次
        private DateTime lastCVSendTime = DateTime.MinValue;
        private Timer timerCV;
        private bool autoRecipeEnabled = false;
        public bool AutoRecipeEnabled => autoRecipeEnabled;
        private MachineMode _currentMachineMode = MachineMode.Normal;
        public MachineMode CurrentMachineMode => _currentMachineMode;
        public Form1()
        {
            Instance = this;
            LocalFileLogger.Info("APP", "Startup cwd=" + AppDomain.CurrentDomain.BaseDirectory +
                " version=" + Assembly.GetExecutingAssembly().GetName().Version);
            InitializeComponent();
            InitCmdMapping();
            InitEventSystem();
            InitLinkSignals();
            LoadTimeoutConfig();
            _cppRouter = new CppCommandRouter(this);
            _tcpServer = new EqTcpServer(
     Log,
     _cppRouter.HandleRawJson,
     () =>
     {
         LocalFileLogger.Info("TCP", "Main software connected");
        
         PpidDrawingMapForm.TrySendLatestToCpp();
     });
            _tcpServer.Start(9999);
            LocalFileLogger.Info("TCP", "Server started port=9999");
            cmdHandler = new CommandHandler(
    ReadWordArray,
    WriteWordArray,    // ★ 加这一行
    Log,
    _tcpServer,
    this

);
            _linkHandler = new LinkSignalHandler(
    ReadWordArray,
    WriteWordArray,
    Log,
    _tcpServer
);
            Log("[TCP] EQ TCP Server 已启动");
            this.FormClosing += Form1_FormClosing;
        }

        // ===============================
        // 初始化界面
        // ===============================

        private void InitializeComponent()
        {
            this.components = new Container();
            this.chkActive = new CheckBox();
            this.btnStart = new Button();
            this.btnStop = new Button();
            //this.btnTrigger = new Button();
            this.lblState = new Label();
            this.txtLog = new TextBox();
           
          
            this.timer1 = new Timer(this.components);
            this.timer2 = new Timer(this.components);
            this.timerHeartbeat = new Timer(this.components);
            this.statusTimer = new Timer(this.components);
            this.timerCV = new Timer(this.components);
            this.variableCompolet1 = new VariableCompolet(this.components);
        

            // chkActive
            this.chkActive.AutoSize = true;
            this.chkActive.Location = new System.Drawing.Point(12, 12);
            this.chkActive.Text = "Compolet Active";
            this.chkActive.CheckedChanged += chkActive_CheckedChanged;

            // btnStart
            this.btnStart.Location = new System.Drawing.Point(150, 8);
            this.btnStart.Text = "开始监控";
            this.btnStart.Click += btnStart_Click;
            
            // btnStop
            this.btnStop.Location = new System.Drawing.Point(250, 8);
            this.btnStop.Text = "停止监控";
            this.btnStop.Click += btnStop_Click;

            // btnStop
            //this.btnTrigger.Location = new System.Drawing.Point(350, 8);
            //this.btnTrigger.Text = "触发";
            //this.btnTrigger.Click += btnTrigger_Click;

            // lblState
            this.lblState.AutoSize = true;
            this.lblState.Location = new System.Drawing.Point(350, 13);
            this.lblState.Text = "状态: Idle";

            // txtLog
            this.txtLog.Location = new System.Drawing.Point(12, 40);
            this.txtLog.Size = new System.Drawing.Size(600, 400);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = ScrollBars.Vertical;


            // timer
            this.timer1.Interval = 200;
            this.timer1.Tick += timerT1_Tick;

            // timer
            this.timer2.Interval = 200;
            this.timer2.Tick += timerT2_Tick;

            this.timerHeartbeat.Interval = 4000;   // 4 秒翻转
            this.timerHeartbeat.Tick += timerHeartbeat_Tick;

            this.statusTimer.Interval = 1000;  // 每秒检查一次
            this.statusTimer.Tick += StatusTimer_Tick;

            // CV 自动上报计时器（每 1 分钟检查一次）    
            timerCV.Interval = 60000;    // 60 秒
            timerCV.Tick += TimerCV_Tick;

            // variableCompolet
            this.variableCompolet1.Changed += variableCompolet1_Changed;
            CheckBox chkEq2Eq = new CheckBox();
            chkEq2Eq.Location = new System.Drawing.Point(420, 12);
            chkEq2Eq.Text = "EQ↔EQ通讯";
            chkEq2Eq.Checked = eq2eqEnabled;  
            chkEq2Eq.AutoSize = true;
            chkEq2Eq.CheckedChanged += (s, e) =>
            {
                eq2eqEnabled = chkEq2Eq.Checked;
              
                ApplyEq2EqLinkSignals(eq2eqEnabled);
            };
            // ===============================
            // ★ Auto Recipe 开关
            // ===============================
            CheckBox chkAutoRecipe = new CheckBox();
            chkAutoRecipe.Location = new System.Drawing.Point(520, 12);   // EQ↔EQ 旁边
            chkAutoRecipe.Text = "Auto Recipe";
            chkAutoRecipe.AutoSize = true;
            chkAutoRecipe.Checked = autoRecipeEnabled;

            chkAutoRecipe.CheckedChanged += (s, e) =>
            {
                autoRecipeEnabled = chkAutoRecipe.Checked;
                // ★ 上报 AutoRecipeChangeModeReport（协议要求）
                RaiseAutoRecipeChangeModeReport(autoRecipeEnabled);
            };

            chkCIMMode = new CheckBox();
            chkCIMMode.Location = new System.Drawing.Point(350, 12);
            chkCIMMode.Text = "CIM Mode ON";
            chkCIMMode.Checked = cimModeEnabled;   // 与当前状态同步
            chkCIMMode.CheckedChanged += (s, e) =>
            {
                // 避免重复触发（内部调用修改 Checked 时）
                if (chkCIMMode.Focused)
                    SetCimMode(chkCIMMode.Checked);
            };

            // Form1
            this.ClientSize = new System.Drawing.Size(950, 560);
            this.Controls.Add(chkEq2Eq);
            this.Controls.Add(chkAutoRecipe);
            this.Controls.Add(this.chkActive);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnStop);
            //this.Controls.Add(this.btnTrigger);
            //this.Controls.Add(this.lblState);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(chkCIMMode);
            this.Text = "EQ Mode Change Simulator (RV/SD Demo)";
            this.StartPosition = FormStartPosition.CenterScreen;
            // ===============================
            //  Job Request Panel
            // ===============================
            panelJobRequest = new Panel();
            panelJobRequest.BorderStyle = BorderStyle.FixedSingle;
            panelJobRequest.Location = new System.Drawing.Point(630, 80);
            panelJobRequest.Size = new System.Drawing.Size(280, 260);
            panelJobRequest.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // 标题
            Label lblTitle = new Label();
            lblTitle.Text = "手动长单请求 JobDataRequest";
            lblTitle.Font = new Font("微软雅黑", 10, FontStyle.Bold);
            lblTitle.Location = new Point(10, 10);
            lblTitle.AutoSize = true;
            panelJobRequest.Controls.Add(lblTitle);

            // RequestJobID
            Label lblJobID = new Label();
            lblJobID.Text = "RequestJobID:";
            lblJobID.Location = new Point(10, 50);
            lblJobID.AutoSize = true;
            panelJobRequest.Controls.Add(lblJobID);

            txtJobID = new TextBox();
            txtJobID.Location = new Point(130, 45);
            txtJobID.Width = 130;
            panelJobRequest.Controls.Add(txtJobID);

            // CSTSequenceNumber
            Label lblCSTSeq = new Label();
            lblCSTSeq.Text = "CSTSequence:";
            lblCSTSeq.Location = new Point(10, 90);
            lblCSTSeq.AutoSize = true;
            panelJobRequest.Controls.Add(lblCSTSeq);

            txtCSTSeq = new TextBox();
            txtCSTSeq.Location = new Point(130, 85);
            txtCSTSeq.Width = 130;
            panelJobRequest.Controls.Add(txtCSTSeq);

            // SlotSequenceNumber
            Label lblSlotSeq = new Label();
            lblSlotSeq.Text = "SlotSequence:";
            lblSlotSeq.Location = new Point(10, 130);
            lblSlotSeq.AutoSize = true;
            panelJobRequest.Controls.Add(lblSlotSeq);

            txtSlotSeq = new TextBox();
            txtSlotSeq.Location = new Point(130, 125);
            txtSlotSeq.Width = 130;
            panelJobRequest.Controls.Add(txtSlotSeq);

            // RequestOption
            Label lblReqOption = new Label();
            lblReqOption.Text = "RequestOption:";
            lblReqOption.Location = new Point(10, 170);
            lblReqOption.AutoSize = true;
            panelJobRequest.Controls.Add(lblReqOption);

            cmbReqOption = new ComboBox();
            cmbReqOption.Location = new Point(130, 165);
            cmbReqOption.Width = 130;
            cmbReqOption.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbReqOption.Items.Clear();
            cmbReqOption.Items.AddRange(new object[]
            {
    "Job ID",
    "Sequence Number",
    "VCR Miss Match Job ID",
    "VCR Read Job ID"
            });
            cmbReqOption.SelectedIndex = 0;
            panelJobRequest.Controls.Add(cmbReqOption);

            // Send Button
            btnSendJobRequest = new Button();
            btnSendJobRequest.Text = "发送请求";
            btnSendJobRequest.Location = new Point(80, 220);
            btnSendJobRequest.Width = 120;
            btnSendJobRequest.Click += BtnSendJobRequest_Click;
            panelJobRequest.Controls.Add(btnSendJobRequest);
            this.Controls.Add(panelJobRequest);
            // ===============================
            // ★ 手动移板 panel（新版）
            // ===============================
            panelMove = new Panel();
            panelMove.BorderStyle = BorderStyle.FixedSingle;
            panelMove.Location = new Point(630, 10);
            panelMove.Size = new Size(280, 60);
            panelMove.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // 下拉框：Report Option
            ComboBox cmbManualMoveOption = new ComboBox();
            cmbManualMoveOption.Name = "cmbManualMoveOption";
            cmbManualMoveOption.Location = new Point(10, 15);
            cmbManualMoveOption.Width = 120;
            cmbManualMoveOption.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbManualMoveOption.Items.AddRange(new object[]
            {
    "Line In",
    "Line Out",
    "Scrap",
    "Delete"
            });
            cmbManualMoveOption.SelectedIndex = 0;   // 默认选 1
            panelMove.Controls.Add(cmbManualMoveOption);

            // 按钮（继续用原 BtnManualMove_Click）
            btnManualMove = new Button();
            btnManualMove.Text = "手动移板";
            btnManualMove.Location = new Point(150, 10);
            btnManualMove.Size = new Size(120, 35);
            btnManualMove.Click += BtnManualMove_Click;   // ⭐ 保留原来的
            panelMove.Controls.Add(btnManualMove);

            // 添加到 Form
            this.Controls.Add(panelMove);
            // ===============================
            // ★ 超时设置面板（T1/T2/T3 + 保存按钮）
            // ===============================
            Panel panelTimeout = new Panel();
            panelTimeout.BorderStyle = BorderStyle.FixedSingle;
            panelTimeout.Location = new Point(630, 350);
            panelTimeout.Size = new Size(280, 190);
            panelTimeout.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            Label lblT1 = new Label();
            lblT1.Text = "T1 超时(秒):";
            lblT1.Location = new Point(10, 10);
            lblT1.AutoSize = true;
            panelTimeout.Controls.Add(lblT1);

            TextBox txtT1 = new TextBox();
            txtT1.Name = "txtT1";
            txtT1.Location = new Point(120, 8);
            txtT1.Width = 120;
            panelTimeout.Controls.Add(txtT1);

            Label lblT2 = new Label();
            lblT2.Text = "T2 超时(秒):";
            lblT2.Location = new Point(10, 45);
            lblT2.AutoSize = true;
            panelTimeout.Controls.Add(lblT2);

            TextBox txtT2 = new TextBox();
            txtT2.Name = "txtT2";
            txtT2.Location = new Point(120, 42);
            txtT2.Width = 120;
            panelTimeout.Controls.Add(txtT2);

            Label lblT3 = new Label();
            lblT3.Text = "T3(预留):";
            lblT3.Location = new Point(10, 80);
            lblT3.AutoSize = true;
            panelTimeout.Controls.Add(lblT3);

            TextBox txtT3 = new TextBox();
            txtT3.Name = "txtT3";
            txtT3.Location = new Point(120, 78);
            txtT3.Width = 120;
            panelTimeout.Controls.Add(txtT3);
            // ===============================
            // ★ CV 上报间隔（小时）
            // ===============================
            Label lblCV = new Label();
            lblCV.Text = "CV上报间隔(小时):";
            lblCV.Location = new Point(10, 115);
            lblCV.AutoSize = true;
            panelTimeout.Controls.Add(lblCV);

            TextBox txtCV = new TextBox();
            txtCV.Name = "txtCV";
            txtCV.Location = new Point(120, 112);
            txtCV.Width = 120;
            panelTimeout.Controls.Add(txtCV);

            // 保存按钮
            Button btnSaveTimeout = new Button();
            btnSaveTimeout.Text = "保存设置";
            btnSaveTimeout.Location = new Point(120, 155);
            btnSaveTimeout.Width = 120;
            btnSaveTimeout.Click += BtnSaveTimeout_Click;
            panelTimeout.Controls.Add(btnSaveTimeout);
            this.Controls.Add(panelTimeout);
            // ===============================
            // ★ 清空日志按钮（自动贴在日志框下方）
            // ===============================
            Button btnClearLog = new Button();
            btnClearLog.Text = "清空日志";
            btnClearLog.Width = 100;
            btnClearLog.Height = 28;

            // 自动放在 txtLog 底部 + 10px
            btnClearLog.Location = new Point(
                txtLog.Left,
                txtLog.Bottom + 10
            );

            // 可随窗体宽度变化（左对齐即可）
            btnClearLog.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            btnClearLog.Click += (s, e) => txtLog.Clear();
            this.Controls.Add(btnClearLog);

            Button btnSignalMonitor = new Button();
            btnSignalMonitor.Text = "信号监控";
            btnSignalMonitor.Width = 100;
            btnSignalMonitor.Height = 28;
            btnSignalMonitor.Location = new Point(
                btnClearLog.Right + 10,
                btnClearLog.Top
            );

            btnSignalMonitor.Click += (s, e) =>
            {
                new SignalMonitorForm(variableCompolet1, _tcpServer).Show();
            };

            this.Controls.Add(btnSignalMonitor);

            //Button btnPpidMap = new Button();
            //btnPpidMap.Text = "PPID映射";
            //btnPpidMap.Width = 100;
            //btnPpidMap.Height = 28;
            //btnPpidMap.Location = new Point(
            //    btnSignalMonitor.Right + 10,
            //    btnSignalMonitor.Top
            //);

            //btnPpidMap.Click += (s, e) =>
            //{
            //    new PpidDrawingMapForm().ShowDialog(this);
            //};

            //this.Controls.Add(btnPpidMap);

        }

        //初始化CIM TO EQ
        void InitCmdMapping()
        {
            // ====== 来自 RV_CIMToEQ_Data_01_03_00  ======
            AddCmd("CIMModeChangeCommand", "RV_CIMToEQ_Data_01_03_00", 0, 0, "SD_EQToCIM_Data01_03_01_00", 2, 0);
            AddCmd("CIMMessageSetCommand", "RV_CIMToEQ_Data_01_03_00", 0, 1, "SD_EQToCIM_Data01_03_01_00", 2, 1);
            AddCmd("CIMMessageClearCommand", "RV_CIMToEQ_Data_01_03_00", 0, 2, "SD_EQToCIM_Data01_03_01_00", 2, 2);
            AddCmd("CurrentRecipeChangeCommand", "RV_CIMToEQ_Data_01_03_00", 0, 6, "SD_EQToCIM_RecipeData_03_01_00", 1, 1);



            AddCmd("DateTimeSetCommand", "RV_CIMToEQ_Data_01_03_00", 0, 3, "SD_EQToCIM_Data01_03_01_00", 2, 3);
            AddCmd("MachineModeChangeCommand", "RV_CIMToEQ_Data_01_03_00", 0, 4, "SD_EQToCIM_Data01_03_01_00", 2, 4);
            AddCmd("RecipeParameterRequestCommand", "RV_CIMToEQ_Data_01_03_00", 0, 5, "SD_EQToCIM_RecipeData_03_01_00", 1, 0);
            
            AddCmd("FGCodeCommand", "RV_CIMToEQ_Data_01_03_00", 0, 7, "SD_EQToCIM_Data01_03_01_00", 2, 7);

           
        }
        void AddCmd(string name, string cimTag, int word, int bit,
            string repTag, int repWord, int repBit)
        {
            CmdMapping[name] = new CmdMap
            {
                CmdName = name,
                CimTag = cimTag,
                CimWord = word,
                CimBit = bit,
                ReplyTag = repTag,
                ReplyWord = repWord,
                ReplyBit = repBit
            };

            CmdStates[name] = new CmdState();

            // 建立 Tag 分组
            if (!CmdGroups.ContainsKey(cimTag))
                CmdGroups[cimTag] = new List<string>();

            CmdGroups[cimTag].Add(name);
        }
        //初始化EQ TO  CIM
        void InitEventSystem()
        {
            AddEvent("MachineStatusChangeReport",
                "SD_EQToCIM_Data01_03_01_00", 0, 5,
                "RV_CIMToEQ_Data_01_03_00", 1, 0);
            AddEvent("RecipeChangeReport",
                "SD_EQToCIM_RecipeData_03_01_00", 0, 2,
                "RV_CIMToEQ_Data_01_03_00", 1, 10);
            AddEvent("CurrentRecipeNumberChangeReport",
               "SD_EQToCIM_RecipeData_03_01_00", 0, 1,
               "RV_CIMToEQ_Data_01_03_00", 1, 9);
            AddEvent("CIMMessageConfirmReport",
               "SD_EQToCIM_Data01_03_01_00", 0, 6,
               "RV_CIMToEQ_Data_01_03_00", 1, 1);
            AddEvent("OperatorLoginRequest",
               "SD_EQToCIM_Data01_03_01_00", 0, 10,
               "RV_CIMToEQ_Data_01_03_00", 1, 5);
            AddEvent("AlarmReport",
              "SD_EQToCIM_Data01_03_01_00", 0, 11,
              "RV_CIMToEQ_Data_01_03_00", 1, 6);
            AddEvent("TactTimeChangeReport",
            "SD_EQToCIM_Data02_03_01_00", 0, 1,
            "RV_CIMToEQ_Data_01_03_00", 1, 14);
            AddEvent("MachineModeChangeReport",
                "SD_EQToCIM_Data01_03_01_00", 0, 9,
                "RV_CIMToEQ_Data_01_03_00", 1, 4);
            AddEvent("VCRStatusReport",
              "SD_EQToCIM_Data01_03_01_00", 0, 8,
              "RV_CIMToEQ_Data_01_03_00", 1, 3);
            AddEvent("JobDataRequest",
    "SD_EQToCIM_Data01_03_01_00", 0, 15,  
    "RV_CIMToEQ_Data_01_03_00", JobDataRequestReplyBitWord, JobDataRequestReplyBit);
            EventMapping["JobDataRequest"].BlockType = EventMap.ReplyBlockType.JobDataRequest;
            EventMapping["JobDataRequest"].ReplyBlockWord = JobDataRequestReplyBlockOffset;
            AddEvent("JobManualMoveReport",
              "SD_EQToCIM_Data01_03_01_00", 0, 14,
              "RV_CIMToEQ_Data_01_03_00", 3, 12);
            AddEvent("JobJudgeResultReport",
            "SD_EQToCIM_Data01_03_01_00", 0, 13,
            "RV_CIMToEQ_Data_01_03_00", 1, 7);
            AddEvent("ReceivedJobReport",
           "SD_EQToCIM_ReceiveJob01_03_01_00", 0, 0,
           "RV_CIMToEQ_Data_01_03_00", 3, 0);
            AddEvent("SentOutJobReport",
        "SD_EQToCIM_SentJob01_03_01_00", 0, 0,
        "RV_CIMToEQ_Data_01_03_00", 3, 6);
            AddEvent("DefectCodeReport",
      "SD_EQToCIM_Data02_03_01_00", 0, 0,
      "RV_CIMToEQ_Data_01_03_00", 1, 11);
            AddEvent("CVDataReport",
     "SD_EQToCIM_MachineVariable_03_01_00", 0, 1,
     "RV_CIMToEQ_Data_01_03_00", 3, 15);
            AddEvent("UTDataReport",
     "SD_EQToCIM_Data01_03_01_00", 0, 12,
     "RV_CIMToEQ_Data_01_03_00", 1, 13);
            AddEvent("DVDataReport",
     "SD_EQToCIM_MachineVariable_03_01_00", 0, 0,
     "RV_CIMToEQ_Data_01_03_00", 3, 14);
            AddEvent("AutoRecipeChangeModeReport",
    "SD_EQToCIM_RecipeData_03_01_00", 0, 0,
    "RV_CIMToEQ_Data_01_03_00", 1, 8);
           




            AddEvent("DateTimeRequest",
                "SD_EQToCIM_Data01_03_01_00", 0, 7,
                "RV_CIMToEQ_Data_01_03_00", 1, 2);      
            // ⭐新加 JudgeData 事件（整合进统一系统）
            AddEvent("PanelJudgeDataDownloadRequest",
                "SD_EQToCIM_JudgeData_03_01_00", 0, 1,
                "RV_CIMToEQ_PanelManagement_01_03_00", 0, 1);

            AddEvent("PanelDataUpdateReport",
                "SD_EQToCIM_JudgeData_03_01_00", 0, 2,
                "RV_CIMToEQ_PanelManagement_01_03_00", 0, 2);

            // 如果以后要加更多事件，一样 add 即可
        }
        void AddEvent(string name, string eqTag, int eqWord, int eqBit,
              string repTag, int repWord, int repBit)
        {
            EventMapping[name] = new EventMap
            {
                Name = name,
                EQTag = eqTag,
                EQWord = eqWord,
                EQBit = eqBit,
                ReplyTag = repTag,
                ReplyWord = repWord,
                ReplyBit = repBit
            };

            EventStates[name] = new EQEventState();
        }

        void InitLinkSignals()
        {
            // ===== 上游 → 我（我读取）=====
          
            AddLink("SendAble", "RV_EQToEQ_LinkSignal_02_03_00", 0, 3);
            AddLink("SendStart", "RV_EQToEQ_LinkSignal_02_03_00", 0, 4);
            AddLink("SendComplete", "RV_EQToEQ_LinkSignal_02_03_00", 0, 5);


            // ……我->下游  读取
            AddLink("ReceiveAble", "RV_EQToEQ_LinkSignal_04_03_00", 3, 3);
            AddLink("ConveyerState", "RV_EQToEQ_LinkSignal_04_03_00", 3, 11);
            AddLink("ReceiveComplete", "RV_EQToEQ_LinkSignal_04_03_00", 3, 5);

            // ……继续补齐
        }

        void AddLink(string name, string tag, int word, int bit)
        {
            LinkMappings[name] = new LinkMap
            {
                Name = name,
                Tag = tag,
                Word = word,
                Bit = bit
            };
            LinkStates[name] = false;
        }
        private void LoadTimeoutConfig()
        {
            string path = "eq_config.json";
            if (!System.IO.File.Exists(path))
                return;

            try
            {
                string json = System.IO.File.ReadAllText(path);
                dynamic cfg = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                // ===== 必填项 =====
                T1 = cfg.T1;
                T2 = cfg.T2;
                T3 = cfg.T3;

                // ===== 新增 CV 上报间隔（兼容旧配置）======
                try
                {
                    CVIntervalHours = cfg.CVIntervalHours != null ? (int)cfg.CVIntervalHours : 1;
                }
                catch
                {
                    CVIntervalHours = 1;   // 旧版本 JSON 不含该字段
                }

                // ===== 更新 UI 显示 =====
                (this.Controls.Find("txtT1", true)[0] as TextBox).Text = T1.ToString();
                (this.Controls.Find("txtT2", true)[0] as TextBox).Text = T2.ToString();
                (this.Controls.Find("txtT3", true)[0] as TextBox).Text = T3.ToString();
                (this.Controls.Find("txtCV", true)[0] as TextBox).Text = CVIntervalHours.ToString();

                Log($"已载入配置：T1={T1}, T2={T2}, T3={T3}, CVIntervalHours={CVIntervalHours}");
            }
            catch (Exception ex)
            {
                Log($"配置文件读取失败，使用默认值（错误: {ex.Message}）");
            }
        }




        // ===============================
        // 主入口
        // ===============================

        [STAThread]
        static void Main()
        {
            Application.Run(new Form1());
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                LocalFileLogger.Info("APP", "Closing");
                Log("[System] 软件关闭，复位 EQ↔EQ 通讯信号");

                // ===== 1. 先停所有定时器 =====
                timer1?.Stop();
                timer2?.Stop();
                timerHeartbeat?.Stop();
                statusTimer?.Stop();
                timerCV?.Stop();

                // ===== 2. 如果 EQ↔EQ 曾开启，则强制复位 =====
                if (eq2eqEnabled)
                {
                    ApplyEq2EqLinkSignals(false);
                    eq2eqEnabled = false;
                }

                // ===== 3. 可选：关闭 CIM 心跳 =====
                int[] data = ReadWordArray("SD_EQToCIM_Data01_03_01_00");
                if (data != null)
                {
                    data[0] &= ~(1 << 4);   // MachineAlive OFF
                    WriteWordArray("SD_EQToCIM_Data01_03_01_00", data);
                }

               
            }
            catch (Exception ex)
            {
                // 关闭过程中不要弹窗，避免卡死
                LocalFileLogger.Error("APP", "Close reset failed: " + ex.Message);
                Log("[System] 关闭复位异常: " + ex.Message);
            }
            finally
            {
                LocalFileLogger.Info("APP", "Closed");
            }
        }


        // ===============================
        // Compolet Active
        // ===============================

        private void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                variableCompolet1.Active = chkActive.Checked;

                if (chkActive.Checked)
                {
                    // ✔ 关键修复：必须设置 WindowHandle 否则 Changed 不触发
                    variableCompolet1.WindowHandle = this.Handle;
                }
                else
                {
                    variableCompolet1.WindowHandle = IntPtr.Zero;
                }

                Log($"Compolet Active = {chkActive.Checked}");
            }
            catch (Exception ex)
            {
                chkActive.Checked = false;
                MessageBox.Show("激活失败: " + ex.Message);
            }
        }


        // ===============================
        // 监控控制
        // ===============================

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                variableCompolet1.SetEvent("RV_CIMToEQ_Data_01_03_00", 1);
                variableCompolet1.SetEvent("RV_CIMToEQ_PanelManagement_01_03_00", 2);
                variableCompolet1.SetEvent("RV_EQToEQ_LinkSignal_02_03_00", 3);
                variableCompolet1.SetEvent("RV_EQToEQ_LinkSignal_04_03_00", 4);
                LocalFileLogger.Info("PLC", "Monitoring started");
                _state = EqState.Idle;
                UpdateState();

                timer1.Start();
                timer2.Start();
                this.timerHeartbeat.Start();
                this.statusTimer.Start();
                this.timerCV.Start();
                // ===============================
                // ★ EQ↔EQ 默认开启：开始监控时真正生效
                // ===============================
                if (eq2eqEnabled)
                {
                    ApplyEq2EqLinkSignals(true);
                    Log("[EQ↔EQ] 通讯已启用");
                }
                Log($"开始监控: {"RV_CIMToEQ_Data_01_03_00"}");
                Log($"开始监控: {"RV_CIMToEQ_PanelManagement_01_03_00"}");
                Log($"开始监控: {"RV_EQToEQ_LinkSignal_02_03_00"}");
                Log($"开始监控: {"RV_EQToEQ_LinkSignal_04_03_00"}");
                // ===============================
                // ★ 默认开启 CIM MODE + 上报一次状态
                // ===============================
                SetCimMode(true);
            }
            catch (Exception ex)
            {
                LocalFileLogger.Error("PLC", "Monitoring start failed: " + ex.Message);
                MessageBox.Show("Start Error: " + ex.Message);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                timer1.Stop();
                timer2.Stop();
                timerHeartbeat.Stop();
                statusTimer.Stop();
                timerCV.Stop();
                variableCompolet1.ClearEvent("RV_CIMToEQ_Data_01_03_00");
                variableCompolet1.ClearEvent("RV_CIMToEQ_PanelManagement_01_03_00");
                variableCompolet1.ClearEvent("RV_EQToEQ_LinkSignal_02_03_00");
                variableCompolet1.ClearEvent("RV_EQToEQ_LinkSignal_04_03_00");
                LocalFileLogger.Info("PLC", "Monitoring stopped");
                _state = EqState.Idle;
                UpdateState();
                Log("停止监控");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Stop Error: " + ex.Message);
            }
        }
        private void BtnSendJobRequest_Click(object sender, EventArgs e)
        {
            string jobId = txtJobID.Text.Trim();
            if (!int.TryParse(txtCSTSeq.Text, out int cstSeq) ||
                !int.TryParse(txtSlotSeq.Text, out int slotSeq))
            {
                MessageBox.Show("CST/Slot 必须为整数！");
                return;
            }
            int option = cmbReqOption.SelectedIndex + 1;  // 1~4

            Log($"[手动请求 JobDataRequest] JobID={jobId}, CST={cstSeq}, Slot={slotSeq}, Option={option}");
            RequestJobData(jobId, cstSeq, slotSeq, option, false);
        }

        public bool RequestJobData(string jobId, int cstSeq, int slotSeq, int requestOption,
                                   bool notifyCpp)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
            {
                LocalFileLogger.Warn("PLC", "JobDataRequest UI dispatch unavailable");
                if (notifyCpp) SendJobDataRequestFailure(jobId, "ui_dispatch_unavailable");
                return false;
            }
            if (InvokeRequired)
            {
                try
                {
                    return (bool)Invoke(new Func<bool>(
                        () => RequestJobData(jobId, cstSeq, slotSeq, requestOption, notifyCpp)));
                }
                catch (Exception ex)
                {
                    LocalFileLogger.Error("PLC", "JobDataRequest UI dispatch failed: " + ex.Message);
                    Log("[JobDataRequest] UI线程调度失败: " + ex.Message);
                    if (notifyCpp) SendJobDataRequestFailure(jobId, "ui_dispatch_failed");
                    return false;
                }
            }

            jobId = (jobId ?? "").Trim();
            bool validAscii = jobId.Length > 0 && jobId.Length <= 40;
            foreach (char ch in jobId)
                validAscii &= ch >= 0x21 && ch <= 0x7e;

            if (!cimModeEnabled || !validAscii || cstSeq < 0 || cstSeq > 65535 ||
                slotSeq < 0 || slotSeq > 65535 || requestOption < 1 || requestOption > 4)
            {
                Log($"[JobDataRequest] rejected JobID={jobId}, CST={cstSeq}, Slot={slotSeq}, Option={requestOption}");
                if (notifyCpp) SendJobDataRequestFailure(jobId, "invalid_request");
                return false;
            }

            if (EventStates.TryGetValue("JobDataRequest", out var state) && state.WaitingReply)
            {
                Log($"[JobDataRequest] rejected busy, JobID={jobId}");
                if (notifyCpp) SendJobDataRequestFailure(jobId, "request_busy");
                return false;
            }

            bool sent = SendEventAndBlock("JobDataRequest", data =>
            {
                const int baseWord = 248;
                string fixedJobId = jobId.PadRight(40, ' ');
                byte[] ascii = Encoding.ASCII.GetBytes(fixedJobId);
                for (int i = 0; i < 20; i++)
                {
                    byte lo = ascii[i * 2];
                    byte hi = ascii[i * 2 + 1];
                    data[baseWord + i] = (hi << 8) | lo;
                }
                data[baseWord + 20] = cstSeq;
                data[baseWord + 21] = slotSeq;
                data[baseWord + 22] = requestOption;
            });

            if (!sent)
            {
                if (notifyCpp) SendJobDataRequestFailure(jobId, "event_send_failed");
                return false;
            }

            _jobDataRequestFromAoi = notifyCpp;
            _jobDataRequestJobId = jobId;
            Log($"[JobDataRequest] sent JobID={jobId}, CST={cstSeq}, Slot={slotSeq}, Option={requestOption}, Origin={(notifyCpp ? "AOI" : "Manual")}");
            return true;
        }

        private void SendJobDataRequestFailure(string requestJobId, string reason)
        {
            SendToCpp("JobDataRequestReply", new
            {
                ack = 0,
                length = 0,
                job = Array.Empty<int>(),
                requestJobId = requestJobId ?? "",
                reason = reason ?? "request_failed"
            });
        }
        private void BtnManualMove_Click(object sender, EventArgs e)
        {


            // 读取下拉框控件
            ComboBox cmb = panelMove.Controls["cmbManualMoveOption"] as ComboBox;

            int option = 1;  // 默认

            if (cmb != null)
                option = cmb.SelectedIndex + 1;   // 1~4

            // 发送给 C++
            SendToCpp("ManualJobMove", new
            {
                reportOption = option
            });

           
        }
        private void BtnSaveTimeout_Click(object sender, EventArgs e)
        {
            TextBox txtT1 = this.Controls.Find("txtT1", true)[0] as TextBox;
            TextBox txtT2 = this.Controls.Find("txtT2", true)[0] as TextBox;
            TextBox txtT3 = this.Controls.Find("txtT3", true)[0] as TextBox;
            TextBox txtCV = this.Controls.Find("txtCV", true)[0] as TextBox; // ★ 新增

            // ===== 校验输入 =====
            if (!int.TryParse(txtT1.Text, out int t1) ||
                !int.TryParse(txtT2.Text, out int t2) ||
                !int.TryParse(txtT3.Text, out int t3))
            {
                MessageBox.Show("请输入正确的 T1/T2/T3 整数！");
                return;
            }

            if (!int.TryParse(txtCV.Text, out int cvHours) || cvHours <= 0)
            {
                MessageBox.Show("请输入正确的 CV 上报间隔（>0 小时）！");
                return;
            }

            // ===== 更新本地变量 =====
            T1 = t1;
            T2 = t2;
            T3 = t3;
            CVIntervalHours = cvHours;            // ★ 新增
            lastCVSendTime = DateTime.Now;        // ★ 每次保存都重新计时

            // ===== 保存 JSON 文件 =====
            var cfg = new
            {
                T1 = this.T1,
                T2 = this.T2,
                T3 = this.T3,
                CVIntervalHours = this.CVIntervalHours   // ★ 新增保存字段
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(cfg, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("eq_config.json", json, Encoding.UTF8);

            Log($"已保存配置：T1={T1}, T2={T2}, T3={T3}, CVIntervalHours={CVIntervalHours}");
            MessageBox.Show("保存成功！");
        }


        private void btnTrigger_Click(object sender, EventArgs e)
        {

            TriggerEvent("PanelDataUpdateReport");
        }
        void TriggerEvent(string eventName)
        {
            var em = EventMapping[eventName];
            var st = EventStates[eventName];

            int[] data = ReadWordArray(em.EQTag);

            data[em.EQWord] |= (1 << em.EQBit);
            WriteWordArray(em.EQTag, data);

            Log($"{eventName} = 1 (发送给 CIM)");

            st.Sent = true;
            st.WaitingReply = true;
            st.Deadline = DateTime.Now.AddSeconds(T2);
        }
        public bool SendEventAndBlock(string eventName, Action<int[]> blockWriter)
        {
            eventName = eventName?.Trim() ?? "";
            if (!EventMapping.TryGetValue(eventName, out var em))
            {
                Log($"[ERROR] SendEventAndBlock 未找到事件映射, eventName:{eventName}");
                return false;
            }

            if (!EventStates.TryGetValue(eventName, out var st))
            {
                Log($"[ERROR] SendEventAndBlock 未找到事件状态, eventName:{eventName}");
                return false;
            }

            int[] data = ReadWordArray(em.EQTag);
            if (data == null)
            {
                Log($"[ERR] SendEventAndBlock 读取 {em.EQTag} 失败");
                return false;
            }

            // 写 block 内容
            blockWriter(data);

            // 触发事件位（BIT）
            data[em.EQWord] |= (1 << em.EQBit);

            // 写回 PLC tag
            if (!WriteWordArray(em.EQTag, data))
            {
                Log($"[ERR] SendEventAndBlock 写入 {em.EQTag} 失败");
                return false;
            }

            // 更新事件状态机
            st.Sent = true;
            st.WaitingReply = true;
            st.Deadline = DateTime.Now.AddSeconds(T1);

            Log($"[EQ→CIM] 事件 {eventName} 已发送（包含 Block）");
            return true;
        }


        // ===============================
        // 事件触发 (Changed)
        // ===============================

        private void variableCompolet1_Changed(object sender, EventArgs e)
        {
            try
            {
                HashSet<string> pendingTags = new();   // ⭐ 收集本轮所有 tag（去重）

                while (true)
                {
                    string varName;
                    int eventId;

                    try
                    {
                        variableCompolet1.ReciveEvent(out varName, out eventId, 1);
                    }
                    catch
                    {
                        break;
                    }

                    if (string.IsNullOrEmpty(varName))
                        break;

                    // ⭐ 去重：多次事件只记录一次
                    pendingTags.Add(varName);
                }

                // =============================
                // ⭐ 只处理最终去重后的 Tag 列表
                // =============================
                foreach (var tag in pendingTags)
                {
                    // EQ→EQ
                    if (eq2eqEnabled && LinkSignalTag(tag))
                    {
                        HandleLinkSignal(tag);
                        continue;
                    }

                    // CIM→EQ
                    HandleSingleCommand(tag);
                 
                }
                //后续加上datetimerequest的回复识别reply  block   目前只有识别reply的功能  没有识别reply block的功能
                HandleAllReplies(); 

            }
            catch (Exception ex)
            {
                Log("Changed 异常: " + ex.Message);
            }
        }

        void HandleLinkSignal(string tagName)
        {
            if (!eq2eqEnabled) return;

            int[] words = ReadWordArray(tagName);
            if (words == null) return;

            foreach (var kv in LinkMappings)
            {
                var m = kv.Value;
                if (m.Tag != tagName) continue;

                bool cur = GetBit(words, m.Word, m.Bit);
                bool last = LinkStates[m.Name];

                if (cur != last)
                {
                    Log($"[EQ↔EQ] {m.Name} = {(cur ? "ON" : "OFF")}");
                    LinkStates[m.Name] = cur;

                    // 回调到外部 LinkHandler
                    _linkHandler.Handle(m.Name, cur);
                }
            }
        }

        void HandleSingleCommand(string tagName)
        {
            if (!CmdGroups.ContainsKey(tagName))
                return;

            int[] rv = ReadWordArray(tagName);
            if (rv == null) return;

            foreach (string cmdName in CmdGroups[tagName])
            {
                // ================================
                // 如果 CIM OFF → 忽略所有命令（除了 CIMModeChangeCommand）
                // ================================
                if (!cimModeEnabled && cmdName != "CIMModeChangeCommand")
                {
                    
                    continue;
                }
                ProcessCommand(tagName, cmdName, rv);
            }
        }
        void HandleAllReplies()
        {
            foreach (var kv in EventMapping)
            {
                var em = kv.Value;
                var st = EventStates[kv.Key];

                if (!st.WaitingReply) continue;

                int[] rv = ReadWordArray(em.ReplyTag);
                if (rv == null) continue;

                bool reply = GetBit(rv, em.ReplyWord, em.ReplyBit);

                if (reply)
                {
                    Log($"收到 Reply → {em.Name}");

                    // ====== 新增：按用户定义的 BlockType 解析 ======
                    st.WaitingReply = false;
                    st.Sent = false;

                    bool parseOk = true;

                    try
                    {
                        switch (em.BlockType)
                        {
                            case EventMap.ReplyBlockType.JobDataRequest:
                                parseOk = HandleJobDataRequestReplyBlock(em);
                                break;

                        case EventMap.ReplyBlockType.None:
                        default:
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        parseOk = false;
                        Log($"{em.Name} Reply parse failed: {ex.Message}");
                    }

                    ClearEQEvent(em);

                    if (em.Name == "CVDataReport")
                    {
                        _periodicDataReportStage = PeriodicDataReportStage.Idle;
                        Log("[CV_DATA_REPORT] reply=OK request=OFF");
                        SendUTDataReport();
                    }
                    else if (em.Name == "UTDataReport")
                    {
                        _periodicDataReportStage = PeriodicDataReportStage.Idle;
                        Log("[UT_DATA_REPORT] reply=OK request=OFF");
                    }

                    if (!parseOk)
                    {
                        Log($"{em.Name} Reply received, but parse failed; T2 wait cleared.");
                        if (em.BlockType == EventMap.ReplyBlockType.JobDataRequest &&
                            _jobDataRequestFromAoi)
                            SendJobDataRequestFailure(_jobDataRequestJobId,
                                                      "reply_parse_failed");
                    }

                    if (em.BlockType == EventMap.ReplyBlockType.JobDataRequest)
                    {
                        _jobDataRequestFromAoi = false;
                        _jobDataRequestJobId = "";
                    }
                }
            }
        }

        private bool HandleJobDataRequestReplyBlock(EventMap em)
        {
            int[] rv = ReadWordArray(em.ReplyTag);
            if (rv == null)
            {
                Log("JobDataRequestReply parse failed: source is null");
                return false;
            }

            int srcIndex = GetJobDataRequestReplySourceIndex(rv);

            if (!HasEnoughWords(rv, srcIndex, JobDataRequestReplyJobWords, "JobDataRequestReply JobData"))
                return false;

            int ackIndex = srcIndex == JobDataRequestReplyBlockOffset
                ? JobDataRequestReplyAckOffset
                : srcIndex + JobDataRequestReplyJobWords;

            if (!HasEnoughWords(rv, ackIndex, 1, "JobDataRequestReply Ack"))
                return false;

            // 1) JobData = 150 WORD（PLC格式）
            int[] jobWords = new int[JobDataRequestReplyJobWords];
            Array.Copy(rv, srcIndex, jobWords, 0, JobDataRequestReplyJobWords);

            // 2) Ack = 1 WORD
            int ack = rv[ackIndex];

            // 3) Reserved (20 WORD) 可忽略
            // int[] reserved = new int[20];
            // Array.Copy(rv, baseWord + 151, reserved, 0, 20);

            Log($"JobDataRequestReplyBlock: source.Length={rv.Length}, srcIndex={srcIndex}, Ack={ack}");

            // 4) 发送给 C++
            return SendToCpp("JobDataRequestReply", new
            {
                ack = ack,
                length = JobDataRequestReplyJobWords,
                job = jobWords,
                requestJobId = _jobDataRequestJobId,
                reason = ""
            });
        }

        private int GetJobDataRequestReplySourceIndex(int[] source)
        {
            if (source.Length >= JobDataRequestReplyMinWords)
                return JobDataRequestReplyBlockOffset;

            if (source.Length == JobDataRequestReplyBlockWords)
                return 0;

            return source.Length >= CimToEqDataWords
                ? JobDataRequestReplyBlockOffset
                : 0;
        }

        private bool HasEnoughWords(int[] source, int srcIndex, int copyLength, string context)
        {
            int expectedMinLength = srcIndex + copyLength;
            if (source.Length >= expectedMinLength)
                return true;

            Log($"{context} parse failed: source.Length={source.Length}, srcIndex={srcIndex}, copyLength={copyLength}, expectedMinLength={expectedMinLength}");
            return false;
        }




        void ClearEQEvent(EventMap em)
        {
            int[] data = ReadWordArray(em.EQTag);
            data[em.EQWord] &= ~(1 << em.EQBit);
            WriteWordArray(em.EQTag, data);

            Log($"{em.Name} = 0 (复位)");
        }

        void ProcessCommand(string tagName, string cmdName, int[] rv)
        {
            var map = CmdMapping[cmdName];
            var st = CmdStates[cmdName];

            if (map.CimTag != tagName)
                return;

            bool cur = GetBit(rv, map.CimWord, map.CimBit);

            // ===== ON 边沿（0→1）=====
            if (!st.Last && cur)
            {
                st.WaitingOff = true;
                st.Deadline = DateTime.Now.AddSeconds(T1);

                if (cmdName == "RecipeParameterRequestCommand")
                {
                    bool sent = cmdHandler.HandleRecipeParameterRequestCommand();
                    Log(sent
                        ? $"{cmdName} ON -> waiting main software reply"
                        : $"{cmdName} ON -> failed to notify main software");
                }
                else
                {
                    cmdHandler.Handle(cmdName);
                    WriteReply(map, true);
                    Log($"{cmdName} ON → Reply ON");
                }
            }

            // ===== OFF 边沿（1→0）=====
            if (st.Last && !cur)
            {
                if (st.WaitingOff)
                {
                    WriteReply(map, false);
                    Log($"{cmdName} OFF → Reply OFF");
                }

                st.WaitingOff = false;
            }

            st.Last = cur;
        }
        void WriteReply(CmdMap map, bool value)
        {
            int[] data = ReadWordArray(map.ReplyTag);

            if (value)
                data[map.ReplyWord] |= (1 << map.ReplyBit);
            else
                data[map.ReplyWord] &= ~(1 << map.ReplyBit);

            WriteWordArray(map.ReplyTag, data);
        }

        public bool CompleteRecipeParameterRequestCommandReply(
            int recipeNumber,
            int versionYear,
            int versionMonth,
            int versionDay,
            int versionHour,
            int versionMinute,
            int versionSecond,
            int unitNumber,
            int recipeStepNumber,
            int result)
        {
            const string commandName = "RecipeParameterRequestCommand";
            const string tag = "SD_EQToCIM_RecipeData_03_01_00";
            const int baseWord = 67;

            CmdState state;
            if (!CmdStates.TryGetValue(commandName, out state) || !state.WaitingOff)
            {
                Log("RecipeParameterRequestCommandReply ignored: command is not pending");
                return false;
            }

            int[] data = ReadWordArray(tag);
            if (data == null || data.Length < baseWord + 12)
            {
                Log("RecipeParameterRequestCommandReply: read RecipeData failed");
                return false;
            }

            data[baseWord + 0] = recipeNumber;
            data[baseWord + 1] = versionYear;
            data[baseWord + 2] = versionMonth;
            data[baseWord + 3] = versionDay;
            data[baseWord + 4] = versionHour;
            data[baseWord + 5] = versionMinute;
            data[baseWord + 6] = versionSecond;
            data[baseWord + 7] = unitNumber;
            data[baseWord + 8] = recipeStepNumber;
            data[baseWord + 9] = result;
            data[baseWord + 10] = 0;
            data[baseWord + 11] = 0;
            data[1] |= 1 << 0;

            if (!WriteWordArray(tag, data))
            {
                Log("RecipeParameterRequestCommandReply: write RecipeData failed");
                return false;
            }

            Log($"RecipeParameterRequestCommandReply ON: RecipeNumber={recipeNumber}, Result={result}");
            return true;
        }

        void timerT1_Tick(object sender, EventArgs e)
        {
            foreach (var kv in CmdStates)
            {
                string cmdName = kv.Key;
                var st = kv.Value;
                var map = CmdMapping[cmdName];

                if (!st.WaitingOff) continue;

                if (DateTime.Now > st.Deadline)
                {
                    WriteReply(map, false);
                    Log($"{cmdName} Timeout T1 → Reply OFF");
                    // ★ 发送超时报警
                    RaiseTimeoutAlarm(true, $"Command {cmdName} Timeout (T1)");
                    st.WaitingOff = false;
                }
            }
        }

        private void timerT2_Tick(object sender, EventArgs e)
        {
            foreach (var kv in EventStates)
            {
                string name = kv.Key;
                EQEventState st = kv.Value;
                var em = EventMapping[name];

                if (!st.WaitingReply) continue;

                if (DateTime.Now > st.Deadline)
                {
                    Log($"{name} 等待 Reply 超时 → 自动清零");
                    // ★ 触发超时报警
                    RaiseTimeoutAlarm(false, $"Event {name} Timeout (T2)");
                    ClearEQEvent(em);
                    st.WaitingReply = false;
                    st.Sent = false;
                    if (name == "CVDataReport")
                    {
                        _periodicDataReportStage = PeriodicDataReportStage.Idle;
                        Log("[CV_DATA_REPORT] timeout request=OFF");
                        SendUTDataReport();
                    }
                    else if (name == "UTDataReport")
                    {
                        _periodicDataReportStage = PeriodicDataReportStage.Idle;
                        Log("[UT_DATA_REPORT] timeout request=OFF");
                    }
                    if (em.BlockType == EventMap.ReplyBlockType.JobDataRequest)
                    {
                        if (_jobDataRequestFromAoi)
                            SendJobDataRequestFailure(_jobDataRequestJobId,
                                                      "central_reply_timeout");
                        _jobDataRequestFromAoi = false;
                        _jobDataRequestJobId = "";
                    }
                }
            }
        }
        private void timerHeartbeat_Tick(object sender, EventArgs e)
        {
            const string tag = "SD_EQToCIM_Data01_03_01_00";

            int[] data = ReadWordArray(tag);
            if (data == null) return;

            heartbeatFlag = !heartbeatFlag;   // 翻转 ON/OFF

            // ★ 写 MachineAlive Bit = Word[0], Bit4
            if (heartbeatFlag)
                data[0] |= (1 << 4);
            else
                data[0] &= ~(1 << 4);

            WriteWordArray(tag, data);

            //Log($"[Heartbeat] MachineAlive Bit4 = {(heartbeatFlag ? 1 : 0)}");
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            if (LastEQStatus == EQStatus.Run)
            {
                // 距离最后一次 run 1 分钟未收到新的 run → 降级 idle
                if ((DateTime.Now - lastRunTime).TotalSeconds > 60)
                {
                    ChangeEQStatus(EQStatus.Idle, "NW", "NW", 0);
                }
            }
        }
        private void TimerCV_Tick(object sender, EventArgs e)
        {
            if (!cimModeEnabled) return;
            if (CVIntervalHours <= 0) return;
            if (_periodicDataReportStage != PeriodicDataReportStage.Idle) return;

            if (lastCVSendTime == DateTime.MinValue)
            {
                lastCVSendTime = DateTime.Now;
                return;
            }

            if ((DateTime.Now - lastCVSendTime).TotalHours >= CVIntervalHours)
            {
                lastCVSendTime = DateTime.Now;

                Log($"[CV] 到达上报间隔 {CVIntervalHours} 小时 → 启动 CV/UT 串行上报");
                SendCVDataReport();
            }
        }

        private bool SendCVDataReport()
        {
            const int temperature = 25;
            const int humidity = 60;

            if (!WriteCvDataBlock(temperature, humidity))
            {
                Log("[CV_DATA_REPORT] block write failed; request remains OFF");
                return false;
            }

            bool sent = SendEventAndBlock("CVDataReport", data => { });
            if (!sent)
            {
                Log("[CV_DATA_REPORT] request send failed; request remains OFF");
                return false;
            }

            EventStates["CVDataReport"].Deadline = DateTime.Now.AddSeconds(T2);
            _periodicDataReportStage = PeriodicDataReportStage.CvPending;
            Log("[CV_DATA_REPORT] request=ON data=TEMP=25;HUMI=60 " +
                "request_bit=W0.B1 reply_bit=W3.B15");
            return true;
        }

        private bool SendUTDataReport()
        {
            if (!cimModeEnabled)
            {
                _periodicDataReportStage = PeriodicDataReportStage.Idle;
                Log("[UT_DATA_REPORT] skipped because CIM mode is OFF");
                return false;
            }

            if (!EventMapping.TryGetValue("UTDataReport", out var em) ||
                !EventStates.TryGetValue("UTDataReport", out var st))
            {
                _periodicDataReportStage = PeriodicDataReportStage.Idle;
                Log("[UT_DATA_REPORT] event mapping unavailable");
                return false;
            }

            int[] data = ReadWordArray(em.EQTag);
            if (data == null || data.Length < 173)
            {
                _periodicDataReportStage = PeriodicDataReportStage.Idle;
                Log("[UT_DATA_REPORT] Data01 read failed or is shorter than W172");
                return false;
            }

            WriteUtDataBlock(data);
            data[em.EQWord] |= (1 << em.EQBit);

            if (!WriteWordArray(em.EQTag, data))
            {
                _periodicDataReportStage = PeriodicDataReportStage.Idle;
                Log("[UT_DATA_REPORT] block/request write failed");
                return false;
            }

            st.Sent = true;
            st.WaitingReply = true;
            st.Deadline = DateTime.Now.AddSeconds(T2);
            _periodicDataReportStage = PeriodicDataReportStage.UtPending;
            Log("[UT_DATA_REPORT] request=ON block=W133-W172 " +
                "values=0,0,0,0,0,0,0,0,0,0 " +
                "request_bit=W0.B12 reply_bit=W1.B13");
            return true;
        }

        private static void WriteUtDataBlock(int[] data)
        {
            const int baseWord = 133;
            const int blockWords = 40;

            // W133-134 WaterDIW, W135-136 GasCDA, W137-138 GasN2
            // W139-140 ElectricityGPS, W141-142 ElectricityUPS
            // W143-144 WaterDIWTotal, W145-146 GasCDATotal
            // W147-148 GasN2Total, W149-150 ElectricityGPSTotal
            // W151-152 ElectricityUPSTotal, W153-172 Reserved.
            // All test values are zero, so no 32-bit word-order assumption is needed.
            Array.Clear(data, baseWord, blockWords);
        }

        private bool WriteCvDataBlock(int temperature, int humidity)
        {
            const string blockTag = "BC_EQToCIM_CVData_03_01_00";

            int[] block = ReadWordArray(blockTag);
            if (block == null || block.Length < 210)
            {
                Log("[CV] ❌ 无法读取 CV 数据 Block");
                return false;
            }

            // ====================================================
            // 1) 构造 ASCII 字符串
            // ====================================================
            string text = $"TEMP={temperature};HUMI={humidity}";
            byte[] bytes = Encoding.ASCII.GetBytes(text);

            // ====================================================
            // 2) 清零 210 WORD，再按原有 lo/hi 规则打包 ASCII
            // ====================================================
            Array.Clear(block, 0, 210);
            for (int i = 0; i < bytes.Length; i += 2)
            {
                byte lo = bytes[i];
                byte hi = i + 1 < bytes.Length ? bytes[i + 1] : (byte)0;
                block[i / 2] = (hi << 8) | lo;
            }

            // 写回 PLC
            if (!WriteWordArray(blockTag, block))
            {
                Log("[CV] CVData Block 写入 PLC 失败");
                return false;
            }

            Log($"[CV] Block 写入温度/湿度成功 → {text.Trim()}");
            return true;
        }

        // ===============================
        // PLC 读写辅助
        // ===============================
        bool LinkSignalTag(string tag)
        {
            foreach (var lm in LinkMappings.Values)
                if (lm.Tag == tag) return true;

            return false;
        }
        private int[] ReadWordArray(string tag)
        {
            try
            {
                object obj = variableCompolet1.ReadVariable(tag);
                if (obj == null)
                    return null;

                if (obj is int[]) return (int[])obj;

                if (obj is ushort[] us)
                {
                    int[] arr = new int[us.Length];
                    for (int i = 0; i < us.Length; i++) arr[i] = us[i];
                    return arr;
                }

                if (obj is short[] ss)
                {
                    int[] arr = new int[ss.Length];
                    for (int i = 0; i < ss.Length; i++) arr[i] = ss[i];
                    return arr;
                }

                return null;
            }
            catch (Exception ex)
            {
                LocalFileLogger.Error("PLC", "ReadVariable failed tag=" + tag + " err=" + ex.Message);
                Log("ReadVariable 错误: " + ex.Message);
                return null;
            }
        }

        private bool GetBit(int[] words, int wi, int bi)
        {
            return (words[wi] & (1 << bi)) != 0;
        }

        private void SetBit(int[] words, int wi, int bi, bool on)
        {
            if (on)
                words[wi] |= (1 << bi);
            else
                words[wi] &= ~(1 << bi);
        }
        public bool WriteWordArray(string tagName, int[] value)
        {
            try
            {
                // Compolet 默认以 int[] 当成 WORD 数组处理
                variableCompolet1.WriteVariable(tagName, value);
                return true;
            }
            catch (Exception ex)
            {
                LocalFileLogger.Error("PLC", "WriteVariable failed tag=" + tagName + " err=" + ex.Message);
                Log($"WriteWordArray 写入失败，Tag={tagName}, Err={ex.Message}");
                return false;
            }
        }
        public bool SendToCpp(string cmd, object payload)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                cmd = cmd,
                data = payload
            });

            bool ok = _tcpServer.SendToClient(json);

            if (!ok)
                Log($"[Form1 → C++] 发送失败: cmd={cmd}");
            else
                Log($"[Form1 → C++] 已发送: {cmd}");

            return ok;
        }
        private void RaiseTimeoutAlarm(bool isT1, string detail)
        {
            int alarmId = 23;
            int alarmType = 1;
            int alarmUnitNumber = 0;
            int alarmStatus = 1;

            // ★ ReasonCode: T1=13,  T2=14
            int reasonCode = isT1 ? 13 : 14;
            int subReasonCode = 0;

            // ★ Alarm Text（最多 50 字节）
            string alarmText = detail;
            if (alarmText.Length > 50)
                alarmText = alarmText.Substring(0, 50);

            alarmText = alarmText.PadRight(50, ' ');

            SendEventAndBlock("AlarmReport", data =>
            {
                const int baseWord = 98;

                data[baseWord + 0] = alarmId;
                data[baseWord + 1] = alarmType;
                data[baseWord + 2] = alarmUnitNumber;
                data[baseWord + 3] = alarmStatus;
                data[baseWord + 4] = reasonCode;
                data[baseWord + 5] = subReasonCode;

                // ===== Alarm Text（25 WORD，50 字节）=====
                byte[] txtBytes = Encoding.ASCII.GetBytes(alarmText);
                for (int i = 0; i < 25; i++)
                {
                    byte c1 = txtBytes[i * 2];
                    byte c2 = txtBytes[i * 2 + 1];
                    data[baseWord + 6 + i] = (c2 << 8) | c1;
                }
            });

            Log($"[EQ→CIM] 发送 Timeout Alarm: {(isT1 ? "T1" : "T2")} - {detail}");
        }

        /// <summary>
        /// 当 EQ↔EQ 开关变化时，写入 4 个联机信号
        /// </summary>
        private void ApplyEq2EqLinkSignals(bool enabled)
        {
            // 信号列表
            var targets = new (string tag, int word, int bit)[]
            {
        ("SD_EQToEQ_LinkSignal_03_02_00", 3, 0),
        ("SD_EQToEQ_LinkSignal_03_04_00", 0, 0),
        ("SD_EQToCIM_Data01_03_01_00", 0, 0),
        ("SD_EQToCIM_Data01_03_01_00", 0, 1),
            };

            foreach (var t in targets)
            {
                int[] words = ReadWordArray(t.tag);
                if (words == null)
                {
                    Log($"❌ 无法读取 {t.tag}");
                    continue;
                }

                if (enabled)
                    words[t.word] |= (1 << t.bit);
                else
                    words[t.word] &= ~(1 << t.bit);

                if (WriteWordArray(t.tag, words))
                {
                    Log($"✔ 设置 {t.tag}[{t.word}].bit{t.bit} = {(enabled ? "1" : "0")}");
                }
                else
                {
                    Log($"❌ 写入失败 {t.tag}");
                }
            }
        }
        /// <summary>
        /// 上报 AutoRecipeChangeModeReport（1=开启，2=关闭）
        /// </summary>
        private void RaiseAutoRecipeChangeModeReport(bool enabled)
        {
            int mode = enabled ? 1 : 2;   // 协议：1=ON, 2=OFF

            SendEventAndBlock("AutoRecipeChangeModeReport", data =>
            {
                const int baseWord = 2;   // Block 第1 WORD（Word[1]）

                data[baseWord] = mode;    // 写入 1 或 2
            });

            Log($"[EQ→CIM] AutoRecipeChangeModeReport 已发送 → Mode={(enabled ? "开启(1)" : "关闭(2)")}");
        }
        public void SetCimMode(bool enabled)
        {
            cimModeEnabled = enabled;
            if (chkCIMMode.InvokeRequired)
            {
                chkCIMMode.Invoke(new Action(() => chkCIMMode.Checked = enabled));
            }
            else
            {
                chkCIMMode.Checked = enabled;
            }
            // 2) 写 Reply（SD_EQToCIM_Data01_03_01_00 word[0].bit2）
            // ============================
            int[] sd = ReadWordArray("SD_EQToCIM_Data01_03_01_00");
            if (sd != null)
            {
                if (enabled)
                    sd[0] |= (1 << 2);   // ON →1
                else
                    sd[0] &= ~(1 << 2);  // OFF→0

                WriteWordArray("SD_EQToCIM_Data01_03_01_00", sd);
                Log($"[EQ→CIM] CIMMode  = {(enabled ? 1 : 0)}");
            }

            // ============================
            // 3) 当 CIM Mode = ON → 自动触发两个上报事件
            // ============================
            if (enabled)
            {
                // ---- MachineModeChangeReport ----
                SendEventAndBlock("MachineModeChangeReport", data =>
                {
                    const int baseWord = 65;
                    data[baseWord] = (int)_currentMachineMode;
                });

                // ---- VCRStatusReport ----
                SendToCpp("VCRStatusReport", new { });


                // 使用 EQ 当前记录的状态值（一级 + 二级 + 三级 + Alarm）
                SendEventAndBlock("MachineStatusChangeReport", data =>
                {
                    const int baseWord = 3;

                    data[baseWord + 0] = (int)LastEQStatus;
                    data[baseWord + 1] = LastAlarmId;
                    data[baseWord + 2] = EncodeAsciiWord(LastReason);
                    data[baseWord + 3] = EncodeAsciiWord(LastSub);
                });

            }
        }
        public void SetMachineMode(MachineMode mode)
        {
            if (_currentMachineMode == mode)
                return;

            _currentMachineMode = mode;
       
            if (!CimModeEnabled)
                return;

            SendEventAndBlock("MachineModeChangeReport", data =>
            {
                const int baseWord = 65;
                data[baseWord] = (int)_currentMachineMode;
            });
        }
        public void ChangeEQStatus(EQStatus newStatus, string reason, string sub, int alarmId)
        {
            // 1. RUN → 记录 run 时间
            if (newStatus == EQStatus.Run)
                lastRunTime = DateTime.Now;

            // 2. 状态是否真的变化？
            bool changed = (newStatus != LastEQStatus);

            // 3. 始终更新 reason/sub/alarmId（即便状态未变也更新）
            LastReason = reason;
            LastSub = sub;
            LastAlarmId = alarmId;

            if (!changed)
            {
                //Log($"[EQ] 状态未改变：保持 {newStatus}");
                return;
            }

     
            LastEQStatus = newStatus;

            // 4. CIM OFF → 不上报
            if (!CimModeEnabled)
            {
                Log("[EQ] CIM Mode OFF，状态不发送给 CIM");
                return;
            }

            // 5. 上报给 CIM
            SendEventAndBlock("MachineStatusChangeReport", data =>
            {
                const int baseWord = 3;
                data[baseWord + 0] = (int)newStatus;
                data[baseWord + 1] = alarmId;
                data[baseWord + 2] = EncodeAsciiWord(reason);
                data[baseWord + 3] = EncodeAsciiWord(sub);
            });

            Log($"[EQ→CIM] 上报机器状态: {newStatus}, Reason={reason}, Sub={sub}, Alarm={alarmId}");
        }

        private int EncodeAsciiWord(string code)
        {
            if (string.IsNullOrEmpty(code))
                return 0;

            code = code.PadRight(2, ' ');
            var b = Encoding.ASCII.GetBytes(code.Substring(0, 2));

            byte c1 = b[0];  // 第1字符
            byte c2 = b[1];  // 第2字符

            return (c2 << 8) | c1;  // ★ lo=第1字符, hi=第2字符
        }
      

        // ===============================
        // 日志
        // ===============================

        private void Log(string msg)
        {
            if (this.InvokeRequired)
            {
                // ⭐ 让后台线程回到 UI 线程执行 Log
                this.BeginInvoke(new Action<string>(Log), msg);
                return;
            }

            // ⭐ 已经在 UI 线程，安全写入 txtLog
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
        }

        private void UpdateState()
        {
            lblState.Text = $"状态: {_state}";
        }
        int[] IEqContext.ReadWordArray(string tag) => ReadWordArray(tag);
        bool IEqContext.WriteWordArray(string tag, int[] value) => WriteWordArray(tag, value);
        void IEqContext.TriggerEvent(string name) => TriggerEvent(name);
        void IEqContext.Log(string msg) => Log(msg);
        bool IEqContext.SendEventAndBlock(string eventName, Action<int[]> writer)
            => SendEventAndBlock(eventName, writer);
    }

    }

