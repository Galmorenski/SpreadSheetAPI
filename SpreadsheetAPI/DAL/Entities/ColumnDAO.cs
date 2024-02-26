using SpreadsheetAPI.Models.Enums;
using SpreadsheetAPI.Models.Requests;

namespace SpreadsheetAPI.DAL.Entities
{
    public class ColumnDAO
    {
        public ValueTypes Type { get; set; }
        public string Name { get; set; }
        public List<CellDAO> Cells {get; set;}

        public ColumnDAO(ColumnDTO ColumnDTO) 
        {
            Type = ColumnDTO.Type;
            Name = ColumnDTO.Name;
            Cells = new List<CellDAO>();
        }

        public ColumnDAO()
        {

        }
    }
}
