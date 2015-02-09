Option Strict Off
Option Explicit On
Imports Microsoft.VisualBasic
Imports System
Imports System.Data.Common
Imports System.Diagnostics
Imports System.Xml
Imports UpgradeHelpers.Helpers
Imports Newtonsoft.Json
Imports Oracle.ManagedDataAccess.Client

Public Class ETLCommand
    'Constant for Module Name Used In Error Functions
    Const cModule As String = "ETLCommand."
    Private dataReader As DbDataReader
    Private moCommandNode As XmlElement
    Private moCmd As DbCommand
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

        ' If moParentCommand IsNot Nothing Then
        'Dim reader As DbDataReader = moParentCommand.dataReader
        'Dim val = reader.GetValue(0)
        'End If

        'Dim json As String = JsonConvert.SerializeObject(Me, s)

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
                    If moCmd.Parameters(I).Direction = ParameterDirection.Input Then
                        Dim col = oSrcCommand.dataReader.GetOrdinal(sSource)
                        Dim val = oSrcCommand.dataReader.GetValue(col)

                        moCmd.Parameters(I).Value = val
                    End If
                End If


                If moCmd.Parameters(I).Value IsNot Nothing Then
                    sParams += I.ToString() + " " + sSource + "=" + moCmd.Parameters(I).Value.ToString() + ", "
                End If


            Next I

            Debug.WriteLine("The Params are:")
            Debug.WriteLine(sParams)

        End If

        If mbRowSet Then

            Debug.WriteLine("Calling moCmd START " + moCmd.CommandText)
            dataReader = moCmd.ExecuteReader()

            If (dataReader.HasRows) And (moForEachDoCommand.Count > 0) Then
                Dim i As Int16 = 1
                Dim saveSrcCommand As ETLCommand

                Do While dataReader.Read()
                    Debug.WriteLine("moRs i " + i.ToString())
                    i += 1

                    Dim j As Int16 = 1
                    Dim oChildCommand As ETLCommand
                    Dim cmd As DbCommand
                    Dim e As IDictionaryEnumerator

                    e = moForEachDoCommand.GetEnumerator

                    ' For Each oChildCommandEntry As OrderedDictionary In moForEachDoCommand.GetEnumerator()
                    While e.MoveNext()
                        Debug.WriteLine("moForEachDoCommand j " + j.ToString())
                        j += 1
                        oChildCommand = e.Value
                        ' If saveSrcCommand Is Nothing Then
                        '    saveSrcCommand = oChildCommand.moParentCommand
                        'End If

                        'oChildCommand.moParentCommand = saveSrcCommand
                        oChildCommand.moParentCommand = Me

                        Debug.WriteLine("oChildComand START CALL EXEC " + oChildCommand.CmdName)
                        bReturn = oChildCommand.Execute(oETLControl, ErrorMessage)
                        Debug.WriteLine("oChildCommand END CALL EXEC " + oChildCommand.CmdName)
                        If Not bReturn Then
                            GoTo LocalExit
                        End If
                        'Next oChildCommandEntry
                    End While
                Loop
            End If

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

        If msConnID = "ORACLE" Then

            moCmd = New OracleCommand()

        Else

            moCmd = UpgradeHelpers.DB.AdoFactoryManager.GetFactory().CreateCommand()
        End If


        moCmd.CommandText = GetXmlCData(oNode)

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
            If msConnID = "ORACLE" Then
                moCmd.CommandType = CommandType.StoredProcedure
            End If

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

                If msConnID = "ORACLE" And sParamType = "adcursor" Then
                    Dim oracleParam As OracleParameter
                    oracleParam = TempParameter
                    oracleParam.OracleDbType = OracleDbType.RefCursor
                Else
                    TempParameter.DbType = GetADODataType(sParamType)
                End If
                TempParameter.Direction = GetADOParamDir(sParamDirection)
                oParam = TempParameter
                'INVESTIGATE

                vParamSize = GetAttributeHelper(oParamNode)

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

    Public ReadOnly Property Reader() As DbDataReader
        Get
            Return dataReader
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
        moForEachDoCommand = New ETLCommands()
    End Sub

    Protected Overrides Sub Finalize()
        moCommandNode = Nothing
        dataReader = Nothing
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