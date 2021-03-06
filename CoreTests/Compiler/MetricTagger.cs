﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SOM.Procedures;
using SOM.IO;
using Newtonsoft.Json;
using SOM.Compilers;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace CoreTests
{
    [TestClass]
    public class MetricTaggeTests
    {
        [TestMethod]
        public void MetricTagger_Tags()
        {
            var metrics = new MetricProvider().GetItems("2021-A-HVA");
            Compiler compiler = new Compiler();
            compiler.Source = @"c:\_som\_src\_compile\BOD\compiled";
            compiler.CompileMode = CompileMode.Cache;
            compiler.ContentCompilers.Add(new MetricTagger(metrics));
            compiler.FileFilter = "*.aspx";
            compiler.Dest = @"D:\dev\CyberScope\CyberScopeBranch\CSwebdev\code\CyberScope\HVA\2021";
            compiler.Compile();
            Cache.Inspect();
        }

        [TestMethod]
        public void MetricProvider_Providers()
        {
            var metrics = new MetricProvider().GetItems("2021-A-HVA");
            Assert.IsNotNull(metrics); 
        }
        [TestMethod]
        public void FrmValProvider_Provides()
        {
            var metrics = new MetricProvider().GetItems("2021-A-HVA");
            Cache.Write("");
            foreach (var item in metrics)
            {
                if (item.FK_PickList != "")
                {
                    Cache.Append(string.Format(snippet, item.QPK, item.IdText, item.IdTextVar, item.QTEXT
                        , $"SET @PK_PickList_OTH = (SELECT TOP 1 CONVERT(NVARCHAR(9),PK_PickList) PK_PickList FROM vwPickLists WHERE CodeValue = 'OTH' AND PK_PickListType = {item.FK_PickList})"
                        , ""));
                }
                else
                {
                    Cache.Append(string.Format(snippet, item.QPK, item.IdText, item.IdTextVar, item.QTEXT
                        , "--"
                        , ""));
                }
            } 
            Cache.Inspect();
            Assert.IsNotNull(metrics);
        }
        private string snippet = @"
        {4}
        -- {3}  
		IF ISNULL(@ANS_1B_{2},'') = '' {5}
		BEGIN
			INSERT INTO @ErrorTable  
			SELECT  PK_Question,  PK_FormPage,  page_sort_pos, questgroup_sort_pos, quest_sort_pos, ('Please provide an answer for {1} ' + QuestionText) Error, identifier_text
			FROM [dbo].[vw_OrgSubQuestions] WHERE PK_Question = {0} AND PK_OrgSubmission = @PK_OrgSubmission    
		END 
        "; 
    }
    public class MetricTagger : ICompilable
    {
        #region CTOR 
        private List<Metric> metrics;
        public MetricTagger(List<Metric> metrics)
        {
            this.metrics = metrics;
        }
        #endregion
        public string Compile(string content)
        {
            string result = content; 
            var parser = new RangeExtractor(" PK_Question=\"(\\d{5})", "<tr", "</tr>");
            foreach (var item in parser.Parse(content))
            {
                var mth = Regex.Match(item, " PK_Question=\"(\\d{5})");
                var g = mth.Groups[1].Value;
                var met = (from m in metrics where m.id == g select m).FirstOrDefault();
                if (met != null)
                {
                    var body = $"\n<!--\n{{\n{met.body}\n}}\n-->\n";
                    result = result.Replace(item, $"\n{body}\n\t\t{item}\n");
                } 
            }
            return result;
        }
    }
    public class Metric {
        public string id { get; set; }
        public string QPK { get; set; }
        public string IdText { get; set; } 
        public string IdTextVar { get; set; } 
        public string QTEXT { get; set; }  
        public string body { get; set; }
        public string FK_PickList { get; set; }
    }
    public class MetricProvider { 
        public List<Metric> GetItems(string FORM) {
            List<Metric> items = new List<Metric>();
            var con = ConfigurationManager.ConnectionStrings["default"].ToString();
            using (SqlConnection myConnection = new SqlConnection(con))
            {
                string sql = $"SELECT ID,IdText,QPK,QTEXT,QGROUP,GroupDesc,FK_PickList FROM vw_MetricsCompositeKeys WHERE PK_Form = '{FORM}' AND SECTION_COUNT = 1 AND QuestionTypeCode <> 'STTL'";
                SqlCommand oCmd = new SqlCommand(sql, myConnection);
                SqlDataReader oReader;
                myConnection.Open();
                using (oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat(
                            "'QPK':'{0}', 'QTEXT':'{1}', 'QGROUP':'{2}', 'GroupDesc':'{3}', 'IdText':'{4}', 'ID':'{5}'"
                            , Convert.ToString(oReader["QPK"] ?? "")
                            , Convert.ToString(oReader["QTEXT"] ?? "").Replace("'", "").Replace("\"", "")
                            , Convert.ToString(oReader["QGROUP"] ?? "")
                            , Convert.ToString(oReader["GroupDesc"] ?? "").Replace("'", "")
                            , Convert.ToString(oReader["IdText"] ?? "")
                            , Convert.ToString(oReader["FK_PickList"] ?? "")
                            , Convert.ToString(oReader["ID"] ?? "")
                        ); 
                        items.Add(new Metric()
                        {
                            id = Convert.ToString(oReader["QPK"]),
                            body = sb.ToString().Replace("', '", "'\n, '").Replace("'", "\""),
                            QPK = Convert.ToString(oReader["QPK"]),
                            IdText = Convert.ToString(oReader["IdText"]),
                            IdTextVar = Convert.ToString(oReader["IdText"]).Replace(".","_"),
                            QTEXT = Convert.ToString(oReader["QTEXT"]),
                            FK_PickList = Convert.ToString(oReader["FK_PickList"])
                        });
                    }
                    myConnection.Close();
                }
            }
            return items;
        }  
    }
}
