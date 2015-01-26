VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
  Persistable = 0  'NotPersistable
  DataBindingBehavior = 0  'vbNone
  DataSourceBehavior  = 0  'vbNone
  MTSTransactionMode  = 0  'NotAnMTSObject
END
Attribute VB_Name = "ETLConnections"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = False
Attribute VB_Exposed = True
Attribute VB_Ext_KEY = "SavedWithClassBuilder6" ,"Yes"
Attribute VB_Ext_KEY = "Collection" ,"ETLConnection"
Attribute VB_Ext_KEY = "Member0" ,"ETLConnection"
Attribute VB_Ext_KEY = "Top_Level" ,"Yes"
Option Explicit
'Constant for Module Name Used In Error Functions
Const cModule As String = "ETLConnections."

'local variable to hold collection
Private mCol As Collection
Private moContextNode As MSXML2.IXMLDOMNode

Friend Function LoadFromXML(XMLDoc As MSXML2.DOMDocument60, ErrorMessage As String) As Long
On Error GoTo LocalErrHandler
Const cProcedure As String = "LoadFromXML()"
Dim oNodeList As MSXML2.IXMLDOMNodeList, oNode As MSXML2.IXMLDOMElement, oETLConnection As ETLConnection
Dim I As Long, lReturn As Long, lNodeCount As Long

  lReturn = 0
  'Load NodeList of Connections
  Set oNodeList = XMLDoc.selectNodes("//root/connections/connection")
  lNodeCount = oNodeList.length
  If lNodeCount < 2 Then
    ErrorMessage = MainErrHandler(0, "Error, File Must Have At Least 2 Connections Defined", cModule & cProcedure)
    GoTo LocalExit
  End If
  'Loop Through Connections
  For I = 1 To lNodeCount
    Set oNode = oNodeList.nextNode()
    Set oETLConnection = New ETLConnection
    If oETLConnection.LoadFromXML(oNode, ErrorMessage) Then
      mCol.Add oETLConnection, oETLConnection.ID
    Else
      lNodeCount = 0
      Exit For
    End If
  Next I
  lReturn = lNodeCount

LocalExit:
  MainDebug "Function Exit: " & CStr(lReturn), cModule & cProcedure
  LoadFromXML = lReturn
  Exit Function
  
LocalErrHandler:
  ErrorMessage = MainErrHandler(Err.Number, Err.Description, cModule & cProcedure)
  Resume LocalExit
  
End Function

Public Property Get Item(vntIndexKey As Variant) As ETLConnection
Attribute Item.VB_UserMemId = 0
    'used when referencing an element in the collection
    'vntIndexKey contains either the Index or Key to the collection,
    'this is why it is declared as a Variant
    'Syntax: Set foo = x.Item(xyz) or Set foo = x.Item(5)
  Set Item = mCol(vntIndexKey)
End Property

Public Property Get Count() As Long
    'used when retrieving the number of elements in the
    'collection. Syntax: Debug.Print x.Count
    Count = mCol.Count
End Property


Public Property Get NewEnum() As IUnknown
Attribute NewEnum.VB_UserMemId = -4
Attribute NewEnum.VB_MemberFlags = "40"
    'this property allows you to enumerate
    'this collection with the For...Each syntax
    Set NewEnum = mCol.[_NewEnum]
End Property


Private Sub Class_Initialize()
    'creates the collection when this class is created
    Set mCol = New Collection
End Sub


Private Sub Class_Terminate()
    'destroys collection when this class is terminated
    Set mCol = Nothing
End Sub

