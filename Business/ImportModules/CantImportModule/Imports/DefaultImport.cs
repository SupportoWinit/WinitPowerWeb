using Business.ImportModules;
using Business.Repository;
using Common;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business.IocFactory.Import.ImportCantieri.Imports
{
    /*
     * Classe da verificare e sistemare (inserire log e altro).
     * Ricordarsi prima di metterla in produzione
     */

    public class DefaultImport : IImport
    {
        private readonly ILog _log;

        public DefaultImport(ILog log)
        {
            _log = log;
        }

        public IDictionary<string, string> Import()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, string> Import(string[] inputCants, int filId)
        {

            IDictionary<string, string> errors = new Dictionary<string, string>();

            // inizializzazione del dizionario che permette di recuperare il posizionamento delle colonne
            Dictionary<string, int> columnsNumber = null;

            StringBuilder errorFileds = new StringBuilder();

            ICollection<Cant> cantToInsert = new List<Cant>();
            
            #region INTESTAZIONE 

            #region SALVATAGGIO INTESTAZIONE

            String[] splittedRow = inputCants.First().Split(new Char[] { ';' }, StringSplitOptions.None);
            var columns = splittedRow.Count();
            var emptyRow = new StringBuilder();
            for (var i = 0; i < columns - 1; i++)
                emptyRow.Append(";");


            columnsNumber = new Dictionary<string, int>();
            splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).ToArray();
            columnsNumber.Add("CODICE", Array.IndexOf(splittedRow, "CODICE"));
            columnsNumber.Add("DESCRIZIONE", Array.IndexOf(splittedRow, "DESCRIZIONE"));
            columnsNumber.Add("CODICE CLIENTE", Array.IndexOf(splittedRow, "CODICE CLIENTE"));
            columnsNumber.Add("DESCRIZIONE CLIENTE", Array.IndexOf(splittedRow, "DESCRIZIONE CLIENTE"));
            columnsNumber.Add("MATRICOLA FRU", Array.IndexOf(splittedRow, "MATRICOLA FRU"));
            columnsNumber.Add("DATA ASSOCIAZIONE", Array.IndexOf(splittedRow, "DATA ASSOCIAZIONE"));
            columnsNumber.Add("FILIALE", Array.IndexOf(splittedRow, "FILIALE"));
            columnsNumber.Add("COMUNE", Array.IndexOf(splittedRow, "COMUNE"));
            columnsNumber.Add("VIA", Array.IndexOf(splittedRow, "VIA"));
            columnsNumber.Add("LATITUDINE", Array.IndexOf(splittedRow, "LATITUDINE"));
            columnsNumber.Add("LONGITUDINE", Array.IndexOf(splittedRow, "LONGITUDINE"));
            columnsNumber.Add("NOTE", Array.IndexOf(splittedRow, "NOTE"));



            if (columnsNumber.ContainsValue(-1))
            {

                var errorSb = new StringBuilder();
                foreach (KeyValuePair<string, Int32> kvp in columnsNumber.Where(kvp => kvp.Value == -1))
                {
                    if (kvp.Key.Equals("CODICE") || kvp.Key.Equals("DESCRIZIONE") || kvp.Key.Equals("MATRICOLA FRU") || kvp.Key.Equals("DATA ASSOCIAZIONE"))
                    {
                        errorSb.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_TESTATA_X_COLONNA, kvp.Key.ToString()));
                    }
                }

                if (errorSb.ToString() != String.Empty)
                    throw new InvalidOperationException(errorSb.ToString());

                columnsNumber = null;
            }

            #endregion

            #endregion

            int rowNumber = 0;

            // per ogni riga del csv passato come parametro
            foreach (String stringVar in inputCants.Skip(1).ToArray())
            {
                rowNumber++;
                emptyRow.Clear();

                String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);
                for (var i = 0; i < columns - 1; i++)
                    emptyRow.Append(";");

                //Segnala errore se manca un'intera riga
                if (stringVar == emptyRow.ToString() || splittedLine.Length <= 1)
                {
                    errors.Add("Riga mancante", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RIGA_X_VUOTA, rowNumber.ToString()));
                    continue;
                }
                
                #region ESTRAZIONE DATI

                string codice_Can = splittedLine[columnsNumber["CODICE"]].Trim();
                string descrizione_Can = splittedLine[columnsNumber["DESCRIZIONE"]].Trim();
                string cod_Cliente = splittedLine[columnsNumber["CODICE CLIENTE"]].Trim();
                string descrizione_Cli = splittedLine[columnsNumber["DESCRIZIONE CLIENTE"]].Trim();
                string fru_matr = splittedLine[columnsNumber["MATRICOLA FRU"]];
                string filiale = splittedLine[columnsNumber["FILIALE"]];
                DateTime? data_associazione = (splittedLine[columnsNumber["DATA ASSOCIAZIONE"]] != "") ? DateTime.Parse(splittedLine[columnsNumber["DATA ASSOCIAZIONE"]].Trim()) as DateTime? : null;
                string comune = splittedLine[columnsNumber["COMUNE"]].Trim();
                string via = splittedLine[columnsNumber["VIA"]].Trim();
                string latit = splittedLine[columnsNumber["LATITUDINE"]].Trim();
                string longi = splittedLine[columnsNumber["LONGITUDINE"]].Trim();
                string note = splittedLine[columnsNumber["NOTE"]].Trim();

                #endregion
                
                Cant cantAlreadyExist = RepoManager.CantRepo.FirstOrDefault(c => c.Codice_Cantiere.Trim() == codice_Can.Trim());

                if (cantAlreadyExist != null)
                    errors.Add($"Cantiere {cantAlreadyExist.Codice_Cantiere}", $"Il cantiere con codice {cantAlreadyExist.Codice_Cantiere} è già stato importato!");
                else
                {
                    if (!String.IsNullOrEmpty(codice_Can) && !String.IsNullOrEmpty(descrizione_Can))     //Obbligatorio cod,desc
                    {
                        Cant cantiere = RepoManager.CantRepo.Init();

                        cantiere.Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codice_Can, 20);
                        cantiere.Descrizione_Can = descrizione_Can;
                        cantiere.Tipologia_Can = "CAN";
                        cantiere.Indirizzo_Can = (via.Length < 50) ? via : via.Substring(0, 49);
                        cantiere.LatitudineGps_Can = (latit != "") ? Double.Parse(latit) : 0;
                        cantiere.LongitudineGps_Can = (longi != "") ? Double.Parse(longi) : 0;
                        cantiere.Note_Can = note;

                        #region FILIALE

                        if (!String.IsNullOrEmpty(filiale))
                            cantiere.Fil = RepoManager.FilRepo.FirstOrDefault(f => f.Descrizione_Fil == filiale);

                        #endregion

                        #region COMUNE

                        if (!String.IsNullOrEmpty(comune))
                        {
                            Tab_Comuni exist = RepoManager.Tab_ComuniRepo.FirstOrDefault(x => x.Luogo_Tab_Comuni == comune);

                            cantiere.Luogo_Can = (exist != null) ? comune.Trim() : "";
                            cantiere.Cap_Can = (exist != null) ? exist.Cap_Tab_Comuni : "";
                            cantiere.Provincia_Can = (exist != null) ? exist.Codice_Prov_Tab_Comuni : "";
                        }

                        #endregion

                        #region CLIENTE

                        if (!String.IsNullOrEmpty(cod_Cliente) && !String.IsNullOrEmpty(descrizione_Cli))
                        {

                            Cli exCli = RepoManager.CliRepo.FirstOrDefault(c => c.Codice_Cliente.Trim() == cod_Cliente.Trim());
                            if (exCli == null)
                            {
                                Cli cliente = RepoManager.CliRepo.Init();
                                cliente.Codice_Cliente = CommonService.AggiungiSpaziASinistraSeStringaNumerica(cod_Cliente, 10);
                                cliente.Cognome_Cli = descrizione_Cli;
                                cantiere.Cli = cliente;
                            }
                            else
                            {
                                cantiere.Cli = exCli;
                            }

                        }

                        #endregion

                        #region ASSOCIAZIONE FRU

                        if (!String.IsNullOrEmpty(fru_matr) && (fru_matr.Length == 10) && data_associazione.HasValue)
                        {
                            Fru currentFru = RepoManager.FruRepo.FirstOrDefault(f => f.Codice_Fru.Trim() == fru_matr);

                            Fru_Cant fruCant = RepoManager.Fru_CantRepo.Init();

                            if (currentFru != default(Fru))
                            {
                                fruCant.Fru = currentFru;
                                fruCant.Abilitazione_Data_Inizio_Fru_Can = data_associazione.Value;
                            }
                            else
                            {
                                Fru newFru = RepoManager.FruRepo.Init();

                                newFru.DataOraUltimaModifica_Fru = DateTime.Now;
                                newFru.N_Serie_Fru = fru_matr;
                                newFru.Codice_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(fru_matr, 5);
                                fruCant.Abilitazione_Data_Inizio_Fru_Can = (DateTime)data_associazione;

                                fruCant.Fru = newFru;
                            }

                            cantiere.Fru_Cant.Add(fruCant);
                        }

                        #endregion

                        cantToInsert.Add(cantiere);

                    }
                }
            }

            #region INSERIMENTO E SALVATAGGIO

            try
            {
                RepoManager.CantRepo.DbSet.AddRange(cantToInsert);
                RepoManager.CantRepo.BulkSaveChanges(bulk => bulk.BatchSize = 100);
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante l'inserimento dei cantieri da CSV a causa dell'exception {0}", ex.Message);
                errors.Add("Errore", "Errore durante l'inserimento dei cantieri! Contattare l'assistenza!");
            }

            #endregion

            return errors;

        }


    }
}

