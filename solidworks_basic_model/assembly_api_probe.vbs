Option Explicit
Dim swApp, asmModel, asm, fso, folder, part, comp, err
Set fso = CreateObject("Scripting.FileSystemObject")
folder = "C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model"
Set swApp = CreateObject("SldWorks.Application")
swApp.Visible = True
Set asmModel = swApp.NewDocument("C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_assembly.asmdot", 0, 0, 0)
Set asm = asmModel
part = folder & "\native_parts\L1_Crank_40mm.SLDPRT"
On Error Resume Next
Err.Clear
Set comp = asm.AddComponent5(part, 0, "", False, "", 0, 0, 0)
WScript.Echo "AddComponent5 err=" & Err.Number & " desc=" & Err.Description & " compIsNothing=" & (comp Is Nothing)
Err.Clear
Set comp = asm.AddComponent4(part, "", 0, 0, 0)
WScript.Echo "AddComponent4 err=" & Err.Number & " desc=" & Err.Description & " compIsNothing=" & (comp Is Nothing)
Err.Clear
Set comp = asm.AddComponent(part, 0, 0, 0)
WScript.Echo "AddComponent err=" & Err.Number & " desc=" & Err.Description & " compIsNothing=" & (comp Is Nothing)
Err.Clear
Set comp = asmModel.AddComponent5(part, 0, "", False, "", 0, 0, 0)
WScript.Echo "Model AddComponent5 err=" & Err.Number & " desc=" & Err.Description & " compIsNothing=" & (comp Is Nothing)
asmModel.SaveAs3 folder & "\probe_assembly.SLDASM", 0, 2
