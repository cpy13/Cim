using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using Newtonsoft.Json.Linq;
using EQModeChangeSimulator;

internal static class ResetOfflineTests
{
    private const string Up = "SD_EQToEQ_LinkSignal_03_02_00";
    private const string Down = "SD_EQToEQ_LinkSignal_03_04_00";
    private const string UpInput = "RV_EQToEQ_LinkSignal_02_03_00";
    private const string DownInput = "RV_EQToEQ_LinkSignal_04_03_00";
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private sealed class Memory
    {
        internal readonly Dictionary<string, int[]> Tags = new();
        internal readonly List<string> Writes = new(), Messages = new(), Logs = new();
        internal readonly List<string> Operations = new();
        internal string NullTag, ThrowReadTag, FailWriteTag, ThrowWriteTag;
        internal LocalCimSignalReset Reset;
        internal Memory()
        {
            Tags[Up] = Enumerable.Repeat(0xFFFF, 160).ToArray();
            Tags[Down] = Enumerable.Repeat(0xA5FF, 160).ToArray();
            Tags[UpInput] = new int[160];
            Tags[DownInput] = new int[160];
            Reset = new LocalCimSignalReset(SignalMonitorForm.Signals, Read, Write,
                Logs.Add, Notify);
        }
        internal bool Notify(string cmd, object data) {
            Messages.Add(cmd); Operations.Add("notify:" + cmd); return true;
        }
        internal int[] Read(string tag)
        {
            Operations.Add("read:" + tag);
            if (tag == ThrowReadTag) throw new Exception("simulated read exception");
            return tag == NullTag ? null : (int[])Tags[tag].Clone();
        }
        internal bool Write(string tag, int[] words)
        {
            Check(tag == Up || tag == Down, "must never write peer input");
            Writes.Add(tag);
            Operations.Add("write:" + tag);
            if (tag == ThrowWriteTag) throw new Exception("simulated write exception");
            if (tag == FailWriteTag) return false;
            Tags[tag] = (int[])words.Clone(); return true;
        }
        internal void Clear(string origin) => Reset.Clear(origin);
    }

    private static CimLinkOfflineHarness Pipeline(Memory memory) {
        var handler = new LinkSignalHandler(memory.Read, memory.Write, memory.Logs.Add,
            json => { memory.Messages.Add((string)JObject.Parse(json)["cmd"]); return true; }, () => true);
        return new CimLinkOfflineHarness(memory.Read, handler, memory.Logs.Add);
    }
    private static void Output(Memory memory, string handler, bool value) {
        // Exercise the real normal command writer, without constructing the networked Form1 UI.
        var router = new CppCommandRouter(new Context(memory));
        typeof(CppCommandRouter).GetMethod(handler, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(router, new object[] { JObject.FromObject(new { data = new { value } }) });
    }
    private static void CompareHeadManualReference() {
        var original = new Memory(); var manual = new Memory(); var remote = new Memory();
        new HeadManualResetReference(original.Read, original.Write, original.Notify).Clear();
        manual.Clear("SignalMonitorReset");
        new CppCommandRouter(new Context(remote)).HandleRawJson("{\"cmd\":\"ClearLocalCimSignals\"}");
        foreach (var tag in original.Tags.Keys) {
            Check(original.Tags[tag].SequenceEqual(manual.Tags[tag]), "manual result must equal Git HEAD");
            Check(original.Tags[tag].SequenceEqual(remote.Tags[tag]), "remote result must equal Git HEAD");
        }
        Check(original.Operations.SequenceEqual(manual.Operations) && original.Operations.SequenceEqual(remote.Operations),
            "HEAD order: read/write local upstream, read/write local downstream, one EQResetCommand; no input reads");
        Console.WriteLine("PASS Git HEAD actual manual body: identical target arrays and read/write/notify order for manual and remote");
    }
    private static void TestRestoredHandshake() {
        var m = new Memory(); var link = Pipeline(m);
        m.Tags[UpInput][0] = 0x38; link.Process(UpInput);
        m.Clear("D33Reset");
        Check(link.State("SendAble") && link.State("SendStart") && link.State("SendComplete"),
            "clear must not rewrite existing LinkStates");
        m.Messages.Clear();
        // Field order: clear local -> upstream clears its handshake -> new board -> next outfeed.
        m.Tags[UpInput][0] = (1 << 2) | (1 << 11); link.Process(UpInput);
        m.Tags[UpInput][0] |= 1 << 3; link.Process(UpInput);
        Check(m.Messages.Contains("SendAbleJobData"), "next upstream request reaches AOI without all-five-low");
        Output(m, "HandleReceiveAbleFromCpp", true);
        m.Tags[UpInput][0] |= 1 << 4; link.Process(UpInput);
        Check((m.Tags[Up][3] & 0x18) == 0x18 && m.Messages.Contains("SendStart"), "normal next infeed starts");
        Output(m, "HandleReceiveCompleteFromCpp", true);
        m.Tags[UpInput][0] |= 1 << 5; link.Process(UpInput);
        Check((m.Tags[Up][3] & (1 << 4)) == 0 && m.Messages.Contains("SendComplete"), "normal upstream completion");
        foreach (var handler in new[] { "HandleConveyerStateFromCpp", "HandleGlassExistArm1FromCpp",
            "HandleReceiveAbleFromCpp", "HandleJobTransferSignalFromCpp", "HandleReceiveCompleteFromCpp" }) Output(m, handler, false);
        Check((m.Tags[Up][3] & 0x83C) == 0 && (m.Tags[Up][4] & 1) == 0, "normal AOI infeed cleanup");
        Output(m, "HandleSendAbleFromCpp", true);
        m.Tags[DownInput][3] = (1 << 2) | (1 << 3); link.Process(DownInput);
        Check((m.Tags[Down][0] & 0x18) == 0x18, "next outfeed SendAble/SendStart");
        m.Tags[DownInput][3] |= 1 << 11; link.Process(DownInput);
        Check((m.Tags[Down][0] & (1 << 11)) != 0, "downstream conveyor callback");
        m.Tags[DownInput][3] |= 1 << 5; link.Process(DownInput);
        Check((m.Tags[Down][0] & (1 << 5)) != 0 && (m.Tags[Down][0] & (1 << 11)) == 0, "normal outfeed completion ON");
        m.Tags[DownInput][3] &= ~(1 << 5); link.Process(DownInput);
        Check((m.Tags[Down][0] & 0x38) == 0 && (m.Tags[DownInput][3] & (1 << 3)) != 0,
            "completion OFF is handled while ReceiveAble stays ON");
        Console.WriteLine("PASS actual Form1 edge body + Router writers + LinkHandler: clear, upstream clears, next infeed and outfeed");

        var ready = new Memory(); var readyLink = Pipeline(ready);
        ready.Tags[DownInput][3] = 1 << 3; readyLink.Process(DownInput);
        ready.Clear("D33Reset");
        Check(readyLink.State("ReceiveAble"), "previously observed ReceiveAble ON history is preserved");
        Output(ready, "HandleSendAbleFromCpp", true); readyLink.Process(DownInput);
        Check((ready.Tags[Down][0] & (1 << 4)) == 0,
            "HEAD edge-only behavior: constant previously-observed Ready does not synthesize a new SendStart");
        ready.Tags[DownInput][3] |= 1 << 11; readyLink.Process(DownInput);
        Check((ready.Tags[Down][0] & (1 << 11)) != 0, "constant Ready must not block other input events");
        ready.Tags[DownInput][3] |= 1 << 5; readyLink.Process(DownInput);
        Check((ready.Tags[Down][0] & (1 << 5)) != 0, "constant Ready must not block ReceiveComplete ON");
        ready.Tags[DownInput][3] &= ~(1 << 5); readyLink.Process(DownInput);
        Check((ready.Tags[Down][0] & 0x38) == 0, "constant Ready must not block ReceiveComplete OFF");
        Console.WriteLine("PASS ReceiveAble continuously ON: no whole-tag inhibition; preserved HEAD no-new-edge limitation is explicit");

        var unseen = new Memory(); var unseenLink = Pipeline(unseen);
        unseen.Tags[DownInput][3] = 1 << 3; unseen.Clear("D33Reset");
        Check(!unseenLink.State("ReceiveAble"), "clear does not seed an unobserved Ready into LinkStates");
        Output(unseen, "HandleSendAbleFromCpp", true); unseenLink.Process(DownInput);
        Check((unseen.Tags[Down][0] & (1 << 4)) != 0, "pending Ready event is handled after clear without forcing OFF");
        var early = new Memory(); var earlyLink = Pipeline(early);
        early.Tags[UpInput][0] = 1 << 4; earlyLink.Process(UpInput); early.Clear("D33Reset");
        early.Messages.Clear(); early.Tags[UpInput][0] |= 1 << 3; earlyLink.Process(UpInput);
        Check(early.Messages.Contains("SendAbleJobData"), "new SendAble is not swallowed while other peer bits are held");
        Console.WriteLine("PASS pending ReceiveAble and early upstream SendAble are not swallowed by reset recovery");

        var late = new Memory(); var lateLink = Pipeline(late); late.Clear("D33Reset");
        late.Tags[UpInput][0] = 1 << 4; lateLink.Process(UpInput);
        late.Tags[DownInput][3] = 1 << 3; lateLink.Process(DownInput);
        late.Tags[DownInput][3] |= 1 << 11; lateLink.Process(DownInput);
        Check((late.Tags[Up][3] & (1 << 4)) != 0 && (late.Tags[Down][0] & (1 << 4)) != 0 &&
            late.Messages.Contains("SendStart") && late.Messages.Contains("ConveyerState"),
            "retained HEAD risk: late callbacks may reassert outputs and send transport commands to AOI");
        Console.WriteLine("PASS documented unchanged risk: late handshake callbacks can reassert ON / notify AOI transport");
    }
    private sealed class Context : IEqContext
    {
        internal readonly Memory Memory;
        internal int Calls;
        internal Context(Memory memory) { Memory = memory; }
        public void ClearLocalCimSignals(string origin) { ++Calls; Memory.Clear(origin); }
        public void Log(string s) => Memory.Logs.Add(s);
        public int[] ReadWordArray(string tag) => Memory.Read(tag);
        public bool WriteWordArray(string tag, int[] value) => Memory.Write(tag, value);
        public void TriggerEvent(string e) => throw new Exception("Unexpected report");
        public bool SendEventAndBlock(string e, Action<int[]> writer) => throw new Exception("Unexpected report");
        public bool RequestJobData(string j, int c, int s, int o, bool n) => throw new Exception("Unexpected job request");
        public bool CompleteRecipeParameterRequestCommandReply(int a, int b, int c, int d, int e, int f, int g,
            int h, int i, int j) => throw new Exception("Unexpected recipe request");
    }
    [STAThread]
    private static int Main()
    {
        try
        {
            CompareHeadManualReference();
            var m = new Memory();
            var before = m.Tags.ToDictionary(x => x.Key, x => (int[])x.Value.Clone());
            var ctx = new Context(m);
            var router = new CppCommandRouter(ctx);
            router.HandleRawJson("{\"cmd\":\"ClearLocalCimSignals\",\"data\":{}}");
            Check(ctx.Calls == 1 && m.Writes.SequenceEqual(new[] { Up, Down }), "real router / two output tags");
            // Independent expected masks; checks ALL words, not just the target bits.
            foreach (var tag in before.Keys)
                for (int w = 0; w < before[tag].Length; ++w)
                {
                    int expected = before[tag][w];
                    if ((tag == Up && w == 3) || (tag == Down && w == 0)) expected &= ~0x083C;
                    if ((tag == Up || tag == Down) && w == 4) expected &= ~1;
                    Check(m.Tags[tag][w] == expected, "12-bit scope / Inline / Job / input preservation");
                }
            Check(m.Messages.SequenceEqual(new[] { "EQResetCommand" }), "one legacy notification, no new reply");
            router.HandleRawJson("{\"cmd\":\"ClearLocalCimSignals\",\"data\":{}}");
            Check(m.Writes.Count == 2 && m.Messages.Count == 2, "duplicate command: no redundant writes, one notification per call");
            var manual = new Memory(); manual.Clear("SignalMonitorReset");
            foreach (var tag in new[] { Up, Down })
                Check(manual.Tags[tag].SequenceEqual(m.Tags[tag]), "same manual/remote routine");
            Console.WriteLine("PASS real Router, shared routine without monitor window, exact 12 bits, duplicate writes");

            foreach (var failure in new[] { "null", "short", "read", "false", "write" })
            {
                var f = new Memory();
                if (failure == "null") f.NullTag = Up;
                if (failure == "short") f.Tags[Up] = new int[3];
                if (failure == "read") f.ThrowReadTag = Up;
                if (failure == "false") f.FailWriteTag = Up;
                if (failure == "write") f.ThrowWriteTag = Up;
                new CppCommandRouter(new Context(f)).HandleRawJson("{\"cmd\":\"ClearLocalCimSignals\"}");
                Check(f.Logs.Any(x => x.Contains("Clear failed")) && f.Logs.Any(x => x.Contains("writeCallsOk=False")), "failure log");
                Check(f.Writes.Count(x => x == Down) == 1 && f.Messages.Count == 1, "partial failure continues once, no retry");
            }
            Console.WriteLine("PASS null/short/read exception/write false/write exception; partial failure, no retry or false success");

            TestRestoredHandshake();

            var framing = new JsonLineBuffer();
            var input = Encoding.UTF8.GetBytes("{\"cmd\":\"ClearLocalCimSignals\",\"data\":{\"reason\":\"复位\"}}\r\n{}\n");
            var lines = new List<string>();
            foreach (var b in input) lines.AddRange(framing.Append(new[] { b }, 1));
            Check(lines.Count == 2 && lines[0].Contains("复位") && lines[1] == "{}", "split UTF8 / CRLF / coalescing");
            Check(new JsonLineBuffer().Append(Encoding.UTF8.GetBytes("{}"), 2).Count == 0, "partial line never executes");
            Console.WriteLine("PASS TCP framing with one-byte chunks, UTF8, multiple messages, incomplete command");

            using (var owner = new Control())
            {
                var handle = owner.Handle; // Invisible generic control; never construct Compolet/Form1.
                int ownerThread = Thread.CurrentThread.ManagedThreadId;
                var threaded = new Memory();
                Exception workerError = null;
                int done = 0;
                var worker = new Thread(() => {
                    try
                    {
                        CimUiThread.Run(owner, () => {
                            Check(Thread.CurrentThread.ManagedThreadId == ownerThread, "clear runs on owner");
                            threaded.Clear("D33Reset");
                        });
                        CimUiThread.Run(owner, () => {
                            var words = threaded.Read(Down);
                            words[6] = 123; // Subsequent normal Job write must preserve the cleared handshake.
                            threaded.Write(Down, words);
                        });
                    }
                    catch (Exception ex) { workerError = ex; }
                    finally { Interlocked.Exchange(ref done, 1); }
                });
                worker.IsBackground = true;
                worker.Start();
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (Volatile.Read(ref done) == 0 && DateTime.UtcNow < deadline)
                {
                    Application.DoEvents(); Thread.Sleep(1);
                }
                Check(Volatile.Read(ref done) == 1, "owner dispatch completed");
                worker.Join();
                if (workerError != null) throw workerError;
                Check(threaded.Tags[Down][6] == 123 && (threaded.Tags[Down][0] & 0x83C) == 0,
                    "whole RMW operations ordered on owner thread");
            }
            Console.WriteLine("PASS actual shared WinForms owner dispatcher from background thread; ordered clear and Job RMW");
            Console.WriteLine("ALL CIM OFFLINE TESTS PASSED; no Form1 instance, Compolet instance, sockets or physical PLC");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
    }
}
