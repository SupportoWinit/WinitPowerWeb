using Business.Repository;
using Common;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.ElaborateService.Helpers
{
    internal static class ElaborateServiceHelper
    {
        public static void CoupleBlockedRegs(IEnumerable<Reg> regsToCouple)
        {
            // se le reg passate come parametro non sono null
            if (regsToCouple != null)
            {
                var hoursToCouple = regsToCouple.Where(reg => !reg.Cant.IsActivity).ToList();
                var activitiesToCouple = regsToCouple.Where(reg => reg.Cant.IsActivity).ToList();

                #region Scrittura dell'id corretto nelle attività bloccate e accoppiate o meno

                // se ci sono delle attività bloccate da processare
                if (activitiesToCouple.Count > 0)
                {
                    for (int i = 0; i < activitiesToCouple.Count; i++)
                    {
                        // si verifica che la attività in elaborazione abbia il codice di accoppiamento,
                        // che potrebbe essere stato tolto in giro precedente del ciclo
                        if (!String.IsNullOrEmpty(activitiesToCouple[i].Codice_Accoppiamento))
                        {
                            // recupero tutte le ore collegate all'attività
                            var linkedRegs = hoursToCouple.Where(reg => reg.Codice_Accoppiamento == activitiesToCouple[i].Codice_Accoppiamento).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                            // se ho delle reg ore collegate allora significa che ho anche un'entrata/uscita e quindi inserisco sull'attività
                            // il riferimento alla registrazione di entrata a cui è abbinata
                            if (linkedRegs.Count > 0)
                            {
                                var regE = linkedRegs.First();
                                activitiesToCouple[i].RiferimentoRRN_Att = regE.Reg_Id;
                                activitiesToCouple[i].Codice_Accoppiamento = null;
                                activitiesToCouple[i].Registrazione_Tipo_Reg = (int)RegTypeEnum.Att;
                            }
                            else
                            {
                                // se invece non ci sono ore collegate significa che sto processando una attività singola,
                                // e quindi mi limito a svuotare il codice di accoppiamento.
                                activitiesToCouple[i].Codice_Accoppiamento = null;
                                activitiesToCouple[i].Registrazione_Tipo_Reg = (int)RegTypeEnum.Att;
                            }
                        }
                    }

                    // al termine del trattamento delle attività bloccate salvo sul db quanto fatto
                    RepoManager.RegRepo.BulkUpdate(activitiesToCouple);
                }

                #endregion

                #region Scrittura dell'id corretto nelle reg bloccate e accoppiate

                if (hoursToCouple.Count > 0)
                {
                    // sono elaborate tutte le ore passate come parametro
                    for (int i = 0; i < hoursToCouple.Count; i++)
                    {
                        // si verifica che la reg in elaborazione abbia il codice di accoppiamento,
                        // che potrebbe essere stato tolto in giro precedente del ciclo
                        if (!String.IsNullOrEmpty(hoursToCouple[i].Codice_Accoppiamento))
                        {
                            // si recuperano le due reg che hanno quel codice di acccoppiamento
                            var hourCouple =
                                hoursToCouple.Where(
                                    reg => reg.Codice_Accoppiamento == hoursToCouple[i].Codice_Accoppiamento).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).ToList();

                            // se sono state trovate due reg da accoppiare (situazione normale)
                            if (hourCouple.Count == 2)
                            {
                                // recupero di reg in entrata e in uscita
                                Reg regEToCouple = hourCouple.First();
                                Reg regUToCouple = hourCouple.Last();

                                // aggiornamento del relative record number di riferimento sulla reg in uscita
                                // e svuotamento per entrambe le reg del codice di accoppiamento
                                regUToCouple.RiferimentoRRN_Reg = regEToCouple.Reg_Id;
                                regEToCouple.Codice_Accoppiamento = null;
                                regUToCouple.Codice_Accoppiamento = null;

                                // le reg sono indicate come accoppiate
                                regEToCouple.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;
                                regUToCouple.Registrazione_Stato_Reg = (int)RegStateEnum.Ass;

                            }
                            else
                            // altrimenti, sono registrazioni singole e quindi si procede allo svuotamento del codice di accoppiamento
                            {
                                foreach (var reg in hourCouple)
                                {
                                    reg.Codice_Accoppiamento = null;
                                }
                            }
                        }
                    }

                    // aggiornamento delle reg di ore modificate (se presenti)
                    RepoManager.RegRepo.BulkUpdate(hoursToCouple);
                }

                #endregion

            }
        }

    }
}
