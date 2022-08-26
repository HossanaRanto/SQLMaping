using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLMaping
{
    public class FieldProperty : Attribute
    {
        public bool OnlyInSelect { get; set; } = false;
        public bool ForeignObject { get; set; } = false;
        public bool ForeignOnlyObject { get; set; }
        public string ForeignColumn { get; set; }
        public int MaxValue { get; set; }
        public Type ForeignSourceType { get; set; }
        public string ForeignFrom { get; set; }
        public FieldProperty() { }
    }
}

