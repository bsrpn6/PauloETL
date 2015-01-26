<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmETLMain
#Region "Upgrade Support "
	Private Shared m_vb6FormDefInstance As frmETLMain
	Private Shared m_InitializingDefInstance As Boolean
	Public Shared Property DefInstance() As frmETLMain
		Get
			If m_vb6FormDefInstance Is Nothing OrElse m_vb6FormDefInstance.IsDisposed Then
				m_InitializingDefInstance = True
				m_vb6FormDefInstance = CreateInstance()
				m_InitializingDefInstance = False
			End If
			Return m_vb6FormDefInstance
		End Get
		Set(ByVal Value As frmETLMain)
			m_vb6FormDefInstance = Value
		End Set
	End Property
#End Region
#Region "Windows Form Designer generated code "
	Public Shared Function CreateInstance() As frmETLMain
		Dim theInstance As frmETLMain = New frmETLMain()
		theInstance.Form_Load()
		Return theInstance
	End Function
	Private visualControls() As String = New String() {"components", "ToolTipMain", "cmdExecuteMain", "cmdExecuteJob", "cmdViewJob", "cmdTestConnection", "lvwConnections", "cmdLoadXML", "txtXMLFile", "lvwJobs", "lblXMLFile", "listViewHelper1"}
	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer
	Public ToolTipMain As System.Windows.Forms.ToolTip
	Public WithEvents cmdExecuteMain As System.Windows.Forms.Button
	Public WithEvents cmdExecuteJob As System.Windows.Forms.Button
	Public WithEvents cmdViewJob As System.Windows.Forms.Button
	Public WithEvents cmdTestConnection As System.Windows.Forms.Button
	Public WithEvents lvwConnections As System.Windows.Forms.ListView
	Public WithEvents cmdLoadXML As System.Windows.Forms.Button
	Public WithEvents txtXMLFile As System.Windows.Forms.TextBox
	Public WithEvents lvwJobs As System.Windows.Forms.ListView
	Public WithEvents lblXMLFile As System.Windows.Forms.Label
	Private listViewHelper1 As UpgradeHelpers.Gui.ListViewHelper
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> _
	 Private Sub InitializeComponent()
		Me.components = New System.ComponentModel.Container()
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmETLMain))
		Me.ToolTipMain = New System.Windows.Forms.ToolTip(Me.components)
		Me.cmdExecuteMain = New System.Windows.Forms.Button()
		Me.cmdExecuteJob = New System.Windows.Forms.Button()
		Me.cmdViewJob = New System.Windows.Forms.Button()
		Me.cmdTestConnection = New System.Windows.Forms.Button()
		Me.lvwConnections = New System.Windows.Forms.ListView()
		Me.cmdLoadXML = New System.Windows.Forms.Button()
		Me.txtXMLFile = New System.Windows.Forms.TextBox()
		Me.lvwJobs = New System.Windows.Forms.ListView()
		Me.lblXMLFile = New System.Windows.Forms.Label()
		Me.SuspendLayout()
		Me.listViewHelper1 = New UpgradeHelpers.Gui.ListViewHelper(Me.components)
		Me.listViewHelper1.BeginInit()
		' 
		'cmdExecuteMain
		' 
		Me.cmdExecuteMain.BackColor = System.Drawing.SystemColors.Control
		Me.cmdExecuteMain.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdExecuteMain.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdExecuteMain.Location = New System.Drawing.Point(832, 8)
		Me.cmdExecuteMain.Name = "cmdExecuteMain"
		Me.cmdExecuteMain.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdExecuteMain.Size = New System.Drawing.Size(161, 33)
		Me.cmdExecuteMain.TabIndex = 8
		Me.cmdExecuteMain.Text = "Execute Job Main"
		Me.cmdExecuteMain.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
		Me.cmdExecuteMain.UseVisualStyleBackColor = False
		' 
		'cmdExecuteJob
		' 
		Me.cmdExecuteJob.BackColor = System.Drawing.SystemColors.Control
		Me.cmdExecuteJob.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdExecuteJob.Enabled = False
		Me.cmdExecuteJob.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdExecuteJob.Location = New System.Drawing.Point(872, 392)
		Me.cmdExecuteJob.Name = "cmdExecuteJob"
		Me.cmdExecuteJob.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdExecuteJob.Size = New System.Drawing.Size(137, 41)
		Me.cmdExecuteJob.TabIndex = 7
		Me.cmdExecuteJob.Text = "Execute Job"
		Me.cmdExecuteJob.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
		' 
		'cmdViewJob
		' 
		Me.cmdViewJob.BackColor = System.Drawing.SystemColors.Control
		Me.cmdViewJob.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdViewJob.Enabled = False
		Me.cmdViewJob.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdViewJob.Location = New System.Drawing.Point(872, 336)
		Me.cmdViewJob.Name = "cmdViewJob"
		Me.cmdViewJob.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdViewJob.Size = New System.Drawing.Size(137, 41)
		Me.cmdViewJob.TabIndex = 6
		Me.cmdViewJob.Text = "View Job"
		Me.cmdViewJob.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
		' 
		'cmdTestConnection
		' 
		Me.cmdTestConnection.BackColor = System.Drawing.SystemColors.Control
		Me.cmdTestConnection.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdTestConnection.Enabled = False
		Me.cmdTestConnection.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdTestConnection.Location = New System.Drawing.Point(872, 96)
		Me.cmdTestConnection.Name = "cmdTestConnection"
		Me.cmdTestConnection.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdTestConnection.Size = New System.Drawing.Size(137, 41)
		Me.cmdTestConnection.TabIndex = 4
		Me.cmdTestConnection.Text = "Test Connection"
		Me.cmdTestConnection.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
		' 
		'lvwConnections
		' 
		Me.lvwConnections.BackColor = System.Drawing.SystemColors.Window
		Me.lvwConnections.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.lvwConnections.ForeColor = System.Drawing.SystemColors.WindowText
		Me.lvwConnections.FullRowSelect = True
		Me.lvwConnections.HideSelection = False
		Me.lvwConnections.LabelEdit = False
		Me.lvwConnections.Location = New System.Drawing.Point(8, 96)
		Me.lvwConnections.Name = "lvwConnections"
		Me.lvwConnections.Size = New System.Drawing.Size(857, 217)
		Me.lvwConnections.TabIndex = 3
		Me.lvwConnections.View = System.Windows.Forms.View.Details
		' 
		'cmdLoadXML
		' 
		Me.cmdLoadXML.BackColor = System.Drawing.SystemColors.Control
		Me.cmdLoadXML.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdLoadXML.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdLoadXML.Location = New System.Drawing.Point(8, 40)
		Me.cmdLoadXML.Name = "cmdLoadXML"
		Me.cmdLoadXML.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdLoadXML.Size = New System.Drawing.Size(113, 33)
		Me.cmdLoadXML.TabIndex = 2
		Me.cmdLoadXML.Text = "Load XML"
		Me.cmdLoadXML.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
		Me.cmdLoadXML.UseVisualStyleBackColor = False
		' 
		'txtXMLFile
		' 
		Me.txtXMLFile.AcceptsReturn = True
		Me.txtXMLFile.BackColor = System.Drawing.SystemColors.Window
		Me.txtXMLFile.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.txtXMLFile.Cursor = System.Windows.Forms.Cursors.IBeam
		Me.txtXMLFile.ForeColor = System.Drawing.SystemColors.WindowText
		Me.txtXMLFile.Location = New System.Drawing.Point(112, 8)
		Me.txtXMLFile.MaxLength = 0
		Me.txtXMLFile.Name = "txtXMLFile"
		Me.txtXMLFile.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.txtXMLFile.Size = New System.Drawing.Size(649, 25)
		Me.txtXMLFile.TabIndex = 1
		Me.txtXMLFile.Text = "C:\VB\PauloETL\PauloETL.xml"
		' 
		'lvwJobs
		' 
		Me.lvwJobs.BackColor = System.Drawing.SystemColors.Window
		Me.lvwJobs.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.lvwJobs.ForeColor = System.Drawing.SystemColors.WindowText
		Me.lvwJobs.FullRowSelect = True
		Me.lvwJobs.HideSelection = False
		Me.lvwJobs.LabelEdit = False
		Me.lvwJobs.Location = New System.Drawing.Point(8, 336)
		Me.lvwJobs.Name = "lvwJobs"
		Me.lvwJobs.Size = New System.Drawing.Size(857, 233)
		Me.lvwJobs.TabIndex = 5
		Me.lvwJobs.View = System.Windows.Forms.View.Details
		' 
		'lblXMLFile
		' 
		Me.lblXMLFile.BackColor = System.Drawing.SystemColors.Control
		Me.lblXMLFile.BorderStyle = System.Windows.Forms.BorderStyle.None
		Me.lblXMLFile.Cursor = System.Windows.Forms.Cursors.Default
		Me.lblXMLFile.ForeColor = System.Drawing.SystemColors.ControlText
		Me.lblXMLFile.Location = New System.Drawing.Point(8, 8)
		Me.lblXMLFile.Name = "lblXMLFile"
		Me.lblXMLFile.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.lblXMLFile.Size = New System.Drawing.Size(113, 25)
		Me.lblXMLFile.TabIndex = 0
		Me.lblXMLFile.Text = "XML File Path:"
		' 
		'frmETLMain
		' 
		Me.AutoScaleDimensions = New System.Drawing.SizeF(9, 17)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.BackColor = System.Drawing.SystemColors.Control
		Me.ClientSize = New System.Drawing.Size(1020, 593)
		Me.Controls.Add(Me.cmdExecuteMain)
		Me.Controls.Add(Me.cmdExecuteJob)
		Me.Controls.Add(Me.cmdViewJob)
		Me.Controls.Add(Me.cmdTestConnection)
		Me.Controls.Add(Me.lvwConnections)
		Me.Controls.Add(Me.cmdLoadXML)
		Me.Controls.Add(Me.txtXMLFile)
		Me.Controls.Add(Me.lvwJobs)
		Me.Controls.Add(Me.lblXMLFile)
		Me.Cursor = System.Windows.Forms.Cursors.Default
		Me.Font = New System.Drawing.Font("Arial", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0)
		Me.Location = New System.Drawing.Point(15, 74)
		Me.MaximizeBox = True
		Me.MinimizeBox = True
		Me.Name = "frmETLMain"
		Me.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Text = "Paulo ETL Test Program"
		Me.listViewHelper1.EndInit()
		Me.ResumeLayout(False)
	End Sub
	Sub ReLoadForm(ByVal addEvents As Boolean)
		Form_Load()
		If addEvents Then
			AddHandler MyBase.Closed, AddressOf Me.frmETLMain_Closed
		End If
	End Sub
#End Region
End Class