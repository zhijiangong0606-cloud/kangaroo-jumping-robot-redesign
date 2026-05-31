Option Explicit

Dim swApp, fso, folder, partFolder, outAsm, asmTemplate, asmDoc
Dim MM
MM = 0.001

Set fso = CreateObject("Scripting.FileSystemObject")
folder = fso.GetParentFolderName(WScript.ScriptFullName)
partFolder = folder & "\native_parts"
outAsm = folder & "\KangarooRobot_basic_layout.SLDASM"
asmTemplate = "C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_assembly.asmdot"

Set swApp = CreateObject("SldWorks.Application")
swApp.Visible = True
Set asmDoc = swApp.NewDocument(asmTemplate, 0, 0, 0)
If asmDoc Is Nothing Then
    WScript.Echo "Failed to create assembly."
    WScript.Quit 1
End If

Sub AddPart(fileName, xMM, yMM, zMM)
    Dim compPath, comp, opened, errs, warns
    compPath = partFolder & "\" & fileName
    If Not fso.FileExists(compPath) Then
        WScript.Echo "Missing part: " & compPath
        Exit Sub
    End If
    Set comp = asmDoc.AddComponent(compPath, xMM*MM, yMM*MM, zMM*MM)
    If comp Is Nothing Then
        WScript.Echo "Failed add: " & fileName
    Else
        WScript.Echo "Added: " & fileName
    End If
End Sub

' Basic layout positions. This assembly is a visual layout, not a fully mated mechanism yet.
AddPart "BodySidePlate_verified.SLDPRT", 0, -8, 0
AddPart "BodySidePlate_verified.SLDPRT", 0, 8, 0

AddPart "L1_Crank_40mm.SLDPRT", -80, -18, 60
AddPart "L1_Crank_40mm.SLDPRT", -80, 18, 60
AddPart "L2_Coupler_120mm.SLDPRT", -43, -18, 46
AddPart "L2_Coupler_120mm.SLDPRT", -43, 18, 46
AddPart "L3_ThighRocker_100mm.SLDPRT", 0, -18, 0
AddPart "L3_ThighRocker_100mm.SLDPRT", 0, 18, 0
AddPart "L4_Shank_140mm.SLDPRT", 45, -18, 80
AddPart "L4_Shank_140mm.SLDPRT", 45, 18, 80
AddPart "L5_RearRocker_180mm.SLDPRT", 120, -18, 60
AddPart "L5_RearRocker_180mm.SLDPRT", 120, 18, 60
AddPart "FootPad_85mm.SLDPRT", -58, -18, 231
AddPart "FootPad_85mm.SLDPRT", -58, 18, 231
AddPart "TailRod_210mm.SLDPRT", 200, 0, -20

AddPart "WindingDrum_r18_w24.SLDPRT", -55, 0, 80
AddPart "GearMotor_Placeholder_60x32x26.SLDPRT", -25, 0, 90
AddPart "Servo_Placeholder_38x35x22.SLDPRT", 135, 0, 95
AddPart "Latch_Placeholder_50x12x8.SLDPRT", 80, 0, 72

asmDoc.ForceRebuild3 False
Dim errCode
errCode = asmDoc.SaveAs3(outAsm, 0, 2)
WScript.Echo "Assembly SaveAs3 errorCode=" & errCode
WScript.Echo "Saved assembly: " & outAsm
