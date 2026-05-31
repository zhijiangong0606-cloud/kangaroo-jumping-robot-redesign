On Error Resume Next
Set sw = CreateObject("SldWorks.Application")
If Err.Number <> 0 Then
  WScript.Echo "ERR CreateObject " & Err.Description
  WScript.Quit 1
End If
Err.Clear
sw.Visible = True
If Err.Number <> 0 Then WScript.Echo "ERR Visible " & Err.Description Else WScript.Echo "Visible OK"
Err.Clear
WScript.Echo "Object OK"
WScript.Echo "Revision: " & sw.RevisionNumber
If Err.Number <> 0 Then WScript.Echo "ERR Revision " & Err.Description
