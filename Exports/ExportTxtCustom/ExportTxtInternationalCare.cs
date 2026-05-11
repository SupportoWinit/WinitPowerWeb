using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Business.Repository;
using Newtonsoft.Json.Linq;
using log4net;
using Business.BusinessExtension;
using System.Reflection;
using Westwind.Utilities.Extensions;

namespace Exports.ExportTxtCustom
{

    /**
    *  Export specifico verso software paghe Seldati del cliente SCS in formato .txt
    *  Export per periodo mensile
    *  Per ogni collaboratore, genera tanti record quante sono le causali presenti per il dato giorno.
    *  Ogni record avrà i seguente tracciato:
    *   - Codice tracciato  -> FISSO con valore 'P' (Presenze)
    *   - Anno              -> anno di riferimento
    *   - Mese              -> mese di riferimento
    *   - Filler1           -> campo riempitivo di 1 carattere ' ' (' ')
    *   - Codice azienda    -> codice identificativo dell'azienda nel programma Seldati (4311100 per SCS)
    *   - Codice dipendente -> codice identificativo del dipendente nel programma Seldati
    *   - Filler2           -> campo riempitivo di 2 caratteri '9' ('99')
    *   - Filler1           -> campo riempitivo di 1 carattere ' ' (' ')
    *   - Codice causale    -> codice identificativo della causale (vedere tabella causali)
    *   - Giorno            -> giorno di riferimento
    *   - Numero Ore        -> ore relative alla motivazione in centesimi in formato 000000000 (es: 8 ore e 30 minuti -> 000000850) 
    *   - Filler3           -> campo riempitivo di 22 caratteri ' ' ('                      ')
    *   - Tipo evento       -> Mal/Mat/Inf: “C”=Continuazione “R”=Ricaduta Cigo/Cigs: “P”=Posticipata 
    *   - Filler4           -> campo riempitivo di 2 caratteri ' ' ('  ')
    *   - Codice Cantiere   -> Codice cantiere in formato 000 (opzionale)
    *   - Filler5           -> campo riempitivo di 1 carattere 'E' ('E')
    **/

    /*
     * Si ricorda che l'export prevede i minuti in centesimi invece che in sessantesimi
     */

    class ExportTxtInternationalCare : TxtToolBox
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ExportTxtInternationalCare));

        #region Private Properties

        private const string _ordinaryHoursLabel = "ORD";
        private const string _extraOrdinaryHoursLabel = "STR";
        private const string _rpaHoursLabel = "RPA";
        private const string _trasfHoursLabel = "TRA";

        private const string _codiceTracciato = "P";
        private const string _codiceAzienda = "4311100";

        private string _anno;
        private string _mese;

        private const string _tipoEvento = " ";

        private const string _filler1 = " ";
        private const string _filler2 = "99 ";
        private static readonly string _filler3 = new string(' ', 22);
        private static readonly string _filler4 = new string(' ', 2);
        private const string _filler5 = "E";
        private static readonly string _filler6 = new string(' ', 21);


        #endregion

        #region Constructor



        #endregion

        #region Public Methods

        public override void LaunchExport()
        {

            _anno = ExportDate.ToString("yy");
            _mese = ExportDate.Month.ToString("00");

            DateTime maxBound = ExportDate.AddMonths(1);

            string formattedDate = ExportDate.ToString("yyyy/MM");

            var allowedColls = RepoManager.ColRepo.DbSet.Where(c => SelectedIds.Contains(c.Col_Id) && (c.Matricola_Col != null && c.Matricola_Col != "")).Select(c => new { c.Col_Id, c.Codice_Collaboratore, c.Matricola_Col, c.Tab_Orari_Tipo_Id, c.Data_Disponibilita_Inizio_Col, c.Data_Disponibilita_Fine_Col }).ToList();

            List<string> notAllowedColls = RepoManager.ColRepo.DbSet.Where(c => SelectedIds.Contains(c.Col_Id) && (c.Matricola_Col == null || c.Matricola_Col == "")).Select(c => c.Codice_Collaboratore).ToList();

            notAllowedColls.ForEach(codiceCol =>
            {
                string error = String.Format("Il collaboratore con codice {0} non è stato esportato a causa dell'assenza del numero di matricola", codiceCol);
                _log.InfoFormat(error);
                Errors.Add(new { Messaggio = error });
            });


            foreach (var col in allowedColls)
            {
                string[] codici = col.Matricola_Col.Split('/');
                string exportData = "";
                string codiceAzienda = codici[0];
                string codiceFiliale = "01";
                string codLibMatricola = codici[1];
                string codiceMatricola = codici[2];
                string oreOrdinarie = "";
                string giustificativo1 = "";
                string oreGiustificativo1 = "";
                string giustificativo2 = "";
                string oreGiustificativo2 = "";
                string giustificativo3 = "";
                string oreGiustificativo3 = "";
                string giustificativo4 = "";
                string oreGiustificativo4 = "";
                int totaleOre = 0;
                int totaleGiorniLavorati = 0;
                int totaleGiorniRetribuite = 0;
                int totaleOreRetribuite = 0;
                int totaleSettimante = 0;

                DateTime startOfMonth = new DateTime(ExportDate.Year, ExportDate.Month, 1);
                DateTime endOfMonth = ExportDate.EndOfMonth();

                exportData = exportData + codiceAzienda + ";" + codiceFiliale + ";" + codLibMatricola + ";" + codiceMatricola + ";";

                var justificationRegs = RepoManager.Reg_VRepo.GetAllQueryable().Where(r => r.Col_Id == col.Col_Id && r.Cant_Id != null && r.Data_Reg_AAAA_MM == formattedDate && (r.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || r.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration || r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual) && r.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).OrderBy(c => c.Data_Reg).GroupBy(r => r.Motivazione_Reg_Id);
                int j = 1;

                foreach (var regs in justificationRegs)
                {
                    ILookup<DateTime?, Reg_V> dayDictionarys = regs.Where(r => r.Col_Id == col.Col_Id && r.Cant_Id != null && r.Data_Reg_AAAA_MM == formattedDate && (r.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || r.Registrazione_Tipo_Reg == (int)RegTypeEnum.Duration || r.Registrazione_Tipo_Reg == (int)RegTypeEnum.RettTimeSheetManual) && r.Registrazione_Stato_Reg == (int)RegStateEnum.Ass).OrderBy(c => c.Data_Reg).ToLookup(c => c.Data_Reg);
                    if (regs.Key != null)
                    {
                        #region Calcolo ore con motivazione
                        Tab_Decod mot = RepoManager.Tab_DecodRepo.Single(td => td.Tab_Decod_Id == regs.Key.Value);
                        DateTime lastDate = startOfMonth.AddDays(-1);
                        foreach (var dayRegs in dayDictionarys)
                        {
                            var dif = dayRegs.Key.Value - lastDate;
                            int differenza1 = dif.Days;
                            if (dayRegs.Key > lastDate.AddDays(1))
                            {
                                if (differenza1 < 0)
                                {
                                    differenza1 = endOfMonth.Day + differenza1;
                                }
                                for (int i = 1; i < differenza1; i++)
                                {
                                    switch (j)
                                    {
                                        case 1:
                                            oreGiustificativo1 = oreGiustificativo1 + "0000;";
                                            giustificativo1 = giustificativo1 + "  ;";
                                            break;
                                        case 2:
                                            oreGiustificativo2 = oreGiustificativo2 + "0000;";
                                            giustificativo2 = giustificativo2 + "  ;";
                                            break;
                                        case 3:
                                            oreGiustificativo3 = oreGiustificativo3 + "0000;";
                                            giustificativo3 = giustificativo3 + "  ;";
                                            break;
                                        case 4:
                                            oreGiustificativo4 = oreGiustificativo4 + "0000;";
                                            giustificativo4 = giustificativo4 + "  ;";
                                            break;
                                    }
                                }
                            }
                            int totaleGiornaliero = 0;
                            foreach (Reg_V reg in dayRegs)
                            {
                                if (reg.Durata_Fig != null)
                                {
                                    totaleGiornaliero = totaleGiornaliero + reg.Durata_Fig.Value;
                                }
                                else
                                {
                                    totaleGiornaliero = totaleGiornaliero + reg.Durata_Fis.Value;
                                }
                            }
                            string stringaFinale = "";
                            if (totaleGiornaliero / 60 < 10)
                            {
                                stringaFinale = stringaFinale + "0" + totaleGiornaliero / 60;
                            }
                            else
                            {
                                stringaFinale = stringaFinale + totaleGiornaliero / 60;
                            }
                            if (ToCent(totaleGiornaliero % 60) < 10)
                            {
                                stringaFinale = stringaFinale + "0" + ToCent(totaleGiornaliero % 60).ToString();
                            }
                            else
                            {
                                stringaFinale = stringaFinale + "" + ToCent(totaleGiornaliero % 60).ToString();
                            }
                            string motFinale = mot.Chiave_Tab;
                            if (mot.Chiave_Tab.Length == 1) {
                                motFinale = mot.Chiave_Tab + " ";
                            }
                            switch (j)
                            {
                                case 1:
                                    oreGiustificativo1 = oreGiustificativo1 + stringaFinale + ";";
                                    giustificativo1 = giustificativo1 + motFinale + ";";
                                    break;
                                case 2:
                                    oreGiustificativo2 = oreGiustificativo2 + stringaFinale + ";";
                                    giustificativo2 = giustificativo2 + motFinale + ";"; ;
                                    break;
                                case 3:
                                    oreGiustificativo3 = oreGiustificativo3 + stringaFinale + ";";
                                    giustificativo3 = giustificativo3 + motFinale + ";"; ;
                                    break;
                                case 4:
                                    oreGiustificativo4 = oreGiustificativo4 + stringaFinale + ";";
                                    giustificativo4 = giustificativo4 + motFinale + ";"; ;
                                    break;
                            }
                            lastDate = dayRegs.Key.Value;
                            totaleOre += totaleGiornaliero;
                            totaleOreRetribuite += totaleGiornaliero;
                        }
                        if (dayDictionarys.Last().Key.Value != endOfMonth)
                        {
                            int differenza1 = endOfMonth.Day - dayDictionarys.Last().Key.Value.Day;
                            for (int i = 1; i <= differenza1; i++)
                            {
                                switch (j)
                                {
                                    case 1:
                                        oreGiustificativo1 = oreGiustificativo1 + "0000;";
                                        giustificativo1 = giustificativo1 + "  ;";
                                        break;
                                    case 2:
                                        oreGiustificativo2 = oreGiustificativo2 + "0000;";
                                        giustificativo2 = giustificativo2 + "  ;";
                                        break;
                                    case 3:
                                        oreGiustificativo3 = oreGiustificativo3 + "0000;";
                                        giustificativo3 = giustificativo3 + "  ;";
                                        break;
                                    case 4:
                                        oreGiustificativo4 = oreGiustificativo4 + "0000;";
                                        giustificativo4 = giustificativo4 + "  ;";
                                        break;
                                }
                            }
                        }
                        j++;
                        #endregion
                    }
                    else
                    {
                        #region Ore ordinarie
                        DateTime lastDate = startOfMonth.AddDays(-1);
                        foreach (var dayRegs in dayDictionarys)
                        {
                            var dif = dayRegs.Key.Value - lastDate;
                            int differenza1 = dif.Days;
                            if (dayRegs.Key > lastDate.AddDays(1))
                            {
                                if (differenza1 < 0)
                                {
                                    differenza1 = endOfMonth.Day + differenza1;
                                }
                                for (int i = 1; i < differenza1; i++)
                                {
                                    oreOrdinarie = oreOrdinarie + "0000;";
                                }
                            }
                            int totaleGiornaliero = 0;
                            foreach (Reg_V reg in dayRegs)
                            {
                                if (reg.Durata_Fig != null)
                                {
                                    totaleGiornaliero = totaleGiornaliero + reg.Durata_Fig.Value;
                                }
                                else
                                {
                                    totaleGiornaliero = totaleGiornaliero + reg.Durata_Fis.Value;
                                }
                            }
                            string stringaFinale = "";
                            if (totaleGiornaliero / 60 < 10)
                            {
                                stringaFinale = stringaFinale + "0" + totaleGiornaliero / 60;
                            }
                            else
                            {
                                stringaFinale = stringaFinale + totaleGiornaliero / 60;
                            }
                            if (ToCent(totaleGiornaliero % 60) < 10)
                            {
                                stringaFinale = stringaFinale + "0" + ToCent(totaleGiornaliero % 60).ToString();
                            }
                            else
                            {
                                stringaFinale = stringaFinale + "" + ToCent(totaleGiornaliero % 60).ToString();
                            }
                            oreOrdinarie = oreOrdinarie + stringaFinale + ";";
                            lastDate = dayRegs.Key.Value;
                            totaleOre += totaleGiornaliero;
                            totaleOreRetribuite += totaleGiornaliero;
                            totaleGiorniLavorati = totaleGiorniLavorati + 1;
                            totaleGiorniRetribuite = totaleGiorniRetribuite + 1;
                        }
                        if (dayDictionarys.Last().Key.Value != endOfMonth)
                        {
                            int differenza1 = endOfMonth.Day - dayDictionarys.Last().Key.Value.Day;
                            for (int i = 1; i <= differenza1; i++)
                            {
                                oreOrdinarie = oreOrdinarie + "0000;";
                            }
                        }
                        #endregion
                    }
                }
                int differenza = endOfMonth.Day - startOfMonth.Day;

                if (oreOrdinarie != "") 
                {
                    exportData = exportData + oreOrdinarie;
                }

                #region Giustificativo 1-2

                if (giustificativo1 == "" && oreGiustificativo1 == "")
                {
                    for (int i = 0; i < endOfMonth.Day; i++)
                    {
                        giustificativo1 = giustificativo1 + "  ;";
                        oreGiustificativo1 = oreGiustificativo1 + "0000;";
                    }
                    exportData = exportData + giustificativo1 + oreGiustificativo1;
                }
                else 
                {
                    exportData = exportData + giustificativo1 + oreGiustificativo1;
                }
                if (giustificativo2 == "" && oreGiustificativo2 == "")
                {
                    for (int i = 0; i < endOfMonth.Day; i++)
                    {
                        giustificativo2 = giustificativo2 + "  ;";
                        oreGiustificativo2 = oreGiustificativo2 + "0000;";
                    }
                    exportData = exportData + giustificativo2 + oreGiustificativo2;
                }
                else 
                {
                    exportData = exportData + giustificativo2 + oreGiustificativo2;
                }
                #endregion
                #region Riservato 1-2
                exportData = exportData + "?;0000;0000;0000;0000;0000;0000;";
                #endregion
                #region Ore straordinarie
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                #endregion
                #region Riservato 3-4
                exportData = exportData + "?;1;";
                #endregion
                #region Totali
                string totali = (totaleOre / 1).ToString() + "" + (totaleOre % 1).ToString();
                string totaliRet = (totaleOreRetribuite / 1).ToString() + "" + (totaleOreRetribuite % 1).ToString();
                string totGiorni = totaleGiorniLavorati.ToString();
                if (totaleGiorniLavorati < 10) 
                {
                    totGiorni = "0" + totaleGiorniLavorati.ToString();
                }
                string totGiorniRet = totaleGiorniRetribuite.ToString();
                if (totaleGiorniRetribuite < 10) 
                {
                    totGiorniRet = "0" + totaleGiorniRetribuite.ToString();
                }
                string settimane = (totaleGiorniLavorati / 7).ToString();
                if (totaleGiorniLavorati / 7 < 10) 
                { 
                    settimane = "0" + (totaleGiorniLavorati / 7).ToString();
                }
                exportData = exportData + totali + ";" + totaliRet + ";" + totGiorni + ";" + totGiorniRet + ";" + totGiorniRet + ";" + totGiorniRet + ";" + settimane + ";";
                #endregion
                #region Riservato 5
                exportData = exportData + "?;";
                #endregion
                #region Ore straordinarie
                //Ore straordinario Festivo, per adesso a 0
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Ore straordinario Notturno Feriale
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Ore straordinario Notturno Festivo
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                #endregion
                #region Voci di calcolo
                //Voci di calcolo codice, metto a zero per la struttura
                for (int i = 0; i <= 13; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Voci di calcolo Ore/Giorno, metto a zero per la struttura
                for (int i = 0; i <= 13; i++)
                {
                    exportData = exportData + "00000;";
                }
                //Voci di calcolo Aliquota, metto a zero per la struttura
                for (int i = 0; i <= 13; i++)
                {
                    exportData = exportData + "000000;";
                }
                #endregion
                #region Riservato 6
                exportData = exportData + "?;";
                #endregion
                //Ore Lavoro Supplementare PT
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Magg Lavoro Notturno
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Riservato 7
                exportData = exportData + "?;";
                //Banca Ore Mat
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Banca Ore God
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Riservato 8
                exportData = exportData + "?;";
                //Ore Lavoro Suppls PT 2 Fascia
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Magg Lavoro Notturno 2 Fascia
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Magg Lavoro Festivo
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Magg Lavoro Notturno/Festivo
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Riservato 9
                exportData = exportData + "?;";
                //Ore Quota Fissa <= % LAV
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Ore Quota Fissa > % Lav
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Ore Lavoro Suppl PT Festivo
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Ore Lavoro Suppl PT Notturno
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Ore Lavoro Suppl PT Nott. Festivo
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Riservato 10
                exportData = exportData + "?;";
                #region Giustificativo 3 - 4
                if (giustificativo3 == "" && oreGiustificativo3 == "")
                {
                    for (int i = 0; i < endOfMonth.Day; i++)
                    {
                        giustificativo3 = giustificativo3 + "  ;";
                        oreGiustificativo3 = oreGiustificativo3 + "0000;";
                    }
                    exportData = exportData + giustificativo3 + oreGiustificativo3;
                }
                else 
                {
                    exportData = exportData + giustificativo3 + oreGiustificativo3;
                }
                if (giustificativo4 == "" && oreGiustificativo4 == "")
                {
                    for (int i = 0; i < endOfMonth.Day; i++)
                    {
                        giustificativo4 = giustificativo4 + "  ;";
                        oreGiustificativo4 = oreGiustificativo4 + "0000;";
                    }
                    exportData = exportData + giustificativo4 + oreGiustificativo4;
                }
                else
                {
                    exportData = exportData + giustificativo4 + oreGiustificativo4;
                }
                #endregion
                //Riservato 11
                exportData = exportData + "?;";
                //Banca Ore Magg.Norm.
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Riservato 12
                exportData = exportData + "?;";
                //Magg. Diurna Ordinaria
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Magg 6 Giorno Diurno
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Magg 6 Giorno Notturno
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Magg 7 Giorno Diurno
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                //Magg 7 Giorno Notturno
                for (int i = 0; i < endOfMonth.Day; i++)
                {
                    exportData = exportData + "0000;";
                }
                TxtLines.Add(exportData);
            }
        }

        #endregion

        #region Private Methods

        private int ToCent(int minutes)
        {
            return Convert.ToInt32(((minutes / 60.0) * 100));
        }

        private int ToSixty(int minutes)
        {
            return Convert.ToInt32(((minutes / 100.0) * 60));
        }

        private int RoundSixtyToThirty(int minutes)
        {
            return Convert.ToInt32(Math.Round((double)minutes / 30) * 30);
        }

        private void InitMotivationRegistry(Dictionary<Cant, Dictionary<string, int>> register, Cant cant, string mot)
        {
            if (!register[cant].ContainsKey(mot))
                register[cant][mot] = 0;
        }

        #endregion



    }
}
