Option Strict Off
Option Explicit On
Imports Microsoft.VisualBasic
Imports System
Module MainModule

	Sub GetArgument(ByVal sCommand As String, ByVal ArgName As String, ByRef ArgVal As String)
		Dim lEndPos As Integer

		Dim lStartPos As Integer = (sCommand.IndexOf(ArgName, StringComparison.CurrentCultureIgnoreCase) + 1)
		If lStartPos > 0 Then
			lStartPos += ArgName.Length
			lEndPos = Strings.InStr(lStartPos, sCommand, " ", CompareMethod.Text)
			If lEndPos = 0 Then
				lEndPos = sCommand.Length + 1
			End If
			If lEndPos > lStartPos Then
				ArgVal = sCommand.Substring(lStartPos - 1, Math.Min(lEndPos - lStartPos, sCommand.Length - (lStartPos - 1)))
			End If
		End If

	End Sub

	<STAThread> _
	 Public Sub Main()
		Dim sXMLFile, sJobID, sError As String
		Dim oETLControl As PauloETL.ETLControl

		Dim sCommand As String = Interaction.Command()
		GetArgument(sCommand, "-Job:", sJobID)
		GetArgument(sCommand, "-XML:", sXMLFile)
		If sJobID = "" Or sXMLFile = "" Then
			Throw New System.Exception((Constants.vbObjectError + 1).ToString() + ", PauloETLExecute, Must Pass -Job: and -XML: parameters")
		Else
			oETLControl = New PauloETL.ETLControl()
			If oETLControl.ExecuteJob(sJobID, sXMLFile, sError) <= 0 Then
				Throw New System.Exception((Constants.vbObjectError + 2).ToString() + ", PauloETLExecute, " + sError)
			End If
			oETLControl = Nothing
		End If
		'Terminate Program
		Environment.Exit(0)

	End Sub
End Module