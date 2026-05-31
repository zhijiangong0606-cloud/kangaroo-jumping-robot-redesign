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

Sub CreateBodyPlate()
    Dim model, sk, pts, i, j
    Set model = NewPart("BodySidePlate")
    SelectFrontPlane model
    model.SketchManager.InsertSketch True

    pts = Array(Array(-125,-45), Array(250,-45), Array(330,35), Array(270,120), Array(-110,130), Array(-165,50))
    For i = 0 To UBound(pts)
        j = i + 1
        If j > UBound(pts) Then j = 0
        model.SketchManager.CreateLine pts(i)(0)*MM, pts(i)(1)*MM, 0, pts(j)(0)*MM, pts(j)(1)*MM, 0
    Next

    ' M3 joint holes and module mounting placeholders.
    model.SketchManager.CreateCircleByRadius 0*MM, 0*MM, 0, 1.7*MM
    model.SketchManager.CreateCircleByRadius -80*MM, 60*MM, 0, 1.7*MM
    model.SketchManager.CreateCircleByRadius 120*MM, 60*MM, 0, 1.7*MM
    model.SketchManager.CreateCircleByRadius 200*MM, -20*MM, 0, 1.7*MM
    model.SketchManager.CreateCircleByRadius -25*MM, 90*MM, 0, 8*MM
    model.SketchManager.CreateCircleByRadius 135*MM, 95*MM, 0, 5*MM

    model.SketchManager.InsertSketch True
    ExtrudeSketch model, 4
    SaveAndClose model, "BodySidePlate_verified.SLDPRT"
End Sub

Sub CreateLinkPart(partName, lengthMM, widthMM, thickMM)
    Dim model, r
    Set model = NewPart(partName)
    r = widthMM / 2
    SelectFrontPlane model
    model.SketchManager.InsertSketch True
    ' Overlapping circles plus tangent lines form a capsule outline.
    model.SketchManager.CreateCircleByRadius 0, 0, 0, r*MM
    model.SketchManager.CreateCircleByRadius lengthMM*MM, 0, 0, r*MM
    model.SketchManager.CreateLine 0, r*MM, 0, lengthMM*MM, r*MM, 0
    model.SketchManager.CreateLine 0, -r*MM, 0, lengthMM*MM, -r*MM, 0
    model.SketchManager.CreateCircleByRadius 0, 0, 0, 1.7*MM
    model.SketchManager.CreateCircleByRadius lengthMM*MM, 0, 0, 1.7*MM
    model.SketchManager.InsertSketch True
    ExtrudeSketch model, thickMM
    SaveAndClose model, partName & ".SLDPRT"
End Sub

Sub CreateRectPart(partName, lxMM, lyMM, thickMM)
    Dim model
    Set model = NewPart(partName)
    SelectFrontPlane model
    model.SketchManager.InsertSketch True
    model.SketchManager.CreateCenterRectangle 0, 0, 0, lxMM/2*MM, lyMM/2*MM, 0
    model.SketchManager.InsertSketch True
    ExtrudeSketch model, thickMM
    SaveAndClose model, partName & ".SLDPRT"
End Sub

Sub CreateCylinderPart(partName, radiusMM, depthMM)
    Dim model
    Set model = NewPart(partName)
    SelectFrontPlane model
    model.SketchManager.InsertSketch True
    model.SketchManager.CreateCircleByRadius 0, 0, 0, radiusMM*MM
    model.SketchManager.CreateCircleByRadius 0, 0, 0, 3*MM
    model.SketchManager.InsertSketch True
    ExtrudeSketch model, depthMM
    SaveAndClose model, partName & ".SLDPRT"
End Sub

CreateBodyPlate
CreateLinkPart "L1_Crank_40mm", 40, 16, 3.5
CreateLinkPart "L2_Coupler_120mm", 120, 16, 3.5
CreateLinkPart "L3_ThighRocker_100mm", 100, 18, 3.5
CreateLinkPart "L4_Shank_140mm", 140, 18, 3.5
CreateLinkPart "L5_RearRocker_180mm", 180, 18, 3.5
CreateLinkPart "TailRod_210mm", 210, 12, 6
CreateLinkPart "FootPad_85mm", 85, 24, 6
CreateCylinderPart "WindingDrum_r18_w24", 18, 24
CreateRectPart "GearMotor_Placeholder_60x32x26", 60, 32, 26
CreateRectPart "Servo_Placeholder_38x35x22", 38, 35, 22
CreateRectPart "Latch_Placeholder_50x12x8", 50, 12, 8

WScript.Echo "Native SolidWorks basic parts finished: " & outFolder
