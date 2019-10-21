using Business.ImportModules;
using Business.Repository;
using Common;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Business.ImportModules.CantImportModule.Imports
{
    public sealed class MosaicoImport : ImportBase, IImport
    {
        string codVouch = "";
        string codFisc = "";
        string cognome = "";
        string nome = "";
        string indirizzo = "";
        string cap = "";
        string comune = "";
        string distretto = "";
        string profiloIniz = "";
        string ini = "";

        ILookup<Tuple<string, string>, Cant> _assistiti;
        IDictionary<Tuple<string, string>, ICollection<Cant>> _nuoviAssistiti;

        Tuple<string, string> _currentKey;

        public MosaicoImport(string[] inputCants, int fil_Id = 0) : base(inputCants, fil_Id) { }

       

        protected override void LoadServiceData()
        {
            _nuoviAssistiti = new Dictionary<Tuple<string, string>, ICollection<Cant>>();
            _assistiti = RepoManager.CantRepo.Find(cant => cant.Fil_Id == Fil_Id).ToLookup(c => Tuple.Create(c.Descrizione_Can, c.Cod_Fisc_Can));
        }

        protected override void ElaborateRow()
        {
            _currentKey = Tuple.Create(String.Format("{0} {1}", cognome, nome), codFisc);

            if (_assistiti.Contains(_currentKey)) //Se è già nel db
            {
                Update();
            }
            else if (_nuoviAssistiti.ContainsKey(_currentKey)) //Se è tra quelli da importare
            {
                UpdateBeforeImport();
            }
            else
            {
                Create();  //Altrimenti lo creo
            }
        }

        protected override void ReadRow(string[] rowData)
        {
            codVouch = rowData[Header["CODICE VOUCHER"]];
            codFisc = rowData[Header["CODICE FISCALE"]].Trim().ToUpper();
            cognome = rowData[Header["COGNOME"]].Trim();
            nome = rowData[Header["NOME"]].Trim();
            indirizzo = rowData[Header["INDIRIZZO"]].Trim();
            cap = rowData[Header["CAP"]].Trim();
            comune = rowData[Header["COMUNE"]].Trim();
            distretto = rowData[Header["DISTRETTO"]].Trim();
            profiloIniz = rowData[Header["PROFILO INIZIALE"]].Trim();
            ini = rowData[Header["INI"]].Trim();
        }

        protected override void Save()
        {
            var nuoviAssistiti = _nuoviAssistiti.SelectMany(c => c.Value).ToList();

            RepoManager.CantRepo.BulkInsert(nuoviAssistiti);
            RepoManager.CantRepo.SaveChanges();
        }

        protected override bool ValidateRow()
        {
            bool valid = !IsEmpty();

            DateTime iniDate;

            if (!DateTime.TryParse(ini, out iniDate))
            {
                valid = false;
                Errors.Add("INI", "Il valore inserito nella colonna {0} alla riga {1} non è un formato di data valido!");
            }
                

            //if (codVouch == string.Empty)
            //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_VOUCHER_X_VUOTA, rownumber.ToString()));
            ////nel caso in cui il codice fiscale sia vuoto
            //if (codFisc == string.Empty)
            //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CODICE_FISCALE_X_VUOTA, rownumber.ToString()));
            ////nel caso in cui il codice fiscale sia lungo meno di 16
            //else if (!CommonService.ECodiceFiscaleValido(codFisc))
            //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CODICE_FISCALE_X_ERRATO, rownumber.ToString()));

                //if (cognome == string.Empty)
                //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COGNOME_X_VUOTA, rownumber.ToString()));

                //if (nome == string.Empty)
                //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_NOME_X_VUOTA, rownumber.ToString()));

                //if (indirizzo == string.Empty)
                //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_INDIRIZZO_X_VUOTA, rownumber.ToString()));

                //if (cap == string.Empty)
                //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAP_X_VUOTA, rownumber.ToString()));

                //if (comune == string.Empty)
                //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_COMUNE_X_VUOTA, rownumber.ToString()));

                //if (distretto == string.Empty)
                //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DISTRETTO_X_VUOTA, rownumber.ToString()));

                //if (profiloIniz == string.Empty)
                //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PROFILO_X_VUOTA, rownumber.ToString()));

                //if (ini == string.Empty)
                //    errorFileds.AppendLine(BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_DATA_INIZIALE_X_VUOTA, rownumber.ToString()));

            return valid;

        }

        #region Private methods

        private void Create()
        {
            Cant newCant = RepoManager.CantRepo.Init();
            //int newCliId = RepoManager.CantRepo.GetCodCli(columnsNumber, splittedLine, fil);
            //newCant.Fil_Id = Fil_Id;
            ////newCant.Codice_Cantiere = Get_Cant_Code(codFisc, cants, toAddCants, Fil_Id, newCliId);
            //if (newCliId != 0)
            //    newCant.Cli_Id = newCliId;
            newCant.Cod_Fisc_Can = codFisc;
            newCant.Descrizione_Can = String.Format("{0} {1}", cognome, nome);
            newCant.Cognome_Assistito_Can = String.Format("{0} {1}", cognome, nome);
            newCant.Cap_Can = cap;
            newCant.Luogo_Can = comune;
            newCant.Indirizzo_Can = indirizzo;
            newCant.Livello_Assistito_Can = profiloIniz;
            newCant.DataOraUltimaModifica_Can = Convert.ToDateTime(ini);
            newCant.Codice_Voucher_Can = codVouch;
            newCant.Raggruppamento1_Can = distretto;
            newCant.DisAbilitazione_Can = false;

            // viene abilitato di default il flag gps
            newCant.FlagGps_Can = 1;
            newCant.Tipologia_Can = "ASS";

            // sono automaticamente calcolate le coordinate gps
            RepoManager.CantRepo.UpdateGeoLocation(newCant);
            
            if (!_nuoviAssistiti.ContainsKey(_currentKey))
                _nuoviAssistiti.Add(_currentKey, new List<Cant>());

            _nuoviAssistiti[_currentKey].Add(newCant);

        }

        private void Update()
        {
            var currentCant = _assistiti[_currentKey].First();

            //int newCliId = GetCodCli(columnsNumber, splittedLine, fil);
            //if (newCliId != 0)
            //    currentCant.Cli_Id = newCliId;
            currentCant.Cod_Fisc_Can = codFisc;
            currentCant.Descrizione_Can = String.Format("{0} {1}", cognome, nome);
            currentCant.Cognome_Assistito_Can = String.Format("{0} {1}", cognome, nome);
            currentCant.Cap_Can = cap;
            currentCant.Luogo_Can = comune;
            currentCant.Indirizzo_Can = indirizzo;
            currentCant.Livello_Assistito_Can = profiloIniz;
            currentCant.DataOraUltimaModifica_Can = Convert.ToDateTime(ini);
            currentCant.Codice_Voucher_Can = codVouch;
            currentCant.Raggruppamento1_Can = distretto;
            
        }

        private void UpdateBeforeImport()
        {
            var currentCant = _nuoviAssistiti[_currentKey].First();

            //int newCliId = GetCodCli(columnsNumber, splittedLine, fil);
            //if (newCliId != 0)
            //    currentCant.Cli_Id = newCliId;
            currentCant.Cod_Fisc_Can = codFisc;
            currentCant.Descrizione_Can = String.Format("{0} {1}", cognome, nome);
            currentCant.Cognome_Assistito_Can = String.Format("{0} {1}", cognome, nome);
            currentCant.Cap_Can = cap;
            currentCant.Luogo_Can = comune;
            currentCant.Indirizzo_Can = indirizzo;
            currentCant.Livello_Assistito_Can = profiloIniz;
            currentCant.DataOraUltimaModifica_Can = Convert.ToDateTime(ini);
            currentCant.Codice_Voucher_Can = codVouch;
            currentCant.Raggruppamento1_Can = distretto;
        }

        #endregion

    }
}
