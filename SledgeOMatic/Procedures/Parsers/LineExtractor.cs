﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SOM.Extentions;
namespace SOM.Procedures
{
    public class LineExtractor : ICompiler
    {
        private string _extractTarget;
        private int _numberOfLines = 4; 
        public LineExtractor(string ExtractTarget, int NumberOfLines )
        {
            _extractTarget = ExtractTarget;
            _numberOfLines = NumberOfLines; 
        }
        public string Compile(string content)
        {
            StringBuilder result = new StringBuilder();  
            content = $"{new string('\n', _numberOfLines)}{content}{new string('\n', _numberOfLines)}";
            string[] lines = content.Split('\n');
            int findingCnt = 0;
            for (int lineIndex = _numberOfLines; lineIndex < lines.Length - _numberOfLines; lineIndex++)
            { 
                Match match = Regex.Match(lines[lineIndex], _extractTarget); 
                if (match.Success)
                {
                    findingCnt++; 
                    result.Append($"\n[SRC {findingCnt.ToString()} {lineIndex}] \n");
                    for (int takeIndex = lineIndex - _numberOfLines; takeIndex <= lineIndex + _numberOfLines; takeIndex++)
                    { 
                        if (takeIndex < lines.Length && takeIndex > 0 ) { 
                            result.Append($"[LN {takeIndex.ToString()}] {lines[takeIndex]}\n");
                        }  
                    } 
                }
            } 
            return result.ToString().TrimTrailingNewline();
        }
    }
}
