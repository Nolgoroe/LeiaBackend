using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataObjects
{
    public class CategoriesObject
    {
        [Key]
        public int CategoryObjectId { get; set; }
        public string ObjectName { get; set; }
    }
}
