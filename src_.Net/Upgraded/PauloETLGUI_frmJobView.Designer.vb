<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmJobView
#Region "Upgrade Support "
	Private Shared m_vb6FormDefInstance As frmJobView
	Private Shared m_InitializingDefInstance As Boolean
	Public Shared Property DefInstance() As frmJobView
		Get
			If m_vb6FormDefInstance Is Nothing OrElse m_vb6FormDefInstance.IsDisposed Then
				m_InitializingDefInstance = True
				m_vb6FormDefInstance = CreateInstance()
				m_InitializingDefInstance = False
			End If
			Return m_vb6FormDefInstance
		End Get
		Set(ByVal Value As frmJobView)
			m_vb6FormDefInstance = Value
		End Set
	End Property
#End Region
#Region "Windows Form Designer generated code "
	Public Shared Function CreateInstance() As frmJobView
		Dim theInstance As frmJobView = New frmJobView()
		theInstance.Form_Load()
		Return theInstance
	End Function
	Private visualControls() As String = New String() {"components", "ToolTipMain", "chkSingleStep", "cmdExecStep", "lstSteps", "listBoxHelper1"}
	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer
	Public ToolTipMain As System.Windows.Forms.ToolTip
	Public WithEvents chkSingleStep As System.Windows.Forms.CheckBox
	Public WithEvents cmdExecStep As System.Windows.Forms.Button
	Public WithEvents lstSteps As System.Windows.Forms.ListBox
	Private listBoxHelper1 As UpgradeHelpers.Gui.ListBoxHelper
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> _
	 Private Sub InitializeComponent()
		Me.components = New System.ComponentModel.Container()
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmJobView))
		Me.ToolTipMain = New System.Windows.Forms.ToolTip(Me.components)
		Me.chkSingleStep = New System.Windows.Forms.CheckBox()
		Me.cmdExecStep = New System.Windows.Forms.Button()
		Me.lstSteps = New System.Windows.Forms.ListBox()
		Me.SuspendLayout()
		Me.listBoxHelper1 = New UpgradeHelpers.Gui.ListBoxHelper(Me.components)
		' 
		'chkSingleStep
		' 
		Me.chkSingleStep.Appearance = System.Windows.Forms.Appearance.Normal
		Me.chkSingleStep.BackColor = System.Drawing.SystemColors.Control
		Me.chkSingleStep.CausesValidation = True
		Me.chkSingleStep.CheckAlign = System.Drawing.ContentAlignment.MiddleLeft
		Me.chkSingleStep.CheckState = System.Windows.Forms.CheckState.Unchecked
		Me.chkSingleStep.Cursor = System.Windows.Forms.Cursors.Default
		Me.chkSingleStep.Enabled = True
		Me.chkSingleStep.ForeColor = System.Drawing.SystemColors.ControlText
		Me.chkSingleStep.Location = New System.Drawing.Point(184, 248)
		Me.chkSingleStep.Name = "chkSingleStep"
		Me.chkSingleStep.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.chkSingleStep.Size = New System.Drawing.Size(177, 25)
		Me.chkSingleStep.TabIndex = 2
		Me.chkSingleStep.TabStop = True
		Me.chkSingleStep.Text = "Single Step Commands"
		Me.chkSingleStep.Visible = True
		' 
		'cmdExecStep
		' 
		Me.cmdExecStep.BackColor = System.Drawing.SystemColors.Control
		Me.cmdExecStep.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdExecStep.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdExecStep.Location = New System.Drawing.Point(16, 248)
		Me.cmdExecStep.Name = "cmdExecStep"
		Me.cmdExecStep.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdExecStep.Size = New System.Drawing.Size(153, 41)
		Me.cmdExecStep.TabIndex = 1
		Me.cmdExecStep.Text = "Execute Step"
		Me.cmdExecStep.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
		Me.cmdExecStep.UseVisualStyleBackColor = False
		' 
		'lstSteps
		' 
		Me.lstSteps.BackColor = System.Drawing.SystemColors.Window
		Me.lstSteps.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
		Me.lstSteps.CausesValidation = True
		Me.lstSteps.Cursor = System.Windows.Forms.Cursors.Default
		Me.lstSteps.Enabled = True
		Me.lstSteps.ForeColor = System.Drawing.SystemColors.WindowText
		Me.lstSteps.IntegralHeight = True
		Me.lstSteps.Location = New System.Drawing.Point(8, 32)
		Me.lstSteps.MultiColumn = False
		Me.lstSteps.Name = "lstSteps"
		Me.lstSteps.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.lstSteps.Size = New System.Drawing.Size(785, 211)
		Me.lstSteps.Sorted = False
		Me.lstSteps.TabIndex = 0
		Me.lstSteps.TabStop = True
		Me.lstSteps.Visible = True
		' 
		'frmJobView
		' 
		Me.AutoScaleDimensions = New System.Drawing.SizeF(9, 17)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.BackColor = System.Drawing.SystemColors.Control
		Me.ClientSize = New System.Drawing.Size(803, 316)
		Me.Controls.Add(Me.chkSingleStep)
		Me.Controls.Add(Me.cmdExecStep)
		Me.Controls.Add(Me.lstSteps)
		Me.Cursor = System.Windows.Forms.Cursors.Default
		Me.Font = New System.Drawing.Font("Arial", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0)
		Me.Location = New System.Drawing.Point(37, 162)
		Me.MaximizeBox = True
		Me.MinimizeBox = True
		Me.Name = "frmJobView"
		Me.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Text = "Job Detail"
		listBoxHelper1.SetSelectionMode(Me.lstSteps, System.Windows.Forms.SelectionMode.One)
		Me.ResumeLayout(False)
	End Sub
	Sub ReLoadForm(ByVal addEvents As Boolean)
		Form_Load()
		If addEvents Then
			AddHandler MyBase.Closed, AddressOf Me.frmJobView_Closed
		End If
	End Sub
#End Region
End Class