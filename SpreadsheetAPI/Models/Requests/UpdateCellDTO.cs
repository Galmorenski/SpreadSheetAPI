using SpreadsheetAPI.Models.Enums;

namespace SpreadsheetAPI.Models.Requests
{
    public class UpdateCellDTO
    {
        public string SheetId { get; set; }
        public string ColumnName { get; set;}
        public int Row { get; set; }
        public string Value { get; set; }
        public SetCellMode SetCellMode { get; set; }
    }
}
