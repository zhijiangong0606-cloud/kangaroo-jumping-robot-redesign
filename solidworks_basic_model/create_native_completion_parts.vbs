Option Explicit

Dim swApp, fso, folder, outFolder, templatePath
Dim MM
MM = 0.001

Set fso = CreateObject("Scripting.FileSystemObject")
folder = fso.GetParentFolderName(WScript.ScriptFullName)
outFolder = folder & "\native_parts"
If Not fso.FolderExists(outFolder) Then fso.CreateFolder(outFolder)

Set swApp = CreateObject("SldWorks.Application")
swApp.Visible = True
templatePath = "C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_part.prtdot"

Sub SelectFrontPlane(model)
    model.Extension.SelectByID2 "Front Plane", "PLANE", 0, 0, 0, False, 0, Nothing, 0
    If model.SelectionManager.GetSelectedObjectCount2(-1) = 0 Then model.Extension.SelectByID2 "Plane1", "PLANE", 0, 0, 0, False, 0, Nothing, 0
End Sub

Function NewPart(title)
    Dim model
    Set model = swApp.NewDocument(templatePath, 0, 0, 0)
    model.SetTitle2 title
    Set NewPart = model
End Function

Sub SaveAndClose(model, fileName)
    Dim errCode
    model.ForceRebuild3 False
    errCode = model.SaveAs3(outFolder & "\" & fileName, 0, 2)
    WScript.Echo fileName & " SaveAs3 errorCode=" & errCode
    swApp.CloseDoc model.GetTitle
End Sub

Sub ExtrudeSketch(model, depthMM)
    model.FeatureManager.FeatureExtrusion2 True, False, False, 0, 0, depthMM * MM, 0, False, False, False, False, 0, 0, False, False, False, False, True, True, True, 0, 0, False
End Sub

Sub CreateCylinder(partName, radiusMM, depthMM)
    Dim model
    Set model = NewPart(partName)
    SelectFrontPlane model
    model.SketchManager.InsertSketch True
    model.SketchManager.CreateCircleByRadius 0, 0, 0, radiusMM*MM
    model.SketchManager.InsertSketch True
    ExtrudeSketch model, depthMM
    SaveAndClose model, partName & ".SLDPRT"
End Sub

Sub CreateCapsule(partName, lengthMM, widthMM, thickMM)
    Dim model, r
    Set model = NewPart(partName)
    r = widthMM / 2
    SelectFrontPlane model
    model.SketchManager.InsertSketch True
    model.SketchManager.CreateCircleByRadius 0, 0, 0, r*MM
    model.SketchManager.CreateCircleByRadius lengthMM*MM, 0, 0, r*MM
    model.SketchManager.CreateLine 0, r*MM, 0, lengthMM*MM, r*MM, 0
    model.SketchManager.CreateLine 0, -r*MM, 0, lengthMM*MM, -r*MM, 0
    model.SketchManager.InsertSketch True
    ExtrudeSketch model, thickMM
    SaveAndClose model, partName & ".SLDPRT"
End Sub

CreateCylinder "M3_Axle_40mm", 2, 40
CreateCylinder "Spacer_16mm", 4, 16
CreateCylinder "TailMass_30g_placeholder", 14, 18
CreateCapsule "ElasticTendon_120mm", 120, 6, 4

WScript.Echo "Completion parts finished."
