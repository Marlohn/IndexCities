using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    Label _previewName = null!, _previewCost = null!, _previewCause = null!;
    TextureRect _previewPicture = null!;
    readonly List<(PreviewSignal Icon, Label Label)> _previewSignals = new();
    string _previewVisual = "";

    void BuildPreviewSummary()
    {
        _confirmation.CustomMinimumSize = new(320, 0);
        _confirmation.MouseFilter = Control.MouseFilterEnum.Ignore;
        var header = new HBoxContainer(); _confirmBody.AddChild(header);
        _previewPicture = new TextureRect { CustomMinimumSize = new(72, 64), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
        header.AddChild(_previewPicture);
        var title = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; header.AddChild(title);
        _previewName = Text(title, "", 19); _previewCost = Text(title, "", 23);
        Text(_confirmBody, "Orçamento · materiais, frete e equipe", 12);
        foreach (var kind in Enum.GetValues<PreviewSignal.SignalKind>()) {
            var row = new HBoxContainer(); _confirmBody.AddChild(row);
            var icon = new PreviewSignal { Kind = kind, CustomMinimumSize = new(40, 40) }; row.AddChild(icon);
            _previewSignals.Add((icon, Text(row, "", 15)));
        }
        _previewCause = Text(_confirmBody, "", 13);
        void Ignore(Control control) { control.MouseFilter = Control.MouseFilterEnum.Ignore; foreach (var child in control.GetChildren().OfType<Control>()) Ignore(child); }
        Ignore(_confirmation);
    }

    decimal PreviewAvailable()
    {
        var previous = _movingProject is {} id ? _sim.State.Projects.FirstOrDefault(p => p.Id == id) : null;
        return _sim.Available + (previous?.BudgetRemaining ?? 0) - decimal.Round(previous?.CrewCostDue ?? 0, 2, MidpointRounding.AwayFromZero);
    }

    void RefreshPreviewSummary()
    {
        if (_previewName == null) return;
        _confirmation.Visible = false;
        if (!_hoverPreview || _modal != null || (!_holdLoadedPreview && GetViewport().GuiGetHoveredControl() != null)) return;
        PlacementQuote? quote = null;
        string name, visual, cause;
        decimal total;
        bool valid, connected;
        if (_building is {} kind) {
            name = Catalog.Definitions[kind].Name; visual = CityWorld.Visual(kind);
            if (_dragStart != null && _row.Count >= 2) {
                name += " ×" + _rowValid; total = _rowTotal; valid = _rowValid > 0;
                var roadless = _row.Count(r => r.Quote.PhysicallyValid && !r.Quote.Accessible);
                connected = valid && roadless == 0;
                cause = _row.Count - _rowValid > 0 ? (_row.Count - _rowValid) + " posições inválidas serão puladas." : "Solte para encomendar a fileira.";
                if (roadless > 0) cause = roadless + " obras aguardarão ligação viária. " + cause;
            } else {
                var at = _dragStart ?? _anchor ?? _cursor;
                quote = _movingProject is {} moving ? _sim.QuoteMoveBuilding(moving, at, _rotation) : _sim.QuoteBuilding(kind, at, _rotation);
                total = quote.Total; valid = quote.PhysicallyValid; connected = quote.Accessible; cause = QuoteFeedback(quote);
            }
        } else if (_roadTool && _roadStart is {} start && _roadEnd is {} end) {
            name = "Rua 1+1"; visual = "road.urban_1x1.a.straight";
            quote = _sim.QuoteRoad(start, end, _reverse, _movingProject);
            total = quote.Total; valid = quote.PhysicallyValid; connected = quote.Accessible; cause = QuoteFeedback(quote);
        } else return;
        var available = PreviewAvailable(); var affordable = total <= available;
        _previewName.Text = name; _previewCost.Text = Cash(total);
        _previewCost.AddThemeColorOverride("font_color", new Color(affordable ? "fff1ba" : "ffc47b"));
        if (_previewVisual != visual) { _previewVisual = visual; _previewPicture.Texture = GD.Load<Texture2D>("res://Assets/V1/Thumbnails/" + visual + ".png"); }
        SetPreviewSignal(0, valid, valid ? "Posição válida" : "Posição inválida");
        SetPreviewSignal(1, valid && connected, !valid ? "Confira a posição" : connected ? "Ligação com o exterior" : "Sem ligação · obra parada");
        SetPreviewSignal(2, affordable, affordable ? "Saldo disponível: " + Cash(Math.Max(0, available)) : "Falta " + Cash(total - available));
        if (!affordable && quote == null) cause = "Saldo insuficiente. Nenhuma obra será encomendada. " + cause;
        _previewCause.Text = cause; _confirmation.Visible = true;
    }

    void SetPreviewSignal(int index, bool good, string text)
    {
        var signal = _previewSignals[index]; signal.Icon.SetState(good); signal.Label.Text = text;
        signal.Label.AddThemeColorOverride("font_color", new Color(good ? "deeedc" : "ffc47b"));
    }

    void PreviewBuildingAccess(BuildingKind kind, IEnumerable<(Point At, bool Valid, bool Connected)> items)
    {
        var links = new List<(Point Door, Point Road, bool Connected)>();
        foreach (var item in items.Where(i => i.Valid)) {
            var candidate = new Building { Kind = kind, Position = item.At, Rotation = _rotation };
            if (_sim.Access(candidate) is {} access) links.Add((candidate.Door, access.Position, item.Connected));
        }
        _world.PreviewAccessLinks(links);
    }
}
