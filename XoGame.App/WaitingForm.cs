namespace XoGame.App;

/// <summary>A small modal window that runs an async Bluetooth step (host/connect)
/// and closes itself when it finishes. Cancel abandons the operation.</summary>
internal sealed class WaitingForm : Form
{
    private readonly CancellationTokenSource _cts = new();

    public WaitingForm(string message, Func<CancellationToken, Task> work)
    {
        Text = "Tic-Tac-Toe";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(320, 120);

        var label = new Label
        {
            Text = message,
            Dock = DockStyle.Top,
            Height = 70,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10F),
        };

        var cancel = new Button { Text = "Cancel", Dock = DockStyle.Bottom, Height = 40 };
        cancel.Click += (_, _) =>
        {
            _cts.Cancel();
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(cancel);
        Controls.Add(label);

        _ = RunAsync(work);
    }

    private async Task RunAsync(Func<CancellationToken, Task> work)
    {
        Exception? error = null;

        try
        {
            await work(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            error = ex;
        }

        if (!IsHandleCreated)
            return;

        if (error is not null)
            BeginInvoke(() => MessageBox.Show(this, error.Message, "Bluetooth error"));

        BeginInvoke(() =>
        {
            DialogResult = error is null ? DialogResult.OK : DialogResult.Cancel;
            Close();
        });
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _cts.Dispose();
        base.OnFormClosed(e);
    }
}