<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class frmETLCommand
#Region "Upgrade Support "
	Private Shared m_vb6FormDefInstance As frmETLCommand
	Private Shared m_InitializingDefInstance As Boolean
	Public Shared Property DefInstance() As frmETLCommand
		Get
			If m_vb6FormDefInstance Is Nothing OrElse m_vb6FormDefInstance.IsDisposed Then
				m_InitializingDefInstance = True
				m_vb6FormDefInstance = CreateInstance()
				m_InitializingDefInstance = False
			End If
			Return m_vb6FormDefInstance
		End Get
		Set(ByVal Value As frmETLCommand)
			m_vb6FormDefInstance = Value
		End Set
	End Property
#End Region
#Region "Windows Form Designer generated code "
	Public Shared Function CreateInstance() As frmETLCommand
		Dim theInstance As frmETLCommand = New frmETLCommand()
		Return theInstance
	End Function
	Private visualControls() As String = New String() {"components", "ToolTipMain", "DataGrid1_Column00", "DataGrid1_Column01", "DataGrid1"}
	'Required by the Windows Form Designer
	Private components As System.ComponentModel.IContainer
	Public ToolTipMain As System.Windows.Forms.ToolTip
	Public WithEvents DataGrid1_Column00 As System.Windows.Forms.DataGridViewTextBoxColumn
	Public WithEvents DataGrid1_Column01 As System.Windows.Forms.DataGridViewTextBoxColumn
	Public WithEvents DataGrid1 As System.Windows.Forms.DataGridView
	'NOTE: The following procedure is required by the Windows Form Designer
	'It can be modified using the Windows Form Designer.
	'Do not modify it using the code editor.
	<System.Diagnostics.DebuggerStepThrough()> _
	 Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.ToolTipMain = New System.Windows.Forms.ToolTip(Me.components)
        Me.DataGrid1 = New System.Windows.Forms.DataGridView()
        Me.DataGrid1_Column00 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DataGrid1_Column01 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        CType(Me.DataGrid1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'DataGrid1
        '
        Me.DataGrid1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.DataGrid1.Location = New System.Drawing.Point(8, 24)
        Me.DataGrid1.Name = "DataGrid1"
        Me.DataGrid1.Size = New System.Drawing.Size(625, 249)
        Me.DataGrid1.TabIndex = 0
        '
        'DataGrid1_Column00
        '
        Me.DataGrid1_Column00.HeaderText = ""
        Me.DataGrid1_Column00.Name = "DataGrid1_Column00"
        '
        'DataGrid1_Column01
        '
        Me.DataGrid1_Column01.HeaderText = ""
        Me.DataGrid1_Column01.Name = "DataGrid1_Column01"
        '
        'frmETLCommand
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 17.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.Control
        Me.ClientSize = New System.Drawing.Size(644, 318)
        Me.Controls.Add(Me.DataGrid1)
        Me.Cursor = System.Windows.Forms.Cursors.Default
        Me.Font = New System.Drawing.Font("Arial", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Location = New System.Drawing.Point(4, 30)
        Me.Name = "frmETLCommand"
        Me.RightToLeft = System.Windows.Forms.RightToLeft.No
        Me.Text = "ETLCommand View"
        CType(Me.DataGrid1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
	Sub ReLoadForm(ByVal addEvents As Boolean)
		If addEvents Then
			AddHandler MyBase.Closed, AddressOf Me.frmETLCommand_Closed
		End If
	End Sub
#End Region
End Class