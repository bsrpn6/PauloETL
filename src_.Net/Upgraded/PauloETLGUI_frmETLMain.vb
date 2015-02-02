Option Strict Off
Option Explicit On
Imports Microsoft.VisualBasic
Imports System
Imports System.Windows.Forms
Imports System.Xml
Imports UpgradeHelpers.Gui
Imports UpgradeHelpers.Helpers
Partial Friend Class frmETLMain
	Inherits System.Windows.Forms.Form

	Private moETLControl As PauloETL.ETLControl
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
		InitializeComponent()
		ReLoadForm(False)
	End Sub



	Public ReadOnly Property ETLControl() As PauloETL.ETLControl
		Get
			Return moETLControl
		End Get
	End Property

	Public ReadOnly Property JobID() As String
		Get
			Return lvwJobs.FocusedItem.Name
		End Get
	End Property

	Public ReadOnly Property JobName() As String
		Get
			Return ListViewHelper.GetListViewSubItem(lvwJobs.FocusedItem, 1).Text
		End Get
	End Property

	Private Function LoadJobsFromXML() As Integer
		Dim lReturn, lNodeCount As Integer
		Try
            'Const cProcedure As String = "LoadJobsFromXML()"
			Dim XMLDoc As XmlDocument
			Dim oNodeList As XmlNodeList
			Dim oNode As XmlElement
            'Dim oETLConnection As PauloETL.ETLConnection
            'lReturn = "lNodeCount"
			Dim oItem As ListViewItem
			Dim sValue As String = ""

			lReturn = 0
			lvwJobs.Columns.Insert(0, "ID", CInt(lvwJobs.Width * 0.25))
			lvwJobs.Columns.Insert(1, "Job Name", CInt(lvwJobs.Width * 0.75))
			XMLDoc = moETLControl.XMLDoc
			'Load NodeList of Connections
			oNodeList = XMLDoc.SelectNodes("//root/jobs/job")
			lNodeCount = oNodeList.Count
			If lNodeCount < 1 Then
				MessageBox.Show("Error, File Must Have At Least 1 Job Defined", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
			Else
				'Loop Through Connections
				For I As Integer = 1 To lNodeCount
					'UPGRADE_WARNING: (2065) MSXML2.IXMLDOMNodeList method oNodeList.nextNode has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2065.aspx
                    oNode = oNodeList.Item(I - 1)

					sValue = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("id"))
					oItem = lvwJobs.Items.Insert(I - 1, sValue, sValue, "")
					sValue = ReflectionHelper.GetPrimitiveValue(Of String)(oNode.GetAttribute("name"))
					ListViewHelper.GetListViewSubItem(oItem, 1).Text = sValue
				Next I
				lReturn = lNodeCount

			End If

		Catch excep As System.Exception

			MessageBox.Show(excep.Message, Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)

		End Try
		Return lReturn

	End Function


	Private Sub cmdExecuteJob_Click(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles cmdExecuteJob.Click
		Dim sError As String = ""

		If lvwJobs.FocusedItem.Index + 1 < 1 Then
			MessageBox.Show("Must Select job", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
		Else
			If moETLControl.ExecuteJob(Me.JobID) > 0 Then
				MessageBox.Show("Job Completed", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
			End If
		End If
	End Sub

	Private Sub cmdExecuteMain_Click(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles cmdExecuteMain.Click
		Dim sError As String = ""

		If moETLControl.ExecuteJob("Main", txtXMLFile.Text, sError) > 0 Then
			MessageBox.Show("Job Executed", Application.ProductName)
		End If

	End Sub

	Private Sub cmdLoadXML_Click(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles cmdLoadXML.Click
		Dim sError As String = ""
		Dim oItem As ListViewItem

		Dim lRow As Integer = 1
		lvwConnections.Columns.Insert(0, "ID", CInt(lvwConnections.Width * 0.25))
		lvwConnections.Columns.Insert(1, "Connection Name", CInt(lvwConnections.Width * 0.75))
        If moETLControl.LoadXMLConfig(txtXMLFile.Text, sError) > 0 Then

            Dim i As Integer = 0

            'For Each oETLConnection As PauloETL.ETLConnection In moETLControl.ETLConnections
            '    oItem = lvwConnections.Items.Insert(lRow - 1, oETLConnection.ID, oETLConnection.ID, "")
            '    ListViewHelper.GetListViewSubItem(oItem, 1).Text = oETLConnection.Name
            '    lRow += 1
            'Next oETLConnection

            Dim connectionsEnumerator As IDictionaryEnumerator

            connectionsEnumerator = moETLControl.ETLConnections.GetEnumerator()

            While connectionsEnumerator.MoveNext()
                Dim oETLConnection As PauloETL.ETLConnection
                oETLConnection = moETLControl.ETLConnections.Item(connectionsEnumerator.Key)

                oItem = lvwConnections.Items.Insert(lRow - 1, oETLConnection.ID, oETLConnection.ID, "")
                ListViewHelper.GetListViewSubItem(oItem, 1).Text = oETLConnection.Name
                lRow += 1
            End While


            cmdLoadXML.Enabled = False
            cmdTestConnection.Enabled = True
            cmdViewJob.Enabled = True
            cmdExecuteJob.Enabled = True
            'Load Job List
            LoadJobsFromXML()
        Else
            'Destroy and Re-Create ETL Control Interface
            moETLControl = Nothing
            moETLControl = New PauloETL.ETLControl()
            moETLControl.ModalErrors = True
        End If
	End Sub

	Private Sub cmdTestConnection_Click(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles cmdTestConnection.Click
		Dim oETLConnection As PauloETL.ETLConnection
		Dim ErrorMessage As String = ""

		If lvwConnections.FocusedItem.Index + 1 < 1 Then
			MessageBox.Show("Must Select Connection", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
		Else
			oETLConnection = moETLControl.ETLConnections(lvwConnections.FocusedItem.Name)
			If oETLConnection.OpenConnection(ErrorMessage) Then
				MessageBox.Show("Connection Succeeded", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
			Else
			End If
		End If

	End Sub

	Private Sub cmdViewJob_Click(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles cmdViewJob.Click

		If lvwJobs.FocusedItem.Index + 1 < 1 Then
			MessageBox.Show("Must Select job", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
		Else
            frmJobView.Show()
            'DefInstance.Show()
		End If

	End Sub

	'UPGRADE_WARNING: (2080) Form_Load event was upgraded to Form_Load method and has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2080.aspx
	Private Sub Form_Load()
		moETLControl = New PauloETL.ETLControl()
		moETLControl.ModalErrors = True
	End Sub

	Private Sub frmETLMain_Closed(ByVal eventSender As Object, ByVal eventArgs As EventArgs) Handles MyBase.Closed
		moETLControl = Nothing
	End Sub

    Private Sub btnSelectFile_Click(sender As Object, e As EventArgs) Handles btnSelectFile.Click
        Dim fd As OpenFileDialog = New OpenFileDialog()
        Dim strFileName As String

        fd.Title = "Open File Dialog"
        fd.InitialDirectory = "C:\"
        fd.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
        fd.FilterIndex = 1
        fd.RestoreDirectory = True

        If fd.ShowDialog() = DialogResult.OK Then
            strFileName = fd.FileName
            txtXMLFile.Text = strFileName
        End If

    End Sub

End Class