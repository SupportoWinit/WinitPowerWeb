
var _gridResources = GetGridResources();

var MAINGRID;

var visibleColumns;

angular.module("segnalazioniModule", ['dx']).controller("segnalazioniController", function ($scope) {

    //#region WIDGETOPTIONS

    //#region BUTTONSOPTIONS

    $scope.importButtonOptions = {
        elementAttr: { "class": "leftButton" },
        onInitialized: function (e) {
            if (IsSegnToImport()) {
                e.element.addClass("blinkingButton");
            }
        },
        onClick: function (e) {

            jQuery.ajax({
                type: "POST",
                url: "SegnalazioniPage.aspx/Import",
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (data) {

                    var response = JSON.parse(data.d);

                    if (!response.filesToImport) {
                        e.element.removeClass("blinkingButton");
                    }

                    MAINGRID.option("dataSource").reload();
                },
                error: function (a, b, c) {
                    console.log(a);
                    console.log(b);
                    console.log(c);
                }
            });

        },
        text: "Import"

    }

    $scope.elabButtonOptions = {
        elementAttr: { "class": "leftButton" },
        onClick: function (e) {

            jQuery("#elaboratePopup").dxPopup("instance").show();

        },
        text: "Elabora"

    }

    $scope.printButtonOptions = {
        elementAttr: { "class": "rightButton" },
        onInitialized: function (e) {

        },
        onClick: function (e) {

            jQuery("#printPopup").dxPopup("instance").show();


        },
        text: "Stampa"
    }

    //#endregion BUTTONSOPTIONS

    $scope.maingridOptions = {
        cacheEnabled: false,
        columnChooser: {
            enabled: false,
            height: 300,
            width: 300,
            emptyPanelText: 'Trascina qui una colonna per nasconderla',
            title: "Scegli colonne"
        },
        columnAutoWidth: true,
        columns: [
            {
                dataField: "Damage_Id",
                dataType: "number",
                showInColumnChooser: false,
                visible: false
            },
            {
                dataField: "Data_Ora_Damage",
                caption: GetLocalizedString("Data_Ora_Damage"),
                dataType: "date",
                format: "dd/MM/yyyy HH:mm"
            },
            {
                caption: GetLocalizedString("Col"),
                allowGrouping: true,
                dataField: "Col",
                dataType: "object",
                calculateDisplayValue: function (rowData) {
                    if (!rowData.Col_Id)
                        return null;
                    return rowData.Col.Codice_Collaboratore + "  " + rowData.Col.Cognome_Col;
                },
                calculateFilterExpression: function (filterValue, selectedFilterOperation) {
                    return ["Col_Id", "=", filterValue];
                },
                calculateGroupValue: "Col.Codice_Collaboratore",
                lookup: {
                    columns: [{ dataField: "Codice_Collaboratore" }, { dataField: "Cognome_Col" }],
                    searchExpr: ["Codice_Collaboratore", "Cognome_Col"],
                    select: ["Col_Id", "Codice_Collaboratore", "Cognome_Col"],
                    valueExpr: "Col_Id",
                    displayExpr: "Cognome_Col"
                },
                select: ["Col_Id", "Col.Codice_Collaboratore", "Col.Cognome_Col"]
            },
            {
                dataField: "Codice_Damage",
                caption: GetLocalizedString("Codice_Damage"),
                dataType: "string"
            },
            {
                caption: "Tipo segnalazione",
                dataField: "Tab_Decod",
                dataType: "object",
                calculateDisplayValue: function (rowData) {
                    if (!rowData.Tab_Decod)
                        return null;
                    return rowData.Tab_Decod.Decodifica_Tab;
                },
                calculateFilterExpression: function (filterValue, selectedFilterOperation) {
                    return ["Tab_Decod.Decodifica_Tab", "contains", filterValue];
                },
                calculateGroupValue: "Tab_Decod.Decodifica_Tab",
                lookup: {
                    columns: [{ dataField: "Decodifica_Tab" }],
                    searchExpr: ["Decodifica_Tab"],
                    select: ["Tab_Decod_Id", "Decodifica_Tab"],
                    valueExpr: "Decodifica_Tab",
                    displayExpr: "Decodifica_Tab",
                    filterExpr: ["Nome_Tab", "=", "TIPO_SEGNALAZIONI"]
                },
                select: ["Tab_Decod.Tab_Decod_Id", "Tab_Decod.Decodifica_Tab"],
            },
            {
                caption: "Descrizione tipo segnalazione",
                dataField: "Tab_Damage.Descrizione_Tab_Damage",
                dataType: "string",
                calculateDisplayValue: function (rowData) {
                    if (!rowData.Tab_Damage)
                        return null;
                    return rowData.Tab_Damage.Descrizione_Tab_Damage;
                },
                calculateGroupValue: "Tab_Damage.Descrizione_Tab_Damage",
                select: ["Tab_Damage_Id", "Tab_Damage.Descrizione_Tab_Damage"],
            },
            {
                caption: GetLocalizedString("Cant"),
                dataField: "Cant",
                dataType: "object",
                calculateDisplayValue: function (rowData) {
                    if (!rowData.Cant_Id)
                        return null;
                    return rowData.Cant.Codice_Cantiere + "  " + rowData.Cant.Descrizione_Can;
                },
                calculateFilterExpression: function (filterValue, selectedFilterOperation) {
                    return ["Cant_Id", "=", filterValue];
                },
                calculateGroupValue: "Cant.Codice_Cantiere",
                lookup: {
                    columns: [{ dataField: "Codice_Cantiere" }, { dataField: "Descrizione_Can" }],
                    searchExpr: ["Codice_Cantiere", "Descrizione_Can"],
                    select: ["Cant_Id", "Codice_Cantiere", "Descrizione_Can"],
                    valueExpr: "Cant_Id",
                    displayExpr: "Codice_Cantiere"
                },
                searchExpr: ["Cant_Id", "Cant.Codice_Cantiere", "Cant.Descrizione_Can"],
                select: ["Cant_Id", "Cant.Codice_Cantiere", "Cant.Descrizione_Can"],
                visible: false
            },
            {
                caption: GetLocalizedString("Pru"),
                dataField: "Pru",
                dataType: "object",
                calculateDisplayValue: function (rowData) {
                    if (!rowData.Pru_Id)
                        return null;
                    return rowData.Pru.Codice_Pru;
                },
                calculateFilterExpression: function (filterValue, selectedFilterOperation) {
                    return ["Pru_Id", "=", filterValue];
                },
                calculateGroupValue: "Pru.Codice_Pru",
                lookup: {
                    columns: [{ dataField: "Codice_Pru" }, { dataField: "DisAbilitazione_Pru" }],
                    searchExpr: ["Codice_Pru", "DisAbilitazione_Pru"],
                    select: ["Pru_Id", "Codice_Pru", "DisAbilitazione_Pru"],
                    valueExpr: "Pru_Id",
                    displayExpr: "Codice_Pru"
                },
                searchExpr: ["Pru_Id", "Pru.Codice_Pru", "Pru.DisAbilitazione_Pru"],
                select: ["Pru_Id", "Pru.Codice_Pru", "Pru.DisAbilitazione_Pru"],
                visible: false,
            },
            {
                caption: GetLocalizedString("Fru"),
                dataField: "Fru",
                dataType: "object",
                calculateDisplayValue: function (rowData) {
                    if (!rowData.Fru_Id)
                        return null;
                    return rowData.Fru.Codice_Fru;
                },
                calculateFilterExpression: function (filterValue, selectedFilterOperation) {
                    return ["Fru_Id", "=", filterValue];
                },
                calculateGroupValue: "Fru.Codice_Fru",
                lookup: {
                    columns: [{ dataField: "Codice_Fru" }, { dataField: "DisAbilitazione_Fru" }],
                    searchExpr: ["Codice_Fru", "DisAbilitazione_Fru"],
                    select: ["Fru_Id", "Codice_Fru", "DisAbilitazione_Fru"],
                    valueExpr: "Fru_Id",
                    displayExpr: "Codice_Fru"
                },
                searchExpr: ["Fru_Id", "Fru.Codice_Fru", "Fru.DisAbilitazione_Fru"],
                select: ["Fru_Id", "Fru.Codice_Fru", "Fru.DisAbilitazione_Fru"],
                visible: false,
            },
            {
                caption: GetLocalizedString("MultimediaFileName"),
                dataField: "MultimediaFileName",
                dataType: "string"
            },
            {
                caption: GetLocalizedString("Note_Damage"),
                dataField: "Note_Damage",
                dataType: "string"
            },
            {
                caption: GetLocalizedString("Audio_Attachment"),
                dataField: "Audio_Attachment",
                dataType: "boolean",
                trueText: "Si",
                falseText: "No"
            },
            {
                caption: GetLocalizedString("Image_Attachment"),
                dataField: "Image_Attachment",
                dataType: "boolean",
                trueText: "Si",
                falseText: "No"
            },

        ],
        dataSource: new DevExpress.data.DataSource({

            store: new DevExpress.data.AspNet.createStore({
                key: "Damage_Id",
                loadUrl: "SegnalazioniPage.aspx/GridLoad",
                deleteUrl: "SegnalazioniPage.aspx/GridDelete",
                onBeforeSend: function (operation, ajaxSettings) {
                    ajaxSettings.type = "POST";
                    ajaxSettings.dataType = "application/json";

                    if (operation == "load") {
                        ajaxSettings.data = { data: ajaxSettings.data };
                        if (!ajaxSettings.data.data.select) {
                            ajaxSettings.data.data.select = CreateSelectExpr(MAINGRID);
                        }
                    }
                    console.log(ajaxSettings.data);
                }
            })
        }),
        editing: {
            allowDeleting: true,
            mode: "popup"
        },
        elementAttr: { "class": "maingrid" },
        filterRow: {
            visible: true,
            applyFilter: "auto",
            betweenEndText: 'Fine',
            betweenStartText: 'Inizio',
            operationDescriptions: { 'equal': 'Uguale a', 'notEqual': 'Diverso da', 'lessThan': 'Minore di', 'lessThanOrEqual': 'Minore uguale di', 'greaterThan': 'Maggiore di', 'greaterThanOrEqual': 'Maggiore uguale di', 'startsWith': 'Inizia con', 'contains': 'Contiene', 'notContains': 'Non contiene', 'endsWith': 'Finisce con', 'between': 'Tra' },
            resetOperationText: 'Resetta',
            showAllText: "(Mostra tutti)",
        },
        grouping: {
            autoExpandAll: false
        },
        groupPanel: { visible: true },
        onCellPrepared: function (e) {

            if (e.rowType === "data" && e.column.command === "edit") {


                var isEditing = e.row.isEditing,
                    $links = e.cellElement.find(".dx-link");
                $links.text("");

                if (isEditing) {
                    $links.filter(".dx-link-save").addClass("dx-icon-save");
                    $links.filter(".dx-link-cancel").addClass("dx-icon-revert");
                } else {
                    $links.filter(".dx-link-edit").addClass("dx-icon-edit");
                    $links.filter(".dx-link-delete").addClass("dx-icon-trash");
                }

                if (e.data.MultimediaFileName != "" && e.data.MultimediaFileName != null) {

                    e.cellElement.append(jQuery("<div>").dxButton({
                        icon: "folder",
                        onClick: function () {
                            jQuery.ajax({
                                type: "POST",
                                contentType: "application/json",
                                url: "SegnalazioniPage.aspx/GetFiles",
                                data: JSON.stringify({ damage_Id: e.data.Damage_Id }),
                                dataType: "json",
                                success: function (response) {
                                    var file = JSON.parse(response.d);

                                    if ("error" in file)
                                        DevExpress.ui.notify(file.error, "error", 3000);
                                    else
                                        $scope.download(file);
                                },
                                error: function (a, b, c) {
                                    console.log(a);
                                }
                            });
                        }

                    }));

                }


            }
        },
        onContentReady: function (e) {
            MoveEditColumnToLeft(e.component);
            LoadOnColumnMoved(e.component);
        },
        onInitialized: function (e) {
            e.component.option("dataSource").select(CreateSelectExpr(e.component));
            MAINGRID = e.component;
            visibleColumns = e.component.getVisibleColumns().length;
        },
        onEditorPreparing: function (e) {
            if (e.lookup && e.dataType != "boolean") {
                CreateStore(e, e.dataField, e.lookup.valueExpr, e.lookup.displayExpr, e.lookup.searchExpr, e.lookup.select, e.lookup.columns, e.lookup.filterExpr);
            }
        },
        pager: {
            allowedPageSizes: [10, 20, 50, 100],
            showPageSizeSelector: true,
            visible: true
        },
        remoteOperations: {
            filtering: true,
            grouping: true,
            groupPaging: true,
            paging: true,
            sorting: true
        },
        showBorders: true,
        showRowLines: true,
    }

    //#region POPUPSOPTIONS

    $scope.printPopupOptions = {
        contentTemplate: function (contentContainer) {

            var fromDateBox;
            var toDateBox;

            var printSelectBox;

            var loadIndicator;

            contentContainer.css({ "margin-left": "10%" });
            contentContainer.css({ "margin-right": "10%" });

            jQuery("<div>").dxSelectBox({

                dataSource: GetTabExcelModels(),
                displayExpr: "Nome_Risorsa",
                elementAttr: { "class": "popupSelectBox" },
                onInitialized: function (e) {
                    printSelectBox = e.component;
                },
                placeholder: "Seleziona modello...",
                valueExpr: "ExcelModel_Id"

            }).appendTo(contentContainer);

            var row = jQuery("<div>").addClass("popupDateBoxRow");

            //#region FROM_DATEBOX

            var row = jQuery("<div>").addClass("popupDateBoxRow");

            var label = jQuery("<div>").addClass("dx-field-label").html("Data inizio");

            var dateBox = jQuery("<div>").addClass("dx-field-value").dxDateBox({
                elementAttr: { "class": "leftButton" },
                onInitialized: function (e) {
                    fromDateBox = e.component;
                }
            });

            jQuery("<div>").addClass("dx-field-set").append(label).append(dateBox).appendTo(row);

            //#endregion FROM_DATEBOX

            //#region TO_DATEBOX

            label = jQuery("<div>").addClass("dx-field-label").html("Data fine");

            dateBox = jQuery("<div>").addClass("dx-field-value").dxDateBox({
                elementAttr: { "class": "rightButton" },
                onInitialized: function (e) {
                    toDateBox = e.component;
                }
            });

            jQuery("<div>").addClass("dx-field-set").append(label).append(dateBox).appendTo(row);

            //#endregion TO_DATEBOX

            row.appendTo(contentContainer);

            row = jQuery("<div>").addClass("popupDateBoxRow");
            row.css({
                "width": "100%",
                "text-align": "center"
            });

            loadIndicator = jQuery("<div>").addClass("loader")
            loadIndicator.appendTo(row);

            row.appendTo(contentContainer);

            row = jQuery("<div>").addClass("popupDateBoxRow");
            row.css({
                "width": "100%",
                "text-align": "center"
            });

            jQuery("<div>").dxButton({
                onClick: function (e) {

                    if ($scope.validatePrintOptions()) {

                        loadIndicator.css({ "visibility": "visible" });

                        jQuery.ajax({
                            type: "POST",
                            url: "SegnalazioniPage.aspx/ExportToExcel",
                            data: JSON.stringify({
                                excelModelId: printSelectBox.option("value"),
                                from: fromDateBox.option("value"),
                                to: toDateBox.option("value"),
                            }),
                            contentType: "application/json",
                            dataType: "json",
                            complete: function () {
                                loadIndicator.css({ "visibility": "hidden" });
                            }
                        }).done(response => {
                            var data = JSON.parse(response.d);
                            if (!data.fatalError)
                                $scope.download(data);
                            else
                                DevExpress.ui.notify(data.fatalError, "error", 3000);
                        }).fail(error => {
                            console.log(error)
                        });
                    }
                    loadIndicator.option("visible", false);
                },
                text: "Stampa"
            }).appendTo(row);

            row.appendTo(contentContainer);

            $scope.validatePrintOptions = function () {

                var valid = true;

                if (!printSelectBox.option("value")) {
                    valid = false;
                    DevExpress.ui.notify("Nessun export selezionato!", "error", 3000);
                    return valid;
                }

                if (!fromDateBox.option("value")) {
                    valid = false;
                    DevExpress.ui.notify("Nessuna data di inizio selezionata!", "error", 3000);
                    return valid;
                }

                if (!toDateBox.option("value")) {
                    valid = false;
                    DevExpress.ui.notify("Nessuna data di fine selezionata!", "error", 3000);
                    return valid;
                }

                return valid;
            }

        },
        height: function () {
            return jQuery(window).height() * 0.5;
        },
        title: "Pannello di stampa",
        width: function () {
            return jQuery(window).width() * 0.5;
        }
    }

    $scope.elaboratePopupOptions = {
        contentTemplate: function (contentContainer) {

            var fromDateBox;
            var toDateBox;

            var loadIndicator;

            contentContainer.css({ "margin-left": "10%" });
            contentContainer.css({ "margin-right": "10%" });

            //#region FROM_DATEBOX

            var row = jQuery("<div>").addClass("popupDateBoxRow");

            var label = jQuery("<div>").addClass("dx-field-label").html("Data inizio");

            var dateBox = jQuery("<div>").addClass("dx-field-value").dxDateBox({
                elementAttr: { "class": "leftButton" },
                onInitialized: function (e) {
                    fromDateBox = e.component;
                }
            });

            jQuery("<div>").addClass("dx-field-set").append(label).append(dateBox).appendTo(row);

            //#endregion FROM_DATEBOX

            //#region TO_DATEBOX

            label = jQuery("<div>").addClass("dx-field-label").html("Data fine");

            dateBox = jQuery("<div>").addClass("dx-field-value").dxDateBox({
                elementAttr: { "class": "rightButton" },
                onInitialized: function (e) {
                    toDateBox = e.component;
                }
            });

            jQuery("<div>").addClass("dx-field-set").append(label).append(dateBox).appendTo(row);

            //#endregion TO_DATEBOX

            row.appendTo(contentContainer);

            row = jQuery("<div>").addClass("popupDateBoxRow");

            loadIndicator = jQuery("<div>").addClass("loader")
            loadIndicator.appendTo(row);

            row.appendTo(contentContainer);
            row.css({
                "width": "100%",
                "text-align": "center"
            });

            row = jQuery("<div>").addClass("popupDateBoxRow");
            row.css({
                "width": "100%",
                "text-align": "center"
            });

            jQuery("<div>").dxButton({
                elementAttr: { "class": "bottom-centered-button" },
                onClick: function (e) {

                    if ($scope.validateElaborateOptions()) {
                        loadIndicator.css({ "visibility": "visible" });
                        jQuery.ajax({
                            type: "POST",
                            url: "SegnalazioniPage.aspx/Elaborate",
                            data: JSON.stringify({
                                from: fromDateBox.option("value"),
                                to: toDateBox.option("value"),
                            }),
                            contentType: "application/json",
                            dataType: "json",
                            complete: function () {
                                loadIndicator.css({ "visibility": "hidden" });
                            }
                        }).done(response => { if (response.d == "OK") { MAINGRID.option("dataSource").reload(); } }).fail(error => { console.log(error) });
                    }
                    loadIndicator.option("visible", false);
                },
                text: "Elabora"
            }).appendTo(row);

            row.appendTo(contentContainer);

            $scope.validateElaborateOptions = function () {

                var valid = true;

                if (!fromDateBox.option("value")) {
                    valid = false;
                    DevExpress.ui.notify("Nessuna data di inizio selezionata!", "error", 3000);
                    return valid;
                }

                if (!toDateBox.option("value")) {
                    valid = false;
                    DevExpress.ui.notify("Nessuna data di fine selezionata!", "error", 3000);
                    return valid;
                }

                return valid;
            }

        },
        height: function () {
            return jQuery(window).height() * 0.5;
        },
        title: "Pannello di elaborazione",
        width: function () {
            return jQuery(window).width() * 0.5;
        }
    }

    //#endregion POPUPSOPTIONS

    //#endregion WIDGETOPTIONS

    //#region SCOPE FUNCTIONS

    $scope.download = function (fileInfo) {

        window.location = "Downloader.ashx?" + jQuery.param(fileInfo);

    }

    //#endregion SCOPE FUNCTIONS

});

function CreateSelectExpr(grid) {

    let visibleColumns = grid.getVisibleColumns().filter(c => c.command != "edit");

    let selectArray = [];

    visibleColumns.forEach(c => {
        if (c.select) {
            jQuery.merge(selectArray, c.select);
        } else {
            selectArray.push(c.dataField);
        }
    });

    selectArray.unshift("Damage_Id");
    return selectArray;

}

function CreateStore(column, dataField, valueExpr, displayExpr, searchExpr, select, columns, filterExpr) {
    column.editorName = "dxDropDownBox";
    column.editorOptions.contentTemplate = function (args, content) {

        var container = jQuery("<div></div>");
        container.addClass("container");
        var timer = 0;

        jQuery("<div>").dxTextBox({
            placeholder: "Ricerca...",
            onInput: function (e) {
                clearTimeout(timer);
                timer = setTimeout(function () {
                    var text = e.component.option("text");
                    if (text == "") {
                        args.component.option("dataSource").filter(filterExpr || null);
                    } else {
                        if (filterExpr) {
                            args.component.option("dataSource").filter([filterExpr, "and", CreateFilterArray(searchExpr, text)]);
                        } else {
                            args.component.option("dataSource").filter(CreateFilterArray(searchExpr, text));
                        }
                    }
                    args.component.option("dataSource").load();
                }, 1000);
            }
        }).appendTo(container);

        jQuery("<div></div>").dxDataGrid({
            columnAutoWidth: true,
            columns: columns,
            dataSource: args.component.option("dataSource"),
            keyExpr: valueExpr,
            loadPanel: {
                enabled: true
            },
            noDataText: "Nessun elemento...",
            onContentReady: function (e) {
                args.component.element().find(".dx-popup").dxPopup("instance").repaint();
            },
            onRowClick: function (e) {
                var key = e.component.getKeyByRowIndex(e.rowIndex);
                args.component.option("value", key);
                args.component.close();
            },
            selection: { mode: "single" },
            showBorders: true,
            showColumnLines: true,
            showRowLines: true,
            remoteOperations: {
                paging: true,
                filtering: true
            },
            scrolling: {
                mode: "infinite"
            },
        }).addClass("selectBoxGrid").appendTo(container);

        container.appendTo(content);

    };
    column.editorOptions.dataSource = new DevExpress.data.DataSource({
        paginate: true,
        select: select,
        filter: filterExpr,
        store: new DevExpress.data.AspNet.createStore({
            key: valueExpr,
            loadUrl: "SegnalazioniPage.aspx/LookupLoad",
            onBeforeSend: function (operation, ajaxSettings) {
                let tmp = ajaxSettings.data;
                ajaxSettings.data = {};
                ajaxSettings.data.loadOptions = tmp;
                ajaxSettings.data.entity = dataField;
                console.log(ajaxSettings.data);
            }
        })
    });
    column.editorOptions.displayExpr = displayExpr;
    column.editorOptions.valueExpr = valueExpr;
    column.editorOptions.dropDownOptions = {
        width: "400px",
    }
    column.editorOptions.showClearButton = true;
}

function CreateFilterArray(searchExpr, text) {
    let filter = [[searchExpr[0], "contains", text]];
    if (searchExpr.length > 1) {
        searchExpr.slice(1).forEach((x, y) => {
            filter.push("or");
            filter.push([searchExpr[y + 1], "contains", text]);
        });
    }
    return filter;
}

function LoadOnColumnMoved(grid) {
    if (visibleColumns != grid.getVisibleColumns().length) {

        visibleColumns = grid.getVisibleColumns().length;

        grid.option("dataSource").select(CreateSelectExpr(grid));
        grid.option("dataSource").reload();
    }
}

function MoveEditColumnToLeft(grid) {
    grid.columnOption("command:edit", {
        visibleIndex: -1,
        minWidth: 100
    });
}

function GetLocalizedString(key) {

    var field = "FLD_" + key.toUpperCase();

    if (!_gridResources[field])
        return field;

    return _gridResources[field];

}