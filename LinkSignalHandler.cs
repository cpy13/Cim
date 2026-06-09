using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EQModeChangeSimulator
{
    /// <summary>
    /// EQ-EQ LinkSignal 事件处理（发送给 C++ 客户端）
    /// 模仿 CommandHandler 的结构与风格
    /// </summary>
    public class LinkSignalHandler
    {
        private readonly Func<string, int[]> _readWordArray;
        private readonly Func<string, int[], bool> _writeWordArray;
        private readonly Action<string> _log;
        private readonly EqTcpServer _server;

        public LinkSignalHandler(
            Func<string, int[]> readWordArray,
            Func<string, int[], bool> writeWordArray,
            Action<string> log,
            EqTcpServer server)
        {
            _readWordArray = readWordArray;
            _writeWordArray = writeWordArray;
            _log = log;
            _server = server;
        }
        
        // =============================================================
        // 统一入口：处理上游 EQ → 下游 EQ 的事件
        // name = "SendAble" / "ReceiveAble" / "SendStart"
        // on = true/false
        // =============================================================
        public void Handle(string name, bool on)
        {
            switch (name)
            {
                //接受上游指令
                case "SendAble":
                    if (!Form1.Instance.Eq2EqEnabled) break;
                    HandleSendAble(on);
                    break;

                case "SendStart":
                    if (!Form1.Instance.Eq2EqEnabled) break;
                    HandleSendStart(on);
                    break;
                case "SendComplete":
                    if (!Form1.Instance.Eq2EqEnabled) break;
                    HandleSendComplete(on);
                   
                    break;
                //接受下游指令
                case "ReceiveAble":
                    if (!Form1.Instance.Eq2EqEnabled) break;
                    HandleReceiveAble(on);
                    break;
                case "ConveyerState":
                    HandleConveyerState(on);
                    break;
                case "ReceiveComplete":
                    HandleReceiveComplete(on);
                    break;
                default:
                    _log($"⚠ 未实现的 LinkSignal 事件：{name}");
                    break;
            }
        }

        //作为下游监控上游
        // =============================================================
        // SendAble → ON 时读取 JobData（150 WORD） → 发给 C++
        // =============================================================
        private void HandleSendAble(bool on)
        {            

            if (!on) return;   // OFF 不做读取动作

            // ==========================
            // 读取 PLC 数据
            // ==========================
            const string tag = "RV_EQToEQ_LinkSignal_02_03_00";

            int[] data = _readWordArray(tag);
            if (data == null)
            {
                _log($"❌ SendAble: 无法读取 Tag={tag}");
                return;
            }
            if (data.Length < 160)
            {
                _log($"❌ SendAble: 数据长度不足（需要 ≥160 WORD）");
                return;
            }

            // ==========================
            // 读取 JobData（150 Words）
            // Word[6] ~ Word[155]
            // ==========================
            int[] job = new int[150];
            Array.Copy(data, 6, job, 0, 150);

            LocalFileLogger.Info("EQ2EQ", "Upstream SendAble ON, jobLen=150 tag=" + tag);
    
            SendToCpp("SendAbleJobData", new
            {
                length = 150,
                job = job
            });

        }


     
        private void HandleSendStart(bool on)
        {
            if (!on) return;   // OFF 不做读取动作
            WriteReceiveStart(true);
            SendToCpp("SendStart", new
            {
                value = on
            });
        }
        private void HandleSendComplete(bool on)
        {
            if (!on) return;   // OFF 不做任何处理

            // ① 上游 SendComplete → 我方需要清除 ReceiveStart
            WriteReceiveStart(false);           
            SendToCpp("SendComplete", new
            {
                value = on
            });
        }

        //作为上游监控下游
        // =============================================================
        // ReceiveAble
        // =============================================================
        private void HandleReceiveAble(bool on)
        {
            if (!on) return;   // OFF 不做读取动作

            // 上游收到 ReceiveAble=true → 我方要发送下游 SendStart=ON
            WriteSendStartToDownstream(true);
        }
        private void HandleConveyerState(bool on)
        {
            if (!on) return;   // OFF 不做任何处理
            SendToCpp("ConveyerState", new
            {
                value = on
            });
            // 发给下游 EQ
            WriteConveyerStateToDownstream(true);
        }
        private void HandleReceiveComplete(bool on)
        {
            if (on)
            {            

                // ① 通知 C++
                SendToCpp("ReceiveComplete", new { value = true });

                // ② 关闭 ConveyerState
                WriteConveyerStateToDownstream(false);

                // ③ 打开 SendComplete
                WriteSendCompleteToDownstream(true);
            }
            else
            {
    
                // ① SendAble = OFF
                WriteSendAbleToDownstream(false);

                // ② SendStart = OFF
                WriteSendStartToDownstream(false);

                // ③ SendComplete = OFF
                WriteSendCompleteToDownstream(false);

                _log("✔ ReceiveComplete OFF → 已关闭下游 SendAble / SendStart / SendComplete");
            }
        }

        //辅助函数
        private void WriteReceiveStart(bool on)
        {
            const string tag = "SD_EQToEQ_LinkSignal_03_02_00";

            int[] words = _readWordArray(tag);
            if (words == null || words.Length <= 3)
            {
                _log($"❌ WriteReceiveStart: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[3].Bit4 ===
            if (on)
                words[3] |= (1 << 4);     // 置位 bit4
            else
                words[3] &= ~(1 << 4);    // 清零 bit4

            bool ok = _writeWordArray(tag, words);

            if (ok)
            {
                _log($"✔ 已写入上游 EQ  ReceiveStart = {(on ? "ON" : "OFF")} → {tag}[3].bit4");
            }
            else
            {
                _log("❌ 写入上游 EQ  ReceiveStart 失败");
            }
        }
        private void WriteSendStartToDownstream(bool on)
        {
            const string tag = "SD_EQToEQ_LinkSignal_03_04_00";

            int[] words = _readWordArray(tag);
            if (words == null || words.Length <= 0)
            {
                _log($"❌ WriteSendStartToDownstream: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[0].Bit4 ===
            if (on)
                words[0] |= (1 << 4);     // 置位 bit4
            else
                words[0] &= ~(1 << 4);    // 清零 bit4

            bool ok = _writeWordArray(tag, words);

            if (ok)
            {
                _log($"✔ 已写入下游 EQ SendStart = {(on ? "ON" : "OFF")} → {tag}[0].bit4");
            }
            else
            {
                _log("❌ 写入下游 EQ SendStart 失败");
            }
        }
        private void WriteConveyerStateToDownstream(bool on)
        {
            const string tag = "SD_EQToEQ_LinkSignal_03_04_00";

            int[] words = _readWordArray(tag);
            if (words == null || words.Length <= 0)
            {
                _log($"❌ WriteConveyerStateToDownstream: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[0].Bit11 ===
            if (on)
                words[0] |= (1 << 11);     // 置位 bit11
            else
                words[0] &= ~(1 << 11);    // 清零 bit11

            bool ok = _writeWordArray(tag, words);

            if (ok)
            {
                _log($"✔ 已写入下游 EQ ConveyerState = {(on ? "ON" : "OFF")} → {tag}[0].bit11");
            }
            else
            {
                _log("❌ 写入下游 EQ ConveyerState 失败");
            }
        }
        private void WriteSendCompleteToDownstream(bool on)
        {
            const string tag = "SD_EQToEQ_LinkSignal_03_04_00";

            int[] words = _readWordArray(tag);
            if (words == null || words.Length <= 0)
            {
                _log($"❌ WriteSendCompleteToDownstream: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[0].Bit5 ===
            if (on)
                words[0] |= (1 << 5);     // 置位 bit5
            else
                words[0] &= ~(1 << 5);    // 清零 bit5

            bool ok = _writeWordArray(tag, words);

            if (ok)
            {
                _log($"✔ 已写入下游 EQ SendComplete = {(on ? "ON" : "OFF")} → {tag}[0].bit5");
            }
            else
            {
                _log("❌ 写入下游 EQ SendComplete 失败");
            }
        }
        private void WriteSendAbleToDownstream(bool on)
        {
            const string tag = "SD_EQToEQ_LinkSignal_03_04_00";

            int[] words = _readWordArray(tag);
            if (words == null || words.Length <= 0)
            {
                _log($"❌ WriteSendAbleToDownstream: 读取 {tag} 失败");
                return;
            }

            // === 设置 Word[0].Bit3 ===
            if (on)
                words[0] |= (1 << 3);
            else
                words[0] &= ~(1 << 3);

            bool ok = _writeWordArray(tag, words);

            if (ok)
                _log($"✔ 已写入下游 EQ SendAble = {(on ? "ON" : "OFF")} → {tag}[0].bit3");
            else
                _log("❌ 写入下游 EQ SendAble 失败");
        }


       
        private bool SendToCpp(string cmd, object payload)
        {
            var json = JsonConvert.SerializeObject(new
            {
                cmd = cmd,
                data = payload
            });

            bool ok = _server.SendToClient(json);

          
            return ok;
        }
    }
}
