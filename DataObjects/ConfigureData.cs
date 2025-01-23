using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    /// <summary>
    /// Configuration table for the application.
    /// Add other types of configurations as needed.
    /// Each column is a different configuration type.
    /// Each row is a different configuration set.
    /// All columns are nullable, so if another set of configurations doesn't need them, they could stay empty.
    /// </summary>
    public class ConfigurationData
    {
        [Key]
        public int ConfigId { get; set; }
        public string? ConfigName { get; set; }
        public string? AppVersion { get; set; }
        public string? BaseURL { get; set; }
        
    }
}
