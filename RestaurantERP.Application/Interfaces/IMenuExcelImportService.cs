using RestaurantERP.Application.DTOs.Menu;

namespace RestaurantERP.Application.Interfaces;

public interface IMenuExcelImportService
{
    Task<MenuExcelImportResult> ImportAsync(Stream excelStream, CancellationToken cancellationToken = default);
    byte[] GenerateTemplate();
}
