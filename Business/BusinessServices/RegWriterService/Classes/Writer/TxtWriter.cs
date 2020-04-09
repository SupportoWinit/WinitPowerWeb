using Business.BusinessServices.RegWriterService.Interfaces.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace Business.BusinessServices.RegWriterService.Classes.Writer
{
    public class TxtWriter : IWriter
    {
        private string _destinationPath;
        private string DestinationPath => HttpContext.Current.Server.MapPath(_destinationPath);


        private string _fileNamePattern;
        private string TxtFullPath
        {
            get
            {
                // calcolo del nome file di destinazione 
                string txtFileName = String.Format(_fileNamePattern
                    , "{0}"
                    , DateTime.Now.Year
                    , DateTime.Now.Month
                    , DateTime.Now.Day
                    , DateTime.Now.Hour
                    , DateTime.Now.Minute
                    , DateTime.Now.Second
                    , ".txt");


                return Path.Combine(DestinationPath, txtFileName);
            }
        }


        public TxtWriter(string destinationPath, string fileNamePattern)
        {
            _destinationPath = destinationPath;
            _fileNamePattern = fileNamePattern;
        }

        public void Write(IEnumerable<string> regs, string device)
        {

            string path = DestinationPath;

            if (!CheckPath(path))
                CreateMissingPath(path);

            string compiledPath = String.Format(TxtFullPath, device); //Compilato il percorso di scrittura

            File.WriteAllLines(compiledPath, regs);

        }

        private bool CheckPath(string path)
        {
            return Directory.Exists(path);
        }
        private void CreateMissingPath(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}
