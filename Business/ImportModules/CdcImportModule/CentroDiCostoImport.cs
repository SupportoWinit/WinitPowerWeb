using Business.Repository;
using Common;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Business.ImportModules.CdcImportModule
{
    public class CentroDiCostoImport : ImportBase, IImport
    {
        private const string IDCOMPANY = "IDCOMPANY";
        private const string COD_CDC = "COD_CDC";
        private const string DESC_CDC = "DESC_CDC";
        private const string INIZIO_CDC = "INIZIO_CDC";
        private const string FINE_CDC = "FINE_CDC";
        private const string COD_DETT = "COD_DETT";
        private const string DESC_DETT = "DESC_DETT";
        private const string INIZIO_DETT = "INIZIO_DETT";
        private const string FINE_DETT = "FINE_DETT";
        private const string FLATTR = "FLATTR";
        private const string DTSTARTVL = "DTSTARTVL";
        private const string DTENDVL = "DTENDVL";
        private const string COD_CANT = "COD_CANT";
        private const string DESC_CANT = "DESC_CANT";

        private string _idCompany = "";
        private string _codCdc = "";
        private string _descCdc = "";
        private string _inizioCdc = "";
        private string _fineCdc = "";
        private string _codDett = "";
        private string _descDett = "";
        private string _inizioDett = "";
        private string _fineDett = "";
        private string _flattr = "";
        private string _dtstartvl = "";
        private string _dtendvl = "";

        private IDictionary<string, CentroDiCosto> _centriDiCostoNuovi;
        private IDictionary<string, CentroDiCosto> _centriDiCostoEsistenti;
        private IDictionary<string, Cant> _bambiniEsistenti;
        private static readonly ILog _log = LogManager.GetLogger(typeof(CentroDiCostoImport));

        private string _currentKey;

        public CentroDiCostoImport(IEnumerable<string> rows, int fil_Id = 0) : base(rows, fil_Id)
        {
            _centriDiCostoNuovi = new Dictionary<string, CentroDiCosto>();
            _centriDiCostoEsistenti = new Dictionary<string, CentroDiCosto>();
            _bambiniEsistenti = new Dictionary<string, Cant>();
        }

        public override IDictionary<string, string> Import()
        {
            _bambiniEsistenti = RepoManager.CantRepo.DbSet.ToDictionary(b => $"{b.Codice_Cantiere}_{b.Descrizione_Can}");
            _centriDiCostoEsistenti = RepoManager.CentroDiCostoRepo.DbSet.Include("Cant_CentroDiCosto.CentroDiCosto").ToDictionary(b => $"{b.Codice}_{b.Descrizione}");

            ReadHeader();
            bool importCant = Header.ContainsKey(COD_CANT) && Header.ContainsKey(DESC_CANT);

            //Salto la riga di header
            var groupedByCdc = base.Rows.Skip(1).GroupBy(row =>
            {
                var splittedRow = row.Split(';');

                return $"{splittedRow[Header[COD_CDC]]}_{splittedRow[Header[DESC_CDC]]}";
            }).ToList();

            List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable(c => c.Codice_Cantiere.StartsWith("034_")).ToList();
            foreach (Cant cant in cantieri) 
            {
                Cant_CentroDiCosto cdcCant = new Cant_CentroDiCosto
                {
                    Cant_Id = cant.Cant_Id,
                    CentroDiCosto_Id = 1
                };
                RepoManager.CentroDiCostoRepo.CantCentroDiCostoDbSet.Add(cdcCant);
                RepoManager.CentroDiCostoRepo.Context.SaveChanges();
            }
                          

            if (importCant)
            {
                //Salto la riga di header
                groupedByCdc = base.Rows.Skip(1).GroupBy(row =>
                {
                    var splittedRow = row.Split(';');
            
                    return $"{splittedRow[Header[COD_CDC]]};{splittedRow[Header[DESC_CDC]]};{splittedRow[Header[COD_CANT]]};{splittedRow[Header[DESC_CANT]]}";
                }).ToList();
            
                foreach (var cantCdc in groupedByCdc) 
                {
                    var chiave = cantCdc.Key;
                    string[] details = chiave.Split(';');
                    string codiceCdc = details[0];
                    string descCdc = details[1];
                    CentroDiCosto cdc = RepoManager.CentroDiCostoRepo.FirstOrDefault(c => c.Codice == codiceCdc && c.Descrizione == descCdc);
                    if (cdc != default(CentroDiCosto))
                    {
                        string codiceCant = CommonService.AggiungiSpaziASinistra(details[2],20);
                        string descCant = details[3];
                        Cant cantiere = RepoManager.CantRepo.FirstOrDefault(c => c.Codice_Cantiere == codiceCant && c.Descrizione_Can == descCant);
                        if (cantiere != default(Cant))
                        {
                            Cant_CentroDiCosto cdcCant = new Cant_CentroDiCosto
                            {
                                Cant_Id = cantiere.Cant_Id,
                                CentroDiCosto_Id = cdc.CentroDiCosto_Id
                            };
                            RepoManager.CentroDiCostoRepo.CantCentroDiCostoDbSet.Add(cdcCant);
            
                            RepoManager.CentroDiCostoRepo.Context.SaveChanges();
                        }
                        else 
                        {
                            _log.Warn($"Il cantiere {details[2]} - {details[3]} non è presente nel database, impossibile importare il cantiere {details[2]} - {details[3]}");
                        }
                    }
                    else 
                    {
                        _log.Warn($"Il centro di costo {details[0]} - {details[1]} non è presente nel database, impossibile importare il cantiere {details[2]} - {details[3]}");
                    }
                }
            }
            else 
            {
                foreach (var group in groupedByCdc)
                {
                    var centroDiCostoKey = group.Key;
            
                    var centroDiCosto = (CentroDiCosto)null;
            
                    if (!_centriDiCostoEsistenti.TryGetValue(centroDiCostoKey, out centroDiCosto))
                    {
                        var detail = centroDiCostoKey.Split('_');
            
                        centroDiCosto = new CentroDiCosto
                        {
                            Codice = detail[0],
                            Descrizione = detail[1],
                        };
            
                        _centriDiCostoEsistenti.Add(centroDiCostoKey, centroDiCosto);
                    }
            
                    centroDiCosto.Inizio = DateTime.Parse(group.First().Split(';')[3]);
                    centroDiCosto.Fine = DateTime.Parse(group.First().Split(';')[4]);
            
                    foreach (var row in group)
                    {
                        var splittedRow = row.Split(';');
            
                        var bambinoKey = $"{splittedRow[Header[COD_DETT]]}_{splittedRow[Header[DESC_DETT]]}";
            
                        var bambino = (Cant)null;
            
                        if (!_bambiniEsistenti.TryGetValue(bambinoKey, out bambino))
                        {
                            if (!String.IsNullOrEmpty(splittedRow[Header[COD_DETT]]))
                            {
                                bambino = new Cant
                                {
                                    Data_Registrazione_Can = DateTime.Now,
                                    DataOraUltimaModifica_Can = DateTime.Now,
                                    Codice_Cantiere = splittedRow[Header[COD_DETT]],
                                    Descrizione_Can = splittedRow[Header[DESC_DETT]]
                                };
            
                                _bambiniEsistenti.Add($"{splittedRow[Header[COD_DETT]]}_{splittedRow[Header[DESC_DETT]]}", bambino);
            
                                RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(bambino);
            
                                RepoManager.CantRepo.DbSet.Add(bambino);
                            }
                        }
            
                        if (!String.IsNullOrEmpty(splittedRow[Header[COD_DETT]]))
                        {
                            if (!centroDiCosto.Cant_CentroDiCosto.Any(c => c.Cant != null && $"{c.Cant.Codice_Cantiere}_{c.Cant.Descrizione_Can}" == bambinoKey))
                            {
                                centroDiCosto.Cant_CentroDiCosto.Add(new Cant_CentroDiCosto
                                {
                                    Cant = bambino,
                                    CentroDiCosto = centroDiCosto
                                });
                            }
                        }
            
                    }
            
                    if (centroDiCosto.CentroDiCosto_Id == default)
                        RepoManager.CentroDiCostoRepo.DbSet.Add(centroDiCosto);
                }
            
                RepoManager.CentroDiCostoRepo.Context.SaveChanges();
            }

            return new Dictionary<string, string>();
        }

        protected override void ElaborateRow()
        {
            _currentKey = $"{_codCdc}_{_descCdc}";

            if (_centriDiCostoEsistenti.ContainsKey(_currentKey)) //Se è già nel db
            {
                Update();
            }
            else
            {
                Create();  //Altrimenti lo creo
            }
        }

        protected override void LoadServiceData()
        {
            _centriDiCostoEsistenti = RepoManager.CentroDiCostoRepo.DbSet.ToDictionary(x => $"{x.Codice}_{x.Descrizione}");
            _bambiniEsistenti = RepoManager.CantRepo.DbSet.ToDictionary(x => $"{x.Codice_Cantiere}_{x.Descrizione_Can}");
        }

        protected override void ReadRow(string[] rowData)
        {
            _idCompany = rowData[Header[IDCOMPANY]].Trim();
            _codCdc = rowData[Header[COD_CDC]].Trim();
            _descCdc = rowData[Header[DESC_CDC]].Trim();
            _inizioCdc = rowData[Header[INIZIO_CDC]].Trim();
            _fineCdc = rowData[Header[FINE_CDC]].Trim();
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
            RepoManager.CentroDiCostoRepo.DbSet.AddRange(_centriDiCostoNuovi.Values);

            RepoManager.CantRepo.SaveChanges();
        }

        protected override bool ValidateRow() => true;
        private void Create()
        {
            var newCentroDiCosto = new CentroDiCosto
            {
                Codice = _codCdc,
                Descrizione = _descCdc,
                Inizio = DateTime.Parse(_inizioCdc),
                Fine = DateTime.Parse(_fineCdc)
            };
            var newCantCentroDiCosto = new Cant_CentroDiCosto
            {
                CentroDiCosto = newCentroDiCosto,
            };

            if (_bambiniEsistenti.TryGetValue($"{_codDett}_{_descDett}", out var bambino))
            {
                newCantCentroDiCosto.Cant = bambino;
            }
            else
            {
                newCantCentroDiCosto.Cant = new Cant
                {
                    Codice_Cantiere = _codDett,
                    Descrizione_Can = _descDett
                };

                RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(newCantCentroDiCosto.Cant);

                _bambiniEsistenti.Add($"{_codDett}_{_descDett}", newCantCentroDiCosto.Cant);

                RepoManager.CantRepo.Add(newCantCentroDiCosto.Cant);

            }

            _bambiniEsistenti[$"{_codDett}_{_descDett}"].Cant_CentroDiCosto.Add(newCantCentroDiCosto);

            _centriDiCostoNuovi.Add(_currentKey, newCentroDiCosto);
        }

        private void Update()
        {
            var centroDiCostoEsistente = _centriDiCostoEsistenti[_currentKey];

            centroDiCostoEsistente.Inizio = !string.IsNullOrEmpty(_inizioCdc) ? DateTime.ParseExact(_inizioCdc, "dd-MM-yyyy", CultureInfo.InvariantCulture) : DateTime.MinValue;
            centroDiCostoEsistente.Fine = !string.IsNullOrEmpty(_fineCdc) ? DateTime.ParseExact(_fineCdc, "dd-MM-yyyy", CultureInfo.InvariantCulture) : DateTime.MaxValue;

            if (!centroDiCostoEsistente.Cant_CentroDiCosto.Any(c => c.Cant?.Codice_Cantiere == _codCdc && c.Cant?.Descrizione_Can == _descCdc))
            {
                if (_bambiniEsistenti.ContainsKey(_currentKey))
                {
                    centroDiCostoEsistente.Cant_CentroDiCosto.Add(new Cant_CentroDiCosto
                    {
                        Cant = _bambiniEsistenti[_currentKey],
                        CentroDiCosto = centroDiCostoEsistente
                    });
                }
                else
                {
                    var newCantCdc = new Cant_CentroDiCosto
                    {
                        Cant = new Cant
                        {
                            Codice_Cantiere = _codDett,
                            Descrizione_Can = _descDett
                        },
                        CentroDiCosto = centroDiCostoEsistente
                    };

                    RepoManager.CantRepo.SetEntityBeforeAddOrUpdate(newCantCdc.Cant);

                    centroDiCostoEsistente.Cant_CentroDiCosto.Add(newCantCdc);
                }
            }
        }
    }
}

