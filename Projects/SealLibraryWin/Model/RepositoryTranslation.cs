﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.IO;
using Seal.Helpers;
using System.Text.RegularExpressions;
using System;
using System.Data;

namespace Seal.Model
{
    /// <summary>
    /// A RepositoryTranslation defines a translation got from the repository
    /// </summary>
    public class RepositoryTranslation
    {
        /// <summary>
        /// The context of the translation
        /// </summary>
        public string Context = "";

        /// <summary>
        /// The instance of the translation
        /// </summary>
        public string Instance = "";

        /// <summary>
        /// The reference text
        /// </summary>
        public string Reference = "";

        /// <summary>
        /// List of translation texts per language
        /// </summary>
        public Dictionary<string, string> Translations = new Dictionary<string, string>();

        /// <summary>
        /// Usage counter for debug purpose
        /// </summary>
        public int Usage;

        /// <summary>
        /// Init the list of RepositoryTranslation from the CSV file
        /// </summary>
        static public void InitFromCSV(Dictionary<string, RepositoryTranslation> translations, string path, bool hasInstance)
        {
            try
            {
                initFromCSV(translations, path, hasInstance);
            }
            catch
            {
                if (File.Exists(path))
                {
                    try
                    {
                        //probably locked with Excel, copy in a temp file to try
                        string newPath = FileHelper.GetTempUniqueFileName(path);
                        File.Copy(path, newPath, true);
                        initFromCSV(translations, newPath, hasInstance);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Init the list of RepositoryTranslation from the Excel file
        /// </summary>
        static public void InitFromExcel(Dictionary<string, RepositoryTranslation> translations, string path, bool hasInstance)
        {
            try
            {
                initFromExcel(translations, path, hasInstance);
            }
            catch
            {
                if (File.Exists(path))
                {
                    try
                    {
                        //probably locked with Excel, copy in a temp file to try
                        string newPath = FileHelper.GetTempUniqueFileName(path);
                        File.Copy(path, newPath, true);
                        initFromExcel(translations, newPath, hasInstance);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }
        }

        static private void initFromCSV(Dictionary<string, RepositoryTranslation> translations, string filePath, bool hasInstance)
        {
            if (File.Exists(filePath))
            {
                bool isHeader = true;
                Regex regexp = null;
                List<string> languages = new List<string>();

                foreach (string line in File.ReadAllLines(filePath, System.Text.Encoding.UTF8))
                {
                    if (regexp == null)
                    {
                        string exp = "(?<=^|,)(\"(?:[^\"]|\"\")*\"|[^,]*)";
                        char separator = ',';
                        //use the first line to determine the separator
                        if (line.StartsWith("Context")) separator = line[7];
                        if (separator != ',') exp = exp.Replace(',', separator);
                        regexp = new Regex(exp);
                    }

                    MatchCollection collection = regexp.Matches(line);
                    int startCol = (hasInstance ? 3 : 2);
                    if (collection.Count > startCol)
                    {
                        if (isHeader)
                        {
                            for (int i = startCol; i < collection.Count; i++)
                            {
                                languages.Add(ExcelHelper.FromCsv(collection[i].Value));
                            }
                            isHeader = false;
                        }
                        else
                        {
                            var context = ExcelHelper.FromCsv(collection[0].Value);
                            var reference = ExcelHelper.FromCsv(collection[startCol - 1].Value);

                            RepositoryTranslation translation = null;
                            var key = context + "\r" + reference;
                            if (hasInstance)
                            {
                                var instance = ExcelHelper.FromCsv(collection[1].Value);
                                key += "\r" + instance;
                                if (!translations.ContainsKey(key))
                                {
                                    translation = new RepositoryTranslation() { Context = context, Reference = reference, Instance = instance };
                                    translations.Add(key, translation);
                                }
                                else translation = translations[key];
                            }
                            else
                            {
                                if (!translations.ContainsKey(key))
                                {
                                    translation = new RepositoryTranslation() { Context = context, Reference = reference };
                                    translations.Add(key, translation);
                                }
                                else translation = translations[key];
                            }

                            for (int i = 0; i < languages.Count && i + startCol < collection.Count; i++)
                            {
                                if (string.IsNullOrEmpty(languages[i]) || translation.Translations.ContainsKey(languages[i])) continue;
                                translation.Translations.Add(languages[i], ExcelHelper.FromCsv(collection[i + startCol].Value));
                            }
                        }
                    }
                }
            }
        }

        static private void initFromExcel(Dictionary<string, RepositoryTranslation> translations, string filePath, bool hasInstance)
        {
            if (File.Exists(filePath))
            {
                InitFromDataTable(translations, DataTableLoader.FromExcel(filePath), hasInstance);
            }
        }

        /// <summary>
        /// Init translations from a data table 
        /// </summary>
        static public void InitFromDataTable(Dictionary<string, RepositoryTranslation> translations, DataTable dt, bool hasInstance)
        {
            List<string> languages = new List<string>();

            if (dt.Rows.Count > 1)
            {
                int startCol = (hasInstance ? 3 : 2);
                for (int i = startCol; i < dt.Columns.Count; i++)
                {
                    languages.Add(ExcelHelper.FromCsv(dt.Columns[i].ColumnName));
                }

                foreach (DataRow dr in dt.Rows)
                {
                    var context = ExcelHelper.FromCsv(dr[0].ToString());
                    var reference = ExcelHelper.FromCsv(dr[startCol - 1].ToString());

                    RepositoryTranslation translation = null;
                    var key = context + "\r" + reference;
                    if (hasInstance)
                    {
                        var instance = ExcelHelper.FromCsv(dr[1].ToString());
                        key += "\r" + instance;
                        if (!translations.ContainsKey(key))
                        {
                            translation = new RepositoryTranslation() { Context = context, Reference = reference, Instance = instance };
                            translations.Add(key, translation);
                        }
                        else translation = translations[key];
                    }
                    else
                    {
                        if (!translations.ContainsKey(key))
                        {
                            translation = new RepositoryTranslation() { Context = context, Reference = reference };
                            translations.Add(key, translation);
                        }
                        else translation = translations[key];
                    }

                    for (int i = 0; i < languages.Count && i + startCol < dt.Columns.Count; i++)
                    {
                        if (string.IsNullOrEmpty(languages[i])) continue;
                        var value = ExcelHelper.FromCsv(dr[i + startCol].ToString());
                        if (translation.Translations.ContainsKey(languages[i])) translation.Translations[languages[i]] = value;
                        else translation.Translations.Add(languages[i], value);
                    }
                }
            }
        }
    }
}
