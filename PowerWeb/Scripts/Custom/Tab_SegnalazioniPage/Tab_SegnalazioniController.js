
var _gridResources = GetGridResources();

console.log(_gridResources);

var MAINGRID;

var visibleColumns;

angular.module("tab_segnalazioniModule", ['dx']).controller("tab_segnalazioniController", function ($scope) {

    $scope.maingridOptions = {
        cacheEnabled: false,
        columnChooser: {
            enabled: true,
            height: 300,
            width: 300,
            emptyPanelText: 'Trascina qui una colonna per nasconderla',
            title: "Scegli colonne"
        },
        columnAutoWidth: true,
        columns: [
            {
                dataField: "Tab_Damage_Id",
                dataType: "number",
                formItem: {
                    visible: false
                },
                showInColumnChooser: false,
                visible: false
            },
            {
                dataField: "Tab_Decod_Id",
                caption: GetLocalizedString("CODICE_TAB_DAMAGE"),
                dataType: "object",
                calculateDisplayValue: function (rowData) {
                    if (!rowData.Tab_Decod)
                        return null;
                    return rowData.Tab_Decod.Decodifica_Tab;
                },
                calculateFilterExpression: function (filterValue, selectedFilterOperation) {
                    return ["Tab_Decod_Id", "=", filterValue];
                },
                calculateGroupValue: "Tab_Decod.Decodifica_Tab",
                lookup: {
                    entity: "Tab_Decod",
                    columns: [{ dataField: "Decodifica_Tab" }],
                    searchExpr: ["Decodifica_Tab"],
                    select: ["Tab_Decod_Id", "Decodifica_Tab"],
                    valueExpr: "Tab_Decod_Id",
                    displayExpr: "Decodifica_Tab",
                    filterExpr: ["Nome_Tab", "=", "TIPO_SEGNALAZIONI"]
                },
                select: ["Tab_Decod.Tab_Decod_Id", "Tab_Decod.Decodifica_Tab"],
            },
            ,
            {
                dataField: "Descrizione_Tab_Damage",
                caption: GetLocalizedString("Descrizione_Tab_Damage"),
                dataType: "string"
            },
            {
                dataField: "DisAbilitazione_Tab_Damage",
                caption: GetLocalizedString("DisAbilitazione_Tab_Damage"),
                dataType: "boolean",
                trueText: "Si",
                falseText: "No"
            }
        ],
        dataSource: new DevExpress.data.DataSource({

            store: new DevExpress.data.AspNet.createStore({
                key: "Tab_Damage_Id",
                loadUrl: "Tab_SegnalazioniPage.aspx/GridLoad",
                insertUrl:"Tab_SegnalazioniPage.aspx/GridInsert",
                updateUrl: "Tab_SegnalazioniPage.aspx/GridUpdate",
                deleteUrl: "Tab_SegnalazioniPage.aspx/GridDelete",
                onBeforeSend: function (operation, ajaxSettings) {

                    ajaxSettings.type = "POST";
                    ajaxSettings.dataType = "application/json";

                    if (operation == "load") {
                        ajaxSettings.data = { data: ajaxSettings.data };
                        if (!ajaxSettings.data.data.select) {
                            ajaxSettings.data.data.select = CreateSelectExpr(MAINGRID);
                        }
                    }
                }
            })
        }),
        editing: {
            allowDeleting: true,
            allowAdding: true,
            allowUpdating: true,
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
                CreateStore(e, e.dataField, e.lookup.valueExpr, e.lookup.displayExpr, e.lookup.searchExpr, e.lookup.select, e.lookup.columns,e.lookup.filterExpr,e.lookup.entity);
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

    selectArray.unshift("Tab_Damage_Id");
    return selectArray;

}

function CreateStore(column, dataField, valueExpr, displayExpr, searchExpr, select, columns, filterExpr,entity) {
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
            loadUrl: "Tab_SegnalazioniPage.aspx/LookupLoad",
            onBeforeSend: function (operation, ajaxSettings) {
                let tmp = ajaxSettings.data;
                ajaxSettings.data = {};
                ajaxSettings.data.loadOptions = tmp;
                ajaxSettings.data.entity = entity;
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

    console.log(field);

    if (!_gridResources[field])
        return field;

    return _gridResources[field];

}