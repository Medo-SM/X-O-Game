using XoGame.Bluetooth;

namespace XoGame.App;

/// <summary>Lets the player pick which device to join. The scan runs in the background;
/// if nothing is found (or scanning fails) an address can still be typed manually.</summary>
internal sealed class JoinDialog : Form
{
    private readonly ListBox _devices = new();
    private readonly TextBox _address = new();
    private readonly Button _connect = new() { Text = "Connect", Enabled = false };
    private readonly Label _status = new();

    public string? SelectedAddress { get; private set; }
    public string SelectedName { get; private set; } = string.Empty;

    public JoinDialog()
    {
        Text = "Join over Bluetooth";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(380, 300);

        var label = new Label { Text = "Nearby devices:", Location = new Point(12, 12), Size = new Size(356, 20) };

        _devices.Location = new Point(12, 36);
        _devices.Size = new Size(356, 130);

        var manual = new Label { Text = "…or type an address manually:", Location = new Point(12, 178), Size = new Size(356, 20) };

        _address.Location = new Point(12, 200);
        _address.Size = new Size(356, 28);

        _status.Location = new Point(12, 168);
        _status.Size = new Size(356, 0);
        _status.ForeColor = Color.Gray;

        _connect.Location = new Point(200, 244);
        _connect.Size = new Size(90, 36);
        _connect.Click += (_, _) => Finish();

        var cancel = new Button { Text = "Cancel", Location = new Point(294, 244), Size = new Size(74, 36) };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _devices.SelectedIndexChanged += (_, _) => UpdateConnectState();
        _devices.DoubleClick += (_, _) => { if (_connect.Enabled) Finish(); };
        _address.TextChanged += (_, _) => UpdateConnectState();

        Controls.Add(cancel);
        Controls.Add(_connect);
        Controls.Add(_address);
        Controls.Add(_status);
        Controls.Add(manual);
        Controls.Add(_devices);
        Controls.Add(label);

        AcceptButton = _connect;
        CancelButton = cancel;

        _ = LoadDevicesAsync();
    }

    private async Task LoadDevicesAsync()
    {
        _status.Text = "Scanning…";
        try
        {
            var devices = await BluetoothConnection.DiscoverAsync();
            if (!IsHandleCreated)
                return;

            BeginInvoke(() =>
            {
                foreach (var device in devices)
                    _devices.Items.Add(device);

                _status.Text = devices.Count == 0
                    ? "None found - enter the address manually."
                    : $"{devices.Count} device(s) found.";
                _devices.Enabled = devices.Count > 0;
                UpdateConnectState();
            });
        }
        catch (Exception)
        {
            if (!IsHandleCreated)
                return;

            BeginInvoke(() =>
            {
                _status.Text = "Scanning failed - enter the address manually.";
                UpdateConnectState();
            });
        }
    }

    private void UpdateConnectState()
    {
        _connect.Enabled = _devices.SelectedIndex >= 0 || !string.IsNullOrWhiteSpace(_address.Text);
    }

    private void Finish()
    {
        if (_devices.SelectedItem is BluetoothDeviceInfo selected)
        {
            SelectedAddress = selected.Address;
            SelectedName = selected.Name;
        }
        else if (!string.IsNullOrWhiteSpace(_address.Text))
        {
            SelectedAddress = _address.Text.Trim();
            SelectedName = SelectedAddress;
        }
        else
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}