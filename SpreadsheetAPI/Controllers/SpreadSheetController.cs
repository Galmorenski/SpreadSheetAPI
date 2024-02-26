using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using SpreadsheetAPI.DAL;
using SpreadsheetAPI.DAL.Entities;
using SpreadsheetAPI.Models.Enums;
using SpreadsheetAPI.Models.Requests;
using System.Text.RegularExpressions;

namespace SpreadsheetAPI.Controllers
{
    [Route("api/v1/spreadsheets")]
    public class SpreadSheetController : Controller
    {
        ISpreadSheetService _spreadSheets;
        public SpreadSheetController(ISpreadSheetService spreadSheets)
        {
            _spreadSheets = spreadSheets;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSpreadSheet([FromBody] CreateSpreadSheetDTO spreadSheetDTO)
        {
            if (!CheckDistinctColumnNames(spreadSheetDTO.Columns) || spreadSheetDTO.Columns.Count == 0)
                return BadRequest();
            try
            {
                var spreadSheetDAO = new SpreadSheetDAO(spreadSheetDTO);
                var responseId = await _spreadSheets.AddSpreadSheetAsync(spreadSheetDAO);
                return Ok(responseId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetSpreadSheet(string id)
        {
            if (!ObjectId.TryParse(id, out _))
                return BadRequest("Please enter an objectId");
            try
            {
                var spreadSheet = await _spreadSheets.GetSpreadSheetByIdAsync(id);
                if (spreadSheet == null)
                {
                    return NotFound();
                }
               
                ResolveLookups(spreadSheet);
                return Ok(spreadSheet);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCellValue([FromBody] UpdateCellDTO updateCellDTO)
        {
            var spreadSheet = await _spreadSheets.GetSpreadSheetByIdAsync(updateCellDTO.SheetId);
            if (spreadSheet == null)
            {
                return NotFound("Inexistent sheet id");
            }
            var column = spreadSheet.Columns.FirstOrDefault(col => col.Name == updateCellDTO.ColumnName);
            if (column == null)
            {
                return NotFound("Inexistent Column name");
            }


            if (updateCellDTO.SetCellMode == SetCellMode.Lookup)
            {

                ValidateLookupConvention(updateCellDTO.Value, out string referencedColumnName, out int referencedRowIndex);
                if (referencedColumnName == updateCellDTO.ColumnName && referencedRowIndex == updateCellDTO.Row)
                {
                    return BadRequest("cells can't self-reference");
                }
                Dictionary<string, (ValueTypes, List<CellDAO>)> columnsDictionary = spreadSheet.Columns.ToDictionary(c => c.Name, c => (c.Type, c.Cells));
                if (!ValidateNoCircularReference(updateCellDTO.ColumnName, updateCellDTO.Row, referencedColumnName, referencedRowIndex, columnsDictionary, out ValueTypes valueType))
                {
                    return BadRequest("CircularReferenceDetected");
                }
                if (column.Type != valueType)
                    return BadRequest("Value does not fit column constraints");
                updateCellDTO.Value = $"{referencedColumnName},{referencedRowIndex}";

            }
            else
            {
                var valueType = GetInputValueType(updateCellDTO.Value);

                if (column.Type != valueType)
                    return BadRequest("Value does not fit column constraints");
            }

            await _spreadSheets.SetCellValueAsync(spreadSheet.Id, column.Name, updateCellDTO.Row, updateCellDTO.Value, updateCellDTO.SetCellMode); // could be done with an object to be cleaner
            return Ok($"{spreadSheet.Id} at column: {column.Name} at row {updateCellDTO.Row} was updated to {updateCellDTO.Value}   ");
        }



        #region private methods
        private bool CheckDistinctColumnNames(List<ColumnDTO> columns)
        {
            HashSet<string> names = new();
            foreach (ColumnDTO column in columns)
            {
                if (names.Contains(column.Name))
                    return false;
                names.Add(column.Name);
            }
            return true;
        }

        private ValueTypes GetInputValueType(string value) // can be of types: int, string and bool
        {

            if (value.StartsWith("$"))
            {
                return ValueTypes.String;
            }
            else if (int.TryParse(value, out int intValue))
            {
                return ValueTypes.Integer;
            }
            else if (bool.TryParse(value, out bool boolValue))
            {
                return ValueTypes.Boolean;
            }
            else
            {
                throw new BadHttpRequestException("value type not supported");
            }
        }

        private void ValidateLookupConvention(string value, out string columnName, out int rowIndex)
        {
            string pattern = @"^lookup\((?<ColumnName>[a-zA-Z]+),(?<RowIndex>10|[0-9])\)$";
            Match match = Regex.Match(value, pattern);

            if (match.Success)
            {
                columnName = match.Groups["ColumnName"].Value;
                rowIndex = int.Parse(match.Groups["RowIndex"].Value);
            }
            else
            {
                throw new BadHttpRequestException("Lookup values must follow the given forumla - lookup(columnName,RowIndex)");
            }

        }

        // there's a way to skip the creation of the dictionary (which introduces extra processing) as the columns can be indexed by name, however for this demo the purpose is to reduce travels to DB
        // in an in-memory solution sheets could be created as dictionaries right at the start (which would be sort of equivalent)
        private bool ValidateNoCircularReference(string updatedCellColumnName, int updatedCellRowIndex, string referencedColumnName, int referencedRowIndex, Dictionary<string, (ValueTypes, List<CellDAO>)> columnsDictionary, out ValueTypes valueType)
        {
            var destinationCell = columnsDictionary[referencedColumnName].Item2[referencedRowIndex];
            while (destinationCell.RefPosition != string.Empty)
            {
                var nextCellInRefChain = destinationCell.RefPosition.Split(",");
                referencedColumnName = nextCellInRefChain[0];
                referencedRowIndex = int.Parse(nextCellInRefChain[1]);

                if (referencedColumnName == updatedCellColumnName && referencedRowIndex == updatedCellRowIndex)
                {
                    valueType = ValueTypes.String;
                    return false;
                }
                destinationCell = columnsDictionary[referencedColumnName].Item2[referencedRowIndex];
            }
            valueType = columnsDictionary[referencedColumnName].Item1;
            return true;
        }

        
        private void ResolveLookups(SpreadSheetDAO spreadSheetDAO)
        {
            Dictionary<string, (ValueTypes, List<CellDAO>)> columnsDictionary = spreadSheetDAO.Columns.ToDictionary(c => c.Name, c => (c.Type, c.Cells));

            List<CellDAO> rootCells = new List<CellDAO>();
            foreach (var column in spreadSheetDAO.Columns)
            {
                foreach (var cell in column.Cells)
                {
                    if (cell.isRoot)
                        rootCells.Add(cell);
                }
            }
            foreach(CellDAO cell in rootCells)
            {
                cell.Value = ResolveLookupBranch(columnsDictionary, cell);
            }
        }

        private string ResolveLookupBranch(Dictionary<string, (ValueTypes, List<CellDAO>)> columnsDictionary, CellDAO cell)
        {
            if (cell.RefPosition == string.Empty)
            {
                return cell.Value;
            }
            var nextCellInRefChainData = cell.RefPosition.Split(",");
            var nextReferencedColumnName = nextCellInRefChainData[0];
            var nextReferencedRowIndex = int.Parse(nextCellInRefChainData[1]);
            cell.RefPosition = string.Empty;
            cell.Value = ResolveLookupBranch(columnsDictionary, columnsDictionary[nextReferencedColumnName].Item2[nextReferencedRowIndex]);
            return cell.Value;
        }
        #endregion





    }
}
