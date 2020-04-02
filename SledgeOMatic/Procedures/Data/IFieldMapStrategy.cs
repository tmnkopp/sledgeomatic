﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOM.Procedures.Data
{
    public interface IFieldMapStrategy
    {
        string Wrap  { get; set;  }
        string Execute(SchemaField schemaField);
    }
}