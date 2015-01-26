Option Strict Off
Option Explicit On
Imports Microsoft.VisualBasic
Imports System
Imports System.Data
Imports System.Data.Common
Imports System.Diagnostics
Imports System.Windows.Forms
Imports System.Xml
Imports UpgradeHelpers.DB.ADO
Imports UpgradeHelpers.Helpers
'UPGRADE_NOTE: (1043) Class instancing was changed to public. More Information: http://www.vbtonet.com/ewis/ewi1043.aspx
Public Class ETLCommand
	'Constant for Module Name Used In Error Functions
	Const cModule As String = "ETLCommand."

	Private moCommandNode As XmlElement
	Private moCmd As DbCommand
	Private moRs As ADORecordSetHelper
	Private moForEach As ETLCommands
	Private moParentCommand As ETLCommand
	Private moETLConnection As ETLConnection
	Private mvParamSources() As String
	Private moDebugForm As frmETLCommand

	Private msCmdName As String = ""
	Private msConnID As String = ""
	Private mbRowSet As Boolean
	Private mbBeginTran As Boolean
	Private mbEnabled As Boolean
	Private msLocation As String = ""
	Private mbUseDebugForm As Boolean
	Private mbDebugFormLoaded As Boolean
	Private mbHasParameters As Boolean

	Friend Function Execute(ByRef oETLControl As ETLControl, ByRef ErrorMessage As String) As Boolean
		On Error GoTo LocalErrHandler
		Const cProcedure As String = "Execute()"
		Dim bReturn As Boolean
		Dim sSource As String = ""
		Dim oSrcCommand As ETLCommand

		If mbEnabled Then
			bReturn = False
		Else
			bReturn = True
			GoTo LocalExit
		End If
		Debug.WriteLine("Executing " & msCmdName & moCmd.CommandText)
		If (Not (moParentCommand Is Nothing)) And (mbHasParameters) Then
			For I As Integer = 0 To moCmd.Parameters.Count - 1
				sSource = mvParamSources(I)
				'Set Default Source Command Pointer to Parent Command
				oSrcCommand = moParentCommand
				'If Prefix of Source is . Then Move to Source's Parent Recursively
				Do While sSource.StartsWith(".")
					oSrcCommand = moParentCommand.ParentCommand
					sSource = sSource.Substring(Math.Max(1, 0))
				Loop 
				If sSource.StartsWith("@") Then
					moCmd.Parameters(I).Value = oSrcCommand.Cmd.Parameters(sSource.Substring(Math.Max(1, 0))).Value
				Else
					moCmd.Parameters(I).Value = oSrcCommand.Rs(sSource)
				End If
			Next I
		End If
		If mbUseDebugForm Then
			moDebugForm.Show()
			mbDebugFormLoaded = True
		End If
		'  Debug.Print moETLConnection.Cn.State
		If mbRowSet Then
			'    Set moRs = moCmd.Execute
			'    Set moRs.ActiveConnection = moETLConnection.Cn
			'    Set moRs.ActiveCommand = moCmd
			'    moRs.CursorLocation = adUseClient
			'    moRs.Open moCmd, , adOpenStatic, adLockReadOnly
			'    Set moRs.ActiveConnection = Nothing
			'    Set moRs.ActiveCommand = Nothing
			moRs = New ADORecordSetHelper("")
			Rs.CursorLocation = CursorLocationEnum.adUseClient
			moRs.Open(moCmd)
			If (moRs.RecordCount <> 0) And (moForEach.Count > 0) Then
				Debug.WriteLine("Rows: " & moRs.RecordCount)
                'If mbUseDebugForm Then
                'moDebugForm.DataGrid1.DataSource = moRs.Tables(0)
                'UPGRADE_ISSUE: (2064) ADODB.Recordset property moRs.Fields was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
                'For Each fld As ADOFieldHelper In moRs.Fields
                ''UPGRADE_ISSUE: (2064) ADODB.Field property fld.ActualSize was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2064.aspx
                'Debug.WriteLine(VB6.TabLayout(fld.FieldMetadata.ColumnName, CStr(ADORecordSetHelper.GetDBType(fld.FieldMetadata.DataType)), CStr(fld.ActualSize)))
                '   Next fld/*
                If mbUseDebugForm Then
                    moDebugForm.DataGrid1.DataSource = moRs.Tables(0)
                    For Each fld As DataColumn In moRs.Tables(0).Columns
                        'Note that the MaxLenght property is different than the ActualSize property.
                        Debug.WriteLine(VB6.TabLayout(fld.ColumnName, CStr(ADORecordSetHelper.GetDBType(fld.DataType)), CStr(fld.MaxLength)))
                    Next fld
                End If

                moRs.MoveFirst()
                Do While Not moRs.EOF
                    If mbDebugFormLoaded Then
                        moDebugForm.Show()
                        'frmSingleStep.Show vbModal, moDebugForm
                    End If
                    For Each oChildCommand As ETLCommand In moForEach
                        bReturn = oChildCommand.Execute(oETLControl, ErrorMessage)
                        If Not bReturn Then
                            GoTo LocalExit
                        End If
                    Next oChildCommand
                    moRs.MoveNext()
                Loop
            End If
            moRs.Close()
            moRs = Nothing
        Else
            moCmd.ExecuteNonQuery()
        End If
        bReturn = True

LocalExit:
        MainDebug("Function Exit: " & bReturn, cModule & cProcedure)
        Return bReturn

LocalErrHandler:
        ErrorMessage = MainErrHandler(Information.Err().Number, Information.Err().Description, cModule & cProcedure, msLocation)
        Resume LocalExit

	End Function

	Friend Function LoadFromXML(ByRef oNode As XmlElement, ByRef oETLControl As ETLControl, ByVal ParentLocation As String, ByRef ErrorMessage As String, Optional ByRef ParentCommand As ETLCommand = Nothing) As Boolean
		On Error GoTo LocalErrHandler
		Const cProcedure As String = "LoadFromXML()"
		'UPGRADE_ISSUE: (2068) ADODB.ADO_LONGPTR object was not upgraded. More Information: http://www.vbtonet.com/ewis/ewi2068.aspx
        Dim vParamSize As Long
		Dim bReturn As Boolean
		Dim sParamType, sParamName, sParamDirection As String
		Dim oParamNodes As XmlNodeList
		Dim oParamNode As XmlElement
		Dim oParam As DbParameter
		Dim oForEachNode As XmlElement

		bReturn = False
		msCmdName = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("name"))
		msLocation = ParentLocation & Environment.NewLine & "Command:  " & msCmdName
		mbEnabled = ReflectionHelper.GetPrimitiveValue(Of Boolean)(oNode.GetAttribute("enabled"))
		If Not mbEnabled Then
			bReturn = True
			GoTo LocalExit
		End If
		Debug.WriteLine("Loading Command From XML: " & msCmdName)
		mbRowSet = ReflectionHelper.GetPrimitiveValue(Of Boolean)(oNode.GetAttribute("rowset"))
		mbBeginTran = ReflectionHelper.GetPrimitiveValue(Of Boolean)(oNode.GetAttribute("begintran"))
		msConnID = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("connid"))
		moCmd.CommandText = GetXmlCData(ReflectionHelper.GetPrimitiveValue(Of XmlNode)(oNode))
		If moCmd.CommandText = "" Then
			ErrorMessage = MainErrHandler(0, "Command Element Does Not Have SQL Statement in CDATA Section", cModule & cProcedure, msLocation)
			GoTo LocalExit
		End If
		moETLConnection = oETLControl.ETLConnections(msConnID)
		If Not moETLConnection.IsOpen Then
			If Not moETLConnection.OpenConnection(ErrorMessage) Then
				GoTo LocalExit
			End If
		End If
		moCmd.Connection = moETLConnection.Cn
		If Not (ParentCommand Is Nothing) Then
			moParentCommand = ParentCommand
		End If
		oParamNodes = oNode.SelectNodes("params/param")
		If oParamNodes.Count > 0 Then
			ReDim mvParamSources(oParamNodes.Count - 1)
			mbHasParameters = True
			For I As Integer = 1 To oParamNodes.Count
				'UPGRADE_WARNING: (2065) MSXML2.IXMLDOMNodeList method oParamNodes.nextNode has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2065.aspx
				oParamNode = oParamNodes.GetEnumerator().Current
				sParamName = ReflectionHelper.GetPrimitiveValue(Of String)(oParamNode.GetAttribute("name"))
				sParamType = ReflectionHelper.GetPrimitiveValue(Of String)(oParamNode.GetAttribute("type"))
				sParamDirection = ReflectionHelper.GetPrimitiveValue(Of String)(oParamNode.GetAttribute("direction"))
				mvParamSources(I - 1) = ReflectionHelper.GetPrimitiveValue(Of String)(oParamNode.GetAttribute("source"))
				Dim TempParameter As DbParameter
				TempParameter = moCmd.CreateParameter()
				TempParameter.ParameterName = sParamName
				TempParameter.DbType = GetADODataType(sParamType)
				TempParameter.Direction = GetADOParamDir(sParamDirection)
				oParam = TempParameter
				vParamSize = ReflectionHelper.GetPrimitiveValue(Of Integer)(oParamNode.GetAttribute("size"))
				'UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx
				If Not Convert.IsDBNull(vParamSize) Then
					oParam.Size = vParamSize
				End If
				moCmd.Parameters.Add(oParam)
				oParam = Nothing
			Next I
		End If
		oParamNodes = Nothing
		If oETLControl.ShowDebugForm Then
			mbUseDebugForm = True
			moDebugForm = frmETLCommand.CreateInstance()
			moDebugForm.Text = msCmdName
		End If
		oForEachNode = oNode.SelectSingleNode("foreach")
		If moForEach.LoadFromXML(oForEachNode, oETLControl, msLocation, ErrorMessage, Me) >= 0 Then
			bReturn = True
		End If
		oForEachNode = Nothing

LocalExit:
		MainDebug("Function Exit: " & bReturn, cModule & cProcedure)
		Return bReturn

LocalErrHandler:
		ErrorMessage = MainErrHandler(Information.Err().Number, Information.Err().Description, cModule & cProcedure, msLocation)
		Resume LocalExit

	End Function

	Public ReadOnly Property ParentCommand() As ETLCommand
		Get
			Return moParentCommand
		End Get
	End Property

	Public ReadOnly Property CommandNode() As XmlElement
		Get
			Return moCommandNode
		End Get
	End Property

	Public ReadOnly Property Cmd() As DbCommand
		Get
			Return moCmd
		End Get
	End Property

	Public ReadOnly Property Rs() As ADORecordSetHelper
		Get
			Return moRs
		End Get
	End Property

	Public ReadOnly Property ForEach() As ETLCommands
		Get
			Return moForEach
		End Get
	End Property

	Public ReadOnly Property CommandText() As String
		Get
			Return moCmd.CommandText
		End Get
	End Property

	Public ReadOnly Property BeginTran() As Boolean
		Get
			Return mbBeginTran
		End Get
	End Property

	Public ReadOnly Property Rowset() As Boolean
		Get
			Return mbRowSet
		End Get
	End Property

	Public ReadOnly Property CmdName() As String
		Get
			Return msCmdName
		End Get
	End Property

	Friend Sub New()
		MyBase.New()
		moCmd = UpgradeHelpers.DB.AdoFactoryManager.GetFactory().CreateCommand()
		moForEach = New ETLCommands()
	End Sub

	Protected Overrides Sub Finalize()
		moCommandNode = Nothing
		moRs = Nothing
		moCmd = Nothing
		moForEach = Nothing
		moParentCommand = Nothing
		moETLConnection = Nothing
		If mbDebugFormLoaded Then
			moDebugForm.Close()
		End If
		If mbUseDebugForm Then
			moDebugForm = Nothing
		End If
	End Sub
End Class