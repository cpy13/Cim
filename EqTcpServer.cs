using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EQModeChangeSimulator
{
    public class EqTcpServer
    {
        private readonly Action<string> _log;
        private readonly Action<string> _onJson;

        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private Thread _acceptThread;
        private TcpClient _currentClient;
        private NetworkStream _currentStream;
        private readonly Action _onClientConnected;
        public EqTcpServer(
    Action<string> log,
    Action<string> onJson,
    Action onClientConnected = null)
        {
            _log = log;
            _onJson = onJson;
            _onClientConnected = onClientConnected;
        }

        public void Start(int port)
        {
            if (_listener != null)
                return;

            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            LocalFileLogger.Info("TCP", "Server listening port=" + port);
            _log($"[TCP] EQ TCP Server 启动，监听端口: {port}");

            // ★★ 启动真正的后台线程，不做任何同步阻塞 ★★
            _acceptThread = new Thread(() => AcceptLoop(_cts.Token));
            _acceptThread.IsBackground = true;
            _acceptThread.Start();

            _log("[TCP] AcceptLoop 后台线程已启动");
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _listener = null;

                LocalFileLogger.Info("TCP", "Server stopped");
                _log("[TCP] EQ TCP Server 已停止");
            }
            catch (Exception ex)
            {
                LocalFileLogger.Error("TCP", "Stop failed: " + ex.Message);
                _log("[TCP] Stop 异常: " + ex.Message);
            }
        }

        // ★★ 注意：这是同步循环，不是异步 ★★
        private void AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = _listener.AcceptTcpClient(); // sync wait
                    LocalFileLogger.Info("TCP", "Client connected remote=" + client.Client.RemoteEndPoint);
                    _log("[TCP] 客户端连接进来: " + client.Client.RemoteEndPoint);

                    // 处理客户端（异步）
                    _ = HandleClientAsync(client, token);
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        LocalFileLogger.Error("TCP", "Accept failed: " + ex.Message);
                        _log("[TCP] Accept 异常: " + ex.Message);
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            // ★ 记录当前客户端（用于回发）
            _currentClient = client;
            _currentStream = client.GetStream();
            _onClientConnected?.Invoke();   // 新增：客户端已连接通知
            var stream = client.GetStream();
            byte[] buffer = new byte[4096];

            while (!token.IsCancellationRequested && client.Connected)
            {
                try
                {
                    int len = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (len == 0)
                    {
                        LocalFileLogger.Warn("TCP", "Client disconnected");
                        _log("[TCP] 客户端断开连接");
                        break;
                    }

                    string json = Encoding.UTF8.GetString(buffer, 0, len);
                    _log($"[TCP] 收到 JSON: {json}");

                    foreach (var part in json.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(part))
                            _onJson(part.Trim());
                    }
                }
                catch (Exception ex)
                {
                    LocalFileLogger.Error("TCP", "Read failed: " + ex.Message);
                    _log("[TCP] Read 异常: " + ex.Message);
                    break;
                }
            }

            client.Close();
            LocalFileLogger.Info("TCP", "Client handler ended");
            _log("[TCP] HandleClient 结束");
        }

        /// <summary>
        /// 发送 JSON 文本给当前 C++ 客户端
        /// </summary>
        //public bool SendToClient(string json)
        //{
        //    try
        //    {
        //        if (_currentClient == null ||
        //            !_currentClient.Connected ||
        //            _currentStream == null)
        //        {
        //            _log("[TCP] 发送失败：无可用客户端连接");
        //            return false;   // ★ 返回失败
        //        }

        //        // C++ Qt 必须已 '\n' 结束一条消息
        //        string msg = json + "\n";
        //        byte[] bytes = Encoding.UTF8.GetBytes(msg);

        //        _currentStream.Write(bytes, 0, bytes.Length);
        //        _currentStream.Flush();

        //        //_log($"[TCP] 发送给 C++ 客户端: {msg}");
        //        return true;   // ★ 返回成功
        //    }
        //    catch (Exception ex)
        //    {
        //        _log("[TCP] SendToClient 异常: " + ex.Message);
        //        return false;   // ★ 返回失败
        //    }
        //}

        private readonly object _sendLock = new object();

        public bool SendToClient(string json)
        {
            try
            {
                if (_currentClient == null ||
                    !_currentClient.Connected ||
                    _currentStream == null)
                {
                    LocalFileLogger.Warn("TCP", "Send failed: no client");
                    _log("[TCP] 发送失败：无可用客户端连接");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    LocalFileLogger.Warn("TCP", "Send failed: empty json");
                    _log("[TCP] 发送失败：json 为空");
                    return false;
                }

                // 保证每条消息只带一个换行分隔符
                string msg = json.EndsWith("\n") ? json : json + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(msg);

                lock (_sendLock)
                {
                    _currentStream.Write(bytes, 0, bytes.Length);
                    _currentStream.Flush();
                }

                return true;
            }
            catch (Exception ex)
            {
                LocalFileLogger.Error("TCP", "Send failed: " + ex.Message);
                _log("[TCP] SendToClient 异常: " + ex.Message);
                return false;
            }
        }
    }
}
