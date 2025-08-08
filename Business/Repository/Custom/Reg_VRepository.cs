using BingMapsRESTToolkit;
using Business.BusinessExtension;
using Business.XmlExportsData.Orlando;
using Business.XmlExportsData.Perfetto;
using Business.XmlExportsData.Scs;
using Common;
using Data;
using DevExpress.Data.Linq;
using DevExpress.Data.PLinq.Helpers;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraPrinting.XamlExport;
using DevExpress.XtraRichEdit.Import.Html;
using DevExpress.XtraSpellChecker.Parser;
using Domain;
using log4net;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Westwind.Utilities.Extensions;

namespace Business.Repository.Custom
{
    public class Reg_VRepository : GenericRepository<Reg_V>, IReg_VRepository
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Reg_V));

        public Reg_VRepository(PowerWebEntities context)
            : base(context)
        {

        }

        private static List<Tab_Decod> Tab_Decods
        {
            get
            {


                List<Tab_Decod> oLista = PowerWebContext.GetFromSession<List<Tab_Decod>>("Tab_Decods_Reg");
                if (oLista == null)
                {
                    oLista = RepoManager.Tab_DecodRepo.GetAll(true).ToList();
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_Reg", oLista);
                }
                return oLista;
            }
        }


        #region Dati elaborate per tab messaggi

        /// <summary>
        /// L'id dell'utente che ha lanciato l'elaborazione
        /// </summary>
        private int? _elaborateUserId;

        /// <summary>
        /// La data e ora di avvio dell'elaborazione
        /// </summary>
        private DateTime? _elaborateDateTime;

        #endregion

        /// <summary>
        /// Evidenzia le timbrature effettuate oltre l'orario stabilito.
        /// </summary>
        /// <param name="regVs">L'elenco delle reg_v da controllare.</param>
        /// <param name="regs">L'elenco delle registrazioni su cui eventualmente modificare il colore.</param>
        /// <param name="cants">L'elenco dei cantieri collegati alle registrazioni specificate per il controllo.</param>
        /// <param name="cols">L'elenco dei collaboratori collegati alle registrazioni specifciate per il controllo.</param>
        /// <param name="elaborateUserId">L'identificativo dell'utente di lancio dell'operazione (utilizzato per la scrittura della tabella messaggi).</param>
        /// <param name="elaborateDateTime">La data e ora dell'operazione (utilizzata per la scrittura della tabella messaggi).</param>
        /// <param name="application">L'applicazione di lancio dell'operazione (utilizzata per la scrittura della tabella messaggi).</param>
        /// <returns>L'elenco degli errori eventualmente riscontrato durante le operazioni di controllo.</returns>
        public List<KeyValuePair<string, string>> CheckDeelay(IEnumerable<Reg_V> regVs, IEnumerable<Reg> regs, List<Cant> cants, List<Col> cols, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application)
        {
            var regsDic = new Dictionary<int, Reg>();

            foreach (Reg reg in regs)
            {
                regsDic.Add(reg.Reg_Id, reg);
            }

            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();

            if (regVs.Count() > 0)
            {
                var groupByColRegs = regVs.GroupBy(reg => reg.Col_Id);

                //Recupero i Parametri di Arrotondamento Generali da Scheda parametri
                RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;
                int paramThresholdStart = RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_I.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_I.Value : -1;
                int paramThresholdEnd = RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_F.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_F.Value : -1;
                int paramTinutesStart = RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_I.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_I.Value : -1;
                int paramTinutesEnd = RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_F.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_F.Value : -1;
                int utilizzoLimiteEntrata = RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Entrata;
                int utilizzoLimiteUscita = RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Uscita;
                int delayTollerance;
                TimeSpan delayMorningTollerance;
                TimeSpan delayAfternoonTollerance;

                // calcolo del mezzogiorno (utilizzato per la divisione mattutina e pomeridiana del limite d'entrata)
                TimeSpan midDay = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Inizio_Pomeriggio ?? new TimeSpan(12, 0, 0);
                TimeSpan midNight = new TimeSpan(0, 0, 0);

                foreach (var colGroup in groupByColRegs)
                {

                    int colGroupId = colGroup.Key.HasValue ? colGroup.Key.Value : -1;

                    if (colGroupId != -1)
                    {
                        Col currentCol = cols.SingleOrDefault(col => col.Col_Id == colGroupId);

                        if (currentCol.Limite_Entrata_Inizio_Pomeriggio_Col.HasValue)
                            midDay = currentCol.Limite_Entrata_Inizio_Pomeriggio_Col.Value;

                        //Recupera la tolleranza del ritardo dal COL o dai PARAM, altrimenti la setta a 0
                        delayTollerance = currentCol.Ritardo_Tolleranza_Minuti_Col ?? (RepoManager.ParamRepo.ParametersRow.Ritardo_Tolleranza_Minuti ?? 0);

                        //Recupera la tolleranza limite d'entrata dai PARAM, altrimenti la setta a 0
                        delayMorningTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata ?? new TimeSpan(0, 0, 0);
                        delayAfternoonTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata_Pomeriggio ?? new TimeSpan(0, 0, 0);

                        if (currentCol != null)
                        {
                            var groupByCantRegs = colGroup.GroupBy(reg => reg.Cant_Id);

                            foreach (var cantGroup in groupByCantRegs)
                            {
                                int cantGroupId = cantGroup.Key.HasValue ? cantGroup.Key.Value : -1;

                                if (cantGroupId != -1)
                                {
                                    Cant currentCant = cants.SingleOrDefault(cant => cant.Cant_Id == cantGroupId);

                                    if (currentCant != null)
                                    {
                                        TimeSpan minEntryHour = currentCant.Limite_Entrata_Mattina_Cant.HasValue ? currentCant.Limite_Entrata_Mattina_Cant.Value : TimeSpan.Zero;

                                        List<Reg_V> currentRegVs = cantGroup.OrderBy(regV => regV.Data_Ora_Fis_E).ToList();

                                        foreach (Reg_V currentRegV in currentRegVs)
                                        {
                                            Reg currentRegE = regsDic[currentRegV.RegE];

                                            Dictionary<EntryLimitTypeEnum, EntryLimitData> entryLimitConfig = GetEntryLimitConifg(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay);

                                            if (currentCant.Limite_Entrata_Mattina_Cant != null || currentCant.Limite_Entrata_Pomeriggio_Cant != null)
                                            {
                                                if (currentCant.Limite_Entrata_Mattina_Cant != null) {
                                                    TimeSpan limite = currentCant.Limite_Entrata_Mattina_Cant.Value;
                                                    if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < new TimeSpan(12, 0, 0))
                                                    {
                                                        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > limite)
                                                        {
                                                            TimeSpan temp = currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Subtract(limite);
                                                            currentRegE.Ritardo_Durata = (int)temp.TotalMinutes;
                                                        }
                                                        else
                                                        {
                                                            currentRegE.Ritardo_Durata = 0;
                                                        }
                                                    }
                                                }
                                                if (currentCant.Limite_Entrata_Pomeriggio_Cant != null) {
                                                    TimeSpan limite = currentCant.Limite_Entrata_Pomeriggio_Cant.Value;
                                                    if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > new TimeSpan(12, 0, 0))
                                                    {
                                                        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > limite)
                                                        {
                                                            TimeSpan temp = currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Subtract(limite);
                                                            currentRegE.Ritardo_Durata = (int)temp.TotalMinutes;
                                                        }
                                                        else
                                                        {
                                                            currentRegE.Ritardo_Durata = 0;
                                                        }
                                                    }
                                                }
                                            }
                                            else {
                                                currentRegE.Ritardo_Durata = 0;
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                RepoManager.RegRepo.Context.BulkUpdate(regs);
            }
            return errors;
        }

        #region Arrotondamenti e Controllo Sovrapposizioni

        /// <summary>
        /// Effettua l'arrotondamento sulle registrazioni specificate.
        /// </summary>
        /// <param name="regVs">L'elenco delle reg_v da arrotondare.</param>
        /// <param name="regs">L'elenco delle registrazioni su cui eventualmente scrivere l'arrotondamento.</param>
        /// <param name="cants">L'elenco dei cantieri collegati alle registrazioni specificate per l'arrotondamento.</param>
        /// <param name="cols">L'elenco dei collaboratori collegati alle registrazioni specifciate per l'arrotondamento.</param>
        /// <param name="elaborateUserId">L'identificativo dell'utente di lancio dell'operazione (utilizzato per la scrittura della tabella messaggi).</param>
        /// <param name="elaborateDateTime">La data e ora dell'operazione (utilizzata per la scrittura della tabella messaggi).</param>
        /// <param name="application">L'applicazione di lancio dell'operazione (utilizzata per la scrittura della tabella messaggi).</param>
        /// <returns>L'elenco degli errori eventualmente riscontrato durante le operazioni di arrotondamento.</returns>
        public List<KeyValuePair<string, string>> Rounding(IEnumerable<Reg_V> regVs, IEnumerable<Reg> regs, List<Cant> cants, List<Col> cols, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application,bool delete)
        {
            var regsDic = new Dictionary<int, Reg>();

            foreach (Reg reg in regs)
            {
                regsDic.Add(reg.Reg_Id, reg);
            }

            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();

            if (regVs.Count() > 0)
            {
                var groupByColRegs = regVs.GroupBy(reg => reg.Col_Id);

                //Recupero i Parametri di Arrotondamento Generali da Scheda parametri
                RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;
                int paramThresholdStart = RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_I.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_I.Value : -1;
                int paramThresholdEnd = RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_F.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Soglia_Arrot_F.Value : -1;
                int paramTinutesStart = RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_I.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_I.Value : -1;
                int paramTinutesEnd = RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_F.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Minuti_Arrot_F.Value : -1;
                int utilizzoLimiteEntrata = RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Entrata;
                int utilizzoLimiteUscita = RepoManager.ParamRepo.ParametersRow.Utilizzo_Limite_Uscita;
                int delayTollerance;
                TimeSpan delayMorningTollerance;
                TimeSpan delayAfternoonTollerance;

                // calcolo del mezzogiorno (utilizzato per la divisione mattutina e pomeridiana del limite d'entrata)
                TimeSpan midDay = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Inizio_Pomeriggio ?? new TimeSpan(12, 0, 0);
                TimeSpan midNight = new TimeSpan(0, 0, 0);

                foreach (var colGroup in groupByColRegs)
                {

                    int colGroupId = colGroup.Key.HasValue ? colGroup.Key.Value : -1;

                    if (colGroupId != -1)
                    {
                        Col currentCol = cols.SingleOrDefault(col => col.Col_Id == colGroupId);

                        if (currentCol.Limite_Entrata_Inizio_Pomeriggio_Col.HasValue)
                            midDay = currentCol.Limite_Entrata_Inizio_Pomeriggio_Col.Value;

                        //Recupera la tolleranza del ritardo dal COL o dai PARAM, altrimenti la setta a 0
                        delayTollerance = currentCol.Ritardo_Tolleranza_Minuti_Col ?? (RepoManager.ParamRepo.ParametersRow.Ritardo_Tolleranza_Minuti ?? 0);

                        //Recupera la tolleranza limite d'entrata dai PARAM, altrimenti la setta a 0
                        delayMorningTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata ?? new TimeSpan(0, 0, 0);
                        delayAfternoonTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata_Pomeriggio ?? new TimeSpan(0, 0, 0);

                        if (currentCol != null)
                        {
                            var groupByCantRegs = colGroup.GroupBy(reg => reg.Cant_Id);

                            foreach (var cantGroup in groupByCantRegs)
                            {
                                int cantGroupId = cantGroup.Key.HasValue ? cantGroup.Key.Value : -1;

                                if (cantGroupId != -1)
                                {
                                    Cant currentCant = cants.SingleOrDefault(cant => cant.Cant_Id == cantGroupId);

                                    if (currentCant != null)
                                    {
                                        TimeSpan minEntryHour = currentCant.Limite_Entrata_Mattina_Cant.HasValue ? currentCant.Limite_Entrata_Mattina_Cant.Value : TimeSpan.Zero;


                                        //Recupero i Parametri di Arrotondamento del Collaboratore
                                        RoundingMethodEnum roundingEnum = currentCol.RoundingMethodEnum;
                                        int thresholdStart = currentCol.SogliaI_Col.HasValue ? currentCol.SogliaI_Col.Value : -1;
                                        int thresholdEnd = currentCol.SogliaF_Col.HasValue ? currentCol.SogliaF_Col.Value : -1;
                                        int minutesStart = currentCol.ArrotI_Col.HasValue ? currentCol.ArrotI_Col.Value : -1;
                                        int minutesEnd = currentCol.ArrotF_Col.HasValue ? currentCol.ArrotF_Col.Value : -1;

                                        if (roundingEnum == RoundingMethodEnum.None)
                                        {
                                            if (currentCol.Qualifica_Col != "0") {
                                                //Recupero i Parametri di Arrotondamento del Cantiere
                                                roundingEnum = currentCant.RoundingMethodEnum;
                                                thresholdStart = currentCant.Soglia_Arrot_Fig_Can.HasValue ? currentCant.Soglia_Arrot_Fig_Can.Value : -1;
                                                thresholdEnd = currentCant.Soglia_Arrot_Fig_F_Can.HasValue ? currentCant.Soglia_Arrot_Fig_F_Can.Value : -1;

                                                minutesStart = currentCant.Minuti_Tolleranza_Can.HasValue ? currentCant.Minuti_Tolleranza_Can.Value : -1;
                                                minutesEnd = currentCant.Minuti_Tolleranza_F_Can.HasValue ? currentCant.Minuti_Tolleranza_F_Can.Value : -1;
                                            }
                                        }

                                        if (roundingEnum == RoundingMethodEnum.None)
                                        {
                                            roundingEnum = roundingParamEnum;

                                            thresholdStart = paramThresholdStart;
                                            thresholdEnd = paramThresholdEnd;
                                            minutesStart = paramTinutesStart;
                                            minutesEnd = paramTinutesEnd;
                                        }

                                        if (roundingEnum == RoundingMethodEnum.Duration || roundingEnum == RoundingMethodEnum.Disabled) {
                                            roundingEnum = RoundingMethodEnum.None;

                                            thresholdStart = 0;
                                            thresholdEnd = 0;
                                            minutesStart = 0;
                                            minutesEnd = 0;
                                        }

                                        if (currentCant.Tolleranza_Limite_Entrata_Cant != null) {
                                            delayMorningTollerance = currentCant.Tolleranza_Limite_Entrata_Cant.Value;
                                        }
                                        if (currentCol.Tolleranza_Limite_Entrata_Col != null) {
                                            delayMorningTollerance = currentCol.Tolleranza_Limite_Entrata_Col.Value;
                                        }

                                        List<Reg_V> currentRegVs = cantGroup.OrderBy(regV => regV.Data_Ora_Fis_E).ToList();
                                        Reg previousReg = null;

                                        foreach (Reg_V currentRegV in currentRegVs)
                                        {
                                            try { 
                                            Reg currentRegE = regsDic[currentRegV.RegE];
                                            Reg currentRegU = null;
                                            if (previousReg == null) {
                                                previousReg = currentRegE;
                                            }

                                            if (currentRegE.Registrazione_Tipo_RegEnum != RegTypeEnum.Att && currentRegE.Registrazione_Tipo_RegEnum != RegTypeEnum.Pass && currentRegV.RegU != null)
                                            //Se NON è una Attività allora imposto DataOra Fig Uscita = Data Ora Fis Uscita
                                            {
                                                //currentRegU = regs.Single(reg => reg.Reg_Id == currentRegV.RegU);
                                                currentRegU = regsDic[currentRegV.RegU.Value];
                                                currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg; 
                                            }

                                            //viene impostata la data ed ora FIGURATIVA uguale a quella fisica
                                            currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fis_Reg;

                                            if (currentCant.Raggruppamento1_Can is null)
                                                currentCant.Raggruppamento1_Can = "";
                                            #region ARROTONDAMENTO ENTRATA/USCITA
                                            //se si è nel caso di arrotondamento sull'entrata ed uscita
                                            if ((roundingEnum == RoundingMethodEnum.StartEnd && !currentCant.Raggruppamento1_Can.Equals("5")) || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.UseEUDurationRounding) == 1)
                                            {
                                                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.UseEUDurationRounding) == 1) {
                                                        if (roundingEnum != RoundingMethodEnum.StartEnd) {
                                                            thresholdStart = -1;
                                                            thresholdEnd = -1;
                                                        }
                                                    }
                                                #region 1.Arrotondo la Registrazione di Entrata

                                                //vengono estratti i minuti della corrente registrazione di entrata
                                                int currentRegEMin = currentRegE.Registrazione_Data_Ora_Fis_Reg.Minute;

                                                //creo una nuova variabile che rappresenterà il modulo dei minuti
                                                int moduleRegEMin = currentRegEMin;

                                                //viene controllato se i minuti di start inseriti sono maggiori di 0 (minuti arrotondamento entrata)
                                                if (minutesStart > 0)
                                                    //viene calcolato il valore come resto della divisione tra i minuti reali e il valore dei parametri
                                                    moduleRegEMin = currentRegEMin % minutesStart;

                                                //se si è in presenza di un valore della soglia sull'entrata valido(Soglia di entrata) 
                                                //se la soglia è uguale a zero si arrotonda sempre al limite successivo
                                                if (thresholdStart >= 0)
                                                {
                                                    //se il modulo dei minuti è maggiore della soglia di entrata impostata nei parametri
                                                    if (moduleRegEMin > thresholdStart)
                                                        //i nuovi minuti della registrazione figurativa di entrata sono uguali alla differenza tra minutesStart - moduleRegEMin
                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(minutesStart - moduleRegEMin);
                                                    else
                                                        //altrimenti i minuti della registrazione figurativa di entrata sono uguali al modulo * -1
                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(moduleRegEMin * -1);
                                                }

                                                #endregion

                                                #region 2.Arrotondo la Registrazione di Uscita

                                                //se sono in presenza di una registrazione di uscita
                                                if (currentRegU != null)
                                                {
                                                    //vengono estratti i minuti della registrazione di uscita
                                                    int currentRegUMin = currentRegU.Registrazione_Data_Ora_Fis_Reg.Minute;
                                                    int moduleRegUMin = currentRegUMin;

                                                    // viene controllato se i minuti di start inseriti sono maggiori di 0
                                                    if (minutesEnd > 0)
                                                        //viene calcolato il valore come resto della divisione tra i minuti reali e il valore dei parametri
                                                        moduleRegUMin = currentRegUMin % minutesEnd;

                                                    //se la soglia di uscita è valorizzata
                                                    if (thresholdEnd >= 0)
                                                    {
                                                        //se il modulo dei minuti è maggiore della soglia di uscita impostata nei parametri
                                                        if (moduleRegUMin > thresholdEnd)
                                                            //i nuovi minuti della registrazione figurativa di uscita sono uguali alla differenza tra minutesStart - moduleRegEMin
                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(minutesEnd - moduleRegUMin);
                                                        else
                                                            //altrimenti i minuti della registrazione figurativa di entrata sono uguali al modulo * -1
                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(moduleRegUMin * -1);
                                                    }
                                                }   
                                                
                                                #endregion

                                                #region 3.Gestione del limite d'entrata


                                                if ((utilizzoLimiteEntrata == (int)UtilizzoLimiteEntrata.LimiteEntrata) ||
                                                       (utilizzoLimiteEntrata == (int)UtilizzoLimiteEntrata.LimiteEntrataERitardo))

                                                {
                                                    #region Modifica per non arrotondare le ore modificate
                                                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoArrotondamentoOreModificate) == 1)
                                                    {
                                                        if ((currentRegV.Tipo_Modifica == 0 || currentRegV.Tipo_Modifica == 1 || currentRegV.Tipo_Modifica == 2) /*&& currentCol.Qualifica_Col != "0"*/ && currentCol.Raggruppamento2_Col != "1") {
                                                            // calcolo dei dati di limite d'entrata riguardo la registrazione che si sta processando
                                                            Dictionary<EntryLimitTypeEnum, EntryLimitData> entryLimitConfig = null;
                                                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiDaTurni) == 1)
                                                            {
                                                                entryLimitConfig = GetEntryLimitConifgOrario(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay, currentRegE, currentRegU);
                                                            }
                                                            else
                                                            {
                                                                entryLimitConfig = GetEntryLimitConifg(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay);
                                                            }

                                                            //primo limite della mattina, se non si è valorizzato il campo del limite resituisce mezzogiorno
                                                            TimeSpan fistMorningLimit = new TimeSpan(12, 0, 0);
                                                                TimeSpan tmpMidDay = midDay;

                                                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiXCol) == 1)
                                                                {
                                                                    if (currentCant.Telefono_1_Can != null) 
                                                                    {
                                                                        midDay = TimeSpan.Parse(currentCant.Telefono_2_Can);
                                                                        string[] Coperture = currentCant.Telefono_1_Can.Split(',');
                                                                        TimeSpan beforeE = TimeSpan.Parse(Coperture[1]).Subtract(new TimeSpan(0, 30, 0));
                                                                        TimeSpan afterE = TimeSpan.Parse(Coperture[1]).Add(new TimeSpan(0, 30, 0));
                                                                        TimeSpan beforeU = TimeSpan.Parse(currentCant.Telefono_1_Rif_Can).Subtract(new TimeSpan(0, 30, 0));
                                                                        TimeSpan afterU = TimeSpan.Parse(currentCant.Telefono_1_Rif_Can).Add(new TimeSpan(0, 30, 0));
                                                                        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > beforeE/*&& (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > beforeU && currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < afterU)*/)
                                                                        {
                                                                            entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime = TimeSpan.Parse(Coperture[1]);
                                                                            currentRegE.Turno = "Coperture Serali";
                                                                        }
                                                                        else if (currentRegE.Turno == "Coperture Serali")
                                                                        {
                                                                            entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime = TimeSpan.Parse(Coperture[0]);
                                                                        }
                                                                    }
                                                                }

                                                                // se è configurata la gestione del limite d'entrata (valorizzata o per la mattina, per il pomeriggio o per orario) e se la registrazione 
                                                                // risulta abbinata allora si procede (se la registrazione d'entrata risulta presente) come segue:
                                                                // - in caso di registrazione mattutina (ante metà giornata configurata) allora si verifica che, se configurato il limite d'entrata mattutino,
                                                                //   la registrazione sia antecedente a tale ora; in questo caso l'ora figurativa dell'entrata viene spostata al limite d'entrata.
                                                                // - in caso non si tratti di una registrazione mattutina (post metà giornata configurata) allora si verifica che, 
                                                                //   se configurato il limite d'entrata pomeridiano e la registrazione abbinata sia a cavallo del limite e nella tolleranza esplicitata 
                                                                //   (se non configurata si è sicuramente fuori tolleranza) allora si procede allo spostamento dell'ora figurativa d'entrata al limite d'entrata
                                                                // al termine dell'operazione in ogni caso, se l'uscita risulta inferiore all'entrata, si procede al suo spostamento per far coincidere il dato.

                                                                // se la registrazione risulta abbinata e ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                                                if (currentRegV.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && entryLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegE != null)
                                                            {
                                                                // se la registrazione risulta essere mattutina ed è configurato il limite d'entrata mattutino,
                                                                // altrimenti se la registrazione risulta essere pomeridiana e risulta configurato n limite d'entrata pomeridiano
                                                                if (currentRegV.Data_Ora_Fis_E.TimeOfDay < midDay && entryLimitConfig[EntryLimitTypeEnum.Morning].IsConfigured)
                                                                {
                                                                    //se si ha il limite d'entrata configurato viene impostato come limite mattutino il limite d'entrata
                                                                    if (entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime != null)
                                                                        fistMorningLimit = entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value;

                                                                        // se l'ora figurativa dell'entrata è inferiore al limite d'entrata allora viene spostata al limite d'entrata;
                                                                        if (delayMorningTollerance != null && currentCant.Importo4 == null && currentCol.Trattenuta_Vitto_Col == null)
                                                                        {
                                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Subtract(delayMorningTollerance))
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Hours,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Minutes,
                                                                                    0);

                                                                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DelayAfter) == 1)
                                                                            {
                                                                                if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Subtract(delayMorningTollerance))
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                        entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Hours,
                                                                                        entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Minutes,
                                                                                        0);
                                                                            }
                                                                        } else if (currentCol.Trattenuta_Vitto_Col != null) {
                                                                            int tolleranzaPre = 180;
                                                                            if (currentCol.Trattenuta_Vitto_Col != null)
                                                                            {
                                                                                if (currentCol.Trattenuta_Vitto_Col.Value > 0)
                                                                                    tolleranzaPre = (int)currentCol.Trattenuta_Vitto_Col.Value;
                                                                            }
                                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Subtract(new TimeSpan(0, tolleranzaPre, 0)))
                                                                            {
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                currentCol.Limite_Entrata_Mattina_Col.Value.Hours,
                                                                                currentCol.Limite_Entrata_Mattina_Col.Value.Minutes,
                                                                                0);
                                                                            }
                                                                        } else if (currentCant.Importo4 != null) {
                                                                            int tolleranzaPre = 180;
                                                                            if (currentCant.Importo4 != null)
                                                                            {
                                                                                if (currentCant.Importo4.Value > 0)
                                                                                    tolleranzaPre = (int)currentCant.Importo4.Value;
                                                                            }
                                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Subtract(new TimeSpan(0, tolleranzaPre, 0)))
                                                                            {
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                currentCant.Limite_Entrata_Mattina_Cant.Value.Hours,
                                                                                currentCant.Limite_Entrata_Mattina_Cant.Value.Minutes,
                                                                                0);
                                                                            }
                                                                        }
                                                                        else if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value)
                                                                        {
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Hours,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Minutes,
                                                                                    0);
                                                                        }

                                                                    //limite d'entrata su cant
                                                                    //if (currentCant.Limite_Entrata_Mattina_Cant != null || currentCol.Limite_Entrata_Mattina_Col != null)
                                                                    //{
                                                                    //    TimeSpan entryLimit = new TimeSpan();
                                                                    //
                                                                    //    if (currentCol.Limite_Entrata_Mattina_Col != null)
                                                                    //    {
                                                                    //        List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                                                                    //        if (currentCol.Tolleranza_Limite_Entrata_Col != null)
                                                                    //        {
                                                                    //            entryLimit = currentCol.Limite_Entrata_Mattina_Col.Value.Add((TimeSpan)currentCol.Tolleranza_Limite_Entrata_Col);
                                                                    //        }
                                                                    //        else
                                                                    //        {
                                                                    //            entryLimit = currentCol.Limite_Entrata_Mattina_Col.Value.Add(parametri.First().Tolleranza_Limite_Entrata.Value);
                                                                    //        }
                                                                    //        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit)
                                                                    //        {
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                    //            currentCol.Limite_Entrata_Mattina_Col.Value.Hours,
                                                                    //            currentCol.Limite_Entrata_Mattina_Col.Value.Minutes,
                                                                    //            0);
                                                                    //        }
                                                                    //    }
                                                                    //    else if (currentCant.Limite_Entrata_Mattina_Cant != null)
                                                                    //    {
                                                                    //        List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                                                                    //        if (currentCant.Tolleranza_Limite_Entrata_Cant != null)
                                                                    //        {
                                                                    //            entryLimit = currentCant.Limite_Entrata_Mattina_Cant.Value.Add((TimeSpan)currentCant.Tolleranza_Limite_Entrata_Cant);
                                                                    //        }
                                                                    //        else
                                                                    //        {
                                                                    //            entryLimit = currentCant.Limite_Entrata_Mattina_Cant.Value.Add(parametri.First().Tolleranza_Limite_Entrata.Value);
                                                                    //        }
                                                                    //        //entryLimit = currentCant.Limite_Entrata_Mattina_Cant.Value.Add((TimeSpan)currentCant.Tolleranza_Limite_Entrata_Cant);
                                                                    //        //imposto la tolleranza per il limite pre a 3 ore per i cantieri che non hanno vincoli
                                                                    //        //uso il valore importo4 per i cantieri con il vincolo (Komplett)
                                                                    //        int tolleranzaPre = 180;
                                                                    //        if (currentCant.Importo4 != null) {
                                                                    //            if(currentCant.Importo4.Value > 0)
                                                                    //            tolleranzaPre = (int)currentCant.Importo4.Value;
                                                                    //        }
                                                                    //        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit && currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > currentCant.Limite_Entrata_Mattina_Cant.Value.Subtract(new TimeSpan(0, tolleranzaPre, 0)))
                                                                    //        {
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                    //            currentCant.Limite_Entrata_Mattina_Cant.Value.Hours,
                                                                    //            currentCant.Limite_Entrata_Mattina_Cant.Value.Minutes,
                                                                    //            0);
                                                                    //        }
                                                                    //        else { 
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                    //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                    //            currentRegE.Registrazione_Data_Ora_Fis_Reg.Hour,
                                                                    //            currentRegE.Registrazione_Data_Ora_Fis_Reg.Minute,
                                                                    //            0);
                                                                    //        }
                                                                    //    }
                                                                    //
                                                                    //}

                                                                }
                                                                else if (currentRegV.Data_Ora_Fis_E.TimeOfDay >= midDay && entryLimitConfig[EntryLimitTypeEnum.Afternoon].IsConfigured)
                                                                {
                                                                    //se si ha il limite d'entrata configurato viene impostato come limite mattutino il limite d'entrata
                                                                    if (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime != null)
                                                                        fistMorningLimit = entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value;

                                                                    // se l'ora figurativa dell'entrata è inferiore al limite d'entrata allora viene spostata al limite d'entrata;
                                                                    if (delayAfternoonTollerance != null)
                                                                    {
                                                                        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Subtract(delayAfternoonTollerance))
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                                0);

                                                                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DelayAfter) == 1)
                                                                        {
                                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Subtract(delayAfternoonTollerance))
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                                    0);
                                                                        }
                                                                    }
                                                                    else if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value)
                                                                    {
                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                                entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                                0);
                                                                    }

                                                                    //limite d'entrata su cant
                                                                    if (currentCant.Limite_Entrata_Mattina_Cant != null || currentCol.Limite_Entrata_Mattina_Col != null)
                                                                    {
                                                                        TimeSpan entryLimit = new TimeSpan();

                                                                        if (currentCol.Limite_Entrata_Mattina_Col != null)
                                                                        {
                                                                            List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                                                                            if (currentCol.Tolleranza_Limite_Entrata_Col != null)
                                                                            {
                                                                                entryLimit = currentCol.Limite_Entrata_Pomeriggio_Col.Value.Add((TimeSpan)currentCol.Tolleranza_Limite_Entrata_Col);
                                                                            }
                                                                            else
                                                                            {
                                                                                entryLimit = currentCol.Limite_Entrata_Pomeriggio_Col.Value.Add(parametri.First().Tolleranza_Limite_Entrata_Pomeriggio.Value);
                                                                            }

                                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit)
                                                                            {
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                currentCol.Limite_Entrata_Pomeriggio_Col.Value.Hours,
                                                                                currentCol.Limite_Entrata_Pomeriggio_Col.Value.Minutes,
                                                                                0);
                                                                            }
                                                                        }
                                                                        else if (currentCant.Limite_Entrata_Pomeriggio_Cant != null)
                                                                        {
                                                                            entryLimit = currentCant.Limite_Entrata_Pomeriggio_Cant.Value.Add((TimeSpan)currentCant.Tolleranza_Limite_Entrata_Cant);

                                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit)
                                                                            {
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                currentCant.Limite_Entrata_Pomeriggio_Cant.Value.Hours,
                                                                                currentCant.Limite_Entrata_Pomeriggio_Cant.Value.Minutes,
                                                                                0);
                                                                            }
                                                                        }

                                                                    }
                                                                    /*                      
                                                                                                                                // viene recuperata la tolleranza del limite d'entrata (se non configurata si contano le 12 ore per coprire l'intera mezza giornata)
                                                                                                                                TimeSpan entryLimitTollerance = entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTollerance ?? new TimeSpan(12, 0, 0);

                                                                                                                                // se la registrazione è a cavallo del limite d'entrata e in tolleranza
                                                                                                                                // allora l'ora figurativa viene spostata al limite d'entrata
                                                                                                                                if (currentRegV.Data_Ora_Fis_E.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value
                                                                                                                                    && currentRegV.Data_Ora_Fis_U.Value.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value
                                                                                                                                    && (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_E.TimeOfDay.Ticks) <= entryLimitTollerance.Ticks)
                                                                                                                                {
                                                                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                                                                        entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                                                                                        entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                                                                                        0);
                                                                                                                                }

                                                                                                                                //se si è in presenza di una registrazione notturna
                                                                                                                                if (currentRegV.Data_Ora_Fis_U.Value.Date > currentRegV.Data_Ora_Fis_E.Date)
                                                                                                                                {
                                                                                                                                    //se il limite pomeridiano è uguale o maggiore alla mezzanotte ma inferiore al limite mattutino ed entro la tolleranza
                                                                                                                                    if (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value >= midNight
                                                                                                                                             && entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value < fistMorningLimit
                                                                                                                                             && (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_E.TimeOfDay.Ticks) <= entryLimitTollerance.Ticks)
                                                                                                                                    {
                                                                                                                                        //come registrazione figurativa di entrata viene presa la data dell'uscita e i minuti dati dal limite pomeridiano impostatao dai parametri
                                                                                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                                                                        entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                                                                                        entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                                                                                        0);
                                                                                                                                    }

                                                                                                                                    //se si ha una registrazione notturna ma il limite cade prima della mezzanotte
                                                                                                                                    else if (currentRegV.Data_Ora_Fis_E.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value
                                                                                                                                        && (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_E.TimeOfDay.Ticks) <= entryLimitTollerance.Ticks)
                                                                                                                                    {
                                                                                                                                        //la registrazione figurativa prende la data dalla registrazione di entrata
                                                                                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                                                                        entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                                                                                        entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                                                                                        0);

                                                                                                                                    }
                                                                                                                                }*/
                                                                }

                                                                // in ogni caso, al termine dell'operazione, se è presente una registrazione d'uscita
                                                                // e l'ora figurativa di questa è inferiore all'ora figurativa dell'entrata allora 
                                                                // si porta l'ora d'uscita all'ora d'entrata (a patto che si trovino nella stessa data - questione degli arrotondamenti a 00:00)
                                                                if (currentRegU != null)
                                                                    if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay
                                                                        && currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Date == currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Date)
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg;

                                                                    midDay = tmpMidDay;
                                                                }
                                                        }
                                                        
                                                    }
                                                    #endregion
                                                    else
                                                    {
                                                        // calcolo dei dati di limite d'entrata riguardo la registrazione che si sta processando
                                                        Dictionary<EntryLimitTypeEnum, EntryLimitData> entryLimitConfig = null;

                                                        midDay = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Inizio_Pomeriggio ?? new TimeSpan(12, 0, 0);
                                                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiDaTurni) == 1)
                                                        {
                                                            entryLimitConfig = GetEntryLimitConifgOrario(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay, currentRegE, currentRegU);
                                                        }
                                                        else
                                                        {
                                                            entryLimitConfig = GetEntryLimitConifg(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay);
                                                        }

                                                        //primo limite della mattina, se non si è valorizzato il campo del limite resituisce mezzogiorno
                                                        TimeSpan fistMorningLimit = new TimeSpan(12, 0, 0);

                                                        //if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Usa_Orario || RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Usa_Orario) {
                                                        //    TimeSpan tmp = entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Subtract(TimeSpan.FromMinutes(5));
                                                        //    midDay = tmp;
                                                        //}

                                                        // se è configurata la gestione del limite d'entrata (valorizzata o per la mattina, per il pomeriggio o per orario) e se la registrazione 
                                                        // risulta abbinata allora si procede (se la registrazione d'entrata risulta presente) come segue:
                                                        // - in caso di registrazione mattutina (ante metà giornata configurata) allora si verifica che, se configurato il limite d'entrata mattutino,
                                                        //   la registrazione sia antecedente a tale ora; in questo caso l'ora figurativa dell'entrata viene spostata al limite d'entrata.
                                                        // - in caso non si tratti di una registrazione mattutina (post metà giornata configurata) allora si verifica che, 
                                                        //   se configurato il limite d'entrata pomeridiano e la registrazione abbinata sia a cavallo del limite e nella tolleranza esplicitata 
                                                        //   (se non configurata si è sicuramente fuori tolleranza) allora si procede allo spostamento dell'ora figurativa d'entrata al limite d'entrata
                                                        // al termine dell'operazione in ogni caso, se l'uscita risulta inferiore all'entrata, si procede al suo spostamento per far coincidere il dato.

                                                        // se la registrazione risulta abbinata e ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                                        if (currentRegV.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && entryLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegE != null)
                                                        {
                                                            // se la registrazione risulta essere mattutina ed è configurato il limite d'entrata mattutino,
                                                            // altrimenti se la registrazione risulta essere pomeridiana e risulta configurato n limite d'entrata pomeridiano
                                                            if (currentRegV.Data_Ora_Fis_E.TimeOfDay < midDay && entryLimitConfig[EntryLimitTypeEnum.Morning].IsConfigured)
                                                            {
                                                                //se si ha il limite d'entrata configurato viene impostato come limite mattutino il limite d'entrata
                                                                if (entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime != null)
                                                                    fistMorningLimit = entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value;

                                                                // se l'ora figurativa dell'entrata è inferiore al limite d'entrata allora viene spostata al limite d'entrata;
                                                                if (delayMorningTollerance != null && delayMorningTollerance > new TimeSpan(0, 0, 0))
                                                                {
                                                                    if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Subtract(delayMorningTollerance))
                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Hours,
                                                                            entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Minutes,
                                                                            0);
                                                                    //utilizzo la tolleranza anche dentro all'orario lavorativo
                                                                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DelayAfter) == 1)
                                                                    {
                                                                        if (currentCant.Turno4_Can.HasValue) {
                                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Add(currentCant.Turno4_Can.Value))
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Hours,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Minutes,
                                                                                    0);
                                                                        }
                                                                    }
                                                                }
                                                                else if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value)
                                                                {
                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Hours,
                                                                            entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Minutes,
                                                                            0);
                                                                }

                                                                //limite d'entrata su cant
                                                                //if (currentCant.Limite_Entrata_Mattina_Cant != null || currentCol.Limite_Entrata_Mattina_Col != null)
                                                                //{
                                                                //    TimeSpan entryLimit = new TimeSpan();
                                                                //
                                                                //    if (currentCol.Limite_Entrata_Mattina_Col != null)
                                                                //    {
                                                                //        List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                                                                //        if (currentCol.Tolleranza_Limite_Entrata_Col != null)
                                                                //        {
                                                                //            entryLimit = currentCol.Limite_Entrata_Mattina_Col.Value.Add((TimeSpan)currentCol.Tolleranza_Limite_Entrata_Col);
                                                                //        }
                                                                //        else
                                                                //        {
                                                                //            entryLimit = currentCol.Limite_Entrata_Mattina_Col.Value.Add(parametri.First().Tolleranza_Limite_Entrata.Value);
                                                                //        }
                                                                //        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoArrotColAuthorized) == 1 && currentCol.Tipo_Arrotondamento_Col == 4)
                                                                //        {
                                                                //            int soglia = int.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.NoArrotColAuthorized, "soglia"));
                                                                //            TimeSpan entryLimitAuthorized = currentCol.Limite_Entrata_Mattina_Col.Value.Subtract(new TimeSpan(0,soglia,0));
                                                                //            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit && currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > entryLimitAuthorized) {
                                                                //                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                //                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                //                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                //                currentCol.Limite_Entrata_Mattina_Col.Value.Hours,
                                                                //                currentCol.Limite_Entrata_Mattina_Col.Value.Minutes,
                                                                //                0);
                                                                //            }
                                                                //        }
                                                                //        else {
                                                                //            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit)
                                                                //            {
                                                                //                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                //                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                //                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                //                currentCol.Limite_Entrata_Mattina_Col.Value.Hours,
                                                                //                currentCol.Limite_Entrata_Mattina_Col.Value.Minutes,
                                                                //                0);
                                                                //            }
                                                                //        } 
                                                                //    }
                                                                //    else if (currentCant.Limite_Entrata_Mattina_Cant != null)
                                                                //    {
                                                                //        List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                                                                //        if (currentCant.Tolleranza_Limite_Entrata_Cant != null)
                                                                //        {
                                                                //            entryLimit = currentCant.Limite_Entrata_Mattina_Cant.Value.Add((TimeSpan)currentCant.Tolleranza_Limite_Entrata_Cant);
                                                                //        }
                                                                //        else
                                                                //        {
                                                                //            entryLimit = currentCant.Limite_Entrata_Mattina_Cant.Value.Add(parametri.First().Tolleranza_Limite_Entrata.Value);
                                                                //        }
                                                                //        //entryLimit = currentCant.Limite_Entrata_Mattina_Cant.Value.Add((TimeSpan)currentCant.Tolleranza_Limite_Entrata_Cant);
                                                                //
                                                                //        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit)
                                                                //        {
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                //            currentCant.Limite_Entrata_Mattina_Cant.Value.Hours,
                                                                //            currentCant.Limite_Entrata_Mattina_Cant.Value.Minutes,
                                                                //            0);
                                                                //        }
                                                                //    }
                                                                //
                                                                //}

                                                            }
                                                            else if (currentRegV.Data_Ora_Fis_E.TimeOfDay >= midDay && entryLimitConfig[EntryLimitTypeEnum.Afternoon].IsConfigured)
                                                            {
                                                                //se si ha il limite d'entrata configurato viene impostato come limite mattutino il limite d'entrata
                                                                if (entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime != null)
                                                                    fistMorningLimit = entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value;

                                                                // se l'ora figurativa dell'entrata è inferiore al limite d'entrata allora viene spostata al limite d'entrata;
                                                                if (delayAfternoonTollerance != new TimeSpan(0, 0, 0))
                                                                {
                                                                    if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Subtract(delayAfternoonTollerance))
                                                                        currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                            entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                            0);

                                                                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DelayAfter) == 1)
                                                                    {
                                                                        if (currentCant.Turno4_Can.HasValue)
                                                                        {
                                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value && currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Add(currentCant.Turno4_Can.Value))
                                                                                currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Hours,
                                                                                    entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime.Value.Minutes,
                                                                                    0);
                                                                        }
                                                                    }
                                                                }
                                                                else if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value)
                                                                {
                                                                    currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Hours,
                                                                            entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime.Value.Minutes,
                                                                            0);
                                                                }

                                                                //limite d'entrata su cant
                                                                //if (currentCant.Limite_Entrata_Mattina_Cant != null || currentCol.Limite_Entrata_Mattina_Col != null)
                                                                //{
                                                                //    TimeSpan entryLimit = new TimeSpan();
                                                                //
                                                                //    if (currentCol.Limite_Entrata_Mattina_Col != null)
                                                                //    {
                                                                //        List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                                                                //        if (currentCol.Tolleranza_Limite_Entrata_Col != null)
                                                                //        {
                                                                //            entryLimit = currentCol.Limite_Entrata_Mattina_Col.Value.Add((TimeSpan)currentCol.Tolleranza_Limite_Entrata_Col);
                                                                //        }
                                                                //        else
                                                                //        {
                                                                //            entryLimit = currentCol.Limite_Entrata_Mattina_Col.Value.Add(parametri.First().Tolleranza_Limite_Entrata_Pomeriggio.Value);
                                                                //        }
                                                                //
                                                                //        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit)
                                                                //        {
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                //            currentCol.Limite_Entrata_Mattina_Col.Value.Hours,
                                                                //            currentCol.Limite_Entrata_Mattina_Col.Value.Minutes,
                                                                //            0);
                                                                //        }
                                                                //    }
                                                                //    else if (currentCant.Limite_Entrata_Pomeriggio_Cant != null)
                                                                //    {
                                                                //        entryLimit = currentCant.Limite_Entrata_Pomeriggio_Cant.Value.Add((TimeSpan)currentCant.Tolleranza_Limite_Entrata_Cant);
                                                                //
                                                                //        if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < entryLimit)
                                                                //        {
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                //            currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                //            currentCant.Limite_Entrata_Pomeriggio_Cant.Value.Hours,
                                                                //            currentCant.Limite_Entrata_Pomeriggio_Cant.Value.Minutes,
                                                                //            0);
                                                                //        }
                                                                //    }
                                                                //
                                                                //}
                                                            }

                                                            // in ogni caso, al termine dell'operazione, se è presente una registrazione d'uscita
                                                            // e l'ora figurativa di questa è inferiore all'ora figurativa dell'entrata allora 
                                                            // si porta l'ora d'uscita all'ora d'entrata (a patto che si trovino nella stessa data - questione degli arrotondamenti a 00:00)
                                                            if (currentRegU != null)
                                                                if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay
                                                                    && currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Date == currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Date)
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg;
                                                        }
                                                    }
                                                }

                                                #endregion

                                                #region 4.Gestione limite di uscita

                                                if (utilizzoLimiteUscita == (int)UtilizzoLimiteUscita.LimiteUscita)
                                                {
                                                    #region Modifica per non arrotondare le ore modificate
                                                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoArrotondamentoOreModificate) == 1)
                                                    {
                                                        if ((currentRegV.Tipo_Modifica == 0 || currentRegV.Tipo_Modifica == 3 || currentRegV.Tipo_Modifica == 6)/* && currentCol.Qualifica_Col != "0"*/ && currentCol.Raggruppamento2_Col != "1") {
                                                            // calcolo dei dati di limite d'entrata riguardo la registrazione che si sta processando
                                                            Dictionary<ExitLimitTypeEnum, ExitLimitData> exitLimitConfig = null;
                                                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiDaTurni) == 1)
                                                            {
                                                                exitLimitConfig = GetExitLimitConifgOrario(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay, currentRegE, currentRegU);
                                                            }
                                                            else
                                                            {
                                                                exitLimitConfig = GetExitLimitConifg(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay);
                                                            }

                                                            //primo limite della mattina, se non si è valorizzato il campo del limite resituisce mezzogiorno
                                                            TimeSpan fistMorningLimit = new TimeSpan(12, 0, 0);
                                                            List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                                                            midDay = parametri.First().Limite_Entrata_Inizio_Pomeriggio ?? new TimeSpan(12, 0, 0);

                                                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiXCol) == 1)
                                                                {
                                                                    if (currentCant.Telefono_1_Can != null) 
                                                                    {
                                                                        midDay = TimeSpan.Parse(currentCant.Telefono_1_Rif_Can);
                                                                        if (currentRegE.Turno == "Coperture Serali")
                                                                        {
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime = TimeSpan.Parse(currentCant.Telefono_1_Rif_Can);
                                                                        }
                                                                    }
                                                                }


                                                                // se è configurata la gestione del limite d'uscita (valorizzata o per la mattina, per il pomeriggio o per orario) e se la registrazione 
                                                                // risulta abbinata allora si procede (se la registrazione d'entrata risulta presente) come segue:
                                                                // - in caso di registrazione mattutina (ante metà giornata configurata) allora si verifica che, se configurato il limite d'uscita mattutino,
                                                                //   la registrazione sia seguente a tale ora; in questo caso l'ora figurativa dell'uscita viene spostata al limite d'uscita.
                                                                // - in caso non si tratti di una registrazione mattutina (post metà giornata configurata) allora si verifica che, 
                                                                //   se configurato il limite d'uscita pomeridiano e la registrazione abbinata sia a cavallo del limite e nella tolleranza esplicitata 
                                                                //   (se non configurata si è sicuramente fuori tolleranza) allora si procede allo spostamento dell'ora figurativa d'uscita al limite d'entrata
                                                                // al termine dell'operazione in ogni caso, se l'uscita risulta inferiore all'entrata, si procede al suo spostamento per far coincidere il dato.

                                                                // se la registrazione risulta abbinata e ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                                                if (currentRegV.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && exitLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegU != null)
                                                            {
                                                                // se la registrazione risulta essere mattutina ed è configurato il limite d'uscita mattutino,
                                                                // altrimenti se la registrazione risulta essere pomeridiana e risulta configurato n limite d'uscita pomeridiano
                                                                if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay < midDay && exitLimitConfig[ExitLimitTypeEnum.Morning].IsConfigured)
                                                                {
                                                                    TimeSpan exitLimitMorningTollerance = exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTollerance ?? new TimeSpan(0, 0, 1);

                                                                    //se si ha il limite d'uscita configurato viene impostato come limite mattutino il limite d'uscita
                                                                    if (exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime != null)
                                                                        fistMorningLimit = exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value;

                                                                    // se l'ora figurativa dell'uscita è maggiore del limite d'uscita allora viene spostata al limite d'uscita;
                                                                    if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value
                                                                        && (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Subtract(exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value) <= exitLimitMorningTollerance.Duration()))
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Hours,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Minutes,
                                                                            0);
                                                                }
                                                                else if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay >= midDay && exitLimitConfig[ExitLimitTypeEnum.Afternoon].IsConfigured)
                                                                {
                                                                    // viene recuperata la tolleranza del limite d'uscita (se non configurata si contano le 12 ore per coprire l'intera mezza giornata)
                                                                    TimeSpan exitLimitAfternoonTollerance = exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTollerance ?? new TimeSpan(12, 0, 0);

                                                                    // se la registrazione è a cavallo del limite d'uscita e in tolleranza
                                                                    // allora l'ora figurativa viene spostata al limite d'uscita
                                                                    if ((currentRegV.Data_Ora_Fis_U.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value)
                                                                        && (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Subtract(exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value) < exitLimitAfternoonTollerance.Duration()))
                                                                    {
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                            0);
                                                                    }

                                                                    if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Add(exitLimitAfternoonTollerance))
                                                                    {
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                            0);
                                                                    }

                                                                    //se si è in presenza di una registrazione notturna
                                                                    if (currentRegV.Data_Ora_Fis_U.Value.Date > currentRegV.Data_Ora_Fis_U.Value.Date)
                                                                    {
                                                                        //se il limite pomeridiano è uguale o maggiore alla mezzanotte ma inferiore al limite mattutino ed entro la tolleranza
                                                                        if (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value >= midNight
                                                                                 && exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value < fistMorningLimit
                                                                                 && (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Ticks) <= exitLimitAfternoonTollerance.Ticks)
                                                                        {
                                                                            //come registrazione figurativa di entrata viene presa la data dell'uscita e i minuti dati dal limite pomeridiano impostatao dai parametri
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                            0);

                                                                        }

                                                                        //se si ha una registrazione notturna ma il limite cade prima della mezzanotte
                                                                        else if (currentRegV.Data_Ora_Fis_E.TimeOfDay < exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value
                                                                            && (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Ticks) <= exitLimitAfternoonTollerance.Ticks)
                                                                        {
                                                                            //la registrazione figurativa prende la data dalla registrazione di entrata
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                            exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                            0);

                                                                        }
                                                                    }
                                                                }

                                                                // in ogni caso, al termine dell'operazione, se è presente una registrazione d'uscita
                                                                // e l'ora figurativa di questa è inferiore all'ora figurativa dell'entrata allora 
                                                                // si porta l'ora d'uscita all'ora d'entrata (a patto che si trovino nella stessa data - questione degli arrotondamenti a 00:00)
                                                                if (currentRegU != null)
                                                                    if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay
                                                                        && currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Date == currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Date)
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg;
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                    else
                                                    {
                                                        // calcolo dei dati di limite d'entrata riguardo la registrazione che si sta processando
                                                        Dictionary<ExitLimitTypeEnum, ExitLimitData> exitLimitConfig = null;
                                                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiDaTurni) == 1)
                                                        {
                                                            exitLimitConfig = GetExitLimitConifgOrario(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay, currentRegE, currentRegU);
                                                        }
                                                        else
                                                        {
                                                            exitLimitConfig = GetExitLimitConifg(currentCant, currentCol, currentRegV.Data_Reg.Value, midDay);
                                                        }

                                                        //primo limite della mattina, se non si è valorizzato il campo del limite resituisce mezzogiorno
                                                        TimeSpan fistMorningLimit = new TimeSpan(12, 30, 0);
                                                        List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                                                        midDay = parametri.First().Limite_Entrata_Inizio_Pomeriggio ?? new TimeSpan(12, 30, 0);


                                                        // se è configurata la gestione del limite d'uscita (valorizzata o per la mattina, per il pomeriggio o per orario) e se la registrazione 
                                                        // risulta abbinata allora si procede (se la registrazione d'entrata risulta presente) come segue:
                                                        // - in caso di registrazione mattutina (ante metà giornata configurata) allora si verifica che, se configurato il limite d'uscita mattutino,
                                                        //   la registrazione sia seguente a tale ora; in questo caso l'ora figurativa dell'uscita viene spostata al limite d'uscita.
                                                        // - in caso non si tratti di una registrazione mattutina (post metà giornata configurata) allora si verifica che, 
                                                        //   se configurato il limite d'uscita pomeridiano e la registrazione abbinata sia a cavallo del limite e nella tolleranza esplicitata 
                                                        //   (se non configurata si è sicuramente fuori tolleranza) allora si procede allo spostamento dell'ora figurativa d'uscita al limite d'entrata
                                                        // al termine dell'operazione in ogni caso, se l'uscita risulta inferiore all'entrata, si procede al suo spostamento per far coincidere il dato.

                                                        // se la registrazione risulta abbinata e ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                                        if (currentRegV.Registrazione_Stato_Reg == (int)RegStateEnum.Ass && exitLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegU != null)
                                                        {
                                                            // se la registrazione risulta essere mattutina ed è configurato il limite d'uscita mattutino,
                                                            // altrimenti se la registrazione risulta essere pomeridiana e risulta configurato n limite d'uscita pomeridiano
                                                            if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay < midDay && exitLimitConfig[ExitLimitTypeEnum.Morning].IsConfigured)
                                                            {
                                                                TimeSpan exitLimitMorningTollerance = exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTollerance ?? new TimeSpan(0, 0, 1);

                                                                //se si ha il limite d'uscita configurato viene impostato come limite mattutino il limite d'uscita
                                                                if (exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime != null)
                                                                    fistMorningLimit = exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value;

                                                                // se l'ora figurativa dell'uscita è maggiore del limite d'uscita allora viene spostata al limite d'uscita;
                                                                if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value
                                                                    && (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Subtract(exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value) <= exitLimitMorningTollerance.Duration()))
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Hours,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Minutes,
                                                                        0);

                                                                if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Subtract(exitLimitMorningTollerance.Duration()))
                                                                {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Hours,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Morning].ExitLimitTime.Value.Minutes,
                                                                        0);
                                                                }
                                                            }
                                                            else if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay >= midDay && exitLimitConfig[ExitLimitTypeEnum.Afternoon].IsConfigured)
                                                            {
                                                                // viene recuperata la tolleranza del limite d'uscita (se non configurata si contano le 12 ore per coprire l'intera mezza giornata)
                                                                TimeSpan exitLimitAfternoonTollerance = exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTollerance ?? new TimeSpan(12, 0, 0);

                                                                // se la registrazione è a cavallo del limite d'uscita e in tolleranza
                                                                // allora l'ora figurativa viene spostata al limite d'uscita
                                                                if ((currentRegV.Data_Ora_Fis_U.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value)
                                                                    && (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Subtract(exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value) < exitLimitAfternoonTollerance.Duration()))
                                                                {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                        0);
                                                                }
                                                                if (currentRegV.Data_Ora_Fis_U.Value.TimeOfDay > exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Subtract(exitLimitAfternoonTollerance)) {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                        0);
                                                                }

                                                                //se si è in presenza di una registrazione notturna
                                                                if (currentRegV.Data_Ora_Fis_U.Value.Date > currentRegV.Data_Ora_Fis_E.Date)
                                                                {
                                                                    //se il limite pomeridiano è uguale o maggiore alla mezzanotte ma inferiore al limite mattutino ed entro la tolleranza
                                                                    if (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value >= midNight
                                                                             && exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value < fistMorningLimit
                                                                             && (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Ticks) <= exitLimitAfternoonTollerance.Ticks)
                                                                    {
                                                                        //come registrazione figurativa di entrata viene presa la data dell'uscita e i minuti dati dal limite pomeridiano impostatao dai parametri
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                        0);

                                                                    }

                                                                    //se si ha una registrazione notturna ma il limite cade prima della mezzanotte
                                                                    else if (currentRegV.Data_Ora_Fis_E.TimeOfDay < exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value
                                                                        && (exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Ticks - currentRegV.Data_Ora_Fis_U.Value.TimeOfDay.Ticks) <= exitLimitAfternoonTollerance.Ticks)
                                                                    {
                                                                        //la registrazione figurativa prende la data dalla registrazione di entrata
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                                        currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Hours,
                                                                        exitLimitConfig[ExitLimitTypeEnum.Afternoon].ExitLimitTime.Value.Minutes,
                                                                        0);

                                                                    }
                                                                }
                                                            }

                                                            // in ogni caso, al termine dell'operazione, se è presente una registrazione d'uscita
                                                            // e l'ora figurativa di questa è inferiore all'ora figurativa dell'entrata allora 
                                                            // si porta l'ora d'uscita all'ora d'entrata (a patto che si trovino nella stessa data - questione degli arrotondamenti a 00:00)
                                                            if (currentRegU != null)
                                                                if (currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay < currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay
                                                                    && currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Date == currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Date)
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fig_Reg;
                                                        }
                                                    }
                                                }
                                                    

                                                #endregion

                                                #region Arrotondamenti Casp
                                                //Personalizzazione casp
                                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.Casp) == 1 && !currentCant.Raggruppamento1_Can.Equals("5"))
                                                {
                                                    if (currentRegU != null)
                                                    {
                                                        if (currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek != DayOfWeek.Sunday && !RepoManager.Tab_FestiviRepo.IsHolidayOrNotWorkDays(currentRegU.Registrazione_Data_Ora_Fis_Reg.Date))
                                                        {

                                                            var mondayFridayPause = Int32.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "mondayFridayPause")); //Minuti
                                                            var saturdayPause = Int32.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "saturdayPause")); //Minuti
                                                            var extraTimePause = Int32.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "extraTimePause")); //Minuti
                                                            double amount = double.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "amount"));
                                                            var startRounding = new TimeSpan();

                                                            DateTime elaborateStartDate = new DateTime();
                                                            TimeSpan weekMaxSchedule = new TimeSpan();
                                                            TimeSpan weekMinSchedule = new TimeSpan();
                                                            TimeSpan saturdayMaxSchedule = new TimeSpan();
                                                            TimeSpan saturdayMinSchedule = new TimeSpan();
                                                            var winterDate = DateTime.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "elaborateWinterStartDate"));
                                                            var summerDate = DateTime.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "elaborateSummerStartDate"));

                                                            if (IsBetween(currentRegE.Registrazione_Data_Ora_Fis_Reg.Date, summerDate, winterDate))
                                                            {
                                                                elaborateStartDate = summerDate;
                                                                weekMaxSchedule = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "summerWeekMaxSchedule"));
                                                                weekMinSchedule = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "summerWeekMinSchedule"));
                                                                saturdayMaxSchedule = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "summerSaturdayMaxSchedule"));
                                                                saturdayMinSchedule = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "summerSaturdayMinSchedule"));
                                                                startRounding = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "SummerStartArrot"));
                                                            }
                                                            else
                                                            {
                                                                elaborateStartDate = winterDate;
                                                                weekMaxSchedule = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "winterWeekMaxSchedule"));
                                                                weekMinSchedule = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "winterWeekMinSchedule"));
                                                                saturdayMaxSchedule = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "winterSaturdayMaxSchedule"));
                                                                saturdayMinSchedule = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "winterSaturdayMinSchedule"));
                                                                startRounding = TimeSpan.Parse(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.Casp, "WinterStartArrot"));

                                                            }

                                                            //if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > new TimeSpan(17, 25, 0)) //17:25
                                                            //{
                                                            //    currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fis_Reg;
                                                            //    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg;
                                                            //
                                                            //    if (currentRegU.Registrazione_Data_Ora_Fis_Reg - currentRegE.Registrazione_Data_Ora_Fis_Reg > TimeSpan.FromHours(6))
                                                            //    {
                                                            //        currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg.Subtract(TimeSpan.FromMinutes(extraTimePause));
                                                            //    }
                                                            //}
                                                            if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < weekMinSchedule //16:30
                                                                && (currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek != DayOfWeek.Saturday
                                                                && currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek != DayOfWeek.Sunday))
                                                            {
                                                                if (currentRegU.Registrazione_Data_Ora_Fis_Reg - currentRegE.Registrazione_Data_Ora_Fis_Reg > TimeSpan.FromHours(6))
                                                                {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg - TimeSpan.FromMinutes(mondayFridayPause);
                                                                }
                                                            }
                                                            else if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < saturdayMinSchedule //15:30
                                                                 && (currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek == DayOfWeek.Saturday
                                                                 || currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek == DayOfWeek.Sunday))
                                                            {
                                                                if (currentRegU.Registrazione_Data_Ora_Fis_Reg - currentRegE.Registrazione_Data_Ora_Fis_Reg > TimeSpan.FromHours(6))
                                                                {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg - TimeSpan.FromMinutes(saturdayPause);
                                                                }
                                                            }
                                                            else if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= weekMinSchedule //16:30
                                                                 && currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= weekMaxSchedule //17:25
                                                                 && (currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek != DayOfWeek.Saturday
                                                                 && currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek != DayOfWeek.Sunday))
                                                            {
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Year,
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Month,
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Day,
                                                                    weekMinSchedule.Hours, weekMinSchedule.Minutes, weekMinSchedule.Seconds);

                                                                //Se abbiamo almeno 4 ore di lavoro allora  tolgo la pausa pranzo
                                                                if (currentRegU.Registrazione_Data_Ora_Fis_Reg - currentRegE.Registrazione_Data_Ora_Fis_Reg > TimeSpan.FromHours(6))
                                                                {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Subtract(TimeSpan.FromMinutes(mondayFridayPause));
                                                                }
                                                            }
                                                            else if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > weekMaxSchedule //17:25
                                                                 && (currentRegU.Registrazione_Data_Ora_Fis_Reg.DayOfWeek != DayOfWeek.Saturday
                                                                 && currentRegU.Registrazione_Data_Ora_Fis_Reg.DayOfWeek != DayOfWeek.Sunday))
                                                            {
                                                                //Aggiugere scaglioni di mezz'ora
                                                                //startRounding = weekMaxSchedule.Add(new TimeSpan(0, 5, 0));
                                                                amount = 30;

                                                                var extraMinutes = (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay - weekMaxSchedule).TotalMinutes;
                                                                var multiplier = Math.Ceiling(extraMinutes / 30);
                                                                //multiplier += 1;
                                                                var totalAmountToAdd = TimeSpan.FromMinutes(amount * multiplier);

                                                                var exitTime = startRounding.Add(totalAmountToAdd);
                                                                var newExit = new DateTime(
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Year,
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Month,
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Day,
                                                                    exitTime.Hours,
                                                                    exitTime.Minutes,
                                                                    0);

                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg = newExit;

                                                                if (currentRegU.Registrazione_Data_Ora_Fis_Reg - currentRegE.Registrazione_Data_Ora_Fis_Reg > TimeSpan.FromHours(6))
                                                                {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Subtract(TimeSpan.FromMinutes(mondayFridayPause + extraTimePause));
                                                                }
                                                            }
                                                            else if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= saturdayMinSchedule //15:30
                                                                 && currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= saturdayMaxSchedule //16:25
                                                                && (currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek == DayOfWeek.Saturday
                                                                 || currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek == DayOfWeek.Sunday))
                                                            {
                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg = new DateTime(
                                                                     currentRegU.Registrazione_Data_Ora_Fis_Reg.Year,
                                                                     currentRegU.Registrazione_Data_Ora_Fis_Reg.Month,
                                                                     currentRegU.Registrazione_Data_Ora_Fis_Reg.Day,
                                                                     saturdayMinSchedule.Hours, saturdayMinSchedule.Minutes, saturdayMinSchedule.Seconds);

                                                                if (currentRegU.Registrazione_Data_Ora_Fis_Reg - currentRegE.Registrazione_Data_Ora_Fis_Reg > TimeSpan.FromHours(6))
                                                                {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Subtract(TimeSpan.FromMinutes(saturdayPause));
                                                                }
                                                            }
                                                            else if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > saturdayMaxSchedule //16:25
                                                                && (currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek == DayOfWeek.Saturday
                                                                 || currentRegU.Registrazione_Data_Ora_Fis_Reg.Date.DayOfWeek == DayOfWeek.Sunday))
                                                            {
                                                                startRounding = saturdayMaxSchedule.Add(new TimeSpan(0, 5, 0));
                                                                amount = 30;

                                                                var extraMinutes = (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay - startRounding).TotalMinutes;
                                                                var multiplier = Math.Ceiling(extraMinutes / 30);
                                                                var totalAmountToAdd = TimeSpan.FromMinutes(amount * multiplier);

                                                                var exitTime = startRounding.Add(totalAmountToAdd);
                                                                var newExit = new DateTime(
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Year,
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Month,
                                                                    currentRegU.Registrazione_Data_Ora_Fis_Reg.Day,
                                                                    exitTime.Hours,
                                                                    exitTime.Minutes,
                                                                    0);

                                                                currentRegU.Registrazione_Data_Ora_Fig_Reg = newExit;

                                                                if (currentRegU.Registrazione_Data_Ora_Fis_Reg - currentRegE.Registrazione_Data_Ora_Fis_Reg > TimeSpan.FromHours(6))
                                                                {
                                                                    currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fig_Reg.Value.Subtract(TimeSpan.FromMinutes(saturdayPause + extraTimePause));
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            currentRegE.Registrazione_Data_Ora_Fig_Reg = currentRegE.Registrazione_Data_Ora_Fis_Reg;
                                                            currentRegU.Registrazione_Data_Ora_Fig_Reg = currentRegU.Registrazione_Data_Ora_Fis_Reg;
                                                        }
                                                    }

                                                }
                                                #endregion
                                            }
                                                #endregion
                                            }
                                            catch (Exception e)
                                            {
                                            }
                                        }
                                        if (currentCol.Metodo_Arrotondamento_Col == 1 || currentCant.Metodo_Arrotondamento_Can == 1) {
                                            roundingEnum = RoundingMethodEnum.Duration;
                                        }
                                                
                                        if (roundingEnum == RoundingMethodEnum.Duration)
                                        {
                                            //DeleteDurationRounding(regVs);
                                            DurationRounding(currentRegVs, roundingEnum);
                                        }

                                    }
                                }
                            }

                            #region 4.Gestione dei ritardi

                            if (utilizzoLimiteEntrata == (int)UtilizzoLimiteEntrata.Ritardo || utilizzoLimiteEntrata == (int)UtilizzoLimiteEntrata.LimiteEntrataERitardo)
                            {
                                bool isfirstAfternoonReg = true,
                                     isFirstMorningReg = true;
                                var regsByDate = colGroup.GroupBy(r => r.Data_Ora_Fig_EDate);
                                //Personalizzazione per gestire i ritardi anche sull'uscita (Pulitait)
                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ExitDelay) == 1)
                                {
                                    foreach (var dateGroup in regsByDate)
                                    {
                                        List<Reg_V> delayRegVs = dateGroup.OrderBy(regV => regV.Data_Ora_Fis_E).ToList();

                                        isfirstAfternoonReg = true;
                                        isFirstMorningReg = true;
                                        Dictionary<EntryLimitTypeEnum, EntryLimitData> entryLimitConfig;
                                        midDay = new TimeSpan(12, 0, 0);

                                        foreach (Reg_V currentRegV in delayRegVs)
                                        {
                                            //Se è presente la registrazione d'uscita vado a prelevarla, il controllo è presente per evitare eccezioni
                                            Reg currentRegE = regsDic[currentRegV.RegE];
                                            Reg currentRegU = null;
                                            if (currentRegV.RegU.HasValue) {
                                                currentRegU = regsDic[currentRegV.RegU.Value];
                                            }

                                            Cant currentCant = RepoManager.CantRepo.Single(c => c.Cant_Id == currentRegE.Cant_Id);
                                            // calcolo dei dati di limite d'entrata per il cantiere sul quale si ha timbrato, metodo su misura per pulitait visto i loro orari particolari
                                            entryLimitConfig = GetEntryLimitConifgMoreCant(currentCant, currentCol, currentRegE.Registrazione_Data_Ora_Fis_Reg.Date, midDay);

                                            // se ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                            if (entryLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegE != null)
                                            {
                                                TimeSpan delayLimit;
                                                TimeSpan? morningDelayLimit,
                                                    afternoonDelayLimit,
                                                    morningDelayExitLimit,
                                                    afternoonDelayExitLimit;
                                                int delayDuration = 0;

                                                //recupera i limiti d'entrata e d'uscita dai parametri...
                                                if (entryLimitConfig[EntryLimitTypeEnum.MorningDealyLimitList].EntryLimitTimeList == null && entryLimitConfig[EntryLimitTypeEnum.AfternoonDealyLimitList].EntryLimitTimeList == null && entryLimitConfig[EntryLimitTypeEnum.MorningDealyLimitListExit].ExitLimitTimeList == null && entryLimitConfig[EntryLimitTypeEnum.Morning].ExitLimitTime == null)
                                                {
                                                    morningDelayLimit = entryLimitConfig[EntryLimitTypeEnum.Morning].IsConfigured ? entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime : null;
                                                    afternoonDelayLimit = entryLimitConfig[EntryLimitTypeEnum.Afternoon].IsConfigured ? entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime : null;
                                                    morningDelayExitLimit = entryLimitConfig[EntryLimitTypeEnum.MorningExit].IsConfigured ? entryLimitConfig[EntryLimitTypeEnum.MorningExit].EntryLimitTime : null;
                                                    afternoonDelayExitLimit = entryLimitConfig[EntryLimitTypeEnum.AfternoonExit].IsConfigured ? entryLimitConfig[EntryLimitTypeEnum.AfternoonExit].EntryLimitTime : null;
                                                }
                                                //...o dagli orari
                                                else
                                                {
                                                    morningDelayLimit = entryLimitConfig[EntryLimitTypeEnum.MorningDealyLimitList].EntryLimitTimeList.FirstOrDefault();
                                                    afternoonDelayLimit = entryLimitConfig[EntryLimitTypeEnum.AfternoonDealyLimitList].EntryLimitTimeList.FirstOrDefault();
                                                    morningDelayExitLimit = entryLimitConfig[EntryLimitTypeEnum.MorningDealyLimitListExit].ExitLimitTimeList.FirstOrDefault();
                                                    afternoonDelayExitLimit = entryLimitConfig[EntryLimitTypeEnum.AfternoonDealyLimitListExit].ExitLimitTimeList.FirstOrDefault();
                                                }

                                                //if (afternoonDelayLimit != null && afternoonDelayLimit != TimeSpan.Zero)
                                                //{
                                                //    midDay = afternoonDelayLimit.Value;
                                                //}

                                                //else
                                                //{
                                                //    afternoonDelayLimit = null;
                                                //}

                                                //A differenza dei ritardi normali non lo calcolo solo sulla prima registrazione ma su tutte
                                                if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < midDay && morningDelayLimit != null)
                                                {
                                                    isFirstMorningReg = false;

                                                    delayLimit = morningDelayLimit.Value.Add(TimeSpan.FromMinutes(delayTollerance));

                                                    if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > delayLimit)
                                                    {
                                                        delayDuration = Convert.ToInt32(Math.Floor(currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - morningDelayLimit.Value.TotalMinutes));
                                                    }
                                                }

                                                //Stessa cosa vale per il pomeriggio
                                                else if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= midDay && afternoonDelayLimit != null)
                                                {
                                                    isfirstAfternoonReg = false;

                                                    delayLimit = afternoonDelayLimit.Value.Add(TimeSpan.FromMinutes(delayTollerance));

                                                    if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > delayLimit)
                                                    {
                                                        delayDuration = Convert.ToInt32(Math.Floor(currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - afternoonDelayLimit.Value.TotalMinutes));
                                                    }
                                                }

                                                if (currentRegU != null) {
                                                    if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < midDay && morningDelayLimit != null)
                                                    {
                                                        isFirstMorningReg = false;

                                                        delayLimit = morningDelayExitLimit.Value.Add(TimeSpan.FromMinutes(delayTollerance));

                                                        if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > delayLimit)
                                                        {
                                                            delayDuration += Convert.ToInt32(Math.Floor(currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - morningDelayExitLimit.Value.TotalMinutes));
                                                        }
                                                    }

                                                    else if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= midDay && afternoonDelayLimit != null)
                                                    {
                                                        isfirstAfternoonReg = false;

                                                        delayLimit = afternoonDelayExitLimit.Value.Add(TimeSpan.FromMinutes(delayTollerance));

                                                        if (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > delayLimit)
                                                        {
                                                            delayDuration += Convert.ToInt32(Math.Floor(currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - afternoonDelayExitLimit.Value.TotalMinutes));
                                                        }
                                                    }
                                                }

                                                currentRegE.Ritardo_Durata = delayDuration;
                                            }
                                        }
                                    }
                                }
                                else {
                                    foreach (var dateGroup in regsByDate)
                                    {
                                        List<Reg_V> delayRegVs = dateGroup.OrderBy(regV => regV.Data_Ora_Fis_E).ToList();

                                        isfirstAfternoonReg = true;
                                        isFirstMorningReg = true;
                                        Dictionary<EntryLimitTypeEnum, EntryLimitData> entryLimitConfig;
                                        midDay = new TimeSpan(12, 0, 0);

                                        foreach (Reg_V currentRegV in delayRegVs)
                                        {
                                            Reg currentRegE = regsDic[currentRegV.RegE];
                                            Cant currentCant = RepoManager.CantRepo.Single(c => c.Cant_Id == currentRegE.Cant_Id);

                                            // calcolo dei dati di limite d'entrata riguardo la registrazione che si sta processando
                                            entryLimitConfig = GetEntryLimitConifg(currentCant, currentCol, currentRegE.Registrazione_Data_Ora_Fis_Reg.Date, midDay);

                                            // se ci sono dei limiti d'entrata configurati ed esiste una registrazione d'entrata
                                            if (entryLimitConfig.Any(kvp => kvp.Value.IsConfigured) && currentRegE != null)
                                            {
                                                TimeSpan delayLimit;
                                                TimeSpan? morningDelayLimit,
                                                    afternoonDelayLimit;
                                                int delayDuration = 0;

                                                //recupera i limiti d'entrata dai parametri...
                                                if (entryLimitConfig[EntryLimitTypeEnum.MorningDealyLimitList].EntryLimitTimeList == null && entryLimitConfig[EntryLimitTypeEnum.AfternoonDealyLimitList].EntryLimitTimeList == null)
                                                {
                                                    morningDelayLimit = entryLimitConfig[EntryLimitTypeEnum.Morning].IsConfigured ? entryLimitConfig[EntryLimitTypeEnum.Morning].EntryLimitTime : null;
                                                    afternoonDelayLimit = entryLimitConfig[EntryLimitTypeEnum.Afternoon].IsConfigured ? entryLimitConfig[EntryLimitTypeEnum.Afternoon].EntryLimitTime : null;
                                                }
                                                //...o dagli orari
                                                else
                                                {
                                                    morningDelayLimit = entryLimitConfig[EntryLimitTypeEnum.MorningDealyLimitList].EntryLimitTimeList.FirstOrDefault();
                                                    afternoonDelayLimit = entryLimitConfig[EntryLimitTypeEnum.AfternoonDealyLimitList].EntryLimitTimeList.FirstOrDefault();
                                                }

                                                if (afternoonDelayLimit != null && afternoonDelayLimit != TimeSpan.Zero)
                                                {
                                                    midDay = afternoonDelayLimit.Value;
                                                }

                                                else
                                                {
                                                    afternoonDelayLimit = null;
                                                }

                                                //Se è la prima registrazione della mattina, ne calcola il ritardo
                                                if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < midDay && morningDelayLimit != null && isFirstMorningReg)
                                                {
                                                    isFirstMorningReg = false;

                                                    delayLimit = morningDelayLimit.Value.Add(TimeSpan.FromMinutes(delayTollerance));

                                                    if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > delayLimit)
                                                    {
                                                        delayDuration = Convert.ToInt32(Math.Floor(currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - morningDelayLimit.Value.TotalMinutes));
                                                    }
                                                }

                                                //Se è la prima registrazione del pomeriggio, ne calcola il ritardo
                                                else if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= midDay && afternoonDelayLimit != null && isfirstAfternoonReg)
                                                {
                                                    isfirstAfternoonReg = false;

                                                    delayLimit = afternoonDelayLimit.Value.Add(TimeSpan.FromMinutes(delayTollerance));

                                                    if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > delayLimit)
                                                    {
                                                        delayDuration = Convert.ToInt32(Math.Floor(currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.TotalMinutes - afternoonDelayLimit.Value.TotalMinutes));
                                                    }
                                                }
                                                currentRegE.Ritardo_Durata = delayDuration;
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion


                        }
                    }
                }
                RepoManager.RegRepo.Context.BulkUpdate(regs);
            }
            return errors;
        }

        /// <summary>
        /// Effettua l'arrotondamento sulle registrazioni specificate.
        /// </summary>
        /// <param name="regVs">L'elenco delle reg_v da arrotondare.</param>
        /// <param name="regs">L'elenco delle registrazioni su cui eventualmente scrivere l'arrotondamento.</param>
        /// <param name="cants">L'elenco dei cantieri collegati alle registrazioni specificate per l'arrotondamento.</param>
        /// <param name="cols">L'elenco dei collaboratori collegati alle registrazioni specifciate per l'arrotondamento.</param>
        /// <param name="elaborateUserId">L'identificativo dell'utente di lancio dell'operazione (utilizzato per la scrittura della tabella messaggi).</param>
        /// <param name="elaborateDateTime">La data e ora dell'operazione (utilizzata per la scrittura della tabella messaggi).</param>
        /// <param name="application">L'applicazione di lancio dell'operazione (utilizzata per la scrittura della tabella messaggi).</param>
        /// <returns>L'elenco degli errori eventualmente riscontrato durante le operazioni di arrotondamento.</returns>
        public List<KeyValuePair<string, string>> CopertureSerali(IEnumerable<Reg_V> regVs, IEnumerable<Reg> regs, List<Cant> cants, List<Col> cols, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application, bool delete)
        {
            var regsDic = new Dictionary<int, Reg>();

            foreach (Reg reg in regs)
            {
                regsDic.Add(reg.Reg_Id, reg);
            }
            List<Reg> toUpdateRegs = new List<Reg>();

            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();

            if (regVs.Count() > 0)
            {
                var groupByColRegs = regVs.GroupBy(reg => reg.Col_Id);

                foreach (var colGroup in groupByColRegs)
                {

                    int colGroupId = colGroup.Key.HasValue ? colGroup.Key.Value : -1;

                    if (colGroupId != -1)
                    {
                        Col currentCol = cols.SingleOrDefault(col => col.Col_Id == colGroupId);

                        if (currentCol != null)
                        {
                            var groupByCantRegs = colGroup.GroupBy(reg => reg.Cant_Id);

                            foreach (var cantGroup in groupByCantRegs)
                            {
                                int cantGroupId = cantGroup.Key.HasValue ? cantGroup.Key.Value : -1;

                                if (cantGroupId != -1)
                                {
                                    Cant currentCant = cants.SingleOrDefault(cant => cant.Cant_Id == cantGroupId);
                                    TimeSpan limite = new TimeSpan();
                                    if (currentCant.Turno1_Can.HasValue) {
                                        limite = currentCant.Turno1_Can.Value;
                                    }

                                    if (currentCant != null && limite != default(TimeSpan))
                                    {
                                        List<Reg_V> currentRegVs = cantGroup.OrderBy(regV => regV.Data_Ora_Fis_E).ToList();

                                        foreach (Reg_V currentRegV in currentRegVs)
                                        {
                                            try
                                            {
                                                Reg currentRegE = RepoManager.RegRepo.Single(r => r.Reg_Id == currentRegV.RegE);
                                                Reg currentRegU = null;
                                                if (currentRegV.RegU.HasValue)
                                                {
                                                    currentRegU = RepoManager.RegRepo.Single(r => r.Reg_Id == currentRegV.RegU.Value);
                                                }

                                                if (currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.TimeOfDay >= limite)
                                                {
                                                    currentRegE.Turno = "Coperture Serali";
                                                    toUpdateRegs.Add(currentRegE);
                                                    if (currentRegU != null)
                                                    {
                                                        currentRegU.Turno = "Coperture Serali";
                                                        toUpdateRegs.Add(currentRegU);
                                                    }
                                                }
                                                else
                                                {
                                                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiXCol) == 1)
                                                    {
                                                        if (currentCant.Telefono_1_Can != null) {
                                                            string[] Coperture = currentCant.Telefono_1_Can.Split(',');
                                                            TimeSpan beforeE = TimeSpan.Parse(Coperture[1]).Subtract(new TimeSpan(0, 30, 0));
                                                            TimeSpan afterE = TimeSpan.Parse(Coperture[1]).Add(new TimeSpan(0, 30, 0));
                                                            TimeSpan beforeU = TimeSpan.Parse(currentCant.Telefono_1_Rif_Can).Subtract(new TimeSpan(0, 30, 0));
                                                            TimeSpan afterU = TimeSpan.Parse(currentCant.Telefono_1_Rif_Can).Add(new TimeSpan(0, 30, 0));
                                                            if (currentRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > beforeE/*&& (currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay > beforeU && currentRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < afterU)*/)
                                                            {
                                                                currentRegE.Turno = "Coperture Serali";
                                                                toUpdateRegs.Add(currentRegE);
                                                                if (currentRegU != null)
                                                                {
                                                                    currentRegU.Turno = "Coperture Serali";
                                                                    toUpdateRegs.Add(currentRegU);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                currentRegE.Turno = "";
                                                                toUpdateRegs.Add(currentRegE);
                                                                if (currentRegU != null)
                                                                {
                                                                    currentRegU.Turno = "";
                                                                    toUpdateRegs.Add(currentRegU);
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        currentRegE.Turno = "";
                                                        toUpdateRegs.Add(currentRegE);
                                                        if (currentRegU != null)
                                                        {
                                                            currentRegU.Turno = "";
                                                            toUpdateRegs.Add(currentRegU);
                                                        }
                                                    }
                                                }
                                            }
                                        catch (KeyNotFoundException ke)
                                        {
                                            _log.Error(String.Format("Chiave non trovata nella gestione coperture serali del collaboratore {0}, Errore: {1}", currentCol.Col_Id, ke.Message));
                                        }
                                        catch (Exception e)
                                        {
                                            _log.Error(String.Format("Errore nell'elaborazione delle coperture serali: {0}", e.Message));
                                        }
                                    }
                                        
                                    }
                                }
                            }
                        }
                    }
                }
                RepoManager.RegRepo.Context.BulkUpdate(toUpdateRegs);
            }
            return errors;
        }

        public List<KeyValuePair<string, string>> AdjustOverlappedRegs(IEnumerable<Reg_V> regVs, IEnumerable<Reg> regs,List<Col> cols, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application, bool delete)
        {
            var regsDic = new Dictionary<int, Reg>();

            foreach (Reg reg in regs)
            {
                regsDic.Add(reg.Reg_Id, reg);
            }

            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();

            if (regVs.Count() > 0)
            {
                var groupByColRegs = regVs.GroupBy(reg => reg.Col_Id);

                foreach (var colGroup in groupByColRegs)
                {
                    int colGroupId = colGroup.Key.HasValue ? colGroup.Key.Value : -1;

                    if (colGroupId != -1)
                    {
                        Col currentCol = cols.SingleOrDefault(col => col.Col_Id == colGroupId);

                        if (currentCol != null)
                        {
                            List<Reg_V> currentRegVs = colGroup.OrderBy(regV => regV.Data_Ora_Fis_E).ToList();

                            Reg lastReg = null;

                            try
                            {
                                foreach (Reg_V currentRegV in currentRegVs)
                                {
                                    Reg currentRegE = regsDic[currentRegV.RegE];
                                    Reg currentRegU = null;
                                    if (currentRegV.RegU.HasValue)
                                    {
                                        currentRegU = regsDic[currentRegV.RegU.Value];
                                    }
                                    if (lastReg != null) {
                                        TimeSpan differenza = currentRegE.Registrazione_Data_Ora_Fis_Reg - lastReg.Registrazione_Data_Ora_Fis_Reg;
                                        if (differenza.Days == 0 && differenza.Hours == 0 && differenza.Minutes == 0 && differenza.Seconds < 5) {
                                            DateTime nuovaOraFig = currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.AddSeconds(5);
                                            currentRegE.Registrazione_Data_Ora_Fig_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Year,
                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Month,
                                                currentRegE.Registrazione_Data_Ora_Fig_Reg.Value.Day,
                                                lastReg.Registrazione_Data_Ora_Fig_Reg.Value.Hour,
                                                lastReg.Registrazione_Data_Ora_Fig_Reg.Value.Minute,
                                                nuovaOraFig.Second);
                                            DateTime nuovaOraFis = currentRegE.Registrazione_Data_Ora_Fis_Reg.AddSeconds(5);
                                            currentRegE.Registrazione_Data_Ora_Fis_Reg = new DateTime(currentRegE.Registrazione_Data_Ora_Fis_Reg.Year,
                                                currentRegE.Registrazione_Data_Ora_Fis_Reg.Month,
                                                currentRegE.Registrazione_Data_Ora_Fis_Reg.Day,
                                                lastReg.Registrazione_Data_Ora_Fis_Reg.Hour,
                                                lastReg.Registrazione_Data_Ora_Fis_Reg.Minute,
                                                nuovaOraFis.Second);

                                        }
                                    }
                                    lastReg = currentRegU;
                                }
                            }
                            catch (KeyNotFoundException ke)
                            {
                                _log.Error(String.Format("Chiave non trovata nella gestione coperture serali del collaboratore {0}, Errore: {1}", currentCol.Col_Id, ke.Message));
                            }
                            catch (Exception e)
                            {
                                _log.Error(String.Format("Errore nell'elaborazione delle coperture serali: {0}", e.Message));
                            }    
                        }
                    }
                }
                RepoManager.RegRepo.Context.BulkUpdate(regs);
            }
            return errors;
        }

        public void delete10mins() {
            var regsDic = new Dictionary<int, Reg>();
            IEnumerable<Reg_V> toDelete = RepoManager.Reg_VRepo.GetAll().Where(regv => regv.Durata_Fig < 10 || regv.Durata_Fis < 10);
            foreach (var regv in toDelete) {
                Reg currentRegE = regsDic[regv.RegE];
                RepoManager.RegRepo.Delete(currentRegE, true);
                Reg currentRegU = regsDic[regv.RegU.Value];
                RepoManager.RegRepo.Delete(currentRegU, true);
            }
        }

        public static bool IsBetween(DateTime item, DateTime start, DateTime end)
        {
            return Comparer<DateTime>.Default.Compare(item, start) >= 0
                && Comparer<DateTime>.Default.Compare(item, end) <= 0;
        }

        public List<KeyValuePair<String, String>> DeletePausaPranzo(IEnumerable<Reg> regs)
        {
            List<Tab_Decod> motivazioni = RepoManager.Tab_DecodRepo.GetAllQueryable(m => m.Decodifica_Tab == "Pausa").ToList();
            // Lista che conteerrà gli errori di elaborazione
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            // Lista che conterrà gli arrotondamenti da aggiungere a db
            List<Reg> roundingsToAdd = new List<Reg>();

            // Recupero il metodo di arrotondamento dalla scheda parametri
            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

            RepoManager.RegRepo.Context.BulkDelete(regs.Where(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && reg.Note_Reg == "Pausa").ToList());

            // Filtra le regv selezionando solo quelle di tipo arrotondamento per durata e cancella direttamente
            RepoManager.RegRepo.Context.BulkDelete(regs.Where(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && reg.Motivazione_Reg_Id == motivazioni.First().Tab_Decod_Id).ToList());

            return errors;
        }

        public List<KeyValuePair<String, String>> DeleteNewPausaPranzo(IEnumerable<Reg> regs)
        {
            // Lista che conteerrà gli errori di elaborazione
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            // Lista che conterrà gli arrotondamenti da aggiungere a db
            List<Reg> roundingsToAdd = new List<Reg>();

            // Recupero il metodo di arrotondamento dalla scheda parametri
            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

            List<Reg> regConPausa =regs.Where(r => r.Registrazione_Tipo_Reg == 0 && r.Rettifica_Durata != null).ToList();

            regConPausa.ForEach(reg => { reg.Rettifica_Durata = 0; reg.Note_Reg = ""; });

            RepoManager.RegRepo.BulkUpdate(regConPausa);

            return errors;
        }

        /// <summary>
        /// Effettua l'arrotondamento sulle registrazioni specificate.
        /// </summary>
        /// <param name="regVs">L'elenco delle reg_v da arrotondare, già filtrate per collaboratore e data.</param>
        /// <returns>L'elenco degli errori eventualmente riscontrato durante le operazioni di arrotondamento.</returns>
        /// 

        //TODO: Gestione degli errori
        public List<KeyValuePair<String, String>> DeleteDurationRounding(IEnumerable<Reg> regs)
        {
            // Lista che conteerrà gli errori di elaborazione
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            // Lista che conterrà gli arrotondamenti da aggiungere a db
            List<Reg> roundingsToAdd = new List<Reg>();

            // Recupero il metodo di arrotondamento dalla scheda parametri
            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

            // Filtra le regv selezionando solo quelle di tipo arrotondamento per durata e cancella direttamente
            RepoManager.RegRepo.Context.BulkDelete(regs.Where(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.ArrotDur).ToList());

            return errors;
        }


        public List<KeyValuePair<String, String>> DurationRounding(IEnumerable<Reg_V> regVs,RoundingMethodEnum metodo)
        {
            // Lista che conteerrà gli errori di elaborazione
            _log.Info(String.Format("Inizio arrotondamento per durata di {0} regs", regVs.Count()));
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            // Lista che conterrà gli arrotondamenti da aggiungere a db
            List<Reg> roundingsToAdd = new List<Reg>();

            // Recupero il metodo di arrotondamento dalla scheda parametri
            RoundingMethodEnum roundingParamEnum = (RoundingMethodEnum)RepoManager.ParamRepo.ParametersRow.Metodo_Arrotondamento;

            if (roundingParamEnum == RoundingMethodEnum.None || roundingParamEnum == RoundingMethodEnum.StartEnd) {
                roundingParamEnum = metodo;
            }

            List<Reg_V> filteredRegVs = new List<Reg_V>();
            List<Tab_Decod> pausa = RepoManager.Tab_DecodRepo.GetAllQueryable(p => p.Decodifica_Tab == "Pausa").ToList();
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ArrotondamentoPausa) == 0)
            {
                // Filtra le regv selezionando solo quelle 'lavorative' (ore e viaggi)
                filteredRegVs = regVs.Where(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();
            }
            else {
                // Filtra le regv selezionando solo quelle 'lavorative' (ore e viaggi) senza contare le pause
                filteredRegVs = regVs.Where(reg => (reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip) && reg.Motivazione_Reg_Id != pausa.First().Tab_Decod_Id).ToList();
            }
            // Controllo che mi siano state passate delle regv e che nei parametri sia attivato l'arrotondamento per durata
            if (filteredRegVs.Count() > 0 && roundingParamEnum == RoundingMethodEnum.Duration)
            {
                // Raggruppa le registrazioni per collaboratore
                var regsByCol = filteredRegVs.GroupBy(reg => reg.Col_Id).ToList();

                //Recupero i Parametri di Arrotondamento Generali da Scheda parametri
                int paramMinutesDuration = RepoManager.ParamRepo.ParametersRow.Default_Minuti_Durata.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Minuti_Durata.Value : -1;
                int paramThresholdDuration = RepoManager.ParamRepo.ParametersRow.Default_Soglia_Durata.HasValue ? RepoManager.ParamRepo.ParametersRow.Default_Soglia_Durata.Value : -1;
                int paramFromHourThresholdDuration = RepoManager.ParamRepo.ParametersRow.Soglia_Minima_Arrotondamento_Durata.HasValue ? RepoManager.ParamRepo.ParametersRow.Soglia_Minima_Arrotondamento_Durata.Value : 60;

                double totalCol = regsByCol.Count();
                double percCol = 0;
                double countCol = 1;

                foreach (var colGroup in regsByCol)
                {
                    percCol = (countCol / totalCol) * 100;
                    //emetto messaggio di Elaborazione del COL "N"

                    countCol++;

                    var currColId = colGroup.Key.HasValue ? colGroup.Key : -1;

                    if (currColId != -1)
                    {
                        Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == currColId);

                        // Recupera i parametri dal collaboratore. Se il collaboratore non ha parametri impostati, li prende dalla scheda parametri
                        int thresholdDuration = currentCol.Soglia_Durata_Col.HasValue ? currentCol.Soglia_Durata_Col.Value : paramThresholdDuration;
                        int minutesDuration = currentCol.Arrot_Durata_Col.HasValue ? currentCol.Arrot_Durata_Col.Value : paramMinutesDuration;
                        int fromHourThresholdDuration = currentCol.Soglia_Minima_Arrotondamento_Durata_Col.HasValue ? currentCol.Soglia_Minima_Arrotondamento_Durata_Col.Value : paramFromHourThresholdDuration;

                        if (currentCol != default(Col))
                        {
                            // Aggiungo un controllo per komplett se sono manutentori
                            if (currentCol.Qualifica_Col != "0") {
                                // Raggruppa le registrazioni per data (giorno)
                                var regsByColDate = colGroup.GroupBy(reg => reg.Data_Reg).ToList();

                                foreach (var colDateGroup in regsByColDate)
                                {
                                    // Accumulatore delle durate per il giorno corrente
                                    int workDayDuration = 0;
                                    foreach (Reg_V regv in colDateGroup)
                                    {
                                        if (regv.Durata_Fis != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ArrotAllRegs) == 0)
                                        {
                                            // Accumula la durata delle registrazioni della giornata
                                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.UseEUDurationRounding) == 0)
                                            {
                                                workDayDuration += regv.Durata_Fis.Value;
                                            }
                                            else
                                            {
                                                workDayDuration += regv.Durata_Fig.Value;
                                            }
                                        }
                                        else if (regv.Durata_Fis != null && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ArrotAllRegs) == 1)
                                        {
                                            List<Cant> cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regv.Cant_Id).ToList();
                                            int tmpThresholDuration = thresholdDuration;
                                            int tmpMinutesDuration = minutesDuration;
                                            int tmpFromHourThresholdDuration = fromHourThresholdDuration;
                                            if (currentCol.RoundingMethodEnum == RoundingMethodEnum.Duration)
                                            {
                                                if (!currentCol.Soglia_Durata_Col.HasValue)
                                                {
                                                    thresholdDuration = cantiere.First().Soglia_Durata_Can.HasValue ? cantiere.First().Soglia_Durata_Can.Value : thresholdDuration;
                                                }
                                                if (!currentCol.Arrot_Durata_Col.HasValue)
                                                {
                                                    minutesDuration = cantiere.First().Arrot_Durata_Can.HasValue ? cantiere.First().Arrot_Durata_Can.Value : minutesDuration;
                                                }
                                                if (!currentCol.Soglia_Minima_Arrotondamento_Durata_Col.HasValue)
                                                {
                                                    if (fromHourThresholdDuration == 60 && (cantiere.First().Soglia_Durata_Can.HasValue || cantiere.First().Arrot_Durata_Can.HasValue))
                                                    {
                                                        fromHourThresholdDuration = 0;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                thresholdDuration = cantiere.First().Soglia_Durata_Can.HasValue ? cantiere.First().Soglia_Durata_Can.Value : thresholdDuration;
                                                minutesDuration = cantiere.First().Arrot_Durata_Can.HasValue ? cantiere.First().Arrot_Durata_Can.Value : minutesDuration;
                                                if (fromHourThresholdDuration == 60 && (cantiere.First().Soglia_Durata_Can.HasValue || cantiere.First().Arrot_Durata_Can.HasValue))
                                                {
                                                    fromHourThresholdDuration = 0;
                                                }
                                            }
                                            // Ore e minuti lavorati nella giornata corrente
                                            int hoursWorked = 0; 
                                            int minutesWorked = 0; 

                                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.UseEUDurationRounding) == 0)
                                            {
                                                hoursWorked = regv.Durata_Fis.Value / 60;
                                                minutesWorked = regv.Durata_Fis.Value % 60;
                                            }
                                            else
                                            {
                                                hoursWorked = regv.Durata_Fig.Value / 60;
                                                minutesWorked = regv.Durata_Fig.Value % 60;
                                            }

                                            if (minutesDuration == 0)
                                            {
                                                // Se il parametro è a 0, lo porto a 60 per poter fare i calcoli
                                                minutesDuration = 60;
                                            }

                                            if (minutesWorked < fromHourThresholdDuration) //Inserire parametro
                                                continue;

                                            int moduleMinutes = minutesWorked % minutesDuration;
                                            //Se ci sono minuti in esubero rispetto al parametro, genero la regv di arrotondamento
                                            if (moduleMinutes != 0)
                                            {
                                                //Se sono sopra alla soglia, genero una regv di arrotondamento positiva
                                                if (moduleMinutes > thresholdDuration)
                                                {
                                                    // La reg di arrotondamento avrà durata tale da portare la durata totale di giornata al parametro superiore specificato
                                                    TimeSpan roundingTime = new TimeSpan(0, minutesDuration - moduleMinutes, 0);
                                                    roundingsToAdd.Add(RepoManager.RegRepo.GenerateRoundingRegCan(currColId.GetValueOrDefault(), regv.Cant_Id.Value, colDateGroup.Key.Value, RoundingTypeEnum.RoundingPlus, roundingTime));
                                                }
                                                //Se sono sotto alla soglia, genero una regv di arrotondamento negativa
                                                else
                                                {
                                                    // La reg di arrotondamento avrà durata tale da portare la durata totale di giornata al parametro inferiore specificato
                                                    TimeSpan roundingTime = new TimeSpan(0, moduleMinutes, 0);
                                                    roundingsToAdd.Add(RepoManager.RegRepo.GenerateRoundingRegCan(currColId.GetValueOrDefault(), regv.Cant_Id.Value, colDateGroup.Key.Value, RoundingTypeEnum.RoundingMinus, roundingTime));
                                                }
                                            }
                                            thresholdDuration = tmpThresholDuration;
                                            minutesDuration = tmpMinutesDuration;
                                            fromHourThresholdDuration = tmpFromHourThresholdDuration;
                                        }
                                    }

                                    if (workDayDuration > 0 && RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ArrotAllRegs) == 0)
                                    {
                                        //List<Cant> cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == ).ToList();
                                        // Ore e minuti lavorati nella giornata corrente
                                        int hoursWorked = workDayDuration / 60;
                                        int minutesWorked = workDayDuration % 60;

                                        if (minutesDuration == 0)
                                        {
                                            // Se il parametro è a 0, lo porto a 60 per poter fare i calcoli
                                            minutesDuration = 60;
                                        }

                                        if (minutesWorked <= fromHourThresholdDuration) //Inserire parametro
                                            continue;

                                        int moduleMinutes = minutesWorked % minutesDuration;

                                        //Se ci sono minuti in esubero rispetto al parametro, genero la regv di arrotondamento
                                        if (moduleMinutes != 0)
                                        {
                                            //Se sono sopra alla soglia, genero una regv di arrotondamento positiva
                                            if (moduleMinutes > thresholdDuration)
                                            {
                                                // La reg di arrotondamento avrà durata tale da portare la durata totale di giornata al parametro superiore specificato
                                                TimeSpan roundingTime = new TimeSpan(0, minutesDuration - moduleMinutes, 0);
                                                roundingsToAdd.Add(RepoManager.RegRepo.GenerateRoundingReg(currColId.GetValueOrDefault(), colDateGroup.Key.Value, RoundingTypeEnum.RoundingPlus, roundingTime));
                                            }
                                            //Se sono sotto alla soglia, genero una regv di arrotondamento negativa
                                            else
                                            {
                                                // La reg di arrotondamento avrà durata tale da portare la durata totale di giornata al parametro inferiore specificato
                                                TimeSpan roundingTime = new TimeSpan(0, moduleMinutes, 0);
                                                roundingsToAdd.Add(RepoManager.RegRepo.GenerateRoundingReg(currColId.GetValueOrDefault(), colDateGroup.Key.Value, RoundingTypeEnum.RoundingMinus, roundingTime));
                                            }
                                        }
                                    }
                                }
                            }
                            
                        }
                    }
                }
                // se al termine del ciclo sono state generate delle rettifiche allora si procede alla loro scrittura nel database
                if (roundingsToAdd.Any())
                {
                    // salvataggio nel database delle rettifiche
                    RepoManager.RegRepo.Add(roundingsToAdd, true);
                }

                //BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione terminata");
                //BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione terminata");
            }
            _log.Info(String.Format("Arrotondamento per durata di {0} regs terminato", regVs.Count()));
            return errors;
        }

        public List<KeyValuePair<String, String>> PausaPranzo(IEnumerable<Reg_V> regVs)
        {
            // Lista che conterrà gli errori di elaborazione
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            // Lista che conterrà le timbrature da aggiungere a db
            List<Reg> regsToAdd = new List<Reg>();

            List<Reg_V> filteredRegVs = new List<Reg_V>();
            List<Tab_Decod> pausa = RepoManager.Tab_DecodRepo.GetAllQueryable(p => p.Decodifica_Tab == "Pausa").ToList();
            // Filtra le regv selezionando solo quelle 'lavorative' (ore e viaggi)
            filteredRegVs = regVs.Where(reg => (reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)).ToList();
            // Controllo che mi siano state passate delle regv e che nei parametri sia attivato l'arrotondamento per durata
            if (filteredRegVs.Count() > 0)
            {
                // Raggruppa le registrazioni per collaboratore
                var regsByCol = filteredRegVs.GroupBy(reg => reg.Col_Id).ToList();

                double totalCol = regsByCol.Count();

                foreach (var colGroup in regsByCol)
                {
                    var currColId = colGroup.Key.HasValue ? colGroup.Key : -1;

                    if (currColId != -1)
                    {
                        Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == currColId);

                        if (currentCol != default(Col))
                        {
                            // Raggruppa le registrazioni per data (giorno)
                            var regsByColDate = colGroup.GroupBy(reg => reg.Data_Reg).ToList();

                            foreach (var colDateGroup in regsByColDate)
                            {
                                //inizio spunti per nuovo metodo pausa
                                //bool substract = true; variabile per verificare se si ha già arrotondato la giornata
                                //se va arrotondata sarà da vedere che problemi da modificare timbrature già esistenti e non aggiungerne di nuove con durata negativa
                                int tmpcantId = 0;
                                double arrot = 100;
                                int durata = 0;
                                int currentDurata = 0;
                                string tmpTurno = "";
                                string currentTurno = "";
                                DateTime tmpDate = new DateTime(1999,12,31);
                                foreach (var regvs in colDateGroup.GroupBy(r => r.Cant_Id))
                                {
                                    int tmpDurata = 0;
                                    foreach (Reg_V regv in regvs)
                                    {
                                        if (regv.Durata_Fig != null && regv.Cant_Id != null)
                                        {
                                            durata += regv.Durata_Fig.Value;
                                            tmpDurata += regv.Durata_Fig.Value;
                                        }
                                    }
                                    if (tmpDurata > currentDurata)
                                    {
                                        currentDurata = tmpDurata;
                                        List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regvs.Key).ToList();
                                        if (cantieri.First().Importo1 != null)
                                        {
                                            //if (cantieri.First().Importo1 < arrot)
                                            //{
                                                arrot = cantieri.First().Importo1.Value;
                                                tmpcantId = cantieri.First().Cant_Id;
                                                if (tmpTurno == "")
                                                {
                                                    tmpTurno = currentTurno;
                                                }
                                            //}
                                        }
                                    }
                                }
                                if (tmpcantId != 0) 
                                {
                                    Cant cantiere = RepoManager.CantRepo.Single(c => c.Cant_Id == tmpcantId);
                                    if (tmpTurno == "" && durata >= cantiere.Importo10.Value)
                                    {
                                        // creo la registrazione con durata negativa in base al parametro presente nel cantiere
                                        TimeSpan roundingTime = new TimeSpan(0, 0, 0);
                                        Reg tmp = RepoManager.RegRepo.GeneratePausaPranzo(currColId.GetValueOrDefault(), tmpcantId, colDateGroup.Key.Value, RoundingTypeEnum.RoundingMinus, roundingTime, tmpTurno);
                                        if (tmp.Col_Id != null)
                                        {
                                            regsToAdd.Add(tmp);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // se al termine del ciclo sono state generate delle rettifiche allora si procede alla loro scrittura nel database
                if (regsToAdd.Any())
                {
                    // salvataggio nel database delle rettifiche
                    RepoManager.RegRepo.Add(regsToAdd, true);
                }

                //BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione terminata");
                //BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione terminata");
            }
            return errors;
        }

        public List<KeyValuePair<String, String>> NewPausaPranzo(IEnumerable<Reg_V> regVs)
        {
            // Lista che conterrà gli errori di elaborazione
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            // Lista che conterrà le timbrature da aggiornare a db
            List<Reg> regsToUpdate = new List<Reg>();

            List<Reg_V> filteredRegVs = new List<Reg_V>();
            List<Tab_Decod> pausa = RepoManager.Tab_DecodRepo.GetAllQueryable(p => p.Decodifica_Tab == "Pausa").ToList();
            // Filtra le regv selezionando solo quelle 'lavorative' (ore e viaggi)
            filteredRegVs = regVs.Where(reg => (reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None)).ToList();
            // Controllo che mi siano state passate delle regv e che nei parametri sia attivato l'arrotondamento per durata
            if (filteredRegVs.Count() > 0)
            {
                // Raggruppa le registrazioni per collaboratore
                var regsByCol = filteredRegVs.GroupBy(reg => reg.Col_Id).ToList();

                double totalCol = regsByCol.Count();

                foreach (var colGroup in regsByCol)
                {
                    var currColId = colGroup.Key.HasValue ? colGroup.Key : -1;

                    if (currColId != -1)
                    {
                        Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == currColId);

                        if (currentCol != default(Col))
                        {
                            // Raggruppa le registrazioni per data (giorno)
                            var regsByColDate = colGroup.GroupBy(reg => reg.Data_Reg).ToList();

                            foreach (var colDateGroup in regsByColDate)
                            {
                                if (colDateGroup.Count() == 1) 
                                {
                                    foreach(Reg_V reg in colDateGroup)
                                    {
                                        Reg attreg = RepoManager.RegRepo.SingleOrDefault(r => r.RiferimentoRRN_Att == reg.RegE);
                                        Cant att = default;
                                        if (attreg != default)
                                        {
                                            att = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == attreg.Cant_Id && c.Tipologia_Can == "ATT");
                                        }
                                        Cant cantiere = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == reg.Cant_Id);
                                        if (cantiere.Importo1 != null && cantiere.Importo10 != null)
                                        {
                                            if (reg.Durata_Fig.Value > cantiere.Importo10.Value)
                                            {
                                                Reg regE = RepoManager.RegRepo.Single(r => r.Reg_Id == reg.RegE);
                                                regE.Rettifica_Durata = (int)cantiere.Importo1.Value;
                                                regE.Note_Reg = "Pausa Di " + (int)cantiere.Importo1.Value + " minuti";
                                                regsToUpdate.Add(regE);
                                            }
                                        } 
                                        else if (cantiere.Importo1 != null)
                                        {
                                            if (att != default) 
                                            {
                                                if (att.Descrizione_Can == "Pausa") 
                                                {
                                                    Reg regE = RepoManager.RegRepo.Single(r => r.Reg_Id == reg.RegE);
                                                    regE.Rettifica_Durata = (int)cantiere.Importo1.Value;
                                                    regE.Note_Reg = "Pausa Di " + (int)cantiere.Importo1.Value + " minuti";
                                                    regsToUpdate.Add(regE);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // se al termine del ciclo sono state generate delle rettifiche allora si procede alla loro scrittura nel database
                if (regsToUpdate.Any())
                {
                    // salvataggio nel database delle rettifiche
                    RepoManager.RegRepo.Update(regsToUpdate, true);
                }

                //BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione terminata");
                //BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione terminata");
            }
            return errors;
        }

        public List<KeyValuePair<String, String>> PausaPranzoKomplett(IEnumerable<Reg_V> regVs)
        {
            // Lista che conterrà gli errori di elaborazione
            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            // Lista che conterrà le timbrature da aggiungere a db
            List<Reg> regsToAdd = new List<Reg>();

            List<Reg_V> filteredRegVs = new List<Reg_V>();
            List<Tab_Decod> pausa = RepoManager.Tab_DecodRepo.GetAllQueryable(p => p.Decodifica_Tab == "Pausa").ToList();
            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ArrotondamentoPausa) == 0)
            {
                // Filtra le regv selezionando solo quelle 'lavorative' (ore e viaggi)
                filteredRegVs = regVs.Where(reg => (reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip) && (reg.Codice_Commessa_Can == "Hotel" || reg.Cant_Id == 42219)).ToList();
            }
            else
            {
                // Filtra le regv selezionando solo quelle 'lavorative' (ore e viaggi) senza contare le pause
                filteredRegVs = regVs.Where(reg => (reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip) && reg.Motivazione_Reg_Id != pausa.First().Tab_Decod_Id).ToList();
            }
            // Controllo che mi siano state passate delle regv e che nei parametri sia attivato l'arrotondamento per durata
            if (filteredRegVs.Count() > 0)
            {
                // Raggruppa le registrazioni per collaboratore
                var regsByCol = filteredRegVs.GroupBy(reg => reg.Col_Id).ToList();

                double totalCol = regsByCol.Count();

                foreach (var colGroup in regsByCol)
                {
                    var currColId = colGroup.Key.HasValue ? colGroup.Key : -1;

                    if (currColId != -1)
                    {
                        Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == currColId);

                        if (currentCol != default(Col) && (currentCol.Raggruppamento1_Col != "1" && currentCol.Raggruppamento2_Col != "0"))
                        {
                            // Raggruppa le registrazioni per data (giorno)
                            var regsByColDate = colGroup.GroupBy(reg => reg.Data_Reg).ToList();

                            foreach (var colDateGroup in regsByColDate)
                            {
                                int tmpcantId = 0;
                                double arrot = 100;
                                int durata = 0;
                                bool valida = true;
                                DateTime inizio = new DateTime(1999, 12, 31);
                                int currentDurata = 0;
                                string tmpTurno = "";
                                string currentTurno = "";
                                DateTime tmpDate = new DateTime(1999, 12, 31);
                                foreach (var regvs in colDateGroup.GroupBy(r => r.Cant_Id))
                                {
                                    int tmpDurata = 0;
                                    foreach (Reg_V regv in regvs.OrderBy(r => r.Data_Ora_Fig_E))
                                    {
                                        if (regv.Durata_Fig != null && regv.Cant_Id != null)
                                        {
                                            if (regv.Cant_Id == 42192)
                                            {
                                                if (regv.Durata_Fig >= 240)
                                                {
                                                    // creo la registrazione con durata negativa in base al parametro presente nel cantiere
                                                    TimeSpan roundingTime = new TimeSpan(0, 0, 0);
                                                    Reg tmp = RepoManager.RegRepo.GeneratePausaPranzo(currColId.Value, regv.Cant_Id.Value, colDateGroup.Key.Value, RoundingTypeEnum.RoundingMinus, roundingTime, regv.Turno);
                                                    if (tmp.Col_Id != null)
                                                    {
                                                        regsToAdd.Add(tmp);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                List<Cant> cantiere = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regv.Cant_Id.Value).ToList();
                                                if (cantiere.First().Turno2_Can != null)
                                                {
                                                    if (cantiere.First().Turno2_Can.Value > regv.Data_Ora_Fig_ETime.Value)
                                                    {
                                                        durata += regv.Durata_Fig.Value;
                                                        tmpDurata += regv.Durata_Fig.Value;
                                                        if (currentTurno == "" && regv.Turno == "Coperture Serali")
                                                        {
                                                            currentTurno = "Coperture Serali";
                                                        }
                                                        if (inizio.Equals(new DateTime(1999, 12, 31)))
                                                        {
                                                            inizio = regv.Data_Ora_Fig_E.Value;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (currentTurno == "" && regv.Turno == "Coperture Serali")
                                                    {
                                                        if (durata < regv.Durata_Fig.Value)
                                                        {
                                                            currentTurno = "Coperture Serali";
                                                        }
                                                    }
                                                    if (inizio.Equals(new DateTime(1999, 12, 31)))
                                                    {
                                                        inizio = regv.Data_Ora_Fig_E.Value;
                                                    }
                                                    durata += regv.Durata_Fig.Value;
                                                    tmpDurata += regv.Durata_Fig.Value;
                                                }
                                                //List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regv.Cant_Id).ToList();
                                                //if (cantieri.First().Importo1 != null)
                                                //{
                                                //    if (cantieri.First().Importo1 < arrot)
                                                //    {
                                                //        arrot = cantieri.First().Importo1.Value;
                                                //        tmpcantId = regv.Cant_Id.Value;
                                                //        if (tmpTurno == "")
                                                //        {
                                                //            tmpTurno = regv.Turno;
                                                //        }
                                                //    }
                                                //}
                                            }
                                        }
                                    }
                                    if (tmpDurata > currentDurata)
                                    {
                                        currentDurata = tmpDurata;
                                        List<Cant> cantieri = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == regvs.Key).ToList();
                                        if (cantieri.First().Importo1 != null || currentCol.Retribuzione_Oraria_Col != null)
                                        {
                                            //if (cantieri.First().Importo1 < arrot)
                                            //{
                                            //arrot = cantieri.First().Importo1.Value;
                                            tmpcantId = cantieri.First().Cant_Id;
                                            if (tmpTurno == "")
                                            {
                                                tmpTurno = currentTurno;
                                            }
                                            //}
                                        }
                                    }
                                }
                                if (durata >= 240)
                                {
                                    if (tmpTurno == "" || tmpcantId != 42192)
                                    {
                                        // creo la registrazione con durata negativa in base al parametro presente nel cantiere
                                        TimeSpan roundingTime = new TimeSpan(0, 0, 0);
                                        Reg tmp = RepoManager.RegRepo.GeneratePausaPranzo(currColId.Value, tmpcantId, colDateGroup.Key.Value, RoundingTypeEnum.RoundingMinus, roundingTime, tmpTurno);
                                        if (tmp.Col_Id != null)
                                        {
                                            regsToAdd.Add(tmp);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // se al termine del ciclo sono state generate delle rettifiche allora si procede alla loro scrittura nel database
                if (regsToAdd.Any())
                {
                    // salvataggio nel database delle rettifiche
                    RepoManager.RegRepo.Add(regsToAdd, true);
                }

                //BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione terminata");
                //BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(100, "Elaborazione terminata");
            }
            return errors;
        }


        /// <summary>
        /// Verifica e gestisce l'eventuale sovrapposizione delle registrazioni specificate.
        /// </summary>
        /// <param name="regVs">Le registrazion si cui effettuare il controllo di sovrapposizione.</param>
        /// <param name="isOnLine">se impostato a <c>true</c> indica che l'operazione viene effettuata online (da interfaccia grafica); altrimenti va impostato a <c>false</c>.</param>
        /// <returns></returns>
        public List<KeyValuePair<String, String>> CheckOverlaps(IEnumerable<Reg_V> regVs, bool isOnLine = false)
        {
            // inizializazione della stub utilizzata per recuperare il nome delle proprietà
            Reg_V regVStub = Init();

            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            //Elimino le eventuali REGV NON ABBINATE (senza Ora di uscita)
            // se sono online, cioè a video controllo la presenza della data e ora di uscita (nella fase di check sui valori non ancora salvati la reg_v non ha ancora un id)
            // se invece sono batch (cioè provengo dall'elaborate) allora controllo la presenza della regu (il riaccoppiamento è già stato effettuato)
            if (isOnLine)
                regVs = regVs.Where(reg => reg.Data_Ora_Fis_U != null).ToList();
            else
                regVs = regVs.Where(reg => reg.RegU != null).ToList();

            var groupByColRegs = regVs.GroupBy(regV => regV.Col_Id);

            List<Reg_V> isOverlappingRegVs = new List<Reg_V>();
            List<Reg_V> isNotOverlappingRegVs = new List<Reg_V>();

            foreach (var colGroup in groupByColRegs)
            {
                List<Reg_V> currentRegVs = colGroup.OrderBy(reg => reg.Data_Ora_Fis_E).ToList();

                DateTime lastFisU = DateTime.MinValue;

                foreach (Reg_V currentRegV in currentRegVs)
                {
                    //if (currentRegV.Data_Ora_Fis_E.Hour >= lastFisU.Hour && currentRegV.Data_Ora_Fis_E.Minute >= lastFisU.Minute)
                    var currentDataOraE = new DateTime(currentRegV.Data_Ora_Fis_E.Year, currentRegV.Data_Ora_Fis_E.Month, currentRegV.Data_Ora_Fis_E.Day,
                        currentRegV.Data_Ora_Fis_E.Hour, currentRegV.Data_Ora_Fis_E.Minute, currentRegV.Data_Ora_Fis_E.Second);
                    if (currentDataOraE >= lastFisU)
                    {
                        isNotOverlappingRegVs.Add(currentRegV);
                        lastFisU = currentRegV.Data_Ora_Fis_U != null ? currentRegV.Data_Ora_Fis_U.Value : lastFisU;
                    }
                    else
                        isOverlappingRegVs.Add(currentRegV);
                }
            }

            var regIds = isOverlappingRegVs.Select(regv => regv.RegE).ToList();
            // vengono recuperati gli id della regu solo se non null (in quanto nella versione con isOnline = true può capitare, non essendo ancora scritte sul database)
            regIds.AddRange(isOverlappingRegVs.Where(regv => regv.RegU != null).Select(regv => regv.RegU.Value));

            // in questo ciclo si trattano solamente le reg_v non nuove
            IEnumerable<Reg> isOverlappingRegs = RepoManager.RegRepo.Find(reg => regIds.Where(regId => regId != 0).ToList().Contains(reg.Reg_Id), true).ToList();
            foreach (var isOverlappingReg in isOverlappingRegs)
            {
                // viene impostato il nuovo stato della registrazione solamente se non si è online, visto che un'eventuale modifica della
                // reg_v online prima del salvataggio sul db genera un errore al primo savechanges del contesto
                if (!isOnLine)
                    isOverlappingReg.Registrazione_Stato_RegEnum |= RegStateEnum.Overlap;

                // se sono online allora devo passare un nome di colonna valida altrimenti l'errore non viene preso in considerazione;
                // in conseguenza, in online, per la chiave viene impostato il campo Data_Reg
                if (isOnLine)
                    errors.Add(new KeyValuePair<string, string>(CommonService.GetPropertyName(() => regVStub.Data_Reg),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_OVERLAP_DELLA_REG) + isOverlappingReg.Reg_Id));
                else
                    errors.Add(new KeyValuePair<string, string>(FunctionMessageEnum.CheckOverlaps.ToString(),
                        BusinessService.GetLocalizedString(PowerWebResources.ERR_OVERLAP_DELLA_REG) + isOverlappingReg.Reg_Id));
            }

            // se si sta processando online e sono presenti delle reg nuove
            if (isOnLine && regIds.Contains(0))
            {
                // si aggiunge un errore specifico per la reg nuova
                errors.Add(new KeyValuePair<string, string>(CommonService.GetPropertyName(() => regVStub.Data_Reg),
                    BusinessService.GetLocalizedString(PowerWebResources.ERR_OVERLAP_DELLA_REG) + "[NUOVA]"));
            }
            return errors;
        }

        /// <summary>
        /// Calcola e restituisce con i dati specificati le cofigurazioni specifiche del limite d'entrata.
        /// </summary>
        /// <param name="cant">Il cantiere con cui calcolare la specifica configurazione.</param>
        /// <param name="col">Il collaboratore con cui calcolare la specifica configurazione.</param>
        /// <param name="date">La data di cui processare il limite d'entrata.</param>
        /// <param name="midDay">The mid day.</param>
        /// <returns>
        /// Un dizionario con chiave il tipo di limite d'entrata e valore i dati relativi.
        /// </returns>
        private Dictionary<EntryLimitTypeEnum, EntryLimitData> GetEntryLimitConifgMoreCant(Cant cant, Col col, DateTime date, TimeSpan midDay)
        {
            // inizializzazione del dizionario che conterrà le confgiurazioni da ritornare
            var returnDic = new Dictionary<EntryLimitTypeEnum, EntryLimitData>();

            // si calcolano i parametri del limite d'entrata recuperando i dati dai 3 elementi che li contengono e privilegiando la
            // gerarchia collaboratore, cantiere, parametri se non richiesto di utilizzare l'eventuale orario collegato al collaboratore;
            // in definitiva il limite d'entrata mattutino e pomeridiano è dato:
            // - in caso sia richiesto il recupero da orario e il collaboratore abbia un orario collegato, con definizione di entrata:
            //      - il limite d'entrata mattutino è dato dalla prima entrata pre metà giornata
            //      - il limite d'entrata pomeridiano è dato dalla prima entrata post metà giornata
            // - in caso non sia configurato il calcolo del limite d'entrata con l'orario si procede alla lettura dei
            //   parametri utilizzando la gerarchia:
            //      - collaboratore
            //      - cantiere
            //      - parametri

            // inizializzazione dei valori che conterranno i dati da restituire nel dizionario
            TimeSpan? morningEntryLimit = null;
            List<TimeSpan> morningEntryLimitList = null;
            List<TimeSpan> afternoonEntryLimitList = null;
            TimeSpan? afternoonEntryLimit = null;
            TimeSpan? afternoonEntryLimitTollerance = GetEntryLimitTolleranceValue(col, cant);
            TimeSpan? morningExitLimit = null;
            List<TimeSpan> morningExitLimitList = null;
            List<TimeSpan> afternoonExitLimitList = null;
            TimeSpan? afternoonExitLimit = null;
            TimeSpan? afternoonExitLimitTollerance = GetEntryLimitTolleranceValue(col, cant);

            #region LIMITE DI ENTRATA DA ORARIO

            // se è configurato l'utilizzo dell'orario per il calcolo del limite d'entrata
            if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Usa_Orario && col.Tab_Orari_Tipo_Id.HasValue)
            {
                // calcolo del piano di dettaglio per il giorno/collaboratore
                List<Tuple<int, TimeSpan, TimeSpan>> dayColPlanDetail = RepoManager.Tab_OrariRepo.GetDayPlanDetailCant(date, col.Col_Id,cant.Cant_Id);

                // se sono presenti dei piani con entrata e uscita per piano collaboratore
                if (dayColPlanDetail.Any())
                {
                    // selezione delle ore d'entrata e loro ordinamento
                    var sortedEntryTimes = dayColPlanDetail.Select(dayDetail => dayDetail.Item2).OrderBy(entryTime => entryTime).ToList();

                    // il limite d'entrata mattutino, se presente, è il primo valore nella prima metà della giornata
                    morningEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime.TotalMinutes < midDay.TotalMinutes);


                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata mattutini
                        morningEntryLimitList = sortedEntryTimes.ToList();

                    var sortedEntryTimesAfternoon = dayColPlanDetail.Select(dayDetail => dayDetail.Item2).OrderByDescending(entryTime => entryTime).ToList();

                    // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                    afternoonEntryLimit = sortedEntryTimesAfternoon.FirstOrDefault(entryTime => entryTime.TotalMinutes >= midDay.TotalMinutes);

                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata pomeridiani
                        afternoonEntryLimitList = sortedEntryTimesAfternoon.ToList();

                    // selezione delle ore d'uscita e loro ordinamento 
                    var sortedExitTime = dayColPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(entryTime => entryTime).ToList();

                    // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                    morningExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime.TotalMinutes < midDay.TotalMinutes);


                    //nel caso in cui vi siano più orari
                    if (sortedExitTime.Count >= 1)
                        //vengono estratti tutti i limiti d'uscita mattutini
                        morningExitLimitList = sortedExitTime.ToList();

                    var sortedExitTimeAfternoon = dayColPlanDetail.Select(dayDetail => dayDetail.Item3).OrderByDescending(entryTime => entryTime).ToList();

                    // il limite d'uscita pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                    afternoonExitLimit = sortedExitTimeAfternoon.LastOrDefault(entryTime => entryTime.TotalMinutes >= midDay.TotalMinutes);

                    //nel caso in cui vi siano più orari
                    if (sortedExitTime.Count >= 1)
                        //vengono estratti tutti i limiti d'uscita pomeridiani
                        afternoonExitLimitList = sortedExitTimeAfternoon.ToList();

                }
                else {
                    // se il collaboratore in questione non possiede un orario vado ad applicare i parametri del collaboratore/cantiere
                    // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                    if (col != null)
                    {
                        // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                        if (col.Limite_Entrata_Mattina_Col.HasValue)
                            morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                        // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                        if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                        {
                            afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                        }
                    }

                    // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                    if (cant != null)
                    {
                        // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                        if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                            morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                        // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                        if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                        {
                            afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                        }
                    }

                    // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                    // si procede all'impostazione della variabile con il dato di configurazione centrale
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                        morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                    // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                    // si procede all'impostazione delle variabili con il dato di configurazione centrale
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                    {
                        afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                    }
                }

            }
            #endregion

            //se non vengono estratti i limiti dal piano orario
            else
            {
                // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                if (col != null)
                {
                    // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                    if (col.Limite_Entrata_Mattina_Col.HasValue)
                        morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                    // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                    if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                    {
                        afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                    }
                }

                // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                if (cant != null)
                {
                    // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                    if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                        morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                    // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                    if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                    {
                        afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                    }
                }

                // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                // si procede all'impostazione della variabile con il dato di configurazione centrale
                if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                    morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                // si procede all'impostazione delle variabili con il dato di configurazione centrale
                if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                {
                    afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                }

            }

            // costruzione dei dati di ritorno con i calcoli precedentemente effettuati
            // (la tolleranza del limite mattutino è impostata a null in quanto non presente)
            returnDic.Add(EntryLimitTypeEnum.Morning, new EntryLimitData() { EntryLimitTime = morningEntryLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.Afternoon, new EntryLimitData() { EntryLimitTime = afternoonEntryLimit, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitList, new EntryLimitData() { EntryLimitTimeList = morningEntryLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitList, new EntryLimitData() { EntryLimitTimeList = afternoonEntryLimitList, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningExit, new EntryLimitData() { ExitLimitTime = morningExitLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonExit, new EntryLimitData() { ExitLimitTime = afternoonExitLimit, EntryLimitTollerance = afternoonExitLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitListExit, new EntryLimitData() { ExitLimitTimeList = morningExitLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitListExit, new EntryLimitData() { ExitLimitTimeList = afternoonExitLimitList, EntryLimitTollerance = afternoonExitLimitTollerance });


            // ritorno delle configurazioni calcolate dal metodo
            return returnDic;

        }

        /// <summary>
        /// Calcola e restituisce con i dati specificati le cofigurazioni specifiche del limite d'entrata.
        /// </summary>
        /// <param name="cant">Il cantiere con cui calcolare la specifica configurazione.</param>
        /// <param name="col">Il collaboratore con cui calcolare la specifica configurazione.</param>
        /// <param name="date">La data di cui processare il limite d'entrata.</param>
        /// <param name="midDay">The mid day.</param>
        /// <returns>
        /// Un dizionario con chiave il tipo di limite d'entrata e valore i dati relativi.
        /// </returns>
        private Dictionary<EntryLimitTypeEnum, EntryLimitData> GetEntryLimitConifgTurni(Cant cant, Col col, DateTime date, TimeSpan midDay, Reg currentReg, Reg previousReg)
        {
            // inizializzazione del dizionario che conterrà le confgiurazioni da ritornare
            var returnDic = new Dictionary<EntryLimitTypeEnum, EntryLimitData>();

            // si calcolano i parametri del limite d'entrata recuperando i dati dai 3 elementi che li contengono e privilegiando la
            // gerarchia collaboratore, cantiere, parametri se non richiesto di utilizzare l'eventuale orario collegato al collaboratore;
            // in definitiva il limite d'entrata mattutino e pomeridiano è dato:
            // - in caso sia richiesto il recupero da orario e il collaboratore abbia un orario collegato, con definizione di entrata:
            //      - il limite d'entrata mattutino è dato dalla prima entrata pre metà giornata
            //      - il limite d'entrata pomeridiano è dato dalla prima entrata post metà giornata
            // - in caso non sia configurato il calcolo del limite d'entrata con l'orario si procede alla lettura dei
            //   parametri utilizzando la gerarchia:
            //      - collaboratore
            //      - cantiere
            //      - parametri

            // inizializzazione dei valori che conterranno i dati da restituire nel dizionario
            TimeSpan? morningEntryLimit = null;
            List<TimeSpan> morningEntryLimitList = null;
            List<TimeSpan> afternoonEntryLimitList = null;
            TimeSpan? afternoonEntryLimit = null;
            TimeSpan? afternoonEntryLimitTollerance = GetEntryLimitTolleranceValue(col, cant);
            TimeSpan? morningExitLimit = null;
            List<TimeSpan> morningExitLimitList = null;
            List<TimeSpan> afternoonExitLimitList = null;
            TimeSpan? afternoonExitLimit = null;
            TimeSpan? afternoonExitLimitTollerance = GetEntryLimitTolleranceValue(col, cant);

            #region LIMITE DI ENTRATA DA ORARIO

            // se è configurato l'utilizzo dell'orario per il calcolo del limite d'entrata
            if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Usa_Orario && col.Tab_Orari_Tipo_Id.HasValue)
            {
                // calcolo del piano di dettaglio per il giorno/collaboratore
                List<Tuple<int, TimeSpan, TimeSpan>> dayColPlanDetail = RepoManager.Tab_OrariRepo.GetDayPlanDetail(date, col.Col_Id);

                // se sono presenti dei piani con entrata e uscita per piano collaboratore
                if (dayColPlanDetail.Any())
                {
                    // selezione delle ore d'entrata e loro ordinamento
                    var sortedEntryTimes = dayColPlanDetail.Select(dayDetail => dayDetail.Item2).OrderBy(entryTime => entryTime).ToList();

                    // il limite d'entrata mattutino, se presente, è il primo valore nella prima metà della giornata
                    morningEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime < midDay);


                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata mattutini
                        morningEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime < midDay).ToList();



                    // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                    afternoonEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime >= midDay);

                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata pomeridiani
                        afternoonEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime >= midDay).ToList();

                    // selezione delle ore d'uscita e loro ordinamento 
                    var sortedExitTime = dayColPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(entryTime => entryTime).ToList();

                    // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                    morningExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime < midDay);


                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata mattutini
                        morningExitLimitList = sortedExitTime.Where(entryTime => entryTime < midDay).ToList();



                    // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                    afternoonExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime >= midDay);

                    //nel caso in cui vi siano più orari
                    if (sortedEntryTimes.Count >= 1)
                        //vengono estratti tutti i limiti di entrata pomeridiani
                        afternoonExitLimitList = sortedExitTime.Where(entryTime => entryTime >= midDay).ToList();

                }
                else
                {
                    // se non è presente un orario vado a impostare i parametri o generali o del collaboratore/cantiere
                    // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                    if (col != null)
                    {
                        // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                        if (col.Limite_Entrata_Mattina_Col.HasValue)
                            morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                        // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                        if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                        {
                            afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                        }
                    }

                    // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                    if (cant != null)
                    {
                        // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                        if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                            morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                        // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                        if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                        {
                            afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                        }
                    }

                    // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                    // si procede all'impostazione della variabile con il dato di configurazione centrale
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                        morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                    // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                    // si procede all'impostazione delle variabili con il dato di configurazione centrale
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                    {
                        afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                    }
                }
            }
            #endregion

            //se non vengono estratti i limiti dal piano orario
            else
            {
                 // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                 if (col != null)
                 {
                     // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                     if (col.Limite_Entrata_Mattina_Col.HasValue)
                         morningEntryLimit = col.Limite_Entrata_Mattina_Col;
                 
                     // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                     if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                     {
                         afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                     }
                 }
                 
                 // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                 if (cant != null)
                 {
                     // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                     if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                         morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;
                 
                     // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                     if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                     {
                         afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                     }
                 }
                 
                 // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                 // si procede all'impostazione della variabile con il dato di configurazione centrale
                 if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                     morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;
                 
                 // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                 // si procede all'impostazione delle variabili con il dato di configurazione centrale
                 if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                 {
                     afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                 }
                
            }

            // costruzione dei dati di ritorno con i calcoli precedentemente effettuati
            // (la tolleranza del limite mattutino è impostata a null in quanto non presente)
            returnDic.Add(EntryLimitTypeEnum.Morning, new EntryLimitData() { EntryLimitTime = morningEntryLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.Afternoon, new EntryLimitData() { EntryLimitTime = afternoonEntryLimit, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitList, new EntryLimitData() { EntryLimitTimeList = morningEntryLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitList, new EntryLimitData() { EntryLimitTimeList = afternoonEntryLimitList, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningExit, new EntryLimitData() { EntryLimitTime = morningExitLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonExit, new EntryLimitData() { EntryLimitTime = afternoonExitLimit, EntryLimitTollerance = afternoonExitLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitListExit, new EntryLimitData() { EntryLimitTimeList = morningExitLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitListExit, new EntryLimitData() { EntryLimitTimeList = afternoonExitLimitList, EntryLimitTollerance = afternoonExitLimitTollerance });

            // ritorno delle configurazioni calcolate dal metodo
            return returnDic;

        }

        public bool SameWeek(Reg currentReg, Reg previousReg) {
            bool equal = false;
            if (currentReg.Registrazione_Data_Ora_Fis_Reg.DayOfYear - currentReg.Registrazione_Data_Ora_Fis_Reg.DayOfWeek == previousReg.Registrazione_Data_Ora_Fis_Reg.DayOfYear - previousReg.Registrazione_Data_Ora_Fis_Reg.DayOfWeek) {
                equal = true;
            }
            return equal;
        }

        /// <summary>
        /// Calcola e restituisce con i dati specificati le cofigurazioni specifiche del limite d'entrata.
        /// </summary>
        /// <param name="cant">Il cantiere con cui calcolare la specifica configurazione.</param>6
        /// <param name="col">Il collaboratore con cui calcolare la specifica configurazione.</param>
        /// <param name="date">La data di cui processare il limite d'entrata.</param>
        /// <param name="midDay">The mid day.</param>
        /// <returns>
        /// Un dizionario con chiave il tipo di limite d'entrata e valore i dati relativi.
        /// </returns>
        private Dictionary<EntryLimitTypeEnum, EntryLimitData> GetEntryLimitConifg(Cant cant, Col col, DateTime date, TimeSpan midDay)
        {
            // inizializzazione del dizionario che conterrà le confgiurazioni da ritornare
            var returnDic = new Dictionary<EntryLimitTypeEnum, EntryLimitData>();

            // si calcolano i parametri del limite d'entrata recuperando i dati dai 3 elementi che li contengono e privilegiando la
            // gerarchia collaboratore, cantiere, parametri se non richiesto di utilizzare l'eventuale orario collegato al collaboratore;
            // in definitiva il limite d'entrata mattutino e pomeridiano è dato:
            // - in caso sia richiesto il recupero da orario e il collaboratore abbia un orario collegato, con definizione di entrata:
            //      - il limite d'entrata mattutino è dato dalla prima entrata pre metà giornata
            //      - il limite d'entrata pomeridiano è dato dalla prima entrata post metà giornata
            // - in caso non sia configurato il calcolo del limite d'entrata con l'orario si procede alla lettura dei
            //   parametri utilizzando la gerarchia:
            //      - collaboratore
            //      - cantiere
            //      - parametri

            // inizializzazione dei valori che conterranno i dati da restituire nel dizionario
            TimeSpan? morningEntryLimit = null;
            List<TimeSpan> morningEntryLimitList = null;
            List<TimeSpan> afternoonEntryLimitList = null;
            TimeSpan? afternoonEntryLimit = null;
            TimeSpan? afternoonEntryLimitTollerance = GetEntryLimitTolleranceValue(col, cant);
            TimeSpan? morningExitLimit = null;
            List<TimeSpan> morningExitLimitList = null;
            List<TimeSpan> afternoonExitLimitList = null;
            TimeSpan? afternoonExitLimit = null;
            TimeSpan? afternoonExitLimitTollerance = GetEntryLimitTolleranceValue(col, cant);

            #region LIMITE DI ENTRATA DA ORARIO

            // se è configurato l'utilizzo dell'orario per il calcolo del limite d'entrata
            if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Usa_Orario && col.Tab_Orari_Tipo_Id.HasValue)
            {
                // calcolo del piano di dettaglio per il giorno/collaboratore
                List<Tuple<int, TimeSpan, TimeSpan>> dayColPlanDetail = RepoManager.Tab_OrariRepo.GetDayPlanDetailCant(date, col.Col_Id,cant.Cant_Id);

                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AllColLimitiByOrario) == 1)
                {
                    // se sono presenti dei piani con entrata e uscita per piano collaboratore
                    if (dayColPlanDetail.Any() && col.Tipo_Contratto_Col == "4")
                    {
                        // selezione delle ore d'entrata e loro ordinamento
                        var sortedEntryTimes = dayColPlanDetail.Select(dayDetail => dayDetail.Item2).OrderBy(entryTime => entryTime).ToList();

                        // il limite d'entrata mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata mattutini
                            morningEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime >= midDay).ToList();

                        // selezione delle ore d'uscita e loro ordinamento 
                        var sortedExitTime = dayColPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(entryTime => entryTime).ToList();

                        // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata mattutini
                            morningExitLimitList = sortedExitTime.Where(entryTime => entryTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonExitLimitList = sortedExitTime.Where(entryTime => entryTime >= midDay).ToList();

                    }
                    else
                    {
                        // se non è presente un orario vado a impostare i parametri o generali o del collaboratore/cantiere
                        // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                        if (col != null)
                        {
                            // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                            if (col.Limite_Entrata_Mattina_Col.HasValue)
                                morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                            // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                            if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                            {
                                afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                            }
                        }

                        // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                        if (cant != null)
                        {
                            // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                            if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                                morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                            // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                            if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                            {
                                afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                            }
                        }

                        // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                        // si procede all'impostazione della variabile con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                            morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                        // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                        // si procede all'impostazione delle variabili con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                        {
                            afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                        }
                    }
                }
                else {
                    // se sono presenti dei piani con entrata e uscita per piano collaboratore
                    if (dayColPlanDetail.Any())
                    {
                        // selezione delle ore d'entrata e loro ordinamento
                        var sortedEntryTimes = dayColPlanDetail.Select(dayDetail => dayDetail.Item2).OrderBy(entryTime => entryTime).ToList();

                        // il limite d'entrata mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata mattutini
                            morningEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime >= midDay).ToList();

                        // selezione delle ore d'uscita e loro ordinamento 
                        var sortedExitTime = dayColPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(entryTime => entryTime).ToList();

                        // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata mattutini
                            morningExitLimitList = sortedExitTime.Where(entryTime => entryTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonExitLimitList = sortedExitTime.Where(entryTime => entryTime >= midDay).ToList();

                    }
                    else
                    {
                        // se non è presente un orario vado a impostare i parametri o generali o del collaboratore/cantiere
                        // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                        if (col != null)
                        {
                            // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                            if (col.Limite_Entrata_Mattina_Col.HasValue)
                                morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                            // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                            if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                            {
                                afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                            }
                        }

                        // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                        if (cant != null)
                        {
                            // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                            if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                                morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                            // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                            if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                            {
                                afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                            }
                        }

                        // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                        // si procede all'impostazione della variabile con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                            morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                        // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                        // si procede all'impostazione delle variabili con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                        {
                            afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                        }
                    }
                }

                
            }
            else // se è configurato l'utilizzo dell'orario per il calcolo del limite d'entrata
            if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Usa_Orario && cant.Tab_Orari_Tipo_Id.HasValue)
            {
                // calcolo del piano di dettaglio per il giorno/collaboratore
                List<Tuple<int, TimeSpan, TimeSpan>> dayCanPlanDetail = RepoManager.Tab_OrariRepo.GetDayPlanDetail(date, cant.Cant_Id, "Can");

                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AllColLimitiByOrario) == 1)
                {
                    // se sono presenti dei piani con entrata e uscita per piano collaboratore
                    if (dayCanPlanDetail.Any() && col.Tipo_Contratto_Col == "4")
                    {
                        // selezione delle ore d'entrata e loro ordinamento
                        var sortedEntryTimes = dayCanPlanDetail.Select(dayDetail => dayDetail.Item2).OrderBy(entryTime => entryTime).ToList();

                        // il limite d'entrata mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata mattutini
                            morningEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime >= midDay).ToList();

                        // selezione delle ore d'uscita e loro ordinamento 
                        var sortedExitTime = dayCanPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(entryTime => entryTime).ToList();

                        // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata mattutini
                            morningExitLimitList = sortedExitTime.Where(entryTime => entryTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonExitLimitList = sortedExitTime.Where(entryTime => entryTime >= midDay).ToList();

                    }
                    else
                    {
                        // se non è presente un orario vado a impostare i parametri o generali o del collaboratore/cantiere
                        // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                        if (col != null)
                        {
                            // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                            if (col.Limite_Entrata_Mattina_Col.HasValue)
                                morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                            // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                            if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                            {
                                afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                            }
                        }

                        // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                        if (cant != null)
                        {
                            // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                            if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                                morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                            // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                            if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                            {
                                afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                            }
                        }

                        // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                        // si procede all'impostazione della variabile con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                            morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                        // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                        // si procede all'impostazione delle variabili con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                        {
                            afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                        }
                    }
                }
                else
                {
                    // se sono presenti dei piani con entrata e uscita per piano collaboratore
                    if (dayCanPlanDetail.Any())
                    {
                        // selezione delle ore d'entrata e loro ordinamento
                        var sortedEntryTimes = dayCanPlanDetail.Select(dayDetail => dayDetail.Item2).OrderBy(entryTime => entryTime).ToList();

                        // il limite d'entrata mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata mattutini
                            morningEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonEntryLimit = sortedEntryTimes.FirstOrDefault(entryTime => entryTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonEntryLimitList = sortedEntryTimes.Where(entryTime => entryTime >= midDay).ToList();

                        // selezione delle ore d'uscita e loro ordinamento 
                        var sortedExitTime = dayCanPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(entryTime => entryTime).ToList();

                        // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata mattutini
                            morningExitLimitList = sortedExitTime.Where(entryTime => entryTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonExitLimit = sortedExitTime.FirstOrDefault(entryTime => entryTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonExitLimitList = sortedExitTime.Where(entryTime => entryTime >= midDay).ToList();

                    }
                    else
                    {
                        // se non è presente un orario vado a impostare i parametri o generali o del collaboratore/cantiere
                        // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                        if (col != null)
                        {
                            // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                            if (col.Limite_Entrata_Mattina_Col.HasValue)
                                morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                            // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                            if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                            {
                                afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                            }
                        }

                        // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                        if (cant != null)
                        {
                            // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                            if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                                morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                            // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                            if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                            {
                                afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                            }
                        }

                        // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                        // si procede all'impostazione della variabile con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                            morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                        // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                        // si procede all'impostazione delle variabili con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                        {
                            afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                        }
                    }
                }


            }
            #endregion

            //se non vengono estratti i limiti dal piano orario
            else
            {
                 // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                 if (col != null)
                 {
                     // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                     if (col.Limite_Entrata_Mattina_Col.HasValue)
                         morningEntryLimit = col.Limite_Entrata_Mattina_Col;
                 
                     // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                     if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                     {
                         afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                     }
                 }
                 
                 // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                 if (cant != null)
                 {
                     // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                     if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                         morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;
                 
                     // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                     if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                     {
                         afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                     }
                 }
                 
                 // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                 // si procede all'impostazione della variabile con il dato di configurazione centrale
                 if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                     morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;
                 
                 // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                 // si procede all'impostazione delle variabili con il dato di configurazione centrale
                 if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                 {
                     afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                 }
            }

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiXCol) == 1)
            {
                if (col.Telefono_3_Col != null && col.Telefono_4_Col != null && col.Fax_1_Col != null) 
                {
                    int cantId = Int32.Parse(col.Fax_1_Col);
                    if (cantId == cant.Cant_Id) {
                        morningEntryLimit = TimeSpan.Parse(col.Telefono_3_Col);
                        afternoonEntryLimit = TimeSpan.Parse(col.Telefono_4_Col);
                    }
                }
            }

            // costruzione dei dati di ritorno con i calcoli precedentemente effettuati
            // (la tolleranza del limite mattutino è impostata a null in quanto non presente)
            returnDic.Add(EntryLimitTypeEnum.Morning, new EntryLimitData() { EntryLimitTime = morningEntryLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.Afternoon, new EntryLimitData() { EntryLimitTime = afternoonEntryLimit, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitList, new EntryLimitData() { EntryLimitTimeList = morningEntryLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitList, new EntryLimitData() { EntryLimitTimeList = afternoonEntryLimitList, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningExit, new EntryLimitData() { EntryLimitTime = morningExitLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonExit, new EntryLimitData() { EntryLimitTime = afternoonExitLimit, EntryLimitTollerance = afternoonExitLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitListExit, new EntryLimitData() { EntryLimitTimeList = morningExitLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitListExit, new EntryLimitData() { EntryLimitTimeList = afternoonExitLimitList, EntryLimitTollerance = afternoonExitLimitTollerance });

            // ritorno delle configurazioni calcolate dal metodo
            return returnDic;

        }

        private Dictionary<ExitLimitTypeEnum, ExitLimitData> GetExitLimitConifg(Cant cant, Col col, DateTime date, TimeSpan midDay)
        {
            var returnDic = new Dictionary<ExitLimitTypeEnum, ExitLimitData>();

            // si calcolano i parametri del limite d'entrata recuperando i dati dai 3 elementi che li contengono e privilegiando la
            // gerarchia collaboratore, cantiere, parametri se non richiesto di utilizzare l'eventuale orario collegato al collaboratore;
            // in definitiva il limite d'entrata mattutino e pomeridiano è dato:
            // - in caso sia richiesto il recupero da orario e il collaboratore abbia un orario collegato, con definizione di entrata:
            //      - il limite d'entrata mattutino è dato dalla prima entrata pre metà giornata
            //      - il limite d'entrata pomeridiano è dato dalla prima entrata post metà giornata
            // - in caso non sia configurato il calcolo del limite d'entrata con l'orario si procede alla lettura dei
            //   parametri utilizzando la gerarchia:
            //      - collaboratore
            //      - cantiere
            //      - parametri

            // inizializzazione dei valori che conterranno i dati da restituire nel dizionario
            TimeSpan? morningExitLimit = null;
            TimeSpan? morningExitLimitTollerance = GetExitLimitMorningTolleranceValue(col, cant);
            List<TimeSpan> morningExitLimitList = null;
            List<TimeSpan> afternoonExitLimitList = null;
            TimeSpan? afternoonExitLimit = null;
            TimeSpan? afternoonExitLimitTollerance = GetExitLimitAfternoonTolleranceValue(col, cant);

            #region LIMITE DI USCITA DA ORARIO
            // se è configurato l'utilizzo dell'orario per il calcolo del limite d'entrata
            if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Usa_Orario && col.Tab_Orari_Tipo_Id.HasValue)
            {
                // calcolo del piano di dettaglio per il giorno/collaboratore
                List<Tuple<int, TimeSpan, TimeSpan>> dayColPlanDetail = RepoManager.Tab_OrariRepo.GetDayPlanDetailCant(date, col.Col_Id, cant.Cant_Id);

                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AllColLimitiByOrario) == 0) {
                    // se sono presenti dei piani con entrata e uscita per piano collaboratore
                    if (dayColPlanDetail.Any())
                    {
                        // selezione delle ore d'uscita e loro ordinamento
                        var sortedEntryTimes = dayColPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(exitTime => exitTime).ToList();

                        // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningExitLimit = sortedEntryTimes.FirstOrDefault(exitTime => exitTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di uscita mattutini
                            morningExitLimitList = sortedEntryTimes.Where(exitTime => exitTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonExitLimit = sortedEntryTimes.FirstOrDefault(exitTime => exitTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonExitLimitList = sortedEntryTimes.Where(exitTime => exitTime >= midDay).ToList();

                    }
                }
                else{
                    // se sono presenti dei piani con entrata e uscita per piano collaboratore
                    if (dayColPlanDetail.Any() && col.Tipo_Contratto_Col == "4")
                    {
                        // selezione delle ore d'uscita e loro ordinamento
                        var sortedEntryTimes = dayColPlanDetail.Select(dayDetail => dayDetail.Item3).OrderBy(exitTime => exitTime).ToList();

                        // il limite d'uscita mattutino, se presente, è il primo valore nella prima metà della giornata
                        morningExitLimit = sortedEntryTimes.FirstOrDefault(exitTime => exitTime < midDay);


                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di uscita mattutini
                            morningExitLimitList = sortedEntryTimes.Where(exitTime => exitTime < midDay).ToList();



                        // il limite d'entrata pomeridiano, se presente, è il primo valore successivo alla seconda metà della giornata;
                        afternoonExitLimit = sortedEntryTimes.FirstOrDefault(exitTime => exitTime >= midDay);

                        //nel caso in cui vi siano più orari
                        if (sortedEntryTimes.Count >= 1)
                            //vengono estratti tutti i limiti di entrata pomeridiani
                            afternoonExitLimitList = sortedEntryTimes.Where(exitTime => exitTime >= midDay).ToList();

                    }
                    else {
                        // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                        if (col != null)
                        {
                            // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                            if (col.Limite_Uscita_Mattina_Col.HasValue)
                                morningExitLimit = col.Limite_Uscita_Mattina_Col;

                            // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                            if (col.Limite_Uscita_Pomeriggio_Col.HasValue)
                            {
                                afternoonExitLimit = col.Limite_Uscita_Pomeriggio_Col;
                            }

                            if (col.Tolleranza_Limite_Uscita_Pomeriggio_Col.HasValue)
                            {

                            }
                        }

                        // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                        if (cant != null)
                        {
                            // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                            if (!morningExitLimit.HasValue && cant.Limite_Uscita_Mattina_Cant.HasValue)
                                morningExitLimit = cant.Limite_Uscita_Mattina_Cant;

                            // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                            if (!afternoonExitLimit.HasValue && cant.Limite_Uscita_Pomeriggio_Cant.HasValue)
                            {
                                afternoonExitLimit = cant.Limite_Uscita_Pomeriggio_Cant;
                            }
                        }

                        // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                        // si procede all'impostazione della variabile con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Mattina.HasValue && !morningExitLimit.HasValue)
                            morningExitLimit = RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Mattina;

                        // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                        // si procede all'impostazione delle variabili con il dato di configurazione centrale
                        if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Pomeriggio.HasValue && !afternoonExitLimit.HasValue)
                        {
                            afternoonExitLimit = RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Pomeriggio;
                        }
                    }
                }
            }
            #endregion

            //se non vengono estratti i limiti dal piano orario
            else
            {
                // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                if (col != null)
                {
                    // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                    if (col.Limite_Uscita_Mattina_Col.HasValue)
                        morningExitLimit = col.Limite_Uscita_Mattina_Col;

                    // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                    if (col.Limite_Uscita_Pomeriggio_Col.HasValue)
                    {
                        afternoonExitLimit = col.Limite_Uscita_Pomeriggio_Col;
                    }

                    if (col.Tolleranza_Limite_Uscita_Pomeriggio_Col.HasValue) {
                        
                    }
                }

                // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                if (cant != null)
                {
                    // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                    if (!morningExitLimit.HasValue && cant.Limite_Uscita_Mattina_Cant.HasValue)
                        morningExitLimit = cant.Limite_Uscita_Mattina_Cant;

                    // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                    if (!afternoonExitLimit.HasValue && cant.Limite_Uscita_Pomeriggio_Cant.HasValue)
                    {
                        afternoonExitLimit = cant.Limite_Uscita_Pomeriggio_Cant;
                    }
                }

                // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                // si procede all'impostazione della variabile con il dato di configurazione centrale
                if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Mattina.HasValue && !morningExitLimit.HasValue)
                    morningExitLimit = RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Mattina;

                // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                // si procede all'impostazione delle variabili con il dato di configurazione centrale
                if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Pomeriggio.HasValue && !afternoonExitLimit.HasValue)
                {
                    afternoonExitLimit = RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Pomeriggio;
                }

            }

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.LimitiXCol) == 1)
            {
                if (col.Telefono_3_Rif_Col != null && col.Telefono_4_Rif_Col != null && col.Fax_1_Col != null)
                {
                    if (int.Parse(col.Fax_1_Col) == cant.Cant_Id) {
                        morningExitLimit = TimeSpan.Parse(col.Telefono_3_Rif_Col);
                        afternoonExitLimit = TimeSpan.Parse(col.Telefono_4_Rif_Col);
                    }
                }
            }

            // costruzione dei dati di ritorno con i calcoli precedentemente effettuati
            // (la tolleranza del limite mattutino è impostata a null in quanto non presente)
            returnDic.Add(ExitLimitTypeEnum.Morning, new ExitLimitData() { ExitLimitTime = morningExitLimit, ExitLimitTollerance = morningExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.Afternoon, new ExitLimitData() { ExitLimitTime = afternoonExitLimit, ExitLimitTollerance = afternoonExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.MorningDealyLimitList, new ExitLimitData() { ExitLimitTimeList = morningExitLimitList, ExitLimitTollerance = morningExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.AfternoonDealyLimitList, new ExitLimitData() { ExitLimitTimeList = afternoonExitLimitList, ExitLimitTollerance = afternoonExitLimitTollerance });


            // ritorno delle configurazioni calcolate dal metodo
            return returnDic;
        }

        private Dictionary<EntryLimitTypeEnum, EntryLimitData> GetEntryLimitConifgOrario(Cant cant, Col col, DateTime date, TimeSpan midDay, Reg regE, Reg regU)
        {
            // inizializzazione del dizionario che conterrà le confgiurazioni da ritornare
            var returnDic = new Dictionary<EntryLimitTypeEnum, EntryLimitData>();

            // si calcolano i parametri del limite d'entrata recuperando i dati dai 3 elementi che li contengono e privilegiando la
            // gerarchia collaboratore, cantiere, parametri se non richiesto di utilizzare l'eventuale orario collegato al collaboratore;
            // in definitiva il limite d'entrata mattutino e pomeridiano è dato:
            // - in caso sia richiesto il recupero da orario e il collaboratore abbia un orario collegato, con definizione di entrata:
            //      - il limite d'entrata mattutino è dato dalla prima entrata pre metà giornata
            //      - il limite d'entrata pomeridiano è dato dalla prima entrata post metà giornata
            // - in caso non sia configurato il calcolo del limite d'entrata con l'orario si procede alla lettura dei
            //   parametri utilizzando la gerarchia:
            //      - collaboratore
            //      - cantiere
            //      - parametri

            // inizializzazione dei valori che conterranno i dati da restituire nel dizionario
            TimeSpan? morningEntryLimit = null;
            List<TimeSpan> morningEntryLimitList = null;
            List<TimeSpan> afternoonEntryLimitList = null;
            TimeSpan? afternoonEntryLimit = null;
            TimeSpan? afternoonEntryLimitTollerance = GetEntryLimitTolleranceValue(col, cant);
            TimeSpan? morningExitLimit = null;
            List<TimeSpan> morningExitLimitList = null;
            List<TimeSpan> afternoonExitLimitList = null;
            TimeSpan? afternoonExitLimit = null;
            TimeSpan? afternoonExitLimitTollerance = GetEntryLimitTolleranceValue(col, cant);

            if (regE != null && regU != null) {
                int tolleranza = 15;
                if (regE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay < midDay)
                {
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue)
                    {
                        tolleranza = (int)RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.Value.TotalMinutes;
                    }
                }
                else
                {
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue)
                    {
                        tolleranza = (int)RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.Value.TotalMinutes;
                    }
                }
                //recupero l'orario del cantiere
                List<Tab_Orari> orario = RepoManager.Tab_OrariRepo.GetAll().Where(orr => orr.Tab_Orari_Tipo_Id == cant.Tab_Orari_Tipo_Id).ToList();
                //Creo degli orari che siano 15 minuti prima e dopo le due timbrature
                DateTime beforeE = regE.Registrazione_Data_Ora_Fig_Reg.Value.Add(new TimeSpan(0, -tolleranza, 0));
                DateTime afterE = regE.Registrazione_Data_Ora_Fig_Reg.Value.Add(new TimeSpan(0, tolleranza, 0));
                DateTime beforeU = regU.Registrazione_Data_Ora_Fig_Reg.Value.Add(new TimeSpan(0, -tolleranza, 0));
                DateTime afterU = regU.Registrazione_Data_Ora_Fig_Reg.Value.Add(new TimeSpan(0, tolleranza, 0));
                TimeSpan tempE = new TimeSpan();
                TimeSpan tempU = new TimeSpan();
                double diffE = 5000;
                double diffU = 5000;
                if (orario.Count() > 0)
                {
                    foreach (Tab_Orari or in orario)
                    {
                        //in base al giorno della timbratura seleziono il giorno dell'orario corretto
                        bool valido = false;
                        switch (regE.Registrazione_Data_Ora_Fig_Reg.Value.DayOfWeek)
                        {
                            case DayOfWeek.Monday:
                                if (or.G1)
                                    valido = true;
                                break;
                            case DayOfWeek.Tuesday:
                                if (or.G2)
                                    valido = true;
                                break;
                            case DayOfWeek.Wednesday:
                                if (or.G3)
                                    valido = true;
                                break;
                            case DayOfWeek.Thursday:
                                if (or.G4)
                                    valido = true;
                                break;
                            case DayOfWeek.Friday:
                                if (or.G5)
                                    valido = true;
                                break;
                            case DayOfWeek.Saturday:
                                if (or.G6)
                                    valido = true;
                                break;
                            case DayOfWeek.Sunday:
                                if (or.G7)
                                    valido = true;
                                break;
                        }
                        if (valido)
                        {
                            //se siamo in un giorno con orario vado a crearmi le ore in base agli orari
                            DateTime orarioE = new DateTime(afterE.Year, afterE.Month, afterE.Day, or.Ora_E.Value.Hours, or.Ora_E.Value.Minutes, or.Ora_E.Value.Seconds);
                            DateTime orarioU = new DateTime(afterU.Year, afterU.Month, afterU.Day, or.Ora_U.Value.Hours, or.Ora_U.Value.Minutes, or.Ora_U.Value.Seconds);
                            if (orarioE > beforeE && orarioE < afterE)
                            {
                                double tmpDiffE = (orarioE - regE.Registrazione_Data_Ora_Fig_Reg.Value).TotalMinutes;
                                //tmpDiffE = (orarioE - beforeE).TotalMinutes;
                                double tmpDiffU = (orarioU - afterU).TotalMinutes;
                                tmpDiffU = (orarioU - beforeU).TotalMinutes;
                                if (tmpDiffE < 0)
                                {
                                    tmpDiffE = tmpDiffE * -1;
                                }
                                if (/*tmpDiffU <= diffU &&*/ tmpDiffE <= diffE)
                                {
                                    diffU = Math.Abs(tmpDiffU);
                                    diffE = Math.Abs(tmpDiffE);
                                    tempE = or.Ora_E.Value;
                                    tempU = or.Ora_U.Value;
                                }
                            }
                        }
                    }
                    if (tempE < midDay)
                    {
                        morningEntryLimit = tempE;
                    }
                    else
                    {
                        afternoonEntryLimit = tempE;
                    }
                }
                else {
                    // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                    if (col != null)
                    {
                        // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                        if (col.Limite_Entrata_Mattina_Col.HasValue)
                            morningEntryLimit = col.Limite_Entrata_Mattina_Col;

                        // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                        if (col.Limite_Entrata_Pomeriggio_Col.HasValue)
                        {
                            afternoonEntryLimit = col.Limite_Entrata_Pomeriggio_Col;
                        }
                    }

                    // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                    if (cant != null)
                    {
                        // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                        if (!morningEntryLimit.HasValue && cant.Limite_Entrata_Mattina_Cant.HasValue)
                            morningEntryLimit = cant.Limite_Entrata_Mattina_Cant;

                        // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                        if (!afternoonEntryLimit.HasValue && cant.Limite_Entrata_Pomeriggio_Cant.HasValue)
                        {
                            afternoonEntryLimit = cant.Limite_Entrata_Pomeriggio_Cant;
                        }
                    }

                    // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                    // si procede all'impostazione della variabile con il dato di configurazione centrale
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina.HasValue && !morningEntryLimit.HasValue)
                        morningEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Mattina;

                    // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                    // si procede all'impostazione delle variabili con il dato di configurazione centrale
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio.HasValue && !afternoonEntryLimit.HasValue)
                    {
                        afternoonEntryLimit = RepoManager.ParamRepo.ParametersRow.Limite_Entrata_Pomeriggio;
                    }
                }
            }


            // costruzione dei dati di ritorno con i calcoli precedentemente effettuati
            // (la tolleranza del limite mattutino è impostata a null in quanto non presente)
            returnDic.Add(EntryLimitTypeEnum.Morning, new EntryLimitData() { EntryLimitTime = morningEntryLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.Afternoon, new EntryLimitData() { EntryLimitTime = afternoonEntryLimit, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitList, new EntryLimitData() { EntryLimitTimeList = morningEntryLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitList, new EntryLimitData() { EntryLimitTimeList = afternoonEntryLimitList, EntryLimitTollerance = afternoonEntryLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningExit, new EntryLimitData() { EntryLimitTime = morningExitLimit, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonExit, new EntryLimitData() { EntryLimitTime = afternoonExitLimit, EntryLimitTollerance = afternoonExitLimitTollerance });
            returnDic.Add(EntryLimitTypeEnum.MorningDealyLimitListExit, new EntryLimitData() { EntryLimitTimeList = morningExitLimitList, EntryLimitTollerance = null });
            returnDic.Add(EntryLimitTypeEnum.AfternoonDealyLimitListExit, new EntryLimitData() { EntryLimitTimeList = afternoonExitLimitList, EntryLimitTollerance = afternoonExitLimitTollerance });

            // ritorno delle configurazioni calcolate dal metodo
            return returnDic;

        }

        private Dictionary<ExitLimitTypeEnum, ExitLimitData> GetExitLimitConifgOrario(Cant cant, Col col, DateTime date, TimeSpan midDay, Reg regE, Reg regU)
        {
            // inizializzazione del dizionario che conterrà le confgiurazioni da ritornare
            var returnDic = new Dictionary<ExitLimitTypeEnum, ExitLimitData>();

            // si calcolano i parametri del limite d'entrata recuperando i dati dai 3 elementi che li contengono e privilegiando la
            // gerarchia collaboratore, cantiere, parametri se non richiesto di utilizzare l'eventuale orario collegato al collaboratore;
            // in definitiva il limite d'entrata mattutino e pomeridiano è dato:
            // - in caso sia richiesto il recupero da orario e il collaboratore abbia un orario collegato, con definizione di entrata:
            //      - il limite d'entrata mattutino è dato dalla prima entrata pre metà giornata
            //      - il limite d'entrata pomeridiano è dato dalla prima entrata post metà giornata
            // - in caso non sia configurato il calcolo del limite d'entrata con l'orario si procede alla lettura dei
            //   parametri utilizzando la gerarchia:
            //      - collaboratore
            //      - cantiere
            //      - parametri

            // inizializzazione dei valori che conterranno i dati da restituire nel dizionario
            TimeSpan? morningEntryLimit = null;
            List<TimeSpan> morningEntryLimitList = null;
            List<TimeSpan> afternoonEntryLimitList = null;
            TimeSpan? afternoonEntryLimit = null;
            TimeSpan? afternoonEntryLimitTollerance = GetEntryLimitTolleranceValue(col, cant);
            TimeSpan? morningExitLimit = null;
            List<TimeSpan> morningExitLimitList = null;
            List<TimeSpan> afternoonExitLimitList = null;
            TimeSpan? morningExitLimitTollerance = GetExitLimitMorningTolleranceValue(col, cant);
            TimeSpan? afternoonExitLimit = null;
            TimeSpan? afternoonExitLimitTollerance = GetEntryLimitTolleranceValue(col, cant);

            if (regE != null && regU != null)
            {
                List<Tab_Orari> orario = RepoManager.Tab_OrariRepo.GetAll().Where(orr => orr.Tab_Orari_Tipo_Id == cant.Tab_Orari_Tipo_Id).ToList();
                DateTime beforeE = regE.Registrazione_Data_Ora_Fig_Reg.Value.Add(new TimeSpan(0, -20, 0));
                DateTime afterE = regE.Registrazione_Data_Ora_Fig_Reg.Value.Add(new TimeSpan(0, 20, 0));
                DateTime beforeU = regU.Registrazione_Data_Ora_Fig_Reg.Value.Add(new TimeSpan(0, -20, 0));
                DateTime afterU = regU.Registrazione_Data_Ora_Fig_Reg.Value.Add(new TimeSpan(0, 20, 0));
                TimeSpan tempE = new TimeSpan();
                TimeSpan tempU = new TimeSpan();
                double diffE = 5000;
                double diffU = 5000;
                if (orario.Count() > 0)
                {
                    foreach (Tab_Orari or in orario)
                    {
                        bool valido = false;
                        switch (regE.Registrazione_Data_Ora_Fig_Reg.Value.DayOfWeek)
                        {
                            case DayOfWeek.Monday:
                                if (or.G1)
                                    valido = true;
                                break;
                            case DayOfWeek.Tuesday:
                                if (or.G2)
                                    valido = true;
                                break;
                            case DayOfWeek.Wednesday:
                                if (or.G3)
                                    valido = true;
                                break;
                            case DayOfWeek.Thursday:
                                if (or.G4)
                                    valido = true;
                                break;
                            case DayOfWeek.Friday:
                                if (or.G5)
                                    valido = true;
                                break;
                            case DayOfWeek.Saturday:
                                if (or.G6)
                                    valido = true;
                                break;
                            case DayOfWeek.Sunday:
                                if (or.G7)
                                    valido = true;
                                break;
                        }
                        if (valido)
                        {
                            //se siamo in un giorno con orario vado a crearmi le ore in base agli orari
                            DateTime orarioE = new DateTime(afterE.Year, afterE.Month, afterE.Day, or.Ora_E.Value.Hours, or.Ora_E.Value.Minutes, or.Ora_E.Value.Seconds);
                            DateTime orarioU = new DateTime(afterU.Year, afterU.Month, afterU.Day, or.Ora_U.Value.Hours, or.Ora_U.Value.Minutes, or.Ora_U.Value.Seconds);
                            if (orarioU > beforeU && orarioU < afterU)
                            {
                                double tmpDiffE = (orarioE - regE.Registrazione_Data_Ora_Fig_Reg.Value).TotalMinutes;
                                //tmpDiffE = (orarioE - beforeE).TotalMinutes;
                                double tmpDiffU = (orarioU - regU.Registrazione_Data_Ora_Fig_Reg.Value).TotalMinutes;
                                //tmpDiffU = (orarioU - beforeU).TotalMinutes;
                                if (tmpDiffU < 0)
                                {
                                    tmpDiffU = tmpDiffU * -1;
                                }
                                if (tmpDiffU <= diffU /*&& tmpDiffE <= diffE*/)
                                {
                                    diffU = Math.Abs(tmpDiffU);
                                    diffE = Math.Abs(tmpDiffE);
                                    tempE = or.Ora_E.Value;
                                    tempU = or.Ora_U.Value;
                                }
                            }
                        }
                    }
                    if (tempU < midDay)
                    {
                        morningExitLimit = tempU;
                    }
                    else
                    {
                        afternoonExitLimit = tempU;
                    }
                }
                else {
                    // se il collaboratore passato come parmetro è valorizzato si tenta di recuperare le configurazione da lui
                    if (col != null)
                    {
                        // se il collaboratore ha impostato il limite d'entrata mattutino si inserisce il valore nella variabile utilizzata dal metodo
                        if (col.Limite_Uscita_Mattina_Col.HasValue)
                            morningExitLimit = col.Limite_Uscita_Mattina_Col;

                        // se il collaboratore ha impostato il limite d'entrata pomeridiano si inseriscono i valori nelle variabili utilizzate dal metodo
                        if (col.Limite_Uscita_Pomeriggio_Col.HasValue)
                        {
                            afternoonExitLimit = col.Limite_Uscita_Pomeriggio_Col;
                        }

                        if (col.Tolleranza_Limite_Uscita_Pomeriggio_Col.HasValue)
                        {

                        }
                    }

                    // si procede alla verifica dei dati del cantiere solamente se è valorizzato e precedentemente
                    if (cant != null)
                    {
                        // se il cantiere ha impostato un valore di limite d'entrata mattutino e il collaboratore non l'ha settato, si procede all'impostazione della variabile con il dato del cantiere
                        if (!morningExitLimit.HasValue && cant.Limite_Uscita_Mattina_Cant.HasValue)
                            morningExitLimit = cant.Limite_Uscita_Mattina_Cant;

                        // se il cantiere ha impostato un valore di limite d'entrata pomeridiano e il collaboratore non l'ha settato, si procede all'impostazione delle variabili con il dato del cantiere
                        if (!afternoonExitLimit.HasValue && cant.Limite_Uscita_Pomeriggio_Cant.HasValue)
                        {
                            afternoonExitLimit = cant.Limite_Uscita_Pomeriggio_Cant;
                        }
                    }

                    // se la configurazione centrale ha impostato un valore di limite d'entrata mattutino e il collaboratore e il cantiere non l'hanno precedentemente setttato,
                    // si procede all'impostazione della variabile con il dato di configurazione centrale
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Mattina.HasValue && !morningExitLimit.HasValue)
                        morningExitLimit = RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Mattina;

                    // se la configurazione centrale ha impostato un valore di limite d'entrata pomeridiano e il collaboratore e il cantiere non l'hanno precedentemente settato,
                    // si procede all'impostazione delle variabili con il dato di configurazione centrale
                    if (RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Pomeriggio.HasValue && !afternoonExitLimit.HasValue)
                    {
                        afternoonExitLimit = RepoManager.ParamRepo.ParametersRow.Limite_Uscita_Pomeriggio;
                    }
                }
            }


            // costruzione dei dati di ritorno con i calcoli precedentemente effettuati
            // (la tolleranza del limite mattutino è impostata a null in quanto non presente)
            returnDic.Add(ExitLimitTypeEnum.Morning, new ExitLimitData() { ExitLimitTime = morningExitLimit, ExitLimitTollerance = morningExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.Afternoon, new ExitLimitData() { ExitLimitTime = afternoonExitLimit, ExitLimitTollerance = afternoonExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.MorningDealyLimitList, new ExitLimitData() { ExitLimitTimeList = morningExitLimitList, ExitLimitTollerance = morningExitLimitTollerance });
            returnDic.Add(ExitLimitTypeEnum.AfternoonDealyLimitList, new ExitLimitData() { ExitLimitTimeList = afternoonExitLimitList, ExitLimitTollerance = afternoonExitLimitTollerance });

            // ritorno delle configurazioni calcolate dal metodo
            return returnDic;

        }

        /// <summary>
        /// Recupera la tolleranza del limite d'entrata utilizzando i dati specificati.
        /// </summary>
        /// <param name="col">Il collaboratore da cui estrarre in gerarchia la tolleranza del limite d'entrata pomeridiano.</param>
        /// <param name="cant">Il cantiere da cui estrarre in gerarchia la tolleranza del limite d'entrata pomeridiano.</param>
        /// <returns>La tolleranza del limite d'entrata pomeridiano presente nei dati specificati.</returns>
        private TimeSpan? GetEntryLimitTolleranceValue(Col col, Cant cant)
        {
            // la tolleranza del limite d'entrata pomeridiano viene calcolata seguendo la seguente gerarchia:
            // - quella del collaboratore se presente
            // - quella del cantiere se presente
            // - quella della scheda parametri se presente

            TimeSpan? entryLimitTollerance = null;

            if (col.Tolleranza_Limite_Entrata_Pomeriggio_Col.HasValue)
                entryLimitTollerance = col.Tolleranza_Limite_Entrata_Pomeriggio_Col;

            if (!entryLimitTollerance.HasValue && cant.Tolleranza_Limite_Entrata_Pomeriggio_Cant.HasValue)
                entryLimitTollerance = cant.Tolleranza_Limite_Entrata_Pomeriggio_Cant;

            if (!entryLimitTollerance.HasValue && RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata_Pomeriggio.HasValue)
                entryLimitTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Entrata_Pomeriggio;

            return entryLimitTollerance;
        }

        private TimeSpan? GetExitLimitMorningTolleranceValue(Col col, Cant cant)
        {
            TimeSpan? exitLimitMorningTollerance = null;

            if (col.Tolleranza_Limite_Uscita_Mattina_Col.HasValue)
                exitLimitMorningTollerance = col.Tolleranza_Limite_Uscita_Mattina_Col;

            if (!exitLimitMorningTollerance.HasValue && cant.Tolleranza_Limite_Uscita_Mattina_Cant.HasValue)
                exitLimitMorningTollerance = cant.Tolleranza_Limite_Uscita_Mattina_Cant;

            if (!exitLimitMorningTollerance.HasValue && RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Uscita_Mattina.HasValue)
                exitLimitMorningTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Uscita_Mattina;

            return exitLimitMorningTollerance;
        }

        private TimeSpan? GetExitLimitAfternoonTolleranceValue(Col col, Cant cant)
        {
            TimeSpan? exitLimitAfternoonTollerance = null;

            if (col.Tolleranza_Limite_Uscita_Pomeriggio_Col.HasValue)
                exitLimitAfternoonTollerance = col.Tolleranza_Limite_Uscita_Pomeriggio_Col;

            if (!exitLimitAfternoonTollerance.HasValue && cant.Tolleranza_Limite_Uscita_Pomeriggio_Cant.HasValue)
                exitLimitAfternoonTollerance = cant.Tolleranza_Limite_Uscita_Pomeriggio_Cant;

            if (!exitLimitAfternoonTollerance.HasValue && RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Uscita_Pomeriggio.HasValue)
                exitLimitAfternoonTollerance = RepoManager.ParamRepo.ParametersRow.Tolleranza_Limite_Uscita_Pomeriggio;

            return exitLimitAfternoonTollerance;
        }



        #endregion



        #region Elaborazione Cartellino

        private Boolean IsTotalInRange(int year, int month, DateTime from, DateTime to)
        {
            var currPeriod = new DateTime(year, month, 1);
            return (currPeriod >= from && currPeriod <= to);
        }

        /// <summary>
        /// Metodo che dato il collaboratore, la lista dei cantieri e un periodo ritorna tutte le reg_v che compongono l'orario di riferimento
        /// (queste reg_v sono poi utilizzate per comporre l'orario previsto).
        /// </summary>
        /// <param name="col">Il collaboratore per cui ricercare gli orari.</param>
        /// <param name="cants">Il cantiere utilizzato per la generazione dell'orario previsto per cantiere.</param>
        /// <param name="from">L'inzio del periodo di ricerca delle reg_v (data compresa nel periodo considerato per la creazione).</param>
        /// <param name="to">La fine del periodo di ricerca delle reg_v (data compresa nel periodo considerato per la creazione).</param>
        /// <param name="isFromFreeTimesheet">Parametro utilizzato per ritornare il fatto che le reg_v di ritorno provengono da un record (nuovo o presente) della Col_Orari.</param>
        /// <param name="freeTimeSheetId">Parametro utilizzato per ritornare l'eventuale id della Col_Orari utilizzato per costruire l'elenco di reg_v;
        /// se questo parametro avrà valore 0 se le reg_v sono create da tab_orari o se il col_orari non è presente (nuovo)</param>
        /// <returns>La lista di reg_v la cui durata consiste nell'orario previsto.</returns>
        public List<Reg_V> GetTimesheet(Col col, List<Cant> cants, DateTime from, DateTime to, out bool isFromFreeTimesheet, out int freeTimeSheetId)
        {
            // recupero l'elenco dei giorni festivi
            var holidays = RepoManager.Tab_FestiviRepo.Find(hol => hol.Giorno_Tab_Festivi >= from && hol.Giorno_Tab_Festivi <= to, true).ToList();

            // inzializzazione della lista di reg_v che andranno a comporre il timesheet
            var result = new List<Reg_V>();

            int counter = 0;

            // se il collaboratore ha un tipo orario collegato
            if (col.Tab_Orari_Tipo_Id.HasValue)
            {
                #region Generazione orario previsto per i collaboratori che hanno collegato un tipo orario

                // in caso ci sia collegato un tipo orario si ritorna tale dato
                isFromFreeTimesheet = false;
                freeTimeSheetId = 0;

                // recupero tutti gli orari salvati nel database
                var allTimesheets = RepoManager.Tab_OrariRepo.GetAll(true).ToList();

                // per ogni giorno dell'intervallo passato come parametro
                for (DateTime i = from; i < to; i = i.AddDays(1))
                {
                    // recupero tutti gli orari del tipo specificato che hanno una data inizio minore alla data in elaborazione
                    var timesheets = allTimesheets.Where(tor => tor.Tab_Orari_Tipo_Id == col.Tab_Orari_Tipo_Id && i >= tor.Data_Inizio).ToList();

                    // se sono stati trovati deli orari validi
                    if (timesheets.Count > 0)
                    {
                        // recupero l'ultimo orario valido e recupero, con esso, tutti gli orari con stesso id e stessa data
                        var lastTimesheetDate = timesheets.Max(tor => tor.Data_Inizio);
                        timesheets = timesheets.Where(tor => tor.Data_Inizio == lastTimesheetDate).ToList();

                        // per ogni orario valido recuperato
                        foreach (var timesheet in timesheets.Where(timesheet =>
                            // se l'orario risulta applicabile alla data in elaborazione e la data in elaborazione non è un festivo
                            RepoManager.Tab_OrariRepo.IsToApplyTimesheet(timesheet, i) && holidays.All(hol => hol.Giorno_Tab_Festivi != i.Date)))
                        {
                            // se si sta elaborando un lunedì e nell'orario il lunedì è lavorabile
                            // si aggiungono alla lista tutte le reg_v per quel collaboratore, cantieri, giorno
                            if (i.DayOfWeek == DayOfWeek.Monday && timesheet.G1)
                                result.Add(GetNewRegVFromTimesheet(col, cants, counter--, i, timesheet));

                            // se si sta elaborando un martedì e nell'orario il martedì è lavorabile
                            // si aggiungono alla lista tutte le reg_v per quel collaboratore, cantieri, giorno
                            if (i.DayOfWeek == DayOfWeek.Tuesday && timesheet.G2)
                                result.Add(GetNewRegVFromTimesheet(col, cants, counter--, i, timesheet));

                            // se si sta elaborando un mercoledì e nell'orario il mercoledì è lavorabile
                            // si aggiungono alla lista tutte le reg_v per quel collaboratore, cantieri, giorno
                            if (i.DayOfWeek == DayOfWeek.Wednesday && timesheet.G3)
                                result.Add(GetNewRegVFromTimesheet(col, cants, counter--, i, timesheet));

                            // se si sta elaborando un giovedì e nell'orario il giovedì è lavorabile
                            // si aggiungono alla lista tutte le reg_v per quel collaboratore, cantieri, giorno
                            if (i.DayOfWeek == DayOfWeek.Thursday && timesheet.G4)
                                result.Add(GetNewRegVFromTimesheet(col, cants, counter--, i, timesheet));

                            // se si sta elaborando un venerdì e nell'orario il venerdì è lavorabile
                            // si aggiungono alla lista tutte le reg_v per quel collaboratore, cantieri, giorno
                            if (i.DayOfWeek == DayOfWeek.Friday && timesheet.G5)
                                result.Add(GetNewRegVFromTimesheet(col, cants, counter--, i, timesheet));

                            // se si sta elaborando un sabato e nell'orario il sabato è lavorabile
                            // si aggiungono alla lista tutte le reg_v per quel collaboratore, cantieri, giorno
                            if (i.DayOfWeek == DayOfWeek.Saturday && timesheet.G6)
                                result.Add(GetNewRegVFromTimesheet(col, cants, counter--, i, timesheet));

                            // se si sta elaborando un domenica e nell'orario il domenica è lavorabile
                            // si aggiungono alla lista tutte le reg_v per quel collaboratore, cantieri, giorno
                            if (i.DayOfWeek == DayOfWeek.Sunday && timesheet.G7)
                                result.Add(GetNewRegVFromTimesheet(col, cants, counter--, i, timesheet));
                        }
                    }
                }

                #endregion
            }
            else // se il collaboratore non ha un tipo orario collegato
            {
                #region Generazione orario previsto per i collaboratori che non hanno un tipo orario collegato

                // se il collaboratore non ha un tipo orario collegato allora si procede a generare un tipo orario libero;
                // se per il mese corrente esiste un record in Col_Orario si ripropone quello; in caso contrario si provvede
                // a generarne uno vuoto pronto per essere compilato e salvato runtime dall'utente stesso

                // in caso si generi il dato dalla tabella Col_Orario allora si ritorna tale dato
                isFromFreeTimesheet = true;

                // TODO: in futuro gestire anche recupero per cantiere e motivazione
                // TODO: in futuro gestire il fatto che tra from e to ci possa essere più di un mese
                // TODO: sostituire la gestione del ritorno con le reg_v
                // TODO: gestione del ritorno di più id di tabella Col_Orario
                // verifica della presenza di un col_orario per il collaboratore attualmente in processo, per l'anno e il mese del
                // periodo (entrata/uscita)
                var yearFrom = from.Year;
                var yearTo = to.Year;
                var monthFrom = from.Month;
                var monthTo = to.Month;
                var colFreeTimesheets = RepoManager.ColCantOrarioRepo.Find(co => co.Col_Id == col.Col_Id &&
                    ((co.Anno_Orario == yearFrom && co.Mese_Orario == monthFrom) || (co.Anno_Orario == yearTo && co.Mese_Orario == monthTo))).AsQueryable();

                // inizializzazione delle date del periodo richiesto
                var periodDates = CommonService.GetDatesFromPeriod(from, to);

                // se sono stati trovati dei dati allora si procede alla produzione delle reg_v con tali dati
                if (colFreeTimesheets.Any())
                {
                    // inzializzazione dell'id temporaneo del timesheet
                    int tmpFreeTimeSheetId = 0;

                    // ciclo di elaborazione delle date del periodo
                    periodDates.ForEach(date =>
                    {
                    // recupero, tra i Col_Orario recuperati quello relativo al mese/anno del giorno in elaborazione
                    var freeTimesheet = colFreeTimesheets.FirstOrDefault(cft => cft.Anno_Orario == date.Year && cft.Mese_Orario == date.Month);

                    // ritorno dell'id della Col_Orario collegato
                    tmpFreeTimeSheetId = tmpFreeTimeSheetId == 0 ? freeTimesheet.Col_Cant_Orario_Id : tmpFreeTimeSheetId;

                    // se l'orario è stato trovato allora si recupera la durata
                    double duration = 0; // valore di default in caso di mancanza orario
                        if (freeTimesheet != null)
                        {
                        // si recupera la durata dalla proprietà che la contiene corrispodente al giorno
                        // in elaborazione
                        duration = Convert.ToDouble(CommonService.GetPropertyValue(freeTimesheet, RepoManager.ColCantOrarioRepo.GetPropertyNameFromDate(date)));
                        }

                    // generazione della nuova reg_v con la durata calcolata
                    result.Add(new Reg_V()
                        {
                            RegE = counter--,
                            RegU = counter--,
                            Col_Id = col.Col_Id,
                            Col_Mnemonic = col.Codice_Collaboratore,
                            Col_Desc = col.CognomeNome_Col,
                            Data_Ora_Fig_E = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0),
                            Data_Ora_Fig_U = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0).AddMinutes(duration),
                            Durata_Fig = Convert.ToInt32(duration),
                            Motivazione_Reg_Cod = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN),
                            Motivazione_Reg_Id = -1
                        });
                    });

                    // ritorno dell'id della Col_Orario collegato
                    freeTimeSheetId = tmpFreeTimeSheetId;
                }
                else
                {
                    // ritorno dell'id della Col_Orario collegato (in questo caso nuovo)
                    freeTimeSheetId = 0;

                    // in caso non siano stati trovati dei dati allora si procede alla generazione di un orario vuoto (durata reg 0)
                    periodDates.ForEach(date => result.Add(new Reg_V()
                    {
                        RegE = counter--,
                        RegU = counter--,
                        Col_Id = col.Col_Id,
                        Col_Mnemonic = col.Codice_Collaboratore,
                        Col_Desc = col.CognomeNome_Col,
                        Data_Ora_Fig_E = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0),
                        Data_Ora_Fig_U = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0),
                        Durata_Fig = 0,
                        Motivazione_Reg_Cod = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN),
                        Motivazione_Reg_Id = -1
                    }));
                }

                #endregion
            }

            return result;

        }


        public List<Reg_V> Add2Minutes(List<Reg_V> regvlist)
        {
            // inzializzazione della lista di reg_v che saranno corrette
            var result = new List<Reg_V>();

            //estraggo il max e il min delle date presenti nelle reg
            DateTime? from = regvlist.Min(r => r.Data_Ora_Fis_E);
            DateTime? to = regvlist.Max(r => r.Data_Ora_Fis_U);

            //verifico se ho ricevuto delle REG NON ABBINATE
            var oddRegvs = regvlist.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None && regv.Registrazione_Stato_Reg == (int)RegStateEnum.None);
            if (oddRegvs.Count() > 0)
            {
            } //ho registarzione singole quindi devo far partire la mia routine }

            else return null;

            return null;
        }

        private static Reg_V GetNewRegVFromTimesheet(Col col, IEnumerable<Cant> cants, int counter, DateTime i, Tab_Orari timesheet)
        {
            Cant cant = cants.SingleOrDefault(can => can.Cant_Id == timesheet.Cant_Id);

            Reg_V newRegv = new Reg_V();

            // si procede con l'elaborazione solamente se non ci si sta basando per il timesheet sulla durata
            if (timesheet.Ora_E != null && timesheet.Ora_U != null)
            {
                newRegv.RegE = counter;
                newRegv.RegU = counter;

                newRegv.Col_Id = col.Col_Id;
                newRegv.Col_Mnemonic = col.Codice_Collaboratore;
                newRegv.Col_Desc = col.CognomeNome_Col;

                newRegv.Cant_Id = timesheet.Cant_Id;

                if (cant != null)
                {
                    newRegv.Cant_Mnemonic = cant.Codice_Cantiere;
                    newRegv.Cant_Desc = cant.Descrizione_Can;
                }

                newRegv.Data_Ora_Fig_E = i.Date.AddTicks(timesheet.Ora_E.Value.Ticks);
                newRegv.Data_Ora_Fig_U = i.Date.AddTicks(timesheet.Ora_U.Value.Ticks);
                newRegv.Motivazione_Reg_Cod = BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN);
                newRegv.Motivazione_Reg_Id = -1;

                var duration = newRegv.Data_Ora_Fig_U - newRegv.Data_Ora_Fig_E;
                if (duration.HasValue)
                    newRegv.Durata_Fig = (int)duration.Value.TotalMinutes;
            }
            return newRegv;
        }

        #endregion

        #region Gestione Data Blocco e Monte Minuti

        /// <summary>
        /// Elaborazione in caso di spostamento della data blocco
        /// </summary>
        /// <param name="cols">Lista dei collaboratori sui quali effettuare l'elaborazione</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="isBackward">if set to <c>true</c> [is backward].</param>
        /// <returns></returns>
        public List<KeyValuePair<String, String>> ElaborateBlockDate(List<Col> cols, DateTime from, DateTime to, Boolean isBackward = false)
        {
            // inizializzazione del numero di collaboratori da trattare
            double colCount = cols.Count();

            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<string, string>>();

            // in caso la data di partenza sia inferiore alla data della prima registrazione, allora la nuova data di partenza è la data
            // della prima registrazione (questo per evitare di ciclare anni a vuoto nel calcolo del monte minuti)
            DateTime firstRegVDate = Convert.ToDateTime(RepoManager.Reg_VRepo.Min(regv => regv.Data_Reg));
            if (from < firstRegVDate)
                from = firstRegVDate;

            // alla data di destinazione della data blocco sottraggo sempre un giorno (il giorno indicato -il 1° del mese- non è mai compreso)
            to = to.AddDays(-1);

            // inizializzazione dell'indice del collaboratore in esecuzione
            double colIndex = 0;

            //vengono presi in carico tutti i collaboraqtori
            foreach (var col in cols)
            {
                // aggiornamento dell'indice del collaboratore in elaborazione
                colIndex++;

                // inizializzazione del messaggio da visualizare nella label di elaborazione
                string colMessage = String.Format("In elaborazione collaboratore {0} di {1}", colIndex, colCount);

                // calcolo della percentuale di elaborazione
                double colPerc = (colIndex / colCount) * 100;

                // aggiornamento del dizionario di visualizzazione e posizionamento della progress bar
                BusinessService.EditBlockDateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(colPerc, colMessage);

                //se non si torna indietro con la data blocco
                if (!isBackward)
                {
                    // genero/aggiorno il monte minuti nel database per il periodo indicato
                    SetMonthMinutesAmmount(col, from, to);

                    // recupero dai minuti salvati nel monte minuti quanto inserire nel collaboratore
                    int totalMinutes = GetTotalMinutesFromMonthMinutesAmmounts(col, from, to);

                    col.Monte_Minuti += totalMinutes;
                }
                else col.Monte_Minuti -= SubstractTotals(col, from, to, true);
            }

            //viene aggiornato il campo nella tebella collaboratori
            RepoManager.ColRepo.Update(cols, true);

            return errors;
        }

        private bool IsToUpdate(List<int> elaboratePeriod, int month)
        {
            return elaboratePeriod.Contains(month);
        }

        /// <summary>
        /// Metodo di calcolo del totale del monte minuti dalla tabella Col_Monte minuti
        /// </summary>
        /// <param name="currCol">The curr col.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="isToSaveChanges">if set to <c>true</c> [is to save changes].</param>
        /// <returns></returns>
        public int SubstractTotals(Col currCol, DateTime from, DateTime to, Boolean isToSaveChanges = false)
        {
            //inizializzazione della somma totale del monte minuti per il collaboratore
            int sum = 0;

            //viene estratta la riga della tabella col_Monte minuti per l'anno compreso fra i limiti from e to 
            var totalsForCol = RepoManager.Col_Monte_MinutiRepo.Find(tot => tot.Col_Id == currCol.Col_Id && tot.Anno_Col_Monte_Minuti >= from.Year && tot.Anno_Col_Monte_Minuti <= to.Year).ToList();

            //si va costrutire un array con tutti i totali per mese
            foreach (var total in totalsForCol)
            {
                int[] totals = new int[13];
                totals[1] = total.M01_Col_Monte_Minuti;
                totals[2] = total.M02_Col_Monte_Minuti;
                totals[3] = total.M03_Col_Monte_Minuti;
                totals[4] = total.M04_Col_Monte_Minuti;
                totals[5] = total.M05_Col_Monte_Minuti;
                totals[6] = total.M06_Col_Monte_Minuti;
                totals[7] = total.M07_Col_Monte_Minuti;
                totals[8] = total.M08_Col_Monte_Minuti;
                totals[9] = total.M09_Col_Monte_Minuti;
                totals[10] = total.M10_Col_Monte_Minuti;
                totals[11] = total.M11_Col_Monte_Minuti;
                totals[12] = total.M12_Col_Monte_Minuti;

                //si va a calcolare la somma totale 
                for (int i = 1; i < totals.Length; i++)
                {
                    if (IsTotalInRange(total.Anno_Col_Monte_Minuti, i, from, to))
                    {
                        sum += totals[i];
                        if (isToSaveChanges)
                            totals[i] = 0;
                    }
                }

                //se nei parametri è richiesto il save changes allora si aggiornano i campi del database
                if (isToSaveChanges)
                {
                    total.M01_Col_Monte_Minuti = totals[1];
                    total.M02_Col_Monte_Minuti = totals[2];
                    total.M03_Col_Monte_Minuti = totals[3];
                    total.M04_Col_Monte_Minuti = totals[4];
                    total.M05_Col_Monte_Minuti = totals[5];
                    total.M06_Col_Monte_Minuti = totals[6];
                    total.M07_Col_Monte_Minuti = totals[7];
                    total.M08_Col_Monte_Minuti = totals[8];
                    total.M09_Col_Monte_Minuti = totals[9];
                    total.M10_Col_Monte_Minuti = totals[10];
                    total.M11_Col_Monte_Minuti = totals[11];
                    total.M12_Col_Monte_Minuti = totals[12];
                }
            }

            if (isToSaveChanges)
                RepoManager.Col_Monte_MinutiRepo.Update(totalsForCol, true);

            //viene ritornata la somma ottenuta
            return sum;
        }

        /// <summary>
        /// Imposta (inserisce o modifica) il monte minuti per il collaboratore indicato per il periodo specificato.
        /// </summary>
        /// <param name="col">Il collaboratore per cui inserire o aggiornare il monte minuti.</param>
        /// <param name="from">La data di inizio del periodo da processare.</param>
        /// <param name="to">La data di fine del periodo da processare.</param>
        /// <exception cref="System.ArgumentException">To date must be major than from date</exception>
        public void SetMonthMinutesAmmount(Col col, DateTime from, DateTime to)
        {
            // convalida input del meotodo, la data di arrivo deve essere maggiore della data di partenza
            if (to < from)
                throw new ArgumentException("To date must be major than from date");

            // inizializzo le liste dei record della tabella monte minuti da inserire e da aggiornare
            var colMonthsMinutesToAddOrUpdate = new List<Col_Monte_Minuti>();

            if (from.Year >= RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value.Year)
                from = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value;

            // viene effettuato un ciclo su ogni inizio mese nel periodo passato come parametro
            foreach (var firstMonthDate in CommonService.EachMonth(from, to))
            {
                // per ogni primo giorno del mese calcolo la data di fine del mese stesso
                DateTime endMonthDate = CommonService.GetLastMonthDay(firstMonthDate);

                // recupero (calcolando il cartellino) i minuti di delta del mese
                int monthMinutes = TimesheetModuleItem.GetLastMonthMinutesAmmount(col, firstMonthDate, endMonthDate, false);

                // verifico che nel database sia presente per il collaboratore e l'anno in elaborazione un monte minuti e, se è presente lo recupero
                Col_Monte_Minuti currentMonthsMinutes = RepoManager.Col_Monte_MinutiRepo.FirstOrDefault(monthMin => monthMin.Col_Id == col.Col_Id && monthMin.Anno_Col_Monte_Minuti == firstMonthDate.Year);

                // se il monte minuti non è stato trovato sul database allora provo a recuperarlo dalla lista degli elementi da inserire/aggiornare nel database
                if (currentMonthsMinutes == default(Col_Monte_Minuti))
                    currentMonthsMinutes = colMonthsMinutesToAddOrUpdate.FirstOrDefault(monthMin => monthMin.Col_Id == col.Col_Id && monthMin.Anno_Col_Monte_Minuti == firstMonthDate.Year);

                // se non è stato trovato un monte minuti per il collaboratore e l'anno in elaborazione si procede alla sua inizializzazione
                if (currentMonthsMinutes == default(Col_Monte_Minuti))
                {
                    currentMonthsMinutes = RepoManager.Col_Monte_MinutiRepo.Init();
                    currentMonthsMinutes.Col_Id = col.Col_Id;
                    currentMonthsMinutes.Anno_Col_Monte_Minuti = firstMonthDate.Year;
                }

                // in base al mese attualmente in elaborazione si aggiorna il campo del monte minuti in processo
                var monthPropertyName = MonthPropertyName(firstMonthDate);
                CommonService.SetPropertyValue(currentMonthsMinutes, monthPropertyName, monthMinutes);

                // una volta modificato il dato aggiungo, se non già presente, l'elemento alla lista dei monte minuti da processare
                if (!colMonthsMinutesToAddOrUpdate.Any(monthMin => monthMin.Col_Id == col.Col_Id && monthMin.Anno_Col_Monte_Minuti == firstMonthDate.Year))
                    colMonthsMinutesToAddOrUpdate.Add(currentMonthsMinutes);
            }

            // al termine dell'elaborazione, se c'è qualcosa da scrivere nel database lo scrivo e salvo l'operazione effettuata
            if (colMonthsMinutesToAddOrUpdate.Any())
            {
                // aggiungo eventuali dati da aggiungere (se presenti)
                if (colMonthsMinutesToAddOrUpdate.Any(monthMin => monthMin.Col_Monte_Minuti_Id == 0))
                    RepoManager.Col_Monte_MinutiRepo.Add(colMonthsMinutesToAddOrUpdate.Where(monthMin => monthMin.Col_Monte_Minuti_Id == 0));

                // modifico eventuali dati da modificare (se presenti)
                if (colMonthsMinutesToAddOrUpdate.Any(monthMin => monthMin.Col_Monte_Minuti_Id != 0))
                    RepoManager.Col_Monte_MinutiRepo.Update(colMonthsMinutesToAddOrUpdate.Where(monthMin => monthMin.Col_Monte_Minuti_Id != 0));

                // salvataggio delle modifiche effettuate al database
                RepoManager.Col_Monte_MinutiRepo.SaveChanges();
            }
        }

        /// <summary>
        /// Ritorna il totale dei monti minuti salvati per il collaboratore e il periodo specificato.
        /// </summary>
        /// <param name="col">Il collaboratore di cui andare a recuperare il totale minuti accantonato nei monte minuti.</param>
        /// <param name="from">La data di inizio del periodo da processare.</param>
        /// <param name="to">La data di fine del periodo da processare.</param>
        /// <returns>
        /// Il totale dei minuti calcolati dal monte minuti salvato nel database per il collaboratore e il periodo specificato.
        /// </returns>
        /// <exception cref="System.ArgumentException">To date must be major than from date</exception>
        private int GetTotalMinutesFromMonthMinutesAmmounts(Col col, DateTime from, DateTime to)
        {
            // convalida input del meotodo, la data di arrivo deve essere maggiore della data di partenza
            if (to < from)
                throw new ArgumentException("To date must be major than from date");

            if (from.Year >= RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value.Year)
                from = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg.Value;


            int deltaTotale = 0;

            foreach (var data in CommonService.EachMonth(from.AddMonths(1), to))
            {
                Col_Monte_Minuti currentMonthMinutes = RepoManager.Col_Monte_MinutiRepo.FirstOrDefault(monthMin => monthMin.Col_Id == col.Col_Id && monthMin.Anno_Col_Monte_Minuti == data.Year, true);

                string propertyName = MonthPropertyName(new DateTime(data.Year, data.Month, 1));

                var totalMonthMinutes = (int?)CommonService.GetPropertyValue(currentMonthMinutes, propertyName);

                deltaTotale += totalMonthMinutes.HasValue ? totalMonthMinutes.Value : 0;

            }

            return deltaTotale;
        }

        /// <summary>
        /// Ritorna il nome della proprietà che contiene i minuti del mese specificato per la tabella Col_Monte_Minuti
        /// </summary>
        /// <param name="firstMonthDate">La data contenente il mese da processare.</param>
        /// <returns>Il nome della proprietà che contiene i minuti del mese specificato per la tabella Col_Monte_Minuti</returns>
        private static string MonthPropertyName(DateTime firstMonthDate)
        {
            return String.Format("M{0}_Col_Monte_Minuti", firstMonthDate.Month.ToString("00")); ;
        }

        #endregion

        #region Abbinamento Attività alle Registrazioni

        /// <summary>
        /// Abbina le attività alle registrazioni ricevendo solo le regv relative alle ore (esclusi viaggi, passaggi, attività).
        /// </summary>
        /// <param name="regvs">Le regv da elaborare.</param>
        /// <returns>Il dizionario con gli errori del processo</returns>
        public List<KeyValuePair<String, String>> ElaborateActivities(IEnumerable<Reg_V> regvs)
        {

            List<KeyValuePair<String, String>> errors = new List<KeyValuePair<String, String>>();
            List<int> listRegs = new List<int>();

            // se non ci sono reg da processare allora si esce senza effettuare nessuna operazione
            if (regvs.Count() > 0)
            {

                DateTime? from = regvs.Min(r => r.Data_Ora_Fis_E);
                DateTime? to = regvs.Max(r => r.Data_Ora_Fis_U);

                //Nel caso in cui l'ultima registrazione sia un passaggio (ha solo RegE e non RegU), il massimo delle regE sarà maggiore del massimo delle regU, quindi prendo il massimo delle regE come data termine
                if (to.HasValue)
                {
                    if (regvs.Max(r => r.Data_Ora_Fis_E) > to.Value)
                    {
                        to = regvs.Max(r => r.Data_Ora_Fis_E);
                        to = to.Value.AddDays(1);
                        to = to.Value.Date;
                    }
                }

                //Se non ho nessuna regU vuol dire che tutte le registrazioni sono passaggi, allora la data termine sarà la maggiore delle regE
                else
                {
                    to = regvs.Max(r => r.Data_Ora_Fis_E);
                    to = to.Value.AddDays(1);
                    to = to.Value.Date;
                }

                //Hashset per ottimizzare il costo di "coldIs.Contains(reg.Col_Id)"
                HashSet<int?> coldIs = new HashSet<int?>(regvs.Select(reg => reg.Col_Id).Distinct());

                //Removing associated Activities
                List<Reg> atts = RepoManager.RegRepo.Find(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att && reg.Registrazione_Data_Ora_Fis_Reg >= from &&
                    reg.Registrazione_Data_Ora_Fis_Reg <= to && reg.Col_Id.HasValue && coldIs.Contains(reg.Col_Id) &&
                    !reg.Registrazione_Bloccata, true).ToList();

                _log.Info(String.Format("Trovate {0} attività", atts.Count));

                List<Reg> toUpdateAtts = new List<Reg>();

                if (atts.Count > 0)
                {
                    var attByColDic = new Dictionary<int, List<Reg>>();

                    atts.ForEach(att =>
                    {
                        att.Att_Id = null;
                        att.RiferimentoRRN_Att = null;
                        att.Registrazione_Stato_Reg = (int)RegStateEnum.None;

                        if (!attByColDic.ContainsKey(att.Col_Id.Value))
                            attByColDic.Add(att.Col_Id.Value, new List<Reg>());

                        attByColDic[att.Col_Id.Value].Add(att);
                    });

                    var regvsByCol = regvs.GroupBy(r => r.Col_Id).ToList();

                    double colsTotal = regvsByCol.Count();
                    double colCount = 1;
                    double percRec = 0;

                    foreach (var currentRegvByCol in regvsByCol)
                    {
                        percRec = (colCount / colsTotal) * 100;
                        colCount++;

                        var regvsByColByDate = currentRegvByCol.GroupBy(r => r.Data_Ora_Fis_E).ToList();

                        foreach (var currentRegByColByDate in regvsByColByDate)
                        {
                            var orderedRegsByColByDate = currentRegByColByDate.OrderBy(r => r.Data_Ora_Fis_E).ToList();
                            foreach (var currentRegv in orderedRegsByColByDate)
                            {
                                // elaboro solo le attività per le regv che hanno entrata e uscita
                                if (currentRegv.Data_Ora_Fis_U != null)
                                {
                                    currentRegv.Data_Ora_Fis_U = AdjustMinutes(currentRegv.Data_Ora_Fis_U.Value);

                                    if (attByColDic.ContainsKey(currentRegv.Col_Id.Value))
                                    {
                                        List<Reg> associatedAtt = attByColDic[currentRegv.Col_Id.Value].Where(r => r.Col_Id == currentRegv.Col_Id && r.Registrazione_Data_Ora_Fis_Reg >= currentRegv.Data_Ora_Fis_E && r.Registrazione_Data_Ora_Fis_Reg <= currentRegv.Data_Ora_Fis_U).ToList();

                                        associatedAtt.ForEach(reg =>
                                        {
                                            reg.Att_Id = currentRegv.Cant_Id;
                                            reg.RiferimentoRRN_Att = currentRegv.RegE;
                                            reg.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                                        });
                                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowActivitiesInRegV) == 1) {
                                            if (currentRegv.Registrazione_Tipo_Reg != 4) {
                                                listRegs.Add(currentRegv.RegE);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    RepoManager.RegRepo.Context.BulkUpdate(atts);
                }
                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ShowActivitiesInRegV) == 1) {
                    List<Reg> updateRegs = new List<Reg>();
                    foreach (int id in listRegs) {
                        try {
                            Reg regE = RepoManager.RegRepo.Single(r => r.Reg_Id == id);
                            Reg attReg = RepoManager.RegRepo.Single(r => r.RiferimentoRRN_Att == id);
                            Cant att = RepoManager.CantRepo.Single(c => c.Cant_Id == attReg.Cant_Id);
                            regE.Att_Id = att.Cant_Id;
                            regE.Sotto_Cantiere = att.Note_Can;
                            updateRegs.Add(regE);
                        } catch (Exception e) { 
                        }
                    }
                    RepoManager.RegRepo.Context.BulkUpdate(updateRegs);
                }
            }
            return errors;
        }

        private DateTime AdjustMinutes(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 59);
        }

        #endregion

        #region Elaborazione Viaggi

        public List<KeyValuePair<String, String>> ElaborateTrips(IEnumerable<Reg_V> regvsList, Boolean isToSaveErrorMessage = false, bool isToSaveChanges = true,
            int? elaborateUserId = null, DateTime? elaborateDateTime = null, ApplicationMessageEnum? application = null)
        {
            var errors = new List<KeyValuePair<String, String>>();

            // si elaborano i viaggi solamente se sono tra i moduli abilitati
            if (RepoManager.ParamRepo.ParametersRow.Abilita_Viaggi)
            {
                BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(0, "Inizio elaborazione Viaggi");


                // se sono state passate delle regv allora si tolgono dall'elaborazione tutte quelle che corrispondono a delle rettifiche, che sono delle
                // semplici durate o che sono degli arrotondamenti per durata
                if (regvsList.Any())
                    regvsList = regvsList.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass).ToList();

                // inizializzazione della definizione dell'applicativo per la scrittura dei messaggi nell'apposita tabella
                var currentApplication = application ?? ApplicationMessageEnum.ElaborateTrips;

                // inizializzazione dell'utente e della data/ora di avvio dell'elaborazione
                _elaborateUserId = elaborateUserId ?? PowerWebContext.Current.User.Utenti_Id;
                _elaborateDateTime = elaborateDateTime ?? DateTime.Now;

                regvsList = regvsList.Where(regv => regv.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att).ToList();



                //Serve SOLO per Cancellare le eventuali REG Viaggi Esistente tra la Data (senza Ora) di Inizio e la Data (senza Ora) di Fine 
                //ricavate dalla Lista di REG Ricevute
                DateTime? from = null;
                DateTime? to = null;

                if (regvsList.Any())
                {
                    from = regvsList.Min(r => r.Data_Ora_Fis_E);
                    //se l'ultima timbratura di giornata è un passaggio (non ha RegU), prendo l'entrata.
                    to = regvsList.Max(r => r.Data_Ora_Fis_U) ?? regvsList.Max(r => r.Data_Ora_Fis_E);
                }

                var colIds = regvsList.Select(regv => regv.Col_Id).Distinct().ToList();

                //se i campi from e to sono valorizzati
                if (from.HasValue && to.HasValue)
                {

                    //Identifica le Registrazioni Viaggi esistenti nella VL REGV per Cancellarle prima di ricrearle
                    var regvTrips = regvsList.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip).ToList();

                    //se vi sono dei viaggi
                    if (regvTrips.Count > 0)
                    //nel caso in cui c'erano Registrazioni Viaggi da Cancellare Le Cancella
                    {
                        try
                        {
                            var toDelete = RepoManager.RegRepo.DbSet.Where(r => r.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip && (r.Registrazione_Data_Ora_Fis_Reg >= from && r.Registrazione_Data_Ora_Fis_Reg <= to)).ToList();
                            RepoManager.RegRepo.Delete(toDelete, true);
                        }
                        catch (Exception ex)
                        {
                            _log.ErrorFormat("Errori durante la cancellazione dei viaggi : {0}", ex.Message);
                        }
                        //procede a cancellare tutte le precedenti Registrazioni di VIAGGI che rientrano nei limiti di data ricevuti

                        //la nuova lista di registrazioni da procesare non contiene i viaggi
                        regvsList = regvsList.Where(reg => reg.Registrazione_Tipo_Reg != (int)RegTypeEnum.Trip).ToList();
                    }

                    //verifico se ho ricevuto delle REG NON ABBINATE
                    var oddRegvs = regvsList.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.None && regv.Registrazione_Stato_Reg == (int)RegStateEnum.None).ToList();



                    if (oddRegvs.Count() > 0)
                        errors.Add(new KeyValuePair<String, String>(FunctionMessageEnum.ElaborateTrips.ToString(), BusinessService.GetLocalizedString(PowerWebResources.ERR_PRESENZA_TIMBRATURE_DISPARI)));
                    else
                    {


                        //Estrae SOLO le REG che hanno l'Ora di Fine (Associate) + le RegV dei Passaggi                    
                        var regvs = regvsList.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass || (regv.RegU != null && (regv.Registrazione_Stato_Reg & (int)RegStateEnum.Ass) == (int)RegStateEnum.Ass)).ToList();
                        //recupero da Param i Flag_Ore_Viaggi (se vale 0 NON devo trattare i Viaggi)
                        int paramTripHours = RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi.HasValue ? RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi.Value : 0; // 0 means skip checks
                                                                                                                                                                           //recupero da Param la Durata_Pausa
                        TimeSpan paramPauseTime = RepoManager.ParamRepo.ParametersRow.Durata_Pausa.HasValue ? RepoManager.ParamRepo.ParametersRow.Durata_Pausa.Value : TimeSpan.Zero;
                        //Crea la Lista vuota in cui scrivere le Nuove Registrazioni dei Viaggi
                        var trips = new List<Reg>();

                        //Tratto SOLO le REG SENZA MOTIVAZIONE (NO DEVE TRATTARE ANCHE I VIAGGI FRA REG CON/SENZA MOTIAVZIONE)
                        //var tripsWithoutMot = regvs.Where(regv => regv.Motivazione_Reg_Id == null);                                       

                        #region Raggruppamento delle REG e Generazione dei Viaggi fra le Registrazioni per Collaboratore

                        var tripsByCol = regvs.Where(reg => reg.Col_Id != null && reg.Col_Id != 0).GroupBy(reg => reg.Col_Id).ToList();



                        double colsTotal = tripsByCol.Count();
                        double colCount = 1;
                        double percRec = 0;


                        //Tratta le Regv per Collaboratore
                        foreach (var currentTripByCol in tripsByCol)
                        {
                            percRec = (colCount / colsTotal) * 100;
                            BusinessService.ElaborateStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec, String.Format("Elaboro viaggi del collaboratore {0} di {1}", colCount, colsTotal));
                            BusinessService.ImportDataStatusDictionary[PowerWebContext.Current.User] = new KeyValuePair<double, string>(percRec, String.Format("Elaboro viaggi del collaboratore {0} di {1}", colCount, colsTotal));
                            colCount++;

                            //identifica il primo Collaboratore da Trattare
                            var firstRegVByCol = currentTripByCol.First();

                            Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == firstRegVByCol.Col_Id, false);

                            //recupera dal Collaboratore l'eventuale Durata Pausa (se NON è NULL) altrimenti lascia quella dei Parametri
                            TimeSpan currentColPauseTime = currentCol.Durata_Pausa_Col.HasValue ? currentCol.Durata_Pausa_Col.Value : paramPauseTime; // overriding paramPauseTime                    

                            //Raggruppa le REG x Col/Data (Data senza tenere conto del NOTTURNO)
                            var tripsByColByDate = currentTripByCol.GroupBy(reg => reg.Data_Ora_Fis_E.Date).ToList();

                            //Tratta le Regv per Collaboratore/Data_Ora Entrata
                            foreach (var currentTripsByColByDate in tripsByColByDate)
                            {
                                 if (paramTripHours != (int)FlagTripHoursParamEnum.None)
                                //nel caso in cui il Flag della tab PARAM abiliti i Viaggi (<> 0)
                                {
                                    //Ordina le Regv per Collaboratore/Data-Ora Entrata                                                                                              
                                    var orderedCurrentTripsByColByDate = currentTripsByColByDate.OrderBy(regv => regv.Data_Ora_Fis_E);

                                    if (paramTripHours == (int)FlagTripHoursParamEnum.AllExceptTwo)
                                    //Nel caso in cui in Scheda PARAM sia abilitata la gestione per Tutti Eccetto i Col Esclusi
                                    {
                                        if (currentCol.Flag_Ore_Viaggi_Col != (int)FlagTripHoursColEnum.AllExceptTwo)
                                            //Tratta solo i Collaboratore che NON sono da Escludere
                                            trips.AddRange(ElaborateInternalTrips(orderedCurrentTripsByColByDate, currentCol, currentColPauseTime, errors,
                                                _elaborateUserId, _elaborateDateTime, currentApplication));
                                    }
                                    else if (paramTripHours == (int)FlagTripHoursParamEnum.OnlyOne)
                                    //Nel caso in cui in Scheda PARAM sia abilitata la gestione per SOLO i Collaboratori ABILITATI
                                    {
                                        if (currentCol.Flag_Ore_Viaggi_Col == (int)FlagTripHoursColEnum.OnlyOne)
                                            //Tratta solo i Collaboratore che sono ABILITATI
                                            trips.AddRange(ElaborateInternalTrips(orderedCurrentTripsByColByDate, currentCol, currentColPauseTime, errors,
                                                _elaborateUserId, _elaborateDateTime, currentApplication));
                                    }
                                }
                            }
                        }

                        #endregion


                        if (isToSaveErrorMessage)
                            RepoManager.Tab_MessaggiRepo.InsertMessages(errors, currentApplication, FunctionMessageEnum.ElaborateTrips, _elaborateUserId, _elaborateDateTime);

                        //aggiungo i vaiggi  e se savehanges è true gli inserisco nel db
                        RepoManager.RegRepo.Context.BulkInsert(trips);

                        //se vi sono viaggi
                        if (trips.Count > 0)
                        {
                            //determino l'intervallo da...a in cui vengono calcolati i viaggi
                            from = trips.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg);
                            to = trips.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg);

                            //vengono estratti tutti gli utlimi viaggi (capisco che sono gli ultimi viaggi inseriti nel db perchè RiferimentoRRN_Att è diverso da NULL )                            
                            var addedTrips = RepoManager.RegRepo.Find(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip && reg.Registrazione_Data_Ora_Fis_Reg >= from && reg.Registrazione_Data_Ora_Fis_Reg <= to && reg.RiferimentoRRN_Att != null, true).ToList();

                            //raggruppo i viaggi per riferimento dell'attività(viene usato per poter lavorare con le stored procedure)
                            var tripsByRef = addedTrips.GroupBy(reg => reg.RiferimentoRRN_Att);

                            //ciclo su tutti i viaggi estratti
                            foreach (var trip in tripsByRef)
                            {
                                //il viaggio viene orinato per data ora(è una RegV cioè una coppia di Reg)
                                var tripByFis = trip.OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();
                                int? lastTripId = null;

                                //per ogni valore della RegV , cioè per ogni reg si vanno a mettere a NULL i riferimenti verso le attività,
                                //mentre la RegE del viaggio a riferimento NULL, la RegU del viaggio ha riferimento alla RegE
                                tripByFis.ForEach(reg =>
                                {
                                    reg.RiferimentoRRN_Reg = lastTripId;
                                    reg.RiferimentoRRN_Att = null;
                                    lastTripId = reg.Reg_Id;
                                });
                            }

                            //viene fatto l'aggiornamento dei viaggi già inseriti nel database ma in cui sono stati modificati i riferimenti
                            RepoManager.RegRepo.Update(addedTrips, true);
                        }

                        #endregion
                    }
                }
            }

            return errors;
        }


        #region Generazione Viaggi

        private List<Reg> ElaborateInternalTrips(IOrderedEnumerable<Reg_V> orderedCurrentTripsByMotByColByDate, Col currentCol, TimeSpan paramPauseTime, List<KeyValuePair<String, String>> errors, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application)
        {
            //Ricava da PARAM l'eventuale Codice Cantiere a cui intestare i Viaggi (se caricato)
            int? dummyCantId = RepoManager.ParamRepo.ParametersRow.Cant_Id;
            //int? dummyFruId = RepoManager.ParamRepo.ParametersRow.Fru_Id;
            //Ricava da PARAM l'eventuale DURATA MIN E MAX dei Viaggi ( se caricata)
            TimeSpan paramMaxTripTime = RepoManager.ParamRepo.ParametersRow.Durata_Massima_Viaggio.HasValue ? RepoManager.ParamRepo.ParametersRow.Durata_Massima_Viaggio.Value : TimeSpan.Zero;
            TimeSpan paramMinTripTime = RepoManager.ParamRepo.ParametersRow.Durata_Minima_Viaggio.HasValue ? RepoManager.ParamRepo.ParametersRow.Durata_Minima_Viaggio.Value : TimeSpan.Zero;

            Reg_V lastRegV = null;
            List<Trip> tripList = new List<Trip>();

            // inizializzazione dell'indice di posizionamento del ciclo che aiuta a recuperare
            // la registrazione successiva a quella in elaborazione
            int position = 0;

            //viene estrato il valore dalla scheda parametri se è permesso il viaggio nello stesso cantiere
            int paramTripType = RepoManager.ParamRepo.ParametersRow.Tipo_Viaggio;

            //Recupera da PARAM il Flag_Ore_Viaggi di Inizio/Fine Giornata per decidere se deve o meno gestire anche i Viaggi da Casa al 1° Cantiere e dall'Ultimo Cantiere a Casa
            var paramTripHours = RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi_Inizio_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Flag_Ore_Viaggi_Inizio_Fine.Value : 0; // 0 means skip checks

            //Recupera il parametro della generazione di viaggi di inizio/fine giornata dal collaboratore o dai parametri. Se il parametro è = 3, genera i viaggi di inizio/fine giornata SOLO kilometrici dal/al cantiere SEDE. Se la prima/ultima regv è sul cantiere SEDE, il viaggio sarà comunque solo kilometrico.
            int startEndParam = currentCol.Flag_Viaggio_InizioFine_GIS.HasValue ? currentCol.Flag_Viaggio_InizioFine_GIS.Value : RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G;

            //Tratta le Singole REGV del Collaboratore ricevuto in Ordine di Data/Ora
            foreach (var currentRegV in orderedCurrentTripsByMotByColByDate)
            {
                if (lastRegV != null)
                {
                    //Se sono attivi i viaggi di inizio/fine giornata e vengono generati dalla sede, evito di generare il primo e l'ultimo viaggio dalla/per la sede
                    if (paramTripHours != (int)FlagTripHoursParamEnum.None && startEndParam == (int)TripAssignmentTypeEnum.CalculateFromHeadquarter)
                    {

                        Cant prevCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == lastRegV.Cant_Id);
                        //Se questa è la seconda regv
                        if (position == 1)
                        {
                            //Se la regv precedente (la prima) è stata fatta nel cantiere SEDE e il cantiere corrente non è la sede
                            if (prevCant.Tipo_Cantiere_Can == "SEDE" && currentRegV.Cant_Id != prevCant.Cant_Id)
                            {
                                //Aggiorno le variabili e vado al prossimo passo del for
                                position++;
                                lastRegV = currentRegV;
                                continue;
                            }
                        }

                        //Se questa è l'ultima regv
                        if (position == orderedCurrentTripsByMotByColByDate.Count() - 1)
                        {
                            Cant currCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == currentRegV.Cant_Id);
                            //Se la regv precedente (la prima) è stata fatta nel cantiere SEDE
                            if (currCant.Tipo_Cantiere_Can == "SEDE" && prevCant.Cant_Id != currCant.Cant_Id)
                            {
                                //Aggiorno le variabili e vado al prossimo passo del for
                                position++;
                                lastRegV = currentRegV;
                                continue;
                            }
                        }
                    }

                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NotCalcolatePausaTrip) == 1)
                    {
                        if (lastRegV.Cant_Desc == "PAUSAPRANZO" || currentRegV.Cant_Desc == "PAUSAPRANZO" || (lastRegV.Motivazione_Reg_Cod == "Pausa" || currentRegV.Motivazione_Reg_Cod == "Pausa"))
                        {
                            //Aggiorno le variabili e vado al prossimo passo del for
                            position++;
                            lastRegV = currentRegV;
                            continue;
                        }
                    }

                    //ottengo true se il cantiere di arrivo è diverso da quello di partenza
                    bool differentCant = (lastRegV.Cant_Id != currentRegV.Cant_Id);

                    #region 1.CALCOLO VIAGGI TENENDO PRESENTE SE CALCOLARE VIAGGI SULLO STESSO CANTIERE

                    //se i cantieri sono differenti e non ho attivo il parametro ->CALCOLO VIAGGIO
                    //se i cantieri sono UGUALI e NON ho attivo il parametro ->NO CALCOLO VIAGGIO
                    //se i cantieri sono UGUALI ed E' attivo il parametro ->CALCOLO VAGGIO

                    // calcolo del cantiere della reg precedente e successiva al fine di gestire le ore non lavorate
                    var currentCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == currentRegV.Cant_Id);
                    var lastCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == lastRegV.Cant_Id);
                    if (currentCant.Tipo_Calcolo_Viaggi_Can != "0" && lastCant.Tipo_Calcolo_Viaggi_Can != "0") {
                        if (differentCant || paramTripType == (int)FlagTripTypeEnum.SameCant)
                        //Crea un Viaggio nel caso in cui il Cantiere sia Cambiato oppure se sono previsti anche i Viaggi fra Cantieri Uguali 
                        {
                            //Inizializza i Valori di Default delle 2 nuove Registrazioni (Entrata + Uscita) che deve creare
                            Reg newRegE = RepoManager.RegRepo.Init();
                            Reg newRegU = RepoManager.RegRepo.Init();

                            // calcolo del cantiere della reg precedente e successiva al fine di gestire le ore non lavorate
                            //var currentCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == currentRegV.Cant_Id);
                            //var lastCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == lastRegV.Cant_Id);

                            // calcolo della partenza e/o arrivo da ONL
                            bool isFromOnl = lastCant.Tipo_Cantiere_Can == "ONL";
                            bool isToOnl = currentCant.Tipo_Cantiere_Can == "ONL";

                            //Intesta le nuove Registrazioni al Cantiere (di Default da Param o di Fine Viaggio).
                            // se il cantiere viene preso dalla param si recupera quel cantiere;
                            // in caso contrario si utilizza il cantiere standard solamente se non si ha una destinazione un cantiere con tipo
                            // ONL (ore non lavorate); in questo caso la destinazione il cantiere della registrazione successiva a quella in processo
                            if (dummyCantId != null) // recupero del cantiere dai parametri
                            {
                                var nextRegV = (position + 1) < orderedCurrentTripsByMotByColByDate.Count() ? orderedCurrentTripsByMotByColByDate.ElementAt(position + 1) : null;
                                if (currentRegV == null)
                                {
                                    newRegE.Cant_Id = dummyCantId.Value;
                                }
                                else if (nextRegV == null)
                                {
                                    newRegU.Cant_Id = dummyCantId.Value;
                                }
                                else
                                {
                                    newRegE.Cant_Id = currentRegV.Cant_Id;
                                    newRegU.Cant_Id = nextRegV.Cant_Id;
                                }

                            }
                            else // recupero del cantiere dalla destinazione
                            {
                                // si imposta il cantiere di destinazione con il cantiere di reg_v solamente se il cantiere di destnazione non è
                                // di tipo ONL (ore non lavorate)
                                if (!isToOnl)
                                {
                                    var nextRegV = (position + 1) < orderedCurrentTripsByMotByColByDate.Count() ? orderedCurrentTripsByMotByColByDate.ElementAt(position + 1) : null;
                                    //Il cantiere di destinazione della nuova reg è uguale al cantiere della registrazione corrente
                                    if (nextRegV != null)
                                    {
                                        newRegE.Cant_Id = currentRegV.Cant_Id;
                                        newRegU.Cant_Id = nextRegV.Cant_Id;
                                    }
                                    else
                                        newRegE.Cant_Id = newRegU.Cant_Id = currentRegV.Cant_Id;
                                }
                                else
                                {
                                    // Se il cantiere è ONL e quindi il cantiere di destinazione del viaggio deve essere quello
                                    // della regV successiva
                                    var nextRegV = (position + 1) < orderedCurrentTripsByMotByColByDate.Count() ? orderedCurrentTripsByMotByColByDate.ElementAt(position + 1) : null;

                                    // se esiste una regv successiva utilizzo quel cantiere altrimenti procedo con lo standard
                                    if (nextRegV != null)
                                        newRegE.Cant_Id = newRegU.Cant_Id = nextRegV.Cant_Id;
                                    else
                                        newRegE.Cant_Id = newRegU.Cant_Id = currentRegV.Cant_Id;
                                }
                            }

                            // viene calcolato il cantiere di partenza utilizzato per la generazione del viaggio
                            // se il cantiere di partenza è di tipo ore non lavorate allora viene impostato come cantiere di partenza
                            // il cantiere della regV precedente a quella di partenza; altrimenti viene impostato il cantiere dell'ultima regV
                            int? fromCantId = null;
                            //se il cantiere di partenza è ONL
                            if (isFromOnl)
                            {
                                // recupero della regV precedente
                                var previousRegV = (position - 2) >= 0 ? orderedCurrentTripsByMotByColByDate.ElementAt(position - 2) : null;

                                // se la regv precedente è presente allora si utilizza quel cantiere
                                if (previousRegV != null)
                                    fromCantId = previousRegV.Cant_Id;
                                else // altrimenti si utilizza comunque quello dell'ultima regV
                                    fromCantId = lastRegV.Cant_Id;
                            }
                            else
                                //se il cantiere di partenza non è ONL allora uso il cantiere della registrazione precedentre
                                fromCantId = lastRegV.Cant_Id;

                            //viene calcolata la durata del viaggio come differenza tra l'ora di entrata della destinazione successiva con l'ora di uscita della destinazione precedente
                            DateTime start = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? lastRegV.Data_Ora_Fig_U.Value : lastRegV.Data_Ora_Fis_E;
                            var duration = currentRegV.Data_Ora_Fig_E.Value.Subtract(start);

                            //viene calcolata l'ora fisica di inizio viaggio come l'ora di uscita della registrazione precedente (A)
                            var tripEFisDateTime = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? new DateTime(lastRegV.Data_Ora_Fis_U.Value.Year, lastRegV.Data_Ora_Fis_U.Value.Month, lastRegV.Data_Ora_Fis_U.Value.Day, lastRegV.Data_Ora_Fis_U.Value.Hour, lastRegV.Data_Ora_Fis_U.Value.Minute, 59) : new DateTime(lastRegV.Data_Ora_Fis_E.Year, lastRegV.Data_Ora_Fis_E.Month, lastRegV.Data_Ora_Fis_E.Day, lastRegV.Data_Ora_Fis_E.Hour, lastRegV.Data_Ora_Fis_E.Minute, 59);
                            //l'ora figurativa di inizio viaggio è calcolata dall 'ora figurativa della ragistrazione precedente
                            var tripEFigDateTime = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? lastRegV.Data_Ora_Fis_U : lastRegV.Data_Ora_Fis_E; //DA VERIFICARE
                                                                                                                                                                 //var tripEFigDateTime =lastRegV.Data_Ora_Fig_U;

                            //se il viaggio ha un'ora valida di inizio
                            //viene calcolata l'ora di entrata figurativa prendendola dalle ore figurative
                            if (tripEFigDateTime.HasValue)
                                tripEFigDateTime = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? new DateTime(lastRegV.Data_Ora_Fis_U.Value.Year, lastRegV.Data_Ora_Fis_U.Value.Month, lastRegV.Data_Ora_Fis_U.Value.Day, lastRegV.Data_Ora_Fig_U.Value.Hour, lastRegV.Data_Ora_Fig_U.Value.Minute, 59) : new DateTime(lastRegV.Data_Ora_Fis_E.Year, lastRegV.Data_Ora_Fis_E.Month, lastRegV.Data_Ora_Fis_E.Day, lastRegV.Data_Ora_Fig_E.Value.Hour, lastRegV.Data_Ora_Fig_E.Value.Minute, 59);

                            //Carica i Dati della Registrazione di Entrata (Inizio Viaggio) (Prendo i valori della lastReg_v)
                            newRegE.Registrazione_Data_Ora_Orig_Reg = tripEFisDateTime;
                            newRegE.Registrazione_Data_Ora_Fis_Reg = tripEFisDateTime;
                            newRegE.Registrazione_Data_Ora_Fig_Reg = tripEFigDateTime;
                            newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegE.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegE.Col_Id = currentCol.Col_Id;
                            newRegE.Att_Id = newRegE.Cant_Id;

                            //Carica i Dati della Registrazione di Uscita (Fine Viaggio) (Prendo i valori della currentReg_v)
                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TripFigHours) == 1)
                            {
                                newRegU.ParentReg = newRegE;
                                newRegU.Registrazione_Data_Ora_Orig_Reg = currentRegV.Data_Ora_Fis_E;
                                newRegU.Registrazione_Data_Ora_Fis_Reg = currentRegV.Data_Ora_Fis_E;
                                newRegU.Registrazione_Data_Ora_Fig_Reg = currentRegV.Data_Ora_Fig_E;
                                newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                                newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                                newRegU.Col_Id = currentCol.Col_Id;
                                newRegU.Att_Id = newRegU.Cant_Id;
                                newRegU.Cant_Id = currentRegV.Cant_Id;
                            }
                            else
                            {
                                newRegU.ParentReg = newRegE;
                                newRegU.Registrazione_Data_Ora_Orig_Reg = currentRegV.Data_Ora_Fis_E;
                                newRegU.Registrazione_Data_Ora_Fis_Reg = currentRegV.Data_Ora_Fis_E;
                                newRegU.Registrazione_Data_Ora_Fig_Reg = duration <= TimeSpan.Zero ? tripEFigDateTime : currentRegV.Data_Ora_Fis_E;
                                newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                                newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                                newRegU.Col_Id = currentCol.Col_Id;
                                newRegU.Att_Id = newRegU.Cant_Id;
                                newRegU.Cant_Id = currentRegV.Cant_Id;
                            }
                            //per l'uso della stored procedure inserico i riferimenti 
                            newRegE.RiferimentoRRN_Att = lastRegV.RegE;
                            newRegU.RiferimentoRRN_Att = lastRegV.RegE;

                            // se i viaggi sono nello stesso minuto e l'entrata risulta maggiore dell'uscita
                            // allora si tratta di un viaggio con durata zero (utilizzato da alcuni clienti per i km da cantieri ONL, come la pausa);
                            // in quest caso reg e e reg_v vanno invertiti

                            //controllo se i viaggi sono nello stesso minuto
                            if (newRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Hours == newRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Hours
                                && newRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Minutes == newRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Minutes)
                            {

                                if (newRegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Ticks > newRegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay.Ticks)
                                {
                                    Reg tmpReg = newRegE;
                                    newRegE = newRegU;
                                    newRegU = tmpReg;
                                }
                            }

                            // recupero la presenza o meno della customizzazione che mi impone di SALTARE  o FARE il controllo di durata per i viaggi che iniziano
                            // in un cantiere ONL
                            int customizationEnum = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoTripsMaxMinDurationControlONLCantEnum);

                            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TripOnlyGpsReg) == 0 || (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TripOnlyGpsReg) == 1 && (currentRegV.Registrazione_Lat_Orig_E != null && currentRegV.Registrazione_Long_Orig_E != null && (lastRegV.Registrazione_Lat_Orig_E != null && lastRegV.Registrazione_Long_Orig_E != null)) || (currentRegV.Registrazione_Lat_Orig_U != null && currentRegV.Registrazione_Long_Orig_U != null && lastRegV.Registrazione_Lat_Orig_U != null && lastRegV.Registrazione_Long_Orig_U != null))) 
                            {
                                //se nella personalizzazione si è scelto di saltare il controllo sulla durata max e minima del viaggio 
                                if (customizationEnum == (int)NoTripsMaxMinDurationControlONLCantEnum.SkipControl || isFromOnl)
                                {
                                    //Carica le 2 nuove registrazioni del Viaggio con la Durata Calcolata
                                    tripList.Add(new Trip
                                    {
                                        RegE = newRegE,
                                        RegU = newRegU,
                                        cantIdStart = fromCantId,
                                        IsFromOnl = isFromOnl,
                                        IsToOnl = isToOnl
                                    });
                                }

                                //Controllo della personalizzazione che controlla la durata per viaggi che iniziano su un cantiere ONL
                                else if (customizationEnum == (int)NoTripsMaxMinDurationControlONLCantEnum.DoControl)
                                {
                                    //viene controllato se i valori della massima durata e minima sono valorizzati
                                    if ((paramMaxTripTime != TimeSpan.Zero || paramMinTripTime != TimeSpan.Zero))
                                    {
                                        //viene calcolata la durata del viaggio tra cantiere ONL(A) e cantiere normale(B)
                                        DateTime partenza = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? lastRegV.Data_Ora_Fig_U.Value : lastRegV.Data_Ora_Fis_E;
                                        TimeSpan tripDuration = currentRegV.Data_Ora_Fis_E.Subtract(partenza);

                                        //se la durata rientra nel range tra durata minima e massima allora vengono create le registrazioni di viaggio
                                        if (tripDuration >= paramMinTripTime && (tripDuration <= paramMaxTripTime || paramMaxTripTime == TimeSpan.Zero))
                                        {
                                            //Carica le 2 nuove registrazioni del Viaggio con la Durata Calcolata
                                            tripList.Add(new Trip
                                            {
                                                RegE = newRegE,
                                                RegU = newRegU,
                                                cantIdStart = fromCantId,
                                                IsFromOnl = isFromOnl,
                                                IsToOnl = isToOnl
                                            });
                                        }
                                    }
                                }
                                //se è attiva la personalizzazione che va a troncare il viaggio al valore di durata massima viaggi
                                else if (customizationEnum == (int)NoTripsMaxMinDurationControlONLCantEnum.TruncateToMax)
                                {
                                    //viene calcolata la durata del viaggio
                                    DateTime partenza = lastRegV.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? lastRegV.Data_Ora_Fig_U.Value : lastRegV.Data_Ora_Fis_E;
                                    TimeSpan tripDuration = currentRegV.Data_Ora_Fis_E.Subtract(partenza);

                                    //se la durata del viaggio è minore della durata massimo
                                    if (tripDuration <= paramMaxTripTime)
                                    {
                                        //Carica le 2 nuove registrazioni del Viaggio con la Durata Calcolata
                                        tripList.Add(new Trip
                                        {
                                            RegE = newRegE,
                                            RegU = newRegU,
                                            cantIdStart = fromCantId,
                                            IsFromOnl = isFromOnl,
                                            IsToOnl = isToOnl
                                        });
                                    }
                                    else
                                    {
                                        //se ladurata del viaggio supera la durata massima allora come durata del viaggio viene impostata la durata massima
                                        newRegU.Registrazione_Data_Ora_Fis_Reg = newRegE.Registrazione_Data_Ora_Fis_Reg.Add(paramMaxTripTime);

                                        //Carica le 2 nuove registrazioni del Viaggio con la Durata Calcolata
                                        tripList.Add(new Trip
                                        {
                                            RegE = newRegE,
                                            RegU = newRegU,
                                            cantIdStart = fromCantId,
                                            IsFromOnl = isFromOnl,
                                            IsToOnl = isToOnl
                                        });
                                    }
                                }
                            } 
                        }
                    }
                    
                    #endregion
                }
                //la registrazione precedente è uguale alla successiva del caso prima (cioè passo alla registrazione successiva)
                lastRegV = currentRegV;

                // incremento dell'indice di elaborazione che serve a recuperare la registrazione successiva a quella esistente
                position++;
            }


            #region Gestione Pausa da VIAGGI

            //se vi sono viaggi all'interno della lista E il tipo di detrazione pausa è impostato DA VIAGGIO (1) E la pausa ha un valore
            if (tripList.Count > 0 && /*RepoManager.ParamRepo.ParametersRow.Detrazione_Pausa == 1 &&*/ paramPauseTime != TimeSpan.Zero)
            {
                //flag utilizzato per rilevare la presenza di viaggi sullo stesso cantiere (utilizzato in congiunzione alla customization PauseDeductionOnlyFascia5) (CG2 fa schifo)
                bool isPresentTripsSameCant = false;

                //vengono ordinati i viaggi per durata
                tripList = tripList.OrderByDescending(trip => trip.TripDuration).ToList();
                var pauseTrips = tripList;

                //se è attiva la customization che detrae la pausa solo dai viaggi fatti nella 5^ fascia, estrapola solo i viaggi che ricadono in 5^ fascia
                int customizationEnum = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PauseDeductionOnlyFascia5);
                if (customizationEnum == (int)PauseDeductionOnlyFascia5.Enabled)
                {

                    // Recupera la fascia 5 prima dai parametri o, se presente, dal collaboratore
                    TimeSpan paramTripRange5Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Inizio.Value : TimeSpan.Zero;
                    TimeSpan paramTripRange5End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Fine.Value : TimeSpan.Zero;
                    TimeSpan colTripRange5Start = currentCol.Fascia_Ore_Viaggi_5_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_5_Inizio_Col.Value : paramTripRange5Start;
                    TimeSpan colTripRange5End = currentCol.Fascia_Ore_Viaggi_5_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_5_Fine_Col.Value : paramTripRange5End;

                    //Controllo che almeno una fascia sia valorizzata e che la fine sia maggiore dell'inizio
                    if ((colTripRange5Start != TimeSpan.Zero || colTripRange5End != TimeSpan.Zero) && colTripRange5End > colTripRange5Start)
                    {
                        //Tiene solo i viaggi con partenza ricadente all'interno della 5^ fascia
                        pauseTrips = tripList.Where(trip => (IsStartTripInRange(colTripRange5Start, colTripRange5End, trip))).ToList();

                        //Se, tra i viaggi filtrati per fascia 5, ci sono viaggi sullo stesso cantiere, non serve detrarre la pausa da altri viaggi
                        //lo si segnala tramite l'apposito flag
                        if (pauseTrips.Any(trip => trip.cantIdStart == trip.RegU.Cant_Id))
                        {
                            isPresentTripsSameCant = true;
                        }
                    }
                }
                //istanzio un nuo hashSet(Rappresenta un insieme di valori di tipo viaggio SENZA DUPLICATI)
                HashSet<Trip> toRemove = new HashSet<Trip>();

                //Se è attivo il flag di viaggi sullo stesso cantiere (ideato per CG2 merda), elimino il viaggio sullo stesso cantiere più lungo
                if (isPresentTripsSameCant)
                {
                    //Usa una First e non una FirstOrDefault perché il flag è a true solo se sono presenti viaggi sullo stesso cantiere
                    toRemove.Add(pauseTrips.First(trip => trip.cantIdStart == trip.RegU.Cant_Id));
                }

                //Altrimenti detrae la pausa dai viaggi più lunghi
                else
                {
                    //si vanno a scorrere tutti i viaggi della lista
                    for (int i = 0; i < pauseTrips.Count && paramPauseTime != TimeSpan.Zero; i++)
                    {
                        //viene estratto il viaggio corrispondente all'indice del ciclo
                        Trip currTrip = pauseTrips[i];

                        //viene estratta la durata del viaggio
                        TimeSpan tripDurationMins = currTrip.TripDuration;

                        //se la durata del viaggio è maggiore della pausa
                        if (tripDurationMins > paramPauseTime)
                        {
                            //determino la differenza tra durata del viaggio e pausa
                            TimeSpan diff = tripDurationMins.Subtract(paramPauseTime);

                            //come ora finale del viaggio viene impostata l'ora di enttrata + la differenza tra durata totale del viaggio e pausa
                            currTrip.RegU.Registrazione_Data_Ora_Fig_Reg = currTrip.RegE.Registrazione_Data_Ora_Fig_Reg.Value.Add(diff);
                            currTrip.RegU.Registrazione_Data_Ora_Fis_Reg = currTrip.RegE.Registrazione_Data_Ora_Fis_Reg.Add(diff);

                            //viene impostata a zero la pausa
                            paramPauseTime = TimeSpan.Zero;
                        }
                        //se la durata del viaggio è uguale alla pausa
                        else if (tripDurationMins == paramPauseTime)
                        {
                            //viene azzerta la pausa
                            paramPauseTime = TimeSpan.Zero;

                            //aggiungo il viaggio da rimuovere
                            toRemove.Add(currTrip);
                        }
                        //se la durata del viaggio è minore della pausa
                        else if (tripDurationMins < paramPauseTime)
                        {
                            //viene calcolata la differenza tra la durata della pausa e la durata del viaggio
                            TimeSpan diff = paramPauseTime.Subtract(tripDurationMins);

                            //la nuova durata della pausa è uguale alla differenza
                            paramPauseTime = diff;

                            //il viaggio con durata minore della pausa va rimosso
                            toRemove.Add(currTrip);
                        }

                        //Se si vuole detrarre la pausa SOLO dal viaggio più lungo, sia essa esaurita o meno, si esce dal ciclo
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PauseDetractionOnlyLongestTrip) == (int)PauseDetractionOnlyLongestTrip.OnlyLongest)
                        {
                            break;
                        }
                    }
                }

                //si rimuovono i viaggi che non rispettano le condizioni(viaggi presenti nella lista toRemove)
                tripList.RemoveAll(x => toRemove.Contains(x));
            }

            #endregion

            #region Gestione Fasce Viaggi

            //Recupera da PARAM se vanno o meno trattate le FASCE VIAGGI
            int paramUseTripRange = 1;

            //viene controllato nella scheda parametri se si usano le fasce di viaggio
            if (RepoManager.ParamRepo.ParametersRow.Utilizzo_Fasce_Viaggi.HasValue)
            {
                paramUseTripRange = RepoManager.ParamRepo.ParametersRow.Utilizzo_Fasce_Viaggi.Value;
            }

            //se si hanno viaggi e vengono usate le fasce di viaggio
            if (tripList.Count > 0 && paramUseTripRange != 1)
            {
                #region FASCE VIAGGI DA SCHEDA PARAMETRI
                TimeSpan paramTripRange1Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_1_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_1_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange1End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_1_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_1_Fine.Value : TimeSpan.Zero;

                TimeSpan paramTripRange2Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_2_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_2_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange2End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_2_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_2_Fine.Value : TimeSpan.Zero;

                TimeSpan paramTripRange3Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_3_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_3_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange3End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_3_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_3_Fine.Value : TimeSpan.Zero;

                TimeSpan paramTripRange4Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_4_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_4_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange4End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_4_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_4_Fine.Value : TimeSpan.Zero;

                TimeSpan paramTripRange5Start = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Inizio.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Inizio.Value : TimeSpan.Zero;
                TimeSpan paramTripRange5End = RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Fine.HasValue ? RepoManager.ParamRepo.ParametersRow.Fascia_Ore_Viaggi_5_Fine.Value : TimeSpan.Zero;
                #endregion

                #region FASCE VIAGGI DA SCHEDA COLLABORATORE
                TimeSpan colTripRange1Start = currentCol.Fascia_Ore_Viaggi_1_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_1_Inizio_Col.Value : paramTripRange1Start;
                TimeSpan colTripRange1End = currentCol.Fascia_Ore_Viaggi_1_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_1_Fine_Col.Value : paramTripRange1End;
                TimeSpan colTripRange2Start = currentCol.Fascia_Ore_Viaggi_2_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_2_Inizio_Col.Value : paramTripRange2Start;
                TimeSpan colTripRange2End = currentCol.Fascia_Ore_Viaggi_2_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_2_Fine_Col.Value : paramTripRange2End;
                TimeSpan colTripRange3Start = currentCol.Fascia_Ore_Viaggi_3_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_3_Inizio_Col.Value : paramTripRange3Start;
                TimeSpan colTripRange3End = currentCol.Fascia_Ore_Viaggi_3_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_3_Fine_Col.Value : paramTripRange3End;
                TimeSpan colTripRange4Start = currentCol.Fascia_Ore_Viaggi_4_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_4_Inizio_Col.Value : paramTripRange4Start;
                TimeSpan colTripRange4End = currentCol.Fascia_Ore_Viaggi_4_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_4_Fine_Col.Value : paramTripRange5End;
                TimeSpan colTripRange5Start = currentCol.Fascia_Ore_Viaggi_5_Inizio_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_5_Inizio_Col.Value : paramTripRange5Start;
                TimeSpan colTripRange5End = currentCol.Fascia_Ore_Viaggi_5_Fine_Col.HasValue ? currentCol.Fascia_Ore_Viaggi_5_Fine_Col.Value : paramTripRange5End;
                #endregion

                //viene istanziata una nuova lista di viaggi accettati, cioè che cadono nelle fasce viaggi consentite
                List<Trip> acceptedTrips = new List<Trip>();

                //per ogni viaggio della lista viene controllato se cade nella fascia viaggi consentita e viene aggiunto alla lista di viaggi accettati
                foreach (Trip trip in tripList)
                {
                    if (IsTripInRange(colTripRange1Start, colTripRange1End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                    else if (IsTripInRange(colTripRange2Start, colTripRange2End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                    else if (IsTripInRange(colTripRange3Start, colTripRange3End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                    else if (IsTripInRange(colTripRange4Start, colTripRange4End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                    else if (IsTripInRange(colTripRange5Start, colTripRange5End, trip))
                    {
                        acceptedTrips.Add(trip);
                    }
                }

                //la nuova lista di viaggi si trasforma nella lista di viaggi consetiti
                tripList = acceptedTrips;
            }
            #endregion

            #region Gestione Tabella Distanze KM/Ore

            //Recupera da PARAM se sul Viaggio vanno Gestite ANCHE le Informazioni relative AI KM/ORE presenti nella TAB_DISTANZE e come vanno gestite
            TripAssignmentTypeEnum paramTripAssignement = (TripAssignmentTypeEnum)RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti; // means skip check

            //se la lista di viaggi è valorizzata e si ha la necessita di assegnare i KM e le ore al viaggio
            if ((tripList.Count > 0 && paramTripAssignement != TripAssignmentTypeEnum.None) || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ViewKilometers) == 1)
            //Caso di Viaggio da Intestare al Cantiere Viaggi di PARAM
            {
                // inizializzazione della lista da utilizzare nell'eventuale cancellazione dei viaggi con calcolo gis
                // senza una corrispondente riga nella tabella distanze
                List<Trip> tripsToDelete = new List<Trip>();


                foreach (Trip trip in tripList)
                {
                    //viene etratto il cantiere di partenza viaggio
                    var cantE = RepoManager.CantRepo.Single(c => c.Cant_Id == trip.cantIdStart);
                    //il cantiere di partenza corrisponde a quello di fine 
                    var cantU = cantE;
                    if (trip.cantIdStart != trip.RegU.Cant_Id)
                        cantU = RepoManager.CantRepo.Single(c => c.Cant_Id == trip.RegU.Cant_Id);

                    Tab_Dist distRow = null;

                    //Se in PARAM c'è il Tipo Assegnazione KM/Minuti = FIND (1) 
                    if (paramTripAssignement == TripAssignmentTypeEnum.Find || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ViewKilometers) == 1)

                    //i Dati vengono cercati per Codice Cantiere 
                    //se non trovato per Codice Cantiere allora la ricerca viene effettuata anche per CAP)
                    //se NON trovato per CAP allora la ricerca viene effettuata anche per Luogo)
                    {
                        //vengono estratti i codici cantiere
                        var cantEValue = cantE.Cant_Id.ToString();
                        var cantUValue = cantU.Cant_Id.ToString();

                        #region 1.RICERCA PER CODICE CANTIERE 

                        //ricerco per codice cantiere nella tabella diatanze partendo dal cantiere di inizio a quello di fine
                        distRow = FindInTab_Dist("C", cantEValue, cantUValue);
                        //se non viene trovato il valore allore si procede alla ricera partendo dal cantire di fine a quello di inizio
                        if (distRow == null)
                            distRow = FindInTab_Dist("C", cantUValue, cantEValue);
                        #endregion

                        #region 2.RICERCA PER CAP
                        //se la ricera per codice cantiere non è andata a buon fine si ricerca per CAP
                        if (distRow == null)
                        {
                            //viene estratto il CAP dai cantieri
                            cantEValue = cantE.Cap_Can;
                            cantUValue = cantU.Cap_Can;

                            //ricerca mediante il CAP tra cantiere iniziale e finale
                            distRow = FindInTab_Dist("K", cantEValue, cantUValue);
                            if (distRow == null)
                                //ricerca per cantiere finale e iniziale
                                distRow = FindInTab_Dist("K", cantUValue, cantEValue);
                            #endregion

                            #region 3.RICERCA PER LUOGO
                            //se la ricerca per CAP non ha dato risultati viene ricercato il tutto per luogo
                            if (distRow == null)
                            {
                                //viene estratto il luogo dei cantieri
                                cantEValue = cantE.Luogo_Can;
                                cantUValue = cantU.Luogo_Can;

                                //viene ricercato per luogo cantiere iniziale e luogo cantiere finale
                                distRow = FindInTab_Dist("P", cantEValue, cantUValue);
                                if (distRow == null)
                                    //viene ricarcato per luogo cantiere fianle e cantiere iniziale
                                    distRow = FindInTab_Dist("P", cantUValue, cantEValue);
                            }

                        }
                        #endregion

                    }

                    //se nella scheda param ho Tipo_Assegnazione_KMMinuti=2
                    if (paramTripAssignement == TripAssignmentTypeEnum.Calculate || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ViewKilometers) == 1)
                    {
                        //Viene controllato nella tab decod se è presente la gestione mediante GIS
                        var tdCant = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Chiave_Tab == "G");

                        //se ho la gestione mediante GIS
                        if (tdCant != null)
                        {
                            //viene creato l'indirizzo del cantiere di partenza e di fine compatibile con le richieste di GIS
                            string cantEAddress = string.Format("{0}|{1}|{2}", cantE.Luogo_Can, cantE.Indirizzo_Can, cantE.Cap_Can);
                            string cantUAddress = string.Format("{0}|{1}|{2}", cantU.Luogo_Can, cantU.Indirizzo_Can, cantU.Cap_Can);

                            #region RICERCA DEL VIAGGIO NELLA TAB DISTANZE

                            //cerco il viaggio da cantiereE a cantiereU nella tabella distanze
                            distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tdCant.Tab_Decod_Id && d.Partenza_Tab_Dist.ToUpper() == cantEAddress.ToUpper() && d.Arrivo_Tab_Dist.ToUpper() == cantUAddress.ToUpper());

                            //se non ho trovato il viaggio da cantiereE a cantiereU, provo da cantiereU a cantiereE
                            if (distRow == default(Tab_Dist))
                                distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tdCant.Tab_Decod_Id && d.Partenza_Tab_Dist.ToUpper() == cantUAddress.ToUpper() && d.Arrivo_Tab_Dist.ToUpper() == cantEAddress.ToUpper());

                            #endregion

                            #region GENERAZIONE VIAGGIO DA GIS

                            //altrimenti genero il viaggio da GIS (solo se il flag gis è attivo)
                            if (distRow == default(Tab_Dist) && RepoManager.ParamRepo.ParametersRow.Flag_GPS != 0)
                            {
                                //inizializzo le variabili di inizio e fine viaggio

                                Coordinate newStartRequest = new Coordinate();
                                Coordinate newEndRequest = new Coordinate();

                                if (CommonService.Nz(cantE.LatitudineGps_Can, 0) == 0 || CommonService.Nz(cantE.LongitudineGps_Can, 0) == 0)
                                //Se Il Cantiere di INIZIO VIAGGIO (ENTRATA) NON ha la LATITUDINE o la LONGITUDINE la cerca in base ai dati di ubicazione con BING
                                // e approfitta per aggiornarle anche in Anagrafica CANT
                                {
                                    RepoManager.CantRepo.UpdateGeoLocation(cantE);
                                }

                                newStartRequest.Latitude = cantE.LatitudineGps_Can;
                                newStartRequest.Longitude = cantE.LongitudineGps_Can;

                                if (CommonService.Nz(cantU.LatitudineGps_Can, 0) == 0 || CommonService.Nz(cantU.LongitudineGps_Can, 0) == 0)
                                //Se Il Cantiere di FINE VIAGGIO (USCITA) NON ha la LATITUDINE o la LONGITUDINE la cerca in base ai dati di ubicazione con BING
                                // e approfitta per aggiornarle anche in Anagrafica CANT
                                {
                                    RepoManager.CantRepo.UpdateGeoLocation(cantU);
                                }

                                newEndRequest.Latitude = cantU.LatitudineGps_Can;
                                newEndRequest.Longitude = cantU.LongitudineGps_Can;

                                //Se sono disponibili LAT/LONG sia del Cantiere di Inizio Viaggio (Entrata) sia del Cantiere di Fine Viaggio (Uscita)
                                //Allora calcola con BING il Percorso fra il Cantiere di Inizio Viaggio e quello di Fine Viaggio ottenenedone i KM e la Durata da BING
                                if (CommonService.Nz(cantE.LatitudineGps_Can, 0) != 0 &&
                                    CommonService.Nz(cantE.LongitudineGps_Can, 0) != 0 &&
                                    CommonService.Nz(cantU.LatitudineGps_Can, 0) != 0 &&
                                    CommonService.Nz(cantU.LongitudineGps_Can, 0) != 0)
                                {
                                    // se il cantiere ha impostato la latitudine e la longitudine ma non ha un indirizzo, un cap e un luogo
                                    // allora non si genera il record in tab distanze
                                    if (CommonService.Nz(cantE.Indirizzo_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantE.Cap_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantE.Luogo_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantU.Indirizzo_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantU.Cap_Can, String.Empty) != String.Empty &&
                                        CommonService.Nz(cantU.Luogo_Can, String.Empty) != String.Empty)
                                    {
                                        var service = new CalcoloPercorsoService();
                                        var result = Task.Run(() => service.CalcolaPercorsoAsync(newStartRequest.Latitude, newStartRequest.Longitude, newEndRequest.Latitude, newEndRequest.Longitude)).Result;
                                        //Calcolo rotta tra i due punti
                                        Route routeResult = BusinessService.GetRoute(new Coordinate[] { newStartRequest, newEndRequest });

                                        if (result.DistanzaKm > 0)
                                        //Se è riuscito a Calcolare con BING i KM e la Durata del Viaggio allora crea il REcord della TAB_DISTANZA con Tipo = "G"
                                        {

                                            Tab_Decod tabDecod = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab.ToUpper() == "TIPO_DISTANZA" && td.Chiave_Tab.ToUpper() == "G");

                                            if (tabDecod != null)
                                            {
                                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DurationTrip) == 1)
                                                {
                                                    List<Cant> cantiereU = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == cantU.Cant_Id).ToList();
                                                    List<Cant> cantiereE = RepoManager.CantRepo.GetAllQueryable(c => c.Cant_Id == cantE.Cant_Id).ToList();
                                                    int minuti = 0;
                                                    if (cantiereU.First().Note_Can == "1.5" || cantiereE.First().Note_Can == "1.5")
                                                    {
                                                        minuti = (int)routeResult.TravelDistance;
                                                        minuti = (int)(minuti * 1.5);
                                                        distRow = new Tab_Dist()
                                                        {
                                                            Partenza_Tab_Dist = string.Format("{0}|{1}|{2}", cantE.Luogo_Can, cantE.Indirizzo_Can, cantE.Cap_Can),
                                                            Arrivo_Tab_Dist = string.Format("{0}|{1}|{2}", cantU.Luogo_Can, cantU.Indirizzo_Can, cantU.Cap_Can),
                                                            Tab_Decod_Id = tabDecod.Tab_Decod_Id,
                                                            KM_Tab_Dist = (decimal)result.DistanzaKm,
                                                            Minuti_Tab_Dist = (int)result.DurataMinuti,
                                                        };
                                                    }
                                                    else if (cantiereU.First().Note_Can == "2" || cantiereE.First().Note_Can == "2")
                                                    {
                                                        minuti = (int)routeResult.TravelDistance;
                                                        minuti = (int)(minuti * 2);
                                                        distRow = new Tab_Dist()
                                                        {
                                                            Partenza_Tab_Dist = string.Format("{0}|{1}|{2}", cantE.Luogo_Can, cantE.Indirizzo_Can, cantE.Cap_Can),
                                                            Arrivo_Tab_Dist = string.Format("{0}|{1}|{2}", cantU.Luogo_Can, cantU.Indirizzo_Can, cantU.Cap_Can),
                                                            Tab_Decod_Id = tabDecod.Tab_Decod_Id,
                                                            KM_Tab_Dist = (decimal)result.DistanzaKm,
                                                            Minuti_Tab_Dist = (int)result.DurataMinuti,
                                                        };
                                                    }
                                                    else {
                                                        minuti = (int)routeResult.TravelDistance;
                                                        distRow = new Tab_Dist()
                                                        {
                                                            Partenza_Tab_Dist = string.Format("{0}|{1}|{2}", cantE.Luogo_Can, cantE.Indirizzo_Can, cantE.Cap_Can),
                                                            Arrivo_Tab_Dist = string.Format("{0}|{1}|{2}", cantU.Luogo_Can, cantU.Indirizzo_Can, cantU.Cap_Can),
                                                            Tab_Decod_Id = tabDecod.Tab_Decod_Id,
                                                            KM_Tab_Dist = (decimal)result.DistanzaKm,
                                                            Minuti_Tab_Dist = (int)result.DurataMinuti,
                                                        };
                                                    }
                                                }
                                                else {
                                                    distRow = new Tab_Dist()
                                                    {
                                                        Partenza_Tab_Dist = string.Format("{0}|{1}|{2}", cantE.Luogo_Can, cantE.Indirizzo_Can, cantE.Cap_Can),
                                                        Arrivo_Tab_Dist = string.Format("{0}|{1}|{2}", cantU.Luogo_Can, cantU.Indirizzo_Can, cantU.Cap_Can),
                                                        Tab_Decod_Id = tabDecod.Tab_Decod_Id,
                                                        KM_Tab_Dist = (decimal)result.DistanzaKm,
                                                        Minuti_Tab_Dist = (int)result.DurataMinuti,
                                                    };
                                                }
                                                var errorTab_DistRepo = RepoManager.Tab_DistRepo.Check(distRow, true);
                                                if (!errorTab_DistRepo.Any())
                                                {
                                                    try
                                                    {
                                                        RepoManager.Tab_DistRepo.Add(distRow, true);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        _log.ErrorFormat("Errore durante l'inserimento nella tab. distanze di un nuovo record a causa dell exception {0}", ex.InnerException);
                                                    }

                                                }

                                                else
                                                {
                                                    BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                                    List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                                    foreach (var item in errorTab_DistRepo)
                                                        errRouteCalculate.Add(new KeyValuePair<string, string>(item.Key, String.Format("{0}", item.Value)));
                                                    RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);
                                                }
                                            }
                                        }
                                        else
                                        //Se NON è risucito a Calcolare il Percorso con Bing allora scrive un messaggio di errore nella TAB_MESSAGGI con Riferimento RouteCalculate
                                        {
                                            var colToShow = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == trip.RegE.Col_Id);
                                            string descrizioneCollaboratore = String.Format("{0} - {1}", colToShow.Codice_Collaboratore, colToShow.CognomeNome_Col);

                                            string cantEMessageDesc = String.Format("{0} - {1}", cantE.Codice_Cantiere, cantE.Descrizione_Can);
                                            string cantUMessageDesc = String.Format("{0} - {1}", cantU.Codice_Cantiere, cantU.Descrizione_Can);

                                            List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                            errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore)));
                                            RepoManager.Tab_MessaggiRepo.InsertMessages(errors, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);

                                            trip.RegE.Note_Reg = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore);

                                            if (trip.RegU != null)
                                                trip.RegU.Note_Reg = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore);

                                            if (errRouteCalculate.Any())
                                                BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                        }
                                    }
                                    else
                                    {
                                        // in caso sia presente la latitudine e la longitudine ma non siano presenti nei cantieri dati di ubicazione validi,
                                        // allora il tutto viene segnalato con un messaggio
                                        var colToShow = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == trip.RegE.Col_Id);
                                        string descrizioneCollaboratore = String.Format("{0} - {1}", colToShow.Codice_Collaboratore, colToShow.CognomeNome_Col);

                                        string cantEMessageDesc = String.Format("{0} - {1}", cantE.Codice_Cantiere, cantE.Descrizione_Can);
                                        string cantUMessageDesc = String.Format("{0} - {1}", cantU.Codice_Cantiere, cantU.Descrizione_Can);

                                        List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                        errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore)));
                                        RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);
                                        trip.RegE.Note_Reg = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore);

                                        if (trip.RegU != null)
                                            trip.RegU.Note_Reg = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore);

                                        if (errRouteCalculate.Any())
                                            BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                    }
                                }
                                else
                                //Se NON è risucito a Calcolare il Percorso con Bing allora scrive un messaggio di errore nella TAB_MESSAGGI con Riferimento RouteCalculate
                                {
                                    var colToShow = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == trip.RegE.Col_Id);
                                    string descrizioneCollaboratore = String.Format("{0} - {1}", colToShow.Codice_Collaboratore, colToShow.CognomeNome_Col);

                                    string cantEMessageDesc = String.Format("{0} - {1}", cantE.Codice_Cantiere, cantE.Descrizione_Can);
                                    string cantUMessageDesc = String.Format("{0} - {1}", cantU.Codice_Cantiere, cantU.Descrizione_Can);

                                    List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                    errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore)));
                                    RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);
                                    trip.RegE.Note_Reg = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore);
                                    if (trip.RegU != null)
                                        trip.RegU.Note_Reg = BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_IN_DATA_Z_ORA_W_COL_V_NON_CALCOLABILE_DA_MAPPA, cantEMessageDesc, cantUMessageDesc, trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy"), trip.RegE.Registrazione_Data_Ora_Fis_Reg.ToString("hh:mm"), descrizioneCollaboratore);

                                    if (errRouteCalculate.Any())
                                        BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                }
                            }
                            #endregion
                        }
                    }

                    //ho un viaggio con distanza non nulla
                    if (distRow != null)
                    {
                        // se è attiva la personalizzazione che prevede la non generazione dei viaggi nello stesso comune se inferiori e il kilometraggio espresso
                        // nella tabella distanze è inferiore a tale cifra, allora si provvede a marcare il viaggio che si sta generando per la cancellazione

                        // se il viaggio che si sta generando parte e arriva nello stesso comune, è attiva la personalizzazione del controllo di km in viaggi per stesso comune e i km
                        // assegnati alla tab distanze sono inferiori alla soglia, allora si procede a marcare il viaggio per la cancellazione; in caso contrario si procede alla sua generazione
                        if (IsTripSameMunicipalityToDelete(distRow))
                        {
                            trip.RegE.Codice_Accoppiamento = "1";
                            tripsToDelete.Add(trip);
                        }
                        else
                        {
                            // si calcola la durata dei viaggi utilizzando la tab distanze solamente se
                            // espresso dal livelli di personalizzazione
                            int customizationEnum = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CalculateTripDataEnum);
                            if (customizationEnum != (int)CalculateTripDataEnum.OnlyKm || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ViewKilometers) == 1)
                            {
                                if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.ViewKilometers) == 1)
                                {
                                    trip.RegE.Note_Reg = "Fatta con personalizzazione";
                                }
                                else {
                                    if (trip.TripDuration.TotalMinutes > distRow.Minuti_Tab_Dist)
                                    {
                                        trip.RegU.Registrazione_Data_Ora_Fig_Reg = trip.RegE.Registrazione_Data_Ora_Fig_Reg.Value.AddMinutes(distRow.Minuti_Tab_Dist);
                                        trip.RegU.Registrazione_Data_Ora_Fis_Reg = trip.RegE.Registrazione_Data_Ora_Fis_Reg.AddMinutes(distRow.Minuti_Tab_Dist);

                                        trip.RegE.Note_Reg = "Durata viaggio presa da tabella distanze";

                                    }
                                }
                                
                            }

                            // si procede alla verifica e all'inserimento dei km solamente se
                            // la destinazione non è un cantiere ore non lavorate
                            if (distRow.KM_Tab_Dist > default(decimal) && !trip.IsToOnl)
                                trip.RegE.KM_Reg = distRow.KM_Tab_Dist;
                            else
                                trip.RegE.KM_Reg = 0;
                        }
                    }
                    else
                    {
                        // se sto utilizzando il gis nel calcolo dei viaggi
                        if (paramTripAssignement == TripAssignmentTypeEnum.Calculate)
                        {
                            // se è attivata la personalizzaziontre per la non creazione dei viaggi con calcolo gis senza riga in tabella distanze
                            // allora marco per la cancellazione il viaggio in elaborazione
                            int customizationVersion = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoCreateTripsWithoutTabDistRowEnum);
                            if (customizationVersion == (int)NoCreateTripsWithoutTabDistRowEnum.DoNotCreate)
                            {
                                trip.RegE.Codice_Accoppiamento = "1";
                                tripsToDelete.Add(trip);
                            }
                        }
                    }
                }

                // al termine dell'elaborazione, se sono presenti dei viaggi da cancellare allora
                // procedo all'eliminazione
                if (tripsToDelete.Any())
                    tripList = tripList.Where(trip => trip.RegE.Codice_Accoppiamento != "1").ToList();

            }
            #endregion

            #region Gestione Viaggi Inizio/Fine Giornata

            if (paramTripHours != (int)FlagTripHoursParamEnum.None)
            //nel caso in cui il Flag della tab PARAM abiliti i Viaggi di Inizio/Fine Giornata da/a Casa (<> 0)
            {
                // Altrimenti controlla se sono attivi i viaggi di inizio/fine "standard"
                if (paramTripHours == (int)FlagTripHoursParamEnum.AllExceptTwo)   //(1)
                                                                                  //Nel caso in cui in Scheda PARAM sia abilitata la gestione dei Viaggi da/a Casa per Tutti Eccetto i Col Esclusi
                {
                    if (currentCol.Flag_Ore_Viaggi_Col_Inizio_Fine != (int)FlagTripHoursColStartEndEnum.AllExceptTwo)  // <> 2
                                                                                                                       //Tratta solo i Collaboratore che NON sono da Escludere
                        tripList.AddRange(ElaborateStartEndTrips(orderedCurrentTripsByMotByColByDate, currentCol, elaborateUserId, elaborateDateTime, application));
                }
                else if (paramTripHours == (int)FlagTripHoursParamEnum.OnlyOne)
                //Nel caso in cui in Scheda PARAM sia abilitata la gestione dei Viaggi da/a Casa per i Soli Col Abilitati
                {
                    if (currentCol.Flag_Ore_Viaggi_Col_Inizio_Fine == (int)FlagTripHoursColStartEndEnum.OnlyOne)
                        //Tratta solo i Collaboratore Abilitati
                        tripList.AddRange(ElaborateStartEndTrips(orderedCurrentTripsByMotByColByDate, currentCol, elaborateUserId, elaborateDateTime, application));
                }
            }

            #endregion

            List<Reg> trips = new List<Reg>();

            // mi creo una lista di viaggi da elaborare poi
            foreach (var trip in tripList)
                trips.AddRange(trip.Regs);

            return trips;
        }

        /// <summary>
        /// Determina se, secondo le personalizzazioni, il viaggio generato per la specifica tabella distanze è nello stesso comune e da cancellare o meno.
        /// </summary>
        /// <param name="distRow">La tabella distanze da processare.</param>
        /// <returns><c>true</c> se il viaggio identificato dalla tabella distanze è da cancellare (stesso comune, km inferiori a parametro e personalizzazione attiva); altrimenti <c>false</c></returns>
        private bool IsTripSameMunicipalityToDelete(Tab_Dist distRow)
        {
            // inizializzazione del valore di ritorno del metodo
            bool isToDelete = false;

            // calcolo della personalizzazione
            int municipalityCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.NoCreateTripOnSameMunicipalityUnderKmEnum);

            // se la personalizzazione risulta attiva si calcola dai parametri della customization i km soglia
            decimal kmThreshold = 0;
            if (municipalityCustomization == (int)NoCreateTripOnSameMunicipalityUnderKmEnum.CreateOnlyIfInParam)
                kmThreshold = Convert.ToDecimal(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.NoCreateTripOnSameMunicipalityUnderKmEnum, "KmThreshold"));

            // - il viaggio risulta nello stesso comune solamente se il tipo tab distanza è P (Comune) o G (GIS);
            // - Se il tipo distanza è P arrivo e partenza devono essere uguali affinchè arrivo e partenza siano nello stesso comune;
            // - Se il tipo distanza è G allora arrivo e partenza devono avere la stringa antecedente al primo "|" uguali affinché risulti il viaggio nello stesso comune
            bool isSameMunicipality = false;
            if (distRow.DistType == DistTypeEnum.GIS || distRow.DistType == DistTypeEnum.Place)
            {
                string start = distRow.DistType == DistTypeEnum.GIS ? distRow.Partenza_Tab_Dist.Substring(0, distRow.Partenza_Tab_Dist.IndexOf('|')).ToUpper() : distRow.Partenza_Tab_Dist.ToUpper();
                string end = distRow.DistType == DistTypeEnum.GIS ? distRow.Arrivo_Tab_Dist.Substring(0, distRow.Arrivo_Tab_Dist.IndexOf('|')).ToUpper() : distRow.Arrivo_Tab_Dist.ToUpper();
                isSameMunicipality = start == end;
            }

            // se il viaggio che si sta generando parte e arriva nello stesso comune, è attiva la personalizzazione del controllo di km in viaggi per stesso comune e i km
            // assegnati alla tab distanze sono inferiori alla soglia, allora si procede a marcare il viaggio per la cancellazione; in caso contrario si procede alla sua generazione
            isToDelete = municipalityCustomization == (int)NoCreateTripOnSameMunicipalityUnderKmEnum.CreateOnlyIfInParam && isSameMunicipality && distRow.KM_Tab_Dist < kmThreshold;

            //Se il parametro di viaggio inizio/fine giornata è = 3 (generazione dei viaggi dalla sede), i viaggi non vengono generati se minori della soglia
            if (RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G == (int)TripAssignmentTypeEnum.CalculateFromHeadquarter)
            {
                isToDelete = distRow.KM_Tab_Dist < kmThreshold;
            }

            // ritorno del valore del metodo
            return isToDelete;
        }

        /// <summary>
        /// Determina se un viaggio è totalmente compreso in una fascia
        /// </summary>
        /// <param name="start">L'inizio della fascia.</param>
        /// <param name="end">La fine della fascia.</param>
        /// <param name="trip">Il viaggio.</param>
        /// <returns><c>true</c> se il viaggio è totalmente compreso in una fascia; altrimenti <c>false</c></returns>
        private bool IsTripInRange(TimeSpan start, TimeSpan end, Trip trip)
        {
            if (start == TimeSpan.Zero && end == TimeSpan.Zero)
                return false;
            if (trip.RegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= start && trip.RegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= end)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Determina se un viaggio è totalmente compreso in una fascia o se la fascia è totalmente compresa nel viaggio
        /// </summary>
        /// <param name="start">L'inizio della fascia.</param>
        /// <param name="end">La fine della fascia.</param>
        /// <param name="trip">Il viaggio.</param>
        /// <returns><c>true</c> se il viaggio è totalmente compreso in una fascia o se la fascia è totalmente compresa nel viaggio; altrimenti <c>false</c></returns>
        private bool IsTripInRangeIsRangeInTrip(TimeSpan start, TimeSpan end, Trip trip)
        {
            if (start == TimeSpan.Zero && end == TimeSpan.Zero)
                return false;
            if ((trip.RegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= start && trip.RegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= end) || (trip.RegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= start && trip.RegU.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= end))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Determina se un viaggio ha la partenza compresa in una fascia
        /// </summary>
        /// <param name="start">L'inizio della fascia.</param>
        /// <param name="start">La fine della fascia.</param>
        /// <param name="trip">Il viaggio.</param>
        /// <returns><c>true</c> se la partenza del viaggio è all'interno in una fascia; altrimenti <c>false</c></returns>
        private bool IsStartTripInRange(TimeSpan start, TimeSpan end, Trip trip)
        {
            if (trip.RegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay >= start && trip.RegE.Registrazione_Data_Ora_Fis_Reg.TimeOfDay <= end)
                return true;
            else
                return false;
        }

        private Tab_Dist FindInTab_Dist(string tabKey, string firstValue, string secondValue)
        //Esegue fisicamente la lettura della TAB DISTANZE con il Tipo Record (Z/K/P ricevuto e con i Codici Zona/Cap/Luogo ricevuti dalla FindValidTab_Dist
        {
            Tab_Dist distRow = null;
            if (firstValue != null && secondValue != null)
            {
                var tabDecod = RepoManager.Tab_DecodRepo.SingleOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Chiave_Tab == tabKey);
                if (tabDecod != null)
                    distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tabDecod.Tab_Decod_Id && d.Partenza_Tab_Dist == firstValue && d.Arrivo_Tab_Dist == secondValue);
            }

            return distRow;
        }

        private Tab_Dist FindValidTab_Dist(Cant currentCant, Col currentCol, bool isRegU, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application)
        //Ricerca in Tab Distanze per Z/K/P/G e per Col/cant e/o Cant/Col
        {
            //Recupera il parametro della generazione di viaggi di inizio/fine giornata dal collaboratore o dai parametri. Se il parametro è = 3, genera i viaggi di inizio/fine giornata SOLO kilometrici dal/al cantiere SEDE. Se la prima/ultima regv è sul cantiere SEDE, il viaggio sarà comunque solo kilometrico.
            int startEndParam = currentCol.Flag_Viaggio_InizioFine_GIS.HasValue ? currentCol.Flag_Viaggio_InizioFine_GIS.Value : RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G;

            //Nel caso del Viaggio di Inizio Giornata 
            //imposta come Cantiere di Fine del Viaggio di Inizio Giornata 
            // la Zona di Anagrafica del Cantiere della 1° Entrata del Giorno
            // il Cap del Domicilio del Cantiere della 1° Entrata del Giorno
            // il Luogo del Domicilio del Cantiere della 1° Entrata del Giorno
            //imposta come Cantiere di Inizio del Viaggio di Inizio Giornata 
            // la Zona di Anagrafica del Collaboratore che effettua il Viaggio
            // il Cap del Domicilio del Collaboratore che effettua il Viaggio
            // il Luogo del Domicilio del Collaboratore che effettua il Viaggio
            //provando anche ad invertire i Dati 
            //nel caso in cui con i Dati iniziali non abbia trovato nulla in Tab_Distanze
            Tab_Dist distRow = null;
            if ((TripAssignmentTypeEnum)startEndParam != TripAssignmentTypeEnum.None)
            {
                //Nel caso del Viaggio di Inizio Giornata 
                //imposta come Cantiere di Inizio del Viaggio di Inizio Giornata 
                // la Zona di Anagrafica del Collaboratore che sta effettuando il Viaggio
                //imposta come Cantiere di Fine del Viaggio di Inizio Giornata 
                // la Zona di Anagrafica del Cantiere della 1° Entrata del Giorno
                var colValue = currentCol.Zona_Col;
                var cantValue = currentCant.Zona_Can;

                if (isRegU)
                //Nel caso del Viaggio di Fine Giornata 
                //imposta come Cantiere di Inizio del Viaggio di Fine Giornata 
                // la Zona di Anagrafica del Cantiere dell'Ultima Uscita del Giorno
                // // la Zona di Anagrafica del Collaboratore che sta effettuando il Viaggio
                {
                    colValue = currentCant.Zona_Can;
                    cantValue = currentCol.Zona_Col;
                }

                //prova a cercare in Tabella un Record "Z" 
                //Per i Viaggi di Inizio Giornata
                //  con Partenza = Zona_Col
                //  con Arrivo = Zona_Can
                //Per i Viaggi di Fine Giornata
                //  con Partenza = Zona_Can
                //  con Arrivo = Zona_Col
                distRow = FindInTab_Dist("Z", colValue, cantValue);

                if (distRow == null)
                    //se non lo trova prova anche invertendo i dati
                    //ovvero prova anche a cercare in Tabella un Record "Z" 
                    //Per i Viaggi di Inizio Giornata
                    //  con Partenza = Zona_Can
                    //  con Arrivo = Zona_Col
                    //Per i Viaggi di Fine Giornata
                    //  con Partenza = Zona_Col
                    //  con Arrivo = Zona_Can
                    distRow = FindInTab_Dist("Z", cantValue, colValue);

                if (distRow == default(Tab_Dist))
                {
                    //se NON ha trovato nulla con Chiave "Z"
                    //prova a cercare in Tabella un Record "K" 
                    //Per i Viaggi di Inizio Giornata
                    //  con Partenza = Domicilio_Cap_Col del Col
                    //  con Arrivo = Cap_Can del CAN

                    colValue = currentCol.Domicilio_Cap_Col;
                    cantValue = currentCant.Cap_Can;

                    if (isRegU)
                    {
                        //Per i Viaggi di Fine Giornata
                        //  con Partenza = Cap_Can del CAN
                        //  con Arrivo = Domicilio_Cap_Col del Col
                        colValue = currentCant.Cap_Can;
                        cantValue = currentCol.Domicilio_Cap_Col;
                    }

                    distRow = FindInTab_Dist("K", colValue, cantValue);
                    if (distRow == null)
                        //se non lo trova prova anche invertendo i dati
                        //ovvero prova anche a cercare in Tabella un Record "K"
                        //Per i Viaggi di Inizio Giornata
                        //  con Partenza = Cap_Can del CAN
                        //  con Arrivo = Domicilio_Cap_Col del Col
                        //Per i Viaggi di Fine Giornata
                        //  con Partenza = Domicilio_Cap_Col del Col
                        //  con Arrivo =  Cap_Can del CAN
                        distRow = FindInTab_Dist("K", cantValue, colValue);

                    if (distRow == default(Tab_Dist))
                    {
                        //se NON ha trovato nulla con Chiave "K"
                        //prova a cercare in Tabella un Record "P" 
                        //Per i Viaggi di Inizio Giornata
                        //  con Partenza = Domicilio_Luogo del Col
                        //  con Arrivo = Luogo del CAN
                        colValue = currentCol.Domicilio_Luogo_Col;
                        cantValue = currentCant.Luogo_Can;

                        if (isRegU)
                        {
                            //Per i Viaggi di Fine Giornata
                            //  con Partenza = Luogo del CAN
                            //  con Arrivo = Domicilio_Luogo del Col
                            colValue = cantValue;
                            cantValue = currentCol.Domicilio_Luogo_Col;
                        }

                        distRow = FindInTab_Dist("P", colValue, cantValue);
                        if (distRow == null)
                            //se non lo trova prova anche invertendo i dati
                            //ovvero prova anche a cercare in Tabella un Record "P"
                            //Per i Viaggi di Inizio Giornata
                            //  con Partenza = Luogo del CAN
                            //  con Arrivo = Domicilio_Luogo del Col
                            //Per i Viaggi di Fine Giornata
                            //  con Partenza = Domicilio_Luogo del Col
                            //  con Arrivo =  Luogo del CAN
                            distRow = FindInTab_Dist("P", cantValue, colValue);
                    }
                }

                // se non è stata trovata una tab distanze di tipo Z, K, P
                // allora si cerca di generarla da GIS
                if (distRow == null && ((TripAssignmentTypeEnum)startEndParam == TripAssignmentTypeEnum.Calculate || (TripAssignmentTypeEnum)startEndParam == TripAssignmentTypeEnum.CalculateFromHeadquarter))
                {
                    #region Generazione viaggi di inzio/fine giornata con tabella GIS

                    // viene ricercato nella tab_decod il record che identifica il tipo G per la tabella, se presente proseguo con l'elaborazione
                    var tdCant = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Chiave_Tab == "G");
                    if (tdCant != null)
                    {
                        // calcolo gli indirizzi di entrata ed uscita da utilizzare nella ricerca nella tab distanza GIS
                        var startAddress = string.Format("{0}|{1}|{2}", currentCant.Luogo_Can, currentCant.Indirizzo_Can, currentCant.Cap_Can);
                        var endAddress = string.Format("{0}|{1}|{2}", currentCol.Domicilio_Luogo_Col, currentCol.Domicilio_Indirizzo_Col, currentCol.Domicilio_Cap_Col);

                        #region Ricerca del viaggio nella tab distanze

                        //cerco il viaggio dalla partenza all'arrivo nella tab distanze
                        distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tdCant.Tab_Decod_Id && d.Partenza_Tab_Dist.ToUpper() == startAddress.ToUpper() && d.Arrivo_Tab_Dist.ToUpper() == endAddress.ToUpper());

                        //se non ho trovato il viaggio dalla partenza all'arrivo, provo dall'arrivo alla partenza
                        if (distRow == default(Tab_Dist))
                            distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tdCant.Tab_Decod_Id && d.Partenza_Tab_Dist.ToUpper() == endAddress.ToUpper() && d.Arrivo_Tab_Dist.ToUpper() == startAddress.ToUpper());

                        #endregion

                        // se non ho trovato la tabella distanze con la ricerca e altrimenti genero il viaggio da GIS (solo se il flag gis è attivo)
                        if (distRow == default(Tab_Dist) && RepoManager.ParamRepo.ParametersRow.Flag_GPS != 0)
                        {
                            #region GENERAZIONE VIAGGIO DA GIS


                            Coordinate newStartRequest = new Coordinate();
                            Coordinate newEndRequest = new Coordinate();

                            // se la partenza non ha latitudine o la longitudine la si cerca in base ai dati di ubicazione con BING e si approfitta per aggiornare
                            // con i dati ottenuti l'anagrafica cantiere
                            if (CommonService.Nz(currentCant.LatitudineGps_Can, 0) == 0 || CommonService.Nz(currentCant.LongitudineGps_Can, 0) == 0)
                            {
                                RepoManager.CantRepo.UpdateGeoLocation(currentCant);

                            }

                            newStartRequest.Latitude = currentCant.LatitudineGps_Can;
                            newStartRequest.Longitude = currentCant.LongitudineGps_Can;

                            // se la destinazione del viaggio non ha la latitudine o la longitudine la si cerca in base di ubicazione con BING e si approfitta al contempo per
                            // aggiornare la relativa anagrafica
                            if (CommonService.Nz(currentCol.LatitudineGps_Col, 0) == 0 || CommonService.Nz(currentCol.LongitudineGps_Col, 0) == 0)
                            {
                                RepoManager.ColRepo.UpdateGeoLocation(currentCol);
                            }


                            newEndRequest.Latitude = currentCol.LatitudineGps_Col;
                            newEndRequest.Longitude = currentCol.LongitudineGps_Col;

                            // se sono disponibili LAT/LONG sia della partenza che della destinazione
                            // allora si calcola calcola con BING il percorso fra la partenza e l'arrivo ottenenedone i KM e la Durata da BING
                            if (CommonService.Nz(currentCant.LatitudineGps_Can, 0) != 0 &&
                                CommonService.Nz(currentCant.LongitudineGps_Can, 0) != 0 &&
                                CommonService.Nz(currentCol.LatitudineGps_Col, 0) != 0 &&
                                CommonService.Nz(currentCol.LongitudineGps_Col, 0) != 0)
                            {
                                // se il cantiere ha impostato la latitudine e la longitudine ma non ha un indirizzo, un cap e un luogo
                                // allora non si genera il record in tab distanze
                                if (CommonService.Nz(currentCant.Indirizzo_Can, String.Empty) != String.Empty &&
                                    CommonService.Nz(currentCant.Cap_Can, String.Empty) != String.Empty &&
                                    CommonService.Nz(currentCant.Luogo_Can, String.Empty) != String.Empty &&
                                    CommonService.Nz(currentCol.Domicilio_Indirizzo_Col, String.Empty) != String.Empty &&
                                    CommonService.Nz(currentCol.Domicilio_Cap_Col, String.Empty) != String.Empty &&
                                    CommonService.Nz(currentCol.Domicilio_Luogo_Col, String.Empty) != String.Empty)
                                {
                                    
                                    var service = new CalcoloPercorsoService();
                                    var result = Task.Run(() => service.CalcolaPercorsoAsync(newStartRequest.Latitude, newStartRequest.Longitude, newEndRequest.Latitude, newEndRequest.Longitude)).Result;
                                    // calcolo del percorso tra partenza e arrivo da Bing
                                    Route routeResult = BusinessService.GetRoute(new Coordinate[] { newStartRequest, newEndRequest });


                                    // se bing è riuscito a calcolare i Km e la durate del viaggio allora si crea il corrispettivo record nella tabella distanze con tipo "G"
                                    if (result.DistanzaKm > 0)
                                    {
                                        distRow = new Tab_Dist()
                                        {
                                            Partenza_Tab_Dist = string.Format("{0}|{1}|{2}", currentCant.Luogo_Can, currentCant.Indirizzo_Can, currentCant.Cap_Can),
                                            Arrivo_Tab_Dist = string.Format("{0}|{1}|{2}", currentCol.Domicilio_Luogo_Col, currentCol.Domicilio_Indirizzo_Col, currentCol.Domicilio_Cap_Col),
                                            Tab_Decod_Id = tdCant.Tab_Decod_Id,
                                            KM_Tab_Dist = (decimal)result.DistanzaKm,
                                            Minuti_Tab_Dist = (int)result.DurataMinuti,
                                        };

                                        var errorTabDistRepo = RepoManager.Tab_DistRepo.Check(distRow, true);
                                        if (!errorTabDistRepo.Any())
                                            RepoManager.Tab_DistRepo.Add(distRow, true);
                                        else
                                        {
                                            BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                            List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                            foreach (var item in errorTabDistRepo)
                                                errRouteCalculate.Add(new KeyValuePair<string, string>(item.Key, String.Format("{0}", item.Value)));
                                            RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);
                                        }
                                    }
                                    else // se bing non è riuscito a calcolare il percorso allora si scrive un messaggio d'errore nella tab_messaggi con riferimento al route calculate
                                    {
                                        string descrizioneCol = String.Format("{0} - {1}", currentCol.Codice_Collaboratore, currentCol.CognomeNome_Col);
                                        string descrizioneCant = String.Format("{0} - {1}", currentCant.Codice_Cantiere, currentCant.Descrizione_Can);

                                        List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                        errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_NON_CALCOLABILE_DA_MAPPA, descrizioneCant, descrizioneCol)));
                                        RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);

                                        if (errRouteCalculate.Any())
                                            BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                    }
                                }
                                else
                                {
                                    // in caso sia presente la latitudine e la longitudine ma non siano presenti nei cantieri dati di ubicazione validi,
                                    // allora il tutto viene segnalato con un messaggio
                                    string descrizioneCol = String.Format("{0} - {1}", currentCol.Codice_Collaboratore, currentCol.CognomeNome_Col);
                                    string descrizioneCant = String.Format("{0} - {1}", currentCant.Codice_Cantiere, currentCant.Descrizione_Can);


                                    List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                    errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_NON_CALCOLABILE_DA_MAPPA, descrizioneCant, descrizioneCol)));
                                    RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);

                                    if (errRouteCalculate.Any())
                                        BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                }
                            }
                            else // Se NON è risucito a Calcolare il Percorso con Bing allora scrive un messaggio di errore nella TAB_MESSAGGI con Riferimento RouteCalculate
                            {
                                string descrizioneCol = String.Format("{0} - {1}", currentCol.Codice_Collaboratore, currentCol.CognomeNome_Col);
                                string descrizioneCant = String.Format("{0} - {1}", currentCant.Codice_Cantiere, currentCant.Descrizione_Can);

                                List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_NON_CALCOLABILE_DA_MAPPA, descrizioneCant, descrizioneCol)));
                                RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);

                                if (errRouteCalculate.Any())
                                    BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                            }

                            #endregion



                        }

                        #endregion
                    }
                }
            }

            return distRow;
        }


        /// <summary>
        /// Cerca una Tab_Dist valida tra due cantieri
        /// </summary>
        /// <param name="currentCant">The current cant.</param>
        /// <param name="currentCol">The current col.</param>
        /// <param name="isRegU">if set to <c>true</c> [is reg u].</param>
        /// <param name="elaborateUserId">The elaborate user identifier.</param>
        /// <param name="elaborateDateTime">The elaborate date time.</param>
        /// <param name="application">The application.</param>
        /// <returns></returns>
        private Tab_Dist FindCantCantTab_Dist(Cant sedeCant, Cant cant, Col currentCol, bool isRegU, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application)
        //Ricerca in Tab Distanze per Z/K/P/G e per Col/cant e/o Cant/Col
        {
            //Recupera il parametro della generazione di viaggi di inizio/fine giornata dal collaboratore o dai parametri. Se il parametro è = 3, genera i viaggi di inizio/fine giornata SOLO kilometrici dal/al cantiere SEDE. Se la prima/ultima regv è sul cantiere SEDE, il viaggio sarà comunque solo kilometrico.
            int startEndParam = currentCol.Flag_Viaggio_InizioFine_GIS.HasValue ? currentCol.Flag_Viaggio_InizioFine_GIS.Value : RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G;

            //Nel caso del Viaggio di Inizio Giornata 
            //imposta come Cantiere di Fine del Viaggio di Inizio Giornata 
            // la Zona di Anagrafica del Cantiere di partenza
            // il Cap del Domicilio del Cantiere di partenza
            // il Luogo del Domicilio del Cantiere di partenza
            //imposta come Cantiere di Inizio del Viaggio di Inizio Giornata 
            // la Zona di Anagrafica del Cantiere di arrivo
            // il Cap del Domicilio del Cantiere di arrivo
            // il Luogo del Domicilio del Cantiere di arrivo
            //provando anche ad invertire i Dati 
            //nel caso in cui con i Dati iniziali non abbia trovato nulla in Tab_Distanze
            Tab_Dist distRow = null;
            if ((TripAssignmentTypeEnum)startEndParam != TripAssignmentTypeEnum.None)
            {
                //Nel caso del Viaggio di Inizio Giornata 
                //imposta come Cantiere di Inizio del Viaggio di Inizio Giornata 
                // la Zona di Anagrafica del cantiere di partenza
                //imposta come Cantiere di Fine del Viaggio di Inizio Giornata 
                // la Zona di Anagrafica del Cantiere di arrivo
                var cantStart = sedeCant.Zona_Can;
                var cantEnd = cant.Zona_Can;

                if (isRegU)
                //Nel caso del Viaggio di Fine Giornata 
                //imposta come Cantiere di Inizio del Viaggio di Fine Giornata 
                // la Zona di Anagrafica del Cantiere dell'Ultima Uscita del Giorno
                // // la Zona di Anagrafica del Collaboratore che sta effettuando il Viaggio
                {
                    cantStart = cant.Zona_Can;
                    cantEnd = sedeCant.Zona_Can;
                }

                //prova a cercare in Tabella un Record "Z" 
                //Per i Viaggi di Inizio Giornata
                //  con Partenza = sede
                //  con Arrivo = Zona_Can
                //Per i Viaggi di Fine Giornata
                //  con Partenza = Zona_Can
                //  con Arrivo = sede
                distRow = FindInTab_Dist("Z", cantStart, cantEnd);

                if (distRow == null)
                    //se non lo trova prova anche invertendo i dati
                    //ovvero prova anche a cercare in Tabella un Record "Z" 
                    //  con Partenza = sede
                    //  con Arrivo = Zona_Can
                    //Per i Viaggi di Fine Giornata
                    //  con Partenza = Zona_Can
                    //  con Arrivo = sede
                    distRow = FindInTab_Dist("Z", cantEnd, cantStart);

                if (distRow == default(Tab_Dist))
                {
                    //se NON ha trovato nulla con Chiave "Z"
                    //prova a cercare in Tabella un Record "K" 
                    //Per i Viaggi di Inizio Giornata
                    //  con Partenza = Domicilio_Cap_Col del Col
                    //  con Arrivo = Cap_Can del CAN

                    cantStart = sedeCant.Cap_Can;
                    cantEnd = cant.Cap_Can;

                    if (isRegU)
                    {
                        //Per i Viaggi di Fine Giornata
                        //  con Partenza = Cap_Can del CAN
                        //  con Arrivo = Domicilio_Cap_Col del Col
                        cantStart = cant.Cap_Can;
                        cantEnd = sedeCant.Cap_Can;
                    }

                    distRow = FindInTab_Dist("K", cantStart, cantEnd);
                    if (distRow == null)
                        //se non lo trova prova anche invertendo i dati
                        //ovvero prova anche a cercare in Tabella un Record "K"
                        //Per i Viaggi di Inizio Giornata
                        //  con Partenza = Cap_Can del CAN
                        //  con Arrivo = Domicilio_Cap_Col del Col
                        //Per i Viaggi di Fine Giornata
                        //  con Partenza = Domicilio_Cap_Col del Col
                        //  con Arrivo =  Cap_Can del CAN
                        distRow = FindInTab_Dist("K", cantEnd, cantStart);

                    if (distRow == default(Tab_Dist))
                    {
                        //se NON ha trovato nulla con Chiave "K"
                        //prova a cercare in Tabella un Record "P" 
                        //Per i Viaggi di Inizio Giornata
                        //  con Partenza = Domicilio_Luogo del Col
                        //  con Arrivo = Luogo del CAN
                        cantStart = sedeCant.Luogo_Can;
                        cantEnd = cant.Luogo_Can;

                        if (isRegU)
                        {
                            //Per i Viaggi di Fine Giornata
                            //  con Partenza = Luogo del CAN
                            //  con Arrivo = Domicilio_Luogo del Col
                            cantStart = cant.Luogo_Can;
                            cantEnd = sedeCant.Luogo_Can;
                        }

                        distRow = FindInTab_Dist("P", cantStart, cantEnd);
                        if (distRow == null)
                            //se non lo trova prova anche invertendo i dati
                            //ovvero prova anche a cercare in Tabella un Record "P"
                            //Per i Viaggi di Inizio Giornata
                            //  con Partenza = Luogo del CAN
                            //  con Arrivo = Domicilio_Luogo del Col
                            //Per i Viaggi di Fine Giornata
                            //  con Partenza = Domicilio_Luogo del Col
                            //  con Arrivo =  Luogo del CAN
                            distRow = FindInTab_Dist("P", cantEnd, cantStart);
                    }
                }

                // se non è stata trovata una tab distanze di tipo Z, K, P
                // allora si cerca di generarla da GIS
                if (distRow == null && ((TripAssignmentTypeEnum)startEndParam == TripAssignmentTypeEnum.Calculate || (TripAssignmentTypeEnum)startEndParam == TripAssignmentTypeEnum.CalculateFromHeadquarter))
                {
                    #region Generazione viaggi di inzio/fine giornata con tabella GIS

                    // viene ricercato nella tab_decod il record che identifica il tipo G per la tabella, se presente proseguo con l'elaborazione
                    var tdCant = RepoManager.Tab_DecodRepo.FirstOrDefault(td => td.Nome_Tab == "TIPO_DISTANZA" && td.Chiave_Tab == "G");
                    if (tdCant != null)
                    {
                        // calcolo gli indirizzi di entrata ed uscita da utilizzare nella ricerca nella tab distanza GIS
                        var startAddress = string.Format("{0}|{1}|{2}", sedeCant.Luogo_Can, sedeCant.Indirizzo_Can, sedeCant.Cap_Can);
                        var endAddress = string.Format("{0}|{1}|{2}", cant.Luogo_Can, cant.Indirizzo_Can, cant.Cap_Can);

                        #region Ricerca del viaggio nella tab distanze

                        //cerco il viaggio dalla partenza all'arrivo nella tab distanze
                        distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tdCant.Tab_Decod_Id && d.Partenza_Tab_Dist.ToUpper() == startAddress.ToUpper() && d.Arrivo_Tab_Dist.ToUpper() == endAddress.ToUpper());

                        //se non ho trovato il viaggio dalla partenza all'arrivo, provo dall'arrivo alla partenza
                        if (distRow == default(Tab_Dist))
                            distRow = RepoManager.Tab_DistRepo.FirstOrDefault(d => d.Tab_Decod_Id == tdCant.Tab_Decod_Id && d.Partenza_Tab_Dist.ToUpper() == endAddress.ToUpper() && d.Arrivo_Tab_Dist.ToUpper() == startAddress.ToUpper());

                        #endregion

                        // se non ho trovato la tabella distanze con la ricerca e altrimenti genero il viaggio da GIS (solo se il flag gis è attivo)
                        if (distRow == default(Tab_Dist) && RepoManager.ParamRepo.ParametersRow.Flag_GPS != 0)
                        {

                            #region GENERAZIONE VIAGGIO DA GIS

                            // inizializzo le variabili di inizio e fine viaggio
                            Coordinate startGeocodeResult = new Coordinate();
                            Coordinate endGeocodeResult = new Coordinate();

                            // se la partenza non ha latitudine o la longitudine la si cerca in base ai dati di ubicazione con BING e si approfitta per aggiornare
                            // con i dati ottenuti l'anagrafica cantiere
                            if (CommonService.Nz(sedeCant.LatitudineGps_Can, 0) == 0 || CommonService.Nz(sedeCant.LongitudineGps_Can, 0) == 0)
                            {
                                RepoManager.CantRepo.UpdateGeoLocation(sedeCant);
                            }

                            startGeocodeResult.Latitude = sedeCant.LatitudineGps_Can;
                            startGeocodeResult.Longitude = sedeCant.LongitudineGps_Can;

                            // se la destinazione del viaggio non ha la latitudine o la longitudine la si cerca in base di ubicazione con BING e si approfitta al contempo per
                            // aggiornare la relativa anagrafica
                            if (CommonService.Nz(cant.LatitudineGps_Can, 0) == 0 || CommonService.Nz(cant.LongitudineGps_Can, 0) == 0)
                            {
                                RepoManager.CantRepo.UpdateGeoLocation(cant);
                            }

                            endGeocodeResult.Latitude = cant.LatitudineGps_Can;
                            endGeocodeResult.Longitude = cant.LongitudineGps_Can;

                            // se sono disponibili LAT/LONG sia della partenza che della destinazione
                            // allora si calcola calcola con BING il percorso fra la partenza e l'arrivo ottenenedone i KM e la Durata da BING
                            if (CommonService.Nz(sedeCant.LatitudineGps_Can, 0) != 0 &&
                                CommonService.Nz(sedeCant.LongitudineGps_Can, 0) != 0 &&
                                CommonService.Nz(cant.LatitudineGps_Can, 0) != 0 &&
                                CommonService.Nz(cant.LongitudineGps_Can, 0) != 0)
                            {

                                // se il cantiere ha impostato la latitudine e la longitudine ma non ha un indirizzo, un cap e un luogo
                                // allora non si genera il record in tab distanze
                                if (CommonService.Nz(sedeCant.Indirizzo_Can, String.Empty) != String.Empty &&
                                    CommonService.Nz(sedeCant.Cap_Can, String.Empty) != String.Empty &&
                                    CommonService.Nz(sedeCant.Luogo_Can, String.Empty) != String.Empty &&
                                    CommonService.Nz(cant.Indirizzo_Can, String.Empty) != String.Empty &&
                                    CommonService.Nz(cant.Cap_Can, String.Empty) != String.Empty &&
                                    CommonService.Nz(cant.Luogo_Can, String.Empty) != String.Empty)
                                {
                                    var service = new CalcoloPercorsoService();
                                    var result = Task.Run(() => service.CalcolaPercorsoAsync(startGeocodeResult.Latitude, startGeocodeResult.Longitude, endGeocodeResult.Latitude, endGeocodeResult.Longitude)).Result;
                                    // calcolo del percorso tra partenza e arrivo da Bing
                                    Route routeResult = BusinessService.GetRoute(new Coordinate[] { startGeocodeResult, endGeocodeResult });


                                    // se bing è riuscito a calcolare i Km e la durate del viaggio allora si crea il corrispettivo record nella tabella distanze con tipo "G"
                                    if (routeResult != null)
                                    {

                                        distRow = new Tab_Dist()
                                        {
                                            Partenza_Tab_Dist = string.Format("{0}|{1}|{2}", sedeCant.Luogo_Can, sedeCant.Indirizzo_Can, sedeCant.Cap_Can),
                                            Arrivo_Tab_Dist = string.Format("{0}|{1}|{2}", cant.Luogo_Can, cant.Indirizzo_Can, cant.Cap_Can),
                                            Tab_Decod_Id = tdCant.Tab_Decod_Id,
                                            KM_Tab_Dist = (decimal)result.DistanzaKm,
                                            Minuti_Tab_Dist = Convert.ToInt32(result.DurataMinuti),
                                        };

                                        var errorTabDistRepo = RepoManager.Tab_DistRepo.Check(distRow, true);
                                        if (!errorTabDistRepo.Any())
                                            RepoManager.Tab_DistRepo.Add(distRow, true);
                                        else
                                        {
                                            BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                            List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                            foreach (var item in errorTabDistRepo)
                                                errRouteCalculate.Add(new KeyValuePair<string, string>(item.Key, String.Format("{0}", item.Value)));
                                            RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);
                                        }
                                    }
                                    else // se bing non è riuscito a calcolare il percorso allora si scrive un messaggio d'errore nella tab_messaggi con riferimento al route calculate
                                    {
                                        string descrizioneSede = String.Format("{0} - {1}", sedeCant.Codice_Cantiere, sedeCant.Descrizione_Can);
                                        string descrizioneCant = String.Format("{0} - {1}", cant.Codice_Cantiere, cant.Descrizione_Can);

                                        List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                        errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_NON_CALCOLABILE_DA_MAPPA, descrizioneSede, descrizioneCant)));
                                        RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);

                                        if (errRouteCalculate.Any())
                                            BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                    }
                                }
                                else
                                {
                                    // in caso sia presente la latitudine e la longitudine ma non siano presenti nei cantieri dati di ubicazione validi,
                                    // allora il tutto viene segnalato con un messaggio
                                    string descrizioneSede = String.Format("{0} - {1}", sedeCant.Codice_Cantiere, sedeCant.Descrizione_Can);
                                    string descrizioneCant = String.Format("{0} - {1}", cant.Codice_Cantiere, cant.Descrizione_Can);


                                    List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                    errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_NON_CALCOLABILE_DA_MAPPA, descrizioneSede, descrizioneCant)));
                                    RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);

                                    if (errRouteCalculate.Any())
                                        BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                                }
                            }
                            else // Se NON è risucito a Calcolare il Percorso con Bing allora scrive un messaggio di errore nella TAB_MESSAGGI con Riferimento RouteCalculate
                            {
                                string descrizioneSede = String.Format("{0} - {1}", sedeCant.Codice_Cantiere, sedeCant.Descrizione_Can);
                                string descrizioneCant = String.Format("{0} - {1}", cant.Codice_Cantiere, cant.Descrizione_Can);

                                List<KeyValuePair<String, String>> errRouteCalculate = new List<KeyValuePair<String, String>>();
                                errRouteCalculate.Add(new KeyValuePair<String, String>("_", BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_PERCORSO_VIAGGIO_DA_X_A_Y_NON_CALCOLABILE_DA_MAPPA, descrizioneSede, descrizioneCant)));
                                RepoManager.Tab_MessaggiRepo.InsertMessages(errRouteCalculate, application, FunctionMessageEnum.RouteCalculate, elaborateUserId, elaborateDateTime);

                                if (errRouteCalculate.Any())
                                    BusinessService.ElaborateTripsHasErrors[PowerWebContext.Current.User] = true;

                            }

                            #endregion

                        }

                    }

                    #endregion
                }
            }

            return distRow;
        }

        private Trip generateStartEndTrip(Cant currentCant, Col currentCol, Reg_V regE, TimeSpan threshold, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application, bool isRegU = false)
        //Generazione dei Viaggi di Inizio e Fine Giornata
        {
            Trip trip = null;
            //cercain Tab Distanze un Record valido con Chiave Z/K/P di Col/Cant (x Viaggio Inizio GG) e/o Cant/Col (x Viaggio Fine GG) 
            //e viceversa se non trovato
            var distRow = FindValidTab_Dist(currentCant, currentCol, isRegU, elaborateUserId, elaborateDateTime, application);

            if (distRow != null)
            {
                // viene generato il viaggio solamente se non ci sono impedimenti dalle personalizzazioni per i viaggi nello stesso comune
                if (!IsTripSameMunicipalityToDelete(distRow))
                {
                    var from = regE.Data_Reg.Value.Add(threshold);
                    var to = from.Date.AddDays(1);
                    if (threshold > DateTime.Now.Date.TimeOfDay)
                        to = to.Add(threshold);

                    Reg newRegE = RepoManager.RegRepo.Init();
                    Reg newRegU = RepoManager.RegRepo.Init();
                    newRegU.ParentReg = newRegE;

                    DateTime timeDiffFig;
                    DateTime timeDiffFis;

                    //Se la regv è un passaggio (non ha regU), per evitar rogne imposto la regU uguale alla regE
                    if (regE.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass)
                    {
                        regE.Data_Ora_Fig_U = regE.Data_Ora_Fig_E;
                        regE.Data_Ora_Fis_U = regE.Data_Ora_Fis_E;
                    }

                    if (!isRegU)
                    {
                        timeDiffFig = regE.Data_Ora_Fig_E.Value.Subtract(new TimeSpan(0, (int)distRow.Minuti_Tab_Dist, 0));
                        timeDiffFis = regE.Data_Ora_Fis_E.Subtract(new TimeSpan(0, (int)distRow.Minuti_Tab_Dist, 0));

                        timeDiffFig = new DateTime(timeDiffFig.Year, timeDiffFig.Month, timeDiffFig.Day, timeDiffFig.Hour, timeDiffFig.Minute, 59);
                        timeDiffFis = new DateTime(timeDiffFis.Year, timeDiffFis.Month, timeDiffFis.Day, timeDiffFis.Hour, timeDiffFis.Minute, 59);

                        newRegE.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;

                        newRegU.Registrazione_Data_Ora_Orig_Reg = regE.Data_Ora_Fis_E;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = regE.Data_Ora_Fis_E;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = regE.Data_Ora_Fig_E;
                    }
                    else
                    {
                        timeDiffFig = regE.Data_Ora_Fig_U.Value.Add(new TimeSpan(0, distRow.Minuti_Tab_Dist, 0));
                        timeDiffFis = regE.Data_Ora_Fis_U.Value.Add(new TimeSpan(0, distRow.Minuti_Tab_Dist, 0));

                        var tripUFisDateTime = new DateTime(regE.Data_Ora_Fis_U.Value.Year, regE.Data_Ora_Fis_U.Value.Month, regE.Data_Ora_Fis_U.Value.Day, regE.Data_Ora_Fis_U.Value.Hour, regE.Data_Ora_Fis_U.Value.Minute, 59);
                        var tripUFigDateTime = regE.Data_Ora_Fig_U;
                        if (tripUFigDateTime.HasValue)
                            tripUFigDateTime = new DateTime(regE.Data_Ora_Fis_U.Value.Year, regE.Data_Ora_Fis_U.Value.Month, regE.Data_Ora_Fis_U.Value.Day, regE.Data_Ora_Fig_U.Value.Hour, regE.Data_Ora_Fig_U.Value.Minute, 59);

                        newRegE.Registrazione_Data_Ora_Orig_Reg = tripUFisDateTime;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = tripUFisDateTime;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = tripUFigDateTime;

                        newRegU.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;
                    }

                    if (newRegE.Registrazione_Data_Ora_Fis_Reg > from && newRegU.Registrazione_Data_Ora_Fis_Reg < to)
                    {
                        newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                        newRegE.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                        newRegE.Col_Id = currentCol.Col_Id;
                        newRegE.Cant_Id = currentCant.Cant_Id;
                        newRegE.Att_Id = newRegE.Cant_Id;

                        newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                        newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                        newRegU.Col_Id = currentCol.Col_Id;
                        newRegU.Cant_Id = currentCant.Cant_Id;
                        newRegU.Att_Id = newRegU.Cant_Id;

                        if (distRow.KM_Tab_Dist > default(decimal))
                            newRegE.KM_Reg = distRow.KM_Tab_Dist;
                        else
                            newRegE.KM_Reg = 0;

                        int coupleNumber = CommonService.GetRandomNumber();
                        newRegE.RiferimentoRRN_Att = coupleNumber;
                        newRegU.RiferimentoRRN_Att = coupleNumber;

                        trip = new Trip { RegE = newRegE, RegU = newRegU };
                    }
                }
            }
            return trip;
        }


        /// <summary>
        /// Genera i viaggi di inizio/fine giornata dalla/alla sede.
        /// </summary>
        /// <param name="currentCant">The current cant.</param>
        /// <param name="currentCol">The current col.</param>
        /// <param name="regE">The reg e.</param>
        /// <param name="threshold">The threshold.</param>
        /// <param name="elaborateUserId">The elaborate user identifier.</param>
        /// <param name="elaborateDateTime">The elaborate date time.</param>
        /// <param name="application">The application.</param>
        /// <param name="isRegU">if set to <c>true</c> [is reg u].</param>
        /// <returns></returns>
        private Trip generateStartEndTripToHeadQuarter(Cant sede, Cant cant, Reg_V regE, TimeSpan threshold, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application, bool tripEnd = false)
        //Generazione dei Viaggi di Inizio e Fine Giornata
        {
            Trip trip = null;
            Col currentCol = RepoManager.ColRepo.SingleOrDefault(col => col.Col_Id == regE.Col_Id.Value);
            //cercain Tab Distanze un Record valido con Chiave Z/K/P di Col/Cant (x Viaggio Inizio GG) e/o Cant/Col (x Viaggio Fine GG) 
            //e viceversa se non trovato
            var distRow = FindCantCantTab_Dist(sede, cant, currentCol, tripEnd, elaborateUserId, elaborateDateTime, application);

            if (distRow != null)
            {
                // viene generato il viaggio solamente se non ci sono impedimenti dalle personalizzazioni per i viaggi nello stesso comune
                if (!IsTripSameMunicipalityToDelete(distRow))
                {
                    var from = regE.Data_Reg.Value.Add(threshold);
                    var to = from.Date.AddDays(1);
                    if (threshold > DateTime.Now.Date.TimeOfDay)
                        to = to.Add(threshold);

                    Reg newRegE = RepoManager.RegRepo.Init();
                    Reg newRegU = RepoManager.RegRepo.Init();
                    newRegU.ParentReg = newRegE;

                    DateTime timeDiffFig;
                    DateTime timeDiffFis;

                    if (!tripEnd)
                    {
                        // Imposto come ora di inizio viaggio il minuto precedente la regv
                        timeDiffFig = regE.Data_Ora_Fig_E.Value.AddMinutes(-1);
                        timeDiffFis = regE.Data_Ora_Fis_E.AddMinutes(-1);

                        timeDiffFig = new DateTime(timeDiffFig.Year, timeDiffFig.Month, timeDiffFig.Day, timeDiffFig.Hour, timeDiffFig.Minute, 59);
                        timeDiffFis = new DateTime(timeDiffFis.Year, timeDiffFis.Month, timeDiffFis.Day, timeDiffFis.Hour, timeDiffFis.Minute, 59);

                        newRegE.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;

                        // Imposto come ora di fine viaggio l'ora di entrata nel prossimo cantiere
                        newRegU.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;
                    }
                    else
                    {
                        timeDiffFig = regE.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? regE.Data_Ora_Fig_U.Value : regE.Data_Ora_Fig_E.Value;
                        timeDiffFis = regE.Registrazione_Tipo_Reg != (int)RegTypeEnum.Pass ? regE.Data_Ora_Fis_U.Value : regE.Data_Ora_Fis_E;

                        timeDiffFig = new DateTime(timeDiffFig.Year, timeDiffFig.Month, timeDiffFig.Day, timeDiffFig.Hour, timeDiffFig.Minute, 59);
                        timeDiffFis = new DateTime(timeDiffFis.Year, timeDiffFis.Month, timeDiffFis.Day, timeDiffFis.Hour, timeDiffFis.Minute, 59);

                        newRegE.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegE.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;

                        newRegU.Registrazione_Data_Ora_Orig_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fis_Reg = timeDiffFis;
                        newRegU.Registrazione_Data_Ora_Fig_Reg = timeDiffFig;
                    }

                    if (newRegE.Registrazione_Data_Ora_Fis_Reg > from && newRegU.Registrazione_Data_Ora_Fis_Reg < to)
                    {
                        if (!tripEnd)
                        {
                            newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegE.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegE.Col_Id = regE.Col_Id;
                            //Se sto elaborando il viaggio di inizio giornata, parte dalla sede
                            newRegE.Cant_Id = sede.Cant_Id;
                            newRegE.Att_Id = newRegE.Cant_Id;

                            newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegU.Col_Id = regE.Col_Id;
                            //Se sto elaborando il viaggio di inizio giornata, arriva al primo cantiere lavorativo
                            newRegU.Cant_Id = cant.Cant_Id;
                            newRegU.Att_Id = newRegU.Cant_Id;
                        }

                        else
                        {
                            newRegE.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegE.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegE.Col_Id = regE.Col_Id;
                            //Se sto elaborando il viaggio di fine giornata, parte dall'ultimo cantiere lavorativo
                            newRegE.Cant_Id = cant.Cant_Id;
                            newRegE.Att_Id = newRegE.Cant_Id;

                            newRegU.Registrazione_Tipo_RegEnum = RegTypeEnum.Trip;
                            newRegU.Registrazione_Stato_RegEnum = RegStateEnum.Ass;
                            newRegU.Col_Id = regE.Col_Id;
                            //Se sto elaborando il viaggio di fine giornata, arriva alla sede
                            newRegU.Cant_Id = sede.Cant_Id;
                            newRegU.Att_Id = newRegU.Cant_Id;
                        }

                        if (distRow.KM_Tab_Dist > default(decimal))
                            newRegE.KM_Reg = distRow.KM_Tab_Dist;
                        else
                            newRegE.KM_Reg = 0;

                        int coupleNumber = CommonService.GetRandomNumber();
                        newRegE.RiferimentoRRN_Att = coupleNumber;
                        newRegU.RiferimentoRRN_Att = coupleNumber;

                        trip = new Trip { RegE = newRegE, RegU = newRegU };
                    }
                }
            }
            return trip;
        }

        private List<Trip> ElaborateStartEndTrips(IOrderedEnumerable<Reg_V> orderedCurrentTripsByColByDate, Col currentCol, int? elaborateUserId, DateTime? elaborateDateTime, ApplicationMessageEnum application)
        //Elabora i Viaggi di Inizio/Fine Giornata (in base ai FLAG_ORE_VIAGGI_INIZIO_FINE di PARAM e al FLAG_ORE_VIAGGI_COL_INIZIO_FINE)
        {
            List<Trip> tripList = new List<Trip>();

            if (orderedCurrentTripsByColByDate.Any())
            {
                Reg_V regE = orderedCurrentTripsByColByDate.ElementAt(0);

                Reg_V regU = orderedCurrentTripsByColByDate.ElementAt(orderedCurrentTripsByColByDate.Count() - 1);

                var threshold = DateTime.Now.Date.TimeOfDay;
                if (RepoManager.ParamRepo.ParametersRow.Abilita_Notturno && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.None && RepoManager.ParamRepo.ParametersRow.TipoNotturno != (int)NocturneTypeEnum.Disabled)
                {
                    if (RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Gruppo_Notte_Ril.HasValue)
                        threshold = RepoManager.ParamRepo.ParametersRow.Default_Durata_Max_Gruppo_Notte_Ril.Value;

                    if (currentCol.TipoNotturno_Col != (int)NocturneTypeEnum.None && currentCol.Durata_Max_Gruppo_Notte_Ril_Col.HasValue)
                        threshold = currentCol.Durata_Max_Gruppo_Notte_Ril_Col.Value;
                }

                var firstCant = RepoManager.CantRepo.Single(c => c.Cant_Id == regE.Cant_Id, true);
                var lastCant = RepoManager.CantRepo.Single(c => c.Cant_Id == regU.Cant_Id, true);

                //Se il collaboratore ha il flag valorizzato, prendo il suo altrimenti lo prendo dai parametri generali
                int tipo_Assegnazione_KMMinuti_Inizio_Fine_G = currentCol.Flag_Viaggio_InizioFine_GIS.HasValue ? currentCol.Flag_Viaggio_InizioFine_GIS.Value : RepoManager.ParamRepo.ParametersRow.Tipo_Assegnazione_KMMinuti_Inizio_Fine_G;

                Cant sede = new Cant();
                //Se il parametro dei viaggio inizio/fine giornata è = 3 (viaggia da/per la sede), calcola i viaggi dalla sede
                if (tipo_Assegnazione_KMMinuti_Inizio_Fine_G == (int)TripAssignmentTypeEnum.CalculateFromHeadquarter)
                {
                    /* --- GESTIONE VIAGGIO INIZIO GIORNATA --- */

                    //Se la prima timbratura di giornata è stata fatta nella sede, prendo come primo cantiere quello della seconda regv di giornata
                    if (firstCant.Tipo_Cantiere_Can == "SEDE")
                    {
                        sede = firstCant;
                        //Se ho più di una regv, vado a prendere la seconda
                        if (orderedCurrentTripsByColByDate.Count() > 1)
                        {
                            regE = orderedCurrentTripsByColByDate.ElementAt(1);
                            firstCant = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == regE.Cant_Id, true);
                        }
                    }

                    else
                    {
                        //Recupero il cantiere marcato come sede. PUO' ESSERE SOLO UNO PER CLIENTE!
                        var headquartiers = RepoManager.CantRepo.Find(c => c.Tipo_Cantiere_Can == "SEDE");
                        if (headquartiers.Count() == 1)
                        {
                            sede = headquartiers.ElementAt(0);
                        }
                        else
                        {
                            _log.Error("Durante la generazione dei viaggi sono stati trovati più cantieri marcati come SEDE");
                        }
                    }

                    //Se non mi trovo in sede (quindi la prima o la seconda timbratura non sono state fatte in sede), calcolo il viaggio di inizio giornata
                    if (firstCant.Tipo_Cantiere_Can != "SEDE" && firstCant != default(Cant))
                    {
                        var firstTrip = generateStartEndTripToHeadQuarter(sede, firstCant, regE, threshold, elaborateUserId, elaborateDateTime, application);
                        if (firstTrip != null)
                        {
                            tripList.Add(firstTrip);
                        }
                    }


                    /* --- GESTIONE VIAGGIO FINE GIORNATA --- */
                    //Se l'ultima timbratura di giornata è stata fatta nella sede, prendo come ultimo cantiere quello della penultima regv di giornata
                    if (lastCant.Tipo_Cantiere_Can == "SEDE")
                    {
                        //Se ho più di una regv, vado a prendere la penultima
                        if (orderedCurrentTripsByColByDate.Count() > 1)
                        {
                            regU = orderedCurrentTripsByColByDate.ElementAt(orderedCurrentTripsByColByDate.Count() - 2);
                            lastCant = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == regU.Cant_Id, true);
                        }
                    }

                    //Se non mi trovo in sede (quindi l'ultima o la penultima timbratura non sono state fatte in sede), calcolo il viaggio di fine giornata
                    if (lastCant.Tipo_Cantiere_Can != "SEDE" && lastCant != default(Cant))
                    {
                        var lastTrip = generateStartEndTripToHeadQuarter(sede, lastCant, regU, threshold, elaborateUserId, elaborateDateTime, application, true);
                        if (lastTrip != null)
                        {
                            tripList.Add(lastTrip);
                        }
                    }
                }

                else
                {
                    var firstTrip = generateStartEndTrip(firstCant, currentCol, regE, threshold, elaborateUserId, elaborateDateTime, application);
                    if (firstTrip != null)
                        tripList.Add(firstTrip);

                    var lastTrip = generateStartEndTrip(lastCant, currentCol, regU, threshold, elaborateUserId, elaborateDateTime, application, true);
                    if (lastTrip != null)
                        tripList.Add(lastTrip);
                }
            }

            return tripList;
        }
        #endregion

        /// <summary>
        /// Recupera la funzione utilizzata per filtrare i dati del repository corrente.
        /// </summary>
        /// <value>
        /// La funzione utilizzata per filtrare i dati del repository corrente.
        /// </value>
        public override System.Linq.Expressions.Expression<Func<Reg_V, bool>> Filter
        {
            get
            {
                // per il repository delle reg_v viene applicato il filtro (se richiesto e correttamente configurato) per utente/collaboratore e responsabile e filiale

                var filter = base.Filter;

                // se è necessario applicare dei filtri al repository
                if (BusinessService.IsToApplyDomainFilter())
                {
                    // i filtri sono gerarchicamente strutturati:
                    // 1- utente/cliente o utente/collaboratore
                    // 2- utente/filiale o utente/responsabile
                    // La presenza di uno dei filtri di cui al punto 1 esclude quelli del punto due.
                    // In ogni caso i filtri per utente/cliente e utente/collaboratore sono mutuamente esclusivi (non posso sussistere contemporaneamente)

                    // se per l'utente è specificato un id collaboratore è richiesto di applicare un filtro per quel dato
                    if (PowerWebContext.Current.User.Col_Id.HasValue)
                    {
                        filter = regv => regv.Col_Id == PowerWebContext.Current.User.Col_Id;
                    }
                    else if (PowerWebContext.Current.User.Cli_Id.HasValue)
                    {
                        filter = regv => regv.Cli_Id == PowerWebContext.Current.User.Cli_Id || regv.Att_Cli_Id == PowerWebContext.Current.User.Cli_Id;
                    }
                    else if (RepoManager.ParamRepo.ParametersRow.DomainFilterEnum != DomainFilterEnum.None) // altriementi si tenta di applicare, se configurato, i filtre per filiale e/o responsabile
                    {
                        var allRespIds = PowerWebContext.Current.Resps.Select(resp => resp.Resp_Id);
                        var allFilIds = PowerWebContext.Current.Fils.Select(fil => fil.Fil_Id);

                        filter = regv => (regv.Fil_Id == null || allFilIds.Contains(regv.Fil_Id.Value)) && (regv.Resp_Id == null || allRespIds.Contains(regv.Resp_Id.Value));
                    }
                }

                return filter;
            }
        }

        /// <summary>
        /// Recupera la stringa utilizzata dal repository per filtrare i dati nelle query testuali.
        /// </summary>
        /// <value>
        /// La stringa utilizzata dal repository per filtrare i dati nelle query testuali.
        /// </value>
        public string FilterText
        {
            get
            {
                // viene applicato il filtro (se richiesto e correttamente configurato) per utente/collaboratore e responsabile e filiale

                string filter = String.Empty;

                // se è necessario applicare dei filtri al repository 

                //se lo user che ha effettuato l'accesso in powerweb ha i filtri di filiale/resp attivi all
                if (BusinessService.IsToApplyDomainFilter() && PowerWebContext.Current.User.Liv_Utente != 12)
                {
                    // i filtri sono gerarchicamente strutturati:
                    // 1- utente/cliente o utente/collaboratore
                    // 2- utente/filiale o utente/responsabile
                    // La presenza di uno dei filtri di cui al punto 1 esclude quelli del punto due.
                    // In ogni caso i filtri per utente/cliente e utente/collaboratore sono mutuamente esclusivi (non posso sussistere contemporaneamente)

                    // se per l'utente è specificato un id collaboratore è richiesto di applicare un filtro per quel dato
                    if (PowerWebContext.Current.User.Col_Id.HasValue)
                    {
                        filter = String.Format("Col_Id = {0}", PowerWebContext.Current.User.Col_Id);
                    }
                    else if (PowerWebContext.Current.User.Cli_Id.HasValue)
                    {
                        filter = String.Format("(Cli_Id = {0} OR Att_Cli_Id = {0})", PowerWebContext.Current.User.Cli_Id);
                    }
                    else if (RepoManager.ParamRepo.ParametersRow.DomainFilterEnum != DomainFilterEnum.None) // altriementi si tenta di applicare, se configurato, i filtre per filiale e/o responsabile
                    {
                        //vengopno estratti i tutti responsabili
                        var allRespIds = PowerWebContext.Current.Resps.Select(resp => resp.Resp_Id);
                        var allFilIds = PowerWebContext.Current.Fils.Select(fil => fil.Fil_Id);

                        //viene estratto l'ide dello user che ha fatto l'accesso a Powerweb
                        int userId = PowerWebContext.Current.User.Utenti_Id;

                        //filtro solo i responsabili che corrispondo all'utente che ha effettuato l'accesso 
                        var userRespIds = RepoManager.Utenti_RespRepo.Find(r => r.Utenti_Id == userId).ToList();

                        //filtro solo le filiali che corrispondo all'utente che ha effettuato l'accesso 
                        var userFilIds = RepoManager.Utenti_FilRepo.Find(r => r.Utenti_Id == userId).ToList();

                        //inizializzazione della stringa che compone il filtro
                        StringBuilder tmpFilter = new StringBuilder();

                        //se ho delle filiali E dai parametri è richiesta la gestione delle filiali o entrambi allora viene fatto un filtro per le filiali
                        if (allFilIds.Any() && (RepoManager.ParamRepo.ParametersRow.DomainFilterEnum == DomainFilterEnum.Fil) && PowerWebContext.Current.User.Liv_Utente < 10)
                        
                        {
                            //istanzia la stringa che costituirà il filtro 
                            tmpFilter.Append("(Fil_Id != null");

                            //viene cilato per ogni responabile
                            foreach (int fillId in allFilIds)
                            {
                                if (userFilIds.Any())
                                    foreach (var userfill in userFilIds)
                                    {
                                        //se lo user che ha fatto l'accesso è fra i responsabili allora le RegV vengono filtrate su esso
                                        if (userfill.Fil_Id == fillId)
                                            //nel filtro viene fatta una or fra tutti i responsabili (anche quelli null)
                                            tmpFilter.AppendFormat(" OR Fil_Id = {0}", fillId);
                                    }
                            }

                        }
                        //se ho dei responsabili E dai parametri è richiesta la gestione dei responsabili 
                        else if (allRespIds.Any() && (RepoManager.ParamRepo.ParametersRow.DomainFilterEnum == DomainFilterEnum.Resp) && PowerWebContext.Current.User.Liv_Utente < 10)
                        {
                            //nel caso si voglia gestire solo il responsabile devo ottenere tutti i reponsabili con resp diverso da 
                            tmpFilter.Append("(Resp_Id != null");

                            //viene cilato per ogni responabile
                            foreach (int respId in allRespIds)
                            {
                                if (userRespIds.Any())
                                    foreach (var userResp in userRespIds)
                                    {
                                        //se lo user che ha fatto l'accesso è fra i responsabili allora le RegV vengono filtrate su esso
                                        if (userResp.Resp_Id == respId)
                                            //nel filtro viene fatta una or fra tutti i responsabili (anche quelli null)
                                            tmpFilter.AppendFormat(" OR Resp_Id = {0}", respId);
                                    }
                            }

                        }
                        //nel caso di gestione di filielae e responabili contemporaneamente
                        else if ((allRespIds.Any() && allFilIds.Any()) && RepoManager.ParamRepo.ParametersRow.DomainFilterEnum == DomainFilterEnum.Both)
                        {
                            //nel caso si voglia gestire solo il responsabile devo ottenere tutti i reponsabili con resp diverso da 
                            tmpFilter.Append("(Resp_Id != null");

                            //viene cilato per ogni responabile
                            foreach (int respId in allRespIds)
                            {
                                if (userRespIds.Any())
                                    foreach (var userResp in userRespIds)
                                    {
                                        //se lo user che ha fatto l'accesso è fra i responsabili allora le RegV vengono filtrate su esso
                                        if (userResp.Resp_Id == respId)
                                            //nel filtro viene fatta una or fra tutti i responsabili (anche quelli null)
                                            tmpFilter.AppendFormat(" OR Resp_Id = {0}", respId);
                                    }
                            }
                            //nel caso si voglia gestire solo il responsabile devo ottenere tutti i reponsabili con resp diverso da 
                            tmpFilter.Append(") OR (");

                            //istanzia la stringa che costituirà il filtro 
                            tmpFilter.Append("Fil_Id != null");

                            //viene cilato per ogni responabile
                            foreach (int fillId in allFilIds)
                            {
                                if (userFilIds.Any())
                                    foreach (var userfill in userFilIds)
                                    {
                                        //se lo user che ha fatto l'accesso è fra i responsabili allora le RegV vengono filtrate su esso
                                        if (userfill.Fil_Id == fillId)
                                            //nel filtro viene fatta una or fra tutti i responsabili (anche quelli null)
                                            tmpFilter.AppendFormat(" OR Fil_Id = {0}", fillId);
                                    }
                            }


                        }
                        //nel caso di gestione sia di responsabili che di filiali ma si abbia associato all' utente solo i responsabili
                        else if (allRespIds.Any() && RepoManager.ParamRepo.ParametersRow.DomainFilterEnum == DomainFilterEnum.Both) {
                            //nel caso si voglia gestire solo il responsabile devo ottenere tutti i reponsabili con resp diverso da 
                            tmpFilter.Append("(Resp_Id != null");

                            //viene cilato per ogni responabile
                            foreach (int respId in allRespIds)
                            {
                                if (userRespIds.Any())
                                    foreach (var userResp in userRespIds)
                                    {
                                        //se lo user che ha fatto l'accesso è fra i responsabili allora le RegV vengono filtrate su esso
                                        if (userResp.Resp_Id == respId)
                                            //nel filtro viene fatta una or fra tutti i responsabili (anche quelli null)
                                            tmpFilter.AppendFormat(" OR Resp_Id = {0}", respId);
                                    }
                            }
                        }
                        //nel caso di gestione sia di responsabili che di filiali ma si abbia associato all' utente solo le filiali
                        else if (allFilIds.Any() && RepoManager.ParamRepo.ParametersRow.DomainFilterEnum == DomainFilterEnum.Both)
                        {
                            //istanzia la stringa che costituirà il filtro 
                            tmpFilter.Append("(Fil_Id != null");

                            //viene cilato per ogni responabile
                            foreach (int fillId in allFilIds)
                            {
                                if (userFilIds.Any())
                                    foreach (var userfill in userFilIds)
                                    {
                                        //se lo user che ha fatto l'accesso è fra i responsabili allora le RegV vengono filtrate su esso
                                        if (userfill.Fil_Id == fillId)
                                            //nel filtro viene fatta una or fra tutti i responsabili (anche quelli null)
                                            tmpFilter.AppendFormat(" OR Fil_Id = {0}", fillId);
                                    }
                            }
                        }
                        if (tmpFilter.Length != 0)
                            tmpFilter.Append(")");

                        filter = tmpFilter.ToString();
                    }
                }


                return filter;
            }
        }

        public override Dictionary<string, string> Check(Reg_V entity, bool isNew = false, bool isResetSession = true)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //Verifico che ci sia almeno una delle due Ore di E e/o di U altrimenti NON faccio altri controlli inutili
            // (questo controllo non si effettua per le registrazioni di tipo solo durata)
            if (entity.Registrazione_Tipo_Reg != (int)RegTypeEnum.Duration && entity.Data_Ora_Fis_E.TimeOfDay == TimeSpan.Zero && (!entity.Data_Ora_Fis_U.HasValue || (entity.Data_Ora_Fis_U.HasValue && entity.Data_Ora_Fis_U.Value.TimeOfDay == TimeSpan.Zero)))
                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Ora_Fis_U),
                                                     BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_ORA_FIS_E_U_UGUALI_A_MINVALUE));
            else
            {
                //PER PRIMA COSA VERIFICO CHE LA DATA DELLA REGISTRAZIONE FISICA NON SIA MINORE/UGUALE ALLA DATA DI BLOCCO (se impostata)
                //Nel caso in cui lo sia segnalo errore perchè NON posso toccarla
                var filterDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg;
                if (filterDate.HasValue)
                {
                    //filterDate = filterDate.Value.AddDays(1);
                    //regs = regs.Where(reg => reg.Registrazione_Data_Ora_Fis_Reg >= filterDate.Value).ToList();
                    if (CommonService.Nz(entity.Data_Reg, new DateTime(1, 1, 1)) <= filterDate)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Reg),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_REG_MINORE_DI_DATA_BLOCCO));
                }

                // viene recuperato il cantiere collegato alla reg_v (utilizzato su più controlli)
                var currentCant = RepoManager.CantRepo.SingleOrDefault(cant => cant.Cant_Id == entity.Cant_Id);



                // Se si sta elaborando una reg_v che ha una ora di uscita ed è collegata a un cantiere attività 
                // allora viene generato un errore di check; in caso contrario si procede correttamente con le verifiche
                if (entity.Data_Ora_FigFis_U != null) // se è presente la data/ora di uscita
                {
                    if (entity.Cant_Id != null) // se è presente un cantiere
                    {                        // se il cantiere è stato trovato
                        if (currentCant != null)
                        {
                            // se il cantiere è un'attività
                            if (currentCant.IsActivity)
                            {
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Ora_Fis_U),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_ATTIVITA_NO_ORA_USCITA));
                            }
                        }
                    }
                }

                // controlli per un'eventuale registrazione di sola durata
                if (entity.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration)
                {
                    // deve essere valorizzata la durata
                    if (CommonService.Nz(entity.Durata_Fis_HH_S, String.Empty) == String.Empty)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Fis),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_DURATA_OBBLIGATORIA_PER_REGISTRAZIONI_SOLO_DURATA));
                    }

                    // non devono essere valorizzate le ore di entrata e di uscita
                    if (CommonService.Nz(entity.Data_Ora_Fis_E.TimeOfDay, TimeSpan.Zero) != TimeSpan.Zero || CommonService.Nz(entity.Data_Ora_Fis_U, DateTime.MinValue) != DateTime.MinValue)
                    {
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Durata_Fis),
                            BusinessService.GetLocalizedString(PowerWebResources.ERR_ORA_ENTRATA_E_ORA_USCITA_NON_VALORIZZABILI_SE_REGISTRAZIONE_SOLA_DURATA));
                    }
                }

                // controlli per un'eventuale registrazione bloccata
                if (entity.Registrazione_Bloccata)
                {
                    // se la reg_v passata come parametro risulta bloccata e non viene da una situazione di sblocco
                    // allora blocco qualsiasi tipo di modifica (per modificarla è necessario sbloccarla)
                    if (entity.RegE != 0) // se non si tratta di una registrazione nuova
                    {
                        // viene recuperata la vecchia reg_v
                        Reg_V oldRegV = RepoManager.Reg_VRepo.SingleOrDefault(regv => regv.RegE == entity.RegE);

                        // se anche la vecchia reg_v risulta bloccata significa che non la sto bloccando per la prima volta e quindi blocco la modifica
                        if (oldRegV.Registrazione_Bloccata)
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Ora_Fis_U),
                                BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_MODIFICA_REG_BLOCCATA));

                    }

                    // se si sta cercando di bloccare una reg nuova (cioé non ancora scritta) allora viene visualizzato un errore
                    if (entity.RegE == 0)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Bloccata),
                       BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_BLOCCAGGIO_REG_NUOVA));
                }

                // lo stato delle attività collegate ad una reg deve essere coerente e
                // non deve essere possibile inserire ore di cantieri ONL con motivazione
                if (currentCant != null) // se è presente un cantiere
                {
                    // se il cantiere è un'attività
                    if (currentCant.IsActivity)
                    {
                        // se la reg risulta accoppiata
                        if (entity.RiferimentoRRN_Att != null)
                        {
                            // recupero la reg collegata all'attività
                            var parentReg = RepoManager.RegRepo.SingleOrDefault(reg => reg.Reg_Id == entity.RiferimentoRRN_Att);

                            // se la reg di riferimento dell'attività è bloccata viene generato un errore
                            if (parentReg != null)
                                if (parentReg.Registrazione_Bloccata != entity.Registrazione_Bloccata)
                                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Registrazione_Bloccata),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_STATO_BLOCCO_DIFFORME_DA_REG_ABBINATA));

                        }
                    }

                    // se il cantiere collegato è di tipo ONL allora la motivazione non deve essere valorizzata;
                    // in caso si verifichi questa condizione si ritorna un errore
                    if (currentCant.Tipo_Cantiere_Can == "ONL")
                    {
                        if (entity.Motivazione_Reg_Id != null)
                        {
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Motivazione_Reg_Id), BusinessService.GetLocalizedString(PowerWebResources.ERR_NO_ONL_CON_MOTIVAZIONI));
                        }
                    }
                }

                // se è attiva la valutazione dell'attività
                int activityEvalCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.EnableActivityEvaluationEnum);
                if (activityEvalCustomization == (int)EnableActivityEvaluationEnum.Enabled)
                {
                    // si può valutare solamente una registrazione di tipo attività
                    if (entity.Activity_Evaluation.HasValue && entity.Registrazione_Tipo_Reg != (int)RegTypeEnum.Att)
                        result.AddOrAppend(CommonService.GetPropertyName(() => entity.Activity_Evaluation),
                                  BusinessService.GetLocalizedString(PowerWebResources.ERR_SOLO_ATTIVITA_VALUTATE));
                }

                // eseguo i check custom (personalizzati in base al tipo modulo del cliente)
                var customErrorDictionary = CustomChecks(entity, isNew);

                // se sono stati ritornati degli errori allora li aggiungo al dizionario di ritorno
                if (customErrorDictionary.Count > 0)
                    customErrorDictionary.ForEach(errVal => result.AddOrAppend(errVal.Key, errVal.Value));

                if (entity.Data_Ora_Fis_E != DateTime.MinValue &&
                    entity.Data_Ora_Fis_U.HasValue &&
                    entity.Data_Ora_Fis_U != DateTime.MinValue &&
                    entity.Motivazione_Reg_Id == null)
                {
                    if (entity.Data_Ora_Fis_U >= entity.Data_Ora_Fis_E)
                    //Nel caso in cui non sia a cavallo del giorno verifico che la durata sia Maggiore di 1 MINUTO
                    {
                        //viene determinata la durata della registrazione abbianta (cioè presenta sia ora di entrata che di uscita)
                        var timespan = entity.Data_Ora_Fis_U - entity.Data_Ora_Fis_E;

                        //se è attiva la personalizzazione di chiususra delle timbrature sulla sede
                        if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresEnum) == (int)AutoClosuresEnum.Sede || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresFirstLast) == 1 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosures) == 1 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresXMinuteEnum) == 1 || RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.AutoClosuresAfterXEnum) == 1)
                        {
                            //viene controllato che la durata non sia negativa ma può essere 0
                            if (timespan == null || timespan < new TimeSpan(0, 0, 0))
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Ora_Fis_U),
                                    BusinessService.GetLocalizedString(
                                        PowerWebResources.ERR_DURATA_REG_FIS_MINORE_DI_UN_MINUTO));

                        }
                        //se la peronalizzazione non è attiva la durata minima per una timbratura è di 1 minuto
                        else
                        {

                            if (timespan == null || timespan < new TimeSpan(0, 1, 0))
                                result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Ora_Fis_U),
                                    BusinessService.GetLocalizedString(
                                        PowerWebResources.ERR_DURATA_REG_FIS_MINORE_DI_UN_MINUTO));

                        }

                        if (timespan != null && timespan > new TimeSpan(23, 59, 00))
                            result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Ora_Fis_U),
                                BusinessService.GetLocalizedString(
                                    PowerWebResources.ERR_DURATA_REG_FIS_MAGGIORE_DI_23_59));

                    }
                }
            }
            WriteCheckLog(entity, result, Log);
            return result;
        }

        public override Dictionary<string, string> CheckBeforeDelete(Reg_V entity)
        {
            // inizializzazione del valore di ritorno della check
            var errorDic = new Dictionary<string, string>();

            // si procede all'elaborazione solamente se la registrazione passata come parametro è valorizzata
            if (entity != null)
            {
                // non è possibile cancellare reg_v antecedenti alla data blocco
                DateTime blockedDate = RepoManager.ParamRepo.ParametersRow.Data_Blocco_Reg ?? DateTime.MinValue;
                if (entity.Data_Reg < blockedDate)
                    errorDic.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Reg),
                                                     BusinessService.GetLocalizedString(PowerWebResources.ERR_DATA_REG_MINORE_DI_DATA_BLOCCO));
            }
            // ritorno del valore calcolato dal metodo
            return errorDic;
        }


        /*
        * Controlla se la Reg_V entity è già presente nel database.
        * Una Reg_V viene considerata già presente se nel database ne esiste una con Col_Id, Cant_ID e Data_Ora_Fis_E uguali.
        * Ritorna un boolean: true è già presente, false altrimenti.
        */
        public Boolean CheckAlreadyPresent(Reg_V entity)
        {
            // cerca nel db una Reg_V con Col_Id, Cant_ID e Data_Ora_Fis_E uguali alla Reg_V in questione
            Reg_V isPresent = Find(regV => regV.Col_Id == entity.Col_Id && regV.Cant_Id == entity.Cant_Id && regV.Data_Ora_Fis_E == entity.Data_Ora_Fis_E).ElementAtOrDefault(0);

            if (isPresent != null)
            {
                return true;
            }

            return false;
        }

        private Dictionary<string, string> CustomChecks(Reg_V entity, bool isNew)
        {
            // inizializzazione del dizionario di ritorno degli errori del metodo
            var result = new Dictionary<string, string>();
            CustomChecksEnum customCheckCustomization = 0;

            //calcolo del valore della personalizzazione
            customCheckCustomization = (CustomChecksEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.CustomChecksEnum);

            //controllo se ho attiva la personalizzazione
            if (customCheckCustomization != 0)
            {
                // --- Inizio controllo da effettuare per il solo cliente Mosaico ---
                // se si sta elaborando una check per il cliente Mosaico
                if (customCheckCustomization == CustomChecksEnum.Mosaico)
                {
                    // sono calcolate le date da recuperare per il controllo di sovrapposizione utilizzando il valore del notturno
                    DateTime currentDate = new DateTime(entity.Data_Ora_Fis_E.Year, entity.Data_Ora_Fis_E.Month, entity.Data_Ora_Fis_E.Day);
                    DateTime startCheckDate = currentDate;
                    DateTime endCheckDate = currentDate.AddDays(1);

                    // gestione delle date di inizio/fine periodo in base alla configurazione del notturno
                    BusinessService.ManageNocturneStartEndDate(ref startCheckDate, ref endCheckDate);

                    // inizializzazione della lista che conterrà le reg_v su cui effettuare il controllo di overlaps
                    // con tutte le reg_v presenti nel giorno della reg_v che si sta processando per quello specifico collaboratore, gestendo solo le ore
                    var regVsToCheckOverlaps = Find(regV => regV.Data_Reg >= startCheckDate && regV.Data_Reg < endCheckDate && regV.Col_Id == entity.Col_Id &&
                        regV.Registrazione_Tipo_Reg == (int)RegTypeEnum.None, true).ToList();

                    // se si sta elaborando reg_v non nuova allora la si toglie dalla lista
                    if (!isNew)
                        regVsToCheckOverlaps = regVsToCheckOverlaps.Where(regV => regV.RegE != entity.RegE).ToList();

                    // in ogni caso alla lista di cui controllare l'overlaps aggiungo il nuovo valore attualmente in elaborazione
                    regVsToCheckOverlaps.Add(entity);

                    // si esegue il controllo di sovrapposizione e si recupera il dizionario con gli errori
                    var overlapsErrors = CheckOverlaps(regVsToCheckOverlaps, true);

                    // se ci sono stati degli errori allora li aggiugo all'attuale dizionario di errori
                    if (overlapsErrors.Count > 0)
                        foreach (var ovError in overlapsErrors)
                            result.AddOrAppend(ovError.Key, ovError.Value);

                }
                // --- Fine controllo da effettuare per il solo cliente Mosaico ---

            }



            // --- Inizio controllo sulla data di registrazione ---
            PreventFutureRegEnum customCheckCustomizationPreventFutureRegEnum = PreventFutureRegEnum.Allow;

            //Recupera il valore della personalizzazione
            customCheckCustomizationPreventFutureRegEnum = (PreventFutureRegEnum)RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.PreventFutureRegEnum);

            //se la personalizzazione è attiva
            if (customCheckCustomizationPreventFutureRegEnum == PreventFutureRegEnum.Prevent)
            {
                DateTime currDate = DateTime.Today;
                //Recupera il numero di giorni entro il quale una registrazione è valida
                int dayFromNow = Convert.ToInt32(RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.PreventFutureRegEnum, "NumDays"));
                //Calcola la data massima entro la quale una registrazione è valida
                DateTime limitDate = currDate.AddDays(dayFromNow);

                //Se bisogna fare il controllo anche sulla fine del mese
                if (RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.PreventFutureRegEnum, "EndOfMonth") == "1")
                {
                    //Calcola l'ultimo giorno del mese corrente
                    DateTime endMonth = CommonService.GetLastMonthDay(currDate);
                    //Tiene la data più vicina
                    limitDate = endMonth < limitDate ? endMonth : limitDate;
                }

                //Se la data è troppo lontana, registra l'errore
                CultureInfo userCulture = PowerWebContext.Current.UserCultureInfo;
                string limitDateString = limitDate.ToString("d", userCulture ?? CultureInfo.CurrentUICulture);

                if (entity.Data_Reg > limitDate)
                {
                    result.AddOrAppend(CommonService.GetPropertyName(() => entity.Data_Reg),
                           BusinessService.GetLocalizedStringStrParam(PowerWebResources.ERR_CAMPO_X_DEVE_ESSERE_MINORE_UGUALE_Y,
                           BusinessService.GetLocalizedString(String.Format("FLD_{0}", CommonService.GetPropertyName(() => entity.Data_Reg).ToUpper())),
                           limitDateString));
                }
            }

            // --- Fine controllo sulla data di registrazione ---

            // ritorno del valore degli errori recuperati dal metodo
            return result;
        }

        public override Dictionary<string, string> CheckForImport(Reg_V entity)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            //AGGIUNGO GLI EVENTUALI VALORI PRESENTI NEI CAMPI DELLA TAB REG CHE RICEVO e CHE NON SONO GIA' PRESENTI IN TAB_DECOD
            // PER UN EVENTUALE DISALLINEAMENTO FRA I DATI DEI RECORD DELLA TABELLA REG e LE TAB_DECOD di ACCESS
            // LO FACCIO PER LE TABELLE:
            //                      MOTIVAZIONI            

            if (CommonService.Nz(entity.Motivazione_Reg_Id, 0) != 0 && CommonService.Nz(entity.Motivazione_Reg_Cod, "") != "")
            {
                if (Tab_Decods.SingleOrDefault(x => x.Gruppo_Tab == TabDecodGroupTypeEnum.DECOD_TAB.ToString()
                && x.Nome_Tab == TabDecodNameEnum.TIPO_COL.ToString() && x.Chiave_Tab == entity.Motivazione_Reg_Id.ToString()) == null)
                {
                    var newTabDecod = new Tab_Decod
                    {
                        Gruppo_Tab = TabDecodGroupTypeEnum.DECOD_TAB.ToString(),
                        Nome_Tab = TabDecodNameEnum.MOTIVAZIONI.ToString(),
                        Chiave_Tab = entity.Motivazione_Reg_Cod,
                        Decodifica_Tab = BusinessService.GetLocalizedString(PowerWebResources.LBL_AUTOMATICO)
                    };
                    RepoManager.Tab_DecodRepo.Add(newTabDecod, true);
                    PowerWebContext.SetToSession<List<Tab_Decod>>("Tab_Decods_RegVColRepo", null);
                }
            }
            WriteCheckLog(entity, result, Log);
            return result;
        }



        public override void SetEntityBeforeAddOrUpdate(Reg_V entity)
        {
            base.SetEntityBeforeAddOrUpdate(entity);

            // se l'utente ha richiesto che la reg_v sia di sola durata allora lo si imposta 
            // come tipo registrazione; idem con l'attività; in caso contrario si usa il tipo di default none 
            if (entity.IsOnlyDuration)
                entity.Registrazione_Tipo_Reg = (int)RegTypeEnum.Duration;
            else
                entity.Registrazione_Tipo_Reg = entity.Registrazione_Tipo_Reg == (int)RegTypeEnum.Att ? (int)RegTypeEnum.Att : (int)RegTypeEnum.None;

            // se l'utente ha chiesto che la registazione abbia durata negativa e la registrazione è di sola durata e la stessar risulta 
            if ((entity.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && entity.RegistrationDurationNegative && entity.Durata_Fis >= 0) || (entity.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration && !entity.RegistrationDurationNegative && entity.Durata_Fis < 0))
                entity.Durata_Fis *= -1;
        }

        /// <summary>
        /// Da una reg_v passata come parametro ritorna splittate in lista le due reg pronte per la cancellazione
        /// </summary>
        /// <param name="regVToDelete">La reg_v da cancellazione.</param>
        /// <returns>la lista di reg che compongono la reg_v da cancellare. Gli elementi saranno 1 o 2 a seconda della presenza della reg in uscita.</returns>
        public List<Reg> GetRegToDelete(Reg_V regVToDelete)
        {
            List<Reg> toDeleteRegsOld = new List<Reg>();

            //Recupero gli ID delle REG di Entrata e Uscita (quello di Uscita potrebbe non esserci)
            var currentRegEId = regVToDelete.RegE;
            int? currentRegUId = regVToDelete.RegU;

            //Inizializzo i Dati delle REG di Entrata e Uscita da eliminare                    
            Reg currentRegEOld;
            Reg currentRegUOld = null;
            DateTime dateTimeEFisOld = DateTime.MinValue;
            DateTime dateTimeUFisOld = DateTime.MinValue;

            //Leggo e Aggiungo la REGE fra le REg da Trattare leggendola con il suo ID (c'è sempre)
            currentRegEOld = RepoManager.RegRepo.Single(reg => reg.Reg_Id == currentRegEId);

            toDeleteRegsOld.Add(currentRegEOld);

            //verifico se è presente anche una Reg di Uscita
            if (currentRegUId.HasValue)
            {
                //se esiste Leggo e Aggiungo la Reg di Uscita fra quelle da Cancellare leggendola con il Suo Id           
                currentRegUOld = RepoManager.RegRepo.Single(reg => reg.Reg_Id == currentRegUId.Value);
                toDeleteRegsOld.Add(currentRegUOld);
            }

            // ritorno del valore del metodo
            return toDeleteRegsOld;
        }

        /// <summary>
        /// Metodo che data una regv prima dell'update si occupa di verificare un eventuale cambio di cant e/o col e di conseguenza ne cancella i valori di pru e fru.
        /// Il controllo viene effettuato prima della scrittura su database e quindi non funziona su reg_v le cui reg siano già scritte.
        /// Non viene eseguita nessuna operazione su regV nuove (non ancora scritte su database, cioè con RegE valorizzato a 0)
        /// </summary>
        /// <param name="regVToUpdate">The di cui effettuare l'eventuale update di pru/fru.</param>
        public void ManageCantColChangesBeforeUpdate(Reg_V regVToUpdate)
        {
            // si procede con l'elaborazione solamente se non si sta processando una reg_v nuova
            if (regVToUpdate.RegE != 0)
            {
                // viene recuperata l'attuale reg_v dal database
                Reg_V currentRegV = SingleOrDefault(regV => regV.RegE == regVToUpdate.RegE);

                // se nella reg_v da processare è cambiato il cant_id allora si procede all'annullamento del valore di fru_id
                if (currentRegV.Cant_Id != regVToUpdate.Cant_Id)
                    regVToUpdate.Fru_Id = null;

                // se nella reg_v da processare è cambiato il col_id allora si procede all'annullamento del valore di pru_id
                if (currentRegV.Col_Id != regVToUpdate.Col_Id)
                    regVToUpdate.Pru_Id = null;
            }
        }

        public IEnumerable<Reg_V> GetReg_VByDateRangeOrPassageByRegIds(DateTime from, DateTime to, IEnumerable<int> regIds)
        {
            IEnumerable<Reg_V> result = Enumerable.Empty<Reg_V>();

            try
            {
                result = DbSet
                    .AsNoTracking()
                    .Where(regv => regv.Data_Ora_Fis_E >= from && (regv.Data_Ora_Fis_U <= to || regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass) && regIds.Contains(regv.RegE))
                    .ToList();
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Errore nel metodo {0} del Reg_V repository : {1}", nameof(GetReg_VByDateRangeOrPassageByRegIds), ex.Message);
            }

            return result;

        }

        public string InviaRitardi()
        {
            Col col;
            Cant cant;
            int durata_ritardo_ore = 0,
            durata_ritardo_minuti = 0;
            //Prepara il body della mail caricando il css
            string mailBody = "<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; \">",
                   errorMessage = "";


            DateTime today = DateTime.Today;
            //Recupera le registrazioni di oggi con ritardo non ancora inviate
            List<Reg_V> delayList = RepoManager.Reg_VRepo.Find(regv => (regv.Data_Ora_Fis_E.Year == today.Year && regv.Data_Ora_Fis_E.Month == today.Month && regv.Data_Ora_Fis_E.Day == today.Day) && (regv.Ritardo_Durata != null && regv.Ritardo_Durata > 0) && (!regv.Ritardo_Mail_Sent)).ToList();

            if (delayList.Any())
            {
                mailBody += "<p>Il giorno " + today.ToString("dddd d MMMM yyyy") + " sono stati registrati i seguenti ritardi:</p>";
                mailBody += "<div style='margin-left: 20px;'>";

                //Raggruppa le registrazioni per cantiere
                var delayByCant = delayList.GroupBy(regv => regv.Cant_Id);

                foreach (var cantDelays in delayByCant)
                {
                    cant = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == cantDelays.Key);
                    if (cant != default(Cant))
                    {
                        mailBody += "<p><u>Cantiere</u>: " + cant.Descrizione_Can.ToUpper() + "</p>";
                        //Raggruppa le registrazioni per collaboratore
                        var delayByCantByCol = cantDelays.GroupBy(regv => regv.Col_Id);

                        foreach (var colDelays in delayByCantByCol)
                        {
                            col = RepoManager.ColRepo.SingleOrDefault(c => c.Col_Id == colDelays.Key);
                            //var orario = RepoManager.Tab_OrariRepo.SingleOrDefault(o => o.Tab_Orari_Tipo_Id == col.Tab_Orari_Tipo_Id && o.Cant_Id == cant.Cant_Id);

                            if (col != default(Col))
                            {
                                //Per ogni collaboratore scrive i ritardi
                                mailBody += "<div style='margin: 0 0 10px 20px;'>";
                                mailBody += "<p><u>Collaboratore</u>: " + col.Cognome_Col.ToUpper() + " " + col.Nome_Col + "</p>";
                                var orderedColDelays = colDelays.OrderBy(regv => regv.Data_Ora_Fig_E);

                                foreach (Reg_V ritardo in orderedColDelays)
                                {
                                    if (ritardo.Ritardo_Durata != null)
                                    {
                                        durata_ritardo_minuti = ritardo.Ritardo_Durata.Value % 60;
                                        durata_ritardo_ore = ritardo.Ritardo_Durata.Value / 60;
                                    }
                                    else 
                                    {
                                        Reg regE = RepoManager.RegRepo.Single(r => r.Reg_Id == ritardo.RegE);
                                        durata_ritardo_minuti = regE.Ritardo_Durata.Value % 60;
                                        durata_ritardo_ore = regE.Ritardo_Durata.Value / 60;
                                    }
                                    
                                    //if (orario != default(Tab_Orari))
                                    //{
                                    //    String entrata = "";
                                    //    switch (today.DayOfWeek.ToString()) {
                                    //        case "Monday":
                                    //            if (orario.G1 != false) {
                                    //                entrata = orario.G1.ToString();
                                    //            }
                                    //            break;
                                    //        case "Tuesday":
                                    //            if (orario.G2 != false) {
                                    //                entrata = orario.G2.ToString();
                                    //            }
                                    //            break;
                                    //        case "Wednesday":
                                    //            if (orario.G3 != false) {
                                    //                entrata = orario.G3.ToString();
                                    //            }
                                    //            break;
                                    //        case "Thursday":
                                    //            if (orario.G4 != false) {
                                    //                entrata = orario.G4.ToString();
                                    //            }
                                    //            break;
                                    //        case "Friday":
                                    //            if (orario.G5 != false) {
                                    //                entrata = orario.G5.ToString();
                                    //            }
                                    //            break;
                                    //        case "Saturday":
                                    //            if (orario.G6 != false) {
                                    //                entrata = orario.G6.ToString();
                                    //            }
                                    //            break;
                                    //        case "Sunday":
                                    //            if (orario.G7 != false) {
                                    //                entrata = orario.G7.ToString();
                                    //            }
                                    //            break;
                                    //    }
                                    //    if (entrata == "")
                                    //    {
                                    //        mailBody += "<table style='margin: 0 20px;'>";
                                    //        mailBody += "<tr><td>Ora Entrata:</td><td style='padding-left: 15px'>" + ritardo.Data_Ora_Fis_ETime.Value.Hours.ToString("00") + ":" + ritardo.Data_Ora_Fis_ETime.Value.Minutes.ToString("00") + ",Non era previsto il collaboratore nel cantere </td></tr>";
                                    //    }
                                    //    else {
                                    //        mailBody += "<table style='margin: 0 20px;'>";
                                    //        mailBody += "<tr><td>Ora Entrata:</td><td style='padding-left: 15px'>" + ritardo.Data_Ora_Fis_ETime.Value.Hours.ToString("00") + ":" + ritardo.Data_Ora_Fis_ETime.Value.Minutes.ToString("00") + ", Ora Prevista:</td><td style='padding-left: 15px'>" + orario.Ora_E.ToString() + "</td></tr>";
                                    //    }        
                                    //}
                                    //else {
                                    //    mailBody += "<table style='margin: 0 20px;'>";
                                    //    mailBody += "<tr><td>Ora Entrata:</td><td style='padding-left: 15px'>" + ritardo.Data_Ora_Fig_ETime.Value.Hours.ToString("00") + ":" + ritardo.Data_Ora_Fig_ETime.Value.Minutes.ToString("00") + "</td></tr>";
                                    //}
                                    mailBody += "<p style='margin: 0 20px;font-weight: bold;'>Ritardo: " + durata_ritardo_ore.ToString("00") + ":" + durata_ritardo_minuti.ToString("00") + "</p>";
                                }

                                mailBody += "</div>";
                            }
                        }
                    }
                }

                mailBody += "</div>";
            }

            else
            {
                mailBody += "<p>Nessun collaboratore è in ritardo il giorno " + today.ToString("dddd d MMMM yyyy") + ".</p>";
            }

            mailBody += "</div>";
            //Invia le mail
            errorMessage = CommonService.sendMail(RepoManager.ParamRepo.ParametersRow.CompanyEmail, "PowerWeb - Comunicazione ritardi " + today.ToString("d MMMM yyyy"), mailBody, "newsletter@winit.it", "PowerWeb - Comunicazione ritardi", new string[] { });

            //Se la mail è stata inviata correttamente, segnala che è stata inviata
            if (errorMessage == "Mail inviata!")
            {
                foreach (Reg_V sent in delayList)
                {
                    Reg regE = RepoManager.RegRepo.Single(reg => reg.Reg_Id == sent.RegE);
                    regE.Ritardo_Mail_Sent = true;
                    RepoManager.RegRepo.Update(regE, true);
                }
            }
            return errorMessage;
        }

        public string InviaChiamate()
        {
            Col col;
            string company = RepoManager.ParamRepo.ParametersRow.CompanyName;
            //Prepara il body della mail caricando il css
            string lastPruCode = "",
                   mailBody = "<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; \"><p>Buongiorno,</p>";

            DateTime yesterday = DateTime.Today.AddDays(-1);
            //Recupera tutti i collaboratori non disabilitati
            List<int> allColIds = RepoManager.ColRepo.Find(c => !c.DisAbilitazione_Col).Select(c => c.Col_Id).ToList();
            //Recupera tutti i collaboratori con almeno un registrazione nella giornata di ieri
            List<int> okCols = RepoManager.RegRepo.Find(reg => reg.Registrazione_Data_Ora_Fig_Reg.Value.Year == yesterday.Year && reg.Registrazione_Data_Ora_Fig_Reg.Value.Month == yesterday.Month && reg.Registrazione_Data_Ora_Fig_Reg.Value.Day == yesterday.Day).Select(r => r.Col_Id.Value).Distinct().ToList();
            //Fa la differenza tra le due liste per trovare i collaboratori che non hanno trasmesso
            List<int> koCols = allColIds.Except(okCols).ToList();

            if (koCols.Any())
            {
                mailBody += "<p>Per il giorno " + DateTime.Today.AddDays(-1).ToString("dddd d MMMM yyyy") + " non sono presenti timbrature dei seguenti collaboratori:</p>";
                mailBody += "<div style='margin-left: 20px; '>";

                //Elenca i collaboratori con le rispettive PRU
                foreach (int colId in koCols)
                {
                    col = RepoManager.ColRepo.First(c => c.Col_Id == colId);
                    lastPruCode = col.LastPruCode == "" ? "Nessun unità associata" : col.LastPruCode.Trim();
                    mailBody += "<p>" + col.Cognome_Col.ToUpper() + " " + col.Nome_Col + " (" + lastPruCode + ")" + " - " + company + "</p>";
                }
                mailBody += "</div>";
            }

            else
            {
                mailBody += "<p>Tutti i collaboratori attivi hanno timbrature nella giornata di ieri.</p>";
            }

            mailBody += "<p>Buona giornata,</p>";
            mailBody += "<p>Winit srl</p>";
            mailBody += "</div>";

            //Invia le mail
            return CommonService.sendMail(RepoManager.ParamRepo.ParametersRow.CompanyEmail, "WINIT - Chiamate " + company + " " + DateTime.Today.ToString("d MMMM yyyy"), mailBody, "supporto@win-it.it", "WINIT", new string[] { });
        }

        #region Gestione Exports Xml Reg_V

        /// <summary>
        /// Effettua l'esportazione xml delle registrazioni passate come parametro verso Pefetto, restituendo per il download un file zip con i dati generati.
        /// </summary>
        /// <param name="regVsToProcess">Le Reg_V da processare nell'esportazione Xml.</param>
        /// <param name="filesOutputFolder">La cartella in cui salvare i dati preparati nell'export xml</param>
        /// <returns>
        /// Ritorna il percorso del file da ritornare al browser con i dati esportati
        /// </returns>
        public string PrepareXmlExportToPerfetto(IQueryable<Reg_V> regVsToProcess, string filesOutputFolder)
        {
            #region Temporary Constants

            const string documentNamespace = "Document.Perfetto.WorkingReports.Documents.JobWorkingReports";
            const string documentProfile = "RapportiniSogedi";
            const string documentTitle = "Rapportini per commessa";
            const string documentDescription = "";
            const string documentDomain = "DEFAULT_DOMAIN";
            const string documentSite = "DEFAULT_SITE";
            const string documentUser = "marco.verri";
            const int customHoursValue = 0;

            #endregion

            #region Utilities

            HashSet<DateTime> festività = new HashSet<DateTime>(RepoManager.Tab_FestiviRepo.DbSet.Select(f => f.Giorno_Tab_Festivi).ToList());

            #endregion

            string returnFileName = String.Empty;

            //controllo di avere delle reg da inserire nell'Xml
            if (regVsToProcess.Any())
            {
                // viene recuperato il parametro della personalizzazione di export xml che indica sotto quale soglia kilometrica trattare i viaggi come ore lavorate
                string kmParam = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.RegExportToXmlEnum, "TreatTripAsWorkedUnderKM");
                decimal kmThreshold = 0m;
                if (!String.IsNullOrEmpty(kmParam))
                    decimal.TryParse(kmParam, out kmThreshold);

                // viene recuperato il parametro della personalizzazione di export xml che indica se trattare o meno i viaggi di inizio fine/giornata nello stesso
                // comune sotto il numero di km del threshold
                string doNotTreatStartEndTripUnderKmParam = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.RegExportToXmlEnum, "DoNotTreatStartEndTripUnderKM");
                bool doNotTreatStartEndTripUnderKm = true;
                if (!String.IsNullOrEmpty(doNotTreatStartEndTripUnderKmParam))
                    bool.TryParse(doNotTreatStartEndTripUnderKmParam, out doNotTreatStartEndTripUnderKm);

                // viene recuperato il parametro della personalizzazione di export xml che indica se trattare solo come ore ordinarie (a riempimento) i viaggi
                // nello stesso comune sotto il threshold di Km
                string doNotTreatTripUnderKmAsOvertineParam = RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.RegExportToXmlEnum, "DoNotTreatTripUnderKmAsOvertine");
                bool doNotTreatTripUnderKmAsOvertime = false;
                if (!String.IsNullOrEmpty(doNotTreatTripUnderKmAsOvertineParam))
                    bool.TryParse(doNotTreatTripUnderKmAsOvertineParam, out doNotTreatTripUnderKmAsOvertime);

                // inizializzazione di quanto già esportato in precedenza
                IEnumerable<ExportedData> exportedDatesAndColIds = ReadProcessedDatesAndCols(filesOutputFolder);

                string folderpath = Path.Combine(filesOutputFolder, DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss"));

                // inizializzazione della lista di rapportini (files xml) generati
                var reportsFileName = new List<string>();

                // ciclo di elaborazione delle reg_v per codice cantiere
                // [si ciclano solamente le registrazioni, se presenti, dei collaboratori data non precedentemente processati]
                var regVsNotYetProcessed = regVsToProcess.AsEnumerable().Where(regv => !exportedDatesAndColIds.Any(exp => exp.DataReg == regv.Data_Reg && exp.ColId == regv.Col_Id)).ToList();

                if (regVsNotYetProcessed.Any())
                {
                    var regRowToProcess = new List<RegvRow>();

                    // per prima cosa si processano tutte le registrazioni per collaboratore data e si genera una lista 
                    // di oggetti che contengono i dati di registrazione che contemplano i dati di timing del giorno
                    foreach (var regVsByDate in regVsNotYetProcessed.GroupBy(regv => regv.Data_Reg).ToList()) // per ogni data
                    {
                        foreach (var regvsByDateAndCol in regVsByDate.GroupBy(regv => regv.Col_Mnemonic).ToList()) // per ogni collaboratore
                        {
                            // inizializzazione dei totali di ore ordinarie e straordinarie
                            int totalWorkingHours = 0;
                            int totalOvertimeHours = 0;

                            // inizializzo la variabile che indica se processare ancora i viaggi trasformati in ore lavorate per sottokilometraggio
                            // (se si sta processando un sabato o una domenica allora non si utilizzano i viaggi sotto i 10 Km)
                            bool noMoreUnderKmTrips = regVsByDate.Key.Value.DayOfWeek == DayOfWeek.Sunday || regVsByDate.Key.Value.DayOfWeek == DayOfWeek.Saturday;

                            // inizializzazione della lista delle registrazioni di giornata da processare verso perfetto
                            var dayRegRowToProcess = new List<RegvRow>();

                            // per ogni registrazione all'interno della data (ordinata per tipo registrazione per processare i viaggi in fondo e per ora fisica)
                            var regvsByDateAndColOrdered = regvsByDateAndCol.OrderBy(regv => regv.Registrazione_Tipo_Reg).ThenBy(regv => regv.Data_Ora_Fis_E).ToList();
                            foreach (Reg_V regv in regvsByDateAndColOrdered)
                            {

                                // inizializzazione della variabile che indica l'intenzione di processare la registrazione (di default la si processa)
                                bool isToProcess = true;

                                // se richiesto dai parametri dell'export non si trattano i viaggi di inizio/fine giornata nello stesso comune sotto i 10 km
                                if (doNotTreatStartEndTripUnderKm && regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Trip)
                                {
                                    // si sta trattando un viaggio di inizio/fine giornata, sotto i 10 Km nello stesso comune
                                    // allora si segnala l'intenzione di processare il record in base al parametro
                                    if (IsTripOnSameMunicipality(regv, regvsByDateAndColOrdered) && IsTripOnStartEnd(regv, regvsByDateAndColOrdered) && regv.KM_Reg <= kmThreshold)
                                        isToProcess = !doNotTreatStartEndTripUnderKm;
                                }

                                // se è stata indicata l'intenzione di processare il record
                                if (isToProcess)
                                {
                                    // inizializzazione della nuova registrazione poi da esportare
                                    var newRegRow = default(RegvRow);

                                    // si procede all'elaborazione delle sole reg_v abbinate, cioè che hanno un'uscita, hanno un data registrazione e una durata
                                    int endHour = regv.Data_Ora_Fis_U != null ? Convert.ToInt32((new TimeSpan(regv.Data_Ora_Fis_U.Value.Hour, regv.Data_Ora_Fis_U.Value.Minute, 0)).TotalMinutes) * 60 : 0;
                                    if (endHour != 0 && regv.Data_Reg != null && regv.Durata_Fis != 0)
                                    {
                                        // calcolo del tipo di registrazione che si sta processando (può cambiare in base ai parametri delle personalizzazioni)
                                        RegTypeEnum currentRegType = RegTypeEnum.None;
                                        bool wasTrip = false;
                                        // il viaggio può cambiare di tipo solo se nello stesso comune e inferiore al threshold dei km personalizzati
                                        // altrimenti rimane sicuramente un viaggio
                                        if ((RegTypeEnum)regv.Registrazione_Tipo_Reg == RegTypeEnum.Trip)
                                        {
                                            // il controllo sullo stesso comune viene effettuato qua in quanto time spending
                                            if (IsTripOnSameMunicipality(regv, regvsByDateAndColOrdered))
                                            {
                                                // il viaggio diventa ora lavorata se sotto una certa soglia di Km
                                                decimal kmRegV = regv.KM_Reg.HasValue ? regv.KM_Reg.Value : 0m;
                                                currentRegType = kmRegV > kmThreshold ? RegTypeEnum.Trip : RegTypeEnum.None;

                                                // se la registrazione era un viaggio poi passato a ore lavorate per sottokilometraggio
                                                // lo si segna in una variabile di modo da effettuare delle successive post elaborazioni
                                                wasTrip = true;
                                            }
                                            else
                                                currentRegType = (RegTypeEnum)regv.Registrazione_Tipo_Reg;
                                        }
                                        else // altrimenti il tipo registrazione è determinato dal tipo imposstato in registrazione
                                            currentRegType = (RegTypeEnum)regv.Registrazione_Tipo_Reg;

                                        // si aggiunge all'elenco la riga corrente solamente se non è un viaggio sotto km oppure 
                                        // oppure, se è un viaggio sotto km non si vogliono più viaggi sotto km
                                        if (!wasTrip || !noMoreUnderKmTrips)
                                        {
                                            // generazione di una nuova riga per il rapportino
                                            newRegRow = new RegvRow();

                                            // compilazione dei dati di riga
                                            newRegRow.ColId = Convert.ToInt32(regv.Col_Id);
                                            newRegRow.ColMnemonic = regv.Col_Mnemonic;
                                            newRegRow.CantId = Convert.ToInt32(regv.Cant_Id);
                                            newRegRow.CantMnemonic = regv.Cant_Mnemonic;
                                            newRegRow.DataReg = Convert.ToDateTime(regv.Data_Reg);
                                            var noteCode = String.Format("{0}#{1}#{2}", regv.Col_Mnemonic, regv.Cant_Id, regv.Data_Reg.Value.ToString("yy-MM-dd"));
                                            newRegRow.Note = String.Format("Inserimento automatico {0}", noteCode);
                                            newRegRow.StartHour = Convert.ToInt32((new TimeSpan(regv.Data_Ora_Fis_E.Hour, regv.Data_Ora_Fis_E.Minute, 0)).TotalMinutes) * 60;
                                            newRegRow.EndHour = endHour;
                                            newRegRow.Type = currentRegType;
                                            newRegRow.IsTripUnderKm = wasTrip;

                                            // inizializazione dei campi delle ore nella riga
                                            newRegRow.OrdinaryHours = 0;
                                            newRegRow.OvertimeHours = 0;
                                            newRegRow.TravelHours = 0;

                                            // calcolo durata in secondi della registrazione
                                            int regvDuration = newRegRow.EndHour - newRegRow.StartHour;

                                            // calcolo i totali parziali per la gestione della riga
                                            totalWorkingHours += currentRegType == RegTypeEnum.None ? regvDuration : 0;

                                            // se si sta elaborando un sabato, si tratta sempre di straordinari
                                            if (regv.Data_Reg.Value.DayOfWeek == DayOfWeek.Sunday || regv.Data_Reg.Value.DayOfWeek == DayOfWeek.Saturday)
                                            {
                                                newRegRow.OvertimeHours += regvDuration;
                                            }

                                            else
                                            {
                                                //se la registrazione è un viaggio
                                                if (currentRegType == RegTypeEnum.Trip)
                                                {
                                                    // se le ore lavorate sono superiori o uguali a quanto previsto allora
                                                    // i viaggi sono trattati come ore viaggio
                                                    if (totalWorkingHours >= XmlToPerfettoConstants.OrdinaryHours)
                                                        newRegRow.TravelHours = regvDuration;
                                                    else // se con le ore ordinarie non si è ancora raggiunto il numero di ore previste
                                                    {
                                                        // le ore viaggio sono trattate come ordinarie fino al raggiungimento delle ore previste, il resto sono ore viaggio
                                                        int fillOrdinaryHours = XmlToPerfettoConstants.OrdinaryHours - totalWorkingHours;

                                                        // se la durata del viaggio è inferiore al mancante per il previsto, le ore sono tutte trattate come ordinarie
                                                        if (regvDuration <= fillOrdinaryHours)
                                                        {
                                                            totalWorkingHours += regvDuration;
                                                            newRegRow.Travel8hHours = regvDuration;
                                                        }
                                                        else // se la durata del viaggio è superiore al mancante per il previsto, la quota in eccesso viene trattata come viaggio, il resto come ore ordinarie
                                                        {
                                                            // le ore viaggio sono la differenza tra le ore viaggio e le differenziali
                                                            newRegRow.TravelHours = regvDuration - fillOrdinaryHours;

                                                            // le ore ordinarie sono le ore differenziali
                                                            totalWorkingHours += fillOrdinaryHours;
                                                            newRegRow.Travel8hHours = fillOrdinaryHours;
                                                        }

                                                    }
                                                }
                                                else if (currentRegType == RegTypeEnum.None && String.IsNullOrEmpty(regv.Motivazione_Reg_Cod)) // se la registrazione è un'ora normale (cioè non un viaggio senza motivazione)
                                                {
                                                    // se la durata totale del giorno è maggiore del numero di ore ordinarie configurate
                                                    if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                                    {
                                                        // se si sta processando delle ore viaggio sotto kilometrate che da parametro
                                                        // non devono essere trattate come straordinario allora si procede alla loro non creazione
                                                        // e alla rimozione del loro dato dal totale delle ore processate
                                                        if (newRegRow.IsTripUnderKm && doNotTreatTripUnderKmAsOvertime)
                                                        {
                                                            // si marca la registrazione per la non creazione
                                                            newRegRow = null;

                                                            // rimozione della durata della registrazione dalle ore totali
                                                            totalWorkingHours -= regvDuration;

                                                            // si dice alla procedura di non gestire altri viaggi sotto kilometraggio (per questa data/collaboratore)
                                                            noMoreUnderKmTrips = true;
                                                        }
                                                        else
                                                        {
                                                            // in caso non si stia processando un viaggio sotto kilometrato allora si tolgono gli elementi
                                                            // viaggio già creati fino a esaurimento o rientro in ordinario
                                                            if (dayRegRowToProcess.Any(regRow => regRow.IsTripUnderKm))
                                                            {
                                                                // inizializzazione della lista di elementi da rimuovere dalla lista
                                                                var regvRowsToRemove = new List<RegvRow>();

                                                                // ciclo di elaborazione dei viaggi sotto kilometraggio già inseriti
                                                                foreach (RegvRow regvRow in dayRegRowToProcess.Where(regRow => regRow.IsTripUnderKm).OrderByDescending(regRow => regRow.StartHour).ToList())
                                                                {
                                                                    // procedo a elaborare solamente se non sono già a posto con le ore
                                                                    if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                                                    {
                                                                        // tolgo le ore della registrrazione corrente e la marco da cancellare
                                                                        totalWorkingHours -= regvRow.OrdinaryHours + regvRow.OvertimeHours;
                                                                        totalOvertimeHours -= regvRow.OvertimeHours;
                                                                        regvRowsToRemove.Add(regvRow);
                                                                    }
                                                                    else // se invece sono a posto smetto di ciclare, ho tolto il necessario
                                                                        break;
                                                                }

                                                                // al termine dell'elaborazione, se ci sono da eliminare delle righe con le ore provenienti da viaggi sotto kilometrati
                                                                // lo effettuo
                                                                if (regvRowsToRemove.Any())
                                                                    regvRowsToRemove.ForEach(regvRow => dayRegRowToProcess.Remove(regvRow));
                                                            }

                                                            // si procede alla gestione degli straordinari solamente se ce ne sono ancora
                                                            if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                                            {
                                                                // il totale delle ore straordinarie per la regv che si sta processando è dato dal toltale
                                                                // delle ore del coll/giorno meno le ore straordinarie già assegnate meno le ore previste in giornata
                                                                newRegRow.OvertimeHours = totalWorkingHours - totalOvertimeHours - XmlToPerfettoConstants.OrdinaryHours;
                                                                totalOvertimeHours += newRegRow.OvertimeHours;

                                                                // le ore ordinarie in questo caso sono il restante degli straordinari
                                                                newRegRow.OrdinaryHours = regvDuration - newRegRow.OvertimeHours;
                                                            }
                                                            else // nel caso invece si sia rientrati nell'alveo della normalità si registrano le ore ordinarie
                                                                newRegRow.OrdinaryHours = regvDuration;
                                                        }


                                                    }
                                                    else // nel caso invece non si sia superato il numero di ore ordinarrie configurate (in questo caso sono tutte ore ordinarie)
                                                        newRegRow.OrdinaryHours = regvDuration;
                                                }
                                                else if (currentRegType == RegTypeEnum.None & !String.IsNullOrEmpty(regv.Motivazione_Reg_Cod)) // se la registrazione è un'ora normale con motivazione
                                                {
                                                    // in base al tipo di registrazione si impostano ferie/permessi o malattie/infortuni
                                                    switch (regv.Motivazione_Reg_Cod)
                                                    {
                                                        case "FP":
                                                            newRegRow.VacationHours = regvDuration;
                                                            break;
                                                        case "M":
                                                            newRegRow.SickHours = regvDuration;
                                                            break;
                                                        case "IC":
                                                            newRegRow.InjuryHours = regvDuration;
                                                            break;
                                                    }
                                                }

                                            }
                                        }

                                        // aggiunta della riga all'elenco (se non marcata per la non creazione)
                                        if (newRegRow != null)
                                            dayRegRowToProcess.Add(newRegRow);
                                    }
                                }
                            }

                            // aggiunta dell'elenco cacolato nel giorno/collaboratore all'elenco generale
                            regRowToProcess.AddRange(dayRegRowToProcess);

                            // al cambio di data si verifica se le ore totali sono sotto le 8 ore.
                            // se sono sotto le 8 ore si aggiunge in automatico il restante come ferie e pemessi
                            if (totalWorkingHours < XmlToPerfettoConstants.OrdinaryHours)
                            {
                                // se non sta trattando un fine settimana
                                if (Convert.ToDateTime(regVsByDate.Key).DayOfWeek != DayOfWeek.Sunday && Convert.ToDateTime(regVsByDate.Key).DayOfWeek != DayOfWeek.Saturday)
                                {
                                    // calcolo della durata del riempimento
                                    int fillDuration = XmlToPerfettoConstants.OrdinaryHours - totalWorkingHours;

                                    // generazione di una nuova riga per il rapportino
                                    var newFillRegRow = new RegvRow();

                                    //da tutta la lista delle regv_toProcess vengnono estratte le registrazioni di solo quel collaboratore
                                    var regRowToProcessbyCol = regRowToProcess.Where(regv => regv.ColMnemonic == regvsByDateAndCol.Key).ToList();

                                    //ordinamento delle registrazioni sulla base dell'ora di uscita in modo da ottnere la registrazione più recente nel tempo come ultima
                                    var regRoworderedByData = regRowToProcessbyCol.OrderBy(row => row.EndHour).ToList();

                                    int output = regRoworderedByData.Last().EndHour;
                                    // compilazione dei dati di riga
                                    newFillRegRow.ColId = Convert.ToInt32(regvsByDateAndCol.FirstOrDefault().Col_Id);
                                    newFillRegRow.ColMnemonic = regvsByDateAndCol.Key;
                                    newFillRegRow.CantId = XmlToPerfettoConstants.CantVacationId;
                                    newFillRegRow.CantMnemonic = XmlToPerfettoConstants.CantVacationMnemonic;
                                    newFillRegRow.DataReg = Convert.ToDateTime(regVsByDate.Key);
                                    newFillRegRow.Note = String.Format("Inserimento automatico a riempimento {0} ore", XmlToPerfettoConstants.OrdinaryHours);
                                    newFillRegRow.StartHour = regRoworderedByData.Last().EndHour;
                                    newFillRegRow.EndHour = regRoworderedByData.Last().EndHour + fillDuration;
                                    newFillRegRow.Type = RegTypeEnum.None;

                                    // inizializazione dei campi delle ore nella riga
                                    newFillRegRow.VacationHours = fillDuration;

                                    //alla lista generale delle registrazioni vengono aggiunte quelle a rimepimento delle 8 ore
                                    regRowToProcess.Add(newFillRegRow);

                                }
                            }
                        }
                    }

                    if (regRowToProcess.Any())
                    {

                        int cantNumber = 0;
                        var totalCant = regRowToProcess.Where(regv => regv != null).GroupBy(regv => regv.CantMnemonic).Count();
                        foreach (var regvRowsByCant in regRowToProcess.Where(regv => regv != null).GroupBy(regv => regv.CantMnemonic).ToList())
                        {
                            cantNumber++;
                            // per ogni cantiere viene generato un rapportino, e quindi si genera e salvata un file per ogni cantiere

                            // creo il documento
                            var document = new XmlExportsData.Perfetto.Document();

                            // generazione della sezione dati del documento
                            document.DocumentInfo.Namespace = documentNamespace;
                            document.DocumentInfo.Profile = documentProfile;
                            document.DocumentInfo.Title = documentTitle;
                            document.DocumentInfo.Description = documentDescription;
                            document.DocumentInfo.Creation.Domain = documentDomain;
                            document.DocumentInfo.Creation.Site = documentSite;
                            document.DocumentInfo.Creation.User = documentUser;
                            document.DocumentInfo.Creation.DateTime = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss");

                            // ciclo di elaborazione delle timbrature per cantiere anche per collaboratore/giorno,
                            // questo per calcolare correttamente i dati di totale
                            foreach (var regvRowsByDate in regvRowsByCant.GroupBy(regv => regv.DataReg))
                            {
                                // calcolo della data attualmente in processo (formato stringa)
                                string currentRegVDate = regvRowsByDate.Key.ToString("yyyy-MM-dd");

                                // viene generato un master per ogni data all'interno della stessa commessa
                                var master = new Business.XmlExportsData.Perfetto.XmlMaster();
                                master.Fields.WorkingReportDate = currentRegVDate;
                                master.Fields.Job = regvRowsByCant.Key ?? "0";

                                // inizializzazione del numero di righe in processo
                                int rowsNumber = 0;

                                foreach (var regvRowsByDateAndCol in regvRowsByDate.GroupBy(regv => regv.ColMnemonic).ToList())
                                {
                                    // per ogni registrazione all'interno della data (ordinata per tipo registrazione per processare i viaggi in fondo)
                                    foreach (var regv in regvRowsByDateAndCol.OrderBy(regv => Convert.ToInt32(regv.Type)))
                                    {
                                        // inizializzazione della riga rapportino
                                        Business.XmlExportsData.Perfetto.XmlRow newRow = new Business.XmlExportsData.Perfetto.XmlRow();

                                        // compilazione dei dati di riga
                                        newRow.number = rowsNumber;
                                        newRow.Fields.Line = rowsNumber + 1;
                                        newRow.Fields.Employee = regv.ColMnemonic;
                                        newRow.Fields.Job = regv.CantMnemonic;
                                        newRow.Fields.WorkingReportDate = currentRegVDate;
                                        newRow.Fields.Note = regv.Note;
                                        newRow.Fields.StartHour = regv.StartHour;
                                        newRow.Fields.EndHour = regv.EndHour;

                                        //Se non è un giorno festivo configuro la reg nel modo classico
                                        if (!(festività.Contains(regv.DataReg)) && !(regv.DataReg.DayOfWeek == DayOfWeek.Sunday)) //Se non è un giorno festivo
                                        {
                                            newRow.Fields.CustomHours1 = customHoursValue;
                                            newRow.Fields.CustomHours3 = customHoursValue;
                                            newRow.Fields.OvertimeHours = regv.OvertimeHours;
                                            newRow.Fields.TravelHours = regv.TravelHours;
                                            newRow.Fields.CustomHours2 = regv.Travel8hHours;
                                            newRow.Fields.OrdinaryHours = regv.OrdinaryHours;
                                            newRow.Fields.VacationLeaveHours = regv.VacationHours;
                                            newRow.Fields.SickLeaveHours = regv.SickHours;
                                            newRow.Fields.CustomHours4 = regv.InjuryHours;
                                        }
                                        else
                                        {
                                            //Altrimenti la durata della regV viene quantificata nel campo "CustomHours1" (straordinari festivi secondo Perfetto)
                                            newRow.Fields.CustomHours1 = regv.EndHour - regv.StartHour;
                                            newRow.Fields.CustomHours3 = customHoursValue;
                                            newRow.Fields.OvertimeHours = customHoursValue;
                                            newRow.Fields.TravelHours = customHoursValue;
                                            newRow.Fields.CustomHours2 = customHoursValue;
                                            newRow.Fields.OrdinaryHours = customHoursValue;
                                            newRow.Fields.VacationLeaveHours = customHoursValue;
                                            newRow.Fields.SickLeaveHours = customHoursValue;
                                            newRow.Fields.CustomHours4 = customHoursValue;
                                        }


                                        // aggiunta della riga alla testata
                                        master.Slaves.SlaveBuff.Row.Add(newRow);

                                        // incremento del numero di linee
                                        rowsNumber++;
                                    }
                                }

                                // inserimento del numnero di linee per il master
                                master.Slaves.SlaveBuff.rowsnumber = rowsNumber;

                                // per ogni giorno aggiungo il master all'elenco dei masters
                                document.Documents.Masters.Master.Add(master);
                            }


                            // calcolo il nome del file preparato per Perfetto
                            string currentFileName = String.Format("{0}{1}", cantNumber.ToString("0000"), XmlToPerfettoConstants.ReturnXmlExtension);

                            //contatore del file successivo
                            var nextFileNumber = cantNumber + 1;

                            //all'ultimo file generato non metto il TAG
                            if (cantNumber < totalCant)
                                document.DocumentInfo.NextFile = String.Format("{0}", nextFileNumber.ToString("0000"));


                            // serializzazione e salvataggio del rapportino generato
                            var xsn = new XmlSerializerNamespaces();
                            xsn.Add("", "");
                            var serializer = new XmlSerializer(typeof(XmlExportsData.Perfetto.Document));
                            if (!Directory.Exists(folderpath))
                                Directory.CreateDirectory(folderpath);

                            using (TextWriter textWriter = new StreamWriter(Path.Combine(folderpath, currentFileName)))
                            using (var writer = new PerfettoWriter(textWriter))
                            {
                                writer.Formatting = Formatting.Indented;

                                serializer.Serialize(writer, document, xsn);
                                writer.Close();
                                textWriter.Close();
                            }

                            reportsFileName.Add(Path.Combine(folderpath, currentFileName));

                        }

                        // generazione dell'oggetto envelope da scrivere
                        Business.XmlExportsData.Perfetto.Envelope envelope = PrepareXmlEnvelopeToPerfetto(reportsFileName);

                        // se è stato correttamente creato un envelope
                        if (envelope != default(Business.XmlExportsData.Perfetto.Envelope))
                        {
                            using (var envelopeWriter = new StreamWriter(Path.Combine(folderpath, XmlToPerfettoConstants.EnvelopeFileName)))
                            using (var writer = new PerfettoWriter(envelopeWriter))
                            {
                                try
                                {
                                    writer.Formatting = Formatting.Indented;

                                    // Serialize the object, and close the TextWriter
                                    var serializer = new XmlSerializer(typeof(Business.XmlExportsData.Perfetto.Envelope));
                                    serializer.Serialize(writer, envelope);
                                    writer.Close();
                                    envelopeWriter.Close();
                                }
                                catch (Exception)
                                {

                                }
                                finally
                                {
                                    envelopeWriter.Close();
                                    envelopeWriter.Dispose();
                                    writer.Close();
                                }
                            }
                        }

                        // compressione dei due files generati per il ritorno del dato
                        try
                        {
                            returnFileName = Path.Combine(folderpath, String.Format("{0}{1}", Path.GetFileNameWithoutExtension(envelope.ExportID), XmlToPerfettoConstants.ReturnZipExtension));
                            var zipContent = new List<string>() { Path.Combine(folderpath, XmlToPerfettoConstants.EnvelopeFileName) };
                            zipContent.AddRange(reportsFileName);
                            CommonService.ZipFilesList(zipContent, returnFileName);
                        }
                        catch (Exception)
                        {
                            returnFileName = String.Empty;
                        }

                        // sono salvate per la prossima esecuzione l'elenco dei collaboratori/date elaborate
                        SaveProcessedDateAndCols(regVsNotYetProcessed, exportedDatesAndColIds.ToList(), filesOutputFolder);
                    }
                }
            }

            return returnFileName;
        }

        /// <summary>
        /// Effettua l'esportazione xml delle registrazioni passate come parametro verso Scs, restituendo per il download un file zip con i dati generati.
        /// </summary>
        /// <param name="regVsToProcess">Le Reg_V da processare nell'esportazione Xml.</param>
        /// <param name="filesOutputFolder">La cartella in cui salvare i dati preparati nell'export xml</param>
        /// <returns>
        /// Ritorna il percorso del file da ritornare al browser con i dati esportati
        /// </returns>
        public string PrepareXmlExportToScs(IQueryable<Reg_V> regVsToProcess, string filesOutputFolder, DateTime fine)
        {
            #region Utilities

            HashSet<DateTime> festività = new HashSet<DateTime>(RepoManager.Tab_FestiviRepo.DbSet.Select(f => f.Giorno_Tab_Festivi).ToList());

            #endregion

            string returnFileName = String.Empty;

            //controllo di avere delle reg da inserire nell'Xml
            if (regVsToProcess.Any())
            {

                // inizializzazione di quanto già esportato in precedenza
                IEnumerable<ExportedData> exportedDatesAndColIds = ReadProcessedDatesAndCols(filesOutputFolder);

                string folderpath = Path.Combine(filesOutputFolder, DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss"));

                // inizializzazione della lista di rapportini (files xml) generati
                var reportsFileName = new List<string>();

                // ciclo di elaborazione delle reg_v per codice cantiere
                // [si ciclano solamente le registrazioni, se presenti, dei collaboratori data non precedentemente processati]
                var regVsNotYetProcessed = regVsToProcess.AsEnumerable().Where(regv => !exportedDatesAndColIds.Any(exp => exp.DataReg == regv.Data_Reg && exp.ColId == regv.Col_Id)).ToList();

                if (regVsNotYetProcessed.Any())
                {
                    var regRowToProcess = new List<RegvRow>();

                    // per prima cosa si processano tutte le registrazioni per collaboratore data e si genera una lista 
                    // di oggetti che contengono i dati di registrazione che contemplano i dati di timing del giorno
                    foreach (var regVsByDate in regVsNotYetProcessed.GroupBy(regv => regv.Data_Reg).ToList()) // per ogni data
                    {
                        foreach (var regvsByDateAndCol in regVsByDate.GroupBy(regv => regv.Col_Mnemonic).ToList()) // per ogni collaboratore
                        {
                            // inizializzazione dei totali di ore ordinarie e straordinarie
                            int totalWorkingHours = 0;
                            int totalOvertimeHours = 0;

                            // inizializzo la variabile che indica se processare ancora i viaggi trasformati in ore lavorate per sottokilometraggio
                            // (se si sta processando un sabato o una domenica allora non si utilizzano i viaggi sotto i 10 Km)
                            bool noMoreUnderKmTrips = regVsByDate.Key.Value.DayOfWeek == DayOfWeek.Sunday || regVsByDate.Key.Value.DayOfWeek == DayOfWeek.Saturday;

                            // inizializzazione della lista delle registrazioni di giornata da processare verso perfetto
                            var dayRegRowToProcess = new List<RegvRow>();

                            // per ogni registrazione all'interno della data (ordinata per tipo registrazione per processare i viaggi in fondo e per ora fisica)
                            var regvsByDateAndColOrdered = regvsByDateAndCol.OrderBy(regv => regv.Data_Ora_Fis_E).ToList();
                            foreach (Reg_V regv in regvsByDateAndColOrdered)
                            {
                                // inizializzazione della variabile che indica l'intenzione di processare la registrazione (di default la si processa)
                                bool isToProcess = true;

                                // se è stata indicata l'intenzione di processare il record
                                if (isToProcess)
                                {
                                    // inizializzazione della nuova registrazione poi da esportare
                                    var newRegRow = default(RegvRow);

                                    // si procede all'elaborazione delle sole reg_v abbinate, cioè che hanno un'uscita, hanno un data registrazione e una durata
                                    int endHour = regv.Data_Ora_Fis_U != null ? Convert.ToInt32((new TimeSpan(regv.Data_Ora_Fis_U.Value.Hour, regv.Data_Ora_Fis_U.Value.Minute, 0)).TotalMinutes) * 60 : 0;
                                    // calcolo del tipo di registrazione che si sta processando (può cambiare in base ai parametri delle personalizzazioni)
                                    RegTypeEnum currentRegType = RegTypeEnum.None;
                                    bool wasTrip = false;
                                    // generazione di una nuova riga per il rapportino
                                    newRegRow = new RegvRow();

                                    // compilazione dei dati di riga
                                    newRegRow.ColId = Convert.ToInt32(regv.Col_Id);
                                    newRegRow.ColMnemonic = regv.Col_Mnemonic;
                                    newRegRow.CantId = Convert.ToInt32(regv.Cant_Id);
                                    newRegRow.CantMnemonic = regv.Cant_Mnemonic;
                                    newRegRow.DataReg = Convert.ToDateTime(regv.Data_Reg);
                                    var noteCode = String.Format("{0}#{1}#{2}", regv.Col_Mnemonic, regv.Cant_Id, regv.Data_Reg.Value.ToString("yy-MM-dd"));
                                    newRegRow.Note = String.Format("Inserimento automatico {0}", noteCode);
                                    newRegRow.StartHour = Convert.ToInt32((new TimeSpan(regv.Data_Ora_Fis_E.Hour, regv.Data_Ora_Fis_E.Minute, 0)).TotalMinutes) * 60;
                                    newRegRow.EndHour = endHour;
                                    newRegRow.DurataOre = regv.Durata_Fig.Value/60;
                                    newRegRow.DurataMinuti = regv.Durata_Fig.Value % 60;
                                    newRegRow.Motivazione = regv.Motivazione_Reg_Cod;
                                    newRegRow.Type = currentRegType;
                                    newRegRow.IsTripUnderKm = wasTrip;

                                    // inizializazione dei campi delle ore nella riga
                                    newRegRow.OrdinaryHours = 0;
                                    newRegRow.OvertimeHours = 0;
                                    newRegRow.TravelHours = 0;

                                    // calcolo durata in secondi della registrazione
                                    int regvDuration = newRegRow.EndHour - newRegRow.StartHour;

                                    // calcolo i totali parziali per la gestione della riga
                                    totalWorkingHours += currentRegType == RegTypeEnum.None ? regvDuration : 0;

                                    // se si sta elaborando un sabato, si tratta sempre di straordinari
                                    if (regv.Data_Reg.Value.DayOfWeek == DayOfWeek.Sunday || regv.Data_Reg.Value.DayOfWeek == DayOfWeek.Saturday)
                                    {
                                        newRegRow.OvertimeHours += regvDuration;
                                    }

                                    else
                                    {
                                        if (currentRegType == RegTypeEnum.None && String.IsNullOrEmpty(regv.Motivazione_Reg_Cod)) // se la registrazione è un'ora normale (cioè non un viaggio senza motivazione)
                                        {
                                            // se la durata totale del giorno è maggiore del numero di ore ordinarie configurate
                                            if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                            {
                                                // in caso non si stia processando un viaggio sotto kilometrato allora si tolgono gli elementi
                                                // viaggio già creati fino a esaurimento o rientro in ordinario
                                                if (dayRegRowToProcess.Any(regRow => regRow.IsTripUnderKm))
                                                {
                                                    // inizializzazione della lista di elementi da rimuovere dalla lista
                                                    var regvRowsToRemove = new List<RegvRow>();

                                                    // ciclo di elaborazione dei viaggi sotto kilometraggio già inseriti
                                                    foreach (RegvRow regvRow in dayRegRowToProcess.Where(regRow => regRow.IsTripUnderKm).ToList())
                                                    {
                                                        // procedo a elaborare solamente se non sono già a posto con le ore
                                                        if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                                        {
                                                            // tolgo le ore della registrrazione corrente e la marco da cancellare
                                                            totalWorkingHours -= regvRow.OrdinaryHours + regvRow.OvertimeHours;
                                                            totalOvertimeHours -= regvRow.OvertimeHours;
                                                            regvRowsToRemove.Add(regvRow);
                                                        }
                                                        else // se invece sono a posto smetto di ciclare, ho tolto il necessario
                                                            break;
                                                    }

                                                    // al termine dell'elaborazione, se ci sono da eliminare delle righe con le ore provenienti da viaggi sotto kilometrati
                                                    // lo effettuo
                                                    if (regvRowsToRemove.Any())
                                                        regvRowsToRemove.ForEach(regvRow => dayRegRowToProcess.Remove(regvRow));
                                                }

                                                // si procede alla gestione degli straordinari solamente se ce ne sono ancora
                                                if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                                {
                                                    // il totale delle ore straordinarie per la regv che si sta processando è dato dal toltale
                                                    // delle ore del coll/giorno meno le ore straordinarie già assegnate meno le ore previste in giornata
                                                    newRegRow.OvertimeHours = totalWorkingHours - totalOvertimeHours - XmlToPerfettoConstants.OrdinaryHours;
                                                    totalOvertimeHours += newRegRow.OvertimeHours;

                                                    // le ore ordinarie in questo caso sono il restante degli straordinari
                                                    newRegRow.OrdinaryHours = regvDuration - newRegRow.OvertimeHours;
                                                }
                                                else // nel caso invece si sia rientrati nell'alveo della normalità si registrano le ore ordinarie
                                                    newRegRow.OrdinaryHours = regvDuration;
                                                


                                            }
                                            else // nel caso invece non si sia superato il numero di ore ordinarrie configurate (in questo caso sono tutte ore ordinarie)
                                                newRegRow.OrdinaryHours = regvDuration;
                                        }
                                        else if (currentRegType == RegTypeEnum.None & !String.IsNullOrEmpty(regv.Motivazione_Reg_Cod)) // se la registrazione è un'ora normale con motivazione
                                        {
                                            // in base al tipo di registrazione si impostano ferie/permessi o malattie/infortuni
                                            switch (regv.Motivazione_Reg_Cod)
                                            {
                                                case "FP":
                                                    newRegRow.VacationHours = regvDuration;
                                                    break;
                                                case "M":
                                                    newRegRow.SickHours = regvDuration;
                                                    break;
                                                case "IC":
                                                    newRegRow.InjuryHours = regvDuration;
                                                    break;
                                            }
                                        }

                                    }

                                    // aggiunta della riga all'elenco (se non marcata per la non creazione)
                                    if (newRegRow != null)
                                        dayRegRowToProcess.Add(newRegRow);
                                }
                            }

                            // aggiunta dell'elenco cacolato nel giorno/collaboratore all'elenco generale
                            regRowToProcess.AddRange(dayRegRowToProcess);
                        }
                    }

                    if (regRowToProcess.Any())
                    {
                        List<XmlExportsData.Scs.Fornitura> documenti = new List<XmlExportsData.Scs.Fornitura>();
                        int mese = fine.Month;
                        string matricolaCol = "";
                        var totalCant = regRowToProcess.Where(regv => regv != null).GroupBy(regv => regv.CantMnemonic).Count();
                        regRowToProcess = regRowToProcess.OrderBy(regv => regv.DataReg).ToList();
                        foreach (var regvRowsByCol in regRowToProcess.Where(regv => regv != null).GroupBy(regv => regv.ColId).ToList())
                        {
                            DateTime ultimo = fine.EndOfMonth();
                            var document = new XmlExportsData.Scs.Fornitura();
                            // viene generato un master per ogni data all'interno della stessa commessa
                            var master = new Business.XmlExportsData.Scs.XmlMaster();

                            List<Col> collaboratore = RepoManager.ColRepo.GetAllQueryable().Where(c => c.Col_Id == regvRowsByCol.Key).ToList();

                            // ciclo di elaborazione delle timbrature per cantiere anche per collaboratore/giorno,
                            // questo per calcolare correttamente i dati di totale
                            foreach (var regvRowsByDate in regvRowsByCol.GroupBy(regv => regv.DataReg))
                            {
                                // calcolo della data attualmente in processo (formato stringa)
                                string currentRegVDate = regvRowsByDate.Key.ToString("yyyy-MM-dd");

                                // inizializzazione del numero di righe in processo
                                int rowsNumber = 0;

                                foreach (var regvRowsByDateAndCol in regvRowsByDate.GroupBy(regv => regv.ColMnemonic).ToList())
                                {
                                    int tmpOre = 0;
                                    int tmpMinuti = 0;
                                    string tmpMotivazione = "01";
                                    int dayCount = 1;
                                    DateTime tmpData = new DateTime(1999,12,31);
                                    regvRowsByDateAndCol.OrderBy(r => r.Motivazione);
                                    // per ogni registrazione all'interno della data (ordinata per tipo registrazione per processare i viaggi in fondo)
                                    foreach (var regv in regvRowsByDateAndCol.OrderBy(r => r.Motivazione)) {
                                        //controllo che il cantiere sia associato
                                        if (regv.CantId > 0 && regv.CantId != 0) {
                                            string[] data = tmpData.ToString("yyyy-MM-dd").Split(' ');
                                            //controllo se la timbratura che sto elaborando è l'ultima del giorno
                                            if (dayCount < regvRowsByDateAndCol.Count()) {
                                                //se tutti i parametri sono quelli di default vado semplicemente ad associare alle variabili i dati della registrazione corrente
                                                if (tmpOre == 0 && tmpMinuti == 0 && tmpMotivazione == "01")
                                                {
                                                    tmpData = regv.DataReg;
                                                    tmpOre += regv.DurataOre;
                                                    tmpMinuti += regv.DurataMinuti;
                                                    tmpMotivazione = regv.Motivazione;
                                                }
                                                //in caso la timbratura corrente abbia la stessa motivazione di quella precedente vado a sommare la durata con quella precedente
                                                else if (regv.DataReg == tmpData && regv.Motivazione == tmpMotivazione)
                                                {
                                                    tmpOre += regv.DurataOre;
                                                    tmpMinuti += regv.DurataMinuti;
                                                } 
                                                //se la durata della registrazione corrente è inferiore a zero vado a sottrarre da quella salvata
                                                else if (regv.DurataOre < 0 || regv.DurataMinuti < 0) {
                                                    if (regv.DurataOre == 0 && regv.DurataMinuti < 0)
                                                    {
                                                        if (tmpMinuti == 0)
                                                        {
                                                            tmpOre = tmpOre - 1;
                                                            tmpMinuti = 30;
                                                        }
                                                        else
                                                        {
                                                            tmpOre += regv.DurataOre;
                                                            tmpMinuti += regv.DurataMinuti;
                                                        }

                                                    }
                                                    else
                                                    {
                                                        if (regv.DurataMinuti == 0)
                                                        {
                                                            tmpOre += regv.DurataOre;
                                                            tmpMinuti += regv.DurataMinuti;
                                                        }
                                                        else if (regv.DurataMinuti == -15)
                                                        {
                                                            if (tmpMinuti == 0)
                                                            {
                                                                tmpMinuti = 45;
                                                                tmpOre += regv.DurataOre;
                                                                tmpOre -= 1;
                                                            }
                                                            else
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpMinuti += regv.DurataMinuti;
                                                            }
                                                        }
                                                        else if (regv.DurataMinuti == -30)
                                                        {
                                                            tmpMinuti += regv.DurataMinuti;
                                                            if (tmpMinuti >= 0)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                            }
                                                            else if (tmpMinuti < 0)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpOre = tmpOre - 1;
                                                            }
                                                        }
                                                        else if (regv.DurataMinuti == -45)
                                                        {
                                                            tmpMinuti += 60;
                                                            tmpMinuti += regv.DurataMinuti;
                                                            if (tmpMinuti >= 60)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                            }
                                                            else if (tmpMinuti < 60)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpOre = tmpOre - 1;
                                                            }
                                                        }

                                                    }
                                                    // inizializzazione della riga rapportino
                                                    Business.XmlExportsData.Scs.Movimento newRow = new Business.XmlExportsData.Scs.Movimento();
                                                    Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                    matricolaCol = collaboratore.First().Matricola_Col;

                                                    newRow.CodGiustificativoUfficiale = regv.Motivazione;
                                                    newRow.Data = data[0];
                                                    int ore = regv.DurataOre;
                                                    int minuti = regv.DurataMinuti;
                                                    if (minuti == 60)
                                                    {
                                                        minuti = 0;
                                                        ore = ore - 1;
                                                    }
                                                    if (ore < 0)
                                                    {
                                                        ore = System.Math.Abs(ore);
                                                    }
                                                    if (minuti < 0)
                                                    {
                                                        minuti = System.Math.Abs(minuti);
                                                    }
                                                    newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                    newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                    int centesimi = (minuti * 100) / 60;
                                                    if (centesimi < 0)
                                                    {
                                                        centesimi = System.Math.Abs(centesimi);
                                                    }
                                                    newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                    newRow.GiornoDiRiposo = "N";
                                                    newRow.GiornoChiusuraStraordinari = "N";
                                                    string codiceAzienda = "000000";
                                                    if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                    {
                                                        codiceAzienda = collaboratore.First().Note_Col;
                                                    }
                                                    // aggiunta della riga alla testata
                                                    if (ore > 0 || minuti > 0)
                                                    {
                                                        document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                        document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                        document.Dipendente.Masters.Master.Add(newRow);
                                                    }
                                                }
                                                //se la registrazione corrente ha una motivazione diversa da quella precedente vado a creare sul file la timbratura precedente e salvo
                                                //i dati di quella corrente
                                                else if (regv.DataReg == tmpData && regv.Motivazione != tmpMotivazione)
                                                {
                                                    // inizializzazione della riga rapportino
                                                    Business.XmlExportsData.Scs.Movimento newRow = new Business.XmlExportsData.Scs.Movimento();
                                                    Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                    matricolaCol = collaboratore.First().Matricola_Col;
                                                    string motivazione = "01";
                                                    if (tmpMotivazione != "" && tmpMotivazione != null)
                                                    {
                                                        motivazione = tmpMotivazione;
                                                    }
                                                    newRow.CodGiustificativoUfficiale = motivazione;
                                                    newRow.Data = data[0];
                                                    int ore = tmpOre;
                                                    int minuti = tmpMinuti;
                                                    if (ore > 0 || minuti > 0)
                                                    {
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        int centesimi = (minuti * 100) / 60;
                                                        if (centesimi < 0)
                                                        {
                                                            centesimi = System.Math.Abs(centesimi);
                                                        }
                                                        newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                        newRow.GiornoDiRiposo = "N";
                                                        newRow.GiornoChiusuraStraordinari = "N";
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                        document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                        document.Dipendente.Masters.Master.Add(newRow);
                                                        tmpOre = regv.DurataOre;
                                                        tmpMinuti = regv.DurataMinuti;
                                                    }
                                                    else {
                                                        tmpOre = regv.DurataOre + ore;
                                                        tmpMinuti = regv.DurataMinuti + tmpMinuti;
                                                    }
                                                    tmpData = regv.DataReg;
                                                    tmpMotivazione = regv.Motivazione;
                                                }
                                                else if (regv.DataReg != tmpData && regv.Motivazione == tmpMotivazione)
                                                {
                                                    // inizializzazione della riga rapportino
                                                    Business.XmlExportsData.Scs.Movimento newRow = new Business.XmlExportsData.Scs.Movimento();
                                                    Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                    matricolaCol = collaboratore.First().Matricola_Col;

                                                    newRow.CodGiustificativoUfficiale = tmpMotivazione;
                                                    newRow.Data = data[0];
                                                    int ore = tmpOre;
                                                    int minuti = tmpMinuti;
                                                    if (minuti == 60)
                                                    {
                                                        minuti = 0;
                                                        ore = ore + 1;
                                                    }
                                                    if (ore < 0)
                                                    {
                                                        ore = System.Math.Abs(ore);
                                                    }
                                                    if (minuti < 0)
                                                    {
                                                        minuti = System.Math.Abs(minuti);
                                                    }
                                                    newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                    newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                    int centesimi = (minuti * 100) / 60;
                                                    if (centesimi < 0)
                                                    {
                                                        centesimi = System.Math.Abs(centesimi);
                                                    }
                                                    newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                    newRow.GiornoDiRiposo = "N";
                                                    newRow.GiornoChiusuraStraordinari = "N";
                                                    string codiceAzienda = "000000";
                                                    if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                    {
                                                        codiceAzienda = collaboratore.First().Note_Col;
                                                    }
                                                    // aggiunta della riga alla testata
                                                    if (ore > 0 || minuti > 0)
                                                    {
                                                        document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                        document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                        document.Dipendente.Masters.Master.Add(newRow);
                                                    }
                                                    tmpData = regv.DataReg;
                                                    tmpOre = regv.DurataOre;
                                                    tmpMinuti = regv.DurataMinuti;
                                                    tmpMotivazione = regv.Motivazione;
                                                }
                                            } else {
                                                //se l'ultima timbratura ha la stessa motivazione di quella precedente vado ad aggiungere la durata e salvare sul file
                                                if (regv.DataReg == tmpData && regv.Motivazione == tmpMotivazione)
                                                {
                                                    tmpOre += regv.DurataOre;
                                                    tmpMinuti += regv.DurataMinuti;
                                                    // inizializzazione della riga rapportino
                                                    Business.XmlExportsData.Scs.Movimento newRow = new Business.XmlExportsData.Scs.Movimento();
                                                    Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                    matricolaCol = collaboratore.First().Matricola_Col;
                                                    string motivazione = "01";
                                                    if (regv.Motivazione != "" && regv.Motivazione != null)
                                                    {
                                                        motivazione = regv.Motivazione;
                                                    }
                                                    newRow.CodGiustificativoUfficiale = motivazione;
                                                    data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                    newRow.Data = data[0];
                                                    int ore = tmpOre;
                                                    int minuti = tmpMinuti;
                                                    if (minuti == 60)
                                                    {
                                                        minuti = 0;
                                                        ore = ore + 1;
                                                    } else if (minuti > 60) {
                                                        minuti -= 60;
                                                        ore += 1;
                                                    }
                                                    if (ore < 0)
                                                    {
                                                        ore = System.Math.Abs(ore);
                                                    }
                                                    if (minuti < 0)
                                                    {
                                                        minuti = System.Math.Abs(minuti);
                                                    }
                                                    newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                    newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                    int centesimi = (minuti * 100) / 60;
                                                    if (centesimi < 0)
                                                    {
                                                        centesimi = System.Math.Abs(centesimi);
                                                    }
                                                    newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                    newRow.GiornoDiRiposo = "N";
                                                    newRow.GiornoChiusuraStraordinari = "N";
                                                    string codiceAzienda = "000000";
                                                    if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                    {
                                                        codiceAzienda = collaboratore.First().Note_Col;
                                                    }
                                                    // aggiunta della riga alla testata
                                                    if (ore > 0)
                                                    {
                                                        document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                        document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                        document.Dipendente.Masters.Master.Add(newRow);
                                                    }
                                                }
                                                //se l'ultima timbratura ha una motivazione diversa vado a controllare i dati in nostro possesso
                                                else
                                                {
                                                    //se la timbratura precedente aveva durata negativa vado a sottrarre la durata da quella corrente e aggiungo la corrente al file
                                                    if (tmpOre < 0 || tmpMinuti < 0)
                                                    {
                                                        // inizializzazione della riga rapportino
                                                        Business.XmlExportsData.Scs.Movimento newRow = new Business.XmlExportsData.Scs.Movimento();
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        string motivazione = "01";
                                                        if (regv.Motivazione != "" && regv.Motivazione != null)
                                                        {
                                                            motivazione = regv.Motivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        int ore = regv.DurataOre - tmpOre;
                                                        int minuti = regv.DurataMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        int centesimi = (minuti * 100) / 60;
                                                        if (centesimi < 0)
                                                        {
                                                            centesimi = System.Math.Abs(centesimi);
                                                        }
                                                        newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                        newRow.GiornoDiRiposo = "N";
                                                        newRow.GiornoChiusuraStraordinari = "N";
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }

                                                        // inizializzazione della riga rapportino
                                                        newRow = new Business.XmlExportsData.Scs.Movimento();
                                                        cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;

                                                        newRow.CodGiustificativoUfficiale = tmpMotivazione;
                                                        newRow.Data = data[0];
                                                        ore = tmpOre;
                                                        minuti = tmpMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore - 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        centesimi = (minuti * 100) / 60;
                                                        if (centesimi < 0)
                                                        {
                                                            centesimi = System.Math.Abs(centesimi);
                                                        }
                                                        newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                        newRow.GiornoDiRiposo = "N";
                                                        newRow.GiornoChiusuraStraordinari = "N";
                                                        codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                    }
                                                    //invece se quella corrente ha durata negativa vado a rimuovere la durata da quella precedente e inserisco la precedente nel file
                                                    else if (regv.DurataOre < 0 || regv.DurataMinuti < 0) {
                                                        // inizializzazione della riga rapportino
                                                        Business.XmlExportsData.Scs.Movimento newRow = new Business.XmlExportsData.Scs.Movimento();
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        string motivazione = "01";
                                                        if (tmpMotivazione != "" && tmpMotivazione != null)
                                                        {
                                                            motivazione = tmpMotivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        int ore = 0;
                                                        int minuti = 0;
                                                        if (regv.DurataOre == 0 && regv.DurataMinuti < 0)
                                                        {
                                                            if (tmpMinuti == 0)
                                                            {
                                                                tmpOre = tmpOre - 1;
                                                                tmpMinuti = 30;
                                                            }
                                                            else {
                                                                tmpOre += regv.DurataOre;
                                                                tmpMinuti += regv.DurataMinuti;
                                                            }
                                                            
                                                        }
                                                        else
                                                        {
                                                            if (regv.DurataMinuti == 0)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpMinuti += regv.DurataMinuti;
                                                            }
                                                            else if (regv.DurataMinuti == -15)
                                                            {
                                                                if (tmpMinuti == 0)
                                                                {
                                                                    tmpMinuti = 45;
                                                                    tmpOre += regv.DurataOre;
                                                                    tmpOre -= 1;
                                                                }
                                                                else
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                    tmpMinuti += regv.DurataMinuti;
                                                                }
                                                            }
                                                            else if (regv.DurataMinuti == -30)
                                                            {
                                                                tmpMinuti += regv.DurataMinuti;
                                                                if (tmpMinuti >= 0)
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                }
                                                                else if (tmpMinuti < 0)
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                    tmpOre = tmpOre - 1;
                                                                }
                                                            }
                                                            else if (regv.DurataMinuti == -45)
                                                            {
                                                                tmpMinuti += 60;
                                                                tmpMinuti += regv.DurataMinuti;
                                                                if (tmpMinuti >= 60)
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                }
                                                                else if (tmpMinuti < 60)
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                    tmpOre = tmpOre - 1;
                                                                }
                                                            }
                                                            
                                                        }
                                                        ore = tmpOre;
                                                        minuti = tmpMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        int centesimi = (minuti * 100) / 60;
                                                        if (centesimi < 0)
                                                        {
                                                            centesimi = System.Math.Abs(centesimi);
                                                        }
                                                        newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                        newRow.GiornoDiRiposo = "N";
                                                        newRow.GiornoChiusuraStraordinari = "N";
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0) {
                                                            document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }

                                                        // inizializzazione della riga rapportino
                                                        newRow = new Business.XmlExportsData.Scs.Movimento();
                                                        cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;

                                                        newRow.CodGiustificativoUfficiale = regv.Motivazione;
                                                        newRow.Data = data[0];
                                                        ore = regv.DurataOre;
                                                        minuti = regv.DurataMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore - 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        centesimi = (minuti * 100) / 60;
                                                        if (centesimi < 0)
                                                        {
                                                            centesimi = System.Math.Abs(centesimi);
                                                        }
                                                        newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                        newRow.GiornoDiRiposo = "N";
                                                        newRow.GiornoChiusuraStraordinari = "N";
                                                        codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                    }
                                                    //se i dati invece sono quelli di default vado ad aggiungere la registrazione corrente del file
                                                    else if (tmpOre == 0 && tmpMinuti == 0 && tmpMotivazione == "01") {
                                                        // inizializzazione della riga rapportino
                                                        Business.XmlExportsData.Scs.Movimento newRow = new Business.XmlExportsData.Scs.Movimento();
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        string motivazione = "01";
                                                        if (regv.Motivazione != "" && regv.Motivazione != null)
                                                        {
                                                            motivazione = regv.Motivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        int ore = regv.DurataOre;
                                                        int minuti = regv.DurataMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        int centesimi = (minuti * 100) / 60;
                                                        if (centesimi < 0)
                                                        {
                                                            centesimi = System.Math.Abs(centesimi);
                                                        }
                                                        newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                        newRow.GiornoDiRiposo = "N";
                                                        newRow.GiornoChiusuraStraordinari = "N";
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                    }
                                                    //se non si è entrati in nessuna dell condizioni precedenti vado sempicemente ad aggiungere sia la timbratura precedente che la corrente
                                                    //nel file
                                                    else {
                                                        // inizializzazione della riga rapportino
                                                        Business.XmlExportsData.Scs.Movimento newRow = new Business.XmlExportsData.Scs.Movimento();
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        string motivazione = "01";
                                                        if (tmpMotivazione != "" && tmpMotivazione != null)
                                                        {
                                                            motivazione = tmpMotivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        int ore = tmpOre;
                                                        int minuti = tmpMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        int centesimi = (minuti * 100) / 60;
                                                        if (centesimi < 0)
                                                        {
                                                            centesimi = System.Math.Abs(centesimi);
                                                        }
                                                        newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                        newRow.GiornoDiRiposo = "N";
                                                        newRow.GiornoChiusuraStraordinari = "N";
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                        // inizializzazione della riga rapportino
                                                        newRow = new Business.XmlExportsData.Scs.Movimento();
                                                        cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        motivazione = "01";
                                                        if (regv.Motivazione != "" && regv.Motivazione != null)
                                                        {
                                                            motivazione = regv.Motivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        ore = regv.DurataOre;
                                                        minuti = regv.DurataMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        centesimi = (minuti * 100) / 60;
                                                        if (centesimi < 0)
                                                        {
                                                            centesimi = System.Math.Abs(centesimi);
                                                        }
                                                        newRow.NumMinutiInCentesimi = CommonService.AggiungiZeriASinistra(centesimi.ToString(), 2);
                                                        newRow.GiornoDiRiposo = "N";
                                                        newRow.GiornoChiusuraStraordinari = "N";
                                                        codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        dayCount++;
                                    }
                                }
                            }
                            int i = 1;
                            DateTime inizio = new DateTime(ultimo.Year,ultimo.Month,01);
                            if (collaboratore.First().Data_Disponibilita_Inizio_Col > inizio) {
                                i = collaboratore.First().Data_Disponibilita_Inizio_Col.Value.Day;
                            }
                            int giorni = 31;
                            int month = 0;
                            int anno = 0;
                            string lastCant = "000000";
                            string LastPosizione = "000";
                            foreach (var regvRowsByDate in regvRowsByCol.GroupBy(regv => regv.DataReg))
                            {
                                // calcolo della data attualmente in processo (formato stringa)
                                string currentRegVDate = regvRowsByDate.Key.ToString("yyyy-MM-dd");                                
                                if (mese == 2) {
                                    giorni = regvRowsByDate.Key.EndOfMonth().Day;
                                } else if (mese == 11 || mese == 4 || mese == 6 || mese == 9) {
                                    giorni = 30;
                                }

                                if (i != regvRowsByDate.Key.Day)
                                {
                                    if (i< regvRowsByDate.Key.Day)
                                    {
                                        while (i < regvRowsByDate.Key.Day)
                                        {
                                            // inizializzazione della riga rapportino
                                            Business.XmlExportsData.Scs.ZonaCantiere newRow = new Business.XmlExportsData.Scs.ZonaCantiere();
                                            // compilazione dei dati di riga
                                            matricolaCol = collaboratore.First().Matricola_Col;
                                            if (i < 10)
                                            {
                                                if (mese < 10)
                                                {
                                                    newRow.DataMovimento = "" + regvRowsByDate.Key.Year + "-0" + regvRowsByDate.Key.Month + "-0" + i;
                                                }
                                                else {
                                                    newRow.DataMovimento = "" + regvRowsByDate.Key.Year + "-" + regvRowsByDate.Key.Month + "-0" + i;
                                                }
                                            }
                                            else
                                            {
                                                if (mese < 10)
                                                {
                                                    newRow.DataMovimento = "" + regvRowsByDate.Key.Year + "-0" + regvRowsByDate.Key.Month + "-" + i;
                                                }
                                                else {
                                                    newRow.DataMovimento = "" + regvRowsByDate.Key.Year + "-" + regvRowsByDate.Key.Month + "-" + i;
                                                }    
                                            }
                                            foreach (var regvRowsByDateAndCol in regvRowsByDate.GroupBy(regv => regv.ColMnemonic).ToList())
                                            {
                                                int j = 0;
                                                string LastMovimento = "";
                                                string test = regvRowsByDateAndCol.Key;
                                                // vado a generare un nodo forzatureZone cantieri per giorno
                                                foreach (var regv in regvRowsByDateAndCol.OrderBy(r => r.StartHour))
                                                {
                                                    if (regv.CantId > 0 && regv.CantId != 0)
                                                    {
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        string posizione = "000";
                                                        if (cantiere.Note_Can != "" && cantiere.Note_Can != null)
                                                        {
                                                            posizione = cantiere.Note_Can;
                                                        }
                                                        newRow.IdPosizione = CommonService.AggiungiZeriASinistra(posizione, 3);
                                                        string codice = "000000";
                                                        if (cantiere.Codice_Commessa_Can != "" && cantiere.Codice_Commessa_Can != null)
                                                        {
                                                            codice = cantiere.Codice_Commessa_Can;
                                                        }
                                                        else if (cantiere.Codice_Gestionale_Can != "" && cantiere.Codice_Gestionale_Can != null)
                                                        {
                                                            codice = cantiere.Codice_Gestionale_Can;
                                                            codice = CommonService.AggiungiZeriASinistra(codice, 6);
                                                        }
                                                        newRow.CodiceCantiere = codice;
                                                        if (LastMovimento != newRow.DataMovimento)
                                                        {
                                                            document.Dipendente.ForzatureZoneCantieri.Add(newRow);
                                                        }
                                                        LastMovimento = newRow.DataMovimento;
                                                    }
                                                }
                                            }
                                            i++;
                                        }
                                    }
                                    else
                                    {
                                        i++;
                                    }
                                }

                                foreach (var regvRowsByDateAndCol in regvRowsByDate.GroupBy(regv => regv.ColMnemonic).ToList())
                                {
                                    int j = 0;
                                    string LastMovimento = "";
                                    string test = regvRowsByDateAndCol.Key;
                                    // vado a generare un nodo forzatureZone cantieri per giorno
                                    foreach (var regv in regvRowsByDateAndCol.OrderBy(r => r.StartHour))
                                    {
                                        if (regv.CantId > 0 && regv.CantId != 0)
                                        {
                                            // inizializzazione della riga rapportino
                                            Business.XmlExportsData.Scs.ZonaCantiere newRow = new Business.XmlExportsData.Scs.ZonaCantiere();

                                            Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                            // compilazione dei dati di riga
                                            //newRow.number = rowsNumber;
                                            string motivazione = "01";
                                            if (regv.Motivazione != "" && regv.Motivazione != null)
                                            {
                                                motivazione = regv.Motivazione;
                                            }
                                            matricolaCol = collaboratore.First().Matricola_Col;
                                            string[] data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                            newRow.DataMovimento = data[0];
                                            string posizione = "000";
                                            if (cantiere.Note_Can != "" && cantiere.Note_Can != null) {
                                                posizione = cantiere.Note_Can;
                                            }
                                            LastPosizione = posizione;
                                            newRow.IdPosizione = CommonService.AggiungiZeriASinistra(posizione, 3);
                                            string codice = "000000";
                                            if (cantiere.Codice_Commessa_Can != "" && cantiere.Codice_Commessa_Can != null) {
                                                codice = cantiere.Codice_Commessa_Can;
                                            } else if (cantiere.Codice_Gestionale_Can != "" && cantiere.Codice_Gestionale_Can != null) {
                                                codice = cantiere.Codice_Gestionale_Can;
                                                codice = CommonService.AggiungiZeriASinistra(codice, 6);
                                            }
                                            newRow.CodiceCantiere = codice;
                                            lastCant = codice;
                                            // aggiunta della riga alla testata
                                            if (LastMovimento != newRow.DataMovimento) {
                                                document.Dipendente.ForzatureZoneCantieri.Add(newRow);
                                            }
                                            LastMovimento = newRow.DataMovimento;
                                            month = regv.DataReg.Month;
                                            anno = regv.DataReg.Year;
                                        }
                                    }
                                }
                                i++;  
                            }
                            while (i <= giorni)
                            {
                                // inizializzazione della riga rapportino
                                Business.XmlExportsData.Scs.ZonaCantiere newRow = new Business.XmlExportsData.Scs.ZonaCantiere();
                                // compilazione dei dati di riga
                                matricolaCol = collaboratore.First().Matricola_Col;
                                if (i < 10)
                                {
                                    if (month < 10)
                                    {
                                        newRow.DataMovimento = "" + anno + "-0" + month + "-0" + i;
                                    }
                                    else
                                    {
                                        newRow.DataMovimento = "" + anno + "-" + month + "-0" + i;
                                    }
                                }
                                else {
                                    if (month < 10)
                                    {
                                        newRow.DataMovimento = "" + anno + "-0" + month + "-" + i;
                                    }
                                    else
                                    {
                                        newRow.DataMovimento = "" + anno + "-" + month + "-" + i;
                                    }
                                }
                                
                                newRow.IdPosizione = CommonService.AggiungiZeriASinistra(LastPosizione, 3);
                                newRow.CodiceCantiere = lastCant;

                                // aggiunta della riga alla testata
                                document.Dipendente.ForzatureZoneCantieri.Add(newRow);
                                i++;
                            }
                            if (matricolaCol != "") {
                                // calcolo il nome del file preparato per Perfetto
                                string currentFileName = String.Format("{0}{1}", matricolaCol, XmlToPerfettoConstants.ReturnXmlExtension);
                                matricolaCol = "";

                                // serializzazione e salvataggio del rapportino generato
                                var xsn = new XmlSerializerNamespaces();
                                xsn.Add("", "");
                                var serializer = new XmlSerializer(typeof(XmlExportsData.Scs.Fornitura));
                                if (!Directory.Exists(folderpath))
                                    Directory.CreateDirectory(folderpath);

                                using (TextWriter textWriter = new StreamWriter(Path.Combine(folderpath, currentFileName)))
                                using (var writer = new ScsWriter(textWriter))
                                {
                                    writer.Formatting = Formatting.Indented;

                                    serializer.Serialize(writer, document, xsn);
                                    writer.Close();
                                    textWriter.Close();
                                }

                                reportsFileName.Add(Path.Combine(folderpath, currentFileName));
                            }
                        }

                        // generazione dell'oggetto envelope da scrivere
                        Business.XmlExportsData.Scs.Envelope envelope = PrepareXmlEnvelopeToScs(reportsFileName);

                        // se è stato correttamente creato un envelope
                        if (envelope != default(Business.XmlExportsData.Scs.Envelope))
                        {
                            using (var envelopeWriter = new StreamWriter(Path.Combine(folderpath, XmlToPerfettoConstants.EnvelopeFileName)))
                            using (var writer = new ScsWriter(envelopeWriter))
                            {
                                try
                                {
                                    writer.Formatting = Formatting.Indented;

                                    // Serialize the object, and close the TextWriter
                                    var serializer = new XmlSerializer(typeof(Business.XmlExportsData.Scs.Envelope));
                                    serializer.Serialize(writer, envelope);
                                    writer.Close();
                                    envelopeWriter.Close();
                                }
                                catch (Exception)
                                {

                                }
                                finally
                                {
                                    envelopeWriter.Close();
                                    envelopeWriter.Dispose();
                                    writer.Close();
                                }
                            }
                        }

                        // compressione dei due files generati per il ritorno del dato
                        try
                        {
                            returnFileName = Path.Combine(folderpath, String.Format("{0}{1}", Path.GetFileNameWithoutExtension(envelope.ExportID), XmlToPerfettoConstants.ReturnZipExtension));
                            var zipContent = new List<string>() { Path.Combine(folderpath, XmlToPerfettoConstants.EnvelopeFileName) };
                            zipContent.AddRange(reportsFileName);
                            CommonService.ZipFilesList(zipContent, returnFileName);
                        }
                        catch (Exception)
                        {
                            returnFileName = String.Empty;
                        }

                        // sono salvate per la prossima esecuzione l'elenco dei collaboratori/date elaborate
                        SaveProcessedDateAndCols(regVsNotYetProcessed, exportedDatesAndColIds.ToList(), filesOutputFolder);
                    }
                }
            }

            return returnFileName;
        }

        /// <summary>
        /// Effettua l'esportazione xml delle registrazioni passate come parametro verso Orlando, restituendo per il download un file zip con i dati generati.
        /// </summary>
        /// <param name="regVsToProcess">Le Reg_V da processare nell'esportazione Xml.</param>
        /// <param name="filesOutputFolder">La cartella in cui salvare i dati preparati nell'export xml</param>
        /// <returns>
        /// Ritorna il percorso del file da ritornare al browser con i dati esportati
        /// </returns>
        public string PrepareXmlExportToOrlando(IQueryable<Reg_V> regVsToProcess, string filesOutputFolder, DateTime fine)
        {
            #region Utilities

            HashSet<DateTime> festività = new HashSet<DateTime>(RepoManager.Tab_FestiviRepo.DbSet.Select(f => f.Giorno_Tab_Festivi).ToList());

            #endregion

            string returnFileName = String.Empty;

            //controllo di avere delle reg da inserire nell'Xml
            if (regVsToProcess.Any())
            {

                // inizializzazione di quanto già esportato in precedenza
                IEnumerable<ExportedData> exportedDatesAndColIds = ReadProcessedDatesAndCols(filesOutputFolder);

                string folderpath = Path.Combine(filesOutputFolder, DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss"));

                // inizializzazione della lista di rapportini (files xml) generati
                var reportsFileName = new List<string>();

                if (regVsToProcess.Any())
                {
                    var regRowToProcess = new List<RegvRow>();

                    // per prima cosa si processano tutte le registrazioni per collaboratore data e si genera una lista 
                    // di oggetti che contengono i dati di registrazione che contemplano i dati di timing del giorno
                    foreach (var regVsByDate in regVsToProcess.GroupBy(regv => regv.Data_Reg).ToList()) // per ogni data
                    {
                        foreach (var regvsByDateAndCol in regVsByDate.GroupBy(regv => regv.Col_Mnemonic).ToList()) // per ogni collaboratore
                        {
                            // inizializzazione dei totali di ore ordinarie e straordinarie
                            int totalWorkingHours = 0;
                            int totalOvertimeHours = 0;

                            // inizializzo la variabile che indica se processare ancora i viaggi trasformati in ore lavorate per sottokilometraggio
                            // (se si sta processando un sabato o una domenica allora non si utilizzano i viaggi sotto i 10 Km)
                            bool noMoreUnderKmTrips = regVsByDate.Key.Value.DayOfWeek == DayOfWeek.Sunday || regVsByDate.Key.Value.DayOfWeek == DayOfWeek.Saturday;

                            // inizializzazione della lista delle registrazioni di giornata da processare verso perfetto
                            var dayRegRowToProcess = new List<RegvRow>();

                            // per ogni registrazione all'interno della data (ordinata per tipo registrazione per processare i viaggi in fondo e per ora fisica)
                            var regvsByDateAndColOrdered = regvsByDateAndCol.OrderBy(regv => regv.Data_Ora_Fis_E).ToList();
                            foreach (Reg_V regv in regvsByDateAndColOrdered)
                            {
                                // inizializzazione della variabile che indica l'intenzione di processare la registrazione (di default la si processa)
                                bool isToProcess = true;

                                // se è stata indicata l'intenzione di processare il record
                                if (isToProcess)
                                {
                                    // inizializzazione della nuova registrazione poi da esportare
                                    var newRegRow = default(RegvRow);

                                    // si procede all'elaborazione delle sole reg_v abbinate, cioè che hanno un'uscita, hanno un data registrazione e una durata
                                    int endHour = regv.Data_Ora_Fis_U != null ? Convert.ToInt32((new TimeSpan(regv.Data_Ora_Fis_U.Value.Hour, regv.Data_Ora_Fis_U.Value.Minute, 0)).TotalMinutes) * 60 : 0;
                                    // calcolo del tipo di registrazione che si sta processando (può cambiare in base ai parametri delle personalizzazioni)
                                    RegTypeEnum currentRegType = RegTypeEnum.None;
                                    bool wasTrip = false;
                                    // generazione di una nuova riga per il rapportino
                                    newRegRow = new RegvRow();

                                    // compilazione dei dati di riga
                                    newRegRow.ColId = Convert.ToInt32(regv.Col_Id);
                                    newRegRow.ColMnemonic = regv.Col_Mnemonic;
                                    newRegRow.CantId = Convert.ToInt32(regv.Cant_Id);
                                    newRegRow.CantMnemonic = regv.Cant_Mnemonic;
                                    newRegRow.DataReg = Convert.ToDateTime(regv.Data_Reg);
                                    var noteCode = String.Format("{0}#{1}#{2}", regv.Col_Mnemonic, regv.Cant_Id, regv.Data_Reg.Value.ToString("yy-MM-dd"));
                                    newRegRow.Note = String.Format("Inserimento automatico {0}", noteCode);
                                    newRegRow.StartHour = Convert.ToInt32((new TimeSpan(regv.Data_Ora_Fis_E.Hour, regv.Data_Ora_Fis_E.Minute, 0)).TotalMinutes) * 60;
                                    newRegRow.EndHour = endHour;
                                    newRegRow.DurataOre = regv.Durata_Fig.Value / 60;
                                    newRegRow.DurataMinuti = regv.Durata_Fig.Value % 60;
                                    newRegRow.Motivazione = regv.Motivazione_Reg_Cod;
                                    newRegRow.Type = currentRegType;
                                    newRegRow.IsTripUnderKm = wasTrip;

                                    // inizializazione dei campi delle ore nella riga
                                    newRegRow.OrdinaryHours = 0;
                                    newRegRow.OvertimeHours = 0;
                                    newRegRow.TravelHours = 0;

                                    // calcolo durata in secondi della registrazione
                                    int regvDuration = newRegRow.EndHour - newRegRow.StartHour;

                                    // calcolo i totali parziali per la gestione della riga
                                    totalWorkingHours += currentRegType == RegTypeEnum.None ? regvDuration : 0;

                                    // se si sta elaborando un sabato, si tratta sempre di straordinari
                                    if (regv.Data_Reg.Value.DayOfWeek == DayOfWeek.Sunday || regv.Data_Reg.Value.DayOfWeek == DayOfWeek.Saturday)
                                    {
                                        newRegRow.OvertimeHours += regvDuration;
                                    }

                                    else
                                    {
                                        if (currentRegType == RegTypeEnum.None && String.IsNullOrEmpty(regv.Motivazione_Reg_Cod)) // se la registrazione è un'ora normale (cioè non un viaggio senza motivazione)
                                        {
                                            // se la durata totale del giorno è maggiore del numero di ore ordinarie configurate
                                            if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                            {
                                                // in caso non si stia processando un viaggio sotto kilometrato allora si tolgono gli elementi
                                                // viaggio già creati fino a esaurimento o rientro in ordinario
                                                if (dayRegRowToProcess.Any(regRow => regRow.IsTripUnderKm))
                                                {
                                                    // inizializzazione della lista di elementi da rimuovere dalla lista
                                                    var regvRowsToRemove = new List<RegvRow>();

                                                    // ciclo di elaborazione dei viaggi sotto kilometraggio già inseriti
                                                    foreach (RegvRow regvRow in dayRegRowToProcess.Where(regRow => regRow.IsTripUnderKm).ToList())
                                                    {
                                                        // procedo a elaborare solamente se non sono già a posto con le ore
                                                        if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                                        {
                                                            // tolgo le ore della registrrazione corrente e la marco da cancellare
                                                            totalWorkingHours -= regvRow.OrdinaryHours + regvRow.OvertimeHours;
                                                            totalOvertimeHours -= regvRow.OvertimeHours;
                                                            regvRowsToRemove.Add(regvRow);
                                                        }
                                                        else // se invece sono a posto smetto di ciclare, ho tolto il necessario
                                                            break;
                                                    }

                                                    // al termine dell'elaborazione, se ci sono da eliminare delle righe con le ore provenienti da viaggi sotto kilometrati
                                                    // lo effettuo
                                                    if (regvRowsToRemove.Any())
                                                        regvRowsToRemove.ForEach(regvRow => dayRegRowToProcess.Remove(regvRow));
                                                }

                                                // si procede alla gestione degli straordinari solamente se ce ne sono ancora
                                                if (totalWorkingHours > XmlToPerfettoConstants.OrdinaryHours)
                                                {
                                                    // il totale delle ore straordinarie per la regv che si sta processando è dato dal toltale
                                                    // delle ore del coll/giorno meno le ore straordinarie già assegnate meno le ore previste in giornata
                                                    newRegRow.OvertimeHours = totalWorkingHours - totalOvertimeHours - XmlToPerfettoConstants.OrdinaryHours;
                                                    totalOvertimeHours += newRegRow.OvertimeHours;

                                                    // le ore ordinarie in questo caso sono il restante degli straordinari
                                                    newRegRow.OrdinaryHours = regvDuration - newRegRow.OvertimeHours;
                                                }
                                                else // nel caso invece si sia rientrati nell'alveo della normalità si registrano le ore ordinarie
                                                    newRegRow.OrdinaryHours = regvDuration;



                                            }
                                            else // nel caso invece non si sia superato il numero di ore ordinarrie configurate (in questo caso sono tutte ore ordinarie)
                                                newRegRow.OrdinaryHours = regvDuration;
                                        }
                                        else if (currentRegType == RegTypeEnum.None & !String.IsNullOrEmpty(regv.Motivazione_Reg_Cod)) // se la registrazione è un'ora normale con motivazione
                                        {
                                            // in base al tipo di registrazione si impostano ferie/permessi o malattie/infortuni
                                            switch (regv.Motivazione_Reg_Cod)
                                            {
                                                case "FP":
                                                    newRegRow.VacationHours = regvDuration;
                                                    break;
                                                case "M":
                                                    newRegRow.SickHours = regvDuration;
                                                    break;
                                                case "IC":
                                                    newRegRow.InjuryHours = regvDuration;
                                                    break;
                                            }
                                        }

                                    }

                                    // aggiunta della riga all'elenco (se non marcata per la non creazione)
                                    if (newRegRow != null)
                                        dayRegRowToProcess.Add(newRegRow);
                                }
                            }

                            // aggiunta dell'elenco cacolato nel giorno/collaboratore all'elenco generale
                            regRowToProcess.AddRange(dayRegRowToProcess);
                        }
                    }

                    if (regRowToProcess.Any())
                    {
                        List<XmlExportsData.Orlando.Fornitura> documenti = new List<XmlExportsData.Orlando.Fornitura>();
                        int mese = fine.Month;
                        string matricolaCol = "";
                        var totalCant = regRowToProcess.Where(regv => regv != null).GroupBy(regv => regv.CantMnemonic).Count();
                        regRowToProcess = regRowToProcess.OrderBy(regv => regv.DataReg).ToList();
                        foreach (var regvRowsByCol in regRowToProcess.Where(regv => regv != null).GroupBy(regv => regv.ColId).ToList())
                        {
                            DateTime ultimo = fine.EndOfMonth();
                            var document = new XmlExportsData.Orlando.Fornitura();
                            // viene generato un master per ogni data all'interno della stessa commessa
                            var master = new Business.XmlExportsData.Orlando.XmlMaster();

                            List<Col> collaboratore = RepoManager.ColRepo.GetAllQueryable().Where(c => c.Col_Id == regvRowsByCol.Key).ToList();

                            int oreLimite = 8;
                            int minutiLimite = 0;

                            if (collaboratore.First().Col_Id == 5104) {
                                oreLimite = 9;
                            } else if (collaboratore.First().Col_Id == 5105) {
                                oreLimite = 6;
                                minutiLimite = 30;
                            }

                            // ciclo di elaborazione delle timbrature per cantiere anche per collaboratore/giorno,
                            // questo per calcolare correttamente i dati di totale
                            foreach (var regvRowsByDate in regvRowsByCol.GroupBy(regv => regv.DataReg))
                            {
                                // calcolo della data attualmente in processo (formato stringa)
                                string currentRegVDate = regvRowsByDate.Key.ToString("yyyy-MM-dd");

                                // inizializzazione del numero di righe in processo
                                int rowsNumber = 0;

                                foreach (var regvRowsByDateAndCol in regvRowsByDate.GroupBy(regv => regv.ColMnemonic).ToList())
                                {
                                    int tmpOre = 0;
                                    int tmpMinuti = 0;
                                    string tmpMotivazione = "01";
                                    int dayCount = 1;
                                    DateTime tmpData = new DateTime(1999, 12, 31);
                                    regvRowsByDateAndCol.OrderBy(r => r.Motivazione);
                                    // per ogni registrazione all'interno della data (ordinata per tipo registrazione per processare i viaggi in fondo)
                                    foreach (var regv in regvRowsByDateAndCol.OrderBy(r => r.Motivazione))
                                    {
                                        //controllo che il cantiere sia associato
                                        if (regv.CantId > 0 && regv.CantId != 0)
                                        {
                                            string[] data = tmpData.ToString("yyyy-MM-dd").Split(' ');
                                            //controllo se la timbratura che sto elaborando è l'ultima del giorno
                                            if (dayCount < regvRowsByDateAndCol.Count())
                                            {
                                                //se tutti i parametri sono quelli di default vado semplicemente ad associare alle variabili i dati della registrazione corrente
                                                if (tmpOre == 0 && tmpMinuti == 0 && tmpMotivazione == "01")
                                                {
                                                    tmpData = regv.DataReg;
                                                    tmpOre += regv.DurataOre;
                                                    tmpMinuti += regv.DurataMinuti;
                                                    tmpMotivazione = regv.Motivazione;
                                                }
                                                //in caso la timbratura corrente abbia la stessa motivazione di quella precedente vado a sommare la durata con quella precedente
                                                else if (regv.DataReg == tmpData && regv.Motivazione == tmpMotivazione)
                                                {
                                                    tmpOre += regv.DurataOre;
                                                    tmpMinuti += regv.DurataMinuti;
                                                }
                                                //se la durata della registrazione corrente è inferiore a zero vado a sottrarre da quella salvata
                                                else if (regv.DurataOre < 0 || regv.DurataMinuti < 0)
                                                {
                                                    if (regv.DurataOre == 0 && regv.DurataMinuti < 0)
                                                    {
                                                        if (tmpMinuti == 0)
                                                        {
                                                            tmpOre = tmpOre - 1;
                                                            tmpMinuti = 30;
                                                        }
                                                        else
                                                        {
                                                            tmpOre += regv.DurataOre;
                                                            tmpMinuti += regv.DurataMinuti;
                                                        }

                                                    }
                                                    else
                                                    {
                                                        if (regv.DurataMinuti == 0)
                                                        {
                                                            tmpOre += regv.DurataOre;
                                                            tmpMinuti += regv.DurataMinuti;
                                                        }
                                                        else if (regv.DurataMinuti == -15)
                                                        {
                                                            if (tmpMinuti == 0)
                                                            {
                                                                tmpMinuti = 45;
                                                                tmpOre += regv.DurataOre;
                                                                tmpOre -= 1;
                                                            }
                                                            else
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpMinuti += regv.DurataMinuti;
                                                            }
                                                        }
                                                        else if (regv.DurataMinuti == -30)
                                                        {
                                                            tmpMinuti += regv.DurataMinuti;
                                                            if (tmpMinuti >= 0)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                            }
                                                            else if (tmpMinuti < 0)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpOre = tmpOre - 1;
                                                            }
                                                        }
                                                        else if (regv.DurataMinuti == -45)
                                                        {
                                                            tmpMinuti += 60;
                                                            tmpMinuti += regv.DurataMinuti;
                                                            if (tmpMinuti >= 60)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                            }
                                                            else if (tmpMinuti < 60)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpOre = tmpOre - 1;
                                                            }
                                                        }

                                                    }
                                                    // inizializzazione della riga rapportino
                                                    Business.XmlExportsData.Orlando.Movimento newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                    Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                    matricolaCol = collaboratore.First().Matricola_Col;

                                                    newRow.CodGiustificativoUfficiale = regv.Motivazione;
                                                    newRow.Data = data[0];
                                                    int ore = regv.DurataOre;
                                                    int minuti = regv.DurataMinuti;
                                                    if (minuti == 60)
                                                    {
                                                        minuti = 0;
                                                        ore = ore - 1;
                                                    }
                                                    if (ore < 0)
                                                    {
                                                        ore = System.Math.Abs(ore);
                                                    }
                                                    if (minuti < 0)
                                                    {
                                                        minuti = System.Math.Abs(minuti);
                                                    }
                                                    newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                    newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                    string codiceAzienda = "000000";
                                                    if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                    {
                                                        codiceAzienda = collaboratore.First().Note_Col;
                                                    }
                                                    // aggiunta della riga alla testata
                                                    if (ore > 0 || minuti > 0)
                                                    {
                                                        document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                        document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                        document.Dipendente.Masters.Master.Add(newRow);
                                                    }
                                                }
                                                //se la registrazione corrente ha una motivazione diversa da quella precedente vado a creare sul file la timbratura precedente e salvo
                                                //i dati di quella corrente
                                                else if (regv.DataReg == tmpData && regv.Motivazione != tmpMotivazione)
                                                {
                                                    // inizializzazione della riga rapportino
                                                    Business.XmlExportsData.Orlando.Movimento newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                    Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                    matricolaCol = collaboratore.First().Matricola_Col;
                                                    string motivazione = "01";
                                                    if (tmpMotivazione != "" && tmpMotivazione != null)
                                                    {
                                                        motivazione = tmpMotivazione;
                                                    }
                                                    newRow.CodGiustificativoUfficiale = motivazione;
                                                    newRow.Data = data[0];
                                                    int ore = tmpOre;
                                                    int minuti = tmpMinuti;
                                                    if (ore > 0 || minuti > 0)
                                                    {
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                        {
                                                            ore = oreLimite;
                                                            minuti = minutiLimite;
                                                        }
                                                        
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                        document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                        document.Dipendente.Masters.Master.Add(newRow);
                                                        tmpOre = regv.DurataOre;
                                                        tmpMinuti = regv.DurataMinuti;
                                                    }
                                                    else
                                                    {
                                                        tmpOre = regv.DurataOre + ore;
                                                        tmpMinuti = regv.DurataMinuti + tmpMinuti;
                                                    }
                                                    tmpData = regv.DataReg;
                                                    tmpMotivazione = regv.Motivazione;
                                                }
                                                else if (regv.DataReg != tmpData && regv.Motivazione == tmpMotivazione)
                                                {
                                                    // inizializzazione della riga rapportino
                                                    Business.XmlExportsData.Orlando.Movimento newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                    Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                    matricolaCol = collaboratore.First().Matricola_Col;

                                                    newRow.CodGiustificativoUfficiale = tmpMotivazione;
                                                    newRow.Data = data[0];
                                                    int ore = tmpOre;
                                                    int minuti = tmpMinuti;
                                                    if (minuti == 60)
                                                    {
                                                        minuti = 0;
                                                        ore = ore + 1;
                                                    }
                                                    if (ore < 0)
                                                    {
                                                        ore = System.Math.Abs(ore);
                                                    }
                                                    if (minuti < 0)
                                                    {
                                                        minuti = System.Math.Abs(minuti);
                                                    }
                                                    if (ore > 8 || (ore == 8 && minuti > 0))
                                                    {
                                                        ore = 8;
                                                        minuti = 0;
                                                    }
                                                    newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                    newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                    string codiceAzienda = "000000";
                                                    if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                    {
                                                        codiceAzienda = collaboratore.First().Note_Col;
                                                    }
                                                    // aggiunta della riga alla testata
                                                    if (ore > 0 || minuti > 0)
                                                    {
                                                        document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                        document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                        document.Dipendente.Masters.Master.Add(newRow);
                                                    }
                                                    tmpData = regv.DataReg;
                                                    tmpOre = regv.DurataOre;
                                                    tmpMinuti = regv.DurataMinuti;
                                                    tmpMotivazione = regv.Motivazione;
                                                }
                                            }
                                            else
                                            {
                                                //se l'ultima timbratura ha la stessa motivazione di quella precedente vado ad aggiungere la durata e salvare sul file
                                                if (regv.DataReg == tmpData && regv.Motivazione == tmpMotivazione)
                                                {
                                                    tmpOre += regv.DurataOre;
                                                    tmpMinuti += regv.DurataMinuti;
                                                    // inizializzazione della riga rapportino
                                                    Business.XmlExportsData.Orlando.Movimento newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                    Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                    matricolaCol = collaboratore.First().Matricola_Col;
                                                    string motivazione = "01";
                                                    if (regv.Motivazione != "" && regv.Motivazione != null)
                                                    {
                                                        motivazione = regv.Motivazione;
                                                    }
                                                    newRow.CodGiustificativoUfficiale = motivazione;
                                                    data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                    newRow.Data = data[0];
                                                    int ore = tmpOre;
                                                    int minuti = tmpMinuti;
                                                    if (minuti == 60)
                                                    {
                                                        minuti = 0;
                                                        ore = ore + 1;
                                                    }
                                                    else if (minuti > 60)
                                                    {
                                                        minuti -= 60;
                                                        ore += 1;
                                                    }
                                                    if (ore < 0)
                                                    {
                                                        ore = System.Math.Abs(ore);
                                                    }
                                                    if (minuti < 0)
                                                    {
                                                        minuti = System.Math.Abs(minuti);
                                                    }
                                                    if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                    {
                                                        ore = oreLimite;
                                                        minuti = minutiLimite;
                                                    }
                                                    newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                    newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                    string codiceAzienda = "000000";
                                                    if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                    {
                                                        codiceAzienda = collaboratore.First().Note_Col;
                                                    }
                                                    // aggiunta della riga alla testata
                                                    if (ore > 0)
                                                    {
                                                        document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                        document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                        document.Dipendente.Masters.Master.Add(newRow);
                                                    }
                                                }
                                                //se l'ultima timbratura ha una motivazione diversa vado a controllare i dati in nostro possesso
                                                else
                                                {
                                                    //se la timbratura precedente aveva durata negativa vado a sottrarre la durata da quella corrente e aggiungo la corrente al file
                                                    if (tmpOre < 0 || tmpMinuti < 0)
                                                    {
                                                        // inizializzazione della riga rapportino
                                                        Business.XmlExportsData.Orlando.Movimento newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        string motivazione = "01";
                                                        if (regv.Motivazione != "" && regv.Motivazione != null)
                                                        {
                                                            motivazione = regv.Motivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        int ore = regv.DurataOre - tmpOre;
                                                        int minuti = regv.DurataMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                        {
                                                            ore = oreLimite;
                                                            minuti = minutiLimite;
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }

                                                        // inizializzazione della riga rapportino
                                                        newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                        cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;

                                                        newRow.CodGiustificativoUfficiale = tmpMotivazione;
                                                        newRow.Data = data[0];
                                                        ore = tmpOre;
                                                        minuti = tmpMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore - 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                        {
                                                            ore = oreLimite;
                                                            minuti = minutiLimite;
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                    }
                                                    //invece se quella corrente ha durata negativa vado a rimuovere la durata da quella precedente e inserisco la precedente nel file
                                                    else if (regv.DurataOre < 0 || regv.DurataMinuti < 0)
                                                    {
                                                        // inizializzazione della riga rapportino
                                                        Business.XmlExportsData.Orlando.Movimento newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        string motivazione = "01";
                                                        if (tmpMotivazione != "" && tmpMotivazione != null)
                                                        {
                                                            motivazione = tmpMotivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        int ore = 0;
                                                        int minuti = 0;
                                                        if (regv.DurataOre == 0 && regv.DurataMinuti < 0)
                                                        {
                                                            if (tmpMinuti == 0)
                                                            {
                                                                tmpOre = tmpOre - 1;
                                                                tmpMinuti = 30;
                                                            }
                                                            else
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpMinuti += regv.DurataMinuti;
                                                            }

                                                        }
                                                        else
                                                        {
                                                            if (regv.DurataMinuti == 0)
                                                            {
                                                                tmpOre += regv.DurataOre;
                                                                tmpMinuti += regv.DurataMinuti;
                                                            }
                                                            else if (regv.DurataMinuti == -15)
                                                            {
                                                                if (tmpMinuti == 0)
                                                                {
                                                                    tmpMinuti = 45;
                                                                    tmpOre += regv.DurataOre;
                                                                    tmpOre -= 1;
                                                                }
                                                                else
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                    tmpMinuti += regv.DurataMinuti;
                                                                }
                                                            }
                                                            else if (regv.DurataMinuti == -30)
                                                            {
                                                                tmpMinuti += regv.DurataMinuti;
                                                                if (tmpMinuti >= 0)
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                }
                                                                else if (tmpMinuti < 0)
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                    tmpOre = tmpOre - 1;
                                                                }
                                                            }
                                                            else if (regv.DurataMinuti == -45)
                                                            {
                                                                tmpMinuti += 60;
                                                                tmpMinuti += regv.DurataMinuti;
                                                                if (tmpMinuti >= 60)
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                }
                                                                else if (tmpMinuti < 60)
                                                                {
                                                                    tmpOre += regv.DurataOre;
                                                                    tmpOre = tmpOre - 1;
                                                                }
                                                            }

                                                        }
                                                        ore = tmpOre;
                                                        minuti = tmpMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                        {
                                                            ore = oreLimite;
                                                            minuti = minutiLimite;
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }

                                                        // inizializzazione della riga rapportino
                                                        newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                        cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;

                                                        newRow.CodGiustificativoUfficiale = regv.Motivazione;
                                                        newRow.Data = data[0];
                                                        ore = regv.DurataOre;
                                                        minuti = regv.DurataMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore - 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                        {
                                                            ore = oreLimite;
                                                            minuti = minutiLimite;
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "000115";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                    }
                                                    //se i dati invece sono quelli di default vado ad aggiungere la registrazione corrente del file
                                                    else if (tmpOre == 0 && tmpMinuti == 0 && tmpMotivazione == "01")
                                                    {
                                                        // inizializzazione della riga rapportino
                                                        Business.XmlExportsData.Orlando.Movimento newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        string motivazione = "01";
                                                        if (regv.Motivazione != "" && regv.Motivazione != null)
                                                        {
                                                            motivazione = regv.Motivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        int ore = regv.DurataOre;
                                                        int minuti = regv.DurataMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                        {
                                                            ore = oreLimite;
                                                            minuti = minutiLimite;
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                    }
                                                    //se non si è entrati in nessuna dell condizioni precedenti vado sempicemente ad aggiungere sia la timbratura precedente che la corrente
                                                    //nel file
                                                    else
                                                    {
                                                        // inizializzazione della riga rapportino
                                                        Business.XmlExportsData.Orlando.Movimento newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                        Cant cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        string motivazione = "01";
                                                        if (tmpMotivazione != "" && tmpMotivazione != null)
                                                        {
                                                            motivazione = tmpMotivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        int ore = tmpOre;
                                                        int minuti = tmpMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                        {
                                                            ore = oreLimite;
                                                            minuti = minutiLimite;
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        string codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                        // inizializzazione della riga rapportino
                                                        newRow = new Business.XmlExportsData.Orlando.Movimento();
                                                        cantiere = RepoManager.CantRepo.First(c => c.Cant_Id == regv.CantId);
                                                        matricolaCol = collaboratore.First().Matricola_Col;
                                                        motivazione = "01";
                                                        if (regv.Motivazione != "" && regv.Motivazione != null)
                                                        {
                                                            motivazione = regv.Motivazione;
                                                        }
                                                        newRow.CodGiustificativoUfficiale = motivazione;
                                                        data = regv.DataReg.ToString("yyyy-MM-dd").Split(' ');
                                                        newRow.Data = data[0];
                                                        ore = regv.DurataOre;
                                                        minuti = regv.DurataMinuti;
                                                        if (minuti == 60)
                                                        {
                                                            minuti = 0;
                                                            ore = ore + 1;
                                                        }
                                                        if (ore < 0)
                                                        {
                                                            ore = System.Math.Abs(ore);
                                                        }
                                                        if (minuti < 0)
                                                        {
                                                            minuti = System.Math.Abs(minuti);
                                                        }
                                                        if (ore > oreLimite || (ore == oreLimite && minuti > minutiLimite))
                                                        {
                                                            ore = oreLimite;
                                                            minuti = minutiLimite;
                                                        }
                                                        newRow.NumOre = CommonService.AggiungiZeriASinistra(ore.ToString(), 2);
                                                        newRow.NumMinuti = CommonService.AggiungiZeriASinistra(minuti.ToString(), 2);
                                                        codiceAzienda = "000000";
                                                        if (collaboratore.First().Note_Col != "" && collaboratore.First().Note_Col != null)
                                                        {
                                                            codiceAzienda = collaboratore.First().Note_Col;
                                                        }
                                                        // aggiunta della riga alla testata
                                                        if (ore > 0 || minuti > 0)
                                                        {
                                                            document.Dipendente.CodAziendaUfficiale = "001602";//CommonService.AggiungiZeriASinistra(codiceAzienda, 6);
                                                            document.Dipendente.CodDipendenteUfficiale = CommonService.AggiungiZeriASinistra(collaboratore.First().Matricola_Col, 7);
                                                            document.Dipendente.Masters.Master.Add(newRow);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        dayCount++;
                                    }
                                }
                            }
                            int i = 1;
                            DateTime inizio = new DateTime(ultimo.Year, ultimo.Month, 01);
                            if (collaboratore.First().Data_Disponibilita_Inizio_Col > inizio)
                            {
                                i = collaboratore.First().Data_Disponibilita_Inizio_Col.Value.Day;
                            }
                            matricolaCol = collaboratore.First().CognomeNome_Col;
                            if (matricolaCol != "")
                            {
                                // calcolo il nome del file preparato per Perfetto
                                string currentFileName = String.Format("{0}{1}", matricolaCol, XmlToPerfettoConstants.ReturnXmlExtension);
                                matricolaCol = "";

                                // serializzazione e salvataggio del rapportino generato
                                var xsn = new XmlSerializerNamespaces();
                                xsn.Add("", "");
                                var serializer = new XmlSerializer(typeof(XmlExportsData.Orlando.Fornitura));
                                if (!Directory.Exists(folderpath))
                                    Directory.CreateDirectory(folderpath);

                                using (TextWriter textWriter = new StreamWriter(Path.Combine(folderpath, currentFileName)))
                                using (var writer = new ScsWriter(textWriter))
                                {
                                    writer.Formatting = Formatting.Indented;

                                    serializer.Serialize(writer, document, xsn);
                                    writer.Close();
                                    textWriter.Close();
                                }

                                reportsFileName.Add(Path.Combine(folderpath, currentFileName));
                            }
                        }

                        // generazione dell'oggetto envelope da scrivere
                        Business.XmlExportsData.Orlando.Envelope envelope = PrepareXmlEnvelopeToOrlando(reportsFileName);

                        // se è stato correttamente creato un envelope
                        if (envelope != default(Business.XmlExportsData.Orlando.Envelope))
                        {
                            using (var envelopeWriter = new StreamWriter(Path.Combine(folderpath, XmlToPerfettoConstants.EnvelopeFileName)))
                            using (var writer = new OrlandoWriter(envelopeWriter))
                            {
                                try
                                {
                                    writer.Formatting = Formatting.Indented;

                                    // Serialize the object, and close the TextWriter
                                    var serializer = new XmlSerializer(typeof(Business.XmlExportsData.Orlando.Envelope));
                                    serializer.Serialize(writer, envelope);
                                    writer.Close();
                                    envelopeWriter.Close();
                                }
                                catch (Exception)
                                {

                                }
                                finally
                                {
                                    envelopeWriter.Close();
                                    envelopeWriter.Dispose();
                                    writer.Close();
                                }
                            }
                        }

                        // compressione dei due files generati per il ritorno del dato
                        try
                        {
                            returnFileName = Path.Combine(folderpath, String.Format("{0}{1}", Path.GetFileNameWithoutExtension(envelope.ExportID), XmlToPerfettoConstants.ReturnZipExtension));
                            var zipContent = new List<string>() { Path.Combine(folderpath, XmlToPerfettoConstants.EnvelopeFileName) };
                            zipContent.AddRange(reportsFileName);
                            CommonService.ZipFilesList(zipContent, returnFileName);
                        }
                        catch (Exception)
                        {
                            returnFileName = String.Empty;
                        }

                        // sono salvate per la prossima esecuzione l'elenco dei collaboratori/date elaborate
                        SaveProcessedDateAndCols(regVsToProcess, exportedDatesAndColIds.ToList(), filesOutputFolder);
                    }
                }
            }

            return returnFileName;
        }

        /// <summary>
        /// Determina se il viaggio passato come parametro è fatto all'interno della stesso comune
        /// </summary>
        /// <param name="trip">Il viaggio da verificare.</param>
        /// <param name="dayRegVs">Le registrazioni del giorno per lo stesso collaboratore.</param>
        /// <returns><c>true</c> se il viaggio è all'interno dello stesso comune; altrimenti <c>false</c></returns>
        public bool IsTripOnSameMunicipality(Reg_V trip, IEnumerable<Reg_V> dayRegVs)
        {
            // inizializzazione del valore di ritorno del metodo
            bool isOnSameMunicipality = false;

            // ordinamento delle registrazioni giornaliere per ora d'entrata
            IOrderedEnumerable<Reg_V> orderedDayRegVs = dayRegVs.OrderBy(regv => regv.Data_Ora_Fis_E);

            // inizializzazione delle variabili che indicano se il viaggio passato come parametro è
            // il primo record o l'ultimo dell'elenco del giorno
            bool isFirstDayReg = false;
            bool isLastDayReg = false;

            // inizializzazione degli id del cantiere/collaboratore di partenza e arrivo
            int startEntityId = 0;
            int endEntityId = 0;

            // si recuperano il cantiere di partenza e arrivo in base alla posizione del viaggio all'interno della giornata
            // se il viaggio è all'inzio della giornata allora il comune di partenza deve essere preso dal collaboratore
            // e il comune di arrivo dal cantiere del viaggio stesso
            if (orderedDayRegVs.FirstOrDefault().RegE == trip.RegE)
            {
                startEntityId = trip.Col_Id.Value;
                endEntityId = trip.Cant_Id.Value;

                // segnalo che il viaggio in elaborazione è il rpimo della giornata
                isFirstDayReg = true;
            }
            else if (orderedDayRegVs.Last().RegE == trip.RegE)
            {
                // in caso invece il viaggio sia alla fine della giornata il comune di partenza è il cantiere del viaggio e la destinazione è il comune del collaboratore
                startEntityId = trip.Cant_Id.Value;
                endEntityId = trip.Col_Id.Value;

                // segnalo che il viaggio in elaborazione è il rpimo della giornata
                isLastDayReg = true;
            }
            else
            {
                // se invece il viaggio è nel mezzo della giornata il comune di partenza è nella registrazione precedente mentre il comune di arrivo è il cantiere del viaggio stesso
                List<Reg_V> dayRegVsList = orderedDayRegVs.ToList();
                startEntityId = dayRegVsList.ElementAt(dayRegVsList.FindIndex(regv => regv.RegE == trip.RegE) - 1).Cant_Id.Value;
                endEntityId = trip.Cant_Id.Value;
            }

            // recupero i comuni delle due entità
            string startMunicipality = String.Empty;
            string endMunicipality = String.Empty;

            // se non sono ne all'inizio nella alla fine del giorno recupero entrambi i valori dai cantieri;
            // se sono all'inizio della giornata recupero la partenza dai collaboratori e l'arrivo dai cantieri;
            // se sono alla fine della giornata recupero la partenza dai cantieri e l'arrivo dai collaboratori
            if (!isFirstDayReg && !isLastDayReg)
            {
                startMunicipality = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == startEntityId).Luogo_Can;
                endMunicipality = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == endEntityId).Luogo_Can;
            }
            else if (isFirstDayReg)
            {
                startMunicipality = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == startEntityId).Domicilio_Luogo_Col;
                endMunicipality = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == endEntityId).Luogo_Can;
            }
            else
            {
                startMunicipality = RepoManager.CantRepo.FirstOrDefault(cant => cant.Cant_Id == startEntityId).Luogo_Can;
                endMunicipality = RepoManager.ColRepo.FirstOrDefault(col => col.Col_Id == endEntityId).Domicilio_Luogo_Col;
            }

            // se i comuni sono uguali allora il viaggio è nello stesso comune
            if (startMunicipality.ToUpper() == endMunicipality.ToUpper())
                isOnSameMunicipality = true;


            return isOnSameMunicipality;
        }

        /// <summary>
        /// Determina se il viaggio specificato è un viaggio di inizio o fine giornata.
        /// </summary>
        /// <param name="trip">Il viaggio da verificare.</param>
        /// <param name="dayRegVs">Le registrazioni del giorno per lo stesso collaboratore.</param>
        /// <returns><c>true</c> se il viaggio è un viaggio di inizio o fine giornata; altrimenti <c>false</c></returns>
        public bool IsTripOnStartEnd(Reg_V trip, IEnumerable<Reg_V> dayRegVs)
        {
            // inizializzazione del valore di ritorno del metodo:
            // per default il viaggio non è di inizio o fine giornata
            bool isStartEndTrip = false;

            // ordinamento delle registrazioni giornaliere per ora d'entrata
            IOrderedEnumerable<Reg_V> orderedDayRegVs = dayRegVs.OrderBy(regv => regv.Data_Ora_Fis_E);

            // il viaggio risulta essere di inizio o fine giornata se è la prima o l'ultima registrazione del giorno
            if (orderedDayRegVs.FirstOrDefault().RegE == trip.RegE || orderedDayRegVs.LastOrDefault().RegE == trip.RegE)
                isStartEndTrip = true;

            // ritorno del valore calcolato dal metodo
            return isStartEndTrip;
        }

        /// <summary>
        /// Prepara il file xml envelope per perfetto a partire dal file passasto come parametro.
        /// </summary>
        /// <param name="file">L'elenco di files a cui affiancare l'envelope.</param>
        /// <returns>
        /// L'envelope generato pronto per la scrittura su file
        /// </returns>
        public Business.XmlExportsData.Perfetto.Envelope PrepareXmlEnvelopeToPerfetto(IList<string> files)
        {
            // inizializzazione del valore di ritorno del metodo
            var envelope = new Business.XmlExportsData.Perfetto.Envelope();


            string user = "sa";
            int documentnumber = XmlToPerfettoConstants.Documentnumber;

            //data ora attuale della creazione del file Xml
            string currentRegVDate = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            //creazione dei campi dell'envelope
            envelope.ExportID = String.Format("{0}-{1}", user, currentRegVDate);
            envelope.Description = "I files dei dati del documento Rapportino di lavoro per Commessa sono stati generati con Profilo di esportazione .";
            envelope.DocumentInfo.Domain = "DEFAULT_DOMAIN";
            envelope.DocumentInfo.Site = "DEFAULT_SITE";
            envelope.DocumentInfo.SiteCode = "DESI";
            envelope.DocumentInfo.EnvelopeClass = "WorkingReports";
            envelope.DocumentInfo.RootDocNamespace = "Document.Perfetto.WorkingReports.Documents.JobWorkingReports";
            envelope.DocumentInfo.Datatime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    var newFile = new Business.XmlExportsData.Perfetto.XmlFileTag();
                    newFile.type = "Root";
                    newFile.dataurl = Path.GetFileName(file);
                    newFile.envelopeclass = "WorkingReports";
                    newFile.documentname = "Rapportino di lavoro per Commessa";
                    newFile.documentnumber = documentnumber.ToString();
                    //documentnumber++;

                    envelope.Contents.File.Add(newFile);

                }
            }


            XmlSerializerNamespaces xsn = new XmlSerializerNamespaces();
            xsn.Add("", "http://www.microarea.it/XTech/1.0.0/XMLSchema");

            return envelope;
        }

        /// <summary>
        /// Prepara il file xml envelope per perfetto a partire dal file passasto come parametro.
        /// </summary>
        /// <param name="file">L'elenco di files a cui affiancare l'envelope.</param>
        /// <returns>
        /// L'envelope generato pronto per la scrittura su file
        /// </returns>
        public Business.XmlExportsData.Scs.Envelope PrepareXmlEnvelopeToScs(IList<string> files)
        {
            // inizializzazione del valore di ritorno del metodo
            var envelope = new Business.XmlExportsData.Scs.Envelope();


            string user = "sa";
            int documentnumber = XmlToPerfettoConstants.Documentnumber;

            //data ora attuale della creazione del file Xml
            string currentRegVDate = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            //creazione dei campi dell'envelope
            envelope.ExportID = String.Format("{0}-{1}", user, currentRegVDate);
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    var newFile = new Business.XmlExportsData.Scs.XmlFileTag();
                    newFile.type = "Root";
                    newFile.dataurl = Path.GetFileName(file);
                    newFile.envelopeclass = "WorkingReports";
                    newFile.documentname = "Rapportino di lavoro per Commessa";
                    newFile.documentnumber = documentnumber.ToString();
                    //documentnumber++;

                    envelope.Contents.File.Add(newFile);

                }
            }


            //XmlSerializerNamespaces xsn = new XmlSerializerNamespaces();
            //xsn.Add("", "http://www.microarea.it/XTech/1.0.0/XMLSchema");

            return envelope;
        }

        public Business.XmlExportsData.Orlando.Envelope PrepareXmlEnvelopeToOrlando(IList<string> files)
        {
            // inizializzazione del valore di ritorno del metodo
            var envelope = new Business.XmlExportsData.Orlando.Envelope();


            string user = "sa";
            int documentnumber = XmlToPerfettoConstants.Documentnumber;

            //data ora attuale della creazione del file Xml
            string currentRegVDate = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            //creazione dei campi dell'envelope
            envelope.ExportID = String.Format("{0}-{1}", user, currentRegVDate);
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    var newFile = new Business.XmlExportsData.Orlando.XmlFileTag();
                    newFile.type = "Root";
                    newFile.dataurl = Path.GetFileName(file);
                    newFile.envelopeclass = "WorkingReports";
                    newFile.documentname = "Rapportino di lavoro per Commessa";
                    newFile.documentnumber = documentnumber.ToString();
                    //documentnumber++;

                    envelope.Contents.File.Add(newFile);

                }
            }


            //XmlSerializerNamespaces xsn = new XmlSerializerNamespaces();
            //xsn.Add("", "http://www.microarea.it/XTech/1.0.0/XMLSchema");

            return envelope;
        }

        /// <summary>
        /// Salva nella destinazione indicata, in formato xml, i collaboratori e le date contenute.
        /// </summary>
        /// <param name="regVsToProcess">Le regv da processare per estrarre i dati di collaboratore e data.</param>
        /// <param name="processedDatesCol">L'elenco degli elementi già processati.</param>
        /// <param name="filesOutputFolder">La cartella in cui scrivere il file xml con i dati esportati</param>
        private void SaveProcessedDateAndCols(IEnumerable<Reg_V> regVsToProcess, List<ExportedData> processedDatesCol, string filesOutputFolder)
        {
            // calcolo di tutte le coppie di collaboratore data processate
            IEnumerable<ExportedData> datesColToProcess = regVsToProcess.Select(regv => new ExportedData()
            {
                ColId = regv.Col_Id ?? 0,
                DataReg = regv.Data_Reg ?? DateTime.MinValue

            }).Distinct();

            // se l'elecno di tutte le coppie di collaboratore e data processate contiene valori allora si procede ad epurare quanto già indicato come processato
            if (datesColToProcess.Any())
            {
                IEnumerable<ExportedData> datesColToAdd = datesColToProcess.Where(dtcol => !processedDatesCol.Any(exp => exp.DataReg == dtcol.DataReg && exp.ColId == dtcol.ColId));

                // se si deve inserire qualcosa quindi lo si aggiunge agli elementi già processati
                if (datesColToAdd.Any())
                    processedDatesCol.AddRange(datesColToAdd);
            }

            // calcolo dell'oggetto lista che sarà rappresentato nel file xml riportando solamente le date dell'ultimo anno
            DateTime lastYear = DateTime.Today.AddYears(-1);
            var xmlDataList = new ExportedDataList() { ExportedDatas = processedDatesCol.Where(exp => exp.DataReg > lastYear).ToList() };

            // si procede alla creazione della cartella di destinazione se non già presente
            if (!Directory.Exists(filesOutputFolder))
                Directory.CreateDirectory(filesOutputFolder);

            // calcolo del percorso di scrittura del file xml
            string xmlOutputFilePath = Path.Combine(filesOutputFolder, Common.Properties.Settings.Default.ExportedToPerfettoDatesColFile);

            // scrittura su xml della lista
            var writer = new XmlSerializer(typeof(ExportedDataList));
            using (FileStream outputXmlFile = File.Create(xmlOutputFilePath))
            {
                try
                {
                    writer.Serialize(outputXmlFile, xmlDataList);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    outputXmlFile.Close();
                    outputXmlFile.Dispose();
                }

            }
        }

        /// <summary>
        /// Metodo che legge e restituisce l'elenco delle date/collaboratori già processati nell'esportazione verso Perfetto.
        /// </summary>
        /// <param name="filesOutputFolder">La cartella da dove recuperare il file in cui gli elementi già processati sono salvati.</param>
        /// <returns>L'elenco della coppia di data/collaboratore già processati</returns>
        private IEnumerable<ExportedData> ReadProcessedDatesAndCols(string filesOutputFolder)
        {
            // inizializzazione del valore di ritorno del metodo
            IEnumerable<ExportedData> returnList = Enumerable.Empty<ExportedData>();

            // si procede alla lettura del dato solamente se la cartella è presente
            if (Directory.Exists(filesOutputFolder))
            {
                // calcolo del percorso file con i dati
                string xmlFilePath = Path.Combine(filesOutputFolder, Common.Properties.Settings.Default.ExportedToPerfettoDatesColFile);

                // si procede con l'elaborazione solamente se il file da cui leggere i dati è presente
                if (File.Exists(xmlFilePath))
                {

                    // lettura del contenuto del file 
                    var reader = new XmlSerializer(typeof(ExportedDataList));

                    using (var xmlFile = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        try
                        {
                            var expDataList = (ExportedDataList)reader.Deserialize(xmlFile);
                            returnList = expDataList.ExportedDatas;
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                        finally
                        {
                            xmlFile.Close();
                            xmlFile.Dispose();
                        }
                    }

                }
            }

            // ritorno del valore calcolato dal metodo
            return returnList;
        }

        #endregion

        #region Private class data

        /// <summary>
        /// Entità utilizzata per rappresentare un viaggio
        /// </summary>
        private class Trip
        {
            public Reg RegE { get; set; }
            public Reg RegU { get; set; }

            public int? cantIdStart { get; set; }

            /// <summary>
            /// Recupera o imposta un valore che indica se il viaggio che si sta generando ha come partenza un cantiere di ore non lavorate.
            /// </summary>
            /// <value>
            /// <c>true</c> se il viaggio che si sta generando ha come partenza un cantiere di ore non lavorate; altrimenti, <c>false</c>.
            /// </value>
            public bool IsFromOnl { get; set; }

            /// <summary>
            /// Recupera o imposta un valore che indica se il viaggio che si sta generando ha come arrivo un cantiere di ore non lavorate.
            /// </summary>
            /// <value>
            /// <c>true</c> se il viaggio che si sta generando ha come arrivo un cantiere di ore non lavorate; altrimenti, <c>false</c>.
            /// </value>
            public bool IsToOnl { get; set; }

            public TimeSpan TripDuration
            {
                get
                {
                    return RegU.Registrazione_Data_Ora_Fis_Reg.Subtract(RegE.Registrazione_Data_Ora_Fis_Reg);
                }
            }

            public List<Reg> Regs
            {
                get
                {
                    return new List<Reg> { RegE, RegU };
                }
            }
        }

        /// <summary>
        /// Classe che rappresenta i dati di calcolo del limite d'entrata
        /// </summary>
        public class EntryLimitData
        {

            #region Fields

            /// <summary>
            /// L'ora che indica il limite d'entrata
            /// </summary>
            private TimeSpan? _entryLimitTime = null;

            /// <summary>
            /// L'ora che indica il limite d'entrata
            /// </summary>
            private TimeSpan? _exitLimitTime = null;

            /// <summary>
            /// Lista  di ore che indica il limite d'entrata
            /// </summary>
            private List<TimeSpan> _entryLimitTimeList = null;

            /// <summary>
            /// Lista  di ore che indica il limite d'entrata
            /// </summary>
            private List<TimeSpan> _exitLimitTimeList = null;

            /// <summary>
            /// Il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata
            /// </summary>
            private TimeSpan? _entryLimitTollerance = null;

            #endregion

            #region Properties

            /// <summary>
            /// Recupera o imposta l'ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// L'ora che indica il limite d'entrata.
            /// </value>
            public TimeSpan? EntryLimitTime
            {
                get
                {
                    return _entryLimitTime;
                }
                set
                {
                    _entryLimitTime = value;
                }
            }

            /// <summary>
            /// Recupera o imposta l'ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// L'ora che indica il limite d'entrata.
            /// </value>
            public TimeSpan? ExitLimitTime
            {
                get
                {
                    return _exitLimitTime;
                }
                set
                {
                    _exitLimitTime = value;
                }
            }

            /// <summary>
            /// Recupera o imposta la lista delle ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// Lista di ore che indica il limite d'entrata.
            /// </value>
            public List<TimeSpan> EntryLimitTimeList
            {
                get
                {
                    return _entryLimitTimeList;
                }
                set
                {
                    _entryLimitTimeList = value;
                }
            }

            /// <summary>
            /// Recupera o imposta la lista delle ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// Lista di ore che indica il limite d'entrata.
            /// </value>
            public List<TimeSpan> ExitLimitTimeList
            {
                get
                {
                    return _exitLimitTimeList;
                }
                set
                {
                    _exitLimitTimeList = value;
                }
            }

            /// <summary>
            /// Recupera o imposta il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata.
            /// </summary>
            /// <value>
            /// Il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata.
            /// </value>
            public TimeSpan? EntryLimitTollerance
            {
                get
                {
                    return _entryLimitTollerance;
                }
                set
                {
                    _entryLimitTollerance = value;
                }
            }

            /// <summary>
            /// Recupera il valore che indica se l'istanza corrente è un limite d'entrata configurato.
            /// </summary>
            /// <value>
            /// <c>true</c> se l'istanza corrente è un limite d'entrata configurato; altrimenti, <c>false</c>.
            /// </value>
            public bool IsConfigured
            {
                get
                {
                    // l'istanza corrente risulta configurata se il limite d'entrata impostato ha un valore
                    return EntryLimitTime.HasValue;
                }
            }

            #endregion

        }

        public class ExitLimitData
        {
            #region Fields

            /// <summary>
            /// L'ora che indica il limite d'entrata
            /// </summary>
            private TimeSpan? _exitLimitTime = null;

            /// <summary>
            /// Lista  di ore che indica il limite d'entrata
            /// </summary>
            private List<TimeSpan> _exitLimitTimeList = null;

            /// <summary>
            /// Il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata
            /// </summary>
            private TimeSpan? _exitLimitTollerance = null;

            #endregion

            #region Properties

            /// <summary>
            /// Recupera o imposta l'ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// L'ora che indica il limite d'entrata.
            /// </value>
            public TimeSpan? ExitLimitTime
            {
                get
                {
                    return _exitLimitTime;
                }
                set
                {
                    _exitLimitTime = value;
                }
            }

            /// <summary>
            /// Recupera o imposta la lista delle ora che indica il limite d'entrata.
            /// </summary>
            /// <value>
            /// Lista di ore che indica il limite d'entrata.
            /// </value>
            public List<TimeSpan> ExitLimitTimeList
            {
                get
                {
                    return _exitLimitTimeList;
                }
                set
                {
                    _exitLimitTimeList = value;
                }
            }

            /// <summary>
            /// Recupera o imposta il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata.
            /// </summary>
            /// <value>
            /// Il tempo di tolleranza utilizzato per l'applicazione del limite d'entrata.
            /// </value>
            public TimeSpan? ExitLimitTollerance
            {
                get
                {
                    return _exitLimitTollerance;
                }
                set
                {
                    _exitLimitTollerance = value;
                }
            }

            /// <summary>
            /// Recupera il valore che indica se l'istanza corrente è un limite d'entrata configurato.
            /// </summary>
            /// <value>
            /// <c>true</c> se l'istanza corrente è un limite d'entrata configurato; altrimenti, <c>false</c>.
            /// </value>
            public bool IsConfigured
            {
                get
                {
                    // l'istanza corrente risulta configurata se il limite d'entrata impostato ha un valore
                    return ExitLimitTime.HasValue;
                }
            }

            #endregion
        }

        /// <summary>
        /// Rappresenta il tipo di limite d'entrata utilizzabile dall'applicativo
        /// </summary>
        public enum EntryLimitTypeEnum
        {

            /// <summary>
            /// Limite d'entrata mattutino
            /// </summary>
            Morning,

            /// <summary>
            /// Limite d'entrata pomeridiano
            /// </summary>
            Afternoon,

            /// <summary>
            /// Lista dei limiti mattutini per determinare gli orari
            /// </summary>
            MorningDealyLimitList,

            /// <summary>
            /// Lista dei limiti pomeridiani per determinare gli orari
            /// </summary>
            AfternoonDealyLimitList,

            /// <summary>
            /// Limite d'entrata mattutino d'uscita 
            /// </summary>
            MorningExit,

            /// <summary>
            /// Limite d'entrata pomeridiano d'uscita 
            /// </summary>
            AfternoonExit,

            /// <summary>
            /// Lista dei limiti mattutini d'uscita per determinare gli orari
            /// </summary>
            MorningDealyLimitListExit,

            /// <summary>
            /// Lista dei limiti pomeridiani d'uscita per determinare gli orari
            /// </summary>
            AfternoonDealyLimitListExit

        }

        public enum ExitLimitTypeEnum
        {
            /// <summary>
            /// Limite d'entrata mattutino
            /// </summary>
            Morning,

            /// <summary>
            /// Limite d'entrata pomeridiano
            /// </summary>
            Afternoon,

            /// <summary>
            /// Lista dei limiti mattutini per determinare gli orari
            /// </summary>
            MorningDealyLimitList,

            /// <summary>
            /// Lista dei limiti pomeridiani per determinare gli orari
            /// </summary>
            AfternoonDealyLimitList
        }

        #endregion


    }


}