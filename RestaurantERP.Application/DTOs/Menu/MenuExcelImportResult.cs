namespace RestaurantERP.Application.DTOs.Menu;

public class MenuExcelImportResult
{
    public int CategoriesCreated { get; set; }
    public int CategoriesUpdated { get; set; }
    public int ItemsCreated { get; set; }
    public int ItemsUpdated { get; set; }
    public int RowsSkipped { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Messages { get; set; } = [];
    public bool HasErrors => Errors.Count > 0;
    public int TotalProcessed => ItemsCreated + ItemsUpdated;
}
