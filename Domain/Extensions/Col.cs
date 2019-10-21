using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain
{
    public partial class Col
    {

        public RoundingMethodEnum RoundingMethodEnum
        {
            get { return (RoundingMethodEnum)Metodo_Arrotondamento_Col; }
        }
        
        // CAMPI AGGIUNTIVI E/O CALCOLATI della Tabella COL

        public string CognomeNome_Col
        {
            get { return new StringBuilder(Cognome_Col).Append(" ").Append(Nome_Col).ToString(); }
        }

        public int N_Pru_Col
        {
            get { return Pru_Col.Count; }
        }

        public int N_Note_Col
        {
            get { return Col_Note.Count; }
        }

        public int N_Col_Var
        {
            get { return Col_Var.Count; }
        }

        public NocturneTypeEnum NocturneTypeEnum
        {
            get { return (NocturneTypeEnum)TipoNotturno_Col; }
        }

        // Liste Campi per rendere visibili le Tabelle anche nella FIELD LIST degli XRREPORT

        public List<Pru_Col> RPru_Cols
        {
            get { return Pru_Col.OrderByDescending(PruCol => PruCol.Abilitazione_Data_Inizio_Pru_Col).ToList(); }
        }

        public List<Col_Note> RCol_Notes
        {
            get { return Col_Note.OrderByDescending(ColNote => ColNote.Data_Nota_Col_Note).ToList(); }
        }

        public DateTime? LastDateActivePru
        {
            get
            {
                if (Pru_Col.Any())
                    return Pru_Col.Select(pru => pru.Abilitazione_Data_Inizio_Pru_Col).Max();
                return null;
            }
        }

        public string LastPruCode
        {
            get
            {
                if (Pru_Col.Any())
                    return Pru_Col.Where(pru => pru.Abilitazione_Data_Inizio_Pru_Col == LastDateActivePru).Select(pru => pru.Codice_Pru).First();
                return string.Empty;
            }
        }

        public string LastPruNSerie
        {
            get
            {
                if (Pru_Col.Any())
                    return Pru_Col.Where(fru => fru.Abilitazione_Data_Inizio_Pru_Col == LastDateActivePru).Select(pru => pru.N_Serie_Pru).First();

                return string.Empty;
            }
        }

        //mi ritrorna la data dell'ultima Reg di quel collaboratore
        public DateTime? LastReg
        {
            get
            {
                DateTime? retrunval = null;

                //se ho qualche registarzione per qeul collaboratore mi ritorna la data max di quella reg
                if (Regs.Any())
                    retrunval = Regs.Select(regs => regs.Registrazione_Data_Ora_Fis_Reg.Date).Max();

                return retrunval;
            }

        }

        /// <summary>
        /// Recupera l'indirizzo in formato geolocalizzazione per le richieste a BING.
        /// </summary>
        /// <value>
        /// L'indirizzo in formato geolocalizzazione per le richieste a BING.
        /// </value>
        public string GeocodeAddress
        {
            get
            {
                return String.Format("{0} {1} {2}", Domicilio_Indirizzo_Col, Domicilio_Cap_Col, Domicilio_Luogo_Col);
            }
        }

        /// <summary>
        /// Recupera lo stato dell'ultima registrazione per il record di where is it.
        /// </summary>
        /// <value>
        /// Lo stato dell'ultima registrazione per il record di where is it.
        /// </value>
        public WhereIsItRecordState WhereIsItState
        {
            get
            {
                // di default il collaboratore non ha registrazioni su cui effettuare la verifica
                var returnValue = WhereIsItRecordState.NoRegPresent;

                // se è presente qualche registrazione collegata al collaboratore che sia di tipo ore si recupera l'ultima e si ritorna il valore rispetto allo
                // stato di abbinamento della stessa
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                {
                    // se la registrazione è abbinata ed entrata oppure non abbinata allora si segnala che il collaboratore è all'interno;
                    // altrimenti se segnala che il collaboratore è all'uscita
                    if (lastReg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass)
                    {
                        returnValue = WhereIsItRecordState.LastPass;
                    }
                    else if (lastReg.Registrazione_Stato_Reg == (int)RegStateEnum.None || (lastReg.Registrazione_Stato_Reg != (int)RegStateEnum.None && !lastReg.RiferimentoRRN_Reg.HasValue))
                    {
                        returnValue = WhereIsItRecordState.IsInIt;
                    }
                    else
                    {
                        returnValue = WhereIsItRecordState.IsOutOfIt;
                    }
                }

                // ritorno dello stato del record where is it
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera la data dell'ultima registrazione per il record di where is it.
        /// </summary>
        /// <value>
        /// La data dell'ultima registrazione per il record di where is it.
        /// </value>
        public DateTime? WhereIsItDate
        {
            get
            {
                // di default viene ritornato un valore nullo
                DateTime? returnValue = null;

                // si recupera l'ultima registrazione e, se presente, si ritorna la sua data
                // a meno che non sia l'entrata di un'uscita, in quel caso di recupera l'uscita
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                {
                    returnValue = lastReg.Registrazione_Data_Ora_Fis_Reg.Date;
                }

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera l'ora dell'ultima registrazione per il record di where is it.
        /// </summary>
        /// <value>
        /// L'ora dell'ultima registrazione per il record di where is it.
        /// </value>
        public TimeSpan? WhereIsItHour
        {
            get
            {
                // di default viene ritornato un valore nullo
                TimeSpan? returnValue = null;

                // si recupera l'ultima registrazione e, se presente, si ritorna la sua ora di registrazione
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    returnValue = lastReg.Registrazione_Data_Ora_Fis_Reg.TimeOfDay;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera l'id del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// L'id del cantiere per il record di where is it.
        /// </value>
        public int? WhereIsItCantId {
            get
            {
                // di default viene ritornato un valore nullo
                int? returnValue = null;

                // si recupera l'ultima registrazione e, se presente, si ritorna il suo id cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    returnValue = lastReg.Cant_Id;

                // ritorno del valore calcolato
                return returnValue;
            }            
        }

        /// <summary>
        /// Recupera il codice del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// Il codice del cantiere per il record di where is it.
        /// </value>
        public string WhereIsItCantCode
        {
            get
            {
                // di default viene ritornato stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione e, se presente, si ritorna il codice del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.Codice_Cantiere;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera la descrizione del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// La descrizione del cantiere per il record di where is it.
        /// </value>
        public string WhereIsItCantDes
        {
            get
            {
                // di default viene ritornato stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione e, se presente, si ritorna la descrizione del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.Descrizione_Can;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera l'indirizzo del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// L'indirizzo del cantiere per il record di where is it.
        /// </value>
        public string WhereIsItCantAddress
        {
            get
            {
                // di default viene ritornato stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione e, se presente, si ritorna l'indirizzo del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.Indirizzo_Can;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera la città del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// La città del cantiere per il record di where is it.
        /// </value>
        public string WhereIsItCantCity
        {
            get
            {
                // di default viene ritornato stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione e, se presente, si ritorna la città del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.Luogo_Can;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera il CAP del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// Il CAP del cantiere per il record di where is it.
        /// </value>
        public string WhereIsItCantCap
        {
            get
            {
                // di default viene ritornato stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione e, se presente, si ritorna il CAP del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.Cap_Can;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera la provincia del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// La provincia del cantiere per il record di where is it.
        /// </value>
        public string WhereIsItCantProvince
        {
            get
            {
                // di default viene ritornato stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione e, se presente, si ritorna la provincia del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.Provincia_Can;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera la nazione del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// La nazione del cantiere per il record di where is it.
        /// </value>
        public string WhereIsItCantNation
        {
            get
            {
                // di default viene ritornato stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione e, se presente, si ritorna la nazione del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.Nazione_Can;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera la latitudine del cantiere per il record di where is it.
        /// </summary>
        /// <value>
        /// La latitudine del cantiere per il record di where is it.
        /// </value>
        public double WhereIsItCanLatitude
        {
            get
            {
                // di default viene ritornato il valore 0
                double returnValue = 0d;

                // si recupera l'ultima registrazione e, se presente, si ritorna la latitudine del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.LatitudineGps_Can;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera la longitudine del cantiere peril record di where is it.
        /// </summary>
        /// <value>
        /// La longitudine del cantiere per il record di where is it.
        /// </value>
        public double WhereIsItCanLongitude
        {
            get
            {
                // di default viene ritornato il valore 0
                double returnValue = 0d;

                // si recupera l'ultima registrazione e, se presente, si ritorna la longitudine del cantiere
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    if (lastReg.Cant != null)
                        returnValue = lastReg.Cant.LongitudineGps_Can;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera il valore del turno sull'ultima registrazione utile al record where is it.
        /// </summary>
        /// <value>
        /// Il valore del turno sull'ultima registrazione utile al record where is it.
        /// </value>
        public string WhereIsItTurn {
            get
            {
                // di default viene ritornato il valore stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione ora e si ritorna il turno in essa contenuta
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                        returnValue = lastReg.Turno;

                // ritorno del valore calcolato
                return returnValue;
            }
        }
        /// <summary>
        /// Recupera il valore del turno sull'ultima registrazione utile al record where is it.
        /// </summary>
        /// <value>
        /// Il valore del turno sull'ultima registrazione utile al record where is it.
        /// </value>
        public string WhereIsItSubCant
        {
            get
            {
                // di default viene ritornato il valore stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione ora e si ritorna il sotto_cantiere in essa contenuta
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    returnValue = lastReg.Sotto_Cantiere;

                // ritorno del valore calcolato
                return returnValue;
            }
        }


        /// <summary>
        /// Recupera il valore del tipo attività sull'ultima registrazione utile al record where is it.
        /// </summary>
        /// <value>
        /// Il valore del tipo attività sull'ultima registrazione utile al record where is it.
        /// </value>
        public string WhereIsItActivityType
        {
            get
            {
                // di default viene ritornato il valore stringa vuota
                string returnValue = String.Empty;

                // si recupera l'ultima registrazione ora e si ritorna il tipo attività in essa contenuta
                Reg lastReg = GetLastHourReg();
                if (lastReg != default(Reg))
                    returnValue = lastReg.Tipo_Attivita;

                // ritorno del valore calcolato
                return returnValue;
            }
        }

        /// <summary>
        /// Recupera o imposta la stringa nel fromato hhmm del monte minuti attualmente salvato sul collaboratore.
        /// </summary>
        /// <value>
        /// La stringa nel fromato hhmm del monte minuti attualmente salvato sul collaboratore.
        /// </value>
        public string HHMMMonte_Minuti
        {
            get
            {
                return CommonService.GetHHMMStringFormMinutes(Monte_Minuti);
            }
        }

        #region Private Methods

        /// <summary>
        /// Restituisce l'ultima registrazione ora tra quelle collegate, valore di default se non prsente.
        /// </summary>
        /// <returns>L'ultima registrazione ora tra quelle collegate; valore di default se non presente</returns>
        private Reg GetLastHourReg()
        {
            Reg returnReg = default(Reg);
            
            if (Regs.Any(reg => reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass))
                returnReg = Regs.Where(reg => (reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.None || reg.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass)).OrderBy(reg => reg.Registrazione_Data_Ora_Fis_Reg).Last();

            return returnReg;
        }

        #endregion

        public static JArray Synchronize(IEnumerable<Col> cols)
        {
            JArray items = new JArray();
            JObject stub = null;

            foreach (var col in cols)
            {
                stub = new JObject();

                stub.Add("ColCode", col.Codice_Collaboratore);
                stub.Add("ColDes", col.CognomeNome_Col);
                stub.Add("ColMatr", col.Pru_Col.Any() ? col.Pru_Col.OrderByDescending(c => c.Abilitazione_Data_Inizio_Pru_Col).First().Codice_Pru : "NA");
                stub.Add("Disabilitato", col.DisAbilitazione_Col);
                stub.Add("PowerWebColId",col.Col_Id);

                items.Add(stub);

            }

            return items;

        }

    }
}
