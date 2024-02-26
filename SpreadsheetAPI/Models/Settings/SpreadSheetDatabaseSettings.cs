namespace SpreadsheetAPI.Models.Settings
{
    public class SpreadSheetDatabaseSettings 
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string SpreadSheetCollectionName { get; set; } = null!;        
    }
}
