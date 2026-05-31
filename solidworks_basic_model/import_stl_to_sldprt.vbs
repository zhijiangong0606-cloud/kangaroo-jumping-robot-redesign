Option Explicit

Dim swApp, fso, folder, outFolder, partTemplate
Dim file, doc, ok, errors, warnings, baseName, outPath

Set fso = CreateObject("Scripting.FileSystemObject")
folder = fso.GetParentFolderName(WScript.ScriptFullName)
outFolder = folder & "\sldprt_imported"
If Not fso.FolderExists(outFolder) Then fso.CreateFolder(outFolder)

Set swApp = CreateObject("SldWorks.Application")
swApp.Visible = True

errors = 0
warnings = 0

For Each file In fso.GetFolder(folder).Files
    If LCase(fso.GetExtensionName(file.Name)) = "stl" Then
        WScript.Echo "Opening " & file.Name
        Set doc = swApp.OpenDoc6(file.Path, 1, 0, "", errors, warnings)
        If doc Is Nothing Then
            WScript.Echo "  Failed to open: " & file.Name & " errors=" & errors
        Else
            baseName = fso.GetBaseName(file.Name)
            outPath = outFolder & "\" & baseName & ".SLDPRT"
            ok = doc.SaveAs3(outPath, 0, 2)
            If ok Then
                WScript.Echo "  Saved " & outPath
            Else
                WScript.Echo "  Save failed: " & outPath
            End If
            swApp.CloseDoc doc.GetTitle
        End If
    End If
Next

WScript.Echo "Done. Output folder: " & outFolder

