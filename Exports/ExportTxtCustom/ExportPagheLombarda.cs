using Business;
using Business.BusinessExtension;
using Business.Repository;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exports.ExportTxtCustom
{
    class ExportPagheLombarda : TxtToolBox
    {
        string mese = "";
        string giorno = "";
        string filiale = "41   ";
        string qualifica = "";
        string matricola = "";
        string blank3 = "   ";
        string causale = "";
        string blank33 = "                                 ";
        string ore = "";
        string minuti = "";
        string blank51 = "                                                   ";
        double todayJustifications = 0;

        public override void LaunchExport()
        {

            var collaboratori = RepoManager.ColRepo.Find(col => SelectedIds.Contains(col.Col_Id));
            var tabDecods = RepoManager.Tab_DecodRepo.Find(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Campo1_Tab != null).ToList();

            mese = ExportDate.Month.ToString("00");

            foreach (var col in collaboratori)
            {
                Col collaboratore = col;
                qualifica = collaboratore.Qualifica_Col ?? "1";
                CommonService.FillWithChar(false, ' ', 2, ref qualifica);

                matricola = collaboratore.Codice_Collaboratore.Trim() ?? "0000";

                //Se il codice collaboratore è più lungo di 6 caratteri, tengo solo gli ultimi 4
                if (matricola.Length > 6)
                    matricola = matricola.Substring(matricola.Length - 6);
                else
                    CommonService.FillWithChar(true, ' ', 6, ref matricola);

                var cartellinoEditabileConvertito = TimesheetModuleItem.GenerateCartellino(ExportDate,
                                                                                 collaboratore,
                                                                                 isDecimalHours: true,
                                                                                 calculateWorkedHours: true,
                                                                                 calculateJustifications: true,
                                                                                 calculateTrips: false,
                                                                                 calculateOrdStrTimesheet: true,
                                                                                 devidePlanByDayNight: false,
                                                                                 showWeeklyTotal: false)["straordinari"].Select(TimesheetModuleItem.ConvertTimesheetModuleItemToTimesheet).ToList();

                var cartellinoDaEsportare = cartellinoEditabileConvertito.Where(ts => tabDecods.Any(td => td.Chiave_Tab == ts.Justification)).ToList();

                var cartellinoDaUnire = cartellinoEditabileConvertito.Where(ts => !tabDecods.Any(td => td.Chiave_Tab == ts.Justification)).ToList();

                cartellinoDaEsportare.Add(RepoManager.TimesheetRepo.mergeTimesheets(cartellinoDaUnire.First(), cartellinoDaUnire.Last(), "LBL_LAVORO_NOTTURNO", collaboratore.Col_Id, ExportDate));

                //Recupera il cartellino delle motivazioni (no piano, totale e figurative)
                var justifications = TimesheetModuleItem.GenerateCartellino(ExportDate,
                                                                                 collaboratore,
                                                                                 isDecimalHours: true,
                                                                                 calculateWorkedHours: true,
                                                                                 calculateJustifications: true,
                                                                                 calculateTrips: false,
                                                                                 calculateOrdStrTimesheet: false,
                                                                                 devidePlanByDayNight: false,
                                                                                 showWeeklyTotal: false)["justification"].Where(tmi => tmi.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_PLAN) &&
                                                                                                                                                                               tmi.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_TOTALE) &&
                                                                                                                                                                               tmi.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ORE) &&
                                                                                                                                                                               !tmi.Justification.Equals("OL") &&
                                                                                                                                                                               tmi.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_DELTA) &&
                                                                                                                                                                               tmi.Justification != BusinessService.GetLocalizedString(PowerWebResources.LBL_ARROT_DURATA)).ToList();

                var lavoroNotturno = cartellinoDaEsportare.FirstOrDefault(tms => tms.Justification == "LBL_LAVORO_NOTTURNO");


                for (int i = 1, lastDay = CommonService.GetLastMonthDay(ExportDate).Day; i <= lastDay; i++)
                {
                    giorno = i.ToString("00");
                    todayJustifications = 0;

                    //Scrive i record delle motivazioni
                    foreach (TimesheetModuleItem tmi in justifications)
                    {
                        double totalHours = (double)tmi["Day" + giorno];
                        todayJustifications = CommonService.SumDoubleHours(todayJustifications, totalHours);
                        ore = Math.Truncate(totalHours).ToString("00");
                        minuti = ((totalHours - Math.Truncate(totalHours)) * 100).ToString("00");

                        // Esporta solo se per la motivazione ci sono ore
                        if (ore != "00" || minuti != "00")
                        {
                            causale = tmi.Justification;
                            CommonService.FillWithChar(false, ' ', 3, ref causale);
                            TxtLines.Add(string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", mese, giorno, filiale, qualifica, matricola, blank3, causale, blank33, ore, minuti, blank51));
                        }
                    }

                    //Scrive i record di diurno/notturno/ordinario/straordinario
                    foreach (var timesheet in cartellinoDaEsportare)
                    {
                        TimeSpan timesheetTimeSpan = (TimeSpan)timesheet["Day" + giorno];
                        double timesheetDouble = CommonService.GetDoubleFromMinutes(CommonService.GetMinutesFromTimeSpan(timesheetTimeSpan));

                        causale = RepoManager.Tab_DecodRepo.Single(td => td.Nome_Tab == "EDITABLE_TIMESHEET_JUSTIFICATION" && td.Chiave_Tab == timesheet.Justification).Campo1_Tab.Trim();

                        if (causale == "O")
                        {
                            timesheetDouble = CommonService.SumDoubleHours(timesheetDouble, CommonService.GetDoubleFromTimeSpan((TimeSpan)lavoroNotturno["Day" + giorno]));
                            timesheetDouble = CommonService.SubtractDoubleHours(timesheetDouble, todayJustifications);
                            if (timesheetDouble < 0)
                            {
                                timesheetDouble = 0;
                            }
                        }

                        ore = Math.Truncate(timesheetDouble).ToString("00");
                        minuti = ((timesheetDouble - Math.Truncate(timesheetDouble)) * 100).ToString("00");

                        // Esporta solo se per la motivazione ci sono ore
                        if (ore != "00" || minuti != "00")
                        {
                            if (minuti == "30")
                            {
                                minuti = "50";
                            }

                            CommonService.FillWithChar(false, ' ', 3, ref causale);
                            TxtLines.Add(string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", mese, giorno, filiale, qualifica, matricola, blank3, causale, blank33, ore, minuti, blank51));
                        }
                    }
                }
            }
        }
    }
}
