Option Strict Off
Option Explicit On
Imports System
Imports System.Data
Imports System.Diagnostics
Imports System.Windows.Forms
Imports System.Xml
Module Globals

	Public gbModalErrors As Boolean
	Public gbLogDebug As Boolean

	Sub MainDebug(ByVal DebugMessage As String, Optional ByVal DebugSource As String = "")

		If gbLogDebug Then
			Debug.WriteLine(DebugSource & ": " & DebugMessage)
		End If

	End Sub

	Function MainErrHandler(ByVal ErrorCode As Integer, ByVal ErrorDescr As String, ByVal ErrorSource As String, Optional ByRef ErrorLocation As String = "") As String

		Dim sError As String = "Error From PauloETL Library In " & ErrorSource & Environment.NewLine &  _
		                       "Error Code: " & CStr(ErrorCode) & " - " & ErrorDescr
		If ErrorLocation <> "" Then
			sError = sError & Environment.NewLine & "Error Location in Configuration:" & Environment.NewLine & ErrorLocation
		End If
		'If Modal Errors Set then Display Message Box
		If gbModalErrors Then
			MessageBox.Show(sError, "ETL Error in " & ErrorSource, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
		End If
		Return sError

	End Function

	Function GetADOParamDir(ByVal sParamDir As String) As ParameterDirection
		Dim vReturn As ParameterDirection

		Select Case sParamDir
			Case "adparaminput"
				vReturn = ParameterDirection.Input
			Case "adparaminputoutput"
				vReturn = ParameterDirection.InputOutput
			Case "adparamoutput"
				vReturn = ParameterDirection.Output
			Case "adparamreturnvalue"
				vReturn = ParameterDirection.ReturnValue
                'Case Else
                'UPGRADE_ISSUE: (2070) Constant adParamUnknown was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2070.aspx
                'vReturn = adParamUnknown
        End Select
		Return vReturn

	End Function


	Function GetADODataType(ByVal sDataType As String) As DbType
		Dim vReturn As DbType

		Select Case sDataType
			Case "adchar", "advarchar"
				vReturn = DbType.String
			Case "adboolean"
				vReturn = DbType.Boolean
			Case "addate"
				vReturn = DbType.Date
			Case "adinteger"
				vReturn = DbType.Int32
			Case "adsingle"
				vReturn = DbType.Single
			Case "addouble"
				vReturn = DbType.Double
                'Case Else
                'UPGRADE_ISSUE: (2070) Constant adEmpty was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2070.aspx
                'vReturn = adEmpty
        End Select
		Return vReturn

	End Function

	Function GetXmlCData(ByRef oNode As XmlNode) As String
		Dim sData As String = ""
		Dim oChild As XmlNode

		If oNode.HasChildNodes Then
			For I As Integer = 1 To oNode.ChildNodes.Count
				oChild = oNode.ChildNodes.Item(I)
				If oChild.NodeType = XmlNodeType.CDATA Then
					sData = oChild.InnerText
					Exit For
				End If
			Next I
		End If
		Return sData

	End Function
End Module