namespace SCM.Domain.MasterData.NationalRef;

/// <summary>
/// Danh mục Phân loại Thể lực & Sức khỏe theo Thông tư 32/2023/TT-BYT (1016: Loại I -> 1020: Loại V)
/// </summary>
public class AppRef_PhanLoaiSucKhoe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rank { get; set; }

    public AppRef_PhanLoaiSucKhoe() { }

    public AppRef_PhanLoaiSucKhoe(int id, string name, int rank)
    {
        Id = id;
        Name = name;
        Rank = rank;
    }
}
