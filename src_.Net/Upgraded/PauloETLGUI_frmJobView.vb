Option Strict Off
Option Explicit On
Imports System
Imports System.Windows.Forms
Imports System.Xml
Imports UpgradeHelpers.Gui
Imports UpgradeHelpers.Helpers
Partial Friend Class frmJobView
	Inherits System.Windows.Forms.Form

	Private moETLControl As PauloETL.ETLControl
	Private moStepNodes As XmlNodeList
	Private msJobID As String = ""

	Private isInitializingComponent As Boolean
	Public Sub New()
		MyBase.New()
		If m_vb6FormDefInstance Is Nothing Then
			If m_InitializingDefInstance Then
				m_vb6FormDefInstance = Me
			Else
				Try
					'For the start-up form, the first instance created is the default instance.
					If System.Reflection.Assembly.GetExecutingAssembly.EntryPoint.DeclaringType Is Me.GetType Then
						m_vb6FormDefInstance = Me
					End If

				Catch
				End Try
			End If
		End If
		'This call is required by the Windows Form Designer.
		isInitializingComponent = True
		InitializeComponent()
		isInitializingComponent = False
		ReLoadForm(False)
	End Sub


	Private Sub chkSingleStep_CheckStateChanged(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles chkSingleStep.CheckStateChanged
		If isInitializingComponent Then
			Exit Sub
		End If
		moETLControl.SingleStep = (chkSingleStep.CheckState = CheckState.Checked)
	End Sub

	Private Sub cmdExecStep_Click(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles cmdExecStep.Click
		Dim sError As String = ""

		Dim oStepNode As XmlElement = moStepNodes.Item(ListBoxHelper.GetSelectedIndex(lstSteps))
		moETLControl.ExecuteJobStep(msJobID, oStepNode, sError)

	End Sub

	'UPGRADE_WARNING: (2080) Form_Load event was upgraded to Form_Load method and has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2080.aspx
	Private Sub Form_Load()
		Dim oStepNode As XmlElement
		Dim sError As String = ""

		Me.Text = Me.Text & " : " & frmETLMain.DefInstance.JobName
		moETLControl = frmETLMain.DefInstance.ETLControl
		msJobID = frmETLMain.DefInstance.JobID
		Dim lStepCount As Integer = moETLControl.LoadJobStepsFromXML(msJobID, moStepNodes, sError)
		If lStepCount > 0 Then
			For I As Integer = 1 To lStepCount
				oStepNode = moStepNodes.Item(I - 1)
				lstSteps.AddItem(ReflectionHelper.GetPrimitiveValue(Of String)(oStepNode.GetAttribute("name")))
			Next I
		End If
		moETLControl.ShowDebugForm = True

	End Sub

	Private Sub frmJobView_Closed(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles MyBase.Closed
		moETLControl = Nothing
	End Sub
End Class