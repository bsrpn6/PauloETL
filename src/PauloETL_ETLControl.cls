VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "ETLControl"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = True
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
Attribute VB_Ext_KEY = "SavedWithClassBuilder6" ,"Yes"
Attribute VB_Ext_KEY = "Top_Level" ,"Yes"
Attribute VB_Ext_KEY = "Member0" ,"ETLConnections"
Option Explicit
'Constant for Module Name Used In Error Functions
Const cModule As String = "ETLControl."

Public ShowDebugForm As Boolean
Public SingleStep As Boolean

Private moXMLDoc As MSXML2.DOMDocument60
Private moETLConnections As ETLConnections
'Private moActiveCommand As ETLCommand
Private mbXMLDocLoaded As Boolean
'Private msLocation As String


Public Function LoadXMLConfig(ByVal XMLFileName As String, ErrorMessage As String) As Long
On Error GoTo LocalErrHandler
Const cProcedure As String = "LoadXMLConfig()"
Dim lReturn As Long

  lReturn = 0
  mbXMLDocLoaded = False
  moXMLDoc.validateOnParse = False
  moXMLDoc.async = False
  'Load XML Into Document Object
  If Not moXMLDoc.Load(XMLFileName) Then
    ErrorMessage = MainErrHandler(0, "Error Loading XML File", cModule & cProcedure)
    GoTo LocalExit
  End If
  'Validate XML Against Schema
  moXMLDoc.Validate
  If moXMLDoc.parseError.ErrorCode <> 0 Then
    ErrorMessage = MainErrHandler(0, "Error Validating XML File: " & vbCrLf & moXMLDoc.parseError.srcText, cModule & cProcedure)
    GoTo LocalExit
  End If
  'Load Connections Into Collection
  If moETLConnections.LoadFromXML(moXMLDoc, ErrorMessage) = 0 Then
    GoTo LocalExit
  End If
  'Return Success
  mbXMLDocLoaded = True
  lReturn = 1
  
LocalExit:
  MainDebug "Function Exit: " & CStr(lReturn), cModule & cProcedure
  LoadXMLConfig = lReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure)
  Resume LocalExit
  
End Function

Public Function LoadJobStepsFromXML(ByVal JobID As String, StepList As MSXML2.IXMLDOMNodeList, ErrorMessage As String) As Long
On Error GoTo LocalErrHandler
Const cProcedure As String = "LoadJobStepsFromXML()"
Dim oJobNode As MSXML2.IXMLDOMNode
Dim lReturn As Long, lNodeCount As Long

  lReturn = 0
  'Locate Job Node
  Set oJobNode = moXMLDoc.selectSingleNode("//root/jobs/job[@id=""" & JobID & """]")
  If oJobNode Is Nothing Then
    ErrorMessage = MainErrHandler(0, "Error, Job ID Not Found", cModule & cProcedure, "Job: " & JobID)
    GoTo LocalExit
  End If
  'Load NodeList of Steps
  Set StepList = oJobNode.selectNodes("step")
  lNodeCount = StepList.length
  If lNodeCount < 1 Then
    ErrorMessage = MainErrHandler(0, "Error, Job Must Have At Least 1 Step Defined", cModule & cProcedure, "Job: " & JobID)
    GoTo LocalExit
  End If
  lReturn = lNodeCount
  
LocalExit:
  MainDebug "Function Exit: " & CStr(lReturn), cModule & cProcedure
  LoadJobStepsFromXML = lReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure)
  Resume LocalExit
  
End Function

Public Function ExecuteJobStep(ByVal JobID As String, StepNode As MSXML2.IXMLDOMElement, ErrorMessage As String) As Long
On Error GoTo LocalErrHandler
Const cProcedure As String = "ExecuteJobStep()"
Dim oETLCommand As ETLCommand, oETLCommandNode As MSXML2.IXMLDOMElement
Dim lReturn As Long, sStepName As String, sLocation As String

  lReturn = 0
  'Locate Job Node
  sStepName = StepNode.getAttribute("name")
  sLocation = "Job:  " & JobID & vbCrLf & "Step: " & sStepName
  Set oETLCommandNode = StepNode.selectSingleNode("command")
  Set oETLCommand = New ETLCommand
  If oETLCommand.LoadFromXML(oETLCommandNode, Me, sLocation, ErrorMessage) Then
    If oETLCommand.Execute(Me, ErrorMessage) Then
      lReturn = 1
    End If
  End If
  Set oETLCommand = Nothing
  
LocalExit:
  MainDebug "Function Exit: " & CStr(lReturn), cModule & cProcedure
  ExecuteJobStep = lReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure)
  Resume LocalExit
  
End Function

Public Function ExecuteJob(ByVal JobID As String, Optional ByVal XMLFileName As String = "", Optional ErrorMessage As String = "") As Long
On Error GoTo LocalErrHandler
Const cProcedure As String = "ExecuteJob()"
Dim oStepNodes As MSXML2.IXMLDOMNodeList, oStepNode As MSXML2.IXMLDOMElement
Dim lReturn As Long, lStepCount As Long, I As Long

  lReturn = 0
  If Not mbXMLDocLoaded Then
    If XMLFileName = "" Then
      ErrorMessage = MainErrHandler(0, "Must Specify XMLFileName Unless Already Loaded", cModule & cProcedure)
      GoTo LocalExit
    Else
      If LoadXMLConfig(XMLFileName, ErrorMessage) <= 0 Then
        GoTo LocalExit
      End If
    End If
  End If
  lStepCount = LoadJobStepsFromXML(JobID, oStepNodes, ErrorMessage)
  If lStepCount > 0 Then
    For I = 1 To lStepCount
      Set oStepNode = oStepNodes.Item(I - 1)
      lReturn = ExecuteJobStep(JobID, oStepNode, ErrorMessage)
      If lReturn <= 0 Then
        Exit For
      End If
    Next I
  End If
  
LocalExit:
  MainDebug "Function Exit: " & CStr(lReturn), cModule & cProcedure
  ExecuteJob = lReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure)
  Resume LocalExit

End Function

'Friend Property Let Location(ByVal New_Location As String)
'  msLocation = New_Location
'End Property
'
'Public Property Get Location() As String
'  Location = msLocation
'End Property
'
'Friend Property Set ActiveCommand(New_ActiveCommand As ETLCommand)
' Set moActiveCommand = New_ActiveCommand
'End Property
'
'Public Property Get ActiveCommand() As ETLCommand
'  Set ActiveCommand = moActiveCommand
'End Property


Public Property Get XMLDoc() As MSXML2.DOMDocument60
  Set XMLDoc = moXMLDoc
End Property

Public Property Get ETLConnections() As ETLConnections
  Set ETLConnections = moETLConnections
End Property

Public Property Let ModalErrors(ByVal New_ModalErrors As Boolean)
  gbModalErrors = New_ModalErrors
End Property

Public Property Get ModalErrors() As Boolean
  ModalErrors = gbModalErrors
End Property

Private Sub Class_Initialize()
  Set moXMLDoc = New MSXML2.DOMDocument60
  Set moETLConnections = New ETLConnections
End Sub

Private Sub Class_Terminate()
  Set moXMLDoc = Nothing
  Set moETLConnections = Nothing
End Sub
