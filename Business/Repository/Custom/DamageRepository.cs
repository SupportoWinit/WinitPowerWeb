using Common;
using Data;
using Domain;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Business.Repository.Custom
{
    public class DamageRepository : GenericRepository<Damage>, IDamageRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DamageRepository));

        private static string _backupFolder = HttpContext.Current.Server.MapPath(Common.Properties.Settings.Default.SegnBackup);


        public DamageRepository(PowerWebEntities context)
            : base(context)
        {

        }

        public override Dictionary<string, string> Check(Damage entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (isResetSession)
                //ResetSession();
                try
                {

                }
                catch (Exception ex)
                {

                }

            return null;
        }

        public List<Damage> ImportRowSegnalazioni(IEnumerable<string> paths, List<KeyValuePair<string, string>> importErrors)
        {
            string filesInputPath = Common.Properties.Settings.Default.Files_Input_Path;

            string segnSuspendedFile = HttpContext.Current.Server.MapPath(Path.Combine(filesInputPath, String.Format("{0}_{1}.txt", Common.Properties.Settings.Default.SuspendedSegnFile, DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"))));

            try
            {
                List<Damage> decodedSegnalazioni = new List<Damage>();

                string lastCode = RepoManager.DamageRepo.Max(damage => damage.Codice_Damage);
                int lastCodeToInt = 0;

                foreach (string singleFile in paths)
                {
                    IEnumerable<string> rowSegnalazioni = File.ReadAllLines(singleFile).Where(line => !line.StartsWith("*") && !String.IsNullOrEmpty(line)).ToList();

                    

                    foreach (string rowSegnalazione in rowSegnalazioni)
                    {
                        Damage segnalazione = DecodeSegnalazione(rowSegnalazione, importErrors);

                        if (segnalazione != null)
                        {
                            if (String.IsNullOrEmpty(lastCode))
                            {
                                lastCode = $"Segn_0000"; 
                            }
                            if (lastCodeToInt == 0)
                            {
                                lastCodeToInt = int.Parse(lastCode.Split('_')[1]) + 1;
                            }
                            else
                            {
                                lastCodeToInt++;
                            }

                            segnalazione.Codice_Damage = $"Segn_{lastCodeToInt.ToString("0000")}";


                            decodedSegnalazioni.Add(segnalazione);
                        }
                    }
                }

                return decodedSegnalazioni;
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore durante la fase di import delle segnalazioni con exception {0},", ex.Message);

                return new List<Damage>();
            }
            finally
            {
                BusinessService.BackupProcessedFiles(paths, _backupFolder);

                if (importErrors.Any())
                {
                    BusinessService.CreateSuspendedRegFile(segnSuspendedFile, importErrors);
                }
            }
        }

        public void ElaborateRowSegnalazioni(DateTime from, DateTime to)
        {
            to = to.AddDays(1);

            List<Damage> segnalToElaborate = DbSet.Include("Fru").Include("Pru").Where(d => d.Data_Ora_Damage >= from && d.Data_Ora_Damage < to).ToList();

            segnalToElaborate.ForEach(d =>
            {

                d.Cant = UpdateCant(d);
                d.Col = UpdateCol(d);

            });

            SaveChanges();
        }

        #region IMPORT FUNCTIONS

        Damage DecodeSegnalazione(string rowSegnalazione, List<KeyValuePair<string, string>> importErrors)
        {
            string[] rowSegnazioneValues = rowSegnalazione.Split(';');

            Damage segnalazione = Init();

            if (CheckConcistency(rowSegnazioneValues, importErrors))
            {
                segnalazione.Data_Ora_Damage = ExtractDate(rowSegnazioneValues);
                segnalazione.Data_Registrazione_Damage = DateTime.Now;

                //segnalazione.Codice_Damage = GenerateCodiceSegnalazione();
                segnalazione.Note_Damage = ExtractDescription(rowSegnazioneValues);

                segnalazione.Pru_Id = GetPru(rowSegnazioneValues);
                segnalazione.Fru_Id = GetFru(rowSegnazioneValues);

                segnalazione.Col = GetCol(segnalazione.Pru_Id, segnalazione.Data_Ora_Damage);
                segnalazione.Cant = GetCant(segnalazione.Fru_Id, segnalazione.Data_Ora_Damage);

                segnalazione.MultimediaFileName = rowSegnazioneValues[12];
                segnalazione.Audio_Attachment = rowSegnazioneValues[11] == "0" ? false : true;
                segnalazione.Image_Attachment = rowSegnazioneValues[10] == "0" ? false : true;
                segnalazione.Pdf_Attachment = false;

                segnalazione.Tab_Damage_Id = GetTab_Damage_Id(rowSegnazioneValues);
                segnalazione.Tab_Decod_Id = GetTab_Decod_Id(rowSegnazioneValues);
            }
            else
            {

                //Gestione errori
                segnalazione = null;
            }



            return segnalazione;
        }

        public bool CheckConcistency(string[] rowSegnalazione, List<KeyValuePair<string, string>> errors)
        {

            string pruCode = Common.CommonService.AggiungiSpaziASinistraSeStringaNumerica(rowSegnalazione[0], 10);

            if (RepoManager.PruRepo.SingleOrDefault(p => p.Codice_Pru == pruCode, true) == null)
            {
                errors.Add(new KeyValuePair<string, string>(String.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_MATRICOLA_INESISTENTE, pruCode)), String.Join(";", rowSegnalazione)));
                _log.InfoFormat("La segnalazione con codice fru {0} e codice pru {1} non è stata importata inquanto codice pru inesistente", rowSegnalazione[0], rowSegnalazione[1]);
                return false;
            }



            string fruCode = Common.CommonService.AggiungiSpaziASinistraSeStringaNumerica(rowSegnalazione[1], 10);

            if (RepoManager.FruRepo.SingleOrDefault(fru => fru.Codice_Fru == fruCode, true) == null)
            {
                errors.Add(new KeyValuePair<string, string>(String.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_MATRICOLA_INESISTENTE, fruCode)), String.Join(";", rowSegnalazione)));
                _log.InfoFormat("La segnalazione con codice fru {0} e codice pru {1} non è stata importata inquanto codice fru inesistente", rowSegnalazione[0], rowSegnalazione[1]);
                return false;
            }



            string codice = rowSegnalazione[13];

            if (RepoManager.Tab_DecodRepo.SearchKeyInTable("DECOD_TAB","TIPO_SEGNALAZIONI", codice) == null)
            {
                errors.Add(new KeyValuePair<string, string>(String.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_MANCA_RECORD_TITLE_IN_TAB_DECOD, $"{codice}")), String.Join(";", rowSegnalazione)));
                _log.InfoFormat("La segnalazione con codice fru {0} e codice pru {1} non è stata importata inquanto tab_decod inesistente", rowSegnalazione[0], rowSegnalazione[1]);
                return false;
            }


            string descrizione = rowSegnalazione[14];

            if (RepoManager.Tab_DamageRepo.SingleOrDefault(t => t.Tab_Decod.Decodifica_Tab == codice && t.Descrizione_Tab_Damage == descrizione) == null)
            {
                errors.Add(new KeyValuePair<string, string>(String.Format("*{0}", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_TAB_DAMAGE_X_INESISTENTE, $"{codice} O {descrizione}")), String.Join(";", rowSegnalazione)));
                _log.InfoFormat("La segnalazione con codice fru {0} e codice pru {1} non è stata importata inquanto tab_damage inesistente", rowSegnalazione[0], rowSegnalazione[1]);
                return false;
            }



            return !errors.Any(); //Se ci sono errori non abbiamo passato il controllo
        }

        DateTime ExtractDate(string[] rowSegnalazione)
        {
            DateTime date = new DateTime(
                int.Parse(rowSegnalazione[2]),
                int.Parse(rowSegnalazione[3]),
                int.Parse(rowSegnalazione[4]),
                int.Parse(rowSegnalazione[5]),
                int.Parse(rowSegnalazione[6]),
                0
                );

            return date;
        }
        string ExtractDescription(string[] rowSegnalazione)
        {

            if (String.IsNullOrEmpty(rowSegnalazione[9]))
                return "";

            return rowSegnalazione[9];

        }
        string GenerateCodiceSegnalazione()
        {
            string codice = Max(s => s.Codice_Damage);



            if (string.IsNullOrEmpty(codice))
            {
                return "Segn_0001";
            }

            string numericPart = codice.Split('_')[1];

            int counter = int.Parse(numericPart);

            counter++;

            return string.Format("Segn_{0}", counter.ToString("0000"));


        }
        int GetPru(string[] rowSegnalazione)
        {
            string pruCode = Common.CommonService.AggiungiSpaziASinistraSeStringaNumerica(rowSegnalazione[0], 10);


            return RepoManager.PruRepo.Single(pru => pru.Codice_Pru == pruCode, true).Pru_Id;
        }
        int GetFru(string[] rowSegnalazione)
        {
            string fruCode = CommonService.AggiungiSpaziASinistraSeStringaNumerica(rowSegnalazione[1], 10);

            return RepoManager.FruRepo.Single(fru => fru.Codice_Fru == fruCode, true).Fru_Id;
        }
        Col GetCol(int pruId, DateTime date)
        {
            var pru = RepoManager.PruRepo.DbSet.Include("Pru_Col").SingleOrDefault(c => c.Pru_Id == pruId);

            var allowedPrus = pru.Pru_Col.Where(p => p.Abilitazione_Data_Inizio_Pru_Col <= date).ToList(); //.Min(c => c.Abilitazione_Data_Inizio_Pru_Col);

            if (!allowedPrus.Any())
                return null;

            DateTime minDate = allowedPrus.Max(c => c.Abilitazione_Data_Inizio_Pru_Col);

            return allowedPrus.Single(c => c.Abilitazione_Data_Inizio_Pru_Col == minDate).Col;

        }
        Cant GetCant(int fruId, DateTime date)
        {
            var fru = RepoManager.FruRepo.DbSet.Include("Fru_Cant").SingleOrDefault(c => c.Fru_Id == fruId);

            var allowedFrus = fru.Fru_Cant.Where(p => p.Abilitazione_Data_Inizio_Fru_Can <= date).ToList(); //.Min(c => c.Abilitazione_Data_Inizio_Pru_Col);

            if (!allowedFrus.Any())
                return null;

            DateTime minDate = allowedFrus.Max(c => c.Abilitazione_Data_Inizio_Fru_Can);

            return allowedFrus.Single(c => c.Abilitazione_Data_Inizio_Fru_Can == minDate).Cant;
        }
        int GetTab_Damage_Id(string[] rowSegnalazione)
        {
            string codice = rowSegnalazione[13];

            string descrizione = rowSegnalazione[14];

            var tabDamage = RepoManager.Tab_DamageRepo.SingleOrDefault(t => t.Tab_Decod.Chiave_Tab == codice && t.Descrizione_Tab_Damage == descrizione);

            return tabDamage.Tab_Damage_Id;
        }
        int GetTab_Decod_Id(string[] rowSegnalazione)
        {
            var key = rowSegnalazione[13];

            var tabDecod = RepoManager.Tab_DecodRepo.SingleOrDefault(t => t.Gruppo_Tab == "DECOD_TAB" && t.Nome_Tab == "TIPO_SEGNALAZIONI" && t.Chiave_Tab == key);

            return tabDecod.Tab_Decod_Id;
        }
        #endregion

        #region ELABORATE FUNCTIONS

        Pru UpdatePru(Damage segnalazione)
        {
            return null;
        }

        Fru UpdateFru(Damage segnalazione)
        {
            return null;
        }

        Cant UpdateCant(Damage segnalazione)
        {
            var fru = RepoManager.FruRepo.DbSet.Include("Fru_Cant").Include("Fru_Cant.Cant").SingleOrDefault(c => c.Fru_Id == segnalazione.Fru_Id);

            var allowedFrus = fru.Fru_Cant.Where(p => p.Abilitazione_Data_Inizio_Fru_Can <= DateTime.Now).ToList(); //.Min(c => c.Abilitazione_Data_Inizio_Pru_Col);

            if (!allowedFrus.Any())
                return null;

            DateTime minDate = allowedFrus.Max(c => c.Abilitazione_Data_Inizio_Fru_Can);

            return allowedFrus.Single(c => c.Abilitazione_Data_Inizio_Fru_Can == minDate).Cant;

        }

        Col UpdateCol(Damage segnalazione)
        {
            var pru = RepoManager.PruRepo.DbSet.Include("Pru_Col").Include("Pru_Col.Col").SingleOrDefault(c => c.Pru_Id == segnalazione.Pru_Id);

            var allowedPrus = pru.Pru_Col.Where(p => p.Abilitazione_Data_Inizio_Pru_Col <= DateTime.Now).ToList(); //.Min(c => c.Abilitazione_Data_Inizio_Pru_Col);

            if (!allowedPrus.Any())
                return null;

            DateTime minDate = allowedPrus.Max(c => c.Abilitazione_Data_Inizio_Pru_Col);

            return allowedPrus.Single(c => c.Abilitazione_Data_Inizio_Pru_Col == minDate).Col;
        }

        #endregion










    }
}
