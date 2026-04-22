using EQModeChangeSimulator;
using Newtonsoft.Json;

public class CppClientSender
{
    private readonly EqTcpServer _server;

    public CppClientSender(EqTcpServer server)
    {
        _server = server;
    }

    public void SendToCpp(string cmd, object payload)
    {
        var json = JsonConvert.SerializeObject(
            new { cmd = cmd, data = payload }
        );

        _server.SendToClient(json);
    }
}
