using Business.Repository;
using Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Business.ImportModules.CantImportModule.Imports
{
    public sealed class SolarisImport : ImportBase, IImport
    {
        private const string IDCOMPANY = "IDCOMPANY";
        private const string COD_DETT = "COD_DETT";
        private const string DESC_DETT = "DESC_DETT";
        private const string INIZIO_DETT = "INIZIO_DETT";
        private const string FINE_DETT = "FINE_DETT";
        private const string FLATTR = "FLATTR";
        private const string DTSTARTVL = "DTSTARTVL";
        private const string DTENDVL = "DTENDVL";

        private string _idCompany = "";
        private string _codDett = "";
        private string _descDett = "";
        private string _inizioDett = "";
        private string _fineDett = "";
        private string _flattr = "";
        private string _dtstartvl = "";
        private string _dtendvl = "";

        private IDictionary<string, Cant> _bambiniNuovi;
        private IDictionary<string, Cant> _bambiniEsistenti;

        private string _currentKey;

        public SolarisImport(IEnumerable<string> rows, int fil_Id = 0) : base(rows, fil_Id)
        {
            _bambiniNuovi = new Dictionary<string, Cant>();
            _bambiniEsistenti = new Dictionary<string, Cant>();
        }

        protected override void ElaborateRow()
        {
            _currentKey = $"{_codDett}_{_descDett}";

            if (_bambiniEsistenti.ContainsKey(_currentKey)) //Se è già nel db
            {
                Update();
            }
            else if (_bambiniNuovi.ContainsKey(_currentKey)) //Se è tra quelli da importare
            {
                UpdateBeforeImport();
            }
            else
            {
                Create();  //Altrimenti lo creo
            }
        }

        protected override void LoadServiceData()
        {
            _bambiniEsistenti = RepoManager.CantRepo.GetAll().ToDictionary(x => $"{x.Codice_Cantiere}_{x.Descrizione_Can}");
        }

        protected override void ReadRow(string[] rowData)
        {
            _idCompany = rowData[Header[IDCOMPANY]].Trim();
            _codDett = rowData[Header[COD_DETT]].Trim();
            _descDett = rowData[Header[DESC_DETT]].Trim();
            _inizioDett = rowData[Header[INIZIO_DETT]].Trim();
            _fineDett = rowData[Header[FINE_DETT]].Trim();
            _flattr = rowData[Header[FLATTR]].Trim();
            _dtstartvl = rowData[Header[DTSTARTVL]].Trim();
            _dtendvl = rowData[Header[DTENDVL]].Trim();
        }

        protected override void Save()
        {
            RepoManager.CantRepo.DbSet.AddRange(_bambiniNuovi.Values);
            RepoManager.CantRepo.BulkUpdate(_bambiniEsistenti.Values);
            RepoManager.CantRepo.SaveChanges();
        }

        protected override bool ValidateRow() => true;

        private void Create()
        {
            var bambino = RepoManager.CantRepo.Init();

            RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(bambino);

            bambino.Codice_Cantiere = _codDett;
            bambino.Descrizione_Can = _descDett;
            bambino.Data_Rapporto_Inizio_1_Can = DateTime.ParseExact(_inizioDett, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data inizio  validatà bambino
            bambino.Data_Rapporto_Fine_1_Can = DateTime.ParseExact(_fineDett, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data fine  validatà bambino
            bambino.Data_Rapporto_Inizio_2_Can = String.IsNullOrEmpty(_dtstartvl) ? default(DateTime?) : DateTime.ParseExact(_dtstartvl, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data inizio  validatà visibilità bambino
            bambino.Data_Rapporto_Fine_2_Can = String.IsNullOrEmpty(_dtendvl) ? default(DateTime?) : DateTime.ParseExact(_dtendvl, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data fine  validatà visibilità bambino

            _bambiniNuovi.Add($"{bambino.Codice_Cantiere}_{bambino.Descrizione_Can}", bambino);
        }

        private void Update()
        {
            var bambinoEsistente = _bambiniEsistenti[_currentKey];

            RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(bambinoEsistente);

            bambinoEsistente.Codice_Cantiere = _codDett;
            bambinoEsistente.Descrizione_Can = _descDett;
            bambinoEsistente.Data_Rapporto_Inizio_1_Can = DateTime.ParseExact(_inizioDett, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data inizio  validatà bambino
            bambinoEsistente.Data_Rapporto_Fine_1_Can = DateTime.ParseExact(_fineDett, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data fine  validatà bambino
            bambinoEsistente.Data_Rapporto_Inizio_2_Can = String.IsNullOrEmpty(_dtstartvl) ? default(DateTime?) : DateTime.ParseExact(_dtstartvl, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data inizio  validatà visibilità bambino
            bambinoEsistente.Data_Rapporto_Fine_2_Can = String.IsNullOrEmpty(_dtendvl) ? default(DateTime?) : DateTime.ParseExact(_dtendvl, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data fine  validatà visibilità bambino
        }

        private void UpdateBeforeImport()
        {
            var bambinoNuovoDuplicato = _bambiniNuovi[_currentKey];

            bambinoNuovoDuplicato.Codice_Cantiere = _codDett;
            bambinoNuovoDuplicato.Descrizione_Can = _descDett;
            bambinoNuovoDuplicato.Data_Rapporto_Inizio_1_Can = DateTime.ParseExact(_inizioDett, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data inizio  validatà bambino
            bambinoNuovoDuplicato.Data_Rapporto_Fine_1_Can = DateTime.ParseExact(_fineDett, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data fine  validatà bambino
            bambinoNuovoDuplicato.Data_Rapporto_Inizio_2_Can = String.IsNullOrEmpty(_dtstartvl) ? default(DateTime?) : DateTime.ParseExact(_dtstartvl, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data inizio  validatà visibilità bambino
            bambinoNuovoDuplicato.Data_Rapporto_Fine_2_Can = String.IsNullOrEmpty(_dtendvl) ? default(DateTime?) : DateTime.ParseExact(_dtendvl, "dd-MM-yyyy", CultureInfo.InvariantCulture); //Data fine  validatà visibilità bambino
        }
    }
}
