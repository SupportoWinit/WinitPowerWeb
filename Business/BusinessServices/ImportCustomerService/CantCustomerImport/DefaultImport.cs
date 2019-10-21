using Business.BusinessServices.ImportCustomerService.Helpers;
using Business.BusinessServices.ImportCustomerService.Interfaces;
using Business.BusinessServices.ImportCustomersService;
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

    /*
     * Classe da verificare e sistemare (inserire log e altro).
     * Ricordarsi prima di metterla in produzione
     */

    public class DefaultImport : ICustomerImport
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DefaultImport));
        
        #region Campi privati

        private string codiceCantiere;
        private string descrizioneCantiere;
        private string codiceCliente;
        private string descrizioneCliente;
        private string codiceFru;
        private string descrizioneFiliale;
        private DateTime? dataAssociazione;
        private string comune;
        private string via;
        private string latitudine;
        private string longitudine;
        private string note;

        #endregion

        public DefaultImport()
        {
        }

        public IEnumerable<KeyValuePair<string, string>> Import(IEnumerable<string> inputCants)
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

            var emptyRow = ImportCustomerHelper.CreateEmptyRow(columns, ";");


            columnsNumber = new Dictionary<string, int>();
            splittedRow = splittedRow.Select(s => s.ToUpperInvariant()).Select(s => System.Text.RegularExpressions.Regex.Replace(s, @"\t|\n|\r", "")).ToArray();
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

            if (ImportCustomerHelper.IsHeaderWrongMapped(columnsNumber))
            {
                _log.ErrorFormat("La colonna {0} non è mappata correttamente. Fate attenzione agli spazi e agli 'a capo' nell'header. Import terminato", columnsNumber.First(c => c.Value == -1).Key);

                errors.Add("Errore intestazione", $"La colonna {columnsNumber.First(c => c.Value == -1).Key} non è mappata correttamente. Fate attenzione agli spazi e agli 'a capo' nell'header. Import terminato");

                return errors;
            }

            #endregion

            #endregion

            int rowNumber = 0;

            // per ogni riga del csv passato come parametro
            foreach (String stringVar in inputCants.Skip(1).ToArray())
            {
                rowNumber++;
                emptyRow = ImportCustomerHelper.CreateEmptyRow(columns, ";");

                String[] splittedLine = stringVar.Split(new Char[] { ';' }, StringSplitOptions.None);

                //Segnala errore se manca un'intera riga
                if (stringVar == emptyRow.ToString() || splittedLine.Length <= 1)
                {
                    errors.Add("Riga mancante", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_RIGA_X_VUOTA, rowNumber.ToString()));
                    continue;
                }

                #region ESTRAZIONE DATI

                codiceCantiere = splittedLine[columnsNumber["CODICE"]].Trim();
                descrizioneCantiere = splittedLine[columnsNumber["DESCRIZIONE"]].Trim();
                codiceCliente = splittedLine[columnsNumber["CODICE CLIENTE"]].Trim();
                descrizioneCliente = splittedLine[columnsNumber["DESCRIZIONE CLIENTE"]].Trim();
                codiceFru = splittedLine[columnsNumber["MATRICOLA FRU"]];
                descrizioneFiliale = splittedLine[columnsNumber["FILIALE"]];
                dataAssociazione = (splittedLine[columnsNumber["DATA ASSOCIAZIONE"]] != "") ? DateTime.Parse(splittedLine[columnsNumber["DATA ASSOCIAZIONE"]].Trim()) as DateTime? : null;
                comune = splittedLine[columnsNumber["COMUNE"]].Trim();
                via = splittedLine[columnsNumber["VIA"]].Trim();
                latitudine = splittedLine[columnsNumber["LATITUDINE"]].Trim();
                longitudine = splittedLine[columnsNumber["LONGITUDINE"]].Trim();
                note = splittedLine[columnsNumber["NOTE"]].Trim();

                #endregion

                if (IsCantDuplicated())
                {
                    errors.Add($"Cantiere {codiceCantiere}", $"Il cantiere con codice {codiceCantiere} è già stato importato!");
                    continue;
                }

                if (!IsCantValid())
                {
                    errors.Add($"Cantiere {codiceCantiere}", $"Il cantiere con codice {codiceCantiere} alla riga {rowNumber} non è valido");
                    continue;
                }

                Cant cantiere = CreateCant();

                #region FILIALE

                if (IsFilValid())
                    cantiere.Fil = GetFiliale();

                #endregion

                #region COMUNE

                SetLocation(cantiere);

                #endregion

                #region CLIENTE

                if (IsCliValid())
                    cantiere.Cli = CreateOrGetCli();

                #endregion

                #region ASSOCIAZIONE FRU

                if (!IsFruValid())
                {
                    errors.Add($"Cantiere {codiceCantiere}", $"Il cantiere con codice {codiceCantiere} alla riga {rowNumber} non ha un' unità portatile valida");
                }
                else
                    cantiere.Fru_Cant.Add(CreateFruCant());

                #endregion

                cantToInsert.Add(cantiere);
            }

            #region INSERIMENTO E SALVATAGGIO


            RepoManager.CantRepo.BulkInsert(cantToInsert);

            #endregion

            return errors;

        }


        bool IsCantValid()
        {
            return !String.IsNullOrEmpty(codiceCantiere) && !String.IsNullOrEmpty(descrizioneCantiere);
        }

        bool IsFruValid()
        {
            return !String.IsNullOrEmpty(codiceFru) && (codiceFru.Length == 10) && dataAssociazione.HasValue;
        }

        bool IsCliValid()
        {
            return !String.IsNullOrEmpty(codiceCliente) && !String.IsNullOrEmpty(descrizioneCliente);
        }

        bool IsFilValid()
        {
            return !String.IsNullOrEmpty(descrizioneFiliale);
        }

        bool IsCantDuplicated()
        {
            return RepoManager.CantRepo.FirstOrDefault(c => c.Codice_Cantiere == codiceCantiere) != null;
        }

        Cant CreateCant()
        {
            return new Cant()
            {
                Codice_Cantiere = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codiceCantiere, 20),
                Descrizione_Can = descrizioneCantiere,
                Tipologia_Can = "CAN",
                Indirizzo_Can = (via.Length < 50) ? via : via.Substring(0, 49),
                LatitudineGps_Can = (latitudine != "") ? Double.Parse(latitudine) : 0,
                LongitudineGps_Can = (longitudine != "") ? Double.Parse(longitudine) : 0,
                Note_Can = note
            };
        }

        Fil GetFiliale()
        {
            return RepoManager.FilRepo.FirstOrDefault(fil => fil.Descrizione_Fil == descrizioneFiliale);
        }

        Cli CreateOrGetCli()
        {
            Cli cliente = RepoManager.CliRepo.FirstOrDefault(c => c.Codice_Cliente.Trim() == codiceCliente.Trim());

            if (cliente == null)
            {
                cliente = new Cli();
                cliente.Codice_Cliente = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codiceCliente, 10);
                cliente.Cognome_Cli = descrizioneCliente;
            }

            return cliente;
        }

        Fru_Cant CreateFruCant()
        {
            Fru currentFru = RepoManager.FruRepo.FirstOrDefault(f => f.Codice_Fru.Trim() == codiceFru.Trim());

            Fru_Cant fruCant = RepoManager.Fru_CantRepo.Init();

            if (currentFru != default(Fru))
            {
                fruCant.Fru = currentFru;
                fruCant.Abilitazione_Data_Inizio_Fru_Can = dataAssociazione.Value;
            }
            else
            {
                Fru newFru = RepoManager.FruRepo.Init();

                newFru.DataOraUltimaModifica_Fru = DateTime.Now;
                newFru.N_Serie_Fru = codiceFru;
                newFru.Codice_Fru = CommonService.AggiungiSpaziASinistraSeStringaNumerica(codiceFru, 5);
                fruCant.Abilitazione_Data_Inizio_Fru_Can = dataAssociazione.Value;

                fruCant.Fru = newFru;
            }

            return fruCant;
        }

        void SetLocation(Cant cantiere)
        {
            if (!String.IsNullOrEmpty(comune))
            {
                Tab_Comuni comuneEntity = RepoManager.Tab_ComuniRepo.FirstOrDefault(x => x.Luogo_Tab_Comuni == comune);

                cantiere.Luogo_Can = (comuneEntity != null) ? comune.Trim() : String.Empty;
                cantiere.Cap_Can = (comuneEntity != null) ? comuneEntity.Cap_Tab_Comuni : String.Empty;
                cantiere.Provincia_Can = (comuneEntity != null) ? comuneEntity.Codice_Prov_Tab_Comuni : String.Empty;
            }
        }
    }
}



