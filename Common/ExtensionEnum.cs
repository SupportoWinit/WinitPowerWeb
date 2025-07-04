using System;
using System.Linq.Expressions;


/* ATTENZIONE!!!!

 * Tutti gli enums marcati con attributo "Flags" devono esplicitare la numerazione e quest'ultima DEVE NECESSARIAMENTE seguire le potenze di 2
 *     
 * Si noti che gli enums di tipo "Flags" possono essere composti. Ad esempio posso voler definire un enum per i bordi
 * 
 * [Flags]
 * public enum Borders
 * {
 *   None = 0,
 *   Top = 1,
 *   Right = 2,
 *   Bottom = 4,
 *   Left = 8,
 * }
 * 
 * Per imputare allo stesso valore più enum (ad es. bordo sinistro ed inferiore) utilizzerò la seguente sintassi
 * 
 *  Borders myBorders = Borders.Bottom | Borders.Left;
 *  
 * Il valore di myBorders è quindi 4 | 8 => 4 + 8 = 12
 * 
 * Per controllarne il valore  in un if (ad esempio se devo controllare se tracciare il bordo inferiore) utilizzerò la seguente sintassi
 * 
 *  if((myBorders & Borders.Bottom) == Borders.Bottom)
 * 
 * questo perchè se scrivessi if(myBorders == Borders.Bottom) il compilatore semplificherebbe in if(12 == 4)
 * 
 */
namespace Common
{
    #region CustomizationEnum

    public enum CustomizationEnum
    {
        #region Customizazzione MasterPage

        DisableColumnChooser,
        DisableExcelExport,
        DisablePdfExport,
        DisableViewRowButton,

        #endregion

        IWell,
        EnableDoubleCheckLogin,
        AvoidDisabledCartellinoCols,
        SadCpExport,
        LogMalformedRegs,
        G4,
        GenerateRettificheUnderSchedule,
        ClockAppConvertCantToActivity,
        HideColumnChooser,
        OnlyUserDefinedViews,
        RettificaPausaPranzo,
        HideExportsButtons,
        DefaultGroupViewInCantPage,
        LaunchImportCantVersionEnum,
        LaunchImportColVersionEnum,
        CloneTypeEnum,
        CustomChecksEnum,
        CustomImportFromAccessEnum,
        CustomFlagGpsCantInitEnum,
        NoColorForEditedActivityEnum,
        NoCreateTripsWithoutTabDistRowEnum,
        ExportExcelEnum,
        CalculateTripDataEnum,
        EditManager,
        NoTripsMaxMinDurationControlONLCantEnum,
        RespColDiCantInRaggruppamento1Enum,
        AccessImportTypeEnum,
        CantImportTypeEnum,
        ColImportTypeEnum,
        ImportBlockedRegsEnum,
        ImportFlagEUEnum,
        ShowFlagEUInFormEnum,
        UseCorrectionEnum,
        ShowNocturnAndDayTimesheetEnum,
        ShowTimesheetTripHoursSameRowEnum,
        ShowCorrectionOnTimesheetEnum,
        ShowDurationRoundingsheetEnum,
        ShowONLOnTimesheetEnum,
        AutomaticDateTimeView,
        Export56VersionEnum,
        Export56HistoricUseEnum,
        HideDeltaTotalTimehseetEnum,
        HidePlanTotalTimesheetEnum,
        HidelTotalTotalTimehsheetEnum,
        HidelAdditionalTotalTimehsheetEnum,
        HideJustificationTotalTimehsheetEnum,
        HideStrNoctTotalTimehseetEnum,
        HideArrotTotalTimehseetEnum,
        HideReportSectionTimehsheetEnum,
        ShowAutomaticCorrectionButtonEnum,
        AutomaticCorrectionEnum,
        TimesheetExportZeroValueFormatEnum,
        NoCreateTripOnSameMunicipalityUnderKmEnum,
        RegExportToXmlEnum,
        DoNotCoupleIfDayOddsRegsEnum,
        ReportTitleUsingSavedNameEnum,
        CantEditFormTemplateEnum,
        DoNotCoupleIfDaysRegVUnderEnum,
        ErrModuleShowEmptyColEnum,
        EditTypeErrModuleEnum,
        DefaultFunzAuthLevelEnum,
        ShowComboColEnum,
        ColorAllLinesForTripAndActivityEnum,
        ShowSecondsInHoursErrModuleEnum,
        CustomElaborateRegs,
        CustomExportsBackgroundColorEnum,
        JustificationHourIsWorkedHoursEnum,
        TripHourIsWorkedHoursEnum,
        TimesheeetTotalWithoutMonthlyMinutes,
        ManageOrderCodeSimpleExport,
        ShowDescriptionJustTimesheet,
        EnableActivityEvaluationEnum,
        TimesheetRecoveryHoursTypeEnum,
        HideCancelButtonInMultipleEditRegsEnum,
        AutoGeneratePresidiumActivityEnum,
        PreventFutureRegEnum,
        CheckIfRegvAlreadyPresentEnum,
        CustomerOnlyMultipleEditEvaluationEnum,
        CanDescriptionOver50Enum,
        NoArrotInOrdinaryEnum,
        ComapanyNameEnum,
        PauseDeductionOnlyFascia5,
        AutoClosuresEnum,
        CG2Trips,
        AllowZeroDurationRegs,
        SortExportTimesheetSimple,
        PauseDetractionOnlyLongestTrip,
        HideStrTotalTimesheetEnum,
        PassagesInWhereIsIt,
        RequestFiltersOnMapClick,
        TrackingRegsToShow,
        TimesheetSimpleExport,
        ImportGPSOnlyInWorkingRange,
        TrackingConsecutiveRegsInSameCant,
        IncludeNocturnInDayHours,
        FormattingExportTimeSheetSimple,
        InserimentoNumeroInterventi,
        DividePlanDayNight,
        ShowSendChiamateButtons,
        CantClientIdRequired,
        CalcoloImporti,
        DisabilitaCodiceFiscale16Caratteri,
        ColorDeltaFigFisEnum,
        HideLombardaUnusedFieldsRegVEnum,
        SumJustificationsInRegVReport,
        DoNotShowDisabledColCantId,
        CloneSerialNumber,
        AvoidElaborateOnImport,
        CartellinoCorrection,
        DisablePdfExportGrid,
        DisableExcelExportGrid,
        BlockRestoreReg,
        Solaris,
        TimesheetMultiPagedExport,
        Casp,
        ExcludeAwayHoursInExport,
        ScSExportBudget,
        ExportRigthTime,
        UserIsResp,
        CollabNoHours,
        OrderElaborateRegByCant,
        Overtime,
        ExportStr,
        ImportDouble,
        AutoClosuresFirstLast,
        PausaPranzoIsWorkedHoursEnum,
        AutoClosures,
        SubstractPausaPranzo,
        SubCant,
        NoNocturneStr,
        BadTxt,
        CoordinateZero,
        ViewKilometers,
        DoublePushPin,
        ExitDelay,
        DelayAfter,
        DurationTrip,
        NotCalcolatePausaTrip,
        CloseDifferentCant,
        ReperibilitaTotale,
        NotificaRitardo,
        ArrotondamentoPausa,
        RimozionePausaHotel,
        ExportHotelKomplett,
        LimitiDaTurni,
        CopertureSerali,
        TripFigHours,
        RimozionePausaPranzo,
        NoArrotondamentoOreModificate,
        TripOnlyGpsReg,
        ArrotAllRegs,
        NoArrotColAuthorized,
        AutoClosuresXMinuteEnum,
        AllColLimitiByOrario,
        ShowHourNoTimb,
        UseTabDistDuration,
        NocturneOnCartellino,
        PartialTimesheet,
        DetailsMalattiaCartellino,
        UseStartOfDay,
        NotShowModifyRegs,
        ShowActivitiesInRegV,
        MantainCoordinateModifiedRegs,
        LimitiXCol,
        UseEUDurationRounding,
        ShowDurationRoundingTimesheet,
        ShowDelta,
        LimitPausaPranzo,
        AutoClosuresAfterXEnum
    }

    public enum ClockAppsOperationEnum
    {
        Add,
        Update,
        Remove
    }

    //gestisce l'aggiornamento automatico delle date nelle viste delle RegV_M
    public enum AutomaticDateTimeView
    {
        Disable = 0,
        Enable = 1

    }

    //gestisce le varie correzioni automatiche
    public enum AutomaticCorrectionEnum
    {
        None = 0,
        Add2Minute = 1,
        HistoryBased = 2
    }

    /// <summary>
    /// Indica se consentire registrazioni di durata zero
    /// </summary>
    public enum AllowZeroDurationRegs
    {
        Deny = 0,
        Allow = 1
    }

    // raggruppamento Iniziale Vista Default Pagina CANT
    public enum DefaultGroupeViewInCatPageEnum
    {
        None = 0,
        Tipologia_Can = 1
    }

    // Visualizzazione nella descrizione cantiere di più di 50 caratteri
    public enum CanDescriptionOver50Enum
    {
        Disable = 0,
        Enable = 1
    }

    public enum JsonSegnalazioneTypeEnum
    {
        Portatile,
        Fisso
    }


    /// <summary>
    /// Indica di calcolare l'importo totale delle ore nell'export Hour Report
    /// </summary>
    public enum CalcoloImporti
    {
        /// <summary>
        /// Indica di NON calcolare l'importo totale delle ore nell'export Hour Report
        /// </summary>
        NotShow = 0,

        /// <summary>
        /// Indica di calcolare l'importo totale delle ore nell'export Hour Report
        /// </summary>
        Show = 1
    }


    public enum EditManager
    {
        None = 0,
        ServiziItalia = 1

    }


    // check Custom sulle Reg_V
    public enum CustomChecksEnum
    {
        None = 0,
        Mosaico = 1

    }

    // importazione da access
    public enum CustomImportFromAccessEnum
    {
        None = 0,
        Mosaico = 1

    }

    // tipo di Import Custom da Pagina CANT
    public enum LaunchImportCantVersionEnum
    {
        None = 0,
        ImportMosaico = 1,
        ImportDugoni = 2,
        ImportGenerale = 3
    }

    // tipo di Import Custom da Pagina COL
    public enum LaunchImportColVersionEnum
    {
        None = 0,
        ImportUniLabor = 1
    }
    // comportamento durante la Clone di un Record
    public enum CloneTypeEnum
    {
        //Non si clonano Data_Ora_Fis_U/Durata_Fis_HH_S/Cant_Id/Data_Ora_Fig_U
        None = 0,
        FiledToClone = 1

    }

    // inizializzazione del flag gps nella init di un cantiere
    public enum CustomFlagGpsCantInitEnum
    {
        None = 0,
        FlagGPSTo1 = 1
    }

    // mancata colorazione delle ore delle attività modificate
    public enum NoColorForEditedActivityEnum
    {
        Color = 0,
        NoColor = 1
    }

    // non creazione viaggi in caso di mancata riga in tab dist per GIS
    public enum NoCreateTripsWithoutTabDistRowEnum
    {
        Create = 0,
        DoNotCreate = 1
    }

    // tipo di dati da inserire in reg in caso di calcolo viaggio
    public enum CalculateTripDataEnum
    {
        All = 0,
        OnlyKm = 1
    }

    // saltare il controllo di durata minima/massima viaggi che iniziano da un cantiere ONL
    public enum NoTripsMaxMinDurationControlONLCantEnum
    {
        DoControl = 0,
        SkipControl = 1,
        TruncateToMax = 2
    }

    // importazione del campo access responsabile cantiere in raggruppamento1_can
    public enum RespColDiCantInRaggruppamento1Enum
    {
        DoNotImport = 0,
        ImportInRaggruppamento1 = 1
    }

    // tipo di import access
    public enum AccessImportTypeEnum
    {
        Standard = 0,
        Mosaico = 1
    }

    // tipo di importazione anagrafiche cantiere da excel
    public enum CantImportTypeEnum
    {
        NoImport = 0,
        Mosaico = 1,
        Dugoni = 2,
        GeneraleCantiere = 3,
        GeneraleAssistito = 4,
        NoAssociazione = 5,
        Solaris = 6,
        G4 = 7
    }

    // tipo di importazione anagrafiche collaboratori da excel
    public enum ColImportTypeEnum
    {
        NoImport = 0,
        UniLabor = 1,
        Generale = 2
    }

    // gestione del blocco reg in fase di import da txt
    public enum ImportBlockedRegsEnum
    {
        DoNotManage = 0,
        Manage = 1
    }

    // gestione del flag E/U in fase di import
    public enum ImportFlagEUEnum
    {
        Import = 0,
        DoNotImport = 1
    }

    // visualizzazione del flag E/U in form reg
    public enum ShowFlagEUInFormEnum
    {
        DoNotShow = 0,
        Show = 1
    }

    // visualizzazione campi di Lombarda in form reg
    public enum HideLombardaUnusedFieldsRegVEnum
    {
        DoNotHide = 0,
        Hide = 1
    }

    // somma delle motivazioni nel report delle registrazioni per collaboratore
    public enum SumJustificationsInRegVReport
    {
        DoNotSum = 0,
        Sum = 1
    }

    // include col/cant disabilitati nelle combobox id.col/id.cant
    public enum DoNotShowDisabledColCantId
    {
        DoNotShow = 0,
        Show = 1
    }


    // utilizzato per gestire la visualizzazione degli elementi grafici delle rettifiche
    public enum UseCorrectionEnum
    {
        DoNotUse = 0,
        Use = 1
    }

    public enum DayofWeekTabOrario
    {
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
        Sunday = 7
    }

    // utilizzato per definire la visualizzazione o meno delle ore notturne/diurne nel cartellino
    public enum ShowNocturnAndDayTimesheetEnum
    {
        DoNotShow = 0,
        ShowIfPresent = 1
    }

    // utilizzato per la visualizzazione in una singola riga di cartellino dei dati di viaggio
    public enum ShowTimesheetTripHoursSameRowEnum
    {
        TwoRows = 0,
        OneRow = 1
    }

    // utilizzato per la visualizzazione delle rettifiche all'interno del cartellino
    public enum ShowCorrectionOnTimesheetEnum
    {
        DoNotShow = 0,
        Show = 1
    }

    // utilizzato per la visualizzazione delle rettifiche all'interno del cartellino
    public enum ShowDurationRoundingsheetEnum
    {
        DoNotShow = 0,
        Show = 1
    }

    //utilizzato per visualizzare la combobox dei collaboratori per l'elaborazione dei viaggi all'interno del ColModule
    public enum ShowComboColEnum
    {
        Disable = 0,
        Enable = 1

    }

    public enum ShowONLOnTimesheetEnum
    {
        DoNotShow = 0,
        Show = 1
    }

    public enum ShowAutomaticCorrectionButtonEnum
    {
        Disable = 0,
        Enable = 1
    }

    /// <summary>
    /// Utilizzato per la definizione del tipo di export 56 da processare
    /// </summary>
    public enum Export56VersionEnum
    {
        Standard = 0,
        Kleo = 1
    }

    /// <summary>
    /// Utilizzato per l'utilizzo dei dati anagrafici storici nell'export 56
    /// </summary>
    public enum Export56HistoricUseEnum
    {
        History,
        Current
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione o meno del group summary del delta nei totali per cartellino
    /// </summary>
    public enum HideDeltaTotalTimehseetEnum
    {
        Show = 0,
        Hide = 1
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione o meno del group summary del piano nei totali per cartellino
    /// </summary>
    public enum HidePlanTotalTimesheetEnum
    {
        Show = 0,
        Hide = 1
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione o meno del group summary del totale nei totali per cartellino
    /// </summary>
    public enum HidelTotalTotalTimehsheetEnum
    {
        Show = 0,
        Hide = 1
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione o meno dei group summary delle colonne aggiuntive nei totali per cartellino
    /// </summary>
    public enum HidelAdditionalTotalTimehsheetEnum
    {
        Show = 0,
        Hide = 1
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione o meno dei group summary delle colonne motivazione nei totali per cartellino
    /// </summary>
    public enum HideJustificationTotalTimehsheetEnum
    {
        Show = 0,
        Hide = 1
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione o meno del group summary delle straordinarie notturne nei totali per cartellino
    /// </summary>
    public enum HideStrNoctTotalTimehseetEnum
    {
        Show = 0,
        Hide = 1
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione o meno del group summary degli arrotondamenti nei totali per cartellino
    /// </summary>
    public enum HideArrotTotalTimehseetEnum
    {
        Show = 0,
        Hide = 1
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione o meno del group summary degli straordinari nei totali per cartellino
    /// </summary>
    public enum HideStrTotalTimesheetEnum
    {
        Show = 0,
        Hide = 1
    }

    /// <summary>
    /// Utilizzato per definire la visualizzazione delle sezioni nel report del cartellino
    /// </summary>
    public enum HideReportSectionTimehsheetEnum
    {
        //Mostra tutte le sezioni nel report del cartellino
        ShowAll = 0,

        //Nasconde la sezione del dettaglio nel report del cartellino
        HideDetail = 1,

        //Nasconde la sezione del riepilogo nel report del cartellino
        HideRiep = 2
    }


    public enum TimesheetExportZeroValueFormatEnum
    {
        Normal = 0,
        EmptyString = 1
    }

    /// <summary>
    /// Utilizzato per definire la generazione dei viaggi nello stesso comune sotto kilometraggio (parametro)
    /// </summary>
    public enum NoCreateTripOnSameMunicipalityUnderKmEnum
    {
        CreateAlways = 0,
        CreateOnlyIfInParam = 1
    }

    /// <summary>
    /// Attivazione e tipo esportazione su xml delle registrazioni
    /// </summary>
    public enum RegExportToXmlEnum
    {
        None = 0,
        Perfetto = 1,
        Scs = 2,
        Orlando = 3
    }

    /// <summary>
    /// Nome della compagnia su cui effettuare l'export
    /// </summary>
    public enum ComapanyNameEnum
    {
        Sogedi = 0,
        Tiseco = 1
    }



    /// <summary>
    /// Indica lo stato della personalizzazione di accoppiamento o meno delle registrazioni dispari
    /// </summary>
    public enum DoNotCoupleIfDayOddsRegsEnum
    {
        Couple = 0,
        DoNotCouple = 1
    }

    /// <summary>
    /// Indica come trattare i titolo visualizzati dei report
    /// </summary>
    public enum ReportTitleUsingSavedNameEnum
    {
        StardardTitle = 0,
        SavedViewTitle = 1
    }

    public enum CantEditFormTemplateEnum
    {
        Standard = 0,
        Mosaico = 1
    }

    /// <summary>
    /// Indica se effettuare l'accoppiamento nei giorni con meno di N (parametro) registrazioni
    /// </summary>
    public enum DoNotCoupleIfDaysRegVUnderEnum
    {
        Standard = 0,
        DoNotCoupleIfUnder = 1
    }

    /// <summary>
    /// Indica se visualizzare o meno nel modulo delle errate i giorni senza timbrature nel periodo selezionato
    /// </summary>
    public enum ErrModuleShowEmptyColEnum
    {
        DoNotShow = 0,
        Show = 1
    }

    /// <summary>
    /// Indica il tipo di modifica da applicare al modulo di gestione delle timbrature errate
    /// </summary>
    public enum EditTypeErrModuleEnum
    {
        BatchEdit = 0,
        Inline = 1
    }

    /// <summary>
    /// Utilizzato per assegnare il livello di default alle funzioni non esplicitamente definite in tab_aut
    /// </summary>
    public enum DefaultFunzAuthLevelEnum
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Eleven = 11,
        Twelve = 12
    }

    /// <summary>
    /// Utilizzato per segnalare la colorazione di tutta la riga in caso di viaggio o attività
    /// </summary>
    public enum ColorAllLinesForTripAndActivityEnum
    {
        StandardColor = 0,
        ColorAllLine = 1
    }

    /// <summary>
    /// Utilizzato per indicare la possibilità o meno di modifica dei secondi sulla gestione delle timbrature multiple ed errate
    /// </summary>
    public enum ShowSecondsInHoursErrModuleEnum
    {
        Standard = 0,
        ShowSeconds = 1
    }

    /// <summary>
    /// Utilizzato per indicare il tipo di elaborazione custom delle registrazioni da applicare
    /// </summary>
    public enum CustomElaborateRegs
    {
        /// <summary>
        /// Elaborazione standard (nessuna eleborazione custom eseguita)
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Elaborazione per il cliente Dugoni (abbinamento automatico timbrature per cantieri I/F e Linea)
        /// </summary>
        Dugoni = 1,

        /// <summary>
        /// Elaborazione per le app he prevedono attività
        /// </summary>
        AppWithActivity = 2
    }

    /// <summary>
    /// Indica se visualizzare o meno i colori di background negli export custom
    /// </summary>
    public enum CustomExportsBackgroundColorEnum
    {
        /// <summary>
        /// Visualizza i colori previsti dal codice
        /// </summary>
        ShowColor = 0,

        /// <summary>
        /// Non visualizzare i colori previsti dal codice
        /// </summary>
        DoNotShowColor = 1
    }

    /// <summary>
    /// Indica se utilizzare o meno le ore con motivazione all'interno delle ore lavorate
    /// </summary>
    public enum JustificationHourIsWorkedHoursEnum
    {

        /// <summary>
        /// Non saranno utilizzate le ore con motivazione all'interno delle ore lavorate
        /// </summary>
        DoNotUse = 0,

        /// <summary>
        /// Saranno utilizzate le ore con motivazione all'interno delle ore lavorate
        /// </summary>
        Use = 1

    }

    /// <summary>
    /// Indica se utilizzare o meno le ore viaggio all'interno delle ore lavorate
    /// </summary>
    public enum TripHourIsWorkedHoursEnum
    {

        /// <summary>
        /// Non saranno utilizzate le ore viaggio all'interno delle ore lavorate
        /// </summary>
        DoNotUse = 0,

        /// <summary>
        /// Saranno utilizzate le ore viaggio all'interno delle ore lavorate
        /// </summary>
        Use = 1

    }

    /// <summary>
    /// Indica se visualizzare nel totale del cartellino anche il monte minuti sommato
    /// </summary>
    public enum TimesheeetTotalWithoutMonthlyMinutes
    {

        /// <summary>
        /// Sarà visualizzato nel totale del cartellino anche il monte minuti sommato
        /// </summary>
        Show = 0,

        /// <summary>
        /// Non sarà visualizzato nel totale del cartellino anche il monte minuti sommato (sarà visualizzato il dato puro)
        /// </summary>
        DoNotShow = 1

    }

    /// <summary>
    /// Visualizzazione del codice commessa nell'export semplice
    /// </summary>
    public enum ManageOrderCodeSimpleExport
    {

        /// <summary>
        /// Non sarà visualizzato il codice commessa nell'export
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Sarà visulizzato il codice commessa
        /// </summary>
        Enabled = 1
    }

    /// <summary>
    /// Indica se visualizzare la descrizione nelle motivazioni del cartellino
    /// </summary>
    public enum ShowDescriptionJustTimesheet
    {
        /// <summary>
        /// Non visualizzare le motivazioni nel cartellino
        /// </summary>
        DoNotShow = 0,

        /// <summary>
        /// Visualiza le motivazioni nel cartellino
        /// </summary>
        Show = 1

    }

    /// <summary>
    /// Indica se è attivata o meno la valutazione delle attività
    /// </summary>
    public enum EnableActivityEvaluationEnum
    {

        /// <summary>
        /// La valutazione delle attività risulta disabilitata
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// La valutazione delle attività risulta abilitata
        /// </summary>
        Enabled = 1

    }

    /// <summary>
    /// Indica il tipo di recupero ore da applicare al cartellino in caso di autorizzazione straordinari
    /// </summary>
    public enum TimesheetRecoveryHoursTypeEnum
    {

        /// <summary>
        /// Non sarà applicato il reucpero nella definizione dell'autorizzazione straordinario
        /// </summary>
        NoHoursRecovery = 0,

        /// <summary>
        /// Sarà applicato il recupero settimanale nella definizione dell'autorizzazione stroardinario
        /// </summary>
        WeeklyHoursRecovery = 1

    }

    /// <summary>
    /// Indica se nascondere o meno il pulsante di annullamento operazione nel modulo di manutenzione delle errate
    /// </summary>
    public enum HideCancelButtonInMultipleEditRegsEnum
    {
        /// <summary>
        /// Il pulsante di annullamento operazione nel modulo di manutenzione delle errate non sarà nascosto
        /// </summary>
        DoNotHide = 0,

        /// <summary>
        /// Il pulsante di annullamento operazione nel modulo di manutenzione delle errate sarà nascosto
        /// </summary>
        Hide = 1
    }

    /// <summary>
    /// Indica se gestire o meno l'autogenerazione delle attività di presidio all'uscita per turni specificati
    /// </summary>
    public enum AutoGeneratePresidiumActivityEnum
    {

        /// <summary>
        /// Le attività di presidio all'uscita per specifici turni non saranno generati
        /// </summary>
        DoNotGenerate = 0,

        /// <summary>
        /// Le attività di presidio all'uscita per specifici turni saranno generati
        /// </summary>
        Generate = 1

    }

    /// <summary>
    /// Indica la possibilità o meno di poter inserire le registrazioni future a date condizioni
    /// </summary>
    public enum PreventFutureRegEnum
    {
        /// <summary>
        /// Permette l'inserimento di registrazioni posteriori alla data indicata
        /// </summary>
        Allow = 0,

        /// <summary>
        /// Vieta l'inserimento di registrazioni posteriori alla data indicata
        /// </summary>
        Prevent = 1
    }

    /// <summary>
    /// All'inserimento massivo di nuove regvs, controlla se queste sono già state inserite
    /// </summary>
    public enum CheckIfRegvAlreadyPresentEnum
    {
        /// <summary>
        /// Salta il controllo
        /// </summary>
        Skip = 0,

        /// <summary>
        /// Fa il controllo
        /// </summary>
        Check = 1
    }

    /// <summary>
    /// Tiene conto anche dei passaggi nel modulo WhereIsIt
    /// </summary>
    public enum PassagesInWhereIsIt
    {
        DoNotUse = 0,
        Use = 1
    }

    /// <summary>
    /// Richiede l'utilizzo del filtro su collaboratore e data per aprire il popup della mappa nel modulo delle timbrature multiple
    /// </summary>
    public enum RequestFiltersOnMapClick
    {
        DoNotRequest = 0,
        Request = 1
    }

    public enum CustomerOnlyMultipleEditEvaluationEnum
    {
        Disabled = 0,

        Enabled = 1
    }

    public enum SettingsEditing
    {
        EditMode = 0,
        BatchModeActive = 1,
        BatchModeEnable = 2
    }

    public enum IsInGroupingEnum
    {
        NotInGruoping = 0,
        IsInGrouping = 1
    }

    public enum IsInOrderingEnum
    {
        NotInOrdering = 0,
        IsInOrdering = 1
    }

    public enum GroupHeaderTableEnum
    {
        NotGroupingInRecord = 0,
        IsGroupingInRecord = 1
    }

    public enum NocturneTypeEnum
    {
        None = 0,
        OverMidnight = 1,
        Duration = 2,
        Disabled = 9
    }
    public enum RoundingMethodEnum
    {
        None,
        Duration,
        StartEnd,
        Disabled = 9
    }

    public enum RegTypeEnum
    {
        None = 0,
        Pass = 1,
        Att = 2,
        Trip = 4,
        Duration = 8,
        RettTimesheet = 9,
        ArrotDur = 10,
        RettTimeSheetManual = 11
    }

    /// <summary>
    /// Tipo del piano: normale (diurno e nottunro assieme), solo diurno o solo notturno.
    /// </summary>
    public enum PlanTypeEnum
    {
        Normal = 0,
        Day = 1,
        Night = 2
    }
    /// <summary>
    /// Indica se l'attività deve essere visualizzata o meno nelle griglie.
    /// </summary>
    public enum ActivityStatusEnum
    {
        NotVisible = 1
    }

    /// <summary>
    /// Enum utilizzato solamente per il recupero dei colori nello scheduler in base al <see cref="RegTypeEnum"/>.
    /// </summary>
    public enum SchedulerEnum
    {
        None = 0,
        Pass = 1,
        Att = 2,
        Trip = 4,
        Rett = 9
    }

    public enum TimeSheetEnum
    {
        TotalRow = 0,
        NegativeCell = 1,
        PositiveCell = 2

    }

    /// <summary>
    /// Enum utilizzato per la colorazione della cella delle ore piano nel cartellino
    /// </summary>
    public enum TimesheetPlanEnum
    {
        Timesheet = 0,
        FreeTimesheet = 1,
        CorrectionTimesheet = 2
    }


    public enum RegModifyTypeEnum
    {
        None = 0,
        Exit_Modified = 1,
        Exit_Manual = 2,
        Entry_Modified = 3,
        Both = 4,
        E_Mod_U_Man = 5,
        Entry_Manual = 6,
        E_Man_U_Mod = 7,
        Manual = 8,
        Deelay = 9

    }

    public enum RegEUEnum
    {
        Entry = 0,
        Exit = 1

    }

    public enum RegColorModifyEnum
    {
        None = 0,
        Modify = 1,
        Manual = 2,

    }

    [Flags]
    public enum RegStateEnum  //FLAG Valori Regsitarzione_Stato_Reg in Tabella Reg
    {
        None = 0,
        Ass = 1,
        ErrMax = 2,
        ErrMin = 4,
        Overlap = 9,
    }

    public enum FlagTripHoursParamEnum // FLag Valori 
    {
        None = 0,
        AllExceptTwo = 1,    //Viaggi Abilitati per Tutti i Col salvo i Col disabilitati            
        OnlyOne = 2,         //Viaggi Abilitati solo x i Col Abilitati
    }

    public enum FlagTripHoursColEnum
    {
        None = 0,
        OnlyOne = 1,
        AllExceptTwo = 2,
    }

    public enum FlagTripHoursColStartEndEnum
    {
        None = 0,
        OnlyOne = 1,
        AllExceptTwo = 2,
    }

    public enum FlagTripTypeEnum
    {
        None = 0,
        SameCant = 1,
    }

    public enum DistTypeEnum
    {
        None,
        Cant,
        Cap,
        Place,
        Zone,
        GIS
    }
    [Flags]
    public enum DomainFilterEnum
    {
        None = 0,
        Resp = 1,
        Fil = 2,
        Both = Fil | Resp,
    }

    public enum DomainEnum
    {
        None,
        View,
        ViewUpdate,
    }

    public enum TripAssignmentTypeEnum
    {
        None,
        Find,
        Calculate,
        CalculateFromHeadquarter
    }

    public enum FlagGPSStatusEnum
    {
        None,
        Geocoded,
        Overlapped,
        UnableToFind,
    }


    public enum ModuleTypeEnum
    {
        None = 0,
        Mosaico = 1
    }

    public enum MothlyHoursEnum
    {
        None,
        Inclusive,
        Exclusive,
    }

    public enum CheckMunicipalityEnum
    {
        None,
        Checked,
    }

    public enum CheckCodFiscEnum
    {
        None,
        Checked,
    }

    public enum CheckIBANEnum
    {
        None,
        Checked,
    }

    public enum FunctionMessageEnum
    {
        General,
        Elaborate,
        ElaborateTrips,
        ElaborateActivities,
        CheckOverlaps,
        AssociateReg,
        Import,
        RouteCalculate,
        Rounding,
        CustomElaboratePre,
        CustomElaboratePost
    }

    public enum ApplicationMessageEnum
    {
        Import,
        Elaborate,
        ElaborateTrips
    }

    public enum GridViewColumnTypeEnum
    {
        GridViewDataTextColumn,             //0 Campi Testo
        GridViewDataCheckColumn,            //1 Campi Check Box
        GridViewDataDateColumn,             //2 Campi Date
        GridViewDataTimeSpanEditColumn,     //3 Campi Time(x)
        GridViewDataComboBoxColumn,         //4 Campi Combo Box
        GridViewDataSpinEditColumn,         //5 Campi Spin Edit (numerici) 
        GridViewDataTimeEditColumn,         //6 Campi Time di Date 
    }

    public enum ResourceTypeEnum
    {
        None,
        Field,
        Error,
        Control,
        TabDecod,
        Menu,
        String,
        ReportLabel,
        Grid

    }
    public enum TypoOfDateIntervalTypeEnum
    {
        Oggi,
        Ieri,
        Settimana_Corrente_Del_Giorno,
        Settimana_Corrente_Fino_Al_Giorno_Incluso,
        Settimana_Corrente_Fino_Al_Giorno_Escluso,
        Settimana_Precedente_Alla_Settimana_Del_Giorno,
        Mese_Corrente_del_Giorno,
        Mese_Corrente_Fino_Al_Giorno_Incluso,
        Mese_Corrente_Fino_Al_Giorno_Escluso,
        Mese_Precedente_Al_Mese_del_Giorno,
        Trimestre_Precedente_Al_Mese_Del_Giorno,
        Semestre_Precedente_Al_Mese_Del_Giorno,
        Anno_Corrente_Del_Giorno,
        Anno_Corrente_Fino_Al_Giorno_Incluso,
        Anno_Corrente_Fino_Al_Giorno_Escluso,
        Anno_Precedente_All_Anno_Del_Giorno,
        Una_Settimana_Dal_Giorno_Incluso,
        Un_Mese_Dal_Giorno_Incluso,
        Un_Anno_Dal_Giorno_Incluso,
        Tre_Mesi_Dal_Giorno_Incluso,
        Sei_Mesi_Dal_Giorno_Incluso
    }

    public enum ColorDeltaFigFisEnum
    {
        /// <summary>
        /// Colorazione del DeltaFigFis non attiva
        /// <summary>
        NotActive = 0,

        /// <summary>
        /// Colorazione del DeltaFigFis attiva
        /// <summary>
        Active = 1
    }

    public enum ImportDBTypeEnum
    {
        mdb,
        txt,
    }


    public enum ImportCantTypeEnum
    {
        MosaicoCSV,
    }

    public enum ImportCantMosaicoTypeEnum
    {
        Pubblico,
        Privato,
    }

    public enum CorrectionTypeEnum
    {
        CorrectionPlus,
        CorrectionMinus
    }

    public enum RoundingTypeEnum
    {
        RoundingPlus,
        RoundingMinus
    }

    public enum PauseDetractionOnlyLongestTrip
    {
        AllTrips,
        OnlyLongest
    }

    /// <summary>
    /// Enum utilizzato per definire il tipo di ricerca delle reg_v nella produzione degli oggetti per il cartellino
    /// </summary>
    public enum RegSearchTypeForTimesheetEnum
    {
        /// <summary>
        /// Ricerca delle registrazioni lavorate, non viaggi e con motivazione vuota, non ONL
        /// </summary>
        WorkedRegs,

        /// <summary>
        /// Ricerca delle registrazioni di tipo viaggio
        /// </summary>
        TripRegs,

        /// <summary>
        /// Ricerca delle registrazioni che hanno una motivazione, non ONL
        /// </summary>
        JustificationRegs,

        /// <summary>
        /// Ricerca delle registrazioni di tipo rettifica automatica
        /// </summary>
        CorrectionRegs,

        /// <summary>
        /// Ricerca delle registrazioni di tipo rettifica manuale
        /// </summary>
        CorrectionRegsManu,

        /// <summary>
        /// Ricerca delle registrazioni di tipo arrotondamentpo per durata
        /// </summary>
        DurationRoundingRegs,

        /// <summary>
        /// Ricerca di tutte le registrazioni di tipo ONL (ore non lavorate)
        /// </summary>
        OnlRegs
    }

    /// <summary>
    /// Enum utilizzato per definire il tipo di inserimento che si sta per fare in excel
    /// </summary>
    public enum ExcelInsertTypeEnum
    {
        /// <summary>
        /// Definisce l'inserimento di un dato (stringa, numero ecc.)
        /// </summary>
        Content,

        /// <summary>
        /// Definisce l'inserimento di una formula
        /// </summary>
        Formula,

        /// <summary>
        /// Definisce l'inserimento di un dato con un number format del tipo [h]:mm;@
        /// </summary>
        HhmmTime,

        /// <summary>
        /// Definisce l'inserimento di un dato con un number format del tipo [h]:mm;@
        /// </summary>
        HhmmssTime
    }

    /// <summary>
    /// Definisce il tipo di selezione primaria impostato su un record della Tab_Excel_Model
    /// </summary>
    public enum ExcelModelSelectionTypeEnum
    {
        /// <summary>
        /// Nessun tipo impostato.
        /// </summary>
        None,

        /// <summary>
        /// Richiesta selezione primaria del collaboratore
        /// </summary>
        Col,

        /// <summary>
        /// Richiesta selezione primaria del cantiere
        /// </summary>
        Cant,

        /// <summary>
        /// Richiesta selezione primaria del cliente
        /// </summary>
        Cli
    }

    /// <summary>
    /// Definisce il tipo di custom elaborate
    /// </summary>
    public enum CustomElaborateRegsTypeEnum
    {
        /// <summary>
        /// L'elaborazione ante l'elaborazione standard delle registrazioni
        /// </summary>
        Pre,

        /// <summary>
        /// L'elaborazione post l'elaborazione dell'associazione pru/fru delle timbrature
        /// </summary>
        PostPruFru,

        /// <summary>
        /// L'elaborazione alla fine delle elaborazione delle regs
        /// </summary>
        PostElaborate

    }

    /// <summary>
    /// Definisce il tipo di calcolo nell'esportazione delle registrazioni
    /// </summary>
    public enum ExportRegVCalculationTypeEnum
    {

        /// <summary>
        /// Il tipo di calcolo che riguarda le ore fisiche
        /// </summary>
        Physical,

        /// <summary>
        /// Il tipo di calcolo che riguarda le ore figurative
        /// </summary>
        Rounded

    }

    /// <summary>
    /// Definisce il tipo di ore da trattare nell'esportazione delle registrazioni
    /// </summary>
    public enum ExportRegVHourTypeEnum
    {

        /// <summary>
        /// Il tipo di calcolo tratta solo la durata delle registrazioni
        /// </summary>
        OnlyDuration,

        /// <summary>
        /// Il tipo di calcolo tratta l'ora di entrata e l'ora di uscita delle registrazioni
        /// </summary>
        Eu,

        /// <summary>
        /// Il tipo di calcolo tratta l'ora di entrata e l'ora di uscita e anche la durata delle registrazioni
        /// </summary>
        Both

    }

    /// <summary>
    /// Definisce i possibilit tipi di gruppi gps trattabili in fase di importazione timbrature
    /// </summary>
    public enum GpsGruopTypeEnum
    {
        /// <summary>
        /// Tipo di gruppo composto da timbratura tag e timbrature GPS
        /// </summary>
        TagAndGps,

        /// <summary>
        /// Tipo di gruppo composto da timbrature solo GPS
        /// </summary>
        OnlyGps,

        TagActivityGps

    }

    /// <summary>
    /// Definisce il tipo di coordinate possibili per una linea di registrazione in fase di importazione timbrature GPS
    /// </summary>
    public enum GpsLineTypeEnum
    {
        /// <summary>
        /// Tipo di linea gps latitudine
        /// </summary>
        Latitude,

        /// <summary>
        /// Tipo di linea gps longitudine
        /// </summary>
        Longitude,

        /// <summary>
        /// Solo associata a tag (no coordinate)
        /// </summary>
        Tag

    }

    /// <summary>
    /// Il tipo di coordinate espresse da una linea di registrazione in fase di importazione timbrature GPS
    /// </summary>
    public enum GpsLineCoordinatesDirectionEnum
    {

        /// <summary>
        /// Latitudine nord
        /// </summary>
        North,

        /// <summary>
        /// Latitudine sud
        /// </summary>
        South,

        /// <summary>
        /// Longitudine est
        /// </summary>
        East,

        /// <summary>
        /// Longitudine ovest
        /// </summary>
        West,

        /// <summary>
        /// Nessun valore (registrazione tag)
        /// </summary>
        None
    }

    /// <summary>
    /// Definisce il tipo di associazione da applicare al tag in caso di importazione timbrature GPS+TAG
    /// </summary>
    public enum GpsAssTagTypeEnum
    {

        /// <summary>
        /// IL tag in una timbratura tag e GPS non rappresenta nulla
        /// </summary>
        None,

        /// <summary>
        /// Il tag in una timbratura tag e GPS rappresenta il collaboratore.
        /// </summary>
        Col,

        /// <summary>
        /// Il tag in una timbratura tag e GPS rappresenta il cantiere.
        /// </summary>
        Cant,

        /// <summary>
        /// Il tag in una timbratura tag e GPS rappresenta il cantiere o il collaboratore a seconda dell'anagrafica di inserimento
        /// </summary>
        CantCol,

    }

    /// <summary>
    /// Indica il tipo di chiusura dal applicare alla gestione delle causali da dispositivo
    /// </summary>
    public enum DeviceActivityClosingTypeEnum
    {
        /// <summary>
        /// Non sarà applicata alcuna chiusura automatica alle causali
        /// </summary>
        NoClosure,

        /// <summary>
        /// Le causali saranno chiuse facendo si che ogni attività rappresenti un blocco E/U (valore di default in caso di configurazione non impostata)
        /// </summary>
        SingleActivty,

        /// <summary>
        /// Le causali saranno chiuse togliendo le registrazioni intermedie (cioè mantenendo una registrazione con tutte le causali)
        /// </summary>
        AllReg
    }

    /// <summary>
    /// Indica come importare le timbrature GPS
    /// </summary>
    public enum ImportGPSRegsEnum
    {
        /// <summary>
        /// Le registrazioni GPS vengono importate normalmente
        /// </summary>
        None,

        /// <summary>
        /// Le registrazioni GPS vengono importate come passaggi
        /// </summary>
        AsPass
    }

    /// <summary>
    /// Definisce lo stato del record di Where is it visualizzato
    /// </summary>
    public enum WhereIsItRecordState
    {

        /// <summary>
        /// Il collaboratore si trova all'interno del cantiere
        /// </summary>
        IsInIt,

        /// <summary>
        /// Il collaboratore si trova all'esterno del cantiere
        /// </summary>
        IsOutOfIt,

        /// <summary>
        /// Il collaboratore non ha regisrazioni su cui effettuare la verifica
        /// </summary>
        NoRegPresent,

        /// <summary>
        /// L'ultima registrazione del collaboratore è un passaggio
        /// </summary>
        LastPass

    }

    /// <summary>
    /// Identifica gli stati di vautazione attività
    /// </summary>
    public enum ActivityEvaluationStateEnum
    {

        /// <summary>
        /// Attività conforme
        /// </summary>
        Compliant = 1,

        /// <summary>
        /// Attività conforme in fase di risoluzione
        /// </summary>
        NotCompliantResolving = 2,

        /// <summary>
        /// Attività non conforme non risolvibile
        /// </summary>
        NoCompliantNotResolvable = 3

    }

    /// <summary>
    /// I tipi di motivazione presenti nel cartellino
    /// </summary>
    public enum JustificationTypeEnum
    {
        /// <summary>
        /// Nessuna motivazione (ore figurative)
        /// </summary>
        None,

        /// <summary>
        /// Motivazione piano
        /// </summary>
        Plan,

        /// <summary>
        /// Con motivazione
        /// </summary>
        Just,
    }

    /// <summary>
    /// Identifica i tipi di informazioni aggiuntive utilizzate dall'applicativo
    /// </summary>
    public enum AdditionalInfoEnum
    {

        /// <summary>
        /// Nessuna informazione aggiuntiva
        /// </summary>
        None,

        /// <summary>
        /// Informazione aggiuntiva di tipo turno
        /// </summary>
        Turn,

        /// <summary>
        /// Informazione aggiuntiva di tipo tipo attività
        /// </summary>
        ActivityType,

        /// <summary>
        /// Informazione aggiuntiva che riguarda il sottocaniere
        /// </summary>
        SubCant,

        /// <summary>
        /// Informazione aggiuntiva della matricola pru che ha timbrato l'attività
        /// </summary>
        PruCodeForActivity,

        /// <summary>
        /// Informazione aggiuntiva della squadra che ha timbrato l'attività
        /// </summary>
        Squadra,

        /// <summary>
        /// Informazione aggiuntiva delle note allegate alla timbratura
        /// </summary>
        Note

    }

    /// <summary>
    /// Indica se gli arrotondamenti vengono contati come ore ordinarie o meno nel cartellino
    /// </summary>
    public enum NoArrotInOrdinaryEnum
    {
        /// <summary>
        /// Indica che gli arrotondamenti vengono contati come ore ordinarie nel cartellino
        /// <summary>
        Disabled = 0,

        /// <summary>
        /// Indica che gli arrotondamenti NON vengono contati come ore ordinarie nel cartellino
        /// <summary>
        Enabled = 1
    }

    /// <summary>
    /// Indica se la registrazione present aun ritardo
    /// </summary>
    public enum DelayEnum
    {
        /// <summary>
        /// Indica che la registrazione presenta un ritardo
        /// <summary>
        Delay = 0,

        /// <summary>
        /// Indica che la registrazione non presenta un ritardo
        /// <summary>
        NoDelay = 1
    }

    /// <summary>
    /// Indica se il DeltaFigFis è positivo, negativo o nullo
    /// </summary>
    public enum DeltaFigFis
    {
        /// <summary>
        /// Indica che il DeltaFigFis è nullo
        /// <summary>
        Zero = 0,

        /// <summary>
        /// Indica che il DeltaFigFis è negativo
        /// <summary>
        Negative = 1,

        /// <summary>
        /// Indica che il DeltaFigFis è positivo
        /// <summary>
        Positive = 2
    }

    /// <summary>
    /// Indica che tipo di autochiusura adottare
    /// </summary>
    public enum AutoClosuresEnum
    {
        /// <summary>
        /// Nessuna auto chiusura delle registrazioni
        /// </summary>
        None = 0,

        /// <summary>
        /// Chiusura delle timbrature sul cantiere sede
        /// </summary>
        Sede = 1
    }

    /// <summary>
    /// Indica se la pausa pranzo viene detratta solo dai viaggi che ricadono in fascia 5
    /// </summary>
    public enum PauseDeductionOnlyFascia5
    {
        /// <summary>
        /// Indica che il controllo non viene effettuato
        /// <summary>
        Disabled = 0,

        /// <summary>
        /// Indica che la pausa pranzo viene detratta solo dai viaggi che ricadono in fascia 5
        /// <summary>
        Enabled = 1
    }

    /// <summary>
    /// Ordina l'export base del cartellino
    /// </summary>
    public enum SortExportTimesheetSimple
    {
        /// <summary>
        /// Non ordina i record (tiene l'ordinamento del db)
        /// <summary>
        NotSorted = 0,

        /// <summary>
        /// Ordina i record per cognome e nome
        /// <summary>
        SurnameName = 1
    }

    /// <summary>
    /// Indica quali registrazioni visulizzare nel modulo Tracking
    /// </summary>
    public enum TrackingRegsToShow
    {
        /// <summary>
        /// Visualizza tutte le registrazioni nel modulo Tracking
        /// <summary>
        AllRegs = 0,

        /// <summary>
        /// Visualizza solo i passaggi nel modulo Tracking
        /// <summary>
        OnlyPass = 1
    }

    /// <summary>
    /// Indica quali cartellini esportare nel export semplice del cartellino
    /// </summary>
    public enum TimesheetSimpleExport
    {
        /// <summary>
        /// Visualizza tutti i cartellini presenti
        /// <summary>
        AllTimesheets = 0,

        /// <summary>
        /// Visualizza solo il cartellino delle figurative
        /// <summary>
        OnlyFigTimesheetFormulaTotal = 1
    }

    /// <summary>
    /// Indica se importare solo le registrazioni GPS che rientrano nel raggio (in metri) impostato nel parametro rispetto al cantiere con Tipo_Cantiere_Can = 'OPERATIVO'
    /// </summary>
    public enum ImportGPSOnlyInWorkingRange
    {
        /// <summary>
        /// Importa le timbrature GPS anche fuori dal raggio
        /// <summary>
        NotActive = 0,

        /// <summary>
        /// Importa solo le timbrature GPS che ricadono nel raggio
        /// <summary>
        Active = 1
    }

    /// <summary>
    /// Indica se disabilitare il controllo del codice fiscale a 16 caratteri
    /// </summary>
    public enum DisabilitaCodiceFiscale16Caratteri
    {
        /// <summary>
        /// Lascia abilitato il controllo
        /// <summary>
        Enabled = 0,

        /// <summary>
        /// Disabilita il controllo
        /// <summary>
        Disabled = 1
    }

    /// <summary>
    /// Indica come utilizzare il limite d'entrata (se valorizzato)
    /// </summary>
    public enum UtilizzoLimiteEntrata
    {
        /// <summary>
        /// Utilizza il limite d'entrata per il limite d'entrata
        /// <summary>
        LimiteEntrata = 0,

        /// <summary>
        /// Utilizza il limite d'entrata per la gestione ritardo
        /// <summary>
        Ritardo = 1,

        /// <summary>
        /// Utilizza il limite d'entrata per il limite d'entrata e la gestione ritardo
        /// <summary>
        LimiteEntrataERitardo = 2
    }

    public enum UtilizzoLimiteUscita
    {
        /// <summary>
        /// Utilizza il limite d'entrata per il limite d'entrata
        /// <summary>
        LimiteUscita = 0
    }

    /// <summary>
    /// Indica come visualizzare nel tracking le registrazioni consecutive all'interno dello stesso cantiere
    /// </summary>
    public enum TrackingConsecutiveRegsInSameCant
    {
        /// <summary>
        /// Visualizza tutte le registrazioni consecutive nello stesso cantiere
        /// <summary>
        ShowAll = 0,

        /// <summary>
        /// Visualizza SOLO la prima e l'ultima registrazione consecutiva nello stesso cantiere
        /// <summary>
        FirstLast = 1,

        /// <summary>
        /// Visualizza un solo pushpin con il range del gruppo nella label (primo e ultimo indice)
        /// <summary>
        ShowRange = 2
    }

    /// <summary>
    /// indica se includere o escludere nelle ore diurne del cartellino le ore notturne
    /// </summary>
    public enum IncludeNocturnInDayHours
    {
        /// <summary>
        /// Non includere le ore notturne nelle ore diurne
        /// </summary>
        NoInclude = 0,

        /// <summary>
        /// Includere nelle ore diurne le ore nottunre
        /// </summary>
        Include = 1

    }

    /// <summary>
    ///Indica i.l tipo di foramttazione sulle celle dell'export excel semplice
    /// </summary>
    public enum FormattingExportTimeSheetSimple
    {
        /// <summary>
        /// Indica di formattare le celle come numero
        /// </summary>
        Number = 0,

        /// <summary>
        /// Indica di formattare le celle come ore
        /// </summary>
        Hour = 1

    }

    /// <summary>
    /// Inserimento all'interno dell'export confronto ore budget del numero di inyterventi
    /// </summary>
    public enum InserimentoNumeroInterventi
    {
        /// <summary>
        /// Escludi il numero di interventi nell'export del confrotno ore budget
        /// </summary>
        Escludi = 0,

        /// <summary>
        /// Includi il numero di interventi nell'export di confronto ore budget
        /// </summary>
        Includi = 1
    }

    /// <summary>
    ///Indica se dividere il piano per notturno e diurno nel cartellino
    /// </summary>
    public enum DividePlanDayNight
    {
        /// <summary>
        /// Indica di visualizzare il piano unico nel cartellino
        /// </summary>
        ShowAsOne = 0,

        /// <summary>
        /// Indica di visualizzare il piano diviso per diurno e notturno nel cartellino
        /// </summary>
        DevideByDayNight = 1

    }

    /// <summary>
    /// Indica se visualizzare i bottoni di invio chiamate nel modulo elaborate
    /// </summary>
    public enum ShowSendChiamateButtons
    {
        /// <summary>
        /// Indica di NON visualizzare i bottoni di invio chiamate nel modulo elaborate
        /// </summary>
        Hide = 0,

        /// <summary>
        /// Indica di visualizzare i bottoni di invio chiamate nel modulo elaborate
        /// </summary>
        Show = 1
    }

    /// <summary>
    /// Indica se il campo Id_Cliente è obbligatorio nell'anagrafica dei cantieri
    /// </summary>
    public enum CantClientIdRequired
    {
        /// <summary>
        /// Indica che il campo Id_Cliente NON è obbligatorio nell'anagrafica dei cantieri
        /// </summary>
        NotRequired = 0,

        /// <summary>
        /// Indica che il campo Id_Cliente è obbligatorio nell'anagrafica dei cantieri
        /// </summary>
        Required = 1
    }

    public enum PowerNfcTransponderType
    {
        /**
         * NFC
         */
        N,

        /**
         * GPS
         */
        G,

        /**
         * QRCODE
         */
        Q,

        /**
         * NFC + GPS
         */
        NG,

        /**
         * QRCODE + GPS
         */
        QG
    }

    public enum OperatorComparer
    {
        Contains,
        StartsWith,
        EndsWith,
        Equals = ExpressionType.Equal,
        GreaterThan = ExpressionType.GreaterThan,
        GreaterThanOrEqual = ExpressionType.GreaterThanOrEqual,
        LessThan = ExpressionType.LessThan,
        LessThanOrEqual = ExpressionType.LessThan,
        NotEqual = ExpressionType.NotEqual,
        NotContains = ExpressionType.Not,

    }

    #endregion
}
