Option Explicit
Dim swApp, model
Set swApp = CreateObject("SldWorks.Application")
Set model = swApp.ActiveDoc
If model Is Nothing Then
    WScript.Echo "No active SolidWorks document."
    WScript.Quit 1
End If
model.ShowNamedView2 "*Isometric", 7
model.ViewZoomtofit2
model.ForceRebuild3 False
WScript.Echo "Set active document to isometric view and zoom to fit."
