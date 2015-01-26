<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmSingleStep
#Region "Upgrade Support "
	Private Shared m_vb6FormDefInstance As frmSingleStep
	Private Shared m_InitializingDefInstance As Boolean
	Public Shared Property DefInstance() As frmSingleStep
		Get
			If m_vb6FormDefInstance Is Nothing OrElse m_vb6FormDefInstance.IsDisposed Then
				m_InitializingDefInstance = True
				m_vb6FormDefInstance = CreateInstance()
				m_InitializingDefInstance = False
			End If
			Return m_vb6FormDefInstance
		End Get
		Set(ByVal Value As frmSingleStep)
			m_vb6FormDefInstance = Value
		End Set
	End Property
#End Region
#Region "Windows Form Designer generated code "
	Public Shared Function CreateInstance() As frmSingleStep
		Dim theInstance As frmSingleStep = New frmSingleStep()
		Return theInstance
	End Function
	Private visualControls() As String = New String() {"components", "ToolTipMain", "cmdContinue"}
	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer
	Public ToolTipMain As System.Windows.Forms.ToolTip
	Public WithEvents cmdContinue As System.Windows.Forms.Button
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> _
	 Private Sub InitializeComponent()
		Me.components = New System.ComponentModel.Container()
		Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmSingleStep))
		Me.ToolTipMain = New System.Windows.Forms.ToolTip(Me.components)
		Me.cmdContinue = New System.Windows.Forms.Button()
		Me.SuspendLayout()
		' 
		'cmdContinue
		' 
		Me.cmdContinue.BackColor = System.Drawing.SystemColors.Control
		Me.cmdContinue.Cursor = System.Windows.Forms.Cursors.Default
		Me.cmdContinue.ForeColor = System.Drawing.SystemColors.ControlText
		Me.cmdContinue.Location = New System.Drawing.Point(8, 16)
		Me.cmdContinue.Name = "cmdContinue"
		Me.cmdContinue.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.cmdContinue.Size = New System.Drawing.Size(121, 41)
		Me.cmdContinue.TabIndex = 0
		Me.cmdContinue.Text = "Continue"
		Me.cmdContinue.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
		Me.cmdContinue.UseVisualStyleBackColor = False
		' 
		'frmSingleStep
		' 
		Me.AutoScaleDimensions = New System.Drawing.SizeF(9, 17)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.BackColor = System.Drawing.SystemColors.Control
		Me.ClientSize = New System.Drawing.Size(299, 80)
		Me.Controls.Add(Me.cmdContinue)
		Me.Cursor = System.Windows.Forms.Cursors.Default
		Me.Font = New System.Drawing.Font("Arial", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0)
		Me.Location = New System.Drawing.Point(4, 30)
		Me.MaximizeBox = True
		Me.MinimizeBox = True
		Me.Name = "frmSingleStep"
		Me.RightToLeft = System.Windows.Forms.RightToLeft.No
		Me.Text = "Single Step"
		Me.ResumeLayout(False)
	End Sub
	Sub ReLoadForm(ByVal addEvents As Boolean)
		If addEvents Then
			AddHandler MyBase.Closed, AddressOf Me.frmSingleStep_Closed
		End If
	End Sub
#End Region
End Class