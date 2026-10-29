using System.Collections.ObjectModel;
using System.ComponentModel;
using LTOG.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace LTOG;

public enum MountPhase { Idle, Mounting, Mounted, Unmounting }

/// <summary>One label/value cell on the drive dashboard.</summary>
public sealed record Stat(string Label, string Value);

/// <summary>One row of the raw MAM attribute table.</summary>
public sealed record MamRow(string Id, string Name, string Partition, string Size, string Value, string Hex);

/// <summary>A titled card of stats on the drive dashboard.</summary>
/// <param name="MaxColumns">cells per row; -1 = as many as fit</param>
/// <param name="Tail">more cells after Items, four to a row</param>
public sealed record StatGroup(string Title, IReadOnlyList<Stat> Items, int MaxColumns = -1,
    IReadOnlyList<Stat>? Tail = null)
{
    public IReadOnlyList<Stat> TailItems => Tail ?? [];
}

/// <summary>
/// The view model behind one drive's page: the published drive state, mount phase
/// and options, and the display values and dashboard derived from them.
/// </summary>
public sealed class DriveViewModel : INotifyPropertyChanged
{
    private DriveState _state = null!;
    /// <summary>The drive's latest published state, from <see cref="TapeMonitor"/>.</summary>
    public DriveState State
    {
        get => _state;
        set
        {
            _state = value;
            _driveParams = value.Cartridge?.DriveParams ?? _driveParams;
            _encryption = value.Cartridge?.Encryption ?? _encryption;
            RebuildDashboard();
            Changed();
        }
    }
    public TapeDrive Drive => _state.Drive;
    public string HeaderTitle => $"{Drive.Vendor} {Drive.Product}";

    public Mapping? Mapping { get; set; }          // active mount, if any

    private DriveParams? _driveParams;             // sticky: unreadable while mounted
    private EncryptionInfo? _encryption;           // likewise
    public CartridgeInfo? LastCart => _state.Cartridge;

    private bool _ejecting;
    public bool Ejecting
    {
        get => _ejecting;
        set { _ejecting = value; Changed(); }
    }

    public bool CanEject => OptionsEnabled && LastCart?.State != CartridgeState.NoMedia;

    private MountPhase _phase = MountPhase.Idle;
    public MountPhase Phase
    {
        get => _phase;
        set
        {
            _touchRecord |= value != _phase && value is MountPhase.Mounted or MountPhase.Idle;   // (un)mounted
            _phase = value;
            RebuildDashboard();
            Changed();
        }
    }

    /// <summary>Shown only without a cartridge; the dashboard covers a loaded one.</summary>
    public string CartridgeText => _ejecting ? "正在弹出…" : LastCart?.State switch
    {
        CartridgeState.NoMedia => "未装入磁带",
        CartridgeState.NotReady => "磁带机尚未就绪…",
        CartridgeState.DriveInUse => "磁带机正被其他程序使用",
        _ => "正在读取磁带…",
    };

    /// <summary>Muted drive-status line under the drive name (the cartridge has its own section).</summary>
    public string StatusText => _ejecting ? "正在弹出…" : _phase switch
    {
        MountPhase.Mounting => $"正在挂载到 {Mapping?.Letter}…",
        MountPhase.Mounted => $"已挂载到 {Mapping?.Letter}" + (Mapping?.ReadOnly == true ? "，只读" : ""),
        MountPhase.Unmounting => "正在卸载…",
        _ => LastCart?.State switch
        {
            CartridgeState.NoMedia => "已就绪，未装入磁带",
            CartridgeState.Ltfs or CartridgeState.NotLtfs => "已就绪，已装入磁带",
            CartridgeState.NotReady => "尚未就绪",
            CartridgeState.DriveInUse => "正被其他程序使用",
            _ => "正在检查…",
        },
    };

    public ObservableCollection<string> Letters { get; } = new();

    private int _selectedLetterIndex = -1;
    public int SelectedLetterIndex
    {
        get => _selectedLetterIndex;
        set { _selectedLetterIndex = value; Raise(nameof(SelectedLetterIndex)); }
    }

    public string? SelectedLetter =>
        _selectedLetterIndex >= 0 && _selectedLetterIndex < Letters.Count
            ? Letters[_selectedLetterIndex] : null;

    private bool _readOnly;
    public bool ReadOnlyChecked
    {
        get => _readOnly;
        set { _readOnly = value; Raise(nameof(ReadOnlyChecked)); }
    }

    /// <summary>False when the drive has no usable cartridge; unknown (unreadable) is allowed.</summary>
    public bool CanMount => LastCart?.State is null or CartridgeState.Ltfs or CartridgeState.NotLtfs;

    private bool _globalEnabled = true; // env resolved, no utility running
    public bool GlobalEnabled
    {
        get => _globalEnabled;
        set { _globalEnabled = value; Changed(); }
    }

    // ---- derived UI state --------------------------------------------------

    public bool OptionsEnabled => _phase == MountPhase.Idle && _globalEnabled && !_ejecting;

    public bool ButtonEnabled => _globalEnabled && !_ejecting && _phase switch
    {
        MountPhase.Idle => CanMount && !CartridgeLoading,
        MountPhase.Mounting => true,    // acts as Cancel
        MountPhase.Mounted => !CartridgeLoading,
        _ => false,
    };

    public string ButtonText => _phase switch
    {
        MountPhase.Idle => "挂载",
        MountPhase.Mounting => "正在挂载",
        MountPhase.Mounted => "卸载",
        _ => "正在卸载…",
    };

    /// <summary>
    /// Cancel (mounting) is a neutral/grey button; Mount and Unmount use the
    /// accent (blue) style. A null Style falls back to the implicit default.
    /// </summary>
    public Style? ButtonStyle => _phase == MountPhase.Mounting
        ? null
        : (Style)Application.Current.Resources["AccentButtonStyle"];

    public bool IsBusy => _phase is MountPhase.Mounting or MountPhase.Unmounting;
    public Visibility SpinnerVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;

    // ---- dashboard: display data derived from the drive's state ------------

    private long _capacity, _free;   // bytes
    private string? _sig;
    private TapeRecord? _rec;        // the loaded tape's record, as last saved; null once ejected
    private bool _touchRecord;       // mounted or unmounted: save even if unchanged

    private void SaveRecord(TapeRecord rec, bool ejected = false)
    {
        _rec = ejected ? null : rec;
        _touchRecord = false;
        try { TapeDb.Save(rec); }
        catch (Exception ex) { App.Activity.Note($"recording tape {rec.Uuid} failed: {ex.Message}", isError: true); }
    }

    public bool CartridgeLoading => _ejecting || LastCart?.State is null or CartridgeState.NotReady;
    public bool HasCartridge => !CartridgeLoading && (_phase != MountPhase.Idle ||
                                LastCart?.State is CartridgeState.Ltfs or CartridgeState.NotLtfs);
    public bool NoCartridge => !HasCartridge;
    public bool HasUsage => _capacity > 0;
    public double UsagePercent => _capacity > 0 ? 100.0 * (_capacity - _free) / _capacity : 0;
    public bool UsageCritical => UsagePercent > 90;
    public string UsageText => $"{UsagePercent:0.0}%";
    public Brush UsageBrush => (Brush)Application.Current.Resources[
        UsageCritical ? "SystemFillColorCriticalBrush" : "TextFillColorPrimaryBrush"];
    public string UsedText => SchemaSnapshot.FormatSize(_capacity - _free);
    public string FreeText => SchemaSnapshot.FormatSize(_free);
    public string CapacityText => SchemaSnapshot.FormatSize(_capacity);
    public string UsageSource => _phase == MountPhase.Mounted && _state.Usage != null
        ? "实时数据，来自已挂载的卷"
        : LastCart?.State == CartridgeState.Ltfs ? "来自磁带存储器（数据分区）"
        : "来自磁带存储器（全部分区）";

    public Stat? NameStat { get; private set; }           // full-width tile
    public Stat? ManufacturerStat { get; private set; }   // half-width tiles, under the others
    public Stat? ManufacturedStat { get; private set; }
    public IReadOnlyList<Stat> CartridgeStats { get; private set; } = [];
    public IReadOnlyList<StatGroup> CartridgeGroups { get; private set; } = [];
    public IReadOnlyList<MamRow> MamAttributes { get; private set; } = [];
    public bool HasMamAttributes => MamAttributes.Count > 0;
    public IReadOnlyList<Stat> DriveStats { get; private set; } = [];
    public IReadOnlyList<Stat> DriveIdStats { get; private set; } = [];   // serial + firmware, two columns
    public IReadOnlyList<StatGroup> DriveGroups { get; private set; } = [];


    private void RebuildDashboard()
    {
        var c = LastCart;
        var mam = c?.MamByPartition.FirstOrDefault() ?? new Dictionary<ushort, byte[]>();
        string? T(ushort id) => NativeTape.MamText(mam, id);
        ulong? N(ushort id) => NativeTape.MamNumber(mam, id);
        string? Count(ushort id) => N(id)?.ToString("N0");
        string? MiB(ushort id) => N(id) is { } v ? SchemaSnapshot.FormatSize((long)v << 20) : null;
        string? Bytes(ulong? v) => v is { } b ? SchemaSnapshot.FormatSize((long)b) : null;
        string? Hex(ushort id) => mam.TryGetValue(id, out var v) ? Convert.ToHexString(v) : null;
        string? Date(ushort id) => T(id) is { Length: >= 8 } d
            ? $"{d[..4]}-{d[4..6]}-{d[6..8]}" + (d.Length >= 12 ? $" {d[8..10]}:{d[10..12]}" : "") : T(id);
        string? Loaded(ushort id) => mam.TryGetValue(id, out var v) && v.Length >= 8   // vendor(8) + serial
            && $"{Ascii(v[..8])} {Ascii(v[8..])}".Trim() is { Length: > 0 } s ? s : null;
        static string Ascii(byte[] b) => System.Text.Encoding.ASCII.GetString(b).Trim('\0', ' ');
        static string OnOff(bool b) => b ? "开启" : "关闭";
        static List<Stat> L(params (string Label, string? Value)[] items) =>
            items.Where(i => !string.IsNullOrEmpty(i.Value)).Select(i => new Stat(i.Label, i.Value!)).ToList();
        Stat? Tile(string label, string? value) => HasCartridge ? L((label, value)).FirstOrDefault() : null;

        // per partition (MAM 0x0001 / 0x0000): maximum and remaining capacity, bytes
        var parts = (c?.MamByPartition ?? [])
            .Select(pm => (Cap: NativeTape.MamNumber(pm, 0x0001), Rem: NativeTape.MamNumber(pm, 0x0000) ?? 0))
            .Where(p => p.Cap != null)
            .Select(p => (Cap: (long)p.Cap!.Value << 20, Rem: (long)p.Rem << 20))
            .ToList();

        // capacity: live from a mounted volume, else the per-partition MAM figures, as LTFS reports them
        bool ltfs = c?.State == CartridgeState.Ltfs && parts.Count >= 2;
        (_capacity, _free) = _phase == MountPhase.Mounted && _state.Usage is { } live ? live : (0, 0);
        if (_capacity == 0)
            (_capacity, _free) = ltfs
                ? TapeRecord.LtfsSpace(parts[0].Cap, parts[^1].Cap, parts[^1].Rem)
                : (parts.Sum(p => p.Cap), parts.Sum(p => p.Rem));

        // remember each volume's serial and partitions for the Tapes and Search pages (indexes
        // don't carry them): saved when they change, and when the tape is loaded, mounted,
        // unmounted or ejected, so "as of" is when it was last seen
        var now = DateTime.UtcNow;
        var rec = ltfs && c!.VolumeUuid is { } uuid
            ? new TapeRecord(uuid, T(0x0806), T(0x0401) ?? "", c.LtoGeneration, parts[0].Cap >> 20, parts[0].Rem >> 20,
                parts[^1].Cap >> 20, parts[^1].Rem >> 20, now)
            : null;
        if (rec != null && (_touchRecord || _rec is null || _rec with { SeenUtc = now } != rec))
            SaveRecord(rec);
        else if (rec == null && _rec != null && c?.State == CartridgeState.NoMedia)
            SaveRecord(_rec with { SeenUtc = now }, ejected: true);

        string type = (c?.LtoGeneration ?? "LTO") + N(0x0408) switch
        {
            0x80 => " WORM",
            0x01 => " 清洁带",
            _ => "",
        };
        var name = Tile("名称", c?.VolumeName ?? Mapping?.VolumeName);
        var maker = Tile("制造商", T(0x0400));
        var made = Tile("生产日期", Date(0x0406));
        var cartStats = !HasCartridge ? new List<Stat>() : L(
            ("类型", type),
            ("格式", c?.State == CartridgeState.NotLtfs ? "未格式化"
                : $"LTFS {c?.FormatVersion ?? Mapping?.FormatVersion}".Trim()),
            ("写保护", c == null ? null : c.WriteProtected ? "已启用" : "已禁用"),
            ("序列号", T(0x0401)));

        var groups = !HasCartridge ? new List<StatGroup>() : new List<StatGroup>
        {
            new("标识信息", L(
                ("条码", T(0x0806)),
                ("所属主机", T(0x0807)),
                ("介质池", T(0x0808)),
                ("介质 GUID", Hex(0x0820)))),
            new("使用记录", L(
                ("装载次数", Count(0x0003)),
                ("初始化次数", Count(0x0007)),
                ("累计写入", MiB(0x0220)),
                ("累计读取", MiB(0x0221)),
                ("上次装载写入", MiB(0x0222)),
                ("上次装载读取", MiB(0x0223)),
                ("最后写入时间", Date(0x0804))), MaxColumns: 2,
                Tail: L(
                ("上次装载设备", Loaded(0x020A)),
                ("前 1 次装载设备", Loaded(0x020B)),
                ("前 2 次装载设备", Loaded(0x020C)),
                ("前 3 次装载设备", Loaded(0x020D)))),
            new("物理信息", L(
                ("磁带长度", N(0x0402) is { } len ? $"{len:N0} m" : null),
                ("磁带宽度", N(0x0403) is { } w ? $"{w / 10.0:0.0} mm" : null),
                ("介质密度", N(0x0405) is { } md ? $"0x{md:X2}" : null),
                ("格式化密度", N(0x0006) is { } fd ? $"0x{fd:X2}" : null),
                ("分配组织", T(0x0404) ?? T(0x0005)),
                ("MAM 容量", Bytes(N(0x0407))),
                ("MAM 可用空间", Bytes(N(0x0004))),
                ("磁头位置", c?.Position is { } pos
                    ? (pos.Partition > 0 ? $"分区 {pos.Partition - 1}，" : "") + $"块 {pos.Block:N0}" : null))),
            new("应用程序", L(
                ("厂商", T(0x0800)),
                ("名称", T(0x0801)),
                ("版本", T(0x0802)),
                ("格式版本", T(0x080B)),
                ("介质池 GUID", Hex(0x0821)))),
            new("分区", L(parts.Select((p, i) => ($"分区 {i}",
                (string?)$"可用 {SchemaSnapshot.FormatSize(p.Rem)} / 总容量 {SchemaSnapshot.FormatSize(p.Cap)}")).ToArray()),
                MaxColumns: 2),
        };
        groups.RemoveAll(g => g.Items.Count == 0 && g.TailItems.Count == 0);

        // every attribute of every partition; ids below 0x0400 are per partition,
        // the rest (medium, host) are the same in each, so listed once
        var byPart = c?.MamByPartition ?? [];
        var raw = byPart.SelectMany((pm, part) => pm
                .Where(p => part == 0 || p.Key < 0x0400)
                .Select(p => (Part: part, p.Key, p.Value)))
            .OrderBy(a => a.Key).ThenBy(a => a.Part)
            .Select(a => new MamRow(
                $"0x{a.Key:X4}",
                MamNames.TryGetValue(a.Key, out var n) ? n : "厂商自定义 / 未知",
                a.Key < 0x0400 ? a.Part.ToString() : "全部",
                $"{a.Value.Length:N0} B",
                a.Value.All(b => b is 0 or >= 0x20 and < 0x7F) && a.Value.Any(b => b != 0)
                    ? Ascii(a.Value)
                    : a.Value.Length <= 8 ? $"{a.Value.Aggregate(0UL, (x, b) => x << 8 | b):N0}" : Convert.ToHexString(a.Value),
                Convert.ToHexString(a.Value)))
            .ToList();

        var d = Drive;
        var dp = _driveParams;
        var enc = _encryption;
        var driveStats = L(
            ("厂商", d.Vendor),
            ("型号", d.Product),
            ("代际", d.GenerationText),
            ("接口", d.Bus));
        var driveIdStats = L(
            ("序列号", d.Serial),
            ("固件", d.Revision));
        var driveGroups = new List<StatGroup>
        {
            new("设备", L(
                ("路径", $@"\\.\{d.Device}"),
                ("磁带机编号", $"#{d.Number}"),
                ("最大分区数", dp?.MaximumPartitionCount.ToString()),
                ("带尾预警区", dp == null ? null : $"{dp.EotWarningZoneSize:N0} B"))),
            new("块大小", L(
                ("模式", c?.BlockSize is { } bs ? bs == 0 ? "可变" : $"固定，{bs:N0} B" : null),
                ("最小值", dp == null ? null : $"{dp.MinimumBlockSize:N0} B"),
                ("最大值", dp == null ? null : $"{dp.MaximumBlockSize:N0} B"),
                ("默认值", dp == null ? null : $"{dp.DefaultBlockSize:N0} B"))),
            new("功能", L(
                ("硬件压缩", dp == null ? null : OnOff(dp.Compression)),
                ("ECC", dp == null ? null : OnOff(dp.Ecc)),
                ("加密", enc == null ? null : enc.Algorithms.Count == 0 ? "不支持"
                    : string.Join(", ", enc.Algorithms.Select(a => a == 0x00010014 ? "AES-256-GCM" : $"0x{a:X8}"))),
                ("密钥管理", enc?.ConfigPrevented switch { 1 => "应用程序", 2 => "磁带库 / 系统", _ => null }))),
        };
        driveGroups.RemoveAll(g => g.Items.Count == 0);

        // only hand the repeaters new lists when something changed (keeps selection/scroll)
        string sig = string.Join("\n", new[] { name, maker, made }.OfType<Stat>().Concat(cartStats)
            .Concat(groups.SelectMany(g => g.Items.Concat(g.TailItems)))
            .Concat(driveStats).Concat(driveIdStats).Concat(driveGroups.SelectMany(g => g.Items)).Cast<object>().Concat(raw));
        if (sig == _sig) return;
        _sig = sig;
        (NameStat, ManufacturerStat, ManufacturedStat, CartridgeStats, CartridgeGroups, MamAttributes, DriveStats, DriveIdStats, DriveGroups) =
            (name, maker, made, cartStats, groups, raw, driveStats, driveIdStats, driveGroups);
    }

    /// <summary>SPC-4 / LTO MAM attribute names, for the raw attribute list.</summary>
    private static readonly Dictionary<ushort, string> MamNames = new()
    {
        [0x0000] = "分区剩余容量（MiB）",
        [0x0001] = "分区最大容量（MiB）",
        [0x0002] = "TapeAlert 标志",
        [0x0003] = "装载次数",
        [0x0004] = "MAM 剩余空间（字节）",
        [0x0005] = "分配组织",
        [0x0006] = "格式化密度代码",
        [0x0007] = "初始化次数",
        [0x0008] = "卷标识符",
        [0x0009] = "卷变更引用",
        [0x020A] = "上次装载设备",
        [0x020B] = "前 1 次装载设备",
        [0x020C] = "前 2 次装载设备",
        [0x020D] = "前 3 次装载设备",
        [0x0220] = "介质累计写入量（MiB）",
        [0x0221] = "介质累计读取量（MiB）",
        [0x0222] = "本次 / 上次装载写入量（MiB）",
        [0x0223] = "本次 / 上次装载读取量（MiB）",
        [0x0224] = "首个加密块的逻辑位置",
        [0x0225] = "首个加密块之后首个未加密块的逻辑位置",
        [0x0340] = "介质使用记录",
        [0x0341] = "分区使用记录",
        [0x0400] = "介质制造商",
        [0x0401] = "介质序列号",
        [0x0402] = "介质长度（米）",
        [0x0403] = "介质宽度（0.1 毫米）",
        [0x0404] = "分配组织",
        [0x0405] = "介质密度代码",
        [0x0406] = "介质生产日期",
        [0x0407] = "MAM 容量（字节）",
        [0x0408] = "介质类型",
        [0x0409] = "介质类型信息",
        [0x040A] = "介质数字序列号",
        [0x0800] = "应用程序厂商",
        [0x0801] = "应用程序名称",
        [0x0802] = "应用程序版本",
        [0x0803] = "用户介质文本标签",
        [0x0804] = "最后写入日期和时间",
        [0x0805] = "文本语言标识符",
        [0x0806] = "条码",
        [0x0807] = "所属主机名称",
        [0x0808] = "介质池",
        [0x0809] = "分区用户文本标签",
        [0x080A] = "分区装载 / 卸载信息",
        [0x080B] = "应用程序格式版本",
        [0x080C] = "卷一致性信息",
        [0x0820] = "介质全局唯一标识符",
        [0x0821] = "介质池全局唯一标识符",
    };

    // ---- helpers -----------------------------------------------------------

    /// <summary>Replace the free-letter list, preserving the selection when possible.</summary>
    public void SetLetters(IReadOnlyList<string> letters, string? prefer = null)
    {
        if (prefer == null && letters.SequenceEqual(Letters)) return;   // don't disturb an open dropdown
        string? keep = prefer ?? SelectedLetter;
        Letters.Clear();
        foreach (var l in letters) Letters.Add(l);
        int idx = keep != null ? letters.ToList().IndexOf(keep) : -1;
        if (idx < 0) idx = letters.ToList().IndexOf("T:");
        if (idx < 0 && letters.Count > 0) idx = letters.Count - 1;
        SelectedLetterIndex = idx;
    }

    /// <summary>Lock the letter list to the mounted letter.</summary>
    public void SetMountedLetter(string letter)
    {
        Letters.Clear();
        Letters.Add(letter);
        SelectedLetterIndex = 0;
    }

    /// <summary>Refresh every binding (an empty name means "全部").</summary>
    private void Changed() => Raise(string.Empty);

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
