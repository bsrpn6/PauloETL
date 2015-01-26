Attribute VB_Name = "MainModule"
Option Explicit

Sub GetArgument(ByVal sCommand As String, ByVal ArgName As String, ArgVal As String)
Dim lStartPos As Long, lEndPos As Long

  lStartPos = InStr(1, sCommand, ArgName, vbTextCompare)
  If lStartPos > 0 Then
    lStartPos = lStartPos + Len(ArgName)
    lEndPos = InStr(lStartPos, sCommand, " ", vbTextCompare)
    If lEndPos = 0 Then
      lEndPos = Len(sCommand) + 1
    End If
    If lEndPos > lStartPos Then
      ArgVal = Mid(sCommand, lStartPos, lEndPos - lStartPos)
    End If
  End If
  
End Sub

Sub Main()
Dim sCommand As String, sJobID As String, sXMLFile As String, sError As String
Dim oETLControl As ETLControl

  sCommand = Command()
  GetArgument sCommand, "-Job:", sJobID
  GetArgument sCommand, "-XML:", sXMLFile
  If sJobID = "" Or sXMLFile = "" Then
    Err.Raise vbObjectError + 1, "PauloETLExecute", "Must Pass -Job: and -XML: parameters"
  Else
    Set oETLControl = New ETLControl
    If oETLControl.ExecuteJob(sJobID, sXMLFile, sError) <= 0 Then
      Err.Raise vbObjectError + 2, "PauloETLExecute", sError
    End If
    Set oETLControl = Nothing
  End If
  'Terminate Program
  End

End Sub
