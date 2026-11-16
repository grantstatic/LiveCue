# LiveCue — Master Validation Register
All tests below are **NOT VERIFIED** until evidence is recorded. Do not use on a live show yet.

| ID | Test | Expected result | Status |
|---|---|---|---|
| WIN-001 | Windows GitHub Actions restore/build | Compilation succeeds | Pending |
| WIN-002 | Publish self-contained win-x64 | LiveCue.exe artifact exists | Pending |
| WIN-003 | Run EXE on Windows 10/11 without SDK | Launches without dependencies | Pending |
| WIN-004 | Startup and CPU/RAM test | Responsive on standard laptop | Pending |
| WIN-005 | Extended run and crash recovery | No data loss or instability | Pending |
| DOC-001 | Create/edit/save/reopen RTF | Content and formatting retained | Pending |
| DOC-002 | Import TXT/RTF | Characters and paragraphs correct | Pending |
| DOC-003 | Mixed colour and bold formatting | Presenter reflects formatting | Pending |
| DOC-004 | Long script and Unicode | No hangs or corruption | Pending |
| DOC-005 | Autosave and recovery | Restores unsaved edits | Pending |
| DOC-006 | DOCX import | Not implemented | Blocked |
| OUT-001 | Select second monitor | Correct physical display | Pending |
| OUT-002 | Fullscreen/floating output | Both modes work | Pending |
| OUT-003 | Mirror and font scaling | Not implemented in current GitHub baseline | Blocked |
| OUT-004 | Operator preview follows output | Accurate reading position | Pending |
| OUT-005 | Hotplug monitor and DPI change | No output loss | Pending |
| OUT-006 | Presenter clean output | No editor controls visible | Pending |
| SCR-001 | Continuous scrolling both directions | Smooth and stable | Pending |
| SCR-002 | Manual jogging and immediate stop | Precise movement | Pending |
| SCR-003 | Switch modes | No position jump | Pending |
| SCR-004 | Keyboard shortcuts and emergency pause | Correct behaviour with focus | Pending |
| SCR-005 | Push live edits | No unexpected position jump | Pending |
| SCR-006 | Frame pacing and 3-hour run | No drift or stalls | Pending |
| CTL-001 | Keyboard controller mappings | Current baseline fixed F5-F12 | Pending |
| CTL-002 | Keyboard-emulating jog controller | Operates reliably | Pending |
| CTL-003 | Native USB HID | Not implemented | Blocked |
| CTL-004 | MIDI | Not implemented | Blocked |
| CTL-005 | Controller unplug/replug | No uncontrolled scrolling | Pending |
| CTL-006 | Speed curve, dead zone, force feedback | Not implemented | Blocked |
| SAFE-001 | Close operator during output | Confirmation and clean close | Pending |
| SAFE-002 | Live update error | Presenter remains stable | Pending |
| SAFE-003 | Offline operation | Fully usable offline | Pending |
| SAFE-004 | Multi-hour show simulation | No failures | Pending |
| SAFE-005 | Rapid pause/reverse/jog/edit | No unexpected jumps | Pending |

Note: GitHub initial build baseline is a reduced testable subset of the prior v0.6 archive. Full v0.6 controller mapping, mirror and richer editing functions have not yet been migrated. Never mark these implemented until restored and validated.
