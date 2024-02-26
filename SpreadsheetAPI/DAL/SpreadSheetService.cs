using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SpreadsheetAPI.DAL.Entities;
using SpreadsheetAPI.Models.Enums;
using SpreadsheetAPI.Models.Settings;
using System.Text;
using ZstdSharp.Unsafe;

namespace SpreadsheetAPI.DAL
{
    public class SpreadSheetService : ISpreadSheetService
    {
        private readonly IMongoCollection<SpreadSheetDAO> spreadSheets;

        public SpreadSheetService(IOptions<SpreadSheetDatabaseSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);

            spreadSheets = database.GetCollection<SpreadSheetDAO>(settings.Value.SpreadSheetCollectionName);
          
           
        }

        public async Task<string> AddSpreadSheetAsync(SpreadSheetDAO spreadsheetDAO)
        {
            spreadsheetDAO.Columns.ForEach((col) =>
            {
                for (int i = 0; i < 10; i++) // dumb ways of settings boundries for testing purposes, could be configured in a prettier manner
                    col.Cells.Add(new CellDAO());
            });
            await spreadSheets.InsertOneAsync(spreadsheetDAO);
            return spreadsheetDAO.Id;

        }

        public async Task<SpreadSheetDAO> GetSpreadSheetByIdAsync(string id)
        {
            return await spreadSheets.Find(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task SetCellValueAsync(string sheetId, string columnName, int rowIndex, string value, SetCellMode mode) // could be done better using Builders.Filter and UpdateOneAsync
        {
            var spreadsheet = await spreadSheets.Find(s => s.Id == sheetId).FirstOrDefaultAsync();
            var column = spreadsheet.Columns.FirstOrDefault(c => c.Name == columnName);   
            UpdateCell(column.Cells[rowIndex], value, mode);
            if (mode == SetCellMode.Lookup)
            {
                var referencedCell = value.Split(",");
                var referencedColumn = spreadsheet.Columns.FirstOrDefault(c => c.Name == referencedCell[0]);
                referencedColumn.Cells[int.Parse(referencedCell[1])].isRoot = false;
            }
            await spreadSheets.ReplaceOneAsync(s => s.Id == sheetId, spreadsheet);
        }

        private void UpdateCell(CellDAO cell, string value, SetCellMode type)
        {
            if (type == SetCellMode.Lookup)
            {
                cell.Value = string.Empty;
                cell.RefPosition = value;
                cell.isRoot = true;
            }
            else
            {
                cell.Value = value;
                cell.RefPosition = string.Empty;
                cell.isRoot = false;
            }
        }



    }
}
