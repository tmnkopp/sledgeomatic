﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOM.Procedures
{
    public interface IInterpreter
    {
         string Interpret(string content);
    } 
}