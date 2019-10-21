using Business.DataClasses.GeoBadgeDTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Business.RegFileCreators
{
    public class GeoBadgeRegFileCreator : IRegFileCreator<GeoBadgeReg>
    {
        string fileNamePatter;
        public GeoBadgeRegFileCreator()
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
        /// <summary>
        /// Effettua un parsing delle timbrature e produce un txt regolare effettuando distinzione 
        /// tra GPS e non.
        /// Dato che GeoBadge non fornisce un flag di distinzione tra le varie tipologie di timbrature non posso utilizzare la strategy-pattern
        /// quindi è una catena di if.
        /// </summary>
        /// <param name="unEncodedRegs">Un array di coordinate da elaborare</param>
        public void WriteToFile(IEnumerable<GeoBadgeReg> unEncodedRegs)
        {
            if (unEncodedRegs == null || !unEncodedRegs.Any())
                return;

            var regsToWrite = new List<string>();
            
            foreach (var geoReg in unEncodedRegs)
            {
                string regRow = "";

                #region Reg con coordinate

                if (!String.IsNullOrEmpty(geoReg.Coordinate))
                {

                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                        geoReg.CodicePru,
                        GetLatitude(geoReg.Coordinate),
                        geoReg.Registrazione_Data_Ora_Orig.Year,
                        geoReg.Registrazione_Data_Ora_Orig.Month.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Day.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Hour.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Minute.ToString("00"),
                        0,
                        0,
                        "N"
                    );

                    regsToWrite.Add(regRow);

                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};;",
                        geoReg.CodicePru,
                        GetLongitude(geoReg.Coordinate),
                        geoReg.Registrazione_Data_Ora_Orig.Year,
                        geoReg.Registrazione_Data_Ora_Orig.Month.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Day.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Hour.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Minute.ToString("00"),
                        0,
                        1,
                        "E"
                    );
                    regsToWrite.Add(regRow);
                }

                #endregion

                else

                #region Reg senza coordinate

                {

                    regRow = String.Format("{0};{1};{2};{3};{4};{5};{6};",
                        geoReg.CodicePru,
                        geoReg.CodiceFru,
                        geoReg.Registrazione_Data_Ora_Orig.Year,
                        geoReg.Registrazione_Data_Ora_Orig.Month.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Day.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Hour.ToString("00"),
                        geoReg.Registrazione_Data_Ora_Orig.Minute.ToString("00")
                    );
                    regsToWrite.Add(regRow);
                }

                

                #endregion

            }


            File.WriteAllLines(fileNamePatter, regsToWrite.ToArray());

        }

        /// <summary>
        /// Effettua un parsing delle coordinate e restituisce
        /// una stringa di 10 caratteri (viene sempre messo uno 0 in testa e tanti zeri
        /// quanto servono per arrivare a 10) contenente la latitudine.
        /// Viene rimossa la virgola
        /// </summary>
        /// <param name="coordinates">La stringa da parsare nella pattern 10,00000 11,000000</param>
        /// <returns></returns>
        private string GetLatitude(string coordinates)
        {
            string latitude = coordinates.Split(' ').First();

            if (latitude == "0")
                return "0000000000";

            int prefix = latitude.Substring(0, latitude.IndexOf(',')).Count();

            latitude = latitude.Replace(",", "");

            if (prefix == 2)
            {
                latitude = $"0{latitude}";
            }
            else if (prefix == 1)
            {
                latitude = $"00{latitude}";
            }

            if (latitude.Count() > 10)
                latitude = new string(latitude.Take(10).ToArray());

            else if (latitude.Count() < 10)
                latitude = latitude.PadRight(10, '0');

            return latitude;
        }


        /// <summary>
        /// Effettua un parsing delle coordinate e restituisce
        /// una stringa di 10 caratteri (viene sempre messo uno 0 in testa e tanti zeri
        /// quanto servono per arrivare a 10) contenente la longitudine.
        /// Viene rimossa la virgola.
        /// </summary>
        /// <param name="coordinates">La stringa da parsare nella pattern 10,00000 11,000000</param>
        /// <returns></returns>
        private string GetLongitude(string coordinates)
        {
            string longitude = coordinates.Split(' ').Last();

            if (longitude == "0")
                return "0000000000";

            int prefix = longitude.Substring(0, longitude.IndexOf(',')).Count();

            longitude = longitude.Replace(",", "");

            if (prefix == 2)
            {
                longitude = $"0{longitude}";
            }
            else if (prefix == 1)
            {
                longitude = $"00{longitude}";
            }


            if (longitude.Count() > 10)
                longitude = new string(longitude.Take(10).ToArray());

            else if (longitude.Count() < 10)
                longitude = longitude.PadRight(10, '0');

            return longitude;
        }
    }


}
