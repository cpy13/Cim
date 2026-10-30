$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$outDir = Join-Path $repo 'obj\reset-tests'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$vs = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -property installationPath
$compiler = Join-Path $vs 'MSBuild\Current\Bin\Roslyn\csc.exe'
$json = Join-Path $repo 'bin\x86\Release\Newtonsoft.Json.dll'
$compolet = Join-Path $repo 'VariableCompolet.dll'
$netstandard = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades\netstandard.dll'
$sources = @(Get-ChildItem -LiteralPath $repo -Filter '*.cs' | ForEach-Object FullName)
$sources += Join-Path $PSScriptRoot 'ResetOfflineTests.cs'

function Get-SourceSection([string]$source, [string]$start, [string]$end) {
    $a = $source.IndexOf($start)
    if ($a -lt 0) { throw "Missing source marker: $start" }
    $b = $source.IndexOf($end, $a + $start.Length)
    if ($b -lt 0) { throw "Missing source marker: $end" }
    return $source.Substring($a, $b - $a)
}
$baseline = '410dccc296124691964b1a82327ead8b3f35772c'
$headMonitor = (& git -C $repo show "${baseline}:SignalMonitorForm.cs") -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Cannot read EQ05 pre-port manual reset reference' }
$headForm = (& git -C $repo show "${baseline}:Form1.cs") -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Cannot read EQ05 pre-port link reference' }
$form = [IO.File]::ReadAllText((Join-Path $repo 'Form1.cs'))
$headHandler = (& git -C $repo show "${baseline}:LinkSignalHandler.cs") -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Cannot read EQ05 pre-port handshake reference' }
$handler = [IO.File]::ReadAllText((Join-Path $repo 'LinkSignalHandler.cs'))
$oldHandshake = Get-SourceSection $headHandler '        private void HandleSendAble(' '        private bool SendToCpp('
$newHandshake = Get-SourceSection $handler '        private void HandleSendAble(' '        private bool SendToCpp('
if ($oldHandshake.Replace("`r", '').Trim() -ne $newHandshake.Replace("`r", '').Trim()) {
    throw 'EQ05 normal handshake handlers or tag writers were changed'
}
$headRouter = (& git -C $repo show "${baseline}:CppCommandRouter.cs") -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Cannot read EQ05 pre-port Router reference' }
$router = [IO.File]::ReadAllText((Join-Path $repo 'CppCommandRouter.cs'))
$oldWriters = Get-SourceSection $headRouter '        private void HandleReceiveAbleFromCpp(' '        private int EncodeAsciiWord('
$newWriters = Get-SourceSection $router '        private void HandleReceiveAbleFromCpp(' '        private int EncodeAsciiWord('
if ($oldWriters.Replace("`r", '').Trim() -ne $newWriters.Replace("`r", '').Trim()) {
    throw 'EQ05 normal Router link writers or JobData handler were changed'
}
$signalDef = [regex]::Match($headMonitor, '(?s)        class SignalDef\s*\{.*?\n        \}').Value
$signals = [regex]::Match($headMonitor, '(?s)        private readonly List<SignalDef> Signals = new\(\)\s*\{.*?\n        \};').Value
if (!$signalDef -or !$signals) { throw 'Cannot extract HEAD signal definitions' }
$clear = Get-SourceSection $headMonitor '        private void ClearSendSignals(' '        private Control CreateSignalRow('
$notify = Get-SourceSection $headMonitor '        private void SendEqResetCommand()' '        private bool SendToCpp('
$button = [regex]::Match($headMonitor, '(?s)                // 清 EQ → 上游.*?                SendEqResetCommand\(\);').Value
if (!$button) { throw 'Cannot extract HEAD button execution order' }
$link = Get-SourceSection $form '        void HandleLinkSignal(' '        void HandleSingleCommand('
$headLink = Get-SourceSection $headForm '        void HandleLinkSignal(' '        void HandleSingleCommand('
if ($link.Replace("`r", '').Trim() -ne $headLink.Replace("`r", '').Trim()) {
    throw 'Normal LinkSignal edge dispatch differs from EQ05 pre-port baseline'
}
$maps = Get-SourceSection $form '        void InitLinkSignals()' '        private void LoadTimeoutConfig()'
$generated = @'
using System;
using System.Collections.Generic;
using LinkMap = EQModeChangeSimulator.Form1.LinkMap;
namespace EQModeChangeSimulator {
internal sealed class HeadManualResetReference {
    private readonly Func<string, int[]> _read;
    private readonly Func<string, object, bool> _notify;
    private readonly Dictionary<string, int[]> _lastSnapshot = new();
    private readonly WriteAdapter _compolet;
    private sealed class WriteAdapter {
        private readonly Func<string, int[], bool> _write;
        internal WriteAdapter(Func<string, int[], bool> write) { _write = write; }
        internal void WriteVariable(string tag, int[] words) {
            if (!_write(tag, words)) throw new InvalidOperationException("Simulated Compolet write failure");
        }
    }
    internal HeadManualResetReference(Func<string, int[]> read, Func<string, int[], bool> write,
        Func<string, object, bool> notify) { _read = read; _compolet = new WriteAdapter(write); _notify = notify; }
    private int[] ReadTag(string tag) => _read(tag);
    private void UpdateLamps(string tag, int[] words) { }
    private bool SendToCpp(string cmd, object data) => _notify(cmd, data);
'@ + "`n" + $signalDef + "`n" + $signals + "`n" + $clear + "`n" + $notify + "`n" +
    "internal void Clear() {`n$button`n}`n}`n" + @'
internal sealed class CimLinkOfflineHarness {
    private bool eq2eqEnabled = true;
    private readonly Dictionary<string, Form1.LinkMap> LinkMappings = new();
    private readonly Dictionary<string, bool> LinkStates = new();
    private readonly Func<string, int[]> _read;
    private readonly Action<string> _log;
    private readonly LinkSignalHandler _linkHandler;
    internal CimLinkOfflineHarness(Func<string, int[]> read, LinkSignalHandler handler, Action<string> log) {
        _read = read; _linkHandler = handler; _log = log; InitLinkSignals();
    }
    private int[] ReadWordArray(string tag) => _read(tag);
    private void Log(string text) => _log(text);
    private bool GetBit(int[] words, int word, int bit) => (words[word] & (1 << bit)) != 0;
    internal void Process(string tag) => HandleLinkSignal(tag);
    internal bool State(string name) => LinkStates[name];
'@ + "`n" + $maps + "`n" + $link + "`n} }`n"
$reference = Join-Path $outDir 'HeadManualAndLinkReference.cs'
[IO.File]::WriteAllText($reference, $generated, [Text.UTF8Encoding]::new($false))
$sources += $reference
$exe = Join-Path $outDir 'ResetOfflineTests.exe'
& $compiler /nologo /langversion:latest /target:exe /platform:x86 /main:ResetOfflineTests "/out:$exe" `
    /r:System.dll /r:System.Core.dll /r:System.Data.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    /r:System.Xml.dll /r:Microsoft.CSharp.dll "/r:$json" "/r:$compolet" "/r:$netstandard" $sources
if ($LASTEXITCODE -ne 0) { throw 'Offline test compilation failed' }
Copy-Item -LiteralPath $json -Destination $outDir -Force
Copy-Item -LiteralPath $compolet -Destination $outDir -Force
Push-Location $outDir
try {
    & $exe
    if ($LASTEXITCODE -ne 0) { throw 'Offline tests failed' }
} finally { Pop-Location }
