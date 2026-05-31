Option Explicit
Dim swApp, fso, folder, outFolder, templatePath, model, feat, ok, errs, warns, ext
Set fso = CreateObject("Scripting.FileSystemObject")
folder = "C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model"
outFolder = folder & "\native_parts"
If Not fso.FolderExists(outFolder) Then fso.CreateFolder(outFolder)
Set swApp = CreateObject("SldWorks.Application")
swApp.Visible = True
templatePath = "C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_part.prtdot"
Set model = swApp.NewDocument(templatePath, 0, 0, 0)
If model Is Nothing Then WScript.Echo "no model": WScript.Quit 1
model.SketchManager.InsertSketch True
model.SketchManager.CreateCenterRectangle 0, 0, 0, 0.05, 0.02, 0
model.SketchManager.InsertSketch True
Set feat = model.FeatureManager.FeatureExtrusion2(True, False, False, 0, 0, 0.004, 0, False, False, False, False, 0, 0, False, False, False, False, True, True, True, 0, 0, False)
model.ForceRebuild3 False
errs = 0: warns = 0
Set ext = model.Extension
ok = ext.SaveAs(outFolder & "\api_test_plate_ext.SLDPRT", 0, 1, Nothing, errs, warns)
WScript.Echo "Ext.SaveAs ok=" & ok & " errors=" & errs & " warnings=" & warns
errs = 0: warns = 0
ok = model.SaveAs2(outFolder & "\api_test_plate_saveas2.SLDPRT", 0, False, True)
WScript.Echo "SaveAs2 ok=" & ok
