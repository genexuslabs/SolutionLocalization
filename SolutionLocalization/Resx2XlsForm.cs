using OfficeOpenXml;
using SolutionLocalization;
using SolutionLocalization.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Xml;

namespace Resx2Xls
{
	public partial class Resx2XlsForm
	{
		object m_objOpt = System.Reflection.Missing.Value;

		readonly int COL1_IDX__RESX_SRC = 1;
		readonly int COL2_IDX__RESX_DEST = 2;
		readonly int COL3_IDX__KEY = 3;
		readonly int COL4_IDX__VALUE = 4;
		readonly int COL5_IDX__COMMENT = 5;
		readonly int DATA_COLS_OFFSET = 6;

		readonly int TITLE_ROW = 1;
		readonly int CULTURE_ROW = 2;
		readonly int DATA_ROWS_OFFSET = 3;

		enum ResxToXlsOperation { Create, Build, Update };

		public void ResxToXls(string path, bool deepSearch, string xslFile, string[] cultures, string[] excludeList, bool useFolderNamespacePrefix, string[] directoryExcludes, bool includeNonTranslatableTxt)
		{
			if (!System.IO.Directory.Exists(path))
				return;
			ResxData rd = ResxToDataSet(path, deepSearch, cultures, excludeList, useFolderNamespacePrefix, directoryExcludes, includeNonTranslatableTxt);
			DataSetToXls(rd, xslFile);
		}

		public void ResxToXml(string path, bool deepSearch, string xslFile, string[] cultures, string[] excludeList, bool useFolderNamespacePrefix, string[] directoryExcludes, bool includeNonTranslatableTxt)
		{
			if (!System.IO.Directory.Exists(path))
				return;
			ResxData rd = ResxToDataSet(path, deepSearch, cultures, excludeList, useFolderNamespacePrefix, directoryExcludes, includeNonTranslatableTxt);
			DataSetToXml(rd, xslFile);
		}

		public void XlsToResx(string xlsFile, string xmlFile = null, string outputDir = null)
		{
			if (!File.Exists(xlsFile))
				throw new Exception(string.Format("'{0}' does not exists", xlsFile));

			XmlDocument docValidEntries = null;
			if (!string.IsNullOrEmpty(xmlFile) && File.Exists(xmlFile))
			{
				try
				{
					docValidEntries = new XmlDocument();
					docValidEntries.Load(xmlFile);
				}
				catch (Exception ex)
				{
					Console.WriteLine(string.Format("warning: Disabling validation of translations, cannot load the specified file (exception: {0})", ex.Message));
					docValidEntries = null;
				}
			}

			string path = outputDir == null? new FileInfo(xlsFile).DirectoryName : outputDir;
			using (ExcelPackage app = new ExcelPackage(new FileInfo(xlsFile)))
			{
				ExcelWorksheets sheets = app.Workbook.Worksheets;
				ExcelWorksheet sheet = sheets[1];

				bool hasLanguage = true;
				int col = DATA_COLS_OFFSET;

				while (hasLanguage)
				{
					object val = (sheet.Cells[CULTURE_ROW, col] as ExcelRange).Text;

					if (val is string)
					{
						if (!String.IsNullOrEmpty((string)val))
						{
							string cult = (string)val;
							string pathCulture = path + Path.DirectorySeparatorChar;
							if (!Directory.Exists(pathCulture))
								Directory.CreateDirectory(pathCulture);

							int row = DATA_ROWS_OFFSET;

							string fileSrc;
							string fileDest;
							bool readrow = true;
							while (readrow)
							{
								fileSrc = (sheet.Cells[row, COL1_IDX__RESX_SRC] as ExcelRange).Text.ToString();
								fileDest = (sheet.Cells[row, COL2_IDX__RESX_DEST] as ExcelRange).Text.ToString();

								if (string.IsNullOrEmpty(fileDest))
									break;

								string f = Path.Combine(pathCulture, string.Format("{0}.{1}.resx", JustStemWithFullPath(fileSrc), cult));

								FileInfo fileInfo = new FileInfo(f);
								if (!Directory.Exists(fileInfo.DirectoryName))
									Directory.CreateDirectory(fileInfo.DirectoryName);

								Console.WriteLine(string.Format("Checking translations for '{0}'...", f));

								int resourceItemCount = 0;
								using (StreamWriter sw = new StreamWriter(f, false, Encoding.UTF8))
								{
									using (ResXResourceWriter rw = new ResXResourceWriter(sw))
									{
										while (readrow)
										{
											string key = (sheet.Cells[row, COL3_IDX__KEY] as ExcelRange).Text.ToString();
											object data = (sheet.Cells[row, col] as ExcelRange).Text.ToString();

											if ((key is String) & !String.IsNullOrEmpty(key))
											{
												if (data is string)
												{
													string text = data as string;
													text = text.Replace("\\r", "\r").Replace("\\n", "\n");
													if (text.StartsWith("!="))
														text = text.Substring(1);
													if (IsValidEntry(sheet, row, key, ref text, docValidEntries, cult))
													{
														rw.AddResource(new ResXDataNode(key, text));
														resourceItemCount++;
													}
												}

												row++;

												string file = (sheet.Cells[row, COL2_IDX__RESX_DEST] as ExcelRange).Text.ToString();
												if (file != fileDest)
													break;
											}
											else
											{
												readrow = false;
											}
										}
									}
								}
								if (resourceItemCount == 0)
								{
									try { File.Delete(f); }
									catch { }
									Console.WriteLine("Ignore generation, no-translations defined.");
								}
								else
								{
									Console.WriteLine("Successfully generated.");
								}
							}
						}
						else
							hasLanguage = false;
					}
					else
						hasLanguage = false;

					col++;
				}
			}
			Console.WriteLine("warning: xls document has no language information");
		}
		private bool IsValidEntry(ExcelWorksheet sheet, int row, string key, ref string text, XmlDocument xmlValidEntries, string cult)
		{
			if (xmlValidEntries != null)
			{
				// Check that the translated text corresponde to the current one (the message can be changed in the middle
				// of the translation process; in that case we must not define the translation and ask for that for the new text).
				string resxFile = (sheet.Cells[row, COL1_IDX__RESX_SRC] as ExcelRange).Text.ToString();
				XmlNode entryDef = xmlValidEntries.SelectSingleNode(string.Format("Translations/Worksheet/Message[ResourceFile='{0}' and ResourceKey='{1}']", resxFile, key));
				if (entryDef == null)
				{
					text = null;
					Console.WriteLine(string.Format("warning: Ignoring translation for FILE:{0}, KEY:{1} - obsolete", resxFile, key));
				}
				else
				{
					string defaultText = (sheet.Cells[row, COL4_IDX__VALUE] as ExcelRange).Text.ToString();
					string validText = GetXmlInnerText(entryDef.SelectSingleNode("Text"));
					if (string.Compare(validText, defaultText) != 0)
					{
						// Preserve the current translation
						text = GetXmlInnerText(entryDef.SelectSingleNode("Translation/Text[@Culture='{cult}']"));
						if (string.IsNullOrEmpty(text))
							Console.WriteLine(string.Format("warning: Ignoring translation for FILE:{0}, KEY:{1} - original text has changed", resxFile, key));
					}
				}
			}
			return !String.IsNullOrEmpty(text);
		}
		private string GetXmlInnerText(XmlNode node)
		{
			return node == null ? null : node.InnerText;
		}

		private ResxData ResxToDataSet(string path, bool deepSearch, string[] cultureList, string[] excludeList, bool useFolderNamespacePrefix, string[] directoryExcludes, bool includeNonTranslatableTxt)
		{
			ResxData rd = new ResxData();
			List<string> cultures = new List<string>(cultureList);
			string[] files;

			if (deepSearch)
				files = System.IO.Directory.GetFiles(path, "*.resx", SearchOption.AllDirectories);
			else
				files = System.IO.Directory.GetFiles(path, "*.resx", SearchOption.TopDirectoryOnly);

			List<string> cultureSpecific = new List<string>();
			foreach (string f in files)
			{
				// Ignore .resx processing if "Resources.nontranslatable" file is defined
				if (File.Exists(Path.Combine(Path.GetDirectoryName(f), "Resources.nontranslatable"))
					|| File.Exists(f + ".nontranslatable"))
				{
					continue;
				}

				string cult;
				if (!ResxIsCultureSpecific(f, out cult) && !FileHelper.IsExcluded(path, f, directoryExcludes))
				{
					Console.WriteLine("Reading " + f);
					ReadResx(f, path, rd, cultureList, excludeList, useFolderNamespacePrefix, includeNonTranslatableTxt);
				}
				else if (cultures.Contains(cult))
				{
					cultureSpecific.Add(f);
				}
			}

			foreach (string f in cultureSpecific)
			{
				Console.WriteLine("Reading " + f);
				ReadResxCult(f, path, rd, cultureList, excludeList, useFolderNamespacePrefix);
			}

			return rd;
		}

		private bool ResxIsCultureSpecific(string path, out string cult)
		{
			cult = string.Empty;
			CultureInfo ci = GetResxCultureSpecific(path);
			if (ci == null)
			{
				return false;
			}
			else
			{
				cult = ci.Name;
				return true;
			}
		}

		private CultureInfo GetResxCultureSpecific(string path)
		{
			string cult = String.Empty;
			FileInfo fi = new FileInfo(path);

			//Remove the extension and return the string	
			string fname = JustStem(fi.Name);

			if (fname.IndexOf(".") != -1)
				cult = fname.Substring(fname.LastIndexOf('.') + 1);

			if (cult == String.Empty)
				return null;

			try
			{
				return new System.Globalization.CultureInfo(cult);
			}
			catch
			{
				return null;
			}
		}

		private string GetNamespacePrefix(string projectRoot, string path)
		{
			path = path.Remove(0, projectRoot.Length);
			if (path.StartsWith(@"\"))
				path = path.Remove(0, 1);
			return path.Replace(@"\", ".");
		}

		private void ReadResx(string fileName, string projectRoot, ResxData rd, string[] cultureList, string[] excludeList, bool useFolderNamespacePrefix, bool includeNonTranslatableTxt)
		{
			FileInfo fi = new FileInfo(fileName);

			string fileRelativePath = fi.FullName.Remove(0, AddBS(projectRoot).Length);
			string fileDestination = useFolderNamespacePrefix ?
				GetNamespacePrefix(AddBS(projectRoot), AddBS(fi.DirectoryName)) + fi.Name
				: fi.Name;

			HashSet<string> keys = new HashSet<string>();
			try
			{
				ITypeResolutionService typeres = null;
				using (ResXResourceReader reader = new ResXResourceReader(fileName) { BasePath = fi.DirectoryName })
				{
					reader.UseResXDataNodes = true;

					IDictionaryEnumerator dictResx = reader.GetEnumerator();

					while (dictResx.MoveNext())
					{
						ResXDataNode dnode = (ResXDataNode)dictResx.Value;
						object value = dnode.GetValue(typeres);
						if (value is string)
						{
							AddResx2DatasetRow(dnode.Name, (string)value, dnode.Comment, rd, cultureList, excludeList, fileRelativePath, fileDestination, includeNonTranslatableTxt);
							keys.Add(string.Format("{0}#{1}", fileRelativePath, dnode.Name));
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (TryReadingAsXml(fileName, rd, cultureList, excludeList, fileRelativePath, fileDestination, includeNonTranslatableTxt, keys))
				{
					Console.WriteLine(string.Format("warning: A problem occured reading {0} (Exception: {1})", fileName, ex.Message));
					Console.WriteLine("  >> Info: looking only for <control>.Text expressions.");
				}
				else
				{
					Console.WriteLine(string.Format("error: A problem occured reading {0}", fileName));
					Console.WriteLine(string.Format("  >> Exception: {0}", ex.Message));
				}
			}
		}

		private bool TryReadingAsXml(string resxFileName, ResxData rd, string[] cultureList, string[] excludeList, string fileRelativePath, string fileDestination, bool includeNonTranslatableTxt, HashSet<string> keys)
		{
			// Use this method as a second change to identify all .Text expressions
			try
			{
				XmlDocument xDoc = new XmlDocument();
				xDoc.Load(resxFileName);
				foreach (XmlNode strNode in xDoc.SelectNodes("root/data[@name]"))
					if (strNode.Attributes["name"].Value.EndsWith(".Text")
						&& !keys.Contains(string.Format("{0}#{1}", fileRelativePath, strNode.Attributes["name"].Value)))
					{
						AddResx2DatasetRow(strNode.Attributes["name"].Value, strNode.SelectSingleNode("value").InnerText, null, rd, cultureList, excludeList, fileRelativePath, fileDestination, includeNonTranslatableTxt);
					}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private void AddResx2DatasetRow(string key, string value, string comment, ResxData rd, string[] cultureList, string[] excludeList, string fileRelativePath, string fileDestination, bool includeNonTranslatableTxt)
		{
			if (excludeList.FirstOrDefault(x => key.EndsWith(x)) != null)
				return;

			if (!includeNonTranslatableTxt
				&& (string.IsNullOrEmpty(value)
					|| !string.IsNullOrEmpty(comment) && comment.StartsWith("#NonTranslatable", StringComparison.InvariantCultureIgnoreCase)))
			{
				return;
			}

			value = value.Replace("\r", "\\r").Replace("\n", "\\n");

			ResxData.ResxRow r = rd.Resx.NewResxRow();
			r.FileSource = fileRelativePath;
			r.FileDestination = fileDestination;
			r.Key = key;
			r.Value = value;
			r.Comment = comment;

			rd.Resx.AddResxRow(r);

			foreach (string cult in cultureList)
			{
				ResxData.ResxLocalizedRow lr = rd.ResxLocalized.NewResxLocalizedRow();

				lr.Key = r.Key;
				// change get from resx now if exists.
				lr.Value = String.Empty;
				lr.Culture = cult;

				lr.ParentId = r.Id;
				lr.SetParentRow(r);

				rd.ResxLocalized.AddResxLocalizedRow(lr);
			}
		}

		private void ReadResxCult(string fileName, string projectRoot, ResxData rd, string[] cultureList, string[] excludeList, bool useFolderNamespacePrefix)
		{
			FileInfo fi = new FileInfo(fileName);
			CultureInfo cultureInfo = GetResxCultureSpecific(fileName);
			string fileRelativePath = fi.FullName.Remove(0, AddBS(projectRoot).Length);
			string fileDestination;
			if (useFolderNamespacePrefix)
				fileDestination = GetNamespacePrefix(AddBS(projectRoot), AddBS(fi.DirectoryName)) + fi.Name;
			else
				fileDestination = fi.Name;
			ResXResourceReader reader = new ResXResourceReader(fileName);
			reader.BasePath = fi.DirectoryName;

			try
			{
				#region read
				foreach (DictionaryEntry de in reader)
				{
					if (de.Value is string)
					{
						string key = (string)de.Key;
						bool exclude = false;
						foreach (string e in excludeList)
						{
							if (key.EndsWith(e))
							{
								exclude = true;
								break;
							}
						}
						if (!exclude)
						{
							string strWhere = String.Format("FileSource ='{0}' AND Key='{1}'", fileRelativePath.Replace("." + cultureInfo.Name, ""), de.Key.ToString());
							ResxData.ResxRow[] rows = (ResxData.ResxRow[])rd.Resx.Select(strWhere);
							if ((rows == null) || (rows.Length == 0))
								continue;

							ResxData.ResxRow row = rows[0];
							foreach (ResxData.ResxLocalizedRow lr in row.GetResxLocalizedRows())
							{
								if (lr.Culture == cultureInfo.Name)
								{
									row.BeginEdit();
									string value = de.Value.ToString();
									// update row
									if (value.Contains("\r") || value.Contains("\n"))
										value = value.Replace("\r", "\\r").Replace("\n", "\\n");
									lr.Value = value;
									row.EndEdit();
								}
							}
						}
					}
				}
				#endregion
			}
			catch (Exception ex)
			{
				Console.WriteLine("A problem occured reading " + fileName + "\n" + ex.Message, "Information");
			}
			reader.Close();
		}

		private ResxData XlsToDataSet(string xlsFile)
		{
			ResxData rd = new ResxData();
			using (ExcelPackage app = new ExcelPackage(new FileInfo(xlsFile)))
			{
				ExcelWorksheets sheets = app.Workbook.Worksheets;
				ExcelWorksheet sheet = sheets[1];

				int row = DATA_ROWS_OFFSET;

				bool continueLoop = true;
				while (continueLoop)
				{
					string fileSrc = (sheet.Cells[row, COL1_IDX__RESX_SRC] as ExcelRange).Text.ToString();

					if (String.IsNullOrEmpty(fileSrc))
						break;

					ResxData.ResxRow r = rd.Resx.NewResxRow();
					r.FileSource = (sheet.Cells[row, COL1_IDX__RESX_SRC] as ExcelRange).Text.ToString();
					r.FileDestination = (sheet.Cells[row, COL2_IDX__RESX_DEST] as ExcelRange).Text.ToString();
					r.Key = (sheet.Cells[row, COL3_IDX__KEY] as ExcelRange).Text.ToString();
					r.Value = (sheet.Cells[row, COL4_IDX__VALUE] as ExcelRange).Text.ToString();
					r.Comment = (sheet.Cells[row, COL5_IDX__COMMENT] as ExcelRange).Text.ToString();

					rd.Resx.AddResxRow(r);

					bool hasCulture = true;
					int col = DATA_COLS_OFFSET;
					while (hasCulture)
					{
						string cult = (sheet.Cells[CULTURE_ROW, col] as ExcelRange).Text.ToString();

						if (String.IsNullOrEmpty(cult))
							break;

						ResxData.ResxLocalizedRow lr = rd.ResxLocalized.NewResxLocalizedRow();
						lr.Culture = cult;
						lr.Key = (sheet.Cells[row, COL3_IDX__KEY] as ExcelRange).Text.ToString();
						lr.Value = (sheet.Cells[row, col] as ExcelRange).Text.ToString();
						lr.ParentId = r.Id;
						lr.SetParentRow(r);

						rd.ResxLocalized.AddResxLocalizedRow(lr);

						col++;
					}

					row++;
				}

				rd.AcceptChanges();
			}

			return rd;
		}

		private void DataSetToXls(ResxData rd, string fileName)
		{
			Console.WriteLine("Creating Spreadsheet...");
			using (ExcelPackage app = new ExcelPackage(new FileInfo(fileName)))
			{
				app.Workbook.Worksheets.Add("WorkSheet1");

				ExcelWorksheets sheets = app.Workbook.Worksheets;
				ExcelWorksheet sheet = sheets[1];
				sheet.Name = "Localize";

				// <Define Basic Columns>
				sheet.Cells[TITLE_ROW, COL1_IDX__RESX_SRC].Value = "Resx source";
				sheet.Cells[TITLE_ROW, COL2_IDX__RESX_DEST].Value = "Resx Name";
				sheet.Cells[TITLE_ROW, COL3_IDX__KEY].Value = "Key";
				sheet.Cells[TITLE_ROW, COL4_IDX__VALUE].Value = "Value";
				sheet.Cells[TITLE_ROW, COL5_IDX__COMMENT].Value = "Comment";
				// </Define Basic Columns>

				string[] cultures = GetCulturesFromDataSet(rd);

				if (cultures == null)
					return;

				int index = DATA_COLS_OFFSET;
				foreach (string cult in cultures)
				{
					CultureInfo ci = new CultureInfo(cult);
					sheet.Cells[TITLE_ROW, index].Value = ci.DisplayName;
					sheet.Cells[CULTURE_ROW, index].Value = ci.Name;
					index++;
				}

				DataView dw = rd.Resx.DefaultView;
				dw.Sort = "FileSource, Key";

				int row = DATA_ROWS_OFFSET;
				foreach (DataRowView drw in dw)
				{
					ResxData.ResxRow r = (ResxData.ResxRow)drw.Row;

					if (r.Value.StartsWith("="))
						r.Value = "!" + r.Value;

					ResxData.ResxLocalizedRow[] rows = r.GetResxLocalizedRows();

#if EMPTYRES
					bool hasAlreadyTranslate = false;
		            bool emptyResource = false;
#endif

					foreach (ResxData.ResxLocalizedRow lr in rows)
					{
						string culture = lr.Culture;

						int col = Array.IndexOf(cultures, culture);
						if (lr.Value.StartsWith("="))
							lr.Value = "!" + lr.Value;

						if (col >= 0 && r.Value.Length > 0 && lr.Value.Length > 0)
						{
#if EMPTYRES
							hasAlreadyTranslate = true;
#endif
							sheet.Cells[row, col + DATA_COLS_OFFSET].Value = lr.Value;
						}
						else if (col >= 0 && r.Value.Length == 0)
						{
							//Nothing to translate
#if EMPTYRES
							hasAlreadyTranslate = true;
							emptyResource = true;
#endif
						}
						else if (col >= 0)
						{
							sheet.Cells[row, col + DATA_COLS_OFFSET].Value = lr.Value;
						}
					}

#if EMPTYRES
					if (!emptyResource)
					{
#endif
					sheet.Cells[row, COL1_IDX__RESX_SRC].Value = r.FileSource;
					sheet.Cells[row, COL2_IDX__RESX_DEST].Value = r.FileDestination;
					sheet.Cells[row, COL3_IDX__KEY].Value = r.Key;
					sheet.Cells[row, COL4_IDX__VALUE].Value = r.Value;
					sheet.Cells[row, COL5_IDX__COMMENT].Value = r.Comment;

					row++;
#if EMPTYRES
					}
#endif
				}

				sheet.Cells["A1:Z1"].AutoFitColumns();

				// Save the Workbook and quit Excel.
				app.Save();
			}
		}

		private void DataSetToXml(ResxData rd, string fileName)
		{
			Console.WriteLine("Creating Xml...");
			using (XmlWriter xw = XmlWriter.Create(fileName, new XmlWriterSettings() { Encoding = new UTF8Encoding(false), Indent = true }))
			{
				int row = DATA_ROWS_OFFSET;

				xw.WriteStartElement("Translations");

				xw.WriteStartElement("Worksheet");
				xw.WriteAttributeString("Name", "Localize");

				DataView dw = rd.Resx.DefaultView;
				dw.Sort = "FileSource, Key";
				foreach (DataRowView drw in dw)
				{
					ResxData.ResxRow r = (ResxData.ResxRow)drw.Row;
					xw.WriteStartElement("Message");

					xw.WriteElementString("ResourceFile", r.FileSource);
					xw.WriteElementString("ResourceKey", r.Key);
					xw.WriteElementString("Text", r.Value);
					xw.WriteElementString("Comment", r.Comment);

					xw.WriteStartElement("Translation");
					foreach (ResxData.ResxLocalizedRow lr in r.GetResxLocalizedRows())
					{
						if (string.IsNullOrEmpty(lr.Value))
							continue;

						xw.WriteStartElement("Text");
						xw.WriteAttributeString("Culture", lr.Culture);
						xw.WriteValue(lr.Value);
						xw.WriteEndElement();
					}
					xw.WriteEndElement();

					xw.WriteEndElement();
				}

				xw.WriteEndElement();
				xw.WriteEndElement();
			}
		}

		private string[] GetCulturesFromDataSet(ResxData rd)
		{
			if (rd.ResxLocalized.Rows.Count > 0)
			{
				ArrayList list = new ArrayList();
				foreach (ResxData.ResxLocalizedRow r in rd.ResxLocalized.Rows)
				{
					if (!string.IsNullOrEmpty(r.Culture)
						&& list.IndexOf(r.Culture) < 0)
					{
						list.Add(r.Culture);
					}
				}

				string[] cultureList = new string[list.Count];

				int i = 0;
				foreach (string c in list)
				{
					cultureList[i] = c;
					i++;
				}

				return cultureList;
			}
			else
				return null;
		}

		public static string JustStemWithFullPath(string cPath)
		{
			//Get the name of the file
			string lcFileName = cPath.Trim();

			//Remove the extension and return the string
			if (lcFileName.IndexOf(".") == -1)
				return lcFileName;
			else
				return lcFileName.Substring(0, lcFileName.LastIndexOf('.'));
		}

		public static string JustStem(string cPath)
		{
			//Get the name of the file
			string lcFileName = JustFName(cPath.Trim());

			//Remove the extension and return the string
			if (lcFileName.IndexOf(".") == -1)
				return lcFileName;
			else
				return lcFileName.Substring(0, lcFileName.LastIndexOf('.'));
		}

		public static string JustFName(string cFileName)
		{
			//Create the FileInfo object
			FileInfo fi = new FileInfo(cFileName);

			//Return the file name
			return fi.Name;
		}

		public static string AddBS(string cPath)
		{
			if (cPath.Trim().EndsWith("\\"))
				return cPath.Trim();
			else
				return cPath.Trim() + "\\";
		}
	}
}