using XoGame.Core.Play;

namespace XoGame.App;

internal sealed class GameForm : Form
{
    private readonly Button[] _cells = new Button[9];
    private readonly Label _status = new();
    private readonly GameController _controller;

    public GameForm(GameSession session, string title)
    {
        Text = $"Tic-Tac-Toe - {title}";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(320, 400);

        _status.Text = "Starting…";
        _status.Location = new Point(10, 10);
        _status.Size = new Size(300, 50);
        _status.TextAlign = ContentAlignment.MiddleCenter;
        _status.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        Controls.Add(_status);

        const int size = 66;
        const int gap = 8;
        const int originX = 53;
        const int originY = 70;

        for (var i = 0; i < 9; i++)
        {
            var cell = new Button
            {
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false,
                Location = new Point(originX + (i % 3) * (size + gap), originY + (i / 3) * (size + gap)),
                Size = new Size(size, size),
            };
            var index = i;
            cell.Click += (_, _) => _controller?.SubmitCell(index);
            _cells[i] = cell;
            Controls.Add(cell);
        }

        var newGame = new Button
        {
            Text = "New game",
            Location = new Point(53, 330),
            Size = new Size(100, 40),
            Cursor = Cursors.Hand,
        };
        newGame.Click += (_, _) => _controller?.ResetGame();
        Controls.Add(newGame);

        var menu = new Button
        {
            Text = "Menu",
            Location = new Point(167, 330),
            Size = new Size(100, 40),
            Cursor = Cursors.Hand,
        };
        menu.Click += (_, _) => Close();
        Controls.Add(menu);

        _controller = new GameController(this, session);
        FormClosed += (_, _) => _controller.Dispose();
    }

    /// <summary>Called from the engine's thread via the controller; applies a fresh board state.</summary>
    public void ApplyState(string[] cellTexts, bool enabled, string statusText)
    {
        for (var i = 0; i < _cells.Length; i++)
        {
            _cells[i].Text = cellTexts[i];
            _cells[i].Enabled = enabled;
            _cells[i].ForeColor = cellTexts[i] is "X" or "O" ? Color.Black : Color.Gray;
        }

        _status.Text = statusText;
    }
}