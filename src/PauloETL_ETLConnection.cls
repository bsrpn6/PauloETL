VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "ETLConnection"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
Attribute VB_Ext_KEY = "SavedWithClassBuilder6" ,"Yes"
Attribute VB_Ext_KEY = "Top_Level" ,"No"
Option Explicit
'Constant for Module Name Used In Error Functions
Const cModule As String = "ETLControl."

Private moCn As ADODB.Connection
Private msID As String
Private msName As String
Private msUID As String
Private msPWD As String

Friend Function LoadFromXML(oNode As MSXML2.IXMLDOMElement, ErrorMessage As String) As Boolean
On Error GoTo LocalErrHandler
Const cProcedure As String = "LoadFromXML()"
Dim bReturn As Boolean

  bReturn = False
  msID = oNode.getAttribute("id")
  msName = oNode.getAttribute("name")
  moCn.ConnectionString = oNode.getAttribute("connstring")
  msUID = oNode.getAttribute("uid")
  msPWD = oNode.getAttribute("pwd")
  bReturn = True
  
LocalExit:
  MainDebug "Function Exit: " & CStr(bReturn), cModule & cProcedure
  LoadFromXML = bReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure, "Connection: " & msID)
  Resume LocalExit
  
End Function

Public Function OpenConnection(ErrorMessage As String) As Boolean
On Error GoTo LocalErrHandler
Const cProcedure As String = "OpenConnection()"
Dim bReturn As Boolean

  bReturn = False
  moCn.Open , msUID, msPWD
  bReturn = True
  
LocalExit:
  MainDebug "Function Exit: " & CStr(bReturn), cModule & cProcedure
  OpenConnection = bReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure, "Connection: " & msID)
  Resume LocalExit

End Function

Public Property Get IsOpen() As Boolean

  If moCn.State = adStateOpen Then
    IsOpen = True
  Else
    IsOpen = False
  End If
  
End Property

Public Property Get Cn() As ADODB.Connection
  Set Cn = moCn
End Property

Friend Property Let ID(ByVal New_ID As String)
  msID = New_ID
End Property

Public Property Get ID() As String
  ID = msID
End Property

Friend Property Let Name(ByVal New_Name As String)
  msName = New_Name
End Property

Public Property Get Name() As String
  Name = msName
End Property

Private Sub Class_Initialize()
  Set moCn = New ADODB.Connection
End Sub

Private Sub Class_Terminate()
  Set moCn = Nothing
End Sub
