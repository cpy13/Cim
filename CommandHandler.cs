using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace EQModeChangeSimulator
{
    /// <summary>
    /// CIM 命令解析与处理（含发送给 C++ AOI 客户端）
    /// </summary>
    public class CommandHandler
    {
        private readonly Func<string, int[]> _readWordArray;
        private readonly Func<string, int[], bool> _writeWordArray;
        private readonly Action<string> _log;
        private readonly EqTcpServer _server;
        private readonly IEqContext _ctx;

        public CommandHandler(
     Func<string, int[]> readWordArray,
     Func<string, int[], bool> writeWordArray,
     Action<string> log,
     EqTcpServer server,
     IEqContext ctx)   // ★ 新增
        {
            _readWordArray = readWordArray;
            _writeWordArray = writeWordArray;
            _log = log;
            _server = server;
            _ctx = ctx;       // ★ 保存 ctx，后面用 SendEventAndBlock
        }

        // ==============================
        // CIM → EQ 指令统一入口
        // ==============================
        public void Handle(string cmdName)
        {
            switch (cmdName)
            {
                case "DateTimeSetCommand":
                    HandleDateTime();
                    break;

                case "CIMModeChangeCommand":
                    HandleCIMModeChange();
                    break;

                case "CIMMessageSetCommand":
                    HandleCIMMessageSet();
                    break;

                case "CIMMessageClearCommand":
                    HandleMessageClear();
                    break;

                case "MachineModeChangeCommand":
                    HandleMachineMode();
                    break;

                case "RecipeParameterRequestCommand":
                    HandleRecipeParameterRequestCommand();
                    break;

                case "CurrentRecipeChangeCommand":
                    if (!Form1.Instance.AutoRecipeEnabled) break;
                    HandleCurrentRecipeChange();
                    break;

                case "FGCodeCommand":
                    HandleFGCodeCommand();
                    break;

                default:
                    _log($"⚠ 未实现的 CIM 命令：{cmdName}");
                    break;
            }
        }

        // ==============================
        // 具体指令处理部分
        // ==============================

        private void HandleDateTime()
        {
            int[] rv = _readWordArray("RV_CIMToEQ_Data_01_03_00");

            int year = rv[32];
            int month = rv[33];
            int day = rv[34];
            int hour = rv[35];
            int minute = rv[36];
            int second = rv[37];

            _log($"📅 DateTimeSetCommand → {year}-{month}-{day} {hour}:{minute}:{second}");

            try
            {
                SYSTEMTIME st = new SYSTEMTIME
                {
                    wYear = (ushort)year,
                    wMonth = (ushort)month,
                    wDay = (ushort)day,
                    wHour = (ushort)hour,
                    wMinute = (ushort)minute,
                    wSecond = (ushort)second
                };

                bool ok = SetLocalTime(ref st);
                _log(ok ? "✔ 系统时间修改成功" : "❌ 系统时间修改失败（可能需要管理员权限）");
            }
            catch (Exception ex)
            {
                _log($"❌ 系统时间设置失败: {ex.Message}");
            }
        }

        // Windows API
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetLocalTime(ref SYSTEMTIME st);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        private void HandleCIMModeChange()
        {
            int[] rv = _readWordArray("RV_CIMToEQ_Data_01_03_00");
            if (rv == null)
            {
                _log("❌ 无法读取 CIMModeChange 参数");
                return;
            }

            // ★ 协议：0=OFF, 1=ON
            int mode = rv[6];  // 或你协议中定义的 Word
            bool on = (mode == 1);

            string statusText = on ? "开启 CIM 通讯" : "关闭 CIM 通讯";

            _log($"▶ CIMModeChangeCommand → mode = {mode} → {statusText}");

            // ★ 调用 Form1 开关
            Form1.Instance.SetCimMode(on);
           
            WriteCIMModeChangeCommandReply(true);
           
           
           
        }

        // =====================================================
        // ★★★ 重点：CIM Message Set Command 发给 C++ 客户端
        // =====================================================
        private void HandleCIMMessageSet()
        {
            int[] rv = _readWordArray("RV_CIMToEQ_Data_01_03_00");

            // ★ 按协议从 Word[7] 开始
            int msgType = rv[7];      // WORD offset 0
            int msgID = rv[8];      // WORD offset 1
            int panel = rv[9];      // WORD offset 2

            // ★ 文本从 Word[10] 连续 20 个 WORD（共 40 字节）
            string text = DecodeAscii(rv, 10, 20);

            _log($"▶ CIMMessageSetCommand 执行 → Type={msgType}, ID={msgID}, Panel={panel}, Text='{text}'");

       
            bool ok = SendToCpp("CIMMessageSetCommand", new
            {
                messageType = msgType,
                messageId = msgID,
                panel = panel,
                text = text
            });

            WriteCIMMessageSetReply(ok);

        }

        private void HandleMessageClear()
        {
            int[] rv = _readWordArray("RV_CIMToEQ_Data_01_03_00");

            int msgID = rv[30];
            int panel = rv[31];

         

            _log($"▶ 转发给 C++: Clear ID={msgID}, Panel={panel}");
       
            bool ok = SendToCpp("CIMMessageClearCommand", new
            {
                messageId = msgID,
                panel = panel
            });

           
            WriteCIMMessageClearReply(ok);
        }

        private void HandleMachineMode()
        {
            _log("▶ MachineModeChangeCommand 执行");
        }

        public bool HandleRecipeParameterRequestCommand()
        {
            int[] rv = _readWordArray("RV_CIMToEQ_Data_01_03_00");
            if (rv == null || rv.Length < 51)
            {
                _log("RecipeParameterRequestCommand: 读取 CommandBlock 失败");
                return false;
            }

            const int baseWord = 39;
            int recipeNumber = rv[baseWord + 0];
            int versionYear = rv[baseWord + 1];
            int versionMonth = rv[baseWord + 2];
            int versionDay = rv[baseWord + 3];
            int versionHour = rv[baseWord + 4];
            int versionMinute = rv[baseWord + 5];
            int versionSecond = rv[baseWord + 6];
            int unitNumber = rv[baseWord + 7];
            int recipeStepNumber = rv[baseWord + 8];

            _log($"RecipeParameterRequestCommand: RecipeNumber={recipeNumber}, Unit={unitNumber}, Step={recipeStepNumber}");

            return SendToCpp("RecipeParameterRequestCommand", new
            {
                recipeNumber = recipeNumber,
                versionYear = versionYear,
                versionMonth = versionMonth,
                versionDay = versionDay,
                versionHour = versionHour,
                versionMinute = versionMinute,
                versionSecond = versionSecond,
                unitNumber = unitNumber,
                recipeStepNumber = recipeStepNumber
            });
        }

        private void HandleCurrentRecipeChange()
        {
            int[] rv = _readWordArray("RV_CIMToEQ_Data_01_03_00");
            if (rv == null)
            {
                _log("❌ CurrentRecipeChangeCommand: 读取 RV_CIMToEQ_Data_01_03_00 失败");
                return;
            }

          
            int recipeNumber = rv[333];      // 1~9999
            string reserved = DecodeAscii(rv, 8, 9);  // ASCII 9 words（不参与逻辑）

            _log($"▶ CurrentRecipeChangeCommand 执行 → RecipeNumber={recipeNumber}");

            // ★ 转发给 C++ 客户端
            bool ok = SendToCpp("CurrentRecipeChangeCommand", new
            {
                recipeNumber = recipeNumber
            });
            WriteCurrentRecipeChangeCommandReply(ok);


        }
        private void HandleFGCodeCommand()
        {
            _log("▶ FGCodeCommand 执行");
        }


        //write reply  block
        private void WriteCIMModeChangeCommandReply(bool ok)
        {
            const string tag = "SD_EQToCIM_Data01_03_01_00";

            int[] data = _readWordArray(tag);
            if (data == null) return;



            // ===== Reply Block：Word[14] =====
            data[276] = ok ? 1 : 2;

            _writeWordArray(tag, data);
            _log($"[EQ→CIM] 已写入CIMModeChangeCommandReplyBlock ReturnCode={(ok ? 1 : 2)}");
        }

        private void WriteCIMMessageSetReply(bool ok)
        {
            const string tag = "SD_EQToCIM_Data01_03_01_00";

            int[] data = _readWordArray(tag);
            if (data == null) return;



            // ===== Reply Block：Word[14] =====
            data[277] = ok ? 1 : 2;

            _writeWordArray(tag, data);
            _log($"[EQ→CIM] 已写入 CIMMessageSetReplyBlock ReturnCode={(ok ? 1 : 2)}");
        }

        private void WriteCIMMessageClearReply(bool ok)
        {
            const string tag = "SD_EQToCIM_Data01_03_01_00";

            int[] data = _readWordArray(tag);
            if (data == null) return;

         
            data[278] = ok ? 1 : 2;

            _writeWordArray(tag, data);

            _log($"[EQ→CIM] 已写入 CIMMessageClearCommandReplyBlock ReturnCode={(ok ? 1 : 2)}");
        }
        private void WriteCurrentRecipeChangeCommandReply(bool ok)
        {
            const string tag = "SD_EQToCIM_RecipeData_03_01_00";

            int[] data = _readWordArray(tag);
            if (data == null)
            {
                _log("[ERR] CurrentRecipeChangeCommandReply 读取 SD_EQToCIM_RecipeData_03_01_00 失败");
                return;
            }

            int replyWord = 79;
            data[replyWord] = ok ? 1 : 2;

            if (!_writeWordArray(tag, data))
            {
                _log("[ERR] CurrentRecipeChangeCommandReply 写入失败");
                return;
            }

            _log($"[EQ→CIM] 已写入 CurrentRecipeChangeCommandReplyBlock ReturnCode={(ok ? 1 : 2)}");
        }

        // =====================================================
        // ★ 字符串解析（Word[] → ASCII）
        // =====================================================
        private string DecodeAscii(int[] words, int start, int length)
        {
            List<byte> bytes = new();

            for (int i = 0; i < length; i++)
            {
                int w = words[start + i];

                // ⚠ 根据 CIM 真实情况：低字节是第一个字符
                byte c1 = (byte)(w & 0xFF);       
                byte c2 = (byte)((w >> 8) & 0xFF); 

                if (c1 != 0) bytes.Add(c1);
                if (c2 != 0) bytes.Add(c2);
            }

            return Encoding.ASCII.GetString(bytes.ToArray());
        }
      

        // =====================================================
        // ★★★ 核心：发送给 C++ AOI 客户端
        // =====================================================
        private bool SendToCpp(string cmd, object payload)
        {
            var json = JsonConvert.SerializeObject(new
            {
                cmd = cmd,
                data = payload
            });

            bool ok = _server.SendToClient(json);

            //if (ok)
            //    _log($"[TCP] 已成功发送给 C++ 客户端 → {json}");
            //else
            //    _log($"[TCP] ❌ 发送给 C++ 客户端失败 → {json}");

            return ok;
        }


    }
}
