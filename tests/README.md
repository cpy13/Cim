# EQ05 local CIM signal reset offline tests

Build Release/x86, then run `tests/run-reset-tests.ps1`. All tags are in memory.
The test process never constructs Form1, VariableCompolet or a TCP listener/client.
An invisible generic WinForms Control verifies the shared owner-thread dispatcher.

The runner reads EQ05 pre-port 410dccc:SignalMonitorForm.cs and compiles its original signal
mapping, ClearSendSignals body and button execution order into a test reference.
The normal local upstream write, local downstream write and EQResetCommand order,
all target/non-target words, and manual/remote clear results are compared directly.
The original confirmation dialog remains in the unchanged SignalMonitorForm UI.

The runner also extracts the current Form1 HandleLinkSignal body and asserts that
it is identical to EQ05 pre-port 410dccc. That actual edge-dispatch body and actual link maps
are compiled against memory tags with the real LinkSignalHandler and Router
writers. Tests cover clear -> upstream clears -> next infeed -> next outfeed,
held unrelated peer bits, pending Ready, early upstream SendAble, and completion
ON/OFF while downstream ReceiveAble stays ON. There is no reset isolation, no
input baseline write, and no requirement for peer bits 2/3/4/5/11 to become OFF.

Known original behavior is deliberately retained and tested:
- Previously observed ReceiveAble=ON that stays ON does not produce a new edge.
  A subsequent local SendAble=ON writes only SendAble; it does not synthesize a
  new SendStart. This is an existing EQ05 edge-only limitation, not an isolation
  condition. Removing isolation does not claim to fix constant-Ready protocols.
- A late new SendStart/ReceiveAble edge may reassert local Start bits; Conveyor
  callbacks may notify AOI transport. No new recovery inhibition was added.

Existing shared-helper behavior from earlier work remains: all-zero targets skip
redundant writes; each invocation sends at most one legacy EQResetCommand, with
no request identity, new acknowledgement or retry. Read/write failures are logged
per tag, and the other tag plus the notification are still attempted. Unlike the
old EQ05 button's uncaught write exception, this does not abort the entire call.
This pre-existing error-handling difference was not changed by isolation removal.
Routine completion is not readback confirmation. UI lamps use the existing poll.

Other coverage includes exact twelve-bit scope, peer input/Inline/Job preservation,
duplicate commands, null/short/read/write failures, TCP fragmentation/coalescing,
UTF-8 and background-to-UI whole read/modify/write ordering. No production start,
PLC motion instruction, field connection or live recovery validation is performed.

## EQ05 port baseline and mappings

Source feature: final EQ04 commit aed79f3 (without all-low isolation).
Target baseline: EQ05 commit 410dccc296124691964b1a82327ead8b3f35772c.
The runner pins this baseline so comparisons remain valid after the port is committed.

- Local upstream output: SD_EQToEQ_LinkSignal_03_02_00, Word3 bits 2/3/4/5/11 and Word4 bit0.
- Local downstream output: SD_EQToEQ_LinkSignal_03_05_00, Word0 bits 2/3/4/5/11 and Word4 bit0.
- Upstream input: RV_EQToEQ_LinkSignal_02_03_00.
- Downstream input: RV_EQToEQ_LinkSignal_05_03_00.
- Downstream JobData remains words 6..155 of the EQ05 output tag.

EQ05-specific tests assert these mappings and execute the real downstream JobData
writer. Handshake handlers/tag writers and normal Router link/JobData functions
are checked against EQ05 baseline source. No field Compolet or equipment is used.