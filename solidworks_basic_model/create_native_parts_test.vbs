Option Explicit

Dim swApp, fso, folder, outFolder, templatePath
Dim model, sk, feat, ok

Set fso = CreateObject("Scripting.FileSystemObject")
folder = fso.GetParentFolderName(WScript.ScriptFullName)
outFolder = folder & "\native_parts"
If Not fso.FolderExists(outFolder) Then fso.CreateFolder(outFolder)

Set swApp = CreateObject("SldWorks.Application")
swApp.Visible = True
templatePath = "C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_part.prtdot"

Set model = swApp.NewDocument(templatePath, 0, 0, 0)
If model Is Nothing Then
    WScript.Echo "Failed to create part from template."
    WScript.Quit 1
End If

' A small test plate. Dimensions are in meters in the SolidWorks API.
model.SketchManager.InsertSketch True
model.SketchManager.CreateCenterRectangle 0, 0, 0, 0.05, 0.02, 0
model.SketchManager.InsertSketch True

Set feat = model.FeatureManager.FeatureExtrusion2(True, False, False, 0, 0, 0.004, 0, False, False, False, False, 0, 0, False, False, False, False, True, True, True, 0, 0, False)
If feat Is Nothing Then
    WScript.Echo "Extrusion failed."
Else
    ok = model.SaveAs3(outFolder & "\api_test_plate.SLDPRT", 0, 2)
    WScript.Echo "Native API test save result: " & ok
End If

