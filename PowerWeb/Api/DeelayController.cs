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
    public class DeelayController : GenericUploadFileApi
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
            DateTime today = DateTime.Now;
            DateTime compareToday = new DateTime(today.Year, today.Month, today.Day);
            _log.InfoFormat("Inizio controllo ritardi delle {0}", today.DayOfWeek.ToString());
            HttpStatusCode ritorno = HttpStatusCode.OK;
            List<Cant> cantieriRitardo = new List<Cant>();
            bool inviaMailRitardi = false;
            //creo una lista in cui inseriro i vari tipi di orario
            List<Tab_Orari_Tipo> orari = new List<Tab_Orari_Tipo>();
            List<Tab_Orari> tab_Oraris = new List<Tab_Orari>();
            //recupero tutti i cantieri che hanno un orario associato e non sono disabilitati
            List<Cant> cantieriOrari = RepoManager.CantRepo.GetAllQueryable(c => c.DisAbilitazione_Can == false && c.Tab_Orari_Tipo_Id > 0).ToList();
            //procedo a ciclare tutti i cantieri recuperati per popolare la lista degli orari
            foreach (Cant cantiere in cantieriOrari) {
                _log.InfoFormat("Recuperati {0} cantieri con orario da controllare", cantieriOrari.Count());
                bool valido = false;
                Tab_Orari_Tipo orario = RepoManager.Tab_OrariTipoRepo.Single(or => or.Tab_Orari_Tipo_Id == cantiere.Tab_Orari_Tipo_Id);
                Tab_Orari orarioFinale = new Tab_Orari();
                //recupero l'orario del cantieri raggruppando per data così da utilizzare l'ultimo in ordine cronologico
                var listaOrari = RepoManager.Tab_OrariRepo.GetAllQueryable(or => or.Tab_Orari_Tipo_Id == orario.Tab_Orari_Tipo_Id).GroupBy(or => or.Data_Inizio).ToList();
                foreach (var singoloOrario in listaOrari.Last()) {
                    switch (today.DayOfWeek)
                    {
                        case DayOfWeek.Monday:
                            if (singoloOrario.G1) {
                                orarioFinale = singoloOrario;
                                valido = true;
                            } 
                            break;
                        case DayOfWeek.Tuesday:
                            if (singoloOrario.G2)
                            {
                                orarioFinale = singoloOrario;
                                valido = true;
                            }
                            break;
                        case DayOfWeek.Wednesday:
                            if (singoloOrario.G3)
                            {
                                orarioFinale = singoloOrario;
                                valido = true;
                            }
                            break;
                        case DayOfWeek.Thursday:
                            if (singoloOrario.G4)
                            {
                                orarioFinale = singoloOrario;
                                valido = true;
                            }
                            break;
                        case DayOfWeek.Friday:
                            if (singoloOrario.G5)
                            {
                                orarioFinale = singoloOrario;
                                valido = true;
                            }
                            break;
                        case DayOfWeek.Saturday:
                            if (singoloOrario.G6)
                            {
                                orarioFinale = singoloOrario;
                                valido = true;
                            }
                            break;
                        case DayOfWeek.Sunday:
                            if (singoloOrario.G7)
                            {
                                orarioFinale = singoloOrario;
                                valido = true;
                            }
                            break;
                    }
                }
                
                if (valido)
                {
                    _log.InfoFormat("Il cantiere {0} prevede timbrature oggi", cantiere.Descrizione_Can);
                    //se valido diventa true vuol dire che oggi sono previste timbrature per il cantiere corrente 
                    List<Reg_V> cantRegs = RepoManager.Reg_VRepo.GetAllQueryable(r => r.Cant_Id == cantiere.Cant_Id && r.Data_Reg == compareToday).ToList();
                    //se non trovo timbrature vuol dire che nessuno ha timbrato sul cantiere
                    if (cantRegs.Count() == 0)
                    {
                        TimeSpan oraLimite = today.TimeOfDay;
                        List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                        //calcolo l'ora limite partendo dall'orario e aggiungo la tolleranza
                        if (cantiere.Tolleranza_Limite_Entrata_Cant != null)
                        {
                            oraLimite = orarioFinale.Ora_E.Value.Add(cantiere.Tolleranza_Limite_Entrata_Cant.Value);
                        }
                        else
                        {
                            oraLimite = orarioFinale.Ora_E.Value.Add(parametri.First().Tolleranza_Limite_Entrata.Value);
                        }
                        TimeSpan todayHour = new TimeSpan(today.Hour, today.Minute, today.Second);
                        //se l'ora attuale è oltre l'ora 
                        if (oraLimite < todayHour)
                        {
                            if (cantiere.Data_Rapporto_Inizio_5_Can == null)
                            {
                                //se l'ora attuale è inferiore all'ora limite procedo ad inviare la mail comunicando che non ci sono timbrature
                                cantieriRitardo.Add(cantiere);
                            }
                            else
                            {
                                if (cantiere.Data_Rapporto_Inizio_5_Can < compareToday)
                                {
                                    //se l'ora attuale è inferiore all'ora limite procedo ad inviare la mail comunicando che non ci sono timbrature
                                    cantieriRitardo.Add(cantiere);
                                }
                            }
                        }
                    }
                    else {
                        TimeSpan oraLimite = today.TimeOfDay;
                        List<Param> parametri = RepoManager.ParamRepo.GetAll().ToList();
                        //calcolo l'ora limite partendo dall'orario e aggiungo la tolleranza
                        if (cantiere.Tolleranza_Limite_Entrata_Cant != null)
                        {
                            oraLimite = orarioFinale.Ora_E.Value.Add(cantiere.Tolleranza_Limite_Entrata_Cant.Value);
                        }
                        else
                        {
                            oraLimite = orarioFinale.Ora_E.Value.Add(parametri.First().Tolleranza_Limite_Entrata.Value);
                        }
                        foreach (Reg_V reg in cantRegs) {
                            if (reg.Data_Ora_Fig_ETime.Value > oraLimite && !reg.Ritardo_Mail_Sent) {
                                Reg regE = RepoManager.RegRepo.Single(r => r.Reg_Id == reg.RegE);
                                TimeSpan differenza = reg.Data_Ora_Fig_ETime.Value - oraLimite;
                                regE.Ritardo_Durata = (int)differenza.TotalMinutes;
                                RepoManager.RegRepo.Update(regE,true);
                                inviaMailRitardi = true;
                            }
                        }
                    }
                }
                else 
                {
                    //se valido rimane a false vuol dire che l'orario non prevede timbrature per il giorno corrente
                    _log.InfoFormat("L'orario del cantiere {0} non prevede timbrature per {1}", cantiere.Descrizione_Can, today.DayOfWeek.ToString());
                }
            }
            if (cantieriRitardo.Count() > 0) {
                _log.InfoFormat("Trovati {0} cantieri per cui mandare la mail", cantieriRitardo.Count());
                string invioMail = InviaResoconto(cantieriRitardo);
                if (invioMail != "Mail inviata!")
                {
                    //se rilevo errori nell'invio della mail non aggiorno la data ultimo invio così da riprovare l'invio
                    _log.InfoFormat("Errore rilevato nell'invio della mail");
                    ritorno = HttpStatusCode.InternalServerError;
                }
                else 
                {
                    //se la mail è inviata correttamente aggiorno la data ultimo invio
                    _log.InfoFormat("Invio mail effettuato correttamente, procedo ad aggiornare la data di ultimo invio così da inviare una sola mail");
                    foreach (Cant cantiere in cantieriRitardo) {
                        cantiere.Data_Rapporto_Inizio_5_Can = compareToday;
                        RepoManager.CantRepo.SaveChanges();
                    }
                }
            }
            if (inviaMailRitardi) 
            {
                RepoManager.Reg_VRepo.InviaRitardi();
            }
            return ritorno;
        }
        private string InviaResoconto(List<Cant> cantieri)
        {
            //Prepara il body della mail caricando il css
            string mailBody = "<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; \">",
                   errorMessage = "";
        
        
            DateTime today = DateTime.Now;
            foreach (Cant cantiere in cantieri) {
                mailBody += "<p>Il giorno " + today.ToString("dddd d MMMM yyyy") + " alle ore " + today.ToString("HH:mm") + " non sono state registrate timbrature sul cantiere " + cantiere.Descrizione_Can + "</p>";
            }

            mailBody += "</div>";
            //Invia le mail
            errorMessage = CommonService.sendMail(RepoManager.ParamRepo.ParametersRow.CompanyEmail, "PowerWeb - Comunicazione ritardi " + today.ToString("d MMMM yyyy"), mailBody, "newsletter@winit.it", "PowerWeb - Comunicazione ritardi", new string[] { });
        
            return errorMessage;
        }
    }
}