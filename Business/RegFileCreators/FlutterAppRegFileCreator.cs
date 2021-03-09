using Business.DataClasses.FlutterAppDTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Business.RegFileCreators
{
    class FlutterAppRegFileCreator : IRegFileCreator<FlutterAppReg>
    {
        string fileNamePatter;

        public FlutterAppRegFileCreator()
        {
            fileNamePatter = String.Format("{0}{1}_{2}-{3}-{4}_{5}-{6}-{7}.txt",
                HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.Files_Input_Path),
                Common.Properties.Settings.Default.RegFile,
                DateTime.Now.Year,
                DateTime.Now.Month.ToString("00"),
                DateTime.Now.Day.ToString("00"),
                DateTime.Now.Hour.ToString("00"),
                DateTime.Now.Minute.ToString("00"),
                 DateTime.Now.Second.ToString("00")
                );
        }

        public void WriteToFile(IEnumerable<FlutterAppReg> unEncodedRegs)
        {
            if (unEncodedRegs == null || !unEncodedRegs.Any())
                return;

            var regsToWrite = new List<string>();

            foreach (var fluReg in unEncodedRegs)
            {
                string regRow = "";                

                #region Reg senza coordinate


                regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                    fluReg.CodiceFru,
                    fluReg.Value.First().CodicePru,
                    fluReg.Value.First().Registrazione_Data_Ora_Orig.Year,
                    fluReg.Value.First().Registrazione_Data_Ora_Orig.Month.ToString("00"),
                    fluReg.Value.First().Registrazione_Data_Ora_Orig.Day.ToString("00"),
                    fluReg.Value.First().Registrazione_Data_Ora_Orig.Hour.ToString("00"),
                    fluReg.Value.First().Registrazione_Data_Ora_Orig.Minute.ToString("00"),
                    fluReg.Value.First().verso,
                    "[Motivazione]="+ fluReg.Value.First().motivazione
                );
                regsToWrite.Add(regRow);


                #endregion

            }

            File.WriteAllLines(fileNamePatter, regsToWrite.ToArray());
        }
    }
}
