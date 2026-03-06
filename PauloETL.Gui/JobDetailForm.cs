using PauloETL.Engine;
using PauloETL.Models;

namespace PauloETL.Gui;

/// <summary>
/// Job detail window — mirrors VB6 frmJobView.
/// Shows the steps for a selected job and allows executing individual steps.
/// </summary>
public sealed class JobDetailForm : Form
{
    private readonly ListBox _lstSteps;
    private readonly Button _btnExecStep;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;

    private readonly MainForm _mainForm;
    private readonly JobConfig _job;

    public JobDetailForm(MainForm mainForm)
    {
        _mainForm = mainForm;

        var jobId = mainForm.SelectedJobId;
        var jobName = mainForm.SelectedJobName;
        _job = mainForm.Engine!.Jobs.First(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase));

        Text = $"Job Detail: {jobName}";
        ClientSize = new Size(700, 400);
        Font = new Font("Segoe UI", 10F);
        StartPosition = FormStartPosition.CenterParent;

        _lstSteps = new ListBox
        {
            Location = new Point(12, 12),
            Size = new Size(560, 330)
        };

        _btnExecStep = new Button
        {
            Text = "Execute Step",
            Location = new Point(580, 12),
            Width = 105,
            Height = 35
        };

        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Select a step to execute");
        _statusStrip.Items.Add(_statusLabel);

        Controls.AddRange(new Control[] { _lstSteps, _btnExecStep, _statusStrip });

        // Populate steps
        foreach (var step in _job.Steps)
        {
            _lstSteps.Items.Add(step.Name);
        }

        _btnExecStep.Click += BtnExecStep_Click;

        Resize += (_, _) =>
        {
            _lstSteps.Width = ClientSize.Width - _btnExecStep.Width - 30;
            _lstSteps.Height = ClientSize.Height - _statusStrip.Height - 30;
            _btnExecStep.Left = _lstSteps.Right + 8;
        };
    }

    private async void BtnExecStep_Click(object? sender, EventArgs e)
    {
        if (_lstSteps.SelectedIndex < 0)
        {
            MessageBox.Show("Must select a step", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        var stepName = _job.Steps[_lstSteps.SelectedIndex].Name;
        _btnExecStep.Enabled = false;
        _statusLabel.Text = $"Executing: {stepName}...";

        try
        {
            var success = await _mainForm.Engine!.ExecuteStepAsync(_job.Id, stepName);

            if (success)
            {
                MessageBox.Show($"Step '{stepName}' completed successfully",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                _statusLabel.Text = $"Step completed: {stepName}";
            }
            else
            {
                MessageBox.Show($"Step '{stepName}' completed with errors. Check the log file.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _statusLabel.Text = $"Step failed: {stepName}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Step Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = $"Step error: {stepName}";
        }
        finally
        {
            _btnExecStep.Enabled = true;
        }
    }
}
