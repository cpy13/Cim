using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using static EQModeChangeSimulator.Form1;

//C++命令解析器

namespace EQModeChangeSimulator
{
    /// <summary>
    /// EQ 程序的能力接口（由 Form1 提供）
    /// 用于解耦 Router 与 UI/PLC 层
    /// </summary>
    public interface IEqContext
    {
        int[] ReadWordArray(string tag);
        bool WriteWordArray(string tag, int[] value);
        void TriggerEvent(string eventName);
        void SendEventAndBlock(string eventName, Action<int[]> blockWriter);
        void Log(string msg);
    }

    /// <summary>
    /// 负责解析 C++ 发送的 JSON 指令
    /// 并转为 EQ → CIM 的事件 + Block 上报
    /// </summary>
    public class CppCommandRouter
    {
        private readonly IEqContext _ctx;

        public CppCommandRouter(IEqContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// 统一入口：解析 JSON 文本
        /// </summary>
        public void HandleRawJson(string jsonText)
        {
            try
            {
                var obj = JObject.Parse(jsonText);
                string cmd = (string)obj["cmd"] ?? "";

                switch (cmd)
                {
                    case "MachineStatusReport":
                        HandleMachineStatusReport(obj);
                        break;

                    // eq to cim：
                    case "RecipeChangeReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleRecipeChangeReport(obj);
                        break;
                    case "CurrentRecipeNumberChangeReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleCurrentRecipeNumberChangeReport(obj);
                        break;
                    case "CIMMessageConfirmReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleCIMMessageConfirmReport(obj);
                        break;
                    case "OperatorLoginRequest":
                        if (!Form1.Instance.CimModeEnabled) break;                     
                        HandleOperatorLoginRequest(obj);
                        break;
                    case "AlarmReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleAlarmReport(obj);
                        break;
                    case "TactTimeChangeReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleTactTimeChangeReport(obj);
                        break;
                    case "MachineAutoMode":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleMachineAutoMode(obj);
                        break;
                    case "JobManualMoveReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleJobManualMoveReport(obj);
                        break;
                    case "JobJudgeResultReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleJobJudgeResultReport(obj);
                        break;
                    case "VCRStatusReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleVCRStatusReport(obj);
                        break;
                    case "ReceivedJobReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleReceivedJobReport(obj);
                        break;
                    case "SentOutJobReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleSentOutJobReport(obj);
                        break;
                    case "DefectCodeReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleDefectCodeReport(obj);
                            break;
                    case "DVData":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleDVData(obj);
                        break;
                    case "MachineModeChangeReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleMachineModeChangeReport(obj);
                        break;
                    case "DVDataReport":
                        if (!Form1.Instance.CimModeEnabled) break;
                        HandleDVDataReport(obj);
                        break;

                    //eq to  eq
                    //作为下游发送上游
                    case "ReceiveAble":
                        if (!Form1.Instance.Eq2EqEnabled) break;                     
                        HandleReceiveAbleFromCpp(obj);
                        break;
                    case "ConveyerState":
                        if (!Form1.Instance.Eq2EqEnabled) break;
                        HandleConveyerStateFromCpp(obj);
                        break;
                    case "GlassExistArm1":
                        if (!Form1.Instance.Eq2EqEnabled) break;
                        HandleGlassExistArm1FromCpp(obj);
                        break;
                    case "JobTransferSignal":
                        if (!Form1.Instance.Eq2EqEnabled) break;
                        HandleJobTransferSignalFromCpp(obj);
                        break;
                    case "ReceiveComplete":
                        if (!Form1.Instance.Eq2EqEnabled) break;
                        HandleReceiveCompleteFromCpp(obj);
                        break;
                    //作为下游发送给上游
                    case "SendAble":
                        if (!Form1.Instance.Eq2EqEnabled) break;
                        HandleSendAbleFromCpp(obj);
                        break;

                    case "JobData":
                        if (!Form1.Instance.Eq2EqEnabled) break;
                        HandleJobData(obj);
                        break;
                    default:
                        _ctx.Log($"[TCP] 未知 cmd: {cmd}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _ctx.Log($"[TCP] JSON 解析失败: {ex.Message}, 原始数据: {jsonText}");
            }
        }

        /// <summary>
        /// 将 C++ 发来的机器状态转换为：
        /// 1. MachineStatusChangeReportBlock（Word[3..]）
        /// 2. 触发 MachineStatusChangeReport 事件（Word[0] bit5）
        /// </summary>
        private void HandleMachineStatusReport(JObject obj)
        {
            int status = (int?)obj["status"] ?? 0;
            int alarmId = (int?)obj["alarmId"] ?? 0;
            string reason = (string)obj["reason"] ?? "";
            string sub = (string)obj["sub"] ?? "";
            // ★ ★ 新增：交给 Form1 处理状态逻辑
            Form1.Instance.ChangeEQStatus(
                (EQStatus)status,
                reason,
                sub,
                alarmId
            );
            //_ctx.SendEventAndBlock("MachineStatusChangeReport", data =>
            //{
            //    // ★ MachineStatusChangeReportBlock 从 Word[3] 开始
            //    const int baseWord = 3;

            //    data[baseWord + 0] = status;
            //    data[baseWord + 1] = alarmId;
            //    data[baseWord + 2] = EncodeAsciiWord(reason);
            //    data[baseWord + 3] = EncodeAsciiWord(sub);

            //    // Unit1~Unit8 若暂时不需要，可保持默认值
            //});

            //_ctx.Log($"[TCP] 已处理 MachineStatusReport: Status={status}, AlarmID={alarmId}, Reason={reason}, Sub={sub}");
        }

        private void HandleRecipeChangeReport(JObject obj)
        {
            var d = obj["data"];

            if (d == null)
            {
                _ctx.Log("[TCP] RecipeChangeReport 缺少 data 字段");
                return;
            }

            // 1. Recipe Change Type
            int changeType = (int?)d["recipeChangeType"] ?? 0;

            // 2. Version Info
            int year = (int?)d["versionYear"] ?? 0;
            int month = (int?)d["versionMonth"] ?? 0;
            int day = (int?)d["versionDay"] ?? 0;
            int hour = (int?)d["versionHour"] ?? 0;
            int minute = (int?)d["versionMinute"] ?? 0;
            int second = (int?)d["versionSecond"] ?? 0;

            // 3. Operator ID（字符串，不超过 20 字节 ASCII）
            string operatorId = (string)d["operatorId"] ?? "";
            operatorId = operatorId.PadRight(20, ' '); // 保证 10 WORD = 20 字节

            // 4. Unit Number
            int unitNumber = (int?)d["unitNumber"] ?? 0;

            // 5. Recipe Number
            int recipeNumber = (int?)d["recipeNumber"] ?? 0;


            _ctx.SendEventAndBlock("RecipeChangeReport", data =>
            {
                const int baseWord = 15;  

                // ===== 填入 WORD 数据 =====
                data[baseWord + 0] = changeType;

                data[baseWord + 1] = year;
                data[baseWord + 2] = month;
                data[baseWord + 3] = day;
                data[baseWord + 4] = hour;
                data[baseWord + 5] = minute;
                data[baseWord + 6] = second;

                // ===== 写 OperatorID：10 WORD（20 字节 ASCII）=====
                byte[] opBytes = Encoding.ASCII.GetBytes(operatorId);

                for (int i = 0; i < 10; i++)
                {
                    byte c1 = opBytes[i * 2];
                    byte c2 = opBytes[i * 2 + 1];

                    data[baseWord + 7 + i] = (c2 << 8) | c1;
                }

                // ===== Unit Number =====
                data[baseWord + 17] = unitNumber;

                // ===== Recipe Number =====
                data[baseWord + 18] = recipeNumber;

                // 剩余 Reserved 自动保留原值，不写即可
            });

            
        }


        private void HandleCurrentRecipeNumberChangeReport(JObject obj)
        {
            var d = obj["data"];

            int recipeNumber = (int?)d["currentRecipeNumber"] ?? 0;
            int year = (int?)d["versionYear"] ?? 0;
            int month = (int?)d["versionMonth"] ?? 0;
            int day = (int?)d["versionDay"] ?? 0;
            int hour = (int?)d["versionHour"] ?? 0;
            int minute = (int?)d["versionMinute"] ?? 0;
            int second = (int?)d["versionSecond"] ?? 0;

            int unitNumber = (int?)d["unitNumber"] ?? 0;

            _ctx.SendEventAndBlock("CurrentRecipeNumberChangeReport", data =>
            {
                const int baseWord = 3;

                data[baseWord + 0] = recipeNumber;
                data[baseWord + 1] = year;
                data[baseWord + 2] = month;
                data[baseWord + 3] = day;
                data[baseWord + 4] = hour;
                data[baseWord + 5] = minute;
                data[baseWord + 6] = second;
                data[baseWord + 7] = unitNumber;

                // Reserved 4 words
                data[baseWord + 8] = 0;
                data[baseWord + 9] = 0;
                data[baseWord + 10] = 0;
                data[baseWord + 11] = 0;
            });

           
        }

        private void HandleCIMMessageConfirmReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] CIMMessageConfirmReport 缺少 data 字段");
                return;
            }

            // 1. Message ID
            int msgId = (int?)d["messageId"] ?? 0;

            // 2. Touch Panel Number
            int panel = (int?)d["panel"] ?? 0;

            // 3. Operator ID（10 WORD = 20 字节 ASCII）
            string operatorId = (string)d["operatorId"] ?? "";
            operatorId = operatorId.PadRight(20, ' ');  // 填满 20 字节

            // ===== 写入 PLC（EQ→CIM）=====
            _ctx.SendEventAndBlock("CIMMessageConfirmReport", data =>
            {
                const int baseWord = 44;   

                // ① Message ID
                data[baseWord + 0] = msgId;

                // ② Touch Panel Number
                data[baseWord + 1] = panel;

                // ③ Operator ID（10 WORD ASCII）
                byte[] opBytes = Encoding.ASCII.GetBytes(operatorId);

                for (int i = 0; i < 10; i++)
                {
                    byte c1 = opBytes[i * 2];
                    byte c2 = opBytes[i * 2 + 1];
                    data[baseWord + 2 + i] = (c2 << 8) | c1;
                }

                // Reserved（baseWord + 12 ~ baseWord + 14）不用写
            });

            
        }

        private void HandleOperatorLoginRequest(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] OperatorLoginRequest 缺少 data 字段");
                return;
            }

            // ===== 1. Operator ID（ASCII ≤20 字节）=====
            string operatorId = (string)d["operatorId"] ?? "";
            operatorId = operatorId.PadRight(20, ' ');   // 确保 20 字节

            // ===== 2. User Password（ASCII ≤20 字节）=====
            string password = (string)d["password"] ?? "";
            password = password.PadRight(20, ' ');

            // ===== 3. Touch Panel Number =====
            int panel = (int?)d["panelNumber"] ?? 0;

            // ===== 4. Report Option（1=LogIn, 2=LogOut）=====
            int reportOption = (int?)d["reportOption"] ?? 0;

           
            // ===== 写入 PLC（EQ → CIM）=====
            _ctx.SendEventAndBlock("OperatorLoginRequest", data =>
            {
                const int baseWord = 66; // ★ 按你要求，Block 从 Word[66] 开始

                // ===== Operator ID（10 WORD = 20 字节 ASCII）=====
                byte[] opBytes = Encoding.ASCII.GetBytes(operatorId);
                for (int i = 0; i < 10; i++)
                {
                    byte c1 = opBytes[i * 2];
                    byte c2 = opBytes[i * 2 + 1];
                    data[baseWord + i] = (c2 << 8) | c1;
                }

                // ===== User Password（10 WORD = 20 字节 ASCII）=====
                byte[] pwBytes = Encoding.ASCII.GetBytes(password);
                for (int i = 0; i < 10; i++)
                {
                    byte c1 = pwBytes[i * 2];
                    byte c2 = pwBytes[i * 2 + 1];
                    data[baseWord + 10 + i] = (c2 << 8) | c1;
                }

                // ===== Touch Panel Number =====
                data[baseWord + 20] = panel;

                // ===== Report Option =====
                data[baseWord + 21] = reportOption;

                // Reserved（baseWord + 22 ~ baseWord + 31）不写即可
            });

            _ctx.Log("[EQ→CIM] 已写入 OperatorLoginRequest Block");
        }

        private void HandleAlarmReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] AlarmReport 缺少 data 字段");
                return;
            }

            // ===== 1. 基本字段 =====
            int alarmId = (int?)d["alarmId"] ?? 0;
            int alarmType = (int?)d["alarmType"] ?? 0;
            int alarmUnitNumber = (int?)d["alarmUnitNumber"] ?? 0;
            int alarmStatus = (int?)d["alarmStatus"] ?? 0;
            int reasonCode = (int?)d["alarmCode"] ?? 0;
            int subReasonCode = 0;

            // ===== 2. Alarm Text：ASCII ≤ 50 字节（25 WORD）=====
            string alarmText = (string)d["alarmText"] ?? "";
            alarmText = alarmText.PadRight(50, ' ');  // 填满 50 字节

          

            // ===== 写入 PLC（EQ → CIM）=====
            _ctx.SendEventAndBlock("AlarmReport", data =>
            {
                const int baseWord = 98;   

                // ① Alarm ID
                data[baseWord + 0] = alarmId;

                // ② Alarm Type
                data[baseWord + 1] = alarmType;

                // ③ Alarm Unit Number
                data[baseWord + 2] = alarmUnitNumber;

                // ④ Alarm Status
                data[baseWord + 3] = alarmStatus;

                // ⑤ Alarm Status Reason Code
                data[baseWord + 4] = reasonCode;

                // ⑥ Alarm Status Sub Reason Code
                data[baseWord + 5] = subReasonCode;

                // ⑦ Alarm Text（25 WORD = 50 字节 ASCII）
                byte[] txtBytes = Encoding.ASCII.GetBytes(alarmText);
                for (int i = 0; i < 25; i++)
                {
                    byte c1 = txtBytes[i * 2];
                    byte c2 = txtBytes[i * 2 + 1];
                    data[baseWord + 6 + i] = ( c2<< 8) | c1;
                }

                // Reserved（baseWord + 31 ~ baseWord + 34）不写
            });

            _ctx.Log("[EQ→CIM] 已写入 AlarmReport Block");
        }
        private void HandleTactTimeChangeReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] TactTimeChangeReport 缺少 data");
                return;
            }

            double tactSec = (double?)d["tactTime"] ?? 0;

            _ctx.Log($"[EQ→CIM] TactTimeChangeReport = {tactSec} sec");

          
            _ctx.SendEventAndBlock("TactTimeChangeReport", data =>
            {
                const int baseWord = 391;

                int plcValue = (int)(tactSec * 100);   // 12.75 秒 → 1275

                data[baseWord] = plcValue;
            });
        }
        private void HandleMachineAutoMode(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] MachineAutoMode 缺少 data 字段");
                return;
            }

            bool auto = (bool?)d["value"] ?? false;

            const string tag = "SD_EQToCIM_Data01_03_01_00";
            int[] words = _ctx.ReadWordArray(tag);

            if (words == null || words.Length <= 0)
            {
                _ctx.Log($"❌ MachineAutoMode: 读取 {tag} 失败");
                return;
            }

            // ===== 写入 word[0].bit3 =====
            if (auto)
                words[0] |= (1 << 3);   // 自动
            else
                words[0] &= ~(1 << 3);  // 手动

            bool ok = _ctx.WriteWordArray(tag, words);

            if (ok)
                _ctx.Log($"[EQ→CIM] MachineAutoMode → {(auto ? "AUTO" : "MANUAL")}");
            else
                _ctx.Log($"❌ MachineAutoMode 写入失败");
        }

        private void HandleJobManualMoveReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] JobManualMoveReport 缺少 data 字段");
                return;
            }

            // ========== 1. 解析 JSON 字段 ==========
            string jobId = ((string?)d["jobId"] ?? "").PadRight(40, ' ');          // 20 WORD = 40 字节
            int cstSeq = (int?)d["cstSeq"] ?? 0;
            int slotSeq = (int?)d["slotSeq"] ?? 0;
            int jobPosition = (int?)d["jobPosition"] ?? 0;
            int reportOption = (int?)d["reportOption"] ?? 0;

            // ★ Operator ID = 10 WORD = 20 字节
            string operatorId = ((string?)d["operatorId"] ?? "").PadRight(20, ' ');

            int unitOrPort = (int?)d["unitOrPort"] ?? 0;
            int unitOrPortNumber = (int?)d["unitOrPortNumber"] ?? 0;
            int slotNumber = (int?)d["slotNumber"] ?? 0;


            // ========== 2. 写入 PLC（EQ → CIM）==========
            _ctx.SendEventAndBlock("JobManualMoveReport", data =>
            {
                const int baseWord = 203;  

                // ===== 2.1 Job ID（20 WORD = 40 字节）=====
                byte[] jobBytes = Encoding.ASCII.GetBytes(jobId);
                for (int i = 0; i < 20; i++)
                {
                    byte lo = jobBytes[i * 2];
                    byte hi = jobBytes[i * 2 + 1];
                    data[baseWord + i] = (hi << 8) | lo;
                }

                // ===== 2.2 CST Sequence Number =====
                data[baseWord + 20] = cstSeq;

                // ===== 2.3 Slot Sequence Number =====
                data[baseWord + 21] = slotSeq;

                // ===== 2.4 Job Position =====
                data[baseWord + 22] = jobPosition;

                // ===== 2.5 Report Option =====
                data[baseWord + 23] = reportOption;

                // ===== 2.6 Operator ID（10 WORD = 20 字节）=====
                byte[] opBytes = Encoding.ASCII.GetBytes(operatorId);
                for (int i = 0; i < 10; i++)
                {
                    byte lo = opBytes[i * 2];
                    byte hi = opBytes[i * 2 + 1];
                    data[baseWord + 24 + i] = (hi << 8) | lo;
                }

                // ===== 2.7 UnitOrPort =====
                data[baseWord + 34] = unitOrPort;

                // ===== 2.8 UnitOrPortNumber =====
                data[baseWord + 35] = unitOrPortNumber;

                // ===== 2.9 SlotNumber =====
                data[baseWord + 36] = slotNumber;

                // Reserved ： baseWord + 37 ~ ...
            });

            _ctx.Log("[EQ→CIM] 已写入 JobManualMoveReport Block");
        }
        private void HandleJobJudgeResultReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] JobJudgeResultReport 缺少 data 字段");
                return;
            }

            string jobId = ((string?)d["jobId"] ?? "").PadRight(40, ' ');

            int cstSeq =
     (int?)d["cstSequenceNumber"]
  ?? (int?)d["cstSeq"]
  ?? 0;
            int slotSeq =
    (int?)d["slotSequenceNumber"]
 ?? (int?)d["slotSeq"]
 ?? 0;
            int unitNum = (int?)d["unitNumber"] ?? 0;
            int slotNum = (int?)d["slotNumber"] ?? 0;

            string judgeCode = ((string?)d["jobJudgeCode"] ?? "N").Substring(0, 1);
            string gradeCode = ((string?)d["jobGradeCode"] ?? "N").Substring(0, 1);

            int mpkngType = (int?)d["mpkngType"] ?? 0;

           

            // 写入 EQ→CIM block
            _ctx.SendEventAndBlock("JobJudgeResultReport", data =>
            {
                const int baseWord = 173; 

                // 1) JobID (20 WORD = 40 字节)
                byte[] ascii = Encoding.ASCII.GetBytes(jobId);
                for (int i = 0; i < 20; i++)
                {
                    byte lo = ascii[i * 2];
                    byte hi = ascii[i * 2 + 1];
                    data[baseWord + i] = (hi << 8) | lo;
                }

                // 2) INT fields
                data[baseWord + 20] = cstSeq;
                data[baseWord + 21] = slotSeq;
                data[baseWord + 22] = unitNum;
                data[baseWord + 23] = slotNum;

                
                byte[] j = Encoding.ASCII.GetBytes(judgeCode.PadRight(2, ' '));
                data[baseWord + 24] = (j[1] << 8) | j[0];

                byte[] g = Encoding.ASCII.GetBytes(gradeCode.PadRight(2, ' '));
                data[baseWord + 25] = (g[1] << 8) | g[0];

                // 4) MPKNGType
                data[baseWord + 26] = mpkngType;
            });

            
        }
        private void HandleVCRStatusReport(JObject obj)
        {
            var d = obj["data"];

            if (d == null)
            {
                _ctx.Log("[TCP] VCRStatusReport 缺少 data 字段");
                return;
            }

            // 1) 解析 C++ 传来的 ON/OFF
            bool on = (bool?)d["value"] ?? false;
            int vcrStatus = on ? 1 : 2;
   
            // 2) 写 EQ→CIM Block
            _ctx.SendEventAndBlock("VCRStatusReport", data =>
            {
                const int baseWord = 59;

                // === WORD[59] = VCR Number ===
                data[baseWord + 0] = 1;  // 固定写 1

                // === WORD[60] = VCR Status ===
                data[baseWord + 1] = vcrStatus;  // 1=ON, 0=OFF

                // Reserved 不处理
            });
        }
        private void HandleReceivedJobReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] ReceivedJobReport 缺少 data 字段");
                return;
            }

            // ===== 1) 读取 jobData（150 WORD）=====
            JArray arr = d["jobData"] as JArray ?? new();
            if (arr.Count != 150)
            {
                _ctx.Log($"[TCP] ReceivedJobReport jobData 数量必须150，当前={arr.Count}");
                return;
            }

            ushort[] jobData = new ushort[150];
            for (int i = 0; i < 150; i++)
                jobData[i] = (ushort)arr[i]!.Value<int>();


            // ===== 2) 解析 EQPID（空 → 20 空格）=====
            string eqpid = ((string?)d["eqpid"] ?? "")
                            .PadRight(20, ' ');

            // ===== 3) 解析 TrayID（空 → 20 空格）=====
            string trayId = ((string?)d["trayId"] ?? "")
                            .PadRight(20, ' ');


            // ===== 4) 写入 PLC（ReceivedJobReport Block = 170 WORD）=====
            _ctx.SendEventAndBlock("ReceivedJobReport", data =>
            {
                const int baseWord = 1;  // ★ Word[1].Bit0 为事件位，Block 从 Word[1] 开始

                // ---- 4.1 写 150 WORD JobData ----
                for (int i = 0; i < 150; i++)
                    data[baseWord + i] = jobData[i];

                // ---- 4.2 EQPID（20 字节 → 10 WORD）----
                byte[] eqBytes = Encoding.ASCII.GetBytes(eqpid);
                for (int i = 0; i < 10; i++)
                {
                    byte lo = eqBytes[i * 2];
                    byte hi = eqBytes[i * 2 + 1];
                    data[baseWord + 150 + i] = (hi << 8) | lo;
                }

                // ---- 4.3 TrayID（20 字节 → 10 WORD）----
                byte[] trayBytes = Encoding.ASCII.GetBytes(trayId);
                for (int i = 0; i < 10; i++)
                {
                    byte lo = trayBytes[i * 2];
                    byte hi = trayBytes[i * 2 + 1];
                    data[baseWord + 160 + i] = (hi << 8) | lo;
                }
            });       
        }
        private void HandleSentOutJobReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] SentOutJobReport 缺少 data 字段");
                return;
            }

            // ===== 1) 读取 sentJobData（150 WORD）=====
            JArray arr = d["sentJobData"] as JArray ?? new();
            if (arr.Count != 150)
            {
                _ctx.Log($"[TCP] SentOutJobReport sentJobData 数量必须150，当前={arr.Count}");
                return;
            }

            ushort[] jobData = new ushort[150];
            for (int i = 0; i < 150; i++)
                jobData[i] = (ushort)arr[i]!.Value<int>();


            // ===== 2) EQPID（20 ASCII 字节 → 10 WORD）=====
            string eqpid = ((string?)d["eqpid"] ?? "").PadRight(20, ' ');

            // ===== 3) TrayID（20 ASCII 字节 → 10 WORD）=====
            string trayId = ((string?)d["trayId"] ?? "").PadRight(20, ' ');


            // ===== 4) 写入 PLC Block（170 WORD）=====
            _ctx.SendEventAndBlock("SentOutJobReport", data =>
            {
                const int baseWord = 1;  // ★ Block 从 Word[1] 写起

                // ---- 4.1 150 WORD SentJobData ----
                for (int i = 0; i < 150; i++)
                    data[baseWord + i] = jobData[i];

                // ---- 4.2 EQPID（10 WORD = 20 字节）----
                byte[] eqBytes = Encoding.ASCII.GetBytes(eqpid);
                for (int i = 0; i < 10; i++)
                {
                    byte lo = eqBytes[i * 2];
                    byte hi = eqBytes[i * 2 + 1];
                    data[baseWord + 150 + i] = (hi << 8) | lo;
                }

                // ---- 4.3 TrayID（10 WORD = 20 字节）----
                byte[] trayBytes = Encoding.ASCII.GetBytes(trayId);
                for (int i = 0; i < 10; i++)
                {
                    byte lo = trayBytes[i * 2];
                    byte hi = trayBytes[i * 2 + 1];
                    data[baseWord + 160 + i] = (hi << 8) | lo;
                }
            });

            
        }
        private void HandleDefectCodeReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] DefectCodeReport 缺少 data 字段");
                return;
            }

            // ========== 1. 解析 JSON 字段 ==========

            string jobId = ((string?)d["jobId"] ?? "");
            int cstSeq = (int?)d["cstSeq"] ?? 0;
            int slotSeq = (int?)d["slotSeq"] ?? 0;

            string defectName = ((string?)d["defectCodeName"] ?? "");
            string judgeCode = ((string?)d["judgeCode"] ?? " ");
            string gradeCode = ((string?)d["gradeCode"] ?? " ");

            var points = d["points"] as JArray;

            // ========== 2. 工具函数：ASCII 按 1 WORD = 2 字符写入 ==========
            // 当前你已经通过 CIMMessageSetCommand 测试确认：
            // WORD = lowByte + highByte * 256，即低字节在前。
            void WriteAsciiWords(int[] data, int startWord, string text, int wordCount)
            {
                int byteCount = wordCount * 2;

                string fixedText = (text ?? "");
                if (fixedText.Length > byteCount)
                    fixedText = fixedText.Substring(0, byteCount);

                fixedText = fixedText.PadRight(byteCount, ' ');

                byte[] bytes = Encoding.ASCII.GetBytes(fixedText);

                for (int i = 0; i < wordCount; i++)
                {
                    byte lo = bytes[i * 2];
                    byte hi = bytes[i * 2 + 1];

                    data[startWord + i] = (hi << 8) | lo;
                }
            }

            int ToWordValue(JToken? token)
            {
                int v = (int?)token ?? 0;

                if (v < 0)
                    v = 0;

                if (v > 65535)
                    v = 65535;

                return v;
            }

            // ========== 3. 写 PLC Block ==========
            _ctx.SendEventAndBlock("DefectCodeReport", data =>
            {
                const int baseWord = 1;   // Word[0] 是 EQPEvent，DefectCodeReportBlock 从 Word[1] 开始

                // --------------------------------------------------
                // 3.1 Header 部分
                // --------------------------------------------------

                // JobID：20 WORD = 40 ASCII 字节
                WriteAsciiWords(data, baseWord + 0, jobId, 20);

                // CSTSequenceNumber
                data[baseWord + 20] = cstSeq;

                // SlotSequenceNumber
                data[baseWord + 21] = slotSeq;

                // DefectCodeName：10 WORD = 20 ASCII 字节
                WriteAsciiWords(data, baseWord + 22, defectName, 10);

                // JobJudgeCode：1 WORD ASCII
                WriteAsciiWords(data, baseWord + 32, judgeCode, 1);

                // JobGradeCode：1 WORD ASCII
                WriteAsciiWords(data, baseWord + 33, gradeCode, 1);

                // --------------------------------------------------
                // 3.2 清空 50 个点位区域
                // 每个点 7 WORD：
                // X, Y, Color, ReasonCode[4]
                // --------------------------------------------------
                for (int i = 0; i < 50; i++)
                {
                    int pointBase = baseWord + 34 + i * 7;

                    data[pointBase + 0] = 0; // X
                    data[pointBase + 1] = 0; // Y

                    WriteAsciiWords(data, pointBase + 2, "", 1); // Color
                    WriteAsciiWords(data, pointBase + 3, "", 4); // ReasonCode
                }

                // --------------------------------------------------
                // 3.3 写入实际点位，最多 50 个
                // --------------------------------------------------
                int pointCount = points == null ? 0 : Math.Min(points.Count, 50);

                for (int i = 0; i < pointCount; i++)
                {
                    JObject? p = points![i] as JObject;
                    if (p == null)
                        continue;

                    int pointBase = baseWord + 34 + i * 7;

                    int x = ToWordValue(p["x"]);
                    int y = ToWordValue(p["y"]);

                    string color = (string?)p["color"] ?? " ";
                    string reasonCode = (string?)p["reasonCode"] ?? "";

                    // X#n
                    data[pointBase + 0] = x;

                    // Y#n
                    data[pointBase + 1] = y;

                    // Color#n：1 WORD ASCII
                    WriteAsciiWords(data, pointBase + 2, color, 1);

                    // ReasonCode#n：4 WORD ASCII = 8 字节
                    WriteAsciiWords(data, pointBase + 3, reasonCode, 4);
                }

                // --------------------------------------------------
                // 3.4 Reserved
                // block offset 384 ~ 389，共 6 WORD，清 0
                // --------------------------------------------------
                for (int i = 0; i < 6; i++)
                {
                    data[baseWord + 384 + i] = 0;
                }
            });

            _ctx.Log($"[TCP] DefectCodeReport handled, jobId:{jobId.Trim()}, cst:{cstSeq}, slot:{slotSeq}, points:{(points == null ? 0 : Math.Min(points.Count, 50))}");
        }




        private void HandleDVData(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] DVData 缺少 data 字段");
                return;
            }

            // 1) 提取 DVData 字符串（任意长度，但 PLC 需要转成 210 WORD）
            string dvString = (string)d["dvData"] ?? "";
            dvString = dvString.PadRight(420, ' ');   

            // =========================================================
            // ★ 2) 写入 210 WORD 到 BC_EQToCIM_DVData_03_01_00
            // =========================================================

            const string dvTag = "BC_EQToCIM_DVData_03_01_00";

            int[] dvWords = new int[210];

            byte[] ascii = Encoding.ASCII.GetBytes(dvString);

            for (int i = 0; i < 210; i++)
            {
                byte lo = ascii[i * 2];
                byte hi = ascii[i * 2 + 1];
                dvWords[i] = (hi << 8) | lo;
            }

            if (_ctx.WriteWordArray(dvTag, dvWords))
                _ctx.Log("[EQ→CIM] 已写入 DVData Block （210 WORD）");
            else
                _ctx.Log("[EQ→CIM] ❌ 写入 DVData Block 失败！");


       
           
        }

        private void HandleMachineModeChangeReport(JObject obj)
        {
            var d = obj["data"];

            if (d == null)
            {
                _ctx.Log("[TCP] MachineModeChangeReport 缺少 data 字段");
                return;
            }

            int mode = (int?)d["mode"] ?? 1;   

            Form1.MachineMode machineMode;

            if (mode == 4)
                machineMode = Form1.MachineMode.Bypass;
            else
                machineMode = Form1.MachineMode.Normal;


            Form1.Instance.SetMachineMode(machineMode);
        }

        private void HandleDVDataReport(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] DVDataReport 缺少 data 字段");
                return;
            }

            // =====================================================
            // 1) 解析 JSON 字段（来自 C++）
            // =====================================================
            int dvType = (int?)d["dvType"] ?? 1;

            string jobId = ((string?)d["jobId"] ?? "").PadRight(40, ' ');

            int cstSeq = (int?)d["cstSeq"] ?? 0;
            int slotSeq = (int?)d["slotSeq"] ?? 0;

            int sy = (int?)d["startYear"] ?? 0;
            int sm = (int?)d["startMonth"] ?? 0;
            int sd = (int?)d["startDay"] ?? 0;
            int sh = (int?)d["startHour"] ?? 0;
            int smin = (int?)d["startMinute"] ?? 0;
            int ss = (int?)d["startSecond"] ?? 0;

            int ey = (int?)d["endYear"] ?? 0;
            int em = (int?)d["endMonth"] ?? 0;
            int ed = (int?)d["endDay"] ?? 0;
            int eh = (int?)d["endHour"] ?? 0;
            int emin = (int?)d["endMinute"] ?? 0;
            int es = (int?)d["endSecond"] ?? 0;

            int unitNumber = (int?)d["unitNumber"] ?? 0;
            int slotNumber = (int?)d["slotNumber"] ?? 0;
            int recipeNumber = (int?)d["recipeNumber"] ?? 0;

            // =====================================================
            // 2) 写入 PLC：DVDataReportBlock
            // =====================================================
            _ctx.SendEventAndBlock("DVDataReport", data =>
            {
                const int baseWord = 2;  

                // WORD 0
                data[baseWord + 0] = dvType;

                // WORD 1~20 JobID (20 WORD = 40 字节)
                byte[] jidBytes = Encoding.ASCII.GetBytes(jobId);
                for (int i = 0; i < 20; i++)
                {
                    byte lo = jidBytes[i * 2];
                    byte hi = jidBytes[i * 2 + 1];
                    data[baseWord + 1 + i] = (hi << 8) | lo;
                }

                data[baseWord + 21] = cstSeq;
                data[baseWord + 22] = slotSeq;

                data[baseWord + 23] = sy;
                data[baseWord + 24] = sm;
                data[baseWord + 25] = sd;
                data[baseWord + 26] = sh;
                data[baseWord + 27] = smin;
                data[baseWord + 28] = ss;

                data[baseWord + 29] = ey;
                data[baseWord + 30] = em;
                data[baseWord + 31] = ed;
                data[baseWord + 32] = eh;
                data[baseWord + 33] = emin;
                data[baseWord + 34] = es;

                data[baseWord + 35] = unitNumber;
                data[baseWord + 36] = slotNumber;
                data[baseWord + 37] = recipeNumber;

                // WORD 38~47 Reserved（不用写）
            });

            
        }






        //*************************************************************EQ  TO   EQ**************************************************************************************
        private void HandleReceiveAbleFromCpp(JObject obj)
        {
           
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] ReceiveAble 缺少 data 字段");
                return;
            }

            bool on = (bool?)d["value"] ?? false;


            const string tag = "SD_EQToEQ_LinkSignal_03_02_00";
            int[] words = _ctx.ReadWordArray(tag);

            if (words == null || words.Length <= 3)
            {
                _ctx.Log($"❌ ReceiveAbleFromCpp: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[3].Bit3 ===
            if (on)
                words[3] |= (1 << 3);
            else
                words[3] &= ~(1 << 3);

            bool ok = _ctx.WriteWordArray(tag, words);

            if (ok)
            {
                _ctx.Log($"✔ 发送上游 EQ ReceiveAble = {(on ? "ON" : "OFF")} → {tag}[3].bit3");
            }
            else
            {
                _ctx.Log($"❌ 发送上游 EQ ReceiveAble 失败");
            }
        }
        private void HandleConveyerStateFromCpp(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] ConveyerState 缺少 data 字段");
                return;
            }

            bool on = (bool?)d["value"] ?? false;

            const string tag = "SD_EQToEQ_LinkSignal_03_02_00";
            int[] words = _ctx.ReadWordArray(tag);

            if (words == null || words.Length <= 3)
            {
                _ctx.Log($"❌ ConveyerStateFromCpp: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[3].Bit11 ===
            if (on)
                words[3] |= (1 << 11);     // bit11 = 1
            else
                words[3] &= ~(1 << 11);    // bit11 = 0

            bool ok = _ctx.WriteWordArray(tag, words);

            if (ok)
            {
                _ctx.Log($"✔ 发送上游 EQ ConveyerState = {(on ? "ON" : "OFF")} → {tag}[3].bit11");
            }
            else
            {
                _ctx.Log($"❌ 发送上游 EQ ConveyerState 失败");
            }
        }
        private void HandleGlassExistArm1FromCpp(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] GlassExistArm1 缺少 data 字段");
                return;
            }

            bool on = (bool?)d["value"] ?? false;

            const string tag = "SD_EQToEQ_LinkSignal_03_02_00";
            int[] words = _ctx.ReadWordArray(tag);

            if (words == null || words.Length <= 4)
            {
                _ctx.Log($"❌ GlassExistArm1FromCpp: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[4].Bit0 ===
            if (on)
                words[4] |= (1 << 0);     // bit0 = 1
            else
                words[4] &= ~(1 << 0);    // bit0 = 0

            bool ok = _ctx.WriteWordArray(tag, words);

            if (ok)
            {
                _ctx.Log($"✔ 发送上游 EQ GlassExistArm1 = {(on ? "ON" : "OFF")} → {tag}[4].bit0");
            }
            else
            {
                _ctx.Log($"❌ 发送上游 EQ GlassExistArm1 失败");
            }
        }
        private void HandleJobTransferSignalFromCpp(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] JobTransferSignal 缺少 data 字段");
                return;
            }

            bool on = (bool?)d["value"] ?? false;

            const string tag = "SD_EQToEQ_LinkSignal_03_02_00";
            int[] words = _ctx.ReadWordArray(tag);

            if (words == null || words.Length <= 3)
            {
                _ctx.Log($"❌ JobTransferSignalFromCpp: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[3].Bit2 ===
            if (on)
                words[3] |= (1 << 2);     // bit2 = 1
            else
                words[3] &= ~(1 << 2);    // bit2 = 0

            bool ok = _ctx.WriteWordArray(tag, words);

            if (ok)
            {
                _ctx.Log($"✔ 发送上游 EQ JobTransferSignal = {(on ? "ON" : "OFF")} → {tag}[3].bit2");
            }
            else
            {
                _ctx.Log($"❌ 发送上游 EQ JobTransferSignal 失败");
            }
        }
        private void HandleReceiveCompleteFromCpp(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] ReceiveComplete 缺少 data 字段");
                return;
            }

            bool on = (bool?)d["value"] ?? false;

            const string tag = "SD_EQToEQ_LinkSignal_03_02_00";
            int[] words = _ctx.ReadWordArray(tag);

            if (words == null || words.Length <= 3)
            {
                _ctx.Log($"❌ ReceiveCompleteFromCpp: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[3].Bit5 ===
            if (on)
                words[3] |= (1 << 5);      // bit5 = 1
            else
                words[3] &= ~(1 << 5);     // bit5 = 0

            bool ok = _ctx.WriteWordArray(tag, words);

            if (ok)
            {
                _ctx.Log($"✔ 发送上游 EQ ReceiveComplete = {(on ? "ON" : "OFF")} → {tag}[3].bit5");
            }
            else
            {
                _ctx.Log($"❌ 发送上游 EQ ReceiveComplete 失败");
            }
        }
        private void HandleSendAbleFromCpp(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] SendAble 缺少 data 字段");
                return;
            }

            bool on = (bool?)d["value"] ?? false;

            const string tag = "SD_EQToEQ_LinkSignal_03_04_00";
            int[] words = _ctx.ReadWordArray(tag);

            if (words == null || words.Length <= 0)
            {
                _ctx.Log($"❌ SendAbleFromCpp: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[0].Bit3 ===
            if (on)
                words[0] |= (1 << 3);     // bit3 = 1
            else
                words[0] &= ~(1 << 3);    // bit3 = 0

            bool ok = _ctx.WriteWordArray(tag, words);

            if (ok)
            {
                _ctx.Log($"✔ 发送下游 EQ SendAble = {(on ? "ON" : "OFF")} → {tag}[0].bit3");
            }
            else
            {
                _ctx.Log("❌ 发送下游 EQ SendAble 失败");
            }
        }







        private void HandleJobData(JObject obj)
        {
            var d = obj["data"];
            if (d == null)
            {
                _ctx.Log("[TCP] JobData 缺少 data 字段");
                return;
            }

            int length = (int?)d["length"] ?? 0;

            if (length < 150)
            {
                _ctx.Log($"❌ JobData 长度不足：length={length}, 需要 150 WORD");
                return;
            }

            // ========== 1. 提取 jobData 数组 ==========
            JArray arr = (JArray)d["job"];
            if (arr == null || arr.Count < 150)
            {
                _ctx.Log("❌ JobData 数组缺失或长度不足 150");
                return;
            }

            // 转成 int[]
            int[] jobWords = new int[150];
            for (int i = 0; i < 150; i++)
                jobWords[i] = (int)arr[i];

            _ctx.Log("[TCP] 收到 JobData，准备写入 EQ→EQ");

            // ========== 2. 写入 下游EQ→EQ 标签 ==========
            const string tag = "SD_EQToEQ_LinkSignal_03_04_00";
            int[] words = _ctx.ReadWordArray(tag);

            if (words == null)
            {
                _ctx.Log($"❌ 无法读取 EQ→EQ tag: {tag}");
                return;
            }

            // 检查容量
            if (words.Length < 6 + 150)
            {
                _ctx.Log($"❌ Tag {tag} 的长度不足以写入 150 WORD（需要 >= {6 + 150}）");
                return;
            }

            // 将 jobWords 写到 Word[6] ~ Word[155]
            for (int i = 0; i < 150; i++)
                words[6 + i] = jobWords[i];

            // 写回 PLC
            bool ok = _ctx.WriteWordArray(tag, words);

            if (ok)
                _ctx.Log("✔ 已写入 下游EQ JobData（150 WORD → SD_EQToEQ_LinkSignal_03_04_00）");
            else
                _ctx.Log("❌ EQ→EQ JobData 写入失败");
        }




        /// <summary>
        /// 将 "PR" "FR" 这种2字符 code -> 1 word
        /// 高字节=第1字符 低字节=第2字符
        /// </summary>
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


    }
}
