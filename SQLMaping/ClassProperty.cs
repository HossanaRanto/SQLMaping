using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLMaping
{
    public class ClassProperty : Attribute
    {
        public string[] DefaultProperties { get; set; }
        public ClassProperty(params string[] property)
        {
            this.DefaultProperties = property;
        }
    }
}

