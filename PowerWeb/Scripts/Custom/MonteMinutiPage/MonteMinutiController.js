//Estensione del tipo Number per emulare il metodo Int.toString
Number.prototype.pad = function (size) {
    var s = String(this);
    while (s.length < (size || 2)) { s = "0" + s; }
    return s;
}

var cartellino = angular.module('monteminuti', ['dx']);
cartellino.controller("monteminutiController", function ($scope) {

    //Il pannello di caricamento dei cartellini
    jQuery("#retrieveMonteminutiLoadPanel").dxLoadPanel({
        message: "Recupero i monte minuti..."
    });

    //Il pannello di caricamento dei cartellini
    jQuery("#updateMonteminutiLoadPanel").dxLoadPanel({
        message: "Aggiorno il monte minuti..."
    });

    //La lista contenente i collaboratori con rispettivi monteminuti
    $scope.colMonteminuti = [];
    $scope.validationRules = [{
        type: "pattern",
        pattern: /^-?[0-9]{0,3}([:|.][0-5][0-9])?$/i,
        message: "L'ora dev'essere tra -999:59 e 999:59"
    }]


    $scope.dataGridOptions = {
        dataSource: $scope.colMonteminuti,
        columnAutoWidth: true,
        showBorders: true,
        paging: {
            pageSize: 10
        },
        pager: {
            showPageSizeSelector: true,
            allowedPageSizes: [10, 20, 50, 100, 200],
            showInfo: true
        },
        headerFilter: {
            visible: true
        },
        columns: [{ dataField: 'ColId', caption: 'Id', visible: false, allowEditing: false },
                  { dataField: 'Codice_Collaboratore', caption: 'Codice', allowEditing: false },
                  { dataField: 'Cognome_Col', caption: 'Cognome', allowEditing: false },
                  { dataField: 'Nome_Col', caption: 'Nome', allowEditing: false },
                  {
                      dataField: 'MonteMinuti_Col', caption: 'Monte Minuti', validationRules: [{
                          type: "pattern",
                          pattern: /^-?[0-9]{0,3}([:|.][0-5][0-9])?$/i,
                          message: "L'ora dev'essere tra -999:59 e 999:59"
                      }]
                  }
                 ],
        editing: {
            allowUpdating: true,
            mode: 'cell'
        },
        allowColumnReordering: true,
        allowColumnResizing: true,
        columnChooser: {
            enabled: false,
            height: 300,
            width: 300,
            emptyPanelText: 'Trascina qui una colonna per nasconderla',
            title: "Scegli colonne"
        },
        bindingOptions: {
            filterRow: "filterRow"
        },
        searchPanel: {
            visible: false,
            width: 300,
            placeholder: "Cerca..."
        },
        sorting: { mode: 'multiple' },
        stateStoring: {
            enabled: false,
        },
        loadPanel: {
            text: "Carico i collaboratori..."
        },
        "export": {
            enabled: true,
            fileName: "Collaboratori",
            allowExportSelectedData: true,
            texts: {
                exportAll: "Esporta tutti",
                exportSelectedRows: "Esporta solo selezionati",
                exportTo: "Esporta in Excel"
            }
        },
        onRowUpdated: function (e) { if (e != null && e != undefined && e.key != null && e.key != undefined) $scope.saveRow(e.key) },
        noDataText: "Scegliere un mese per visualizzarne i Monte Minuti",
        //onSelectionChanged: function (e) { if (e != null && e != undefined) setSelectedParameters(e.selectedRowKeys, $scope.date); $scope.keys = e.selectedRowKeys; },
    };

    //Le opzioni del selettore della data
    $scope.datePickerOptions = {
        acceptCustomValue: false,
        dateOutOfRangeMessage: "La data è oltre ai limiti",
        hint: "Scegli un mese",
        placeholder: "Scegli un mese",
        maxZoomLevel: "year",
        minZoomLevel: "decade",
        displayFormat: 'monthAndYear',
        width: 200,
        onValueChanged: function (e) { if (e != null && e != undefined) { $scope.dateChanged(e); } }
    };

    $scope.dateChanged = function (e) {
        jQuery('#retrieveMonteminutiLoadPanel').dxLoadPanel("instance").show();
        var longDate = e.value;
        var stringDate = longDate.getMonth() + 1 + "/" + longDate.getFullYear();
        setSelectedParameters(stringDate);
        dateChanged.PerformCallback();
    }

    $scope.getColMonteminuti = function () {
        $scope.colMonteminuti = colMonteminuti;
        jQuery('#colGrid').dxDataGrid({ dataSource: $scope.colMonteminuti });
        jQuery("#updateMonteminutiLoadPanel").dxLoadPanel("instance").hide();
    }


    $scope.saveRow = function (row) {
        jQuery("#updateMonteminutiLoadPanel").dxLoadPanel("instance").show();
        setRowToUpdate(JSON.stringify(row));
        rowUpdate.PerformCallback();
    }
});

angular.element(document).ready(function () {
    angular.bootstrap(document, ['monteminuti']);
});


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

var printErrorMessage = function (errorMessage) {
    if (errorMessage != undefined && errorMessage != "") {
        DevExpress.ui.notify(errorMessage, "error", 5000);
    }
}


    