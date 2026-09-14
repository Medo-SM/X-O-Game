using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace XoGame.Bluetooth;

/// <summary>
/// Establishes Bluetooth RFCOMM (classic) sessions: hosting a game (listener) or
/// joining one (client), plus device discovery. Works on Windows and Linux (BlueZ).
/// </summary>
public static class BluetoothConnection
{
    /// <summary>Fixed service identifier for this game, shared by host and client.</summary>
    public static readonly Guid ServiceUuid = new("e7f2a9b1-4c3d-4f6e-9a8b-1c2d3e4f5a6b");

    /// <summary>Scans for nearby phones/PCs that could be paired for a game.</summary>
    public static async Task<IReadOnlyList<BluetoothDeviceInfo>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        using var client = new BluetoothClient();
        var results = new List<BluetoothDeviceInfo>();

        await foreach (var device in client.DiscoverDevicesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (device.DeviceName is not null)
                results.Add(new BluetoothDeviceInfo(device.DeviceName, device.DeviceAddress.ToString()));
        }

        return results;
    }

    /// <summary>Blocks until someone connects, then returns a live transport (host = X).</summary>
    public static async Task<BluetoothTransport> HostAsync(CancellationToken cancellationToken = default)
    {
        using var listener = new BluetoothListener(ServiceUuid);
        listener.Start();

        try
        {
            var peer = await Task.Run(listener.AcceptBluetoothClient, cancellationToken).ConfigureAwait(false);
            return new BluetoothTransport(peer.GetStream());
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>Connects to a host device by its Bluetooth address (client = O).</summary>
    public static async Task<BluetoothTransport> ConnectAsync(string address, CancellationToken cancellationToken = default)
    {
        var remote = ParseAddress(address);
        var client = new BluetoothClient();

        await Task.Run(() => client.Connect(remote, ServiceUuid), cancellationToken).ConfigureAwait(false);
        return new BluetoothTransport(client.GetStream());
    }

    public static BluetoothAddress ParseAddress(string address)
        => BluetoothAddress.Parse(address);
}