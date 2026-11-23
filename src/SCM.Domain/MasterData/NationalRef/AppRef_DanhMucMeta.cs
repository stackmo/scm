namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Bảng quản trị metadata phiên bản danh mục quốc gia hệ thống (manifest.json)
/// </summary>
public class AppRef_DanhMucMeta
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public string? SourceFile { get; set; }

    public AppRef_DanhMucMeta() { }

    public AppRef_DanhMucMeta(string categoryCode, string categoryName, int totalRows, string? sourceFile = null)
    {
        CategoryCode = categoryCode;
        CategoryName = categoryName;
        TotalRows = totalRows;
        SourceFile = sourceFile;
    }
}
