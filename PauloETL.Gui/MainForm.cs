using PauloETL.Connections;
using PauloETL.Engine;
using PauloETL.Models;

namespace PauloETL.Gui;

/// <summary>
/// Main application window — mirrors VB6 frmETLMain.
/// Allows loading XML config, viewing connections/jobs, testing connections, and executing jobs.
/// </summary>
public sealed class MainForm : Form
{
    private readonly TextBox _txtXmlFile;
    private readonly Button _btnLoadXml;
    private readonly Button _btnExecuteMain;
    private readonly ListView _lvwConnections;
    private readonly Button _btnTestConnection;
    private readonly ListView _lvwJobs;
    private readonly Button _btnViewJob;
    private readonly Button _btnExecuteJob;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;

    private EtlEngine? _engine;

    public EtlEngine? Engine => _engine;

    public string SelectedJobId => _lvwJobs.SelectedItems.Count > 0
        ? _lvwJobs.SelectedItems[0].Text
        : "";

    public string SelectedJobName => _lvwJobs.SelectedItems.Count > 0
        ? _lvwJobs.SelectedItems[0].SubItems[1].Text
        : "";

    public MainForm()
    {
        Text = "Paulo ETL";
        ClientSize = new Size(900, 600);
        Font = new Font("Segoe UI", 10F);
        StartPosition = FormStartPosition.CenterScreen;

        // XML file path
        var lblXml = new Label { Text = "XML File Path:", Location = new Point(12, 15), AutoSize = true };
        _txtXmlFile = new TextBox
        {
            Text = @"C:\VB\PauloETL\PauloETL.xml",
            Location = new Point(130, 12),
            Width = 520
        };
        _btnLoadXml = new Button { Text = "Load XML", Location = new Point(660, 10), Width = 100, Height = 30 };
        _btnExecuteMain = new Button { Text = "Execute Main", Location = new Point(770, 10), Width = 110, Height = 30 };

        // Connections list
        var lblConn = new Label { Text = "Connections:", Location = new Point(12, 50), AutoSize = true };
        _lvwConnections = new ListView
        {
            Location = new Point(12, 75),
            Size = new Size(750, 150),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _lvwConnections.Columns.Add("ID", 200);
        _lvwConnections.Columns.Add("Connection Name", 540);

        _btnTestConnection = new Button
        {
            Text = "Test Connection",
            Location = new Point(770, 75),
            Width = 110,
            Height = 35,
            Enabled = false
        };

        // Jobs list
        var lblJobs = new Label { Text = "Jobs:", Location = new Point(12, 235), AutoSize = true };
        _lvwJobs = new ListView
        {
            Location = new Point(12, 260),
            Size = new Size(750, 250),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _lvwJobs.Columns.Add("ID", 200);
        _lvwJobs.Columns.Add("Job Name", 540);

        _btnViewJob = new Button
        {
            Text = "View Job",
            Location = new Point(770, 260),
            Width = 110,
            Height = 35,
            Enabled = false
        };
        _btnExecuteJob = new Button
        {
            Text = "Execute Job",
            Location = new Point(770, 305),
            Width = 110,
            Height = 35,
            Enabled = false
        };

        // Status bar
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready");
        _statusStrip.Items.Add(_statusLabel);

        Controls.AddRange(new Control[]
        {
            lblXml, _txtXmlFile, _btnLoadXml, _btnExecuteMain,
            lblConn, _lvwConnections, _btnTestConnection,
            lblJobs, _lvwJobs, _btnViewJob, _btnExecuteJob,
            _statusStrip
        });

        // Events
        _btnLoadXml.Click += BtnLoadXml_Click;
        _btnExecuteMain.Click += BtnExecuteMain_Click;
        _btnTestConnection.Click += BtnTestConnection_Click;
        _btnViewJob.Click += BtnViewJob_Click;
        _btnExecuteJob.Click += BtnExecuteJob_Click;

        // Resize
        Resize += (_, _) => OnLayoutControls();
    }

    private void OnLayoutControls()
    {
        var rightMargin = ClientSize.Width - 130;
        _txtXmlFile.Width = rightMargin - _btnLoadXml.Width - _btnExecuteMain.Width - 30;
        _btnLoadXml.Left = _txtXmlFile.Right + 10;
        _btnExecuteMain.Left = _btnLoadXml.Right + 10;

        var listWidth = ClientSize.Width - _btnTestConnection.Width - 40;
        _lvwConnections.Width = listWidth;
        _btnTestConnection.Left = listWidth + 20;

        _lvwJobs.Width = listWidth;
        _lvwJobs.Height = ClientSize.Height - _lvwJobs.Top - _statusStrip.Height - 20;
        _btnViewJob.Left = listWidth + 20;
        _btnExecuteJob.Left = listWidth + 20;
    }

    private void BtnLoadXml_Click(object? sender, EventArgs e)
    {
        try
        {
            _engine?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _engine = new EtlEngine();
            _engine.LoadConfig(_txtXmlFile.Text);

            // Populate connections
            _lvwConnections.Items.Clear();
            foreach (var kvp in _engine.Connections)
            {
                var conn = kvp.Value;
                var item = new ListViewItem(conn.Id);
                item.SubItems.Add(conn.Name);
                _lvwConnections.Items.Add(item);
            }

            // Populate jobs
            _lvwJobs.Items.Clear();
            foreach (var job in _engine.Jobs)
            {
                var item = new ListViewItem(job.Id);
                item.SubItems.Add(job.Name);
                _lvwJobs.Items.Add(item);
            }

            _btnLoadXml.Enabled = false;
            _btnTestConnection.Enabled = true;
            _btnViewJob.Enabled = true;
            _btnExecuteJob.Enabled = true;
            _statusLabel.Text = $"Loaded: {_engine.Connections.Count} connections, {_engine.Jobs.Count} jobs";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error Loading XML", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            _engine = null;
            _btnLoadXml.Enabled = true;
            _statusLabel.Text = "Load failed";
        }
    }

    private async void BtnExecuteMain_Click(object? sender, EventArgs e)
    {
        if (_engine == null || !_engine.IsLoaded)
        {
            // Auto-load first
            BtnLoadXml_Click(sender, e);
            if (_engine == null || !_engine.IsLoaded)
                return;
        }

        await ExecuteJobAsync("Main");
    }

    private async void BtnTestConnection_Click(object? sender, EventArgs e)
    {
        if (_engine == null || _lvwConnections.SelectedItems.Count == 0)
        {
            MessageBox.Show("Must select a connection", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        var connId = _lvwConnections.SelectedItems[0].Text;
        _statusLabel.Text = $"Testing connection {connId}...";
        _btnTestConnection.Enabled = false;

        try
        {
            var error = await _engine.TestConnectionAsync(connId);
            if (error == null)
            {
                MessageBox.Show("Connection succeeded", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                _statusLabel.Text = $"Connection {connId} OK";
            }
            else
            {
                MessageBox.Show(error, $"Connection {connId} Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                _statusLabel.Text = $"Connection {connId} failed";
            }
        }
        finally
        {
            _btnTestConnection.Enabled = true;
        }
    }

    private void BtnViewJob_Click(object? sender, EventArgs e)
    {
        if (_engine == null || _lvwJobs.SelectedItems.Count == 0)
        {
            MessageBox.Show("Must select a job", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        var jobDetailForm = new JobDetailForm(this);
        jobDetailForm.Show(this);
    }

    private async void BtnExecuteJob_Click(object? sender, EventArgs e)
    {
        if (_engine == null || _lvwJobs.SelectedItems.Count == 0)
        {
            MessageBox.Show("Must select a job", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        await ExecuteJobAsync(SelectedJobId);
    }

    private async Task ExecuteJobAsync(string jobId)
    {
        if (_engine == null) return;

        SetExecutionMode(true);
        _statusLabel.Text = $"Executing job: {jobId}...";

        try
        {
            var success = await _engine.ExecuteJobAsync(jobId);

            if (success)
            {
                MessageBox.Show("Job completed successfully", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                _statusLabel.Text = $"Job {jobId} completed";
            }
            else
            {
                MessageBox.Show("Job completed with errors. Check the log file for details.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _statusLabel.Text = $"Job {jobId} completed with errors";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Job Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = $"Job {jobId} failed";
        }
        finally
        {
            SetExecutionMode(false);
        }
    }

    private void SetExecutionMode(bool executing)
    {
        _btnExecuteMain.Enabled = !executing;
        _btnExecuteJob.Enabled = !executing;
        _btnViewJob.Enabled = !executing;
        _btnTestConnection.Enabled = !executing;
        _btnLoadXml.Enabled = !executing && !(_engine?.IsLoaded ?? false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _engine?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        base.Dispose(disposing);
    }
}
