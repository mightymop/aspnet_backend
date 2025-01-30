using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace backend.Model
{
    public class TestModel
    {
        [Required]
        public string id { get; set; }

        [AllowNull]
        public string data { get; set; }
    }
}
