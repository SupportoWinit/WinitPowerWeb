
using Ionic.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Exports
{
    public abstract class TxtToolBox : IExport
    {
        #region Public Properties

        /// <summary>
        /// Directory dedotta automaticamente che serve come cartella temporanea per il salvataggio dei file durante l'esportazione
        /// </summary>
        public string DownloadPath
        {
            get
            {
                return String.Format(@"{0}{1}", AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Output_Path);
            }
        }

        public List<string> TxtLines { get; set; }

        public List<object> Errors { get; set; }

        public DateTime ExportDate { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string Extension { get; set; }

        public string FileName { get; set; }

        public object[] GridColumns { get; set; }

        public string[] GridFilter { get; set; }

        public bool IsToZip { get; set; }

        public bool CentHours { get; set; }

        public string ModelFilePath { get; set; }

        public int[] SelectedIds { get; set; }

        

        #endregion

        #region Costruttori

        public TxtToolBox()
        {
            TxtLines = new List<string>();
            Errors = new List<object>();
        }

        #endregion

        #region Public Methods

        public abstract void LaunchExport();

        public void SaveToFileSystem()
        {
            string filePath = string.Format("{0}{1}.{2}", DownloadPath, FileName, Extension);

            TextWriter tw = new StreamWriter(filePath);

            foreach (String s in TxtLines) 
            {
                if (ModelFilePath.Contains("RilPre"))
                    tw.Write(s);
                else
                    tw.WriteLine(s);
            }
                

            tw.Close();

            if (IsToZip)
            {
                Extension = "zip";

                using (ZipFile zipFile = new ZipFile())
                {
                    zipFile.AddFile(filePath, @"\");

                    zipFile.Save(string.Format("{0}{1}.{2}", DownloadPath, FileName, Extension));

                    File.Delete(filePath);
                }
            }
        }

        #endregion
    }
}
