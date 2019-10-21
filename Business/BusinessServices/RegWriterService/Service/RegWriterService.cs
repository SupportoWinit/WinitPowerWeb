using Business.BusinessServices.RegWriterService.Classes.Writer;
using Business.BusinessServices.RegWriterService.Interfaces.Writer;
using System.Collections.Generic;
using Common.Properties;
using System;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Web;

namespace Business.BusinessServices.RegWriterService.Service
{

    /// <summary>
    /// Servizio che si occupa della scrittura delle reg(txt solo al momento).
    /// Da integrare dependency injection traamite framework
    /// </summary>
    /// <seealso cref="Business.BusinessServices.RegWriterService.IRegWriterService" />
    public class RegWriterService : IRegWriterService
    {
        IWriter _regWriter;
        IWriter _jsonWriter;

        public RegWriterService() : this(
            new TxtWriter(Settings.Default.Files_Input_Path, $"{Settings.Default.RegFile}" + "_{0}_{1}-{2}-{3}-{4}-{5}-{6}{7}"),
            new TxtWriter(Settings.Default.Files_Input_JSON_Backup_Path, Settings.Default.JsonBackupFileNamePattern)
            )
        {

        }

        public RegWriterService(IWriter regWriter, IWriter jsonWriter)
        {
            _regWriter = regWriter;
            _jsonWriter = jsonWriter;
        }
        
        public void WriteRegs(IEnumerable<string> regs,string deviceCode)
        {
            _regWriter.Write(regs, deviceCode);
        }

        public void BackUpJsonRegs(IEnumerable<object> regs, string deviceCode)
        {
            var jsonArray = JArray.FromObject(regs);
            var stringArray = jsonArray.Select(json => json.ToString()).ToList();

            _jsonWriter.Write(stringArray, deviceCode);
        }

        public void WriteJsonRegsToFilesInput(IEnumerable<string> regs)
        {
            File.WriteAllLines(Path.Combine(HttpContext.Current.Server.MapPath(Settings.Default.Files_Input_Path), $"Registrations_{DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss.fff")}.json"), regs);
        }
    }
}
