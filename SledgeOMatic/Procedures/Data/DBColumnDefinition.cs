﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOM.Procedures.Data
{
    public class DBColumnDefinition
    {
        public string COLUMN_NAME { get; set; }
        public string DATA_TYPE { get; set; }
        public int ORDINAL_POSITION { get; set; } 
        public bool Nullable { get; set; }
        public int MaxLen { get; set; }
        public bool IsPkey() {
            return this.ORDINAL_POSITION == 1;
        }
    }
}
