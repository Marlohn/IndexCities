using Godot;
using System.Globalization;
using System.Reflection;

namespace IndexCities.Game;

public partial class Main
{
    Label? _versionHud;
    ulong _versionHudRefreshAt;
    readonly IReadOnlyDictionary<string, string> _buildIdentity = typeof(Main).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Where(attribute => attribute.Key.StartsWith("CityBuild", StringComparison.Ordinal))
        .ToDictionary(attribute => attribute.Key, attribute => attribute.Value ?? "unknown");

    void BuildVersionHud(Control root)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.SetAnchor(Side.Left, 1);
        panel.SetAnchor(Side.Right, 1);
        panel.SetAnchor(Side.Top, 1);
        panel.SetAnchor(Side.Bottom, 1);
        panel.OffsetLeft = -640;
        panel.OffsetRight = -16;
        panel.OffsetTop = -15;
        panel.OffsetBottom = -1;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(.08f, .13f, .14f, .76f),
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        });
        _versionHud = new Label
        {
            MouseFilter = Control.MouseFilterEnum.Pass,
            AutowrapMode = TextServer.AutowrapMode.Off,
            ClipText = true,
            Text = ""
        };
        _versionHud.AddThemeFontSizeOverride("font_size", 12);
        _versionHud.AddThemeColorOverride("font_color", new Color("deeedc"));
        _versionHud.TooltipText = BuildIdentityTooltip();
        panel.AddChild(_versionHud);
        root.AddChild(panel);
        UpdateVersionHud(true);
    }

    void UpdateVersionHud(bool force = false)
    {
        if (_versionHud == null) return;
        var now = Time.GetTicksMsec();
        if (!force && now < _versionHudRefreshAt) return;
        _versionHudRefreshAt = now + 250;
        var branch = BuildMetadata("CityBuildBranch");
        var commit = BuildMetadata("CityBuildCommit");
        var shortCommit = commit.Length > 8 ? commit[..8] : commit;
        var dirty = BuildMetadata("CityBuildDirty") == "true";
        var localState = dirty ? "alterações locais" : "";
        var fps = Engine.GetFramesPerSecond().ToString("N0", CultureInfo.InvariantCulture);
        _versionHud.Text = $"{fps} FPS · Versão {shortCommit} · {branch}" + (dirty ? " · " + localState : "");
    }

    string BuildIdentityTooltip()
    {
        var branch = BuildMetadata("CityBuildBranch");
        var commit = BuildMetadata("CityBuildCommit");
        var dirty = BuildMetadata("CityBuildDirty") == "true";
        var trackedFiles = BuildMetadata("CityBuildTrackedChanges");
        var untrackedFiles = BuildMetadata("CityBuildUntrackedChanges");
        var built = BuildMetadata("CityBuildUtc");
        return $"Compilação em execução\nBranch: {branch}\nCommit: {commit}\nEstado local: {(dirty ? $"alterações versionadas: {trackedFiles}; novas fontes/assets: {untrackedFiles}" : "sem alterações")}\nCompilado: {built}";
    }

    string BuildMetadata(string key) => _buildIdentity.GetValueOrDefault(key, "unknown");
}
