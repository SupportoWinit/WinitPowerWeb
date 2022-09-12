using System.Collections.Generic;
using System.Linq;
using Business.Repository;
using DevExpress.Web.ASPxCallback;
using DevExpress.Web.ASPxClasses;
using DevExpress.Web.ASPxEditors;
using Domain;
using DevExpress.Web.Data;
using log4net;
using Business;
using Business.BusinessExtension;
using System;
using DevExpress.Web.ASPxGridView;
using Reports;
using Common;
using System.Device.Location;

//TODO: attenzione! Questo modulo non filtra come dovrebbe per responsabile; da aggiungere eventuale filtro per responsabile
namespace PowerWeb.Modules
{
    public partial class TrackingModule : BaseGridModule
    {

        #region Properties

        /// <summary>
        /// Recupera la form di edit per i record della griglia principale del modulo corrente.
        /// </summary>
        /// <value>
        /// La form di edit per i record della griglia principale del modulo corrente.
        /// </value>
        public override PowerFormTemplate EditFormTemplate
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Recupera dai parametri dell'applicativo la chiave di bing utilizzata per effettuare la gelolocalizzazione.
        /// </summary>
        /// <value>
        /// La chiave di bing utilizzata per effettuare la geolocalizzazione.
        /// </value>
        protected string BingKey
        {
            get
            {
                return RepoManager.ParamRepo.ParametersRow.BingKey;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Effettua il reset dei dati di sessione per il modulo corrente.
        /// </summary>
        public override void ResetSession()
        {
            base.ResetSession();
        }

        #endregion

        #region Page Events

        /// <summary>
        /// Gestisce l'evento init della pagina corrente.
        /// </summary>
        /// <param name="sender">Il mittente dell'evento.</param>
        /// <param name="e">Gli <see cref="EventArgs"/> con i dati evento.</param>
        protected void Page_Init(object sender, EventArgs e)
        {
            // alla prima visualizzazione della pagina si procede al reset dei dati inseriti in sessione
            if (!Page.IsCallback && !Page.IsPostBack)
                ResetSession();

            // bind del combobox di gestione dei collaboratori nel pre-filtro
            PowerWebService.FillComboboxes(cmbCol, "Search_Col_Id");
        }

        #endregion
        
        //Inizializza il combobox dei collaboratori
        protected void cmbCol_Init(object sender, EventArgs e)
        {
            //Imposta il primo collaboratore come default
            cmbCol.SelectedIndex = 0;
        }


        //Callback chiamato al click del bottone 'applica', dopo il controllo sulla validità dei campi
        protected void btnApply_OnCallback(object source, CallbackEventArgs e)
        {
            BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = false;
            // Si carica i campi valorizzati
            DateTime? selectedDateNullable = (DateTime)SearchDate.Value;
            int? selectedColIdNullable = (int)cmbCol.Value;

            // Per ulteriore scrupolo, controlla la validità dei campi (già controllata client-side)
            if(!selectedDateNullable.HasValue)
            {
                if(!((ASPxCallback)source).JSProperties.ContainsKey("cpErrorMessage"))
                {
                    ((ASPxCallback)source).JSProperties.Add("cpErrorMessage", "La data selezionata non è valida!");
                }
            }
            else if (!selectedColIdNullable.HasValue)
            {
                if (!((ASPxCallback)source).JSProperties.ContainsKey("cpErrorMessage"))
                {
                    ((ASPxCallback)source).JSProperties.Add("cpErrorMessage", "Il collaboratore selezionato non è valido!");
                }
            }
            // Se i campi sono validi, recupera le registrazioni da visualizzare su mappa
            else
            {
                //Se nelle JSProperties non c'è ancora l'elemento che conterrà le registrazioni da visualizzare, lo aggiunge vuoto
                if (((ASPxCallback)source).JSProperties.ContainsKey("cpCoordinatesToShow"))
                { 
                    ((ASPxCallback)source).JSProperties.Add("cpCoordinatesToShow", null);
                }

                DateTime selectedDate = selectedDateNullable.Value;
                int selectedColId = selectedColIdNullable.Value;

                List<CoordinatesData> coordinates = new List<CoordinatesData>();
                //Recupera le regv  con coordinate valide ORDINATE PER ORA effettuate dal collaboratore specificato nella data specificata
                List<Reg_V> regvsToShow = RepoManager.Reg_VRepo.Find(regv => regv.Col_Id == selectedColId && regv.Data_Reg == selectedDate /*&& regv.Registrazione_Lat_Orig_E.HasValue && regv.Registrazione_Lat_Orig_E.Value != 0d && regv.Registrazione_Long_Orig_E.HasValue && regv.Registrazione_Long_Orig_E.Value != 0d*/).OrderBy(regv => regv.Data_Ora_FigFis_E).ToList();

                regvsToShow.ForEach(reg => {
                    if(!reg.Registrazione_Lat_Orig_E.HasValue || reg.Registrazione_Lat_Orig_E.Value == 0d || !reg.Registrazione_Long_Orig_E.HasValue || reg.Registrazione_Long_Orig_E.Value == 0d)
                    {
                        Cant cant = RepoManager.CantRepo.FirstOrDefault(c => c.Cant_Id == reg.Cant_Id);
                        if(cant.LatitudineGps_Can!=0 && cant.LongitudineGps_Can != 0)
                        {
                            reg.Registrazione_Lat_Orig_E = cant.LatitudineGps_Can;
                            reg.Registrazione_Long_Orig_E = cant.LongitudineGps_Can;
                        }
                    }
                });

                regvsToShow = regvsToShow.Where(regv => (regv.Registrazione_Lat_Orig_E.HasValue && regv.Registrazione_Lat_Orig_E.Value != 0d && regv.Registrazione_Long_Orig_E.HasValue && regv.Registrazione_Long_Orig_E.Value != 0d)).OrderBy(regv => regv.Data_Ora_FigFis_E).ToList();

                //Recupera la customization che indica quali registrazioni visualizzare nel modulo Tracking
                int trackingRegsToShowCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TrackingRegsToShow);

                //Se indicato dalla customization, filtra le registrazioni mantenendo solo i passaggi
                if (trackingRegsToShowCustomization == (int) TrackingRegsToShow.OnlyPass)
                {
                    regvsToShow = regvsToShow.Where(regv => regv.Registrazione_Tipo_Reg == (int)RegTypeEnum.Pass).ToList();
                }

                int prog = 1; //Progressivo delle registrazioni indicate in mappa
                int index = 0; //Indice della registrazione corrente nella lista
                var prevRegv = default(Reg_V);
                var nextRegv = default(Reg_V);

                var initialRegv = default(Reg_V);
                int initialIndex = 0;

                //Customization che indica come visualizzare registrazioni consecutive nello stesso cantiere
                int consecutiveRegsInSameCantCustomization = RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.TrackingConsecutiveRegsInSameCant);

                //Per ogni registrazione recuperata, prepara l'oggetto atto alla visualizzazione sulla mappa
                foreach (var regv in regvsToShow)
                {
                    var sCoord = new GeoCoordinate(regv.Registrazione_Lat_Orig_E.Value, regv.Registrazione_Long_Orig_E.Value);
                    var eCoord = new GeoCoordinate(regv.Registrazione_Long_Orig_U.Value, regv.Registrazione_Long_Orig_U.Value);

                    double distance = sCoord.GetDistanceTo(eCoord);
                    double raggio = RepoManager.ParamRepo.ParametersRow.RaggioGpsDefault.Value;
                    List<Cant> cant = RepoManager.CantRepo.GetAllQueryable(c=> c.Cant_Id == regv.Cant_Id).ToList();
                    if (RepoManager.ParamRepo.GetCustomizationFromEnum(CustomizationEnum.DoublePushPin) == 1 && (distance > cant.First().RaggioGps_Can || distance > raggio)) {
                        //Se si desidera visualizzare solo la prima e l'ultima registrazione consecutiva all'interno dello stesso cantiere, esclude quelle 'centrali'
                        if (consecutiveRegsInSameCantCustomization == (int)TrackingConsecutiveRegsInSameCant.FirstLast)
                        {
                            // Estrae la prossima registrazione (default se la corrente è l'ultima della lista)
                            nextRegv = index == regvsToShow.Count - 1 ? default(Reg_V) : regvsToShow[index + 1];

                            //Visualizza in mappa le registrazioni da non escludere (quando c'è un gruppo di registrazioni nello stesso cantiere, mantiene solo la prima e l'ultima)
                            if (prevRegv == default(Reg_V) || nextRegv == default(Reg_V) || prevRegv.Cant_Id != regv.Cant_Id || nextRegv.Cant_Id != regv.Cant_Id)
                            {
                                // Istanzia un nuovo oggetto da visualizzare in mappa e lo aggiunge alla lista
                                coordinates.Add(new CoordinatesData
                                {
                                    CurrentLatitude = regv.Registrazione_Lat_Orig_E.HasValue ? regv.Registrazione_Lat_Orig_E.Value : 0d,
                                    CurrentLongitude = regv.Registrazione_Long_Orig_E.HasValue ? regv.Registrazione_Long_Orig_E.Value : 0d,
                                    InfoboxTitle = getInfoboxTitle(regv, prog.ToString()),
                                    InfoboxDescription = getInfoboxDescription(regv),
                                    PushpinLabel = prog.ToString()
                                });
                                //Aumenta il progressivo delle registrazioni visualizzate in mappa
                                prevRegv = regv;
                            }
                            //In ogni caso, progredisce con l'indice e aggiorna le variabili
                            index++;
                        }

                        //Se si desidera visualizzare tutte le registrazioni consecutive nello stesso cantiere, le aggiunge tutte
                        else if (consecutiveRegsInSameCantCustomization == (int)TrackingConsecutiveRegsInSameCant.ShowAll)
                        {
                            // Istanzia un nuovo oggetto da visualizzare in mappa e lo aggiunge alla lista
                            coordinates.Add(new CoordinatesData
                            {
                                CurrentLatitude = regv.Registrazione_Lat_Orig_U.HasValue ? regv.Registrazione_Lat_Orig_U.Value : 0d,
                                CurrentLongitude = regv.Registrazione_Long_Orig_U.HasValue ? regv.Registrazione_Long_Orig_U.Value : 0d,
                                InfoboxTitle = getInfoboxTitle(regv, prog.ToString()),
                                InfoboxDescription = getInfoboxDescription(regv),
                                PushpinLabel = prog.ToString()
                            });

                        }

                        //Se si desidera visualizzare un pushpin riportante nella label il range (primo e ultimo indice)
                        else if (consecutiveRegsInSameCantCustomization == (int)TrackingConsecutiveRegsInSameCant.ShowRange)
                        {

                            // Estrae la prossima registrazione (default se la corrente è l'ultima della lista)
                            nextRegv = index == regvsToShow.Count - 1 ? default(Reg_V) : regvsToShow[index + 1];

                            // Se sono la prima e ultima registrazione su quel cantiere, la aggiungo riportando il l'indice nella label
                            if ((prevRegv == default(Reg_V) || prevRegv.Cant_Id != regv.Cant_Id) && (nextRegv == default(Reg_V) || nextRegv.Cant_Id != regv.Cant_Id))
                            {
                                coordinates.Add(new CoordinatesData
                                {
                                    CurrentLatitude = regv.Registrazione_Lat_Orig_U.HasValue ? regv.Registrazione_Lat_Orig_U.Value : 0d,
                                    CurrentLongitude = regv.Registrazione_Long_Orig_U.HasValue ? regv.Registrazione_Long_Orig_U.Value : 0d,
                                    InfoboxTitle = getInfoboxTitle(regv, prog.ToString()),
                                    InfoboxDescription = getInfoboxDescription(regv),
                                    PushpinLabel = prog.ToString()
                                });
                            }

                            //Se sono la prima, mi segno il suo indice
                            else if (prevRegv == default(Reg_V) || prevRegv.Cant_Id != regv.Cant_Id)
                            {
                                initialIndex = prog;
                                initialRegv = regv;
                            }

                            //Se sono l'ultima, aggiungo il puspin con la lable del range
                            else if (nextRegv == default(Reg_V) || nextRegv.Cant_Id != regv.Cant_Id)
                            {
                                coordinates.Add(new CoordinatesData
                                {
                                    CurrentLatitude = regv.Registrazione_Lat_Orig_U.HasValue ? regv.Registrazione_Lat_Orig_U.Value : 0d,
                                    CurrentLongitude = regv.Registrazione_Long_Orig_U.HasValue ? regv.Registrazione_Long_Orig_U.Value : 0d,
                                    InfoboxTitle = getInfoboxTitle(regv, initialIndex.ToString() + " - " + prog.ToString()),
                                    InfoboxDescription = getRangeInfoboxDescription(initialRegv, regv),
                                    PushpinLabel = initialIndex.ToString() + " - " + prog.ToString()
                                });
                            }

                            prevRegv = regv;
                        }
                    }
                    //Se si desidera visualizzare solo la prima e l'ultima registrazione consecutiva all'interno dello stesso cantiere, esclude quelle 'centrali'
                    if (consecutiveRegsInSameCantCustomization == (int)TrackingConsecutiveRegsInSameCant.FirstLast)
                    {
                        // Estrae la prossima registrazione (default se la corrente è l'ultima della lista)
                        nextRegv = index == regvsToShow.Count - 1 ? default(Reg_V) : regvsToShow[index + 1];

                        //Visualizza in mappa le registrazioni da non escludere (quando c'è un gruppo di registrazioni nello stesso cantiere, mantiene solo la prima e l'ultima)
                        if (prevRegv == default(Reg_V) || nextRegv == default(Reg_V) || prevRegv.Cant_Id != regv.Cant_Id || nextRegv.Cant_Id != regv.Cant_Id)
                        {
                            // Istanzia un nuovo oggetto da visualizzare in mappa e lo aggiunge alla lista
                            coordinates.Add(new CoordinatesData
                            {
                                CurrentLatitude = regv.Registrazione_Lat_Orig_E.HasValue ? regv.Registrazione_Lat_Orig_E.Value : 0d,
                                CurrentLongitude = regv.Registrazione_Long_Orig_E.HasValue ? regv.Registrazione_Long_Orig_E.Value : 0d,
                                InfoboxTitle = getInfoboxTitle(regv, prog.ToString()),
                                InfoboxDescription = getInfoboxDescription(regv),
                                PushpinLabel = prog.ToString()
                            });
                            //Aumenta il progressivo delle registrazioni visualizzate in mappa
                            prog++;
                            prevRegv = regv;
                        }
                        //In ogni caso, progredisce con l'indice e aggiorna le variabili
                        index++;
                    }

                    //Se si desidera visualizzare tutte le registrazioni consecutive nello stesso cantiere, le aggiunge tutte
                    else if (consecutiveRegsInSameCantCustomization == (int)TrackingConsecutiveRegsInSameCant.ShowAll)
                    {
                        // Istanzia un nuovo oggetto da visualizzare in mappa e lo aggiunge alla lista
                        coordinates.Add(new CoordinatesData
                        {
                            CurrentLatitude = regv.Registrazione_Lat_Orig_E.HasValue ? regv.Registrazione_Lat_Orig_E.Value : 0d,
                            CurrentLongitude = regv.Registrazione_Long_Orig_E.HasValue ? regv.Registrazione_Long_Orig_E.Value : 0d,
                            InfoboxTitle = getInfoboxTitle(regv, prog.ToString()),
                            InfoboxDescription = getInfoboxDescription(regv),
                            PushpinLabel = prog.ToString()
                        });

                        prog++;
                    }

                    //Se si desidera visualizzare un pushpin riportante nella label il range (primo e ultimo indice)
                    else if (consecutiveRegsInSameCantCustomization == (int) TrackingConsecutiveRegsInSameCant.ShowRange)
                    {

                        // Estrae la prossima registrazione (default se la corrente è l'ultima della lista)
                        nextRegv = index == regvsToShow.Count - 1 ? default(Reg_V) : regvsToShow[index + 1];

                        // Se sono la prima e ultima registrazione su quel cantiere, la aggiungo riportando il l'indice nella label
                        if ((prevRegv == default(Reg_V) || prevRegv.Cant_Id != regv.Cant_Id) && (nextRegv == default(Reg_V) || nextRegv.Cant_Id != regv.Cant_Id))
                        {
                            coordinates.Add(new CoordinatesData
                            {
                                CurrentLatitude = regv.Registrazione_Lat_Orig_E.HasValue ? regv.Registrazione_Lat_Orig_E.Value : 0d,
                                CurrentLongitude = regv.Registrazione_Long_Orig_E.HasValue ? regv.Registrazione_Long_Orig_E.Value : 0d,
                                InfoboxTitle = getInfoboxTitle(regv, prog.ToString()),
                                InfoboxDescription = getInfoboxDescription(regv),
                                PushpinLabel = prog.ToString()
                            });
                        }

                        //Se sono la prima, mi segno il suo indice
                        else if (prevRegv == default(Reg_V) || prevRegv.Cant_Id != regv.Cant_Id)
                        {
                            initialIndex = prog;
                            initialRegv = regv;
                        }

                        //Se sono l'ultima, aggiungo il puspin con la lable del range
                        else if (nextRegv == default(Reg_V) || nextRegv.Cant_Id != regv.Cant_Id)
                        {
                            coordinates.Add(new CoordinatesData
                            {
                                CurrentLatitude = regv.Registrazione_Lat_Orig_E.HasValue ? regv.Registrazione_Lat_Orig_E.Value : 0d,
                                CurrentLongitude = regv.Registrazione_Long_Orig_E.HasValue ? regv.Registrazione_Long_Orig_E.Value : 0d,
                                InfoboxTitle = getInfoboxTitle(regv, initialIndex.ToString() + " - " + prog.ToString()),
                                InfoboxDescription = getRangeInfoboxDescription(initialRegv, regv),
                                PushpinLabel = initialIndex.ToString() + " - " + prog.ToString()
                            });
                        }
                        
                        prevRegv = regv;
                        prog++;
                    }
                }
                
                if (coordinates.Count() > 0)
                {
                    ((ASPxCallback)source).JSProperties["cpCoordinatesToShow"] = coordinates;
                }

                else
                {
                    if (!((ASPxCallback)source).JSProperties.ContainsKey("cpErrorMessage"))
                    {
                        ((ASPxCallback)source).JSProperties["cpErrorMessage"] = BusinessService.GetLocalizedString(PowerWebResources.STR_NESSUN_DATO_DA_VISUALIZZARE);
                    }
                }
            }
            BusinessService.IsToCloseLoadingPanel[PowerWebContext.Current.User] = true;
        }

        /// <summary>
        /// Prepara e ritorna il testo per l'infobox del pushpin relativo alla regv data in pasto.
        /// </summary>
        /// <param name="regv">La regv da cui estrarre le informazioni ritornate dal metodo.</param>
        /// <returns>La stringa da visualizzare nel titolo dell'infobox.</returns>
        private string getInfoboxDescription(Reg_V regv)
        {
            // di default il metodo ritorna stringa vuota
            string infoboxDescription = String.Empty;
            // si procede solamente se la regv passata come parametro risulta valorizzata
            if (regv != default(Reg_V))
            {
                //Estrae il cantiere associato alla reg
                Cant cant = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == regv.Cant_Id);
                //Di default imposta le coordinate della registrazione nella descrizione del cantiere
                String cantDesc = String.Format("{0}; {1}", regv.Registrazione_Lat_Orig_E.ToString(), regv.Registrazione_Long_Orig_E.ToString());
                //Se il cantiere esiste, usa la sua descrizione
                if (cant != default(Cant))
                {
                    cantDesc = cant.Descrizione_Can;
                }
                //Compone la descrizione dell'infobox
                if (regv.Data_Ora_Fig_UTime != null)
                {
                    infoboxDescription = String.Format("<span>{0} {1} <br><br>Uscita {2} <br><br>{3}</span>", regv.Data_Reg.Value.Date.ToString("dd/MM/yyyy"), regv.Data_Ora_Fis_ETime.Value.ToString(), regv.Data_Ora_Fis_UTime.Value.ToString(), cantDesc);
                }
                else {
                    infoboxDescription = String.Format("<span>{0} {1}<br>{2}</span>", regv.Data_Reg.Value.Date.ToString("dd/MM/yyyy"), regv.Data_Ora_Fis_ETime.Value.ToString(), cantDesc);
                }
            }

            // ritorno del valore calcolato dal metodo
            return infoboxDescription;
        }

        /// <summary>
        /// Prepara e ritorna il testo per l'infobox del pushpin rappresentante un range di timbrature
        /// </summary>
        /// <param name="regv">La regv da cui estrarre le informazioni ritornate dal metodo.</param>
        /// <returns>La stringa da visualizzare nel titolo dell'infobox.</returns>
        private string getRangeInfoboxDescription(Reg_V regv1, Reg_V regv2)
        {
            // di default il metodo ritorna stringa vuota
            string infoboxDescription = String.Empty;
            // si procede solamente se la regv passata come parametro risulta valorizzata
            if (regv1 != default(Reg_V) && regv2 != default(Reg_V))
            {
                //Estrae il cantiere associato alla reg
                Cant cant = RepoManager.CantRepo.SingleOrDefault(c => c.Cant_Id == regv1.Cant_Id);
                //Di default imposta le coordinate della registrazione nella descrizione del cantiere
                String cantDesc = String.Format("{0}; {1}", regv1.Registrazione_Lat_Orig_E.ToString(), regv1.Registrazione_Long_Orig_E.ToString());
                //Se il cantiere esiste, usa la sua descrizione
                if (cant != default(Cant))
                {
                    cantDesc = cant.Descrizione_Can;
                }

                //Compone la descrizione dell'infobox
                infoboxDescription = String.Format("<span>{0}-{1}<br>{2}</span>", regv1.Data_Ora_Fis_ETime.Value.ToString(), regv2.Data_Ora_Fis_ETime.Value.ToString(), cantDesc);
            }

            // ritorno del valore calcolato dal metodo
            return infoboxDescription;
        }

        /// <summary>
        /// Calcola e restituisce il titolo dell'infobox da applicare al pushpin relativo alla regv data in pasto.
        /// </summary>
        /// <param name="regv">La regv da cui estrarre le informazioni ritornate dal metodo.</param>
        /// <returns>La stringa da visualizzare nel titolo dell'infobox.</returns>
        private string getInfoboxTitle(Reg_V regv, String prog)
        {
            // di default il metodo ritorna stringa vuota
            string infoboxDescription = String.Empty;

            String nTimb = "Timbratura numero: ";
            // si procede solamente se la regv passata come parametro risulta valorizzata
            if (regv != default(Reg_V))
            { 
                infoboxDescription = String.Format("{0}<br>{1}<br>{2}{3}", regv.Col_Desc, regv.Data_Reg.Value.ToLongDateString(), nTimb, prog);
            }
            // ritorno del valore calcolato dal metodo
            return infoboxDescription;
        }
    }
}