VERSION 5.00
Object = "{831FDD16-0C5C-11D2-A9FC-0000F8754DA1}#2.0#0"; "MSCOMCTL.OCX"
Begin VB.Form frmETLMain 
   Caption         =   "Paulo ETL Test Program"
   ClientHeight    =   8895
   ClientLeft      =   225
   ClientTop       =   1110
   ClientWidth     =   15300
   BeginProperty Font 
      Name            =   "Arial"
      Size            =   11.25
      Charset         =   0
      Weight          =   400
      Underline       =   0   'False
      Italic          =   0   'False
      Strikethrough   =   0   'False
   EndProperty
   LinkTopic       =   "Form1"
   ScaleHeight     =   8895
   ScaleWidth      =   15300
   Begin VB.CommandButton cmdExecuteMain 
      Caption         =   "Execute Job Main"
      Height          =   495
      Left            =   12480
      TabIndex        =   8
      Top             =   120
      Width           =   2415
   End
   Begin VB.CommandButton cmdExecuteJob 
      Caption         =   "Execute Job"
      Enabled         =   0   'False
      Height          =   615
      Left            =   13080
      TabIndex        =   7
      Top             =   5880
      Width           =   2055
   End
   Begin VB.CommandButton cmdViewJob 
      Caption         =   "View Job"
      Enabled         =   0   'False
      Height          =   615
      Left            =   13080
      TabIndex        =   6
      Top             =   5040
      Width           =   2055
   End
   Begin VB.CommandButton cmdTestConnection 
      Caption         =   "Test Connection"
      Enabled         =   0   'False
      Height          =   615
      Left            =   13080
      TabIndex        =   4
      Top             =   1440
      Width           =   2055
   End
   Begin MSComctlLib.ListView lvwConnections 
      Height          =   3255
      Left            =   120
      TabIndex        =   3
      Top             =   1440
      Width           =   12855
      _ExtentX        =   22675
      _ExtentY        =   5741
      View            =   3
      LabelEdit       =   1
      LabelWrap       =   -1  'True
      HideSelection   =   0   'False
      FullRowSelect   =   -1  'True
      _Version        =   393217
      ForeColor       =   -2147483640
      BackColor       =   -2147483643
      BorderStyle     =   1
      Appearance      =   1
      NumItems        =   0
   End
   Begin VB.CommandButton cmdLoadXML 
      Caption         =   "Load XML"
      Height          =   495
      Left            =   120
      TabIndex        =   2
      Top             =   600
      Width           =   1695
   End
   Begin VB.TextBox txtXMLFile 
      Height          =   375
      Left            =   1680
      TabIndex        =   1
      Text            =   "C:\VB\PauloETL\PauloETL.xml"
      Top             =   120
      Width           =   9735
   End
   Begin MSComctlLib.ListView lvwJobs 
      Height          =   3495
      Left            =   120
      TabIndex        =   5
      Top             =   5040
      Width           =   12855
      _ExtentX        =   22675
      _ExtentY        =   6165
      View            =   3
      LabelEdit       =   1
      LabelWrap       =   -1  'True
      HideSelection   =   0   'False
      FullRowSelect   =   -1  'True
      _Version        =   393217
      ForeColor       =   -2147483640
      BackColor       =   -2147483643
      BorderStyle     =   1
      Appearance      =   1
      NumItems        =   0
   End
   Begin VB.Label lblXMLFile 
      Caption         =   "XML File Path:"
      Height          =   375
      Left            =   120
      TabIndex        =   0
      Top             =   120
      Width           =   1695
   End
End
Attribute VB_Name = "frmETLMain"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Private moETLControl As PauloETL.ETLControl

Public Property Get ETLControl() As PauloETL.ETLControl
  Set ETLControl = moETLControl
End Property

Public Property Get JobID() As String
  JobID = lvwJobs.SelectedItem.Key
End Property

Public Property Get JobName() As String
  JobName = lvwJobs.SelectedItem.SubItems(1)
End Property

Private Function LoadJobsFromXML() As Long
On Error GoTo LocalErrHandler
Const cProcedure As String = "LoadJobsFromXML()"
Dim XMLDoc As MSXML2.DOMDocument60
Dim oNodeList As MSXML2.IXMLDOMNodeList, oNode As MSXML2.IXMLDOMElement, oETLConnection As ETLConnection
Dim I As Long, lReturn As Long, lNodeCount As Long, oItem As ListItem, sValue As String

  lReturn = 0
  lvwJobs.ColumnHeaders.Add 1, , "ID", lvwJobs.Width * 0.25
  lvwJobs.ColumnHeaders.Add 2, , "Job Name", lvwJobs.Width * 0.75
  Set XMLDoc = moETLControl.XMLDoc
  'Load NodeList of Connections
  Set oNodeList = XMLDoc.selectNodes("//root/jobs/job")
  lNodeCount = oNodeList.length
  If lNodeCount < 1 Then
    MsgBox "Error, File Must Have At Least 1 Job Defined", vbExclamation, Me.Caption
    GoTo LocalExit
  End If
  'Loop Through Connections
  For I = 1 To lNodeCount
    Set oNode = oNodeList.nextNode()
    sValue = oNode.getAttribute("id")
    Set oItem = lvwJobs.ListItems.Add(I, sValue, sValue)
    sValue = oNode.getAttribute("name")
    oItem.SubItems(1) = sValue
  Next I
  lReturn = lNodeCount

LocalExit:
  LoadJobsFromXML = lReturn
  Exit Function
  
LocalErrHandler:
  MsgBox Err.Description, vbExclamation, Me.Caption
  Resume LocalExit
  
End Function


Private Sub cmdExecuteJob_Click()
Dim sError As String

  If lvwJobs.SelectedItem.Index < 1 Then
    MsgBox "Must Select job", vbExclamation, Me.Caption
  Else
    If moETLControl.executejob(Me.JobID) > 0 Then
      MsgBox "Job Completed", vbInformation, Me.Caption
    End If
  End If
End Sub

Private Sub cmdExecuteMain_Click()
Dim sError As String

  If moETLControl.executejob("Main", txtXMLFile.Text, sError) > 0 Then
    MsgBox "Job Executed"
  End If
  
End Sub

Private Sub cmdLoadXML_Click()
Dim sError As String, oETLConnection As ETLConnection
Dim oItem As ListItem, lRow As Long

  lRow = 1
  lvwConnections.ColumnHeaders.Add 1, , "ID", lvwConnections.Width * 0.25
  lvwConnections.ColumnHeaders.Add 2, , "Connection Name", lvwConnections.Width * 0.75
  If moETLControl.LoadXMLConfig(txtXMLFile.Text, sError) > 0 Then
    For Each oETLConnection In moETLControl.ETLConnections
      Set oItem = lvwConnections.ListItems.Add(lRow, oETLConnection.id, oETLConnection.id)
      oItem.SubItems(1) = oETLConnection.Name
      lRow = lRow + 1
    Next oETLConnection
    cmdLoadXML.Enabled = False
    cmdTestConnection.Enabled = True
    cmdViewJob.Enabled = True
    cmdExecuteJob.Enabled = True
    'Load Job List
    LoadJobsFromXML
  Else
    'Destroy and Re-Create ETL Control Interface
    Set moETLControl = Nothing
    Set moETLControl = New PauloETL.ETLControl
    moETLControl.ModalErrors = True
  End If
End Sub

Private Sub cmdTestConnection_Click()
Dim oETLConnection As ETLConnection, ErrorMessage As String
  
  If lvwConnections.SelectedItem.Index < 1 Then
    MsgBox "Must Select Connection", vbExclamation, Me.Caption
  Else
    Set oETLConnection = moETLControl.ETLConnections(lvwConnections.SelectedItem.Key)
    If oETLConnection.OpenConnection(ErrorMessage) Then
      MsgBox "Connection Succeeded", vbInformation, Me.Caption
    Else
    End If
  End If
  
End Sub

Private Sub cmdViewJob_Click()

  If lvwJobs.SelectedItem.Index < 1 Then
    MsgBox "Must Select job", vbExclamation, Me.Caption
  Else
    frmJobView.Show
  End If

End Sub

Private Sub Form_Load()
  Set moETLControl = New PauloETL.ETLControl
  moETLControl.ModalErrors = True
End Sub

Private Sub Form_Unload(Cancel As Integer)
  Set moETLControl = Nothing
End Sub

