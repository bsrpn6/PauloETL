VERSION 5.00
Begin VB.Form frmJobView 
   Caption         =   "Job Detail"
   ClientHeight    =   4740
   ClientLeft      =   555
   ClientTop       =   2430
   ClientWidth     =   12045
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
   ScaleHeight     =   4740
   ScaleWidth      =   12045
   Begin VB.CheckBox chkSingleStep 
      Caption         =   "Single Step Commands"
      Height          =   375
      Left            =   2760
      TabIndex        =   2
      Top             =   3720
      Width           =   2655
   End
   Begin VB.CommandButton cmdExecStep 
      Caption         =   "Execute Step"
      Height          =   615
      Left            =   240
      TabIndex        =   1
      Top             =   3720
      Width           =   2295
   End
   Begin VB.ListBox lstSteps 
      Height          =   3120
      Left            =   120
      TabIndex        =   0
      Top             =   480
      Width           =   11775
   End
End
Attribute VB_Name = "frmJobView"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Private moETLControl As PauloETL.ETLControl
Private moStepNodes As MSXML2.IXMLDOMNodeList
Private msJobID As String

Private Sub chkSingleStep_Click()
  moETLControl.SingleStep = (chkSingleStep.Value = vbChecked)
End Sub

Private Sub cmdExecStep_Click()
Dim oStepNode As MSXML2.IXMLDOMElement
Dim sError As String
  
  Set oStepNode = moStepNodes.Item(lstSteps.ListIndex)
  moETLControl.ExecuteJobStep msJobID, oStepNode, sError
  
End Sub

Private Sub Form_Load()
Dim oStepNode As MSXML2.IXMLDOMElement
Dim sError As String, lStepCount As Long, I As Long

  Me.Caption = Me.Caption & " : " & frmETLMain.JobName
  Set moETLControl = frmETLMain.ETLControl
  msJobID = frmETLMain.JobID
  lStepCount = moETLControl.LoadJobStepsFromXML(msJobID, moStepNodes, sError)
  If lStepCount > 0 Then
    For I = 1 To lStepCount
      Set oStepNode = moStepNodes.Item(I - 1)
      lstSteps.AddItem oStepNode.getAttribute("name")
    Next I
  End If
  moETLControl.ShowDebugForm = True
  
End Sub

Private Sub Form_Unload(Cancel As Integer)
  Set moETLControl = Nothing
End Sub


