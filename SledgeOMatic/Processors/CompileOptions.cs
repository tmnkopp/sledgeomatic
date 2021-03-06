﻿using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SOM.Compilers;
using SOM.IO;
using SOM.Parsers;
using SOM.Procedures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace SOM 
{
    public interface ICompileProcessor
    {
        void Process(CompileOptions o);
    }
    public class CompileProcessor : ICompileProcessor
    {
        private readonly ICompiler compiler;
        private readonly IConfiguration config;
        private readonly ILogger logger; 
        public CompileProcessor(
              ICompiler compiler
            , IConfiguration config
            , ILogger logger
        )
        {
            this.compiler = compiler;
            this.config = config;
            this.logger = logger;
        }
        public void Process(CompileOptions o) 
        {
            if (o.Path.ToString().EndsWith("yaml"))
            {
                if (!o.Path.Contains(":\\"))
                    o.Path = Environment.GetEnvironmentVariable("som", EnvironmentVariableTarget.User).ToLower().Replace("som.exe", o.Path);
                config.GetSection("AppSettings:CompileConfig").Value = o.Path.ToString();
                logger.LogInformation("{o}", config.GetSection("AppSettings:CompileConfig").Value); 
            } 
            compiler.CompileMode = o.CompileMode; 
            var yaml = new YamlStream();
            using (TextReader tr = File.OpenText(config.GetSection("AppSettings:CompileConfig").Value))
                yaml.Load(tr);
            var root = (YamlMappingNode)yaml.Documents[0].RootNode;

            List<object> oparms;
            foreach (var rootitem in root.Children)
            {
                PropertyInfo pi = compiler.GetType().GetProperty(rootitem.Key.ToString());
                if (pi != null)
                { 
                    if (pi.PropertyType.FullName.Contains("List`1"))
                    { 
                        foreach (var propitems in ((YamlSequenceNode)rootitem.Value).Children)
                        {
                            string stype = ((YamlMappingNode)propitems).FirstOrDefault().Key.ToString();
                            oparms = GetParms((YamlMappingNode)propitems);

                            var typ = Assm().GetTypes().Where(t => t.Name == stype && typeof(ICompilable).IsAssignableFrom(t)).FirstOrDefault();
                            Type gtyp = Type.GetType($"{typ.FullName}, SOM");
                            var obj = Activator.CreateInstance(gtyp, oparms.ToArray());
                            pi.PropertyType.GetMethod("Add").Invoke(pi.GetValue(compiler), new object[] { obj });
                        }
                    }
                    else
                    {
                        pi.SetValue(compiler, rootitem.Value.ToString(), null);
                    }
                }
                MethodInfo[] mlist = compiler.GetType().GetMethods().Where(m => m.Name.ToLower() == rootitem.Key.ToString().ToLower()).ToArray();
                if (mlist.Count() > 0)
                {  
                    oparms = new List<object>();
                    foreach (MethodInfo m in mlist) 
                        if (m.Name == rootitem.Key.ToString() && m.GetParameters().Count() == oparms.Count()) 
                            m.Invoke(compiler, oparms.ToArray());  
                } 
            } 
            if (o.CompileMode != CompileMode.Commit) Cache.Inspect(); 
        }
        private List<object> GetParms(YamlMappingNode propitems)
        {
            List<object> oparms = new List<object>();
            string stype = "";
            foreach (var prop in (YamlMappingNode)propitems)
            {
                stype = prop.Key.ToString();
                if (prop.Value.GetType() == typeof(YamlSequenceNode))
                {
                    foreach (var parm in ((YamlSequenceNode)prop.Value).Children)
                        oparms.Add(parm.ToString());
                }
                if (prop.Value.GetType() == typeof(YamlScalarNode))
                {
                    oparms.Add(prop.Value.ToString());
                }
            }
            return oparms;
        }
        private Assembly Assm() {
            Assembly assmsom = Assembly.GetExecutingAssembly();
            return assmsom;
        }
    }
}
