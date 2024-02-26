using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SpreadsheetAPI.Models.Requests;

namespace SpreadsheetAPI.DAL.Entities
{
    public class SpreadSheetDAO
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public List<ColumnDAO> Columns { get; set; }

        public SpreadSheetDAO(CreateSpreadSheetDTO createSpreadSheetDTO) 
        {
            Columns = createSpreadSheetDTO.Columns.Select(x => new ColumnDAO(x)).ToList();
        }

        public SpreadSheetDAO()
        {

        }
    }


    
}
