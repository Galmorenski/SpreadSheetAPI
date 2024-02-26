namespace SpreadsheetAPI.DAL.Entities
{
    public class CellDAO
    {
        public string Value { get; set; }

        public string RefPosition { get; set; }
        public bool isRoot { get; set; }
    }
}
