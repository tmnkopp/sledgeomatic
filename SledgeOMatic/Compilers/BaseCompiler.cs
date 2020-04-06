﻿using SOM.Extentions;
using SOM.IO;
using SOM.Procedures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOM.Compilers
{
    public enum CompileMode
    {
        Debug,
        Commit,
        ForceCommit
    }
    public class AppSettingsCompiler : BaseCompiler
    {
        public AppSettingsCompiler()
        {
            Source = AppSettings.SourceDir;
            Dest = AppSettings.DestDir;
            FileFilter = "*_*";
        }
        public override void Compile()
        { 
            base.Compile(); 
        }
    }

    public abstract class BaseCompiler
    { 
        public string Source = "";
        public string Dest = "";
        public string FileFilter = "*";
        public List<IProcedure> ContentCompilation;
        public List<IProcedure> FilenameCompilation;
        public CompileMode CompileMode;
        
        private string content = "";

        public virtual void Compile()
        {
            DirectoryInfo DI = new DirectoryInfo($"{Source}");
            foreach (var file in DI.GetFiles(FileFilter, SearchOption.TopDirectoryOnly))
            {
                content = new FileReader(file.FullName).Read().ToString();
                foreach (IProcedure proc in ContentCompilation)
                    content = proc.Execute(content);

                string newFileName = file.Name;
                foreach (IProcedure proc in FilenameCompilation)
                    newFileName = proc.Execute(newFileName).RemoveWhiteAndBreaks(); 

                if (CompileMode != CompileMode.Debug) 
                    CommitFile(content, $"{Dest}\\{newFileName}"); 
            } 
        }
        private void CommitFile(string Content, string FileName)
        {
            if (CompileMode == CompileMode.ForceCommit)
                FileSys.Utils.DirectoryCreator(FileName, AppSettings.BasePath);
            FileWriter fw = new FileWriter($"{FileName}");
            fw.Write(Content);
        }
    }
}
