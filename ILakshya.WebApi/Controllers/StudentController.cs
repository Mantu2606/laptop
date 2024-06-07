using AutoMapper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ILakshya.Dal;
using ILakshya.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ILakshya.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly ICommonRepository<Student> _studentRepository;
        private readonly WebPocHubDbContext _dbContext;

        public StudentController(WebPocHubDbContext dbContext, ICommonRepository<Student> repository, IMapper mapper)
        {
            _dbContext = dbContext;
            _studentRepository = repository;
        }

        // Excel file upload endpoint

        [HttpPost("UploadExcel")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var students = new List<Student>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // Reset the stream position to the beginning
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(stream, false))
                {
                    WorkbookPart workbookPart = doc.WorkbookPart;
                    Sheet sheet = workbookPart.Workbook.Sheets.Elements<Sheet>().FirstOrDefault();
                    WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                    SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                    var headers = new List<string>();
                    bool isFirstRow = true;

                    foreach (Row row in sheetData.Elements<Row>())
                    {
                        if (isFirstRow)
                        {
                            // Read headers
                            headers = row.Elements<Cell>().Select(cell => GetCellValue(doc, cell)).ToList();
                            isFirstRow = false;
                            continue;
                        }

                        var student = new Student();
                        var cells = row.Elements<Cell>().ToArray();

                        if (cells.Length < 14) // Ensure there are enough cells
                        {
                            continue; // Skip rows with insufficient data
                        }

                        student.EnrollNo = cells.Length > 0 ? ParseCellValue(cells[0], doc) : null;
                        student.Name = cells.Length > 1 ? GetCellValue(doc, cells[1]) : null;
                        student.FatherName = cells.Length > 2 ? GetCellValue(doc, cells[2]) : null;
                        student.RollNo = cells.Length > 3 ? ParseCellValue(cells[3], doc) : null;
                        student.GenKnowledge = cells.Length > 4 ? ParseCellValue(cells[4], doc) : null;
                        student.Science = cells.Length > 5 ? ParseCellValue(cells[5], doc) : null;
                        student.EnglishI = cells.Length > 6 ? ParseCellValue(cells[6], doc) : null;
                        student.EnglishII = cells.Length > 7 ? ParseCellValue(cells[7], doc) : null;
                        student.HindiI = cells.Length > 8 ? ParseCellValue(cells[8], doc) : null;
                        student.HindiII = cells.Length > 9 ? ParseCellValue(cells[9], doc) : null;
                        student.Computer = cells.Length > 10 ? ParseCellValue(cells[10], doc) : null;
                        student.Sanskrit = cells.Length > 11 ? ParseCellValue(cells[11], doc) : null;
                        student.Mathematics = cells.Length > 12 ? ParseCellValue(cells[12], doc) : null;
                        student.SocialStudies = cells.Length > 13 ? ParseCellValue(cells[13], doc) : null;
                        student.MaxMarks = 5;  // Assuming max marks are 5 for all subjects
                        student.PassMarks = 2; // Assuming pass marks are 2 for all subjects

                        students.Add(student);
                    }
                }
            }

            // Insert students into the database with IDENTITY_INSERT ON
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    await _dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Students ON");

                    foreach (var student in students)
                    {
                        _dbContext.Students.Add(student);
                    }

                    await _dbContext.SaveChangesAsync();
                    await _dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Students OFF");
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            return Ok(students);
        }

        private int? ParseCellValue(Cell cell, SpreadsheetDocument doc)
        {
            string value = GetCellValue(doc, cell);
            if (value == null)
                return null;

            int parsedValue;
            if (int.TryParse(value, out parsedValue))
                return parsedValue;
            else
                return null; // Return null if parsing fails
        }

        private static string GetCellValue(SpreadsheetDocument doc, Cell cell)
        {
            SharedStringTablePart sstPart = doc.WorkbookPart.SharedStringTablePart;
            string value = cell.CellValue?.InnerText;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return sstPart.SharedStringTable.ChildElements[int.Parse(value)].InnerText;
            }
            return value;
        }

        [HttpGet]
        public IEnumerable<Student> Get()
        {
            return _studentRepository.GetAll();
        }

        [HttpGet("{id:int}")]
        public Student GetDetails(int id)
        {
            return _studentRepository.GetDetails(id);
        }
        // it is by enrollNo by me 
        [HttpGet("{enrollNo}")]
        public ActionResult<Student> GetStudentDetailsByEnrollNo(string enrollNo)
        {
            var student = _studentRepository.GetAll().FirstOrDefault(s => s.EnrollNo.ToString() == enrollNo);
            if (student == null)
            {
                return NotFound();
            }
            return Ok(student);
        }



        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult Create(Student student)
        {
            _studentRepository.Insert(student);
            var result = _studentRepository.SaveChanges();
            return result > 0 ? CreatedAtAction("GetDetails", new { id = student.EnrollNo }, student) : (ActionResult)BadRequest();
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult Update(Student student)
        {
            _studentRepository.Update(student);
            var result = _studentRepository.SaveChanges();
            return result > 0 ? NoContent() : (ActionResult)BadRequest();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Student> Delete(int id)
        {
            var student = _studentRepository.GetDetails(id);
            if (student == null) return NotFound();
            _studentRepository.Delete(student);
            _studentRepository.SaveChanges();
            return NoContent();
        }
    }
}




