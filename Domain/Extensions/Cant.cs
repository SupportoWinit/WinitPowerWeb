using System.Collections.Generic;
using System.Linq;
using System;
using Common;
using Newtonsoft.Json.Linq;
using Domain;

namespace Domain
{
    public partial class Cant
    {
        public NocturneTypeEnum NocturneTypeEnum
        {
            get { return (NocturneTypeEnum)TipoNotturno_Can; }
        }

        public RoundingMethodEnum RoundingMethodEnum
        {
            get { return (RoundingMethodEnum)Metodo_Arrotondamento_Can; }
        }

        // CAMPI AGGIUNTIVI (calcolati) della Tabella CANT 

        public int N_Fru_Cant
        {
            get { return Fru_Cant.Count; }
        }



        public int N_Note_Cant
        {
            get { return Cant_Note.Count; }
        }

        public string Note_Unita_Associata
        {
            get
            {
                if (Fru_Cant.Any())
                {
                    return Fru_Cant.Last().Fru.Note_Fru;
                }
                return null;
            }
        }

        public int N_Cant_Var
        {
            get { return Cant_Var.Count; }
        }

        public string Codice_Cliente
        {
            get { return Cli != null ? Cli.Codice_Cliente : null; }
        }

        public string CognomeNome_Cli
        {
            get { return Cli != null ? String.Format("{0} {1}", Cli.Cognome_Cli, Cli.Nome_Cli) : String.Empty; }
        }

        // Liste Campi per rendere visibili le Tabelle anche nella FIELD LIST degli XRREPORT

        public List<Fru_Cant> RFru_Cants
        {
            get { return Fru_Cant.OrderByDescending(fruCant => fruCant.Abilitazione_Data_Inizio_Fru_Can).ToList(); }
        }

        public List<Cant_Note> RCant_Notes
        {
            get { return Cant_Note.OrderByDescending(CantNote => CantNote.Data_Nota_Can_Note).ToList(); }
        }

        public bool IsActivity
        {
            get { return Tipologia_Can.ToUpper() == "ATT"; }
        }

        public string GeocodeAddress
        {
            get
            {
                return String.Format("{0} {1} {2} {3}", Indirizzo_Can, Cap_Can, Luogo_Can, Provincia_Can);
            }
        }

        public string Fil_Desc
        {
            get { if (Fil_Id.HasValue && Fil != null) return Fil.Descrizione_Fil; else return String.Empty; }
        }

        public string Fil_Cod
        {
            get { if (Fil_Id.HasValue && Fil != null) return Fil.Codice_Fil; else return String.Empty; }
        }

        public DateTime? LastDateActiveFru
        {
            get
            {
                if (Fru_Cant.Any())
                    return Fru_Cant.Max(fru => fru.Abilitazione_Data_Inizio_Fru_Can);
                return null;
            }
        }

        public string LastFruCode
        {
            get
            {
                if (Fru_Cant.Any())
                    return Fru_Cant.Where(fru => fru.Abilitazione_Data_Inizio_Fru_Can == LastDateActiveFru).Select(fru => fru.Codice_Fru).First();

                return string.Empty;
            }
        }

        public string LastFruNSerie
        {
            get
            {
                if (Fru_Cant.Any())
                    return Fru_Cant.Where(fru => fru.Abilitazione_Data_Inizio_Fru_Can == LastDateActiveFru).Select(fru => fru.N_Serie_Fru).First();

                return string.Empty;
            }
        }

        //mi ritrorna la data dell'ultima Reg di quel cantiere
        public DateTime? LastReg
        {
            get
            {
                DateTime? retrunval = null;

                //se ho qualche registarzione per quel cantiere mi ritorna la data max di quella reg
                if (Reg.Any())
                    retrunval = Reg.Max(regs => regs.Registrazione_Data_Ora_Fis_Reg.Date);

                return retrunval;
            }

        }

        public static string ClockAppApiPath
        {
            get
            {
                return "//api/ManagePowerWebCants/";
            }
        }



        public static JArray Synchronize(IEnumerable<Cant> cants)
        {
            JArray items = new JArray();
            JObject stub = null;
            
            foreach (var cant in cants)
            {
                stub = new JObject();

                stub.Add("CantCode", cant.Codice_Cantiere);
                stub.Add("CantDes", cant.Descrizione_Can);
                stub.Add("Latitudine", cant.LatitudineGps_Can);
                stub.Add("Longitudine", cant.LongitudineGps_Can);
                stub.Add("CantMatr", cant.Fru_Cant.Any() ? cant.Fru_Cant.OrderByDescending(c => c.Abilitazione_Data_Inizio_Fru_Can).First().Codice_Fru : "NA");
                stub.Add("Raggio", cant.RaggioGps_Can);
                stub.Add("PowerWebCantId", cant.Cant_Id);
                stub.Add("Disabilitato", cant.DisAbilitazione_Can);
                items.Add(stub);

            }

            return items;

        }

    }
}
