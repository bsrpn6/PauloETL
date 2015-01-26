VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "ETLCommand"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
Attribute VB_Ext_KEY = "SavedWithClassBuilder6" ,"Yes"
Attribute VB_Ext_KEY = "Top_Level" ,"No"
Option Explicit
'Constant for Module Name Used In Error Functions
Const cModule As String = "ETLCommand."

Private moCommandNode As MSXML2.IXMLDOMElement
Private moCmd As ADODB.Command
Private moRs As ADODB.Recordset
Private moForEach As ETLCommands
Private moParentCommand As ETLCommand
Private moETLConnection As ETLConnection
Private mvParamSources() As String
Private moDebugForm As frmETLCommand

Private msCmdName As String
Private msConnID As String
Private mbRowSet As Boolean
Private mbBeginTran As Boolean
Private mbEnabled As Boolean
Private msLocation As String
Private mbUseDebugForm As Boolean
Private mbDebugFormLoaded As Boolean
Private mbHasParameters As Boolean

Friend Function Execute(oETLControl As ETLControl, ErrorMessage As String) As Boolean
On Error GoTo LocalErrHandler
Const cProcedure As String = "Execute()"
Dim bReturn As Boolean, I As Long, sSource As String, oChildCommand As ETLCommand, oSrcCommand As ETLCommand

  If mbEnabled Then
    bReturn = False
  Else
    bReturn = True
    GoTo LocalExit
  End If
  Debug.Print "Executing " & msCmdName & moCmd.CommandText
  If (Not moParentCommand Is Nothing) And (mbHasParameters) Then
    For I = 0 To moCmd.Parameters.Count - 1
      sSource = mvParamSources(I)
      'Set Default Source Command Pointer to Parent Command
      Set oSrcCommand = moParentCommand
      'If Prefix of Source is . Then Move to Source's Parent Recursively
      Do While Left(sSource, 1) = "."
        Set oSrcCommand = moParentCommand.ParentCommand
        sSource = Right(sSource, Len(sSource) - 1)
      Loop
      If Left(sSource, 1) = "@" Then
        moCmd.Parameters(I).Value = oSrcCommand.Cmd.Parameters(Right(sSource, Len(sSource) - 1)).Value
      Else
        moCmd.Parameters(I).Value = oSrcCommand.Rs(sSource)
      End If
    Next I
  End If
  If mbUseDebugForm Then
    moDebugForm.Show
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
    Set moRs = New ADODB.Recordset
    Rs.CursorLocation = adUseClient
    moRs.Open moCmd, , adOpenStatic
    If (moRs.RecordCount <> 0) And (moForEach.Count > 0) Then
      Debug.Print "Rows: " & moRs.RecordCount
      If mbUseDebugForm Then
        Set moDebugForm.DataGrid1.DataSource = moRs
        Dim fld As ADODB.Field
        For Each fld In moRs.Fields
          Debug.Print fld.Name, fld.Type, fld.ActualSize
        Next fld
      End If
      moRs.MoveFirst
      Do While Not moRs.EOF
        If mbDebugFormLoaded Then
          moDebugForm.Show
          'frmSingleStep.Show vbModal, moDebugForm
        End If
        For Each oChildCommand In moForEach
          bReturn = oChildCommand.Execute(oETLControl, ErrorMessage)
          If Not bReturn Then
            GoTo LocalExit
          End If
        Next oChildCommand
        moRs.MoveNext
      Loop
    End If
    moRs.Close
    Set moRs = Nothing
  Else
    moCmd.Execute
  End If
  bReturn = True
  
LocalExit:
  MainDebug "Function Exit: " & CStr(bReturn), cModule & cProcedure
  Execute = bReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure, msLocation)
  Resume LocalExit

End Function

Friend Function LoadFromXML(oNode As MSXML2.IXMLDOMElement, oETLControl As ETLControl, ByVal ParentLocation As String, _
  ErrorMessage As String, Optional ParentCommand As ETLCommand = Nothing) As Boolean
On Error GoTo LocalErrHandler
Const cProcedure As String = "LoadFromXML()"
Dim bReturn As Boolean, I As Long, sParamName As String, sParamType As String, sParamDirection As String, vParamSize As Variant
Dim oParamNodes As MSXML2.IXMLDOMNodeList, oParamNode As MSXML2.IXMLDOMElement, oParam As ADODB.Parameter
Dim oForEachNode As MSXML2.IXMLDOMElement

  bReturn = False
  msCmdName = oNode.getAttribute("name")
  msLocation = ParentLocation & vbCrLf & "Command:  " & msCmdName
  mbEnabled = oNode.getAttribute("enabled")
  If Not mbEnabled Then
    bReturn = True
    GoTo LocalExit
  End If
  Debug.Print "Loading Command From XML: " & msCmdName
  mbRowSet = oNode.getAttribute("rowset")
  mbBeginTran = oNode.getAttribute("begintran")
  msConnID = oNode.getAttribute("connid")
  moCmd.CommandText = GetXmlCData(oNode)
  If moCmd.CommandText = "" Then
    ErrorMessage = MainErrHandler(0, "Command Element Does Not Have SQL Statement in CDATA Section", cModule & cProcedure, msLocation)
    GoTo LocalExit
  End If
  Set moETLConnection = oETLControl.ETLConnections(msConnID)
  If Not moETLConnection.IsOpen Then
    If Not moETLConnection.OpenConnection(ErrorMessage) Then
      GoTo LocalExit
    End If
  End If
  Set moCmd.ActiveConnection = moETLConnection.Cn
  If Not ParentCommand Is Nothing Then
    Set moParentCommand = ParentCommand
  End If
  Set oParamNodes = oNode.selectNodes("params/param")
  If oParamNodes.length > 0 Then
    ReDim mvParamSources(oParamNodes.length - 1)
    mbHasParameters = True
    For I = 1 To oParamNodes.length
      Set oParamNode = oParamNodes.nextNode()
      sParamName = oParamNode.getAttribute("name")
      sParamType = oParamNode.getAttribute("type")
      sParamDirection = oParamNode.getAttribute("direction")
      mvParamSources(I - 1) = oParamNode.getAttribute("source")
      Set oParam = moCmd.CreateParameter(sParamName, GetADODataType(sParamType), GetADOParamDir(sParamDirection))
      vParamSize = oParamNode.getAttribute("size")
      If Not IsNull(vParamSize) Then
        oParam.Size = vParamSize
      End If
      moCmd.Parameters.Append oParam
      Set oParam = Nothing
    Next I
  End If
  Set oParamNodes = Nothing
  If oETLControl.ShowDebugForm Then
    mbUseDebugForm = True
    Set moDebugForm = New frmETLCommand
    moDebugForm.Caption = msCmdName
  End If
  Set oForEachNode = oNode.selectSingleNode("foreach")
  If moForEach.LoadFromXML(oForEachNode, oETLControl, msLocation, ErrorMessage, Me) >= 0 Then
    bReturn = True
  End If
  Set oForEachNode = Nothing
  
LocalExit:
  MainDebug "Function Exit: " & CStr(bReturn), cModule & cProcedure
  LoadFromXML = bReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure, msLocation)
  Resume LocalExit
  
End Function

Public Property Get ParentCommand() As ETLCommand
  Set ParentCommand = moParentCommand
End Property

Public Property Get CommandNode() As MSXML2.IXMLDOMElement
  Set CommandNode = moCommandNode
End Property

Public Property Get Cmd() As ADODB.Command
  Set Cmd = moCmd
End Property

Public Property Get Rs() As ADODB.Recordset
  Set Rs = moRs
End Property

Public Property Get ForEach() As ETLCommands
  Set ForEach = moForEach
End Property

Public Property Get CommandText() As String
  CommandText = moCmd.CommandText
End Property

Public Property Get BeginTran() As Boolean
  BeginTran = mbBeginTran
End Property

Public Property Get Rowset() As Boolean
  Rowset = mbRowSet
End Property

Public Property Get CmdName() As String
  CmdName = msCmdName
End Property

Private Sub Class_Initialize()
  Set moCmd = New ADODB.Command
  Set moForEach = New ETLCommands
End Sub

Private Sub Class_Terminate()
  Set moCommandNode = Nothing
  Set moRs = Nothing
  Set moCmd = Nothing
  Set moForEach = Nothing
  Set moParentCommand = Nothing
  Set moETLConnection = Nothing
  If mbDebugFormLoaded Then
    Unload moDebugForm
  End If
  If mbUseDebugForm Then
    Set moDebugForm = Nothing
  End If
End Sub

