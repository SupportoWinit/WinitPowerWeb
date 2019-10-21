using Business.ImportModules;
using Business.Repository;
using Common;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business.BusinessServices.ImportCustomerService.CantCustomerImport
{
    public class MosaicoImport : ICustomerImport
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MosaicoImport));

        

        public IEnumerable<KeyValuePair<string, string>> Import(IEnumerable<string> inputCants)
        {

            IDictionary<string, string> errors = new Dictionary<string, string>();
            int filId = 0;
            Fil fil = null;  /*RepoManager.FilRepo.First(f => f.Fil_Id == filId);*/

            //trattamento del caso di Import CUSTOM x MOSAICO
            List<Cant> cants = RepoManager.CantRepo.GetAll(true).ToList();
            List<Cant> toAddCants = new List<Cant>();
            //inizializzo la lista dei cant var
            List<Cant_Var> toAddCant_Vars = new List<Cant_Var>();
            List<Cant_Var> toDeleteCant_Vars = new List<Cant_Var>();

            Dictionary<string, Int32> columnsNumber = null;

            // inizializzazione dei valori utilizzati per la visualizzazione dei messaggi a video (barra di avanzamento)
            double nRec = inputCants.Count();
            double countRec = 0;
            double percRec = 0;
            int rownumber = 1;
            var errorFileds = new StringBuilder();

            //Loop di trattamento dei Record presenti nel file di input
            foreach (String stringVar in inputCants)
            {
                // aggiornamento dei dati di percentuale e numero record elaborati
                countRec = countRec + 1;
                percRec = (countRec / nRec) * 100;

                // inserimento del messaggio di stato dell'elaborazione
                BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec,
                    BusinessService.GetLocalizedString(PowerWebResources.STR_IMPORT_RECORD_X_DI_Y.ToString(), countRec.ToString(), nRec.ToString()));

                //divido la stringa in ogni valore
                String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);

                //determino in base al numero di campi come è rappresentata una riga vuota
                var columns = splittedLine.Count();
                var emptyRow = new StringBuilder();
                for (var i = 0; i < columns - 1; i++)
                    emptyRow.Append(";");

                //Segnala errore se manca un'intera riga
                if (stringVar == emptyRow.ToString() || splittedLine.Length <= 1)
                {
                    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RIGA_X_VUOTA, rownumber.ToString())); //String.Format("Non è presente nessun Valore nella riga", rownumber));
                    throw new System.InvalidOperationException(errorFileds.ToString());
                }

                //controllo uniformità della testata
                if (columnsNumber == null)
                {
                    //columnsNumber = get_Columns_Numbers(stringVar);
                }
                else
                {

                    #region CONTROLLO UNIFORMITA' CAMPI

                    //passo a controllare tutti i campi della riga
                    string codVouch = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                    string codFisc = splittedLine[columnsNumber["CODICE FISCALE"]].Trim().ToUpper();
                    string cognome = splittedLine[columnsNumber["COGNOME"]].Trim();
                    string nome = splittedLine[columnsNumber["NOME"]].Trim();
                    string indirizzo = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                    string cap = splittedLine[columnsNumber["CAP"]].Trim();
                    string comune = splittedLine[columnsNumber["COMUNE"]].Trim();
                    string distretto = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                    string profiloIniz = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                    string ini = splittedLine[columnsNumber["INI"]].Trim();
                    DateTime dateIni = new DateTime();


                    if (codVouch == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_VOUCHER_X_VUOTA, rownumber.ToString()));
                    //nel caso in cui il codice fiscale sia vuoto
                    if (codFisc == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CODICE_FISCALE_X_VUOTA, rownumber.ToString()));
                    //nel caso in cui il codice fiscale sia lungo meno di 16
                    else if (!CommonService.ECodiceFiscaleValido(codFisc))
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CODICE_FISCALE_X_ERRATO, rownumber.ToString()));

                    if (cognome == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COGNOME_X_VUOTA, rownumber.ToString()));

                    if (nome == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_NOME_X_VUOTA, rownumber.ToString()));

                    if (indirizzo == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_INDIRIZZO_X_VUOTA, rownumber.ToString()));

                    if (cap == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAP_X_VUOTA, rownumber.ToString()));

                    if (comune == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COMUNE_X_VUOTA, rownumber.ToString()));

                    if (distretto == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DISTRETTO_X_VUOTA, rownumber.ToString()));

                    if (profiloIniz == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PROFILO_X_VUOTA, rownumber.ToString()));

                    if (ini == string.Empty)
                        errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DATA_INIZIALE_X_VUOTA, rownumber.ToString()));
                    else
                    {
                        //controllo uniformità data
                        if (!DateTime.TryParse(splittedLine[columnsNumber["INI"]].Trim(), out dateIni))
                            errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DATA_INIZIALE_X_FORMATO_CORRETTO, rownumber.ToString()));

                        #endregion

                        if (errorFileds.ToString() == String.Empty)
                        {
                            if (cants.FirstOrDefault(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == filId) != null || toAddCants.FirstOrDefault(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == filId) != null)
                            {
                                //caso in cui il cantiere esiste già, devo quindi verificare se è più recente ed interagire con Cant_Var
                                Cant currentCant;
                                if (cants.FirstOrDefault(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == filId) != null)
                                    currentCant = cants.First(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == filId);
                                else
                                    currentCant = toAddCants.First(cant => cant.Cod_Fisc_Can == codFisc && cant.Fil_Id == filId);

                                //caso in cui la data di ultima modifica sia uguale
                                //aggiorno il record nella tabella Cant
                                if (currentCant.DataOraUltimaModifica_Can.ToShortDateString() == dateIni.ToShortDateString())
                                {
                                    // viene salvato il cant_var all'interno dell'elenco dei cant var
                                    Cant_Var newCant_Var = RepoManager.Cant_VarRepo.Init();

                                    CommonService.DuplicateEntity(currentCant, newCant_Var);
                                    toAddCant_Vars.Add(newCant_Var);

                                    int newCliId = 0; // GetCodCli(columnsNumber, splittedLine, fil);

                                    if (newCliId != 0)
                                        currentCant.Cli_Id = newCliId;
                                    currentCant.Fil_Id = filId;
                                    currentCant.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                    currentCant.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                    currentCant.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                    currentCant.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                    currentCant.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                    currentCant.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                    currentCant.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                    currentCant.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                    currentCant.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                    currentCant.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                }
                                else if (currentCant.DataOraUltimaModifica_Can > dateIni)
                                {
                                    //caso in cui il record presente in cant è più aggiornato
                                    //vado ad aggiungere o modificare i record in Cant_Var
                                    var cantsvar = RepoManager.Cant_VarRepo.Find(cv => cv.Cod_Fisc_Can == codFisc && cv.Fil_Id == filId && cv.DataOraUltimaModifica_Can.Year == dateIni.Year && cv.DataOraUltimaModifica_Can.Month == dateIni.Month && cv.DataOraUltimaModifica_Can.Day == dateIni.Day).ToList();

                                    if (cantsvar.Any())
                                    {
                                        Cant_Var vars = cantsvar.Last();
                                        int newCliId = 0; //GetCodCli(columnsNumber, splittedLine, fil);
                                        vars.Codice_Cantiere = currentCant.Codice_Cantiere;
                                        if (newCliId != 0)
                                            vars.Cli_Id = newCliId;
                                        vars.Fil_Id = filId;
                                        vars.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                        vars.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                        vars.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                        vars.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                        vars.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                        vars.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                        vars.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                        vars.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                        vars.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                        vars.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                    }
                                    else
                                    {
                                        //vado ad creare un nuovo record in Cant_Var
                                        Cant_Var newCantVar = RepoManager.Cant_VarRepo.Init();
                                        int newCliId = 0; //GetCodCli(columnsNumber, splittedLine, fil);
                                        //newCantVar.Codice_Cantiere = Get_Cant_Code(splittedLine[columnsNumber["CODICE FISCALE"]], cants, toAddCants, Fil_Id, newCliId);
                                        if (newCliId != 0)
                                            newCantVar.Cli_Id = newCliId;
                                        newCantVar.Fil_Id = filId;
                                        newCantVar.Cant_Id = currentCant.Cant_Id;
                                        newCantVar.Codice_Cantiere = currentCant.Codice_Cantiere;
                                        newCantVar.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                        newCantVar.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                        newCantVar.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                        newCantVar.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                        newCantVar.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                        newCantVar.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                        newCantVar.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                        newCantVar.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                        newCantVar.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                        newCantVar.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                        newCantVar.DisAbilitazione_Can = false;
                                        newCantVar.FlagGps_Can = 0;
                                        newCantVar.Tipologia_Can = "ASS";

                                        toAddCant_Vars.Add(newCantVar);
                                    }
                                }
                                else if (currentCant.DataOraUltimaModifica_Can < dateIni)
                                {
                                    //caso in cui il record è più recente di quello in Cant
                                    //Copio il record Cant in Cant_Var e aggiorno quello in Cant
                                    Cant_Var newCant_Var = RepoManager.Cant_VarRepo.Init();

                                    CommonService.DuplicateEntity(currentCant, newCant_Var);
                                    toAddCant_Vars.Add(newCant_Var);
                                    int newCliId = 0; //GetCodCli(columnsNumber, splittedLine, fil);
                                    if (newCliId != 0)
                                        currentCant.Cli_Id = newCliId;
                                    currentCant.Fil_Id = filId;
                                    currentCant.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                    currentCant.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                    currentCant.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                    currentCant.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                    currentCant.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                    currentCant.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                    currentCant.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                    currentCant.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                    currentCant.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                    currentCant.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                }
                            }
                            else
                            {
                                //il cantiere non esiste, quindi posso caricare i dati nella tabella Cant
                                Cant newCant = RepoManager.CantRepo.Init();
                                int newCliId = 0; // GetCodCli(columnsNumber, splittedLine, fil);
                                newCant.Fil_Id = filId;
                                //newCant.Codice_Cantiere = Get_Cant_Code(splittedLine[columnsNumber["CODICE FISCALE"]], cants, toAddCants, Fil_Id, newCliId);
                                if (newCliId != 0)
                                    newCant.Cli_Id = newCliId;
                                newCant.Cod_Fisc_Can = splittedLine[columnsNumber["CODICE FISCALE"]].Trim();
                                newCant.Descrizione_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                newCant.Cognome_Assistito_Can = String.Format("{0} {1}", splittedLine[columnsNumber["COGNOME"]].Trim(), splittedLine[columnsNumber["NOME"]].Trim());
                                newCant.Cap_Can = splittedLine[columnsNumber["CAP"]].Trim();
                                newCant.Luogo_Can = splittedLine[columnsNumber["COMUNE"]].Trim().ToUpper();
                                newCant.Indirizzo_Can = splittedLine[columnsNumber["INDIRIZZO"]].Trim();
                                newCant.Livello_Assistito_Can = splittedLine[columnsNumber["PROFILO INIZIALE"]].Trim();
                                newCant.DataOraUltimaModifica_Can = Convert.ToDateTime(splittedLine[columnsNumber["INI"]].Trim());
                                newCant.Codice_Voucher_Can = splittedLine[columnsNumber["CODICE VOUCHER"]].Trim();
                                newCant.Raggruppamento1_Can = splittedLine[columnsNumber["DISTRETTO"]].Trim();
                                newCant.DisAbilitazione_Can = false;

                                // viene abilitato di default il flag gps
                                newCant.FlagGps_Can = 1;
                                newCant.Tipologia_Can = "ASS";

                                // sono automaticamente calcolate le coordinate gps
                                RepoManager.CantRepo.UpdateGeoLocation(newCant);

                                // aggiungo il cantiere appena inserito nell'elenco dei cantieri letti
                                // affinché la numerazione progressiva per filiale funzioni anche all'interno dello stesso file
                                cants.Add(newCant);

                                toAddCants.Add(newCant);
                            }
                        }
                    }
                    //incremento il numero di riga
                    rownumber++;
                }
            }

            //se ho errori all'interno del file csv 
            if (errorFileds.ToString() != String.Empty)
                throw new InvalidOperationException(errorFileds.ToString());

            try
            {

                //Update dei cantieri modificati
                RepoManager.CantRepo.Context.BulkUpdate(cants.Where(cant => cant.Cant_Id != 0));

                //Inserimento dei cantieri nuovi
                RepoManager.CantRepo.Context.BulkInsert(toAddCants);

                foreach (Cant_Var cv in toAddCant_Vars)
                {
                    cv.Cant_Id = RepoManager.CantRepo.FirstOrDefault(cant => cant.Codice_Cantiere == cv.Codice_Cantiere).Cant_Id;
                }

                RepoManager.Cant_VarRepo.Add(toAddCant_Vars);
                RepoManager.Cant_VarRepo.SaveChanges();

            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            

            return errors;
        }
    }
}
