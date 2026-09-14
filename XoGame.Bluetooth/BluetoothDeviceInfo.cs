namespace XoGame.Bluetooth;

/// <summary>A discoverable peer, ready to be handed to <see cref="BluetoothConnection.ConnectAsync"/>.</summary>
public sealed record BluetoothDeviceInfo(string Name, string Address)
{
    public override string ToString() => $"{Name} ({Address})";
}