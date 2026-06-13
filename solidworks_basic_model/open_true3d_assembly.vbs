Option Explicit

Dim swApp, model, asmPath, errors, warnings, title

asmPath = "C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\KangarooRobot_TRUE_3D_assembly.SLDASM"

Set swApp = CreateObject("SldWorks.Application")
swApp.Visible = True

errors = 0
warnings = 0
Set model = swApp.OpenDoc6(asmPath, 2, 0, "", errors, warnings)

If model Is Nothing Then
    WScript.Echo "Failed to open assembly. errors=" & errors & " warnings=" & warnings
    WScript.Quit 1
End If

title = model.GetTitle
swApp.ActivateDoc3 title, False, 0, errors
model.ShowNamedView2 "*Isometric", 7
model.ViewZoomtofit2
model.ForceRebuild3 False

WScript.Echo "Opened and activated assembly: " & asmPath
