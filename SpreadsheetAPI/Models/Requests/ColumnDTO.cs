using SpreadsheetAPI.Models.Enums;

namespace SpreadsheetAPI.Models.Requests
{
    public class ColumnDTO
    {
        public string Name { get; set; }
        public ValueTypes Type { get; set; }
    }
}
