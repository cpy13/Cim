using OMRON.Compolet.Variable;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EQModeChangeSimulator
{
    public class SignalMonitorForm : Form
    {
        private readonly VariableCompolet _compolet;
        private Timer _timer;
        private readonly Dictionary<string, CheckBox> _checkBoxMap = new();
        private readonly Dictionary<string, int[]> _lastSnapshot = new();
        private readonly Dictionary<string, LedLamp> _lampMap = new();
        private readonly EqTcpServer _eqServer;
        // ===============================
        // Signal definition（UI / PLC 解耦）
        // ===============================
        class SignalDef
        {
            public string UiGroup;           // UI 一级分组
            public string UiSubGroup;        // UI 二级分组
            public string DisplayName;       // 界面显示名

            public string Tag;               // PLC Tag
            public int Word;
            public int Bit;
            public bool Writable;       //是否允许写 PLC
        }

        // ===============================
        // 协议映射
        // ===============================
        private readonly List<SignalDef> Signals = new()
        {
            // ================= 心跳 =================
            new SignalDef{
                UiGroup="心跳",
                UiSubGroup="CIM",
                DisplayName="CIM 心跳",
                Tag="RV_CIMToEQ_BCAlive_01_03_00",
                Word=0, Bit=0
            },
            new SignalDef{
                UiGroup="心跳",
                UiSubGroup="设备",
                DisplayName="设备心跳",
                Tag="SD_EQToCIM_Data01_03_01_00",
                Word=0, Bit=4
            },

            // ================= 上游通讯 =================
            // 上游 → 本机（RV）
             new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="上游 → 本机",
                DisplayName="Upstream In line",
                Tag="RV_EQToEQ_LinkSignal_02_03_00",
                Word=0, Bit=0
            },
              new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="上游 → 本机",
                DisplayName="Job Transfer Signal",
                Tag="RV_EQToEQ_LinkSignal_02_03_00",
                Word=0, Bit=2
            },
            new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="上游 → 本机",
                DisplayName="Send Able",
                Tag="RV_EQToEQ_LinkSignal_02_03_00",
                Word=0, Bit=3
            },
            new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="上游 → 本机",
                DisplayName="Send Start",
                Tag="RV_EQToEQ_LinkSignal_02_03_00",
                Word=0, Bit=4
            },
         
            new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="上游 → 本机",
                DisplayName="Send Complete",
                Tag="RV_EQToEQ_LinkSignal_02_03_00",
                Word=0, Bit=5
            },
              new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="上游 → 本机",
                DisplayName="Conveyer State",
                Tag="RV_EQToEQ_LinkSignal_02_03_00",
                Word=0, Bit=11
            },
                new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="上游 → 本机",
                DisplayName="Glass Exist Arm#1",
                Tag="RV_EQToEQ_LinkSignal_02_03_00",
                Word=1, Bit=0
            },

            // 本机 → 上游（SD）
              new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="本机 → 上游",
                DisplayName="Downstream In line",
                Tag="SD_EQToEQ_LinkSignal_03_02_00",
                Word=3, Bit=0
            },
                 new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="本机 → 上游",
                DisplayName="Job Transfer Signal",
                Tag="SD_EQToEQ_LinkSignal_03_02_00",
                Word=3, Bit=2
            },
            new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="本机 → 上游",
                DisplayName="Receive Able",
                Tag="SD_EQToEQ_LinkSignal_03_02_00",
                Word=3, Bit=3,
                 Writable=true
            },
            new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="本机 → 上游",
                DisplayName="Receive Start",
                Tag="SD_EQToEQ_LinkSignal_03_02_00",
                Word=3, Bit=4
            },
            new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="本机 → 上游",
                DisplayName="Receive Complete",
                Tag="SD_EQToEQ_LinkSignal_03_02_00",
                Word=3, Bit=5,
                
            },
              new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="本机 → 上游",
                DisplayName="Conveyer State",
                Tag="SD_EQToEQ_LinkSignal_03_02_00",
                Word=3, Bit=11
            },
                new SignalDef{
                UiGroup="上游通讯",
                UiSubGroup="本机 → 上游",
                DisplayName="Glass Exist Arm#1",
                Tag="SD_EQToEQ_LinkSignal_03_02_00",
                Word=4, Bit=0
            },

            // ================= 下游通讯 =================
             // 本机 → 下游（SD）
              new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="本机 → 下游",
                DisplayName="Upstream In line",
                Tag="SD_EQToEQ_LinkSignal_03_05_00",
                Word=0, Bit=0
            },
              new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="本机 → 下游",
                DisplayName="Job Transfer Signal",
                Tag="SD_EQToEQ_LinkSignal_03_05_00",
                Word=0, Bit=2
            },

            new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="本机 → 下游",
                DisplayName="Send Able",
                Tag="SD_EQToEQ_LinkSignal_03_05_00",
                Word=0, Bit=3,
                Writable=true
            },
            new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="本机 → 下游",
                DisplayName="Send Start",
                Tag="SD_EQToEQ_LinkSignal_03_05_00",
                Word=0, Bit=4
            },
            new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="本机 → 下游",
                DisplayName="Send Complete",
                Tag="SD_EQToEQ_LinkSignal_03_05_00",
                Word=0, Bit=5,
                Writable=true

            },
             new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="本机 → 下游",
                DisplayName="Conveyer State",
                Tag="SD_EQToEQ_LinkSignal_03_05_00",
                Word=0, Bit=11
            },
              new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="本机 → 下游",
                DisplayName="Glass Exist Arm#1",
                Tag="SD_EQToEQ_LinkSignal_03_05_00",
                Word=4, Bit=0
            },
            // 下游 → 本机（RV）
             new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="下游 → 本机",
                DisplayName="Downstream In line",
                Tag="RV_EQToEQ_LinkSignal_05_03_00",
                Word=3, Bit=0
            },
              new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="下游 → 本机",
                DisplayName="Job Transfer Signal",
                Tag="RV_EQToEQ_LinkSignal_05_03_00",
                Word=3, Bit=2
            },
            new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="下游 → 本机",
                DisplayName="Receive Able",
                Tag="RV_EQToEQ_LinkSignal_05_03_00",
                Word=3, Bit=3
            },
            new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="下游 → 本机",
                DisplayName="Receive Start",
                Tag="RV_EQToEQ_LinkSignal_05_03_00",
                Word=3, Bit=4
            },
            new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="下游 → 本机",
                DisplayName="Receive Complete",
                Tag="RV_EQToEQ_LinkSignal_05_03_00",
                Word=3, Bit=5
            },
              new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="下游 → 本机",
                DisplayName="Conveyer State",
                Tag="RV_EQToEQ_LinkSignal_05_03_00",
                Word=3, Bit=11
            },
               new SignalDef{
                UiGroup="下游通讯",
                UiSubGroup="下游 → 本机",
                DisplayName="Glass Exist Arm#1",
                Tag="RV_EQToEQ_LinkSignal_05_03_00",
                Word=4, Bit=0
            },

           
        };

        // UiGroup -> 主容器
        private readonly Dictionary<string, TableLayoutPanel> _uiGroupTables = new();

        public SignalMonitorForm(VariableCompolet compolet, EqTcpServer eqServer)
        {
            _compolet = compolet;
            _eqServer = eqServer;
            InitializeComponent();
            InitTimer();
        }

        // ===============================
        // UI
        // ===============================
        private void InitializeComponent()
        {
            this.Text = "EQ 通讯信号监控";
            this.Size = new Size(1200, 850);
            this.MinimumSize = new Size(950, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            this.Controls.Add(main);

            CreateUiGroup(main, "心跳");
            CreateUiGroup(main, "上游通讯");
            CreateUiGroup(main, "下游通讯");

            BuildSignalUi();
            var btnReset = new Button
            {
                Text = "清空信号 / EQ复位",
                Height = 36,
                Dock = DockStyle.Bottom,
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };

            btnReset.Click += (_, __) =>
            {
                if (MessageBox.Show(
            "确定要清板并清空信号？",
            "EQ Reset Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                // 清 EQ → 上游
                ClearSendSignals(Signals.FindAll(s => s.UiSubGroup == "本机 → 上游"));
                // 清 EQ → 下游
                ClearSendSignals(Signals.FindAll(s => s.UiSubGroup == "本机 → 下游"));
                // 通知 C++ 复位
                SendEqResetCommand();
            };

            this.Controls.Add(btnReset);
        }

        private void CreateUiGroup(TableLayoutPanel parent, string groupName)
        {
            GroupBox box = new GroupBox
            {
                Text = groupName,
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            box.Controls.Add(table);
            parent.Controls.Add(box);

            _uiGroupTables[groupName] = table;
        }

        // ===============================
        // 构建二级分组 UI
        // ===============================
        private void BuildSignalUi()
        {
            foreach (var group in _uiGroupTables.Keys)
            {
                var subGroups = new Dictionary<string, List<SignalDef>>();

                foreach (var s in Signals)
                {
                    if (s.UiGroup != group) continue;
                    if (!subGroups.ContainsKey(s.UiSubGroup))
                        subGroups[s.UiSubGroup] = new List<SignalDef>();
                    subGroups[s.UiSubGroup].Add(s);
                }

                var table = _uiGroupTables[group];
                int index = 0;

                foreach (var kv in subGroups)
                {
                    int col = index % 2;
                    int row = index / 2;

                    if (table.RowCount <= row)
                    {
                        table.RowCount++;
                        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    }

                    table.Controls.Add(CreateSubGroupBox(kv.Key, kv.Value), col, row);
                    index++;
                }
            }
        }

        private GroupBox CreateSubGroupBox(string title, List<SignalDef> list)
        {
            GroupBox box = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // === 信号表格 ===
            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            int rows = (int)Math.Ceiling(list.Count / 2.0);
            table.RowCount = rows;

            for (int i = 0; i < rows; i++)
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            for (int i = 0; i < list.Count; i++)
            {
                int col = i / rows;
                int row = i % rows;
                table.Controls.Add(CreateSignalRow(list[i]), col, row);
            }

            root.Controls.Add(table, 0, 0);

            // === 是否需要按钮 ===
            //if (title.Contains("本机 → 上游") || title.Contains("本机 → 下游"))
            //{
            //    var btn = new Button
            //    {
            //        Text = "清空 Send 信号",
            //        Dock = DockStyle.Right,
            //        Width = 120
            //    };

            //    btn.Click += (_, __) => ClearSendSignals(list);

            //    Panel btnPanel = new Panel { Dock = DockStyle.Fill, Height = 35 };
            //    btnPanel.Controls.Add(btn);
            //    root.Controls.Add(btnPanel, 0, 1);
            //}

            box.Controls.Add(root);
            return box;
        }

        private void ClearSendSignals(List<SignalDef> list)
        {
            // 1️⃣ 按 Tag 分组
            var groups = new Dictionary<string, List<SignalDef>>();

            foreach (var s in list)
            {
                // ❌ 不清 In line
                if (s.DisplayName.Contains("In line")) continue;

                // 只清 Send / Receive / Job / Conveyer / Glass
                if (!groups.ContainsKey(s.Tag))
                    groups[s.Tag] = new List<SignalDef>();

                groups[s.Tag].Add(s);
            }

            // 2️⃣ 批量写 PLC
            foreach (var kv in groups)
            {
                string tag = kv.Key;
                int[] words = ReadTag(tag);
                if (words == null) continue;

                foreach (var s in kv.Value)
                {
                    words[s.Word] &= ~(1 << s.Bit); // 强制 OFF
                }

                _compolet.WriteVariable(tag, words);
                _lastSnapshot[tag] = (int[])words.Clone();
                UpdateLamps(tag, words);
            }
        }

        private Control CreateSignalRow(SignalDef s)
        {
            Panel row = new Panel { Height = 30, Dock = DockStyle.Fill };

            var lamp = new LedLamp { Location = new Point(8, 7) };

            Label lbl = new Label
            {
                Text = s.DisplayName,
                Location = new Point(36, 6),
                AutoSize = true,
                Font = new Font("微软雅黑", 9)
            };

            row.Controls.Add(lamp);
            row.Controls.Add(lbl);

            _lampMap[$"{s.Tag}:{s.Word}:{s.Bit}"] = lamp;

            if (s.Writable)
            {
                CheckBox cb = new CheckBox
                {
                    Location = new Point(220, 6),
                    Width = 20
                };

                cb.CheckedChanged += (_, __) =>
                {
                 
                    if (cb.Tag as string == "PLC_SYNC") return;

                    WriteSignalBit(s, cb.Checked);
                };

            
                _checkBoxMap[$"{s.Tag}:{s.Word}:{s.Bit}"] = cb;

                row.Controls.Add(cb);
            }

            return row;
        }
        private void WriteSignalBit(SignalDef s, bool on)
        {
            int[] words = ReadTag(s.Tag);
            if (words == null) return;

            if (on)
                words[s.Word] |= (1 << s.Bit);
            else
                words[s.Word] &= ~(1 << s.Bit);

            _compolet.WriteVariable(s.Tag, words);

            _lastSnapshot[s.Tag] = (int[])words.Clone();
            UpdateLamps(s.Tag, words);

            Console.WriteLine(
                $"[UI WRITE] {s.DisplayName} => {(on ? "ON" : "OFF")}");

      
            if (on && IsReceiveAbleSignal(s))
            {
                HandleManualReceiveAble();
            }
        }
        private bool IsReceiveAbleSignal(SignalDef s)
        {
            return s.Tag == "SD_EQToEQ_LinkSignal_03_02_00"
                && s.Word == 3
                && s.Bit == 3;
        }
        private void HandleManualReceiveAble()
        {
            const string rvTag = "RV_EQToEQ_LinkSignal_02_03_00";

            int[] data = ReadTag(rvTag);
            if (data == null)
            {
                Console.WriteLine("❌ ManualReceiveAble: 无法读取 RV Job 数据");
                return;
            }

            if (data.Length < 156)
            {
                Console.WriteLine("❌ ManualReceiveAble: Job 数据长度不足（<156 WORD）");
                return;
            }

            // Word[6] ~ Word[155] → 150 words
            int[] job = new int[150];
            Array.Copy(data, 6, job, 0, 150);

           SendToCpp("SendReceiveAble", new
            {
                length = 150,
                job = job
            });

            Console.WriteLine("✅ ManualReceiveAble: Job 数据已发送给 C++");
        }

        // ===============================
        // PLC polling
        // ===============================
        private void InitTimer()
        {
            _timer = new Timer { Interval = 300 };
            _timer.Tick += (s, e) => PollPlc();
            _timer.Start();
        }

        private void PollPlc()
        {
            // ============================
            // ⭐【1】单独监控 CIM 心跳
            // ============================
            int[] hbWords = ReadTag("RV_CIMToEQ_BCAlive_01_03_00");
            if (hbWords != null && hbWords.Length > 0)
            {
                bool bit0 = (hbWords[0] & 1) != 0;
                Console.WriteLine(
                    $"[DEBUG][CIM HB] WORD0={hbWords[0]} BIT0={(bit0 ? 1 : 0)}");
            }
            else
            {
                Console.WriteLine("[DEBUG][CIM HB] ReadTag NULL");
            }
            // 第一次读取：初始化 UI
            foreach (var s in Signals)
            {
                if (!_lastSnapshot.ContainsKey(s.Tag))
                {
                    var first = ReadTag(s.Tag);
                    if (first != null)
                    {
                        _lastSnapshot[s.Tag] = (int[])first.Clone();
                        UpdateLamps(s.Tag, first);   // ⭐ 关键：初始化时刷新 UI
                    }
                }
            }

            // 后续轮询：只在变化时更新
            foreach (var tag in new HashSet<string>(_lastSnapshot.Keys))
            {
                int[] cur = ReadTag(tag);
                if (cur == null) continue;

                if (!Same(cur, _lastSnapshot[tag]))
                {
                    _lastSnapshot[tag] = (int[])cur.Clone();
                    UpdateLamps(tag, cur);
                }
            }
        }


        //private int[] ReadTag(string tag)
        //{
        //    try
        //    {
        //        object obj = _compolet.ReadVariable(tag);
        //        if (obj is int[] arr) return arr;

        //        if (obj is ushort[] us)
        //        {
        //            int[] r = new int[us.Length];
        //            for (int i = 0; i < us.Length; i++) r[i] = us[i];
        //            return r;
        //        }
        //    }
        //    catch { }
        //    return null;
        //}
        private int[] ReadTag(string tag)
        {
            try
            {
                object obj = _compolet.ReadVariable(tag);

                if (obj == null)
                {
                    Console.WriteLine($"[ReadTag] {tag} => NULL");
                    return null;
                }

                // ===== 单 WORD（★最关键★）=====
                if (obj is int v)
                {
                    //Console.WriteLine($"[ReadTag] {tag} => int {v}");
                    return new[] { v };
                }

                if (obj is ushort uv)
                {
                    //Console.WriteLine($"[ReadTag] {tag} => ushort {uv}");
                    return new[] { (int)uv };
                }

                // ===== WORD 数组 =====
                if (obj is int[] arr)
                {
                    //Console.WriteLine($"[ReadTag] {tag} => int[{arr.Length}]");
                    return arr;
                }

                if (obj is ushort[] us)
                {
                    int[] r = new int[us.Length];
                    for (int i = 0; i < us.Length; i++)
                        r[i] = us[i];

                    //Console.WriteLine($"[ReadTag] {tag} => ushort[{us.Length}]");
                    return r;
                }

                Console.WriteLine($"[ReadTag] {tag} => Unsupported type: {obj.GetType().FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReadTag][EX] {tag}: {ex.Message}");
            }

            return null;
        }

        private bool Same(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private void UpdateLamps(string tag, int[] words)
        {
            foreach (var s in Signals)
            {
                if (s.Tag != tag) continue;

                bool on = (words[s.Word] & (1 << s.Bit)) != 0;
                string key = $"{s.Tag}:{s.Word}:{s.Bit}";

                _lampMap[key].SetState(on);

                if (s.Writable && _checkBoxMap.TryGetValue(key, out var cb))
                {
                    cb.Tag = "PLC_SYNC";  
                    cb.Checked = on;
                    cb.Tag = null;
                }
            }
        }

        private void SendEqResetCommand()
        {
            SendToCpp("EQResetCommand", new
            {
                reason = "SignalMonitorReset",
                time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
        private bool SendToCpp(string cmd, object payload)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                cmd = cmd,
                data = payload
            });

            return _eqServer != null && _eqServer.SendToClient(json);
        }
    }

    // ===============================
    // 圆形 LED 指示灯
    // ===============================
    class LedLamp : Control
    {
        private bool _on;

        public LedLamp()
        {
            Size = new Size(16, 16);
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
        }

        public void SetState(bool on)
        {
            if (_on != on)
            {
                _on = on;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill = _on ? Color.LimeGreen : Color.FromArgb(200, 200, 200);
            Color border = _on ? Color.Green : Color.Gray;

            using (Brush b = new SolidBrush(fill))
                e.Graphics.FillEllipse(b, 1, 1, Width - 2, Height - 2);

            using (Pen p = new Pen(border))
                e.Graphics.DrawEllipse(p, 1, 1, Width - 2, Height - 2);
        }
    }
}
