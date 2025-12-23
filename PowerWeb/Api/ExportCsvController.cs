using Business.Repository;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Web;
using DevExpress.XtraRichEdit.Fields.Expression;
using UnityEngine;

namespace PowerWeb.Api
{
    public class ExportCsvController : GenericUploadFileApi
    {

        /// <summary>
        /// Il separatore tra un valore e l'altro del file delle registrazioni
        /// </summary>
        private const string RegsFileSeparator = ";";

        /// <summary>
        /// Il nome del file utilizzato per effettuare i controlli di esecuzione esclusiva della web api
        /// </summary>
        private const string ExclusiveAccessFileName = "apiLock.tmp";

        /// <summary>
        /// Esegue l'operazione di import oggetto della API.
        /// </summary>
        protected override HttpStatusCode ExecuteOperation()
        {
            //inizializzo la variaible per tornare indietro di esattamente sette giorni
            DateTime from = DateTime.Now.AddDays(-15);
            // calcolo del nome file di destinazione 
            string Name = "DATI.csv";
            string tmpName = "tmpDATI.csv";
            // al termine dell'operazione di preparazione della scrittura, si scrive un nuovo file delle timbrature
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Output_Csv_Path.Replace("~", "").Replace("\\", "").Replace("--", "\\"));
            string tmpFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Common.Properties.Settings.Default.Files_Output_Csv_Path.Replace("~", "").Replace("\\", "").Replace("--", "\\"));
            string fileName = Path.Combine(tmpFilePath, tmpName);
            string currentFileName = Path.Combine(filePath,Name);
            if (File.Exists(currentFileName)) 
            {
                File.Delete(currentFileName);
            }
            // calcolo del nome del file utilizzato per l'esecuzione esclusiva delle operazioni
            string exclusiveAccessFilePath = Path.Combine(tmpFilePath, tmpName);
            // si procede all'elaborazione solamente se il file non esiete
            if (!File.Exists(exclusiveAccessFilePath))
            {
                // viene generato il file di lock della api
                using (FileStream exclusiveExecutionFile = File.Create(exclusiveAccessFilePath))
                {
                    exclusiveExecutionFile.Close();
                    exclusiveExecutionFile.Dispose();
                }
                try
                {
                    //inizializzo le liste per le regstrazioni e per insrire le varie stringhe da inserire nel file
                    List<String> returnList = new List<string>();
                    List<Reg_V> exportRegs = new List<Reg_V>();
                    //recupero tutte le registrazioni degli ultimi 7 giorni
                    exportRegs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Data_Ora_Fis_E > from).ToList();
                    //metto le timbrature in ordine secondo la data e le raggruppo per collaboratori
                    var regsByCol = exportRegs.OrderBy(r => r.Data_Ora_Fis_E).GroupBy(r => r.Col_Id);
                    foreach (var regs in regsByCol) {
                        //se regs.Key è diverso da null vuol dire che il collaboratore è associato correttamente alle timbrature
                        if (regs.Key != null) {
                            Col collaboratore = RepoManager.ColRepo.Single(c => c.Col_Id == regs.Key);
                            foreach (var reg in regs)
                            {
                                //se reg.Cant_Id è diverso da null vuol dire che il cantiere è associato correttamente alla timbratura
                                if (reg.Cant_Id != null) {
                                    Cant cantiere = RepoManager.CantRepo.Single(c => c.Cant_Id == reg.Cant_Id);
                                    string txtReg = "";
                                    string month = reg.Data_Ora_Fis_E.Month.ToString();
                                    if (reg.Data_Ora_Fis_E.Month < 10) {
                                        month = "0" + month;
                                    }
                                    string day = reg.Data_Ora_Fis_E.Day.ToString();
                                    if (reg.Data_Ora_Fis_E.Day < 10) {
                                        day = "0" + day;
                                    }
                                    string oraE = "" + reg.Data_Ora_Fis_ETime.Value.Hours + ":" + reg.Data_Ora_Fis_ETime.Value.Minutes;
                                    if (reg.Data_Ora_Fis_ETime.Value.Hours < 10 && reg.Data_Ora_Fis_ETime.Value.Minutes >= 10)
                                    {
                                        oraE = "0" + reg.Data_Ora_Fis_ETime.Value.Hours + ":" + reg.Data_Ora_Fis_ETime.Value.Minutes;
                                    }
                                    else if (reg.Data_Ora_Fis_ETime.Value.Hours >= 10 && reg.Data_Ora_Fis_ETime.Value.Minutes < 10)
                                    {
                                        oraE = "" + reg.Data_Ora_Fis_ETime.Value.Hours + ":0" + reg.Data_Ora_Fis_ETime.Value.Minutes;
                                    }
                                    else if (reg.Data_Ora_Fis_ETime.Value.Hours < 10 && reg.Data_Ora_Fis_ETime.Value.Minutes < 10)
                                    {
                                        oraE = "0" + reg.Data_Ora_Fis_ETime.Value.Hours + ":0" + reg.Data_Ora_Fis_ETime.Value.Minutes;
                                    }
                                    if (reg.RegU != null && reg.RegU.Value > 0)
                                    {
                                        string oraU = "" + reg.Data_Ora_Fis_UTime.Value.Hours + ":" + reg.Data_Ora_Fis_UTime.Value.Minutes;
                                        if (reg.Data_Ora_Fis_UTime.Value.Hours < 10 && reg.Data_Ora_Fis_UTime.Value.Minutes >= 10)
                                        {
                                            oraU = "0" + reg.Data_Ora_Fis_UTime.Value.Hours + ":" + reg.Data_Ora_Fis_UTime.Value.Minutes;
                                        }
                                        else if (reg.Data_Ora_Fis_UTime.Value.Hours >= 10 && reg.Data_Ora_Fis_UTime.Value.Minutes < 10)
                                        {
                                            oraU = "" + reg.Data_Ora_Fis_UTime.Value.Hours + ":0" + reg.Data_Ora_Fis_UTime.Value.Minutes;
                                        }
                                        else if (reg.Data_Ora_Fis_UTime.Value.Hours < 10 && reg.Data_Ora_Fis_UTime.Value.Minutes < 10)
                                        {
                                            oraU = "0" + reg.Data_Ora_Fis_UTime.Value.Hours + ":0" + reg.Data_Ora_Fis_UTime.Value.Minutes;
                                        }
                                        string durata = "";
                                        if (reg.Durata_Fig / 60 >= 10)
                                        {
                                            durata += reg.Durata_Fig / 60;
                                        }
                                        else {
                                            durata += "0" + reg.Durata_Fig / 60;
                                        }
                                        if (reg.Durata_Fig % 60 >= 10)
                                        {
                                            durata = durata + ":" + (reg.Durata_Fig % 60).ToString();
                                        }
                                        else {
                                            durata = durata + ":0" + (reg.Durata_Fig % 60).ToString();
                                        }

                                        txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}"
                                            , RegsFileSeparator
                                            , collaboratore.Codice_Collaboratore
                                            , collaboratore.N_Pos_INAIL_Col
                                            , collaboratore.Matricola_Col
                                            , reg.RegE
                                            , reg.Data_Ora_Fis_E.Year
                                            , month
                                            , day
                                            , oraE
                                            , oraU
                                            , durata
                                            , cantiere.Codice_Gestionale_Can
                                            );
                                    }
                                    else {
                                        txtReg = string.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{0}{0}{9}{0}"
                                            , RegsFileSeparator
                                            , collaboratore.Codice_Collaboratore
                                            , collaboratore.N_Pos_INAIL_Col
                                            , collaboratore.Matricola_Col
                                            , reg.RegE
                                            , reg.Data_Ora_Fis_E.Year
                                            , month
                                            , day
                                            , oraE
                                            , cantiere.Codice_Gestionale_Can
                                            );
                                    }
                                    returnList.Add(txtReg);
                                }
                            }
                        }
                    }
                    File.WriteAllLines(fileName, returnList);
                    File.Move(tmpFilePath + "\\"+tmpName, filePath + "\\"+Name);
                }
                catch (Exception e) {
                    return HttpStatusCode.ServiceUnavailable;
                }
                finally {
                    // al termine dell'operazione comunque si elimina il file
                    File.Delete(exclusiveAccessFilePath);
                }
            }
            else
            {
                return HttpStatusCode.ServiceUnavailable;
            }
            return HttpStatusCode.OK;
        }
    }
}