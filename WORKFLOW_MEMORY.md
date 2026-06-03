# Workflow Memory: Kangaroo Jumping Robot Redesign

This file records practical lessons from this project so similar CAD + GitHub + animation tasks can be executed faster next time.

## 1. Project Context

Goal:

- Redesign a failed mechanical design course project into a defensible kangaroo-inspired jumping robot.
- Deliver reports, diagrams, CAD assets, SolidWorks models, animation, and GitHub upload.

Final GitHub repository:

- https://github.com/zhijiangong0606-cloud/kangaroo-jumping-robot-redesign

Core final model:

- `solidworks_basic_model/KangarooRobot_completed_engineering_layout.SLDASM`

Core animation:

- `solidworks_basic_model/mechanism_motion_animation.gif`
- `solidworks_basic_model/mechanism_motion_animation.webp`

## 2. Main Technical Decisions

### Mechanism Architecture

Use a motor-preloaded elastic jumping system instead of direct motor jumping.

Reason:

- Small motors usually cannot provide enough instantaneous jumping power.
- A slow motor can preload an elastic tendon.
- A latch/servo release can provide rapid power output.
- This maps better to kangaroo tendon energy storage.

Final mechanism:

- 1-DOF planar closed-chain hind-leg mechanism.
- Motor + winding drum for preload.
- Elastic tendon for energy storage.
- Servo latch for release.
- Tail rod + tail mass for pitch stability.

Key dimensions:

| Item | Value |
|---|---:|
| H0-H1 | 100 mm |
| H0-H2 | 134 mm |
| L1 H1-A | 40 mm |
| L2 A-B | 120 mm |
| L3 H0-B | 100 mm |
| L4 B-F | 140 mm |
| L5 H2-F | 180 mm |
| Tail rod | 180-220 mm |

Mobility proof:

```text
M = 3(n - 1) - 2j = 3(6 - 1) - 2*7 = 1
```

## 3. Problems Encountered And Solutions

### 3.1 Inventor Was Not Actually Available

Problem:

- The user initially allowed Inventor modeling.
- Local check showed only Autodesk Sync, Desktop Connect, DWG TrueView; full Inventor was not confirmed.

Solution:

- Do not depend on automated Inventor modeling.
- Generate DXF files and modeling guides instead.
- Later, after SolidWorks was configured, switch to SolidWorks API.

Reusable check:

```powershell
Get-ChildItem -LiteralPath 'C:\Program Files\Autodesk' -Force
```

### 3.2 SolidWorks COM Via PowerShell Was Unreliable

Problem:

- `New-Object -ComObject SldWorks.Application` could create an object.
- Direct property/method access in PowerShell produced type library errors such as:

```text
TYPE_E_ELEMENTNOTFOUND
Element not found.
```

Solution:

- Use VBScript for simple SolidWorks automation probes.
- Use C# with SolidWorks Interop DLLs for stronger API calls.

Working SolidWorks version:

```text
SldWorks.Application.34
Revision: 34.0.0
SolidWorks 2026
```

SolidWorks install location found:

```text
D:\solid26\SOLIDWORKS\SLDWORKS.exe
```

Interop DLLs:

```text
D:\solid26\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll
D:\solid26\SOLIDWORKS\api\redist\SolidWorks.Interop.swconst.dll
```

### 3.3 SolidWorks API `AddComponent5` Initially Failed

Problem:

- `AssemblyDoc.AddComponent5(path, ...)` returned null for every component.
- This happened in both VBScript and C#.

Root cause:

- SolidWorks component insertion can fail if the part is not loaded first.

Solution:

- Open each part silently with `OpenDoc6` before calling `AddComponent5`.

Working C# pattern:

```csharp
int errors = 0, warnings = 0;
var part = (ModelDoc2)swApp.OpenDoc6(
    path,
    (int)swDocumentTypes_e.swDocPART,
    (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
    "",
    ref errors,
    ref warnings
);

var comp = asm.AddComponent5(
    path,
    (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig,
    "",
    false,
    "",
    x * MM,
    y * MM,
    z * MM
);
```

### 3.4 SolidWorks `SaveAs3` Return Code Was Misread

Problem:

- VBScript `SaveAs3` returned `0`, initially interpreted as failure.

Solution:

- In this context, `SaveAs3 errorCode=0` means no error.
- Verify by checking file existence and file size.

Check:

```powershell
Get-Item -LiteralPath 'path\to\file.SLDPRT' | Select-Object Name,Length,LastWriteTime
```

### 3.5 Chinese Text Corruption In Python/Powershell Pipe

Problem:

- Python scripts passed through PowerShell here-strings sometimes corrupted Chinese text in generated images.
- Symptoms: text appeared as `????` or mojibake.

Solution:

- For scripts containing Chinese strings, write the script as a UTF-8 file with `apply_patch`.
- Then execute the script file.
- Avoid piping large Chinese Python scripts directly through PowerShell.

Reliable pattern:

```powershell
& 'C:\Users\Gzj\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' 'script.py'
```

### 3.6 Bundled Python Had PIL But Not Matplotlib/ImageIO

Problem:

- System Python lacked PIL.
- Bundled Python had PIL but did not have matplotlib or imageio.

Solution:

- Use bundled Python from workspace dependencies.
- Use PIL directly for diagrams, GIFs, and WebP animations.

Bundled Python path:

```text
C:\Users\Gzj\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe
```

Check modules:

```powershell
@'
from PIL import Image
import PIL
print(PIL.__version__)
'@ | & 'C:\Users\Gzj\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -
```

### 3.7 FFmpeg Was Not Available

Problem:

- `ffmpeg` was not available in the environment.

Solution:

- Generate GIF and animated WebP with PIL.
- This is sufficient for mechanism demonstrations and GitHub/PPT usage.

PIL GIF pattern:

```python
frames[0].save(
    "mechanism_motion_animation.gif",
    save_all=True,
    append_images=frames[1:],
    duration=55,
    loop=0,
    optimize=False,
    disposal=2,
)
```

### 3.8 GitHub Upload Needed Local Git Identity

Problem:

- `git commit` failed because Git user name/email were not configured.

Solution:

- Configure identity locally for the repository, not globally.

Commands:

```powershell
git config user.name "zhijiangong0606-cloud"
git config user.email "zhijiangong0606-cloud@users.noreply.github.com"
git commit -m "Initial kangaroo jumping robot redesign package"
```

### 3.9 GitHub CLI Was Already Authenticated

Useful check:

```powershell
gh auth status
```

Successful account:

```text
zhijiangong0606-cloud
```

Repository creation and push:

```powershell
gh repo create kangaroo-jumping-robot-redesign --public --source . --remote origin --push
```

Verify:

```powershell
gh repo view zhijiangong0606-cloud/kangaroo-jumping-robot-redesign --json name,url,visibility,defaultBranchRef,pushedAt
git status --short --branch
git remote -v
```

## 4. Useful Implementation Techniques

### 4.1 Generate CAD-Friendly DXF First

When full CAD automation is uncertain:

- Generate simple DXF files with lines/circles.
- Keep units in mm.
- Include a verified dimensions CSV.

This lets the project proceed even if Inventor/SolidWorks automation is blocked.

### 4.2 Use Geometry Solving For Mechanism Animation

Do not manually animate link positions.

Use circle intersections:

- A is determined by input crank angle.
- B is intersection of circle centered at A with radius L2 and circle centered at H0 with radius L3.
- F is intersection of circle centered at B with radius L4 and circle centered at H2 with radius L5.

This produces a defensible closed-chain animation.

### 4.3 Generate Key-Frame Sheets From Real GIF Frames

Problem:

- Re-rendering small key-frame canvases caused cropped titles.

Solution:

- Extract real frames from the final GIF and resize them into a key-frame sheet.

Pattern:

```python
from PIL import Image, ImageSequence

im = Image.open("mechanism_motion_animation.gif")
for idx, frame in enumerate(ImageSequence.Iterator(im)):
    if idx in picks:
        fr = frame.convert("RGB").resize((660, 401), Image.Resampling.LANCZOS)
```

### 4.4 Avoid Checking In Local Build Artifacts

Use `.gitignore` for:

```text
~$*
*.exe
*.dll
*.pdb
*.obj
__pycache__/
.pytest_cache/
```

Reason:

- SolidWorks Interop DLLs and compiled helper EXEs are local dependencies, not project assets.
- SolidWorks lock files such as `~$*.SLDASM` should not be committed.

### 4.5 Preserve Core Deliverables In Git

Keep these even if binary:

- `*.SLDASM`
- `*.SLDPRT`
- `*.stl`
- `*.dxf`
- `*.gif`
- `*.webp`
- final render PNGs
- reports and BOM files

Reason:

- This is a mechanical design deliverable, not only source code.
- GitHub repository should be directly useful to the user and teacher.

## 5. Recommended Future Workflow For Similar Tasks

1. Confirm user goal and decide whether the project is concept design, CAD model, animation, or GitHub delivery.
2. Check installed CAD tools:

```powershell
Get-ChildItem 'C:\Program Files','D:\' -Recurse -Filter SLDWORKS.exe -ErrorAction SilentlyContinue
Get-ChildItem 'C:\Program Files\Autodesk' -Force -ErrorAction SilentlyContinue
```

3. If SolidWorks is available:
   - Probe with VBScript first.
   - Use C# Interop for assembly generation.
   - Open parts before `AddComponent5`.
4. If CAD API is blocked:
   - Generate DXF/STL with Python.
   - Provide assembly tables and render diagrams.
5. For mechanism animation:
   - Use geometry solving.
   - Generate GIF/WebP with PIL.
   - Generate a key-frame sheet from actual animation frames.
6. Before GitHub upload:
   - Add README.
   - Add `.gitignore`.
   - Check file sizes.
   - Configure local git identity if needed.
   - Use `gh repo create --source . --push`.
7. Verify remote URL with `gh repo view`.

## 6. Important Paths From This Project

Project root:

```text
C:\Users\Gzj\Desktop\kangaroo_robot_redesign
```

SolidWorks model folder:

```text
C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model
```

Bundled Python:

```text
C:\Users\Gzj\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe
```

SolidWorks install:

```text
D:\solid26\SOLIDWORKS
```

SolidWorks templates:

```text
C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates
```

SolidWorks Interop DLLs:

```text
D:\solid26\SOLIDWORKS\api\redist
```

