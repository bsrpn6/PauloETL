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
Imports System.Web.Script.Serialization
Imports Newtonsoft.Json
Imports System.Collections.Specialized

Public Class ETLCommand
	'Constant for Module Name Used In Error Functions
	Const cModule As String = "ETLCommand."

	Private moCommandNode As XmlElement
	Private moCmd As DbCommand
	Private moRs As ADORecordSetHelper
    Private moForEachDoCommand As ETLCommands
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

        Dim s As Newtonsoft.Json.JsonSerializerSettings = New Newtonsoft.Json.JsonSerializerSettings
        s.ReferenceLoopHandling = ReferenceLoopHandling.Ignore

        Dim json As String = JsonConvert.SerializeObject(Me, s)

        'Debug.WriteLine(json)

        Dim sParams As String = ""

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

                sParams += I.ToString() + " " + sSource + "=" + moCmd.Parameters(I).Value.ToString() + ", "

            Next I

            Debug.WriteLine("The Params are:")
            Debug.WriteLine(sParams)

        End If

        If mbRowSet Then
            moRs = New ADORecordSetHelper("")
            Rs.CursorLocation = CursorLocationEnum.adUseClient
            'UNCOMMENT - CONNECTION
            Debug.WriteLine("Calling moCmd START " + moCmd.CommandText)
            moRs.Open(moCmd)
            Debug.WriteLine("Calling moCmd END CHECK ROW COUNT = " + moRs.RecordCount.ToString())

            If (moRs.RecordCount <> 0) And (moForEachDoCommand.Count > 0) Then
                Debug.WriteLine("Rows: " & moRs.RecordCount)

                moRs.MoveFirst()
                Dim i As Int16 = 1


                Dim saveSrcCommand As ETLCommand



                Do While Not moRs.EOF
                    Debug.WriteLine("moRs i " + i.ToString())
                    i += 1
                    Dim j As Int16 = 1
                    Dim oChildCommand As ETLCommand
                    Dim cmd As DbCommand

                    'cmd = 
                    'If (Not (moParentCommand Is Nothing)) And (mbHasParameters) Then
                    'For I As Integer = 0 To moCmd.Parameters.Count - 1
                    'sSource = mvParamSources(i)
                    'Set Default Source Command Pointer to Parent Command
                    'oSrcCommand = moParentCommand

                    ' For Each oChildCommandEntry As OrderedDictionary In moForEachDoCommand.GetEnumerator()
                    Dim e As IDictionaryEnumerator
                    e = moForEachDoCommand.GetEnumerator
                    While e.MoveNext()
                        Debug.WriteLine("moForEachDoCommand j " + j.ToString())
                        j += 1
                        oChildCommand = e.Value
                        If saveSrcCommand Is Nothing Then
                            saveSrcCommand = oChildCommand.moParentCommand
                        End If

                        oChildCommand.moParentCommand = saveSrcCommand

                        'Debug.WriteLine(e.Key)
                        'oChildCommand.moCmd = moCmd
                        'oChildCommand.moParentCommand = Me
                        Debug.WriteLine("oChildComand START CALL EXEC " + oChildCommand.CmdName)
                        bReturn = oChildCommand.Execute(oETLControl, ErrorMessage)
                        Debug.WriteLine("oChildCommand END CALL EXEC " + oChildCommand.CmdName)
                        If Not bReturn Then
                            GoTo LocalExit
                        End If
                        'Next oChildCommandEntry
                    End While
                    moRs.MoveNext()
                Loop
            End If
            moRs.Close()
            moRs = Nothing
        Else
            'UNCOMMENT - CONNECTION
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

    Friend Function Execute_ORIGINAL(ByRef oETLControl As ETLControl, ByRef ErrorMessage As String) As Boolean
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

        Dim sParams As String = ""

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

                sParams += I.ToString() + " " + sSource + "=" + moCmd.Parameters(I).Value.ToString() + ", "

            Next I

            Debug.WriteLine("The Params are:")
            Debug.WriteLine(sParams)

        End If



        If mbUseDebugForm Then
            moDebugForm.Show()
            mbDebugFormLoaded = True
        End If
        'REMOVE - DEBUGGING THAT DOESN'T WORK
        '  Debug.Print moETLConnection.Cn.State
        If mbRowSet Then
            moRs = New ADORecordSetHelper("")
            Rs.CursorLocation = CursorLocationEnum.adUseClient
            'UNCOMMENT - CONNECTION
            Debug.WriteLine("Calling moCmd START " + moCmd.CommandText)
            moRs.Open(moCmd)
            Debug.WriteLine("Calling moCmd END CHECK ROW COUNT = " + moRs.RecordCount.ToString())
            If (moRs.RecordCount <> 0) And (moForEachDoCommand.Count > 0) Then
                Debug.WriteLine("Rows: " & moRs.RecordCount)

                'REMOVE - DEGBUGGING THAT DOESN'T WORK
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
                    End If

                    'Dim oChildCommand As ETLCommand
                    'For I As Integer = 1 To moForEach.Count
                    '    oChildCommand = moForEach.Item(I)
                    '    bReturn = oChildCommand.Execute(oETLControl, ErrorMessage)
                    '    If Not bReturn Then
                    '        GoTo LocalExit
                    '    End If
                    'Next


                    '   For Each oChildCommand As ETLCommand In moForEach



                    For Each oChildCommandEntry As Object In moForEachDoCommand

                        Dim oChildCommand As ETLCommand = oChildCommandEntry.Value

                        bReturn = oChildCommand.Execute(oETLControl, ErrorMessage)
                        If Not bReturn Then
                            GoTo LocalExit
                        End If
                    Next oChildCommandEntry
                    moRs.MoveNext()
                Loop
            End If
            moRs.Close()
            moRs = Nothing
        Else
            'UNCOMMENT - CONNECTION
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
        'INVESTIGATE
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
        'REMOVE - CHANGED
        'moCmd.CommandText = GetXmlCData(ReflectionHelper.GetPrimitiveValue(Of XmlNode)(oNode))
        moCmd.CommandText = GetXmlCData(oNode)
        If moCmd.CommandText = "" Then
            ErrorMessage = MainErrHandler(0, "Command Element Does Not Have SQL Statement in CDATA Section", cModule & cProcedure, msLocation)
            GoTo LocalExit
        End If
        moETLConnection = oETLControl.ETLConnections(msConnID)
        'UNCOMMENT - CONNECTION
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
                oParamNode = oParamNodes.Item(I - 1)
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
                'INVESTIGATE

                vParamSize = GetAttributeHelper(oParamNode)

                ''UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx

                '  If Not Convert.IsDBNull(vParamSize) Then
                ' oParam.Size = vParamSize
                ' End If

                If vParamSize > 0 Then
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
        If moForEachDoCommand.LoadFromXML(oForEachNode, oETLControl, msLocation, ErrorMessage, Me) >= 0 Then
            bReturn = True
        End If
        oForEachNode = Nothing

LocalExit:
        ' Dim json As String = JsonConvert.SerializeObject(Me)
        ' Debug.WriteLine(json)
        MainDebug("Function Exit: " & bReturn, cModule & cProcedure)
        Return bReturn

LocalErrHandler:
        ErrorMessage = MainErrHandler(Information.Err().Number, Information.Err().Description, cModule & cProcedure, msLocation)
        Resume LocalExit

    End Function

    Function GetAttributeHelper(ByVal oParamNode As XmlElement) As Long
        Dim value As Long
        Try
            value = ReflectionHelper.GetPrimitiveValue(Of Integer)(oParamNode.GetAttribute("size"))
        Catch ex As Exception
            'ignore for now
        End Try

        Return value
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
            Return moForEachDoCommand
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
        moForEachDoCommand = New ETLCommands()
    End Sub

    Protected Overrides Sub Finalize()
        moCommandNode = Nothing
        moRs = Nothing
        moCmd = Nothing
        moForEachDoCommand = Nothing
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