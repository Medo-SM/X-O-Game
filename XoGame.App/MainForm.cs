using XoGame.Bluetooth;
using XoGame.Core.Models;
using XoGame.Core.Play;

namespace XoGame.App;

internal sealed class MainForm : Form
{
    public MainForm()
    {
        Text = "Tic-Tac-Toe";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(340, 240);

        var title = new Label
        {
            Text = "Tic-Tac-Toe",
            Dock = DockStyle.Top,
            Height = 50,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var hotseat = MakeButton("Play locally (hotseat)", () => StartHotseat(), Color.Transparent);
        var host = MakeButton("Bluetooth - Host (X)", async () => await HostBluetoothAsync(), Color.LightSkyBlue);
        var join = MakeButton("Bluetooth - Join (O)", async () => await JoinBluetoothAsync(), Color.LightCoral);

        Controls.Add(join);
        Controls.Add(host);
        Controls.Add(hotseat);
        Controls.Add(title);

        AcceptButton = hotseat;
    }

    private static Button MakeButton(string text, Action onClick, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = backColor,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11F),
            Cursor = Cursors.Hand,
        };
        button.Click += (_, _) => onClick();
        return button;
    }

    private void StartHotseat()
    {
        var session = new GameSession(new LocalPlayer(Symbol.X), new LocalPlayer(Symbol.O));
        ShowGame(session, "Hotseat");
    }

    private async Task HostBluetoothAsync()
    {
        BluetoothTransport? transport = null;

        var wait = new WaitingForm(
            "Waiting for the other device to connect…",
            async ct => transport = await BluetoothConnection.HostAsync(ct));

        if (wait.ShowDialog(this) != DialogResult.OK || transport is null)
            return;

        var session = new GameSession(
            new LocalPlayer(Symbol.X),
            new RemotePlayer(Symbol.O, transport),
            transport,
            Symbol.X);

        ShowGame(session, "Bluetooth - Host (X)");
    }

    private async Task JoinBluetoothAsync()
    {
        using var join = new JoinDialog();
        if (join.ShowDialog(this) != DialogResult.OK || join.SelectedAddress is not { } address)
            return;

        BluetoothTransport? transport = null;

        var wait = new WaitingForm(
            $"Connecting to {join.SelectedName}…",
            async ct => transport = await BluetoothConnection.ConnectAsync(address, ct));

        if (wait.ShowDialog(this) != DialogResult.OK || transport is null)
            return;

        var session = new GameSession(
            new RemotePlayer(Symbol.X, transport),
            new LocalPlayer(Symbol.O),
            transport,
            Symbol.O);

        ShowGame(session, "Bluetooth - Join (O)");
    }

    private void ShowGame(GameSession session, string title)
    {
        using var form = new GameForm(session, title);
        form.ShowDialog(this);
    }
}