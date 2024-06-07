using System.ComponentModel.DataAnnotations;

namespace ILakshya.Model
{
    public class Student
    {
        [Key]
            public int? EnrollNo { get; set; }
            public string Name { get; set; }
            public string FatherName { get; set; }
            public int? RollNo { get; set; }
            public int? GenKnowledge { get; set; }
            public int? Science { get; set; }
            public int? EnglishI { get; set; }
            public int? EnglishII { get; set; }
            public int? HindiI { get; set; }
            public int? HindiII { get; set; }
            public int? Computer { get; set; }
            public int? Sanskrit { get; set; }
            public int? Mathematics { get; set; }
            public int? SocialStudies { get; set; }
            public int? MaxMarks { get; set; }
            public int? PassMarks { get; set; }
        }
    }

