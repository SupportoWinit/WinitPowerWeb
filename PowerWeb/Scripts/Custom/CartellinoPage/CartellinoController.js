//Estensione del tipo Number per emulare il metodo Int.toString
Number.prototype.pad = function (size) {
    var s = String(this);
    var sign = '';
    if (s.charAt(0) == '-') {
        s = s.substring(1);
        sign = '-';
    }
    while (s.length < (size || 2)) { s = "0" + s; }
    return sign + s;
}

//#region INSTANZA WIDGET

var _dateBox;
var _colGrid;
var _motSelectBox;
var _collSelectBox;
var _excelSelectBox;
var _reportSelectBox;
var _motGrid;
var _cartellinoGrid;
var _editableCartellinoGrid;
var _multiView;
var _motPopup;
var _standardCartScroll;
var _editableCartScroll;
var _rettificaRangeSelector;
var _forwardButton;
var _backwardButton;
var _swapEditModeButton;
var _cantSelectBox;
var _excelLoadIndicator;
//#endregion INSTANZA WIDGET

var currentPage = window.location.href.split('/')[window.location.href.split('/').length - 1];

var cartellino = angular.module('cartellino', ['dx']);

/*
Molti widget della pagina del cartellino (selectBox e dataGrid) sono costruiti utilizzando 
il customStore (Devextreme) di cui potete trovare tutte le info e le API sul sito
*/

cartellino.controller("cartellinoController", function ($scope) {


    //console.log(getDisabledColsCustomization());

    //#region STORES SELECTBOXES

    var gridCantSelectBoxsearchTimer;
    $scope.gridCantSelectBoxSource = new DevExpress.data.DataSource({
        store: new DevExpress.data.CustomStore({
            key: "Cant_Id",
            load: dxDropDownBoxLoad,
            byKey: function (e) {
                var d = new jQuery.Deferred();

                d.resolve();

                return d.promise();
            }
        })
    });

    var gridMotivationSelectBoxsearchTimer;
    $scope.gridMotivationSelectBoxSource = new DevExpress.data.DataSource({
        store: new DevExpress.data.CustomStore({
            key: "Tab_Decod_Id",
            load: dxDropDownBoxLoad,
            byKey: function (e) {
                var d = new jQuery.Deferred();

                d.resolve();

                return d.promise();
            }
        })
    });

    //#endregion STORES SELECTBOXES

    //#region PANNELLI CARICAMENTO

    //Il pannello di caricamento dei cartellini
    jQuery("#elaborateCartellinoLoadPanel").dxLoadPanel({
        message: "Elaboro i cartellini..."
    });

    

    //Il pannello di caricamento dell'export
    jQuery("#elaborateExportLoadPanel").dxLoadPanel({
        message: "Genero il file..."
    });

    //Il pannello di generazione delle rettifiche
    jQuery("#generateRettificheLoadPanel").dxLoadPanel({
        message: "Genero le rettifiche..."
    });

    //Il pannello di generazione delle rettifiche
    jQuery("#deleteRettificheLoadPanel").dxLoadPanel({
        message: "Cancello le rettifiche..."
    });

    //#endregion

    //#region STRUTTURE DI SERVIZIO

    //Dizionario motivazioni
    $scope.motivations = getMotivazioni();
    //Dizionario resources
    $scope.resources = getResources();
    //Dizionario lookUp
    $scope.dxLookUp = {
        Cant_Id: {
            Type: "Cant",
            Select: ["Cant_Id", "Codice_Cantiere", "Descrizione_Can"],
            SearchFields: ["Codice_Cantiere", "Descrizione_Can"]
        },
        Tab_Decod_Id: {
            Type: "Tab_Decod",
            Select: ["Tab_Decod_Id", "Chiave_Tab", "Decodifica_Tab"],
            SearchFields: ["Chiave_Tab", "Decodifica_Tab"],
            DefaultFilter: [["Nome_Tab", "=", "MOTIVAZIONI"]]
        }
    }

    //#endregion STRUTTURE DI SERVIZIO

    //#region DATASOURCES

    $scope.cartellini = [];

    //DataSource del popup degli errori
    $scope.errorsPopupDataSource = [];

    //DataSource griglia dei cartellini
    $scope.motivationGridDataSource = [];

    //Recupera le opzione di elaborazione del cartellino da server-side (metodo definito nell'.ascx)
    $scope.printListDataSource = getExports();

    //Datasource selectBox motivazioni
    $scope.motivationsSelectBoxDataSource = [];

    jQuery.each($scope.motivations, function (key, value) {
        if (key != "OL") {
            $scope.motivationsSelectBoxDataSource.push({
                Codice: key,
                Descrizione: value
            });
        }
    });

    //#endregion DATASOURCES

    //#region COLUMNS

    //La lista contenente i le colonne del cartellino
    $scope.cartellinoColumns = [];

    //Lista di colonne griglia collaboratori
    $scope.collColumns = getColColumns();
    //#endregion COLUMNS

    //#region STRUTTURE D'APPOGGIO

    //Indice cartellino visualizzato
    $scope.index = 0;
    //Label cartellino
    $scope.selectedCol;
    //Collaboratore visualizzato (databind griglia cartellino)
    $scope.selectedCartCol = {};
    //Variabile che permette la modifica del cartellino
    $scope.allowUpdating = false;
    //Nuova riga da aggiungere al cartellino
    $scope.newCartRow = {};

    //Variabile contenente l'oggetto summary della griglia
    $scope.summary = {};
    $scope.editGridSummary = {};
    $scope.editGridSummary.calculateCustomSummary = function (options) {
        var value = options.value,
            name = options.name;
        var negative = false;
        if (value != undefined) {
            negative = value.charAt(0) == '-' ? true : false;
        }
        //CustomSummary per le ore
        if (name.startsWith('customSummary')) {

            //Fase 1: inizializzazione del totale
            if (options.summaryProcess == 'start') {
                options.totalValue = "00:00";
            }

            //Fase 2: calcolo del totale
            if (options.summaryProcess == 'calculate') {
                //Divide il totale parziale in ore e minuti

                var totalHours = parseInt(options.totalValue.split(":")[0], 10),
                    totalMinutes = parseInt(options.totalValue.split(":")[1], 10);
                var totMin = ((totalHours < 0 || options.totalValue.startsWith('-') ? -1 : 1) * totalMinutes) + (totalHours * 60);
                var firstChar = "";

                //Se il valore della cella contiene ':' o '.' (quindi se è stata inserita un'ora:minuti valida, validata dalla regex)
                if (value.indexOf(':') !== -1 || value.indexOf('.') !== -1) {
                    let hours = 0,
                        minutes = 0,
                        minHour = 0;
                    //Se l'ora è divisa da ':'
                    if (value.indexOf(':') !== -1) {
                        hours = parseInt(value.split(":")[0], 10);
                        minutes = parseInt(value.split(":")[1], 10);
                    }
                        //Se l'ora è divisa da '.'
                    else {
                        hours = parseInt(value.split(".")[0], 10);
                        minutes = parseInt(value.split(".")[1], 10);
                    }

                    minHour = Math.abs(hours) * 60 + minutes;

                    if (negative) {
                        minHour = -minHour;
                    }

                    totMin += minHour;

                    if (totMin < 0 && totMin > -60) {
                        firstChar = '-';
                    }

                    totalHours = (totMin < 0 ? -1 : 1) * Math.floor(Math.abs(totMin) / 60);
                    totalMinutes = Math.abs(totMin % 60);

                }
                    //Altrimenti è stata inserita solo l'ora (da 0 a 23) senza minuti
                else if (value != "-") {
                    totalHours += parseInt(value, 10);
                }
                else {
                    if (options.totalValue.startsWith("-00")) {
                        firstChar = '-';
                    }
                }

                //Formatta in modo appropriato il totale da visualizzare
                options.totalValue = (firstChar + totalHours.pad(2) + ":" + totalMinutes.pad(2));
            }

            //Fase 3: finalizzazione del totale
            if (options.summaryProcess == 'finalize') {

                if (options.totalValue == "00:00") {
                    options.totalValue = "-";
                }
            }
        }

            //Per la prima colonna scrive 'Totale'
        else if (name == 'summaryType') {
            if (options.summaryProcess == 'start') {
                options.totalValue = "Totale";
            }
            if (options.summaryProcess == 'calculate') {
                options.totalValue = "Totale";
            }
            if (options.summaryProcess == 'finalize') {
                options.totalValue = "Totale";
            }
        }
    }
    //Variabili di salvataggio dei vecchi valori modificati del cartellino
    $scope.oldCartVal;
    $scope.oldCantCartVal;
    $scope.oldEditCartVal;

    //#endregion STRUTTURE D'APPOGGIO

    //#region CONFIGURAZIONE GRIGLIE

    //Opzioni griglia cartellino
    $scope.getGridConfig = function () {
        if ($scope.cartellinoElabOptions.devideByOtherEntity) {

            //Configurazione divisione per cantiere

            //#region  Calcolo summary

            let colCount = $scope.cartellinoColumns.length;

            $scope.totalSummary = [];

            for (var i = 3; i < colCount; i++) {

                let obj = {
                    column: $scope.cartellinoColumns[i].dataField,
                    summaryType: "custom",
                    showInGroupFooter: true,
                    name: "customSummary",
                }

                if ($scope.cartellinoColumns[i].dataField == "TotalDays") {
                    obj.name = "daySum";
                    obj.customizeText = function (data) {
                        return "Totale: " + data.value;
                    }
                }

                $scope.totalSummary.push(obj);
            }

            //#endregion  Calcolo summary

            return {

                allowColumnResizing: true,
                allowGrouping: false,
                columnAutoWidth: true,
                columns: $scope.cartellinoColumns,
                columnResizingMode: "widget",
                customizeColumns: function (columns) {
                    columns[0].width = 200;

                    //Colonna cantiere raggruppata
                    columns[2].groupCellTemplate = function (groupedRow, info) {
                        //Nome del cantiere della riga di raggruppamento
                        jQuery("<div id=\'groupRowText\' style=\"float:left;\">" + info.value + "</div>").appendTo(groupedRow);

                        //Bottone di inserimento motivazioni
                        jQuery("<div id=\"addMotButton\" style=\"float:right;\"></div>").dxButton({
                            icon: 'add',
                            onClick: function (e) {
                                let collapsed = !info.row.isExpanded;
                                if (_motSelectBox.option("value") && $scope.allowUpdating) {
                                    let cant_Id = (collapsed) ? info.row.data.collapsedItems[0].Cant_Id : info.row.data.items[0].Cant_Id;
                                    let existingRowIndex = _cartellinoGrid.getRowIndexByKey({ justification: _motSelectBox.option("value").Chiave_Tab, Cant_Id: cant_Id });

                                    if (existingRowIndex < 0) {
                                        $scope.newCartRow = {
                                            justification: _motSelectBox.option("value").Chiave_Tab,
                                            codice: _motSelectBox.option("value").Decodifica_Tab,
                                            Cant_Id: (collapsed) ? info.row.data.collapsedItems[0].Cant_Id : info.row.data.items[0].Cant_Id,
                                            Cant: (collapsed) ? info.row.data.collapsedItems[0].CantDesc : info.row.data.items[0].CantDesc,
                                            CantDesc: (collapsed) ? info.row.data.collapsedItems[0].CantDesc : info.row.data.items[0].CantDesc,
                                        };

                                        _cartellinoGrid.addRow();
                                        _cartellinoGrid.saveEditData();
                                    } else {
                                        DevExpress.ui.notify("Motivazione già presente", "error", 3000)
                                    }

                                }
                            },
                        }).appendTo(groupedRow);
                    }

                    for (var i = 3; i < columns.length; i++) {
                        columns[i].alignment = "center";
                    }

                },
                dataSource: new DevExpress.data.DataSource({
                    store: new DevExpress.data.CustomStore({
                        key: ["justification", "Cant_Id"],
                        byKey: function (keyArray) {
                            var d = new jQuery.Deferred();
                            d.resolve();
                            return d.promise();
                        },
                        errorHandler: function (error) {
                            console.log(error.message);
                        },
                        insert: function (row) {
                            var d = new jQuery.Deferred();
                            $scope.cartellini[$scope.index].dataSource = $scope.sortCantCartellinoProperties(row);
                            d.resolve();
                            return d.promise();
                        },
                        key: ["justification", "Cant_Id"],
                        load: function (loadOptions) {
                            var d = new jQuery.Deferred();
                            d.resolve($scope.cartellini[$scope.index].dataSource);
                            return d.promise();
                        },
                        onInserting: function (e) {
                            for (var i = 3; i < $scope.cartellinoColumns.length; i++) {
                                e[$scope.cartellinoColumns[i].dataField] = "-";
                            }
                        },
                        update: function (key, values) {
                            var d = new jQuery.Deferred();

                            let tIndex = _cartellinoGrid.getRowIndexByKey(key);

                            let request = {

                                Codice: key.justification,
                                Cant_Id: key.Cant_Id,
                                Col_Id: _collSelectBox.option("selectedItem").ColId,
                                Date: Object.keys(values)[0],
                                Time: Object.values(values)[0],
                                OldTime: $scope.oldCantCartVal,                                                     //Valore durata precedente 
                                OldRowTotal: _cartellinoGrid.cellValue(tIndex, "TotalHours"),

                            }

                            $scope.ajaxPost("SaveCantCartellinoCellUpdate", { parameters: request }, function (a, b, c) { console.log(a); }, function (esito) {
                                let response = JSON.parse(esito.d);
                                if (response.esito == "OK") {
                                    $scope.cartellini[$scope.index].dataSource.forEach((item) => {
                                        if (item.justification == request.Codice && item.Cant_Id == request.Cant_Id) {
                                            item[Object.keys(values)[0]] = Object.values(values)[0];
                                            item.TotalHours = response.newRowMotivTotal != "00:00" ? response.newRowMotivTotal : "-";
                                        }

                                    });

                                    d.resolve(key);

                                } else {

                                    d.reject(response.errors[0]);

                                }

                            })

                            return d.promise();
                        },
                    }),
                }),
                editing: {
                    mode: "cell",
                    allowAdding: false
                },
                grouping: {
                    contextMenuEnabled: true,
                    autoExpandAll: false,
                    expandMode: 'buttonClick',
                    texts: {
                        groupByThisColumn: "Raggruppa per questa colonna",
                        groupContinuedMessage: "Continua dalla pagina precedente",
                        groupContinuesMessage: "Continua nella pagina successiva",
                        ungroup: "Sciogli raggruppamento",
                        ungroupAll: "Sciogli tutti i raggruppamenti"
                    }
                },
                groupPanel: {
                    emptyPanelText: "Trascina qui una colonna per raggruppare",
                    visible: 'auto'
                },
                onInitialized: function (e) {
                    _cartellinoGrid = e.component
                },
                onCellPrepared: function (e) {

                    if (e.rowType == "header") {
                        e.cellElement.addClass("motClass");
                    }

                    if (e.text == "-" || !e.key) {
                        return;
                    }
                    let toAddClass = "motClass";

                    if (e.key.justification == "Delta") {
                        toAddClass = "deltaClass";
                    }
                    if (e.key.justification == "Totale") {
                        toAddClass = "totalClass";
                    }
                    if (e.key.justification == "Rettifiche Manu.") {
                        toAddClass = "mRettClass";
                    }
                    if (e.key.justification == "Rettifiche Auto.") {
                        toAddClass = "autoRettClass";
                    }
                    if (e.key.justification == "OL") {
                        toAddClass = "olClass";
                    }
                    if (e.key.justification == "Ore Piano") {
                        toAddClass = "pianoClass";
                    }

                    e.cellElement.addClass(toAddClass);

                },
                onEditingStart: function (e) {
                    if (e.data.justification == "OL" || e.data.justification == "Rettifiche Auto.") {
                        e.cancel = true;
                    }
                },
                onEditorPreparing: function (e) {
                    let editor = e;
                    e.editorOptions.onKeyDown = function (e) {
                        if (e.jQueryEvent.keyCode == 46 && editor.value != "-") {
                            let ind = _cartellinoGrid.getRowIndexByKey(editor.row.key);
                            _cartellinoGrid.cellValue(ind, editor.dataField, "-");
                            _cartellinoGrid.saveEditData();

                        }

                    }
                },
                onInitNewRow: function (e) {
                    e.data = $scope.newCartRow;
                },
                onRowUpdating: function (e) {
                    $scope.oldCantCartVal = e.oldData[Object.keys(e.newData)[0]];
                },
                onRowValidating: function (e) {

                    var inserted_key = Object.keys(e.newData)[0];
                    var inserted_value = Object.values(e.newData)[0];
                    var negative = false;

                    //Se il dato inserito rispetta la validation rule e vi sono ore lavorate per tale giorno e la somma giornaliera non eccede le 24 H
                    if (e.isValid) {
                        if (inserted_value.match("^-")) {
                            negative = true;

                            inserted_value = inserted_value.slice(1);
                        }
                        if (inserted_value.includes('.')) {
                            inserted_value = inserted_value.replace('.', ':');
                        }
                            //Se è stata inserita solo l'ora (senza minuti), aggiunge 00 minuti alla stringa
                        else {
                            //Se l'ora ha solo una cifra, aggiungo uno 0 
                            if (inserted_value.length == 1) {
                                inserted_value = "0".concat(inserted_value);
                            }

                            //A questo punto, se ho un valore tra 00 e 23, lo trasformo in ore
                            if (inserted_value.localeCompare("00") >= 0 && inserted_value.localeCompare("24") < 0 && !inserted_value.includes(':')) {
                                inserted_value = inserted_value.concat(":00");
                            }
                        }
                        if (negative) {
                            inserted_value = '-' + inserted_value;
                            negative = false;
                        }



                        var index = e.component.getRowIndexByKey(e.key);

                        e.component.cellValue(index, inserted_key, inserted_value);

                    } else {
                        var index = e.component.getRowIndexByKey(e.key);
                        e.component.cellValue(index, inserted_key, "-");
                    }
                },
                paging: {
                    enabled: false
                },
                scrolling: {
                    showScrollbar: 'always'
                },
                showRowLines: true,
                showBorders: true,
                summary: {
                    groupItems: $scope.totalSummary,
                    totalItems: $scope.totalSummary,
                    calculateCustomSummary: $scope.calculateCustomSummary
                },
                width: "auto"

            };
        }
        else {
            //Configurazione divisione per collaboratore
            return {

                allowColumnResizing: true,
                allowGrouping: false,
                columnAutoWidth: true,
                columnResizingMode: "widget",
                columns: $scope.cartellinoColumns,
                customizeColumns: function (columns) {

                    columns[0].width = 130;

                    for (var i = 1; i < columns.length; i++) {
                        columns[i].alignment = "center";
                    }
                },
                dataSource: new DevExpress.data.DataSource({
                    store: new DevExpress.data.CustomStore({
                        errorHandler: function (error) {
                        },
                        load: function (loadOptions) {
                            var d = new jQuery.Deferred();
                            console.log($scope.cartellini[$scope.index]);
                            d.resolve($scope.cartellini[$scope.index].dataSource);
                            return d.promise();
                        },
                        onInserting: function (e) {
                            for (var i = 1; i < $scope.cartellinoColumns.length; i++) {
                                e[$scope.cartellinoColumns[i].dataField] = "-";
                            }
                        },
                        insert: function (row) {
                            var d = new jQuery.Deferred();
                            $scope.cartellini[$scope.index].dataSource = $scope.sortCartellinoProperties(row);
                            d.resolve();
                            return d.promise();
                        },
                        update: function (key, values) {
                            var d = new jQuery.Deferred();

                            //Il selectBox dei cantieri non deve avere un valore solo nel caso di cancellazione
                            if (_cantSelectBox.option("value") != undefined || Object.values(values)[0] == "-") {

                                //Indice numerico delle righe da aggiornare 
                                let dIndex = _cartellinoGrid.getRowIndexByKey("Delta"); //indice riga delta
                                let tIndex = _cartellinoGrid.getRowIndexByKey("Totale"); //indice riga totale
                                let mIndex = _cartellinoGrid.getRowIndexByKey(key);     //indice riga mot modificata


                                let request = {

                                    Codice: key,                                                                    //Codice motivazione
                                    Cant_Id: Object.values(values)[0] != "-" ? _cantSelectBox.option("value").Cant_Id : null,         //Cant_Id
                                    Col_Id: $scope.cartellini[$scope.index].ColId,                                  //Id collaboratore
                                    Date: Object.keys(values)[0],                                     //Data reg
                                    Time: Object.values(values)[0],                                                 //Durata nuova reg
                                    OldTime: $scope.oldCartVal,                                                     //Valore durata precedente 
                                    OldColumnDelta: $scope.cartellinoElabOptions.showDelta ? _cartellinoGrid.cellValue(dIndex, Object.keys(values)[0]) : "-", //Delta corrente non ancora aggiornato
                                    OldRowTotal: _cartellinoGrid.cellValue(tIndex, Object.keys(values)[0]),                    //Totale motivazione corrente non ancora aggiornato
                                    OldRowTotalTotal: _cartellinoGrid.cellValue(tIndex, "TotalHours"),                  //Totale mensile non ancora aggiornato
                                    OldRowDeltaTotal: _cartellinoGrid.cellValue(dIndex, "TotalHours"),                  //Totale delta mensile non ancora aggiornato

                                }

                                console.log(request);


                                $scope.ajaxPost("SaveCartellinoCellUpdate", { parameters: request }, function (a, b, c) { console.log(a); }, function (esito) {
                                    let response = JSON.parse(esito.d);

                                    if (response.esito == "OK") {

                                        //Se tutto va a buon fine aggiorniamo tutti i dati necessari nel datasource 
                                        $scope.cartellini[$scope.index].dataSource.forEach((item) => {
                                            //Motivazione (giorno modificato + delta)
                                            if (item.justification == request.Codice) {
                                                item[Object.keys(values)[0]] = Object.values(values)[0] != "00:00" ? Object.values(values)[0] : "-";
                                                item.Total = response.newRowMotivTotal != "00:00" ? response.newRowMotivTotal : "-";
                                            }
                                            //Riga totale
                                            if (item.codice == "Totale") {
                                                item[Object.keys(values)[0]] = response.newColumnTotal != "00:00" ? response.newColumnTotal : "-";
                                                item.Total = response.newRowTotalTotal != "00:00" ? response.newRowTotalTotal : "-";
                                            }
                                            //Riga delta
                                            if (item.codice == "Delta") {
                                                item[Object.keys(values)[0]] = response.newColumnDelta != "00:00" ? response.newColumnDelta : "-";
                                                item.Total = response.newRowDeltaTotal != "00:00" ? response.newRowDeltaTotal : "-";
                                            }
                                        });

                                        d.resolve();

                                    } else {
                                        d.reject(response.errors[0]);
                                    }

                                })
                            } else {
                                d.reject("E' necessario selezionare un cantiere")
                            }
                            return d.promise();
                        },
                        key: "justification",
                        byKey: function (keyArray) {
                            var d = new jQuery.Deferred();
                            var keys = this.key();
                            $scope.cartellini[$scope.index].dataSource.forEach(function (obj, index) {
                                if (obj[keys] == keyArray) {
                                    d.resolve(obj);
                                    return d.promise();
                                }
                            });
                            d.reject();
                            return d.promise();
                        }
                    }),
                }),
                editing: {
                    mode: "cell",
                    allowAdding: true
                },
                groupPanel: {
                    allowColumnDragging: false,
                    contextMenuEnabled: false,
                    visible: false
                },
                onInitialized: function (e) {
                    _cartellinoGrid = e.component;
                },
                onContentReady: function (e) {
                    //Bind della posizione della scrollView con la scrollView della DataGrid del cartellino editabile
                    if ($scope.cartellinoElabOptions.useEditableCartellino) {
                        _standardCartScroll = _cartellinoGrid.getScrollable();
                        _standardCartScroll.on({
                            "scroll": function (e) {
                                _editableCartScroll.scrollTo(e.component.scrollLeft());
                            }
                        });
                    }
                },
                onCellPrepared: function (e) {

                    if (e.rowType == "header") {
                        e.cellElement.addClass("motClass");
                    }

                    if (e.text == "-" || !e.key) {
                        return;
                    }
                    let toAddClass;
                    if (e.column.dataField == "Totale") {
                        toAddClass = "totalCell";
                    } else if (e.column.cssClass == "weekendCell") {
                        toAddClass = "totalCell";
                    }
                    else {

                        toAddClass = "motClass";

                        if (e.key == "Delta") {
                            toAddClass = "deltaClass";
                        }
                        if (e.key == "Totale") {
                            toAddClass = "totalClass";
                        }
                        if (e.key == "Rettifiche Manu.") {
                            toAddClass = "mRettClass";
                        }
                        if (e.key == "Rettifiche Auto.") {
                            toAddClass = "autoRettClass";
                        }
                        if (e.key == "OL") {
                            toAddClass = "olClass";
                        }
                        if (e.key == "Ore Piano") {
                            toAddClass = "pianoClass";
                        }
                    }
                    e.cellElement.addClass(toAddClass);

                },
                onEditingStart: function (e) {
                    if (e.data.justification == "OL" || e.data.justification == "Rettifiche Auto." || e.data.justification == "Totale" || e.data.justification == "Delta" || e.data.justification == "Ore Piano") {
                        e.cancel = true;
                    }
                },
                onEditorPreparing: function (e) {
                    let editor = e;
                    e.editorOptions.onKeyDown = function (e) {
                        if (e.jQueryEvent.keyCode == 46 && editor.value != "-") {
                            let ind = _cartellinoGrid.getRowIndexByKey(editor.row.key);
                            _cartellinoGrid.cellValue(ind, editor.dataField, "-");
                            _cartellinoGrid.saveEditData();
                        }
                    }
                },
                onInitNewRow: function (e) {
                    e.data = { justification: _motSelectBox.option("value")["Chiave_Tab"], codice: _motSelectBox.option("value")["Decodifica_Tab"] };
                },
                onRowUpdating: function (e) {
                    $scope.oldCartVal = e.oldData[Object.keys(e.newData)[0]];
                },
                onRowValidating: function (e) {
                    var inserted_key = Object.keys(e.newData)[0];
                    var inserted_value = Object.values(e.newData)[0];

                    var negative = false;
                    if (inserted_key == "Codice") {
                        return;
                    }
                    //Se il dato inserito rispetta la validation rule e vi sono ore lavorate per tale giorno e la somma giornaliera non eccede le 24 H
                    if (e.isValid) {

                        if (inserted_value.match("^-")) {
                            negative = true;
                            inserted_value = inserted_value.slice(1);
                        }
                        if (inserted_value.includes('.')) {
                            inserted_value = inserted_value.replace('.', ':');
                        }
                            //Se è stata inserita solo l'ora (senza minuti), aggiunge 00 minuti alla stringa
                        else {
                            //Se l'ora ha solo una cifra, aggiungo uno 0 
                            if (inserted_value.length == 1) {
                                inserted_value = "0".concat(inserted_value);
                            }

                            //A questo punto, se ho un valore tra 00 e 23, lo trasformo in ore
                            if (inserted_value.localeCompare("00") >= 0 && inserted_value.localeCompare("24") < 0 && !inserted_value.includes(':')) {
                                inserted_value = inserted_value.concat(":00");
                            }
                        }
                        if (negative) {
                            inserted_value = '-' + inserted_value;
                            negative = false;
                        }

                        var index = e.component.getRowIndexByKey(e.key);
                        e.component.cellValue(index, inserted_key, inserted_value);

                    } else {
                        var index = e.component.getRowIndexByKey(e.key);
                        e.component.cellValue(index, inserted_key, "-");
                    }
                },
                onToolbarPreparing: function (e) {
                    e.toolbarOptions.items[0].options.disabled = true;
                    e.toolbarOptions.items[0].options.text = "",
                    e.toolbarOptions.items[0].options.onClick = function (e) {
                        if (_motSelectBox.option("value") == null) {
                            return;
                        }
                        if (_cartellinoGrid.getRowIndexByKey(_motSelectBox.option("value").Chiave_Tab) >= 0) {
                            DevExpress.ui.notify("Motivazione già presente!", "error", 3000);
                        } else {
                            _cartellinoGrid.addRow();
                            _cartellinoGrid.saveEditData();
                        }
                    }
                },
                paging: {
                    enabled: false
                },
                scrolling: {
                    showScrollbar: 'always'
                },
                showBorders: true,
                showRowLines: true,
                width: "auto"

            };
        }
    }

    //Opzioni griglia cartellino editabile
    $scope.getEditableGridConfig = function () {
        return {

            allowColumnResizing: true,
            columns: $scope.cartellinoColumns,
            columnAutoWidth: true,
            columnResizingMode: "widget",
            customizeColumns: function (columns) {

                columns[0].width = 130;

                for (var i = 1; i < columns.length; i++) {
                    columns[i].alignment = "center";
                }

            },
            dataSource: new DevExpress.data.DataSource({
                store: new DevExpress.data.CustomStore({
                    mode: "raw",
                    load: function (loadOptions) {
                        var d = new jQuery.Deferred();

                        d.resolve($scope.cartellini[$scope.index].dataSourceEditable);

                        return d.promise();
                    },
                    key: "codice",
                    byKey: function (keyArray) {
                        var d = new jQuery.Deferred();
                        var keys = this.key();
                        $scope.cartellini[$scope.index].dataSource.forEach(function (obj, index) {
                            if (obj[keys] == keyArray) {
                                d.resolve(obj);
                                return d.promise();
                            }
                        });
                        d.reject();
                        return d.promise();
                    }
                }),
            }),
            focusStateEnabled: false,
            onInitialized: function (e) {
                _editableCartellinoGrid = e.component;
            },
            onContentReady: function (e) {
                //Bind della posizione della scrollView con la scrollView della DataGrid del cartellino editabile
                if ($scope.cartellinoElabOptions.useEditableCartellino) {
                    _editableCartScroll = e.component.getScrollable();
                    _editableCartScroll.on({
                        "scroll": function (e) {
                            _standardCartScroll.scrollTo(e.component.scrollLeft());
                        }
                    });
                }
            },
            onCellPrepared: function (e) {

                if (e.rowType == "header") {
                    e.cellElement.addClass("motClass");
                }

                if (e.text == "-" || !e.key) {
                    return;
                }
                let toAddClass;
                if (e.column.dataField == "Totale") {
                    toAddClass = "totalCell";
                } else if (e.column.cssClass == "weekendCell") {
                    toAddClass = "totalCell";
                }
                else {

                    toAddClass = "motClass";

                    if (e.key == "Delta") {
                        toAddClass = "deltaClass";
                    }
                    if (e.key == "Totale") {
                        toAddClass = "totalClass";
                    }
                    if (e.key == "Rettifiche Manu.") {
                        toAddClass = "mRettClass";
                    }
                    if (e.key == "Rettifiche Auto.") {
                        toAddClass = "autoRettClass";
                    }
                    if (e.key == "OL") {
                        toAddClass = "olClass";
                    }
                    if (e.key == "Ore Piano") {
                        toAddClass = "pianoClass";
                    }
                }
                e.cellElement.addClass(toAddClass);

            },
            paging: {
                enabled: false
            },
            scrolling: {
                showScrollbar: 'always'
            },
            showBorders: true,
            showRowLines: true,
            summary: $scope.editGridSummary,
            width: "auto"

        }

    }


    //Le opzioni della griglia dei collaboratori
    $scope.collabDataGridOptions = {
        allowColumnReordering: true,
        allowColumnResizing: true,
        columnAutoWidth: false,
        columnChooser: {
            enabled: false,
            height: 300,
            width: 300,
            emptyPanelText: 'Trascina qui una colonna per nasconderla',
            title: "Scegli colonne"
        },
        columns: $scope.collColumns,
        dataSource: new DevExpress.data.DataSource({
            select: select($scope.collColumns, "Col_Id"),
            store: new DevExpress.data.CustomStore({
                load: function (loadOptions) {
                    var d = new jQuery.Deferred();

                    if (loadOptions.dataField) {

                        let parameters = {
                            dataField: loadOptions.dataField,
                            whereArray: loadOptions.filter || []
                        }

                        $scope.ajaxPost("dxDataGridGetHeaderFilter", parameters, function (a) { console.log(a); }, function (esito) {
                            let response = JSON.parse(esito.d);
                            d.resolve(response);
                        });

                    } else {

                        $scope.ajaxPost("dxDataGridGetColls", { loadOptions: loadOptions }, function (a) { console.log(a); }, function (esito) {
                            let response = JSON.parse(esito.d);
                            d.resolve(response.data, { totalCount: response.totalCount });
                        });

                    }
                    return d.promise();

                },
                key: 'Col_Id'
            })
        }),
        "export": {
            enabled: false,
            fileName: "Collaboratori",
            allowExportSelectedData: true,
            texts: {
                exportAll: "Esporta tutti",
                exportSelectedRows: "Esporta solo selezionati",
                exportTo: "Esporta in Excel"
            }
        },
        filterRow: {
            visible: true,
            applyFilter: "auto",
            betweenEndText: 'Fine',
            betweenStartText: 'Inizio',
            operationDescriptions: { 'equal': 'Uguale a', 'notEqual': 'Diverso da', 'lessThan': 'Minore di', 'lessThanOrEqual': 'Minore o uguale di', 'greaterThan': 'Maggiore di', 'greaterThanOrEqual': 'Maggiore o uguale di', 'startsWith': 'Inizia con', 'notContains': 'Non contiene', 'endsWith': 'Finisce con' },
            resetOperationText: 'Resetta',
            showAllText: "(Mostra tutti)",
        },
        filterSyncEnabled: true,
        filterValue: [["DisAbilitazione_Col", "=", getDisabledColsCustomization()]],
        headerFilter: {
            visible: true,
            allowSearch: true,
            texts: {
                cancel: "Cancella",
                emptyValue: "Vuoto",
                ok:"Ok"
            }
        },
        loadPanel: {
            text: "Carico i collaboratori..."
        },
        noDataText: "Nessun collaboratore disponibile",
        onInitialized: function (e) {
            _colGrid = e.component;
        },
        onEditorPreparing: function (e) {

            if (e.editorName == "dxDateBox") {
                e.editorOptions["displayFormat"] = 'monthAndYear';
                e.editorOptions["maxZoomLevel"] = 'year';
                e.editorOptions["minZoomLevel"] = 'century';
            }
        },
        paging: {
            pageSize: 10
        },
        pager: {
            infoText:'Pagina {0} di {1} ({2} elementi)',
            showPageSizeSelector: true,
            allowedPageSizes: [10, 20, 50, 100, 200],
            showInfo: true
        },
        remoteOperations: {
            filtering: true,
            paging: true,
            sorting: true,
        },
        selection: {
            mode: 'multiple',
            allowSelectAll: true,
            showCheckBoxesMode: 'always'
        },
        showBorders: true,
        sorting: { mode: 'multiple' },
    };

    //#endregion CONFIGURAZIONE GRIGLIE

    //Recupera le opzione di elaborazione del cartellino da server-side (metodo definito nell'.ascx)
    $scope.cartellinoElabOptions = getConfig();

    //#region CONFIGURAZIONE WIDGET

    //#region DATEPICKER

    //Le opzioni del selettore della data
    $scope.datePickerOptions = {
        acceptCustomValue: false,
        dateOutOfRangeMessage: "La data è oltre ai limiti",
        hint: "Scegli un mese",
        placeholder: "Scegli un mese",
        maxZoomLevel: "year",
        minZoomLevel: "decade",
        displayFormat: 'monthAndYear',
        onInitialized: function (e) {
            _dateBox = e.component;
        },
        width: "auto"
    };

    //#endregion DATEPICKER

    //#region SELECTBOX

    //DateBox divisione anagrafiche
    $scope.divisionTypeSelectBoxOptions = {

        dataSource: ["Divisione per collaboratore", "Divisione per cantiere"],
        onInitialized: function (e) {
            if ($scope.cartellinoElabOptions.devideByOtherEntity) {
                e.component.option("value", "Divisione per cantiere");
            } else {
                e.component.option("value", "Divisione per collaboratore");
            }
        },
        onValueChanged: function (e) {
            if (e.value.indexOf("cantiere") != -1) {
                $scope.cartellinoElabOptions.devideByOtherEntity = true;
            }
            if (e.value.indexOf("collaboratore") != -1) {
                $scope.cartellinoElabOptions.devideByOtherEntity = false;
            }
        }

    }

    //Le opzioni della selectbox dei collaboratori per cui sono stati elaborati i cartellini
    $scope.collabSelectBoxOptions = {

        bindingOptions: {
            dataSource: 'cartellini',
            value: 'selectedCartCol'
        },
        displayExpr: function (data) {
            if (!data)
                return undefined;
            return data.ColName + ' ' + data.ColSurname;
        },
        searchEnabled: true,
        onInitialized: function (e) {
            _collSelectBox = e.component;
        },
        onSelectionChanged: function (e) {
            if (e != null && e.selectedItem) {
                $scope.index = e.selectedItem.ID || 0;

                if (_cartellinoGrid)
                    _cartellinoGrid.option("dataSource").reload();

                if (_editableCartellinoGrid) {
                    _editableCartellinoGrid.option("dataSource").reload();
                }
            }
        },
        placeholder: "Selezionare il collaboratore per cui visualizzare il cartellino...",

    }

    //SelectBox cantieri
    $scope.cantSelectBox = {

        acceptCustomValue: true,
        bindingOptions: {
            disabled: '!allowUpdating',
            visible: '!cartellinoElabOptions.devideByOtherEntity'
        },
        contentTemplate: function (e) {
            return jQuery("<div>").dxDataGrid({
                dataSource: $scope.gridCantSelectBoxSource,
                columnAutoWidth: true,
                columns: [{ dataField: "Codice_Cantiere", caption: "Codice" }, { dataField: "Descrizione_Can", caption: "Descrizione" }],
                height: function () {
                    return jQuery(window).height() * 0.4;
                },
                loadPanel: {
                    enabled: false
                },
                onSelectionChanged: function (selectedItems) {
                    _cantSelectBox.option("value", selectedItems.selectedRowsData[0]);
                    _cantSelectBox.option("opened", false);
                },
                paging: { enabled: true, pageSize: 20 },
                selection: { mode: "single" },
                remoteOperations: {
                    paging: true,
                },
                scrolling: {
                    mode: "virtual"
                }
            })
        },
        displayExpr: "Descrizione_Can",
        onInitialized: function (e) {
            _cantSelectBox = e.component;
        },
        onInput: function (e) {
            clearTimeout(gridCantSelectBoxsearchTimer);
            gridCantSelectBoxsearchTimer = setTimeout(function () {
                var text = e.component.option("text");

                $scope.gridCantSelectBoxSource.searchValue(text);
                if (e.component.option("opened") && isSearchIncomplete(e.component)) {
                    $scope.gridCantSelectBoxSource.load();
                } else {
                    e.component.open();
                }
            }, 1000);
        },
        onOpened: function (e) {
            AdjustDropDownWindow(e.element);
            if (isSearchIncomplete(e.component)) {
                $scope.gridCantSelectBoxSource.load();
            }
        },
        onClosed: function (e) {
            var value = e.component.option("value"),
                searchValue = $scope.gridCantSelectBoxSource.searchValue();

            if (isSearchIncomplete(e.component)) {
                e.component.reset();
                e.component.option("value", value);
            }

            if (searchValue) {
                $scope.gridCantSelectBoxSource.searchValue(null);
                $scope.gridCantSelectBoxSource.load();
            }
        },
        openOnFieldClick: false,
        placeholder: "Seleziona cantiere",
        searchEnabled: true,
        showClearButton: true,
        valueExpr: "Cant_Id",

    };

    $scope.motivationSelectBoxOptions = {

        acceptCustomValue: true,
        bindingOptions: {
            disabled: '!allowUpdating',
        },
        contentTemplate: function (e) {
            return jQuery("<div>").dxDataGrid({
                dataSource: $scope.gridMotivationSelectBoxSource,
                columnAutoWidth: true,
                columns: [{ dataField: "Chiave_Tab", caption: "Codice" }, { dataField: "Decodifica_Tab", caption: "Descrizione" }],
                height: function () {
                    return jQuery(window).height() * 0.4;
                },
                loadPanel: {
                    enabled: false
                },
                onSelectionChanged: function (selectedItems) {
                    _motSelectBox.option("value", selectedItems.selectedRowsData[0]);
                    _motSelectBox.option("opened", false);
                },
                paging: { enabled: true, pageSize: 20 },
                selection: { mode: "single" },
                remoteOperations: {
                    paging: true,
                },
                scrolling: {
                    mode: "virtual"
                }
            })
        },
        displayExpr: "Decodifica_Tab",
        onInitialized: function (e) {
            _motSelectBox = e.component;
        },
        onInput: function (e) {
            clearTimeout(gridMotivationSelectBoxsearchTimer);
            gridMotivationSelectBoxsearchTimer = setTimeout(function () {
                var text = e.component.option("text");

                $scope.gridMotivationSelectBoxSource.searchValue(text);
                if (e.component.option("opened") && isSearchIncomplete(e.component)) {
                    $scope.gridMotivationSelectBoxSource.load();
                } else {
                    e.component.open();
                }
            }, 1000);
        },
        onOpened: function (e) {
            AdjustDropDownWindow(e.element);

            if (isSearchIncomplete(e.component)) {
                $scope.gridMotivationSelectBoxSource.load();
            }
        },
        onClosed: function (e) {
            var value = e.component.option("value"),
                searchValue = $scope.gridMotivationSelectBoxSource.searchValue();

            if (isSearchIncomplete(e.component)) {
                e.component.reset();
                e.component.option("value", value);
            }

            if (searchValue) {
                $scope.gridMotivationSelectBoxSource.searchValue(null);
                $scope.gridMotivationSelectBoxSource.load();
            }
        },
        openOnFieldClick: false,
        placeholder: "Seleziona motivazione",
        searchEnabled: true,
        showClearButton: true,
        valueExpr: "Chiave_Tab",

    };

    function AdjustDropDownWindow(editorContainer) {
        var popupInstance = editorContainer.find(".dx-popup").dxPopup("instance");
        popupInstance.option('width', 500);
        popupInstance.off("optionChanged", optionChangedHandler)
        popupInstance.on("optionChanged", optionChangedHandler);
    }

    function optionChangedHandler(args) {
        if (args.name == "width" && args.value < 500) {
            args.component.option("width", 500);
        }
    }

    //#endregion SELECTBOX

    //#region TOOLTIPS

    //Le opzioni del tooltip di help collegato al bottone salvare le preferenze dei cartellini
    $scope.savePreferencesTooltipOptions = {
        target: '#savePreferencesButton',
        position: 'left',
        animation: {
            show: { type: "pop", from: { opacity: 1, scale: 0 }, to: { scale: 1 } },
            hide: { type: "pop", from: { scale: 1 }, to: { scale: 0 } }
        },
        bindingOptions: {
            visible: 'tooltipSavePreferencesVisible'
        }
    }

    //Le opzioni del tooltip delle preferenze
    $scope.preferencesTooltipOptions = {
        target: '#preferencesButton',
        bindingOptions: {
            visible: 'preferencesTooltipVisible'
        },

    }

    //#endregion TOOLTIPS

    //#region POPUPS

    

    //Popover rettifiche automatiche
    $scope.autoRettifichePopoverOptions = {
        target: '#genAutoRettButton',
        contentTemplate: function (contentElement) {

            var popup = this;

            let rLimitMin = parseInt($scope.cartellinoElabOptions.rettificaLimits.split('|')[0]);
            let rLimitMax = parseInt($scope.cartellinoElabOptions.rettificaLimits.split('|')[1]);

            jQuery("<div></div>").dxRangeSelector({
                behavior: {
                    snapToTicks: true
                },
                onInitialized: function (e) {
                    _rettificaRangeSelector = e.component;
                },
                scale: {
                    minorTickInterval: 1,
                    tickInterval: 1,
                    startValue: -60,
                    endValue: 60,
                    label: {
                        format: {
                            type: "fixedpoint"
                        }
                    }
                },
                sliderHandle: {
                    color: 'brown',
                    width: 3,
                    opacity: 1
                },
                size: {
                    height: jQuery(window).height() * 0.15,
                    width: function () {
                        return jQuery(window).width() * 0.5;
                    }
                },
                sliderMarker: {
                    format: {
                        type: "fixedpoint"
                    },
                    customizeText: function (value) {
                        return value.value + " min";
                    }
                },
                title: "Impostazioni rettifiche",
                value: [rLimitMin, rLimitMax]
            }).appendTo(contentElement);

            let row = jQuery("<div class=\'row\'></div>");

            jQuery("<div style=\'display: block; margin: 0 auto;\'></div>").dxButton({
                text: "CONFERMA",
                onClick(e) {
                    //if (_rettificaRangeSelector.getValue()[0] > 0 || _rettificaRangeSelector.getValue()[1] < 0) {
                    //    DevExpress.ui.notify("Range delle rettifiche non valido!", "error", 2000);
                    //    return;
                    //}
                    let data = {
                        minValue: _rettificaRangeSelector.getValue()[0],
                        maxValue: _rettificaRangeSelector.getValue()[1],
                        selectedCollab: _colGrid.getSelectedRowKeys(),
                        selectedPickerDate: _dateBox.option("value")
                    }

                    function successCallBack(esito) {
                        let response = JSON.parse(esito.d);
                        popup.hide();
                        if (response.errors.length > 0) {
                            $scope.errorsPopupDataSource = response.errors;
                        } else {
                            DevExpress.ui.notify("Rettifiche generate con successo!", "success", 3000);
                        }
                    }

                    $scope.ajaxPost("GenerateRangeRettifiche", data, null, successCallBack)
                },
                width: function () {
                    return jQuery(window).width() * 0.1;
                },
            }).appendTo(row);

            row.appendTo(contentElement);

        },
        height: function () {
            return jQuery(window).height() * 0.25;
        },
        width: function () {
            return jQuery(window).width() * 0.5;
        }
    }

    //Popup di stampa excel
    $scope.printExcelPopupOptions = {
        contentTemplate: function (content) {
            var row = jQuery("<div class=\'row\' style=\'display: flex;justify-content: center;align-items: center; \'></div>");

            jQuery("<div class=\'col-md-10\' style=\'margin-left: 2%\'></div>").dxSelectBox({
                dataSource: $scope.printListDataSource,
                displayExpr: "Nome_Risorsa",
                onInitialized: function (e) {
                    _excelSelectBox = e.component;
                },
                valueExpr: "ExcelModel_Id",
            }).appendTo(row);

            row.appendTo(content);

            row = jQuery("<div class=\'row\' style=\'display: flex;justify-content: center;align-items: center; \'></div>");

            jQuery("<div>").dxLoadIndicator({ onInitialized: function (e) { _excelLoadIndicator = e.component; }, visible: false }).appendTo(row);

            row.appendTo(content);
        },
        deferRendering: false,
        showTitle: true,
        shading: false,
        title: "Stampe Excel",
        toolbarItems: [{
            toolbar: 'bottom',
            location: 'after',
            widget: 'dxButton',
            options: {
                onClick(e) {
                    $scope.generateExport(e);
                },
                icon: 'check',
                text: "Stampa"
            }

        }, ],
        height: "auto",
        width: function () {
            return jQuery(window).width() * 0.4;
        }
    }

    //Popup di stampa report
    $scope.printReportPopupOptions = {
        contentTemplate: function (content) {
            var row = jQuery("<div class=\'row\'style=\'display: flex;justify-content: center;align-items: center; \'></div>");

            jQuery("<div class=\'col-md-10\'style=\'margin-left: 2%\'></div>").dxSelectBox({
                dataSource: ["Report Base Cartellino"],
                onInitialized: function (e) {
                    _reportSelectBox = e.component;
                },
                value: "Report Base Cartellino",
                height: "auto",
            }).appendTo(row);



            row.appendTo(content);
        },
        deferRendering: false,
        showTitle: true,
        shading: false,
        title: "Stampe Pdf",
        toolbarItems: [{
            toolbar: 'bottom',
            location: 'after',
            widget: 'dxButton',
            options: {
                onClick(e) {
                    $scope.generateReport(e);
                },
                icon: 'check',
                text: "Stampa"
            }
        }],
        height: "auto",
        width: function () {
            return jQuery(window).width() * 0.4;
        }
    }



    //Popup errori
    $scope.errorsPopupOptions = {
        contentTemplate: function (content) {
            jQuery("<div id=\"errorGrid\"><div>").dxDataGrid({
                columns: ["Messaggio"],
                dataSource: $scope.errorsPopupDataSource,
                height: "auto",
                width: "auto",
                scrolling: {
                    mode: "standard",
                    showScrollbar: "always"
                },
                wordWrapEnabled: true
            }).appendTo(content);
        },
        showTitle: true,
        title: "Attenzione!",
        height: "50%",
        visible: false,
        width: "50%"
    }

    //Popup funzioni
    $scope.functionsPopupOptions = {
        contentTemplate: function (content) {
            console.log($scope.cartellinoElabOptions.showRettifiche);
            jQuery("<div id='form'>").dxForm({
                items: [
                    {
                        itemType: 'tabbed',
                        tabs: [
                            {

                                //#region TAB INFO

                                title: 'Info',
                                colCount: 1,
                                template: function (data, index, item) {

                                    jQuery("<div class=\'dx-fieldset-header\'>Cosa visualizzare?</div>").appendTo(item);

                                    jQuery("<div>").dxForm({
                                        colCount: 2,
                                        items: [
                                        {
                                            template: function (data, itemElement) {
                                                jQuery("<div id=\'showPiano\'></div>").dxCheckBox($scope.showPianoCheckboxOptions).appendTo(itemElement);
                                            }
                                        }, {
                                            template: function (data, itemElement) {
                                                jQuery("<div id=\'showOre\'></div>").dxCheckBox($scope.showOreCheckboxOptions).appendTo(itemElement);
                                            }
                                        }, {
                                            template: function (data, itemElement) {
                                                jQuery("<div id=\'showJust\'></div>").dxCheckBox($scope.showJustCheckboxOptions).appendTo(itemElement);
                                            }
                                        }, {
                                            template: function (data, itemElement) {
                                                jQuery("<div id=\'showDelta\'></div>").dxCheckBox($scope.showDeltaCheckboxOptions).appendTo(itemElement);
                                            }
                                        }, {
                                            template: function (data, itemElement) {
                                                jQuery("<div id=\'showViaggi\'></div>").dxCheckBox($scope.showViaggiCheckboxOptions).appendTo(itemElement);
                                            }
                                        }, {
                                            template: function (data, itemElement) {
                                                jQuery("<div id=\'showTotale\'></div>").dxCheckBox($scope.showTotaleCheckboxOptions).appendTo(itemElement);
                                            }
                                        }, {
                                            template: function (data, itemElement) {
                                                jQuery("<div id=\'showWeeklyTotals\'></div>").dxCheckBox($scope.showWeeklyTotalsCheckboxOptions).appendTo(itemElement);
                                            }
                                        }, {
                                            itemType: "empty"
                                        },
                                        ]
                                    }).appendTo(item);



                                    jQuery("<div class=\'dx-fieldset-header\'>Come visualizzare?</div>").appendTo(item);

                                    jQuery("<div>").dxForm({
                                        colCount: 2,
                                        items: [
                                            {
                                                template: function (data, itemElement) {
                                                    jQuery("<div id=\'devidePlan\'></div>").dxCheckBox($scope.devidePlanSwitchOptions).appendTo(itemElement);
                                                }
                                            }, {
                                                template: function (data, itemElement) {
                                                    jQuery("<div id=\'totalFirstColumn\'></div>").dxCheckBox($scope.totalFirstColumnSwitchOptions).appendTo(itemElement);
                                                }
                                            }
                                        ]
                                    }).appendTo(item);

                                },

                                //#endregion TAB INFO

                            },
                            {

                                //#region TAB FUNZIONI

                                title: 'Funzioni',
                                colCount: 1,
                                items: [{
                                    template: function (data, itemElement) {

                                        var row = jQuery("<div class=\'row\' style=\'padding:10px;border-radius: 25px;\'></div>");


                                        jQuery('<div class=\'col-md-6\'>Genera rettifiche </div>').appendTo(row);
                                        //Bottone
                                        jQuery('<div class=\'col-md-6\' style=\'float:right;margin-right:20px\'></div>').dxButton({
                                            onClick(e) {
                                                if ($scope.validateCallback()) {
                                                    $scope.generateAutoRettifiche();
                                                }
                                            },
                                            text: "Genera",
                                            width: "auto"
                                        }).appendTo(row);
                                        row.appendTo(itemElement);
                                    }
                                }, {
                                    template: function (data, itemElement) {

                                        var row = jQuery("<div class=\'row\' style=\'padding:10px;border-radius: 25px;\'></div>");


                                        jQuery('<div class=\'col-md-6\'>Elimina rettifiche </div>').appendTo(row);
                                        //Bottone
                                        jQuery('<div class=\'col-md-6\' style=\'float:right;margin-right:20px\'></div>').dxButton({
                                            onClick(e) {
                                                if ($scope.validateCallback()) {
                                                    $scope.deleteRettificheManuali();
                                                }
                                            },
                                            text: "Elimina",
                                            width: "auto"
                                        }).appendTo(row);
                                        row.appendTo(itemElement);
                                    }
                                }, {

                                    template: $scope.cartellinoElabOptions.showRettifiche && function (data, itemElement) {
                                        
                                        var row = jQuery("<div class=\'row\' style=\'padding:10px;border-radius: 25px;\'></div>");

                                        //Label 
                                        jQuery('<div class=\'col-md-6\'>Generazione rettifiche parametriche </div>').appendTo(row);

                                        //Bottone
                                        jQuery('<div class=\'col-md-6\' id=\'genAutoRettButton\' style=\'float:right;margin-right:20px\'></div>').dxButton({
                                            onClick(e) {
                                                if ($scope.validateCallback()) {
                                                    jQuery("#autoRettifichePopover").dxPopover("instance").show();
                                                }
                                            },
                                            text: "Genera",
                                            width: "auto"
                                        }).appendTo(row);
                                        row.appendTo(itemElement);
                                    }

                                }]

                                //#endregion TAB FUNZIONI

                            }
                        ]
                    }
                ],
                scrollingEnabled: true,
                height: "100%"
            }).appendTo(content);
        },
        showTitle: true,
        shading: false,
        title: "Funzioni",
        toolbarItems: [
        {
            toolbar: 'bottom',
            location: 'before',
            widget: 'dxButton',
            options: {
                text: 'Salva Permanentemente',
                type: 'success',
                onClick(e) {
                    $scope.saveCartellinoPreferences()
                },
                hint: 'Salvando premanentemente le preferenze, saranno riproposte le prossime volte.\nAltrimenti, le modifiche apportate saranno valide solo per questa sessione.'
            },

        }],
        height: function () {
            return jQuery(window).height() * 0.6;
        },
        visible: false,
        width: function () {
            return jQuery(window).width() * 0.5;
        }
    }

    //#endregion POPUPS

    //#region SWITCH

    //Le opzioni dello switch della divisione del piano in diurno/notturno
    $scope.devidePlanSwitchOptions = {
        value: $scope.cartellinoElabOptions.devidePlanByDayNight,
        text: 'Dividi piano per notturno/diurno',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.devidePlanByDayNight = e.value; } }

    }

    //Le opzioni dello switch per la visualizzazione del totale nella prima colonna
    $scope.totalFirstColumnSwitchOptions = {
        value: $scope.cartellinoElabOptions.totalInFirstColumn,
        text: 'Totale in prima colonna',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.totalInFirstColumn = e.value; } }
    }

    //Le opzioni dello switch per la visualizzazione del totale nella prima colonna
    $scope.devideByOtherEntitySwitchOptions = {
        value: $scope.cartellinoElabOptions.devideByOtherEntity,
        hint: "Visualizzare il dettaglio per cantiere?",
        offText: "NO",
        onText: "SI",
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.devideByOtherEntity = e.value; } },
        visible: ($scope.cartellinoElabOptions.devideByOtherEntityAllowed) ? true : false
    }

    //#endregion SWITCH

    //#region CHECKBOX

    //Le opzioni del checkbox per la visualizzazione del piano
    $scope.showPianoCheckboxOptions = {
        value: $scope.cartellinoElabOptions.showPiano,
        text: 'Piano',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.showPiano = e.value; } }
    }

    //Le opzioni del checkbox per la visualizzazione delle ore lavorate
    $scope.showOreCheckboxOptions = {
        value: $scope.cartellinoElabOptions.showOre,
        text: 'Ore lavorate',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.showOre = e.value; } }
    }

    //Le opzioni del checkbox per la visualizzazione delle motivazioni
    $scope.showJustCheckboxOptions = {
        value: $scope.cartellinoElabOptions.showJust,
        text: 'Motivazioni',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.showJust = e.value; } }
    }

    //Le opzioni del checkbox per la visualizzazione del delta
    $scope.showDeltaCheckboxOptions = {
        value: $scope.cartellinoElabOptions.showDelta,
        text: 'Delta',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.showDelta = e.value; } }
    }

    //Le opzioni del checkbox per la visualizzazione del delta
    $scope.showViaggiCheckboxOptions = {
        value: $scope.cartellinoElabOptions.showViaggi,
        text: 'Viaggi',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.showViaggi = e.value; } }
    }

    //Le opzioni del checkbox per la visualizzazione del totale
    $scope.showTotaleCheckboxOptions = {
        value: $scope.cartellinoElabOptions.showTotale,
        text: 'Totale',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.showTotale = e.value; } }
    }

    //Le opzioni del checkbox per la visualizzazione dei totali settimanali
    $scope.showWeeklyTotalsCheckboxOptions = {
        value: $scope.cartellinoElabOptions.showWeeklyTotals,
        text: 'Totali Settimanali',
        onValueChanged: function (e) { if (e != null) { $scope.cartellinoElabOptions.showWeeklyTotals = e.value; } }
    }

    //#endregion CHECKBOX

    //#region BUTTONS

    

    //Bottone abilita modifiche cartellino
    $scope.swapEditModeButtonOptions = {
        hint: "Abilita la modalità modifica \ndel cartellino",
        icon: 'close',
        onClick: function (e) {
            if (!$scope.allowUpdating) {
                e.component.option("icon", "check");
            } else {
                e.component.option("icon", "close");
            }
            $scope.allowUpdating = !$scope.allowUpdating;
            _cartellinoGrid.option("editing.allowUpdating", $scope.allowUpdating);

            if (!$scope.cartellinoElabOptions.devideByOtherEntity) {
                jQuery(".dx-datagrid-toolbar-button").dxButton("instance").option("disabled", !$scope.allowUpdating);
            }



        },
        onInitialized: function (e) {
            _swapEditModeButton = e.component;
        },
        text: 'Edit',
        visible: $scope.cartellinoElabOptions.editableMode
    };

    //Bottoni movimento cartellini
    $scope.backwardCartellinoButtonOptions = {
        text: '<<',
        onClick: function (e) {

            if ($scope.cartellini != undefined) {
                if ($scope.index == 0) {
                    $scope.index = $scope.cartellini.length - 1;
                }
                else {
                    $scope.index--;
                }
               
                $scope.selectedCartCol = $scope.cartellini[$scope.index];
                _cartellinoGrid.getDataSource().reload();
            }
        },
        onInitialized: function (e) {
            _forwardButton = e.component;
        }
    }

    $scope.forwardCartellinoButtonOptions = {
        text: '>>',
        onClick: function (e) {
            
            if ($scope.cartellini != undefined) {
                if ($scope.index == $scope.cartellini.length - 1) {
                    $scope.index = 0;
                }
                else {
                    $scope.index++;
                }
            }
            $scope.selectedCartCol = $scope.cartellini[$scope.index];
            _cartellinoGrid.getDataSource().reload();
        },
        onInitialized: function (e) {
            _backwardButton = e.component;
        }
    }

    //#endregion BUTTONS

    //#endregion CONFIGURAZIONE WIDGET

    //#region FUNZIONI

    //#region FUNZIONI RENDERING GRIGLIE

    //Funzione per l'inizializzazione della multiview una volta elaborati i vari cartellini
    $scope.initializeGrids = function () {
        jQuery("#cartellinoGrid").dxDataGrid($scope.getGridConfig()).appendTo("#cartellino");

        if ($scope.cartellinoElabOptions.useEditableCartellino && !$scope.cartellinoElabOptions.devideByOtherEntity) {
            jQuery("#editableCartellinoGrid").dxDataGrid($scope.getEditableGridConfig()).appendTo("#cartellinoEditabile");
        }
    }

    //#endregion RENDERING GRIGLIE

    //#region FUNZIONI POPUP

    //Mostra il popup delle funzioni
    $scope.showFunctionsPopup = function () {
        if (jQuery("#functionsPopup").dxPopup("instance").option("visible")) {
            jQuery("#functionsPopup").dxPopup("instance").hide();
        } else {
            jQuery("#functionsPopup").dxPopup("instance").show();
        }

    }

    //Mostra il popup degli errori
    $scope.showErrorsPopup = function () {
        jQuery("#errorsPopup").dxPopup("instance").show();
    }

    //Mostra il popup stampa excel
    $scope.showExcelPrintPopup = function () {
        if (jQuery("#excelPrintPopup").dxPopup("instance").option("visible")) {
            jQuery("#excelPrintPopup").dxPopup("instance").hide();
        } else {
            jQuery("#excelPrintPopup").dxPopup("instance").show();
        }
    }

    //Mostra il popup stampa report
    $scope.showReportPrintPopup = function () {
        if (jQuery("#reportPrintPopup").dxPopup("instance").option("visible")) {
            jQuery("#reportPrintPopup").dxPopup("instance").hide();
        } else {
            jQuery("#reportPrintPopup").dxPopup("instance").show();
        }

    }

    //#endregion FUNZIONI POPUP

    //#region FUNZIONI DI GENERAZIONE AJAX

    

    //Mostra/nasconde gli elementi grafici e passa i dati al server per il calcolo del cartellino
    $scope.generateCartellino = function (e) {

        //Mostra il pannello di caricamento

        if ($scope.validateCallback()) {

            jQuery('#elaborateCartellinoLoadPanel').dxLoadPanel("instance").show();

            //Dati necessari alla generazione del cartellino
            let data = {
                cartellinoOptions: $scope.cartellinoElabOptions,
                selectedPickerDate: _dateBox.option("value"),
                selectedCollab: _colGrid.getSelectedRowKeys()
            };

            function successCallBack(data) {
                var result = JSON.parse(data.d);

                if (result.errorMessage)
                    DevExpress.ui.notify(result.errorMessage, "error", 5000);
                else {

                    $scope.cartellini = result.dataSource;
                    $scope.cartellinoColumns = result.columns;

                    jQuery("#header").fadeOut(1000);
                    jQuery("#full-col-grid-container").fadeOut(1000);
                    jQuery('#bottoniListaCollaboratori').fadeOut(1000, function () {
                        $scope.showCartellini();
                    });

                    //Aggiornamento data di elaborazione 
                    _colGrid.option("dataSource").reload();

                    //Imposta il valore della combobox di selezione dei collaboratori al primo collaboratore della lista
                    if ($scope.cartellini)
                        _collSelectBox.option('value', findJSONElement($scope.cartellini, 'ID', 0));


                    //Se ho meno di 2 collaboratori selezionati, disabilito i tasti per muoversi tra i cartellini
                    if (_colGrid.getSelectedRowKeys().length < 2) {
                        _backwardButton.option('disabled', true);
                        _forwardButton.option('disabled', true);
                    }
                        //Altrimenti li abilito (potrebbero essere stati disabilitati precedentemente)
                    else {
                        _backwardButton.option('disabled', false);
                        _forwardButton.option('disabled', false);
                    }
                }

                jQuery('#elaborateCartellinoLoadPanel').dxLoadPanel("instance").hide();

            }

            $scope.ajaxPost("ElaborateCartellino", data, null, successCallBack);

        }
    }

    //Mostra il loadingPanel e chiama il callback per la cancellazione delle rettifiche
    $scope.deleteRettificheManuali = function (e) {
        let url = "DeleteRettificheManuali";

        let data = {
            selectedCols: _colGrid.getSelectedRowKeys(),
            selectedMonth: _dateBox.option("value").getMonth() + 1 + "/" + _dateBox.option("value").getFullYear(),
        };

        function successCallBack(result) {
            let response = JSON.parse(result.d);
            if (response.errors) {
                $scope.errorsPopupDataSource = response.errors;
                $scope.showErrorsPopup();
            } else {
                DevExpress.ui.notify("Operazione completata con successo!", "success", 3000);
            }
        }

        $scope.ajaxPost(url, data, function (a, b, c) { console.log(a); console.log(b); console.log(c); }, successCallBack);
    }

    //Elimina le ore di straordinari dei collaboratori selezionati (la motivazione di straordinario proviene dalle customization)
    $scope.deleteStr = function () {
        let url = "DeleteStr";
        let data = {
            selectedCols: _colGrid.getSelectedRowKeys(),
            selectedMonth: _dateBox.option("value").getMonth() + 1 + "/" + _dateBox.option("value").getFullYear(),
        };

        function successCallBack(result) {
            let response = JSON.parse(result.d);
            if (response.errors) {
                $scope.errorsPopupDataSource = response.errors;
                $scope.showErrorsPopup();
            } else {
                DevExpress.ui.notify("Operazione completata con successo!", "success", 3000);
            }
        }

        $scope.ajaxPost(url, data, function (a, b, c) { console.log(a); console.log(b); console.log(c); }, successCallBack);
    }

    //Elimina le ore di permesso dei collaboratori selezionati (la motivazione di permesso proviene dalle customization)
    $scope.deletePrm = function () {

        let url = "DeletePrm";
        let data = {
            selectedCols: _colGrid.getSelectedRowKeys(),
            selectedMonth: _dateBox.option("value").getMonth() + 1 + "/" + _dateBox.option("value").getFullYear(),
        };

        function successCallBack(result) {
            let response = JSON.parse(result.d);
            if (response.errors) {
                $scope.errorsPopupDataSource = response.errors;
                $scope.showErrorsPopup();
            } else {
                DevExpress.ui.notify("Operazione completata con successo!", "success", 3000);
            }
        }

        $scope.ajaxPost(url, data, function (a, b, c) { console.log(a); console.log(b); console.log(c); }, successCallBack);
    }

    //Generazione delle rettifiche automatiche da orario o parametro
    $scope.generateAutoRettifiche = function () {

        jQuery('#generateRettificheLoadPanel').dxLoadPanel("instance").show();

        let data = {
            selectedCollab: _colGrid.getSelectedRowKeys(),
            period: _dateBox.option('value')
        };

        function successCallBack(esito) {
            jQuery('#generateRettificheLoadPanel').dxLoadPanel("instance").hide();
            var response = JSON.parse(esito.d);
            if (response.errors.length > 0) {
                $scope.errorsPopupDataSource = response.errors;
                jQuery("#errorsPopup").dxPopup("instance").show();
            } else {
                DevExpress.ui.notify("Operazione completata", "success", 3000);
            }
        };
        $scope.ajaxPost("GenerateAutoRettifiche", data, null, successCallBack);

    }

    //Salva le preferenze del cartellino
    $scope.saveCartellinoPreferences = function () {

        let data = {
            parameters: $scope.cartellinoElabOptions
        };

        function successCallBack(esito) {
            if (esito.d != "OK") {
                DevExpress.ui.notify("Errore durante il salvataggio delle impostazioni", "warning", 3000);
            } else {
                DevExpress.ui.notify("Salvataggio avvenuto con successo", "success", 3000);
            }
        };

        $scope.ajaxPost("SaveCartellinoPreferences", data, null, successCallBack);

        //$scope.validateCallback("saveCartellinoPreferences");
    }

    //Genera un export di tipo txt/pdf/zip
    $scope.generateExport = function (e) {

        if ($scope.validateCallback() && _excelSelectBox.option("value")) {

            _excelLoadIndicator.option("visible", true);

            let url = "GenerateExport";

            let data = {
                selectedCols: _colGrid.getSelectedRowKeys(),
                period: _dateBox.option("value").getMonth() + 1 + "/" + _dateBox.option("value").getFullYear(),
                excelModelId: _excelSelectBox.option("value"),
            };

            function successCallBack(result) {

                _excelLoadIndicator.option("visible", false);

                let response = JSON.parse(result.d)

                if (response.fatalError) {
                    DevExpress.ui.notify(response.fatalError, "error", 3000);
                    return;
                }

                if (response.errors) {
                    $scope.errorsPopupDataSource = response.errors;
                    jQuery("#errorsPopup").dxPopup("instance").show();
                }
                //Viene ritornato il percorso del file salvato sul server e aperta la finestra di download
                $scope.download(response);

                _colGrid.option("dataSource").reload();


            }

            $scope.ajaxPost(url, data, (x => console.log(x)), successCallBack);


            //}
            //$scope.updateExportDate();
        }
    }

    $scope.download = function (fileInfo) {

        window.location = "Downloader.ashx?" + jQuery.param(fileInfo);

    }

    //Export report pdf
    $scope.generateReport = function () {
        if ($scope.validateCallback()) {

            let data = {
                selectedCols: _colGrid.getSelectedRowKeys(),
                selectedDate: _dateBox.option('value')
            };

            function successCallBack(esito) {
                var response = JSON.parse(esito.d);
                $scope.download(response);
            }

            $scope.ajaxPost("GenerateReport", data, null, successCallBack);

        }
    }

    //#endregion FUNZIONI DI GENERAZIONE AJAX

    //#region FUNZIONI DI SUPPORTO

    $scope.validateCallback = function () {

        let keys = _colGrid.getSelectedRowKeys().length;
        let date = _dateBox.option("value");

        let validation = true;
        let message = "";
        let status = "error";

        //Ne collaboratore ne data
        if (keys == 0 && date == undefined) {

            message = "Nessun collaboratore nè data selezionati!";
            validation = false;

        } //No collaboratore
        else if (keys == 0 && date != undefined) {

            message = "Nessun collaboratore selezionato!";
            validation = false;

        }//No data
        else if (keys > 0 && date == undefined) {

            message = "Nessuna data selezionata!";
            validation = false;
        }

        if (!validation) {
            DevExpress.ui.notify(message, status, 3000);
        }

        return validation;

    }

    //Nasconde il bototne per salvare il cartellino se non necessario
    $scope.hideSaveButton = function (e) {
        let saveCartellinoButton = jQuery(e.itemElement).find("#saveCartellinoButton");
        if (!$scope.cartellinoElabOptions.useEditableCartellino) {
            saveCartellinoButton.css('visibility', 'hidden');
        }
        else {
            saveCartellinoButton.css('visibility', 'visible');
        }
    }

    //Nasconde il bototne per salvare il cartellino se non necessario
    $scope.hideRiportoOrePrecedenti = function (e) {
        let riportoLabel = jQuery(e.itemElement).find("#riporto-label"),
            riportoValue = jQuery(e.itemElement).find("#riporto-value");
        if (!$scope.cartellinoElabOptions.showDelta || !$scope.cartellinoElabOptions.useMonteMinuti) {

            riportoLabel.css('visibility', 'hidden');
            riportoValue.css('visibility', 'hidden');
        }
        else {
            riportoLabel.css('visibility', 'visible');
            riportoValue.css('visibility', 'visible');
        }
    }

    //Mostra/nasconde gli elementi grafici per tornare alla lista dei collaboratori
    $scope.showCollaboratoriGrid = function (e) {
        jQuery('#bottoniListaCollaboratori').fadeIn(200);
        jQuery("#full-col-grid-container").fadeIn(200);
        jQuery("#header").fadeIn(200);
        jQuery('#bottoniCartellino').fadeOut(10);
        jQuery('#colSelectors').fadeOut(10);
        jQuery('#cartellinoContainer').fadeOut(10);

        //Rimozione della griglia dal DOM 
        _cartellinoGrid.dispose();

        if (_editableCartellinoGrid != undefined) {

            _editableCartellinoGrid.dispose();
        }

        //Setto le proprieta di modifica come default
        _swapEditModeButton.option("icon", "close");
        $scope.allowUpdating = false;

    }

    $scope.getCurrentCol = function () {
        if ($scope.cartellini[$scope.index] == undefined) {
            return "";
        }
        return $scope.cartellini[$scope.index].DisplayExpr;
    }

    //Mostra/nasconde opportunamente gli elementi grafici
    $scope.showCartellini = function () {
        jQuery('#colSelectors').fadeIn(2000);
        jQuery('#bottoniCartellino').fadeIn(2000);
        jQuery('#cartellinoContainer').fadeIn(1000, function () {
            $scope.initializeGrids();
        });



        //Nasconde il pannello di caricamento
        jQuery('#elaborateCartellinoLoadPanel').dxLoadPanel("instance").hide();
    }

    //Somma il vettore di "durate"
    $scope.timeArraySum = function (array) {
        let hour = 0;
        let minute = 0;

        let splitTime1;

        jQuery.each(array, function (index, item) {
            splitTime1 = item.split(':');
            hour += parseInt(splitTime1[0]); +parseInt(splitTime1[1]) / 60;
            minute += parseInt(splitTime1[1]) % 60;
        });
        hour = ("0" + hour).slice(-2);
        minute = ("0" + minute).slice(-2);
        return hour + ":" + minute;

    };



    //Metodo che si occupa di calcolare i totali dell'editCartellino
    $scope.calculateCustomSummary = function (options) {
        var value = options.value,
            name = options.name;
        var negative = false;
        if (value && isNaN(value)) {
            negative = value.charAt(0) == '-' ? true : false;
        }
        //CustomSummary per le ore
        if (name.startsWith('customSummary')) {

            //Fase 1: inizializzazione del totale
            if (options.summaryProcess == 'start') {
                options.totalValue = "00:00";
            }

            //Fase 2: calcolo del totale
            if (options.summaryProcess == 'calculate') {
                //Divide il totale parziale in ore e minuti

                var totalHours = parseInt(options.totalValue.split(":")[0], 10),
                    totalMinutes = parseInt(options.totalValue.split(":")[1], 10);
                var totMin = ((totalHours < 0 || options.totalValue.startsWith('-') ? -1 : 1) * totalMinutes) + (totalHours * 60);
                var firstChar = "";

                //Se il valore della cella contiene ':' o '.' (quindi se è stata inserita un'ora:minuti valida, validata dalla regex)
                if (value.indexOf(':') !== -1 || value.indexOf('.') !== -1) {
                    let hours = 0,
                        minutes = 0,
                        minHour = 0;
                    //Se l'ora è divisa da ':'
                    if (value.indexOf(':') !== -1) {
                        hours = parseInt(value.split(":")[0], 10);
                        minutes = parseInt(value.split(":")[1], 10);
                    }
                        //Se l'ora è divisa da '.'
                    else {
                        hours = parseInt(value.split(".")[0], 10);
                        minutes = parseInt(value.split(".")[1], 10);
                    }

                    minHour = Math.abs(hours) * 60 + minutes;

                    if (negative) {
                        minHour = -minHour;
                    }

                    totMin += minHour;

                    if (totMin < 0 && totMin > -60) {
                        firstChar = '-';
                    }

                    totalHours = (totMin < 0 ? -1 : 1) * Math.floor(Math.abs(totMin) / 60);
                    totalMinutes = Math.abs(totMin % 60);

                }
                    //Altrimenti è stata inserita solo l'ora (da 0 a 23) senza minuti
                else if (value != "-") {
                    totalHours += parseInt(value, 10);
                }
                else {
                    if (options.totalValue.startsWith("-00")) {
                        firstChar = '-';
                    }
                }

                //Formatta in modo appropriato il totale da visualizzare
                options.totalValue = (firstChar + totalHours.pad(2) + ":" + totalMinutes.pad(2));
            }

            //Fase 3: finalizzazione del totale
            if (options.summaryProcess == 'finalize') {

                if (options.totalValue == "00:00") {
                    options.totalValue = "-";
                }
            }
        }
        else if (name == 'summaryType') {
            if (options.summaryProcess == 'start') {
                options.totalValue = "Totale";
            }
            if (options.summaryProcess == 'calculate') {
                options.totalValue = "Totale";
            }
            if (options.summaryProcess == 'finalize') {
                options.totalValue = "Totale";
            }
        }
        else if (name == "daySum") {

            if (options.summaryProcess == 'start') {
                options.totalValue = 0;
            }

            if (options.summaryProcess == 'calculate') {
                options.totalValue += value;
            }
        }
    }

    //Configura una chiamata ajax di tipo post
    $scope.ajaxPost = function (url, data, errorCallBack, successCallBack) {
        jQuery.ajax({
            type: "POST",
            url: currentPage + "/" + url,
            data: JSON.stringify(data),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            error: errorCallBack,
            success: successCallBack
        });
    }

    //Dowload file da server
    //$scope.download = function (path, fileName) {
    //    var save = document.createElement('a');
    //    save.href = path;
    //    save.target = '_blank';
    //    save.download = fileName || 'unknown';

    //    var evt = new MouseEvent('click', {
    //        'view': window,
    //        'bubbles': true,
    //        'cancelable': false
    //    });
    //    save.dispatchEvent(evt);

    //    (window.URL || window.webkitURL).revokeObjectURL(save.href);
    //}

    //Apre la pagina della gestione monteminuti
    $scope.manageMonteminuti = function () {
        var url = '/Pages/MonteminutiPage.aspx';
        var win = window.open(url, '_blank');
        win.focus();
    }

    //Apre il manuale del cartellino
    $scope.openHelp = function () {
        let win = window.open('/HelpPages/PAGES/_Cartellino_Page.pdf', '_blank');
        win.focus();
    }

    $scope.saveRettificaLimits = function () {
        let minus, plus, slider;
        slider = jQuery("#rettifica-range-slider").dxRangeSlider('instance');
        minus = slider.option('start');
        plus = slider.option('end');
        rettificaLimits = minus + "|" + plus;
    }

    $scope.hideAllLoadingPanels = function () {
        jQuery('#elaborateCartellinoLoadPanel').dxLoadPanel("instance").hide();
        jQuery('#elaborateExportLoadPanel').dxLoadPanel("instance").hide();
        jQuery('#generateRettificheLoadPanel').dxLoadPanel("instance").hide();
        jQuery('#deleteRettificheLoadPanel').dxLoadPanel("instance").hide();
    }

    //Funzione di ordinamento e inserimento di una nuova riga di motivazione
    $scope.sortCartellinoProperties = function (row) {

        let head = 0;
        let tail = $scope.cartellini[$scope.index].dataSource.length;

        if ($scope.cartellinoElabOptions.showPiano) {
            head += 1;
        }

        if ($scope.cartellinoElabOptions.showOre) {
            head++;
        }

        if ($scope.cartellinoElabOptions.autoRettifiche) {
            tail--;
        }

        if ($scope.cartellinoElabOptions.manualRettifiche) {
            tail--;
        }

        if ($scope.cartellinoElabOptions.showTotale) {
            tail--;
        }

        if ($scope.cartellinoElabOptions.showDelta) {
            tail--;
        }

        //Questo casino permette di riordinare una parte dell'array contentente solo le motivazioni

        let dataHead = $scope.cartellini[$scope.index].dataSource.slice(0, head);
        let dataBody = $scope.cartellini[$scope.index].dataSource.slice(head, tail);
        dataBody.push(row);
        dataBody = dataBody.sort(function compare(a, b) {
            if (a.Codice < b.Codice)
                return -1;
            if (a.Codice > b.Codice)
                return 1;
            return 0;
        });

        let dataTail = $scope.cartellini[$scope.index].dataSource.slice(tail, $scope.cartellini[$scope.index].dataSource.length);
        return dataHead.concat(dataBody).concat(dataTail);

    }

    $scope.sortCantCartellinoProperties = function (row) {

        let otherCantsCart = $scope.cartellini[$scope.index].dataSource.filter(c => c.Cant_Id != row.Cant_Id);

        let currentCantCart = $scope.cartellini[$scope.index].dataSource.filter(c => c.Cant_Id == row.Cant_Id);

        let head = 0;
        let tail = currentCantCart.length


        if ($scope.cartellinoElabOptions.showOre) {
            head++;
        }

        if ($scope.cartellinoElabOptions.autoRettifiche) {
            tail--;
        }

        if ($scope.cartellinoElabOptions.manualRettifiche) {
            tail--;
        }

        let dataHead = currentCantCart.slice(0, head);
        let dataBody = currentCantCart.slice(head, tail);
        dataBody.push(row);



        dataBody = dataBody.sort(function compare(a, b) {
            if (a.Codice < b.Codice)
                return -1;
            if (a.Codice > b.Codice)
                return 1;
            return 0;
        });
        let dataTail = currentCantCart.slice(tail, currentCantCart.length);


        return otherCantsCart.concat(dataHead.concat(dataBody).concat(dataTail));
    }

    //#endregion FUNZIONI DI SUPPORTO

    //#region FUNZIONI TOOLTIP

    
    //Mostra il tooltip del bottone di salvataggio preferenze
    $scope.showSavePreferencesTooltip = function () {
        $scope.tooltipSavePreferencesVisible = true;
    }

    //Nasconde il tooltip del bottone di salvataggio preferenze
    $scope.hideSavePreferencesTooltip = function () {
        $scope.tooltipSavePreferencesVisible = false;
    }

    //Mostra il tooltip del bottone di eliminazione cartellino
    $scope.togglePreferencesTooltip = function () {
        $scope.preferencesTooltipVisible = !$scope.preferencesTooltipVisible;
    }

    //#endregion FUNZIONI TOOLTIP

    //#region FUNZIONI NG-BIND VIEW

    $scope.getCurrentColMonteMin = function () {
        if ($scope.cartellini[$scope.index] == undefined) {
            return "";
        }
        return "Monte ore :  " + $scope.cartellini[$scope.index].riportoOrePrecedenti;
    }

    $scope.getCurrentDate = function () {
        if (_dateBox.option("value") == undefined) {
            return "";
        }

        let date = new Date(_dateBox.option("value"))

        return date.toLocaleString(navigator.language || navigator.userLanguage, { month: "long" }) + " " + date.getFullYear();;
    }

    //#endregion FUNZIONI NG-BIND VIEW

    //#region FUNZIONI GESTIONE WIDGETS

    function select(columns, primaryKey) {
        let properties = columns.filter(c => c.visible == true).map(c => c.dataField);
        properties.unshift(primaryKey);
        return properties;
    }

    //Funzione load comune a tutti i selectBox (con customStore e ajax)
    function dxDropDownBoxLoad(loadOptions) {

        var d = new jQuery.Deferred();
        let type = $scope.dxLookUp[this.key()].Type;
        let select = $scope.dxLookUp[this.key()].Select;
        let searchFields = $scope.dxLookUp[this.key()].SearchFields;
        let defaultFilter = $scope.dxLookUp[this.key()].DefaultFilter ? $scope.dxLookUp[this.key()].DefaultFilter.slice(0) : [];

        let partialFilter = defaultFilter.length ? [defaultFilter] : [];

        let filter = [];

        //Preparazione filtro
        if (loadOptions.searchValue != null) {

            if (partialFilter.length > 0) {
                partialFilter.push("and");
            }

            searchFields.forEach((x, y) => {
                if (filter.length > 0) {
                    filter.push("or");
                }
                filter.push([x, "contains", loadOptions.searchValue]);

            });
            partialFilter.push(filter);
        }


        $scope.ajaxPost("dxComboBoxes", { eType: type, take: loadOptions.take, skip: loadOptions.skip, selectArray: select, whereArray: partialFilter }, (x) => { console.log(x) }, function (esito) {

            let response = JSON.parse(esito.d);
            console.log(response);
            d.resolve(response.data, { totalCount: response.totalCount });
        });

        return d.promise();

    }

    //Funzione che in base alla griglia crea un vettore con i nomi delle colonne visibili
    //per la composizione di una query select
    function getVisibleColumns(grid) {
        let visibleColumns = [];

        let c_length = grid.columnCount();
        for (var i = 0; i < c_length; i++) {
            visibleColumns.push(grid.columnOption(i).dataField);
        }
        return visibleColumns;
    }

    //Funzione che calcola il nuovo totale mensile del cartellino editabile
    function time_total_diff(oldValue, newValue, total) {

        let newHours;
        let newMinutes;

        var totparts = total.split(':');

        var t1parts = oldValue.split(':');
        var t1cm = Number(t1parts[0]) * 60 + Number(t1parts[1]); //Minuti totali vecchio timespan
        var t2parts = newValue.split(':');
        var t2cm = Number(t2parts[0]) * 60 + Number(t2parts[1]); //Minuti totali nuovo timespan

        let tot = Number(totparts[0]) * 60 + Number(totparts[1]);
        if (t2cm <= t1cm) { //se nuovi minuti < vecchi minuti
            newHours = ("0" + Math.floor((tot - (t1cm - t2cm)) / 60)).slice(-2);
            newMinutes = ("0" + Math.floor((tot - (t1cm - t2cm)) % 60)).slice(-2);
        } else {

            newHours = ("0" + Math.floor((tot + (t2cm - t1cm)) / 60)).slice(-2);
            newMinutes = ("0" + Math.floor((tot + (t2cm - t1cm)) % 60)).slice(-2);
        }
        //Evitiamo di ritornare 00:00
        if (newHours == "00" && newMinutes == "00") {
            return "-";
        }

        return (newHours + ':' + newMinutes);
    }

    //Funzione che funge da trigger per la chiamata al server dei selectBox per la ricerca
    function isSearchIncomplete(dropDownBox) {
        var displayValue = dropDownBox.option("displayValue"),
            text = dropDownBox.option("text");

        text = text && text.length && text[0];
        displayValue = displayValue && displayValue.length && displayValue[0];

        return text !== displayValue;
    };

    //#endregion FUNZIONI GESTIONE WIDGETS

    $scope.closeAllPopups = function () {
        jQuery("#errorsPopup").dxPopup("instance").hide();
        jQuery("#excelPrintPopup").dxPopup("instance").hide();
        jQuery("#reportPrintPopup").dxPopup("instance").hide();
        jQuery("#functionsPopup").dxPopup("instance").hide();
        jQuery("#autoRettifichePopover").dxPopup("instance").hide();
    }

    //#endregion FUNZIONI

});


//#region FUNZIONI

//Ritorna l'elemento con una certa proprietà richiesta all'interno del JSON specificato
function findJSONElement(array, propName, propValue) {
    let i = 0;
    for (i; i < array.length; i++) {
        if (array[i][propName] == propValue) {
            return array[i];
        }
    }
    return undefined;
}

//Sostituisce l'elemento con una certa proprietà richiesta all'interno del JSON specificato
function replaceJSONElement(array, propName, propValue, newPropName, newPropValue) {
    for (var i = 0; i < array.length; i++) {
        if (array[i][propName] == propValue) {
            array[i][newPropName] = newPropValue;
        }
    }
}

var hideAllLoadingPanels = function () {
    jQuery('#elaborateCartellinoLoadPanel').dxLoadPanel("instance").hide();
    jQuery('#elaborateExportLoadPanel').dxLoadPanel("instance").hide();
    jQuery('#generateRettificheLoadPanel').dxLoadPanel("instance").hide();
    jQuery('#deleteRettificheLoadPanel').dxLoadPanel("instance").hide();
}


//#endregion FUNZIONI
