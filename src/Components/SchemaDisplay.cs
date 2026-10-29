namespace LTOG;

/// <summary>Localize generated metadata descriptions only, without changing parsed index data.</summary>
internal static class SchemaDisplay
{
    public static string Policy(string value) => value switch
    {
        "Permitted" => "允许",
        "Not permitted" => "不允许",
        "Not specified" => "未指定",
        _ => value,
    };

    public static string Location(string value) => value == "Not specified" ? "未指定"
        : value.StartsWith("Partition ", StringComparison.Ordinal)
            ? value.Replace("Partition ", "分区 ").Replace(" starting from block ", "，起始块 ")
            : value;

    public static string Time(string value) => value.Replace(" at ", " ");
}
