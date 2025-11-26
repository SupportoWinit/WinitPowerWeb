using Business.Repository;
using Common;
using Domain;
using System;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Web;
using DevExpress.XtraRichEdit.Fields.Expression;
using UnityEngine;
using DevExpress.PivotGrid.OLAP.AdoWrappers;

namespace PowerWeb.Api
{
    public class CheckNoTimbsController : GenericUploadFileApi
    {

        /// <summary>
        /// Il separatore tra un valore e l'altro del file delle registrazioni
        /// </summary>
        private const string RegsFileSeparator = ";";

        /// <summary>
        /// Il nome del file utilizzato per effettuare i controlli di esecuzione esclusiva della web api
        /// </summary>
        private const string ExclusiveAccessFileName = "apiLock.tmp";

        private static readonly ILog _log = LogManager.GetLogger(typeof(Domain.Reg));

        /// <summary>
        /// Esegue l'operazione di import oggetto della API.
        /// </summary>
        protected override HttpStatusCode ExecuteOperation()
        {
            DateTime dateToCheck = DateTime.Now.AddDays(-1);
            DateTime start = new DateTime(dateToCheck.Year, dateToCheck.Month, dateToCheck.Day, 0, 0, 0);
            DateTime end = new DateTime(dateToCheck.Year, dateToCheck.Month, dateToCheck.Day, 23, 59, 59);
            _log.InfoFormat("Inizio controllo assenze del {0}", dateToCheck.DayOfWeek.ToString());
            HttpStatusCode ritorno = HttpStatusCode.OK;
            List<Col> colAssenti = new List<Col>();
            List<Col> collaboratori = RepoManager.ColRepo.GetAllQueryable(c => c.DisAbilitazione_Col == false).ToList();
            _log.InfoFormat("Recuperati {0} collaboratori per cui controllare la presenza", collaboratori.Count);
            foreach (Col col in collaboratori) 
            {
                List<Reg> colRegs = RepoManager.RegRepo.GetAllQueryable(r => r.Col_Id == col.Col_Id && r.Registrazione_Data_Ora_Fis_Reg > start && r.Registrazione_Data_Ora_Fis_Reg < end).ToList();
                if (colRegs.Count == 0) 
                {
                    colAssenti.Add(col);
                }
            }
            _log.InfoFormat("Rilevati {0} collaboratori senza timbrature", colAssenti.Count);

            string invioMail = InviaResoconto(colAssenti);
            if (invioMail != "Mail inviata!")
            {
                //se rilevo errori nell'invio della mail non aggiorno la data ultimo invio così da riprovare l'invio
                _log.InfoFormat("Errore rilevato nell'invio della mail");
                ritorno = HttpStatusCode.InternalServerError;
            }

            return ritorno;
        }
        private string InviaResoconto(List<Col> collaboratori)
        {
            //Prepara il body della mail caricando il css
            string mailBody = "<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; \">",
                   errorMessage = "";
        
        
            DateTime today = DateTime.Now;
            mailBody += "<p>Il giorno <b>" + today.ToString("dddd d MMMM yyyy") + "</b> non sono state registrate timbrature per i seguenti collaboratorI: </p>";
            foreach (Col collaboratore in collaboratori) {
                var ultimaTimbraturaCol = RepoManager.RegRepo.GetAllQueryable(r => r.Col_Id == collaboratore.Col_Id).OrderBy(r => r.Registrazione_Data_Ora_Fis_Reg).ToList();
                //mailBody += "<p>Il giorno " + today.ToString("dddd d MMMM yyyy") + " non sono state registrate timbrature per il collaboratore " + collaboratore.CognomeNome_Col + "</p>";
                if (ultimaTimbraturaCol.Count > 0)
                {
                    //mailBody += "<p style='margin:0;'> - "
                    //   + collaboratore.CognomeNome_Col
                    //   + " | Ultima timbratura: " + ultimaTimbraturaCol.Last().Registrazione_Data_Ora_Fis_Reg.ToString("HH:mm dd/MM/yyyy")
                    //   + ";</p>";

                    mailBody += "<p style='margin:0;'>" +
                        "<span style='display:inline-block; width:260px;'> - " + collaboratore.CognomeNome_Col + "</span>" +
                        "<span>Ultima timbratura: " + ultimaTimbraturaCol.Last().Registrazione_Data_Ora_Fis_Reg.ToString("dd/MM/yyyy HH:mm") + "</span>" +
                        "</p>";
                }
                else 
                {
                    //mailBody += "<p style='margin:0;'> - "
                    //   + collaboratore.CognomeNome_Col
                    //   + " | Nessuna timbratura registrata;</p>";
                    mailBody += "<p style='margin:0;'>" +
                        "<span style='display:inline-block; width:260px;'> - " + collaboratore.CognomeNome_Col + "</span>" +
                        "<span>Nessuna timbratura rilevata </span>" +
                        "</p>";
                }
                    
            }

            string mailTo = RepoManager.ParamRepo.ParametersRow.CompanyEmail;

            if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.MailTo) == 1) 
            {
                var mailList = Enumerable.Range(1, 5)
                        .Select(i => RepoManager.ParamRepo.GetCustomizationParamFromEnum(CustomizationEnum.MailTo, $"Mail{i}"))
                        .Where(m => !string.IsNullOrWhiteSpace(m));

                mailTo = string.Join(";", mailList);
            }

            mailBody += "</div>";
            //Invia le mail
            errorMessage = CommonService.sendMail("dTezzon@winitsrl.it"/*mailTo*/, "PowerWeb - Comunicazione assenze " + today.ToString("d MMMM yyyy"), mailBody, "newsletter@winit.it", "PowerWeb - Comunicazione assenze", new string[] { });
        
            return errorMessage;
        }
    }
}