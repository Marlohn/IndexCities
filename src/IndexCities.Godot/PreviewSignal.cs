using Godot;
namespace IndexCities.Game;

// Pictograms stay readable without relying on a font's symbol coverage.
public partial class PreviewSignal : Control
{
    public enum SignalKind { Position, Road, Money }
    public SignalKind Kind { get; set; }
    bool _good;
    public void SetState(bool good) { if (_good == good) return; _good = good; QueueRedraw(); }
    public override void _Draw()
    {
        var color = new Color(_good ? "b5e4b0" : "ffc47b");
        if (Kind == SignalKind.Money) {
            DrawCircle(new(20, 20), 14, color);
            DrawArc(new(20, 20), 10, 0, Mathf.Tau, 32, new Color("24393a"), 2, true);
            DrawLine(new(20, 13), new(20, 27), new Color("24393a"), 3, true);
            DrawLine(new(16, 17), new(24, 17), new Color("24393a"), 2, true);
            DrawLine(new(16, 23), new(24, 23), new Color("24393a"), 2, true);
        } else if (Kind == SignalKind.Road) {
            DrawLine(new(6, 8), new(6, 32), color, 3, true);
            DrawLine(new(32, 8), new(32, 32), color, 3, true);
            DrawLine(new(19, 8), new(19, 14), color, 2, true);
            DrawLine(new(19, 26), new(19, 32), color, 2, true);
            DrawLine(new(6, 20), new(_good ? 32 : 14, 20), color, 4, true);
            if (!_good) DrawLine(new(24, 20), new(32, 20), color, 4, true);
        } else {
            DrawRect(new Rect2(5, 5, 30, 30), color, false, 2);
            if (_good) { DrawLine(new(11, 20), new(17, 26), color, 4, true); DrawLine(new(17, 26), new(29, 13), color, 4, true); }
            else { DrawLine(new(12, 12), new(28, 28), color, 4, true); DrawLine(new(28, 12), new(12, 28), color, 4, true); }
        }
    }
}
