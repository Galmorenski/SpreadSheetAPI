using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Moq;
using SpreadsheetAPI.Controllers;
using SpreadsheetAPI.DAL;
using SpreadsheetAPI.DAL.Entities;
using SpreadsheetAPI.Models.Enums;
using SpreadsheetAPI.Models.Requests;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spreadsheet.Tests
{
    public class SpreadSheetControllerTests : IDisposable
    {
        private readonly Mock<ISpreadSheetService> _spreadSheetServiceMock;
        private readonly SpreadSheetController _spreadSheetController;
        public SpreadSheetControllerTests()
        {
            _spreadSheetServiceMock = new Mock<ISpreadSheetService>();
            _spreadSheetController = new SpreadSheetController(_spreadSheetServiceMock.Object);
        }

        public void Dispose()
        {
            _spreadSheetServiceMock.Invocations.Clear();
        }


        [Fact]
        public async Task CreateSpreadSheet_WithNoColumns_ShouldReturnBadRequest()
        {
            var fixture = new Fixture();
            var spreadSheet = fixture.Build<CreateSpreadSheetDTO>()
                .With(x => x.Columns, new List<ColumnDTO>())
                .Create();

            var result = await _spreadSheetController.CreateSpreadSheet(spreadSheet);

            Assert.IsType<BadRequestResult>(result);
        }
        [Fact]
        public async Task CreateSpreadSheet_WithNonUniqueColumnsNames_ShouldReturnBadRequest()
        {
            var fixture = new Fixture();
            string repeatedName = fixture.Create<string>();

            fixture.Customize<ColumnDTO>(c =>
            c.With(x => x.Name, repeatedName));

            var spreadSheet = fixture.Create<CreateSpreadSheetDTO>();

            var result = await _spreadSheetController.CreateSpreadSheet(spreadSheet);

            Assert.IsType<BadRequestResult>(result);
        }

        [Theory]
        [AutoData]
        public async Task CreateSpreadSheet_Successfully_ShouldReturnOkAndSheetId(CreateSpreadSheetDTO createSpreadSheetDTO)
        {
            var fixture = new Fixture();
            string sheetId = ObjectId.GenerateNewId().ToString();

            _spreadSheetServiceMock.Setup(x => x.AddSpreadSheetAsync(It.IsAny<SpreadSheetDAO>())).Returns(Task.FromResult(sheetId));

            var result = await _spreadSheetController.CreateSpreadSheet(createSpreadSheetDTO);

            var objectResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(sheetId, objectResult.Value);
        }

        [Fact]
        public async Task GetSpreadSheet_WithAnImproperId_ShouldReturnBadRequest()
        {
            var fixture = new Fixture();
            string improperId = fixture.Create<string>(); // will create a string in form of GUID

            var result = await _spreadSheetController.GetSpreadSheet(improperId);

            Assert.IsType<BadRequestObjectResult>(result);
        }


        [Fact]
        public async Task GetSpreadSheet_WithNonExistingId_ShouldReturnNotFound()
        {
            var fixture = new Fixture();
            string sheetId = ObjectId.GenerateNewId().ToString();
            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult<SpreadSheetDAO>(null));

            var result = await _spreadSheetController.GetSpreadSheet(sheetId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        // probably the most complex part to properly test, for this demo it'll be using
        // simple dummy values and see that they've been resolved
        public async Task GetSpreadSheet_WithExistingId_ShouldReturnOk()
        {
            var fixture = new Fixture();
            string sheetId = ObjectId.GenerateNewId().ToString();
            var value = fixture.Create<string>();
            var columnName = fixture.Create<string>();

            var HeadCell = new CellDAO() { Value = value, RefPosition = string.Empty };
            var nodeCell = new CellDAO() { RefPosition = $"{columnName},0" };
            var rootCell = new CellDAO() { RefPosition = $"{columnName},1", isRoot = true };
            var rootCell2 = new CellDAO() { RefPosition = $"{columnName},1", isRoot = true };

            var column = new ColumnDAO() { Name = columnName, Type = ValueTypes.String, Cells = new List<CellDAO>() { HeadCell, nodeCell, rootCell, rootCell2 } };

            var spreadsheet = new SpreadSheetDAO() { Id = sheetId, Columns = new List<ColumnDAO>() { column } };
            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>())).ReturnsAsync(spreadsheet);

            var result = await _spreadSheetController.GetSpreadSheet(sheetId);

            var objectResult = Assert.IsType<OkObjectResult>(result);
            var resultSheet = objectResult.Value as SpreadSheetDAO;

            resultSheet.Columns[0].Cells.ForEach(cell =>
            {
                Assert.Equal(cell.Value, value);
            });

            Assert.Equal(objectResult.StatusCode, 200);





        }

        [Theory]
        [AutoData]
        public async Task UpdateCellValue_WithNonExistingSpreadSheet_ShouldReturnNotFound(UpdateCellDTO updateCellDTO)
        {
            var fixture = new Fixture();

            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult<SpreadSheetDAO>(null));

            var result = await _spreadSheetController.UpdateCellValue(updateCellDTO);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCellValue_WithNonExistingColumn_ShouldReturnNotFound()
        {
            var fixture = new Fixture();

            var updateCellDto = fixture.Build<UpdateCellDTO>()
                .With(x => x.ColumnName, fixture.Create<string>())
                .Create();


            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(fixture.Create<SpreadSheetDAO>()));



            var result = await _spreadSheetController.UpdateCellValue(updateCellDto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCellValue_WithViolationInLookUpConvention_ShouldThrowBadHttpRequest()
        {

            var fixture = new Fixture();
            var columnName = fixture.Create<string>();

            var updateCellDto = fixture.Build<UpdateCellDTO>()
                .With(x => x.ColumnName, columnName)
                .With(x => x.SetCellMode, SetCellMode.Lookup)
                .Create();

            var spreadSheetDAO = fixture.Build<SpreadSheetDAO>()
                .With(x => x.Columns, new List<ColumnDAO>() { new ColumnDAO() { Name = columnName } })
                .Create();


            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(spreadSheetDAO));


            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() => _spreadSheetController.UpdateCellValue(updateCellDto));
        }

        [Fact]
        public async Task UpdateCellValue_WithSelfReference_ShouldReturnBadRequest()
        {
            var fixture = new Fixture();
            var columnName = fixture.Create<char>().ToString();

            var updateCellDto = fixture.Build<UpdateCellDTO>()
                .With(x => x.ColumnName, columnName)
                .With(x => x.Row, 1)
                .With(x => x.Value, $"lookup({columnName},1)")
                .With(x => x.SetCellMode, SetCellMode.Lookup)
                .Create();

            var spreadSheetDAO = fixture.Build<SpreadSheetDAO>()
                .With(x => x.Columns, new List<ColumnDAO>() { new ColumnDAO() { Name = columnName } })
                .Create();

            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>()))
             .Returns(Task.FromResult(spreadSheetDAO));

            var result = await _spreadSheetController.UpdateCellValue(updateCellDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCellValue_CircularReference_ShouldReturnBadRequest()
        {
            var fixture = new Fixture();
            var columnName = fixture.Create<char>().ToString();

            var updateCellDto = fixture.Build<UpdateCellDTO>()
                .With(x => x.ColumnName, columnName)
                .With(x => x.Row, 1)
                .With(x => x.Value, $"lookup({columnName},0)")
                .With(x => x.SetCellMode, SetCellMode.Lookup)
                .Create();


            var circularReferenceCell = new CellDAO() { RefPosition = $"{columnName},1" };


            var spreadSheetDAO = fixture.Build<SpreadSheetDAO>()
                .With(x => x.Columns, new List<ColumnDAO>() { new ColumnDAO() {
                    Name = columnName,
                    Cells = new List<CellDAO>() {circularReferenceCell}}
                })
                .Create();

            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>()))
             .Returns(Task.FromResult(spreadSheetDAO));

            var result = await _spreadSheetController.UpdateCellValue(updateCellDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }
        [Fact]
        public async Task UpdateCellValue_WithLookupToWrongColumnType_ShouldReturnBadRequest()
        {
            var fixture = new Fixture();
            var stringColumnName = fixture.Create<char>().ToString();
            var booleanColumnName = fixture.Create<char>().ToString();


            var booleanColumn = new ColumnDAO()
            {
                Name = booleanColumnName,
                Type = ValueTypes.Boolean,
                Cells = new List<CellDAO>() { new CellDAO() { Value = "true", RefPosition = string.Empty } }
            };

            var stringColumn = new ColumnDAO()
            {
                Name = stringColumnName,
                Type = ValueTypes.String,
                Cells = new List<CellDAO>() { new CellDAO() { Value = "value", RefPosition = string.Empty } }
            };

            var spreadSheetDAO = fixture.Build<SpreadSheetDAO>()
           .With(x => x.Columns, new List<ColumnDAO>() { booleanColumn, stringColumn })
           .Create();

            var updateCellDto = fixture.Build<UpdateCellDTO>()
                .With(x => x.ColumnName, booleanColumnName)
                .With(x => x.Row, 1)
                .With(x => x.Value, $"lookup({stringColumnName},0)")
                .With(x => x.SetCellMode, SetCellMode.Lookup)
                .Create();



            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>()))
             .Returns(Task.FromResult(spreadSheetDAO));

            var result = await _spreadSheetController.UpdateCellValue(updateCellDto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCellValue_WithUnfittingValue_ShouldReturnBadRequest()
        {
            var fixture = new Fixture();
            var stringColumnName = fixture.Create<char>().ToString();
                     
            var stringColumn = new ColumnDAO()
            {
                Name = stringColumnName,
                Type = ValueTypes.String,
                Cells = new List<CellDAO>() { new CellDAO() { Value = "value", RefPosition = string.Empty } }
            };

            var spreadSheetDAO = fixture.Build<SpreadSheetDAO>()
           .With(x => x.Columns, new List<ColumnDAO>() { stringColumn })
           .Create();

            var updateCellDto = fixture.Build<UpdateCellDTO>()
                .With(x => x.ColumnName, stringColumnName)
                .With(x => x.Row, 1)
                .With(x => x.Value, "true")
                .With(x => x.SetCellMode, SetCellMode.Value)
                .Create();



            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>()))
             .Returns(Task.FromResult(spreadSheetDAO));

            var result = await _spreadSheetController.UpdateCellValue(updateCellDto);

            Assert.IsType<BadRequestObjectResult>(result);

        }

        [Fact]
        public async Task UpdateCellValue_Success_ShouldReturnOk()
        {
            var fixture = new Fixture();
            var stringColumnName = fixture.Create<char>().ToString();

            var stringColumn = new ColumnDAO()
            {
                Name = stringColumnName,
                Type = ValueTypes.String,
                Cells = new List<CellDAO>() { new CellDAO() { Value = "value", RefPosition = string.Empty } }
            };

            var spreadSheetDAO = fixture.Build<SpreadSheetDAO>()
           .With(x => x.Columns, new List<ColumnDAO>() { stringColumn })
           .Create();

            var updateCellDto = fixture.Build<UpdateCellDTO>()
                .With(x => x.ColumnName, stringColumnName)
                .With(x => x.Row, 1)
                .With(x => x.Value, "$randomstring")
                .With(x => x.SetCellMode, SetCellMode.Value)
                .Create();



            _spreadSheetServiceMock.Setup(x => x.GetSpreadSheetByIdAsync(It.IsAny<string>()))
             .Returns(Task.FromResult(spreadSheetDAO));

            var result = await _spreadSheetController.UpdateCellValue(updateCellDto);

            Assert.IsType<OkObjectResult>(result);

        }
    }
}
