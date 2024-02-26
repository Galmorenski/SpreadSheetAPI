using MongoDB.Driver;
using SpreadsheetAPI.DAL.Entities;
using SpreadsheetAPI.Models.Enums;

namespace SpreadsheetAPI.DAL
{
    public interface ISpreadSheetService
    {
        public Task<string> AddSpreadSheetAsync(SpreadSheetDAO spreadsheetDAO);
        public Task SetCellValueAsync(string sheetId, string columnName, int rowIndex, string value, SetCellMode mode);
        public Task<SpreadSheetDAO> GetSpreadSheetByIdAsync(string Id);


        
    }
}
