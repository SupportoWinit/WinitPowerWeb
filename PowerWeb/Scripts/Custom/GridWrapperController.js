var gridWrapper = angular.module('gridWrapper', ['dx']);
var GRIGLIA;
var page = window.location.pathname.substr(1).split('/')[1];

var clonedRow = {};
var SELECTBOX = undefined;
//Controller 


angular.element(document).ready(function () {
    console.log("Master");
    SELECTBOX = jQuery("#viste").dxSelectBox("instance");

});


gridWrapper.controller("gridWrapperController", function ($scope) {
    console.log("MasterController");
    //Pannello caricamento vista
    jQuery("#loadingVisteLoadingPanel").dxLoadPanel({
        message: "Carico la vista..."
    });

    jQuery("#savingRowLoadingPanel").dxLoadPanel({
        message: "Salvataggio dei dati..."
    });

    //Coordinate cantieri selezionati
    $scope.cantCoordinates = [];

    $scope.lastView = undefined;

    //Opzioni di base box viste
    $scope.selectBoxOption = {
        acceptCustomValue: true,
        dataSource: new DevExpress.data.DataSource({
            store: new DevExpress.data.CustomStore({
                load: function (loadOptions) {
                    console.log("Loading viste");
                    let d = jQuery.Deferred();
                    var config = {
                        op: "load",
                        page: page
                    };
                    jQuery.ajax({
                        url: page + '/loadViste',
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        data: JSON.stringify(config),
                        success: function (result) {
                            var statesList = JSON.parse(result.d);
                            console.log(statesList);
                            
                            d.resolve(statesList);
                        },
                        error: function (a, b, c) {
                            console.log(a);
                            console.log(b);
                            console.log(c);
                        }
                    })
                    return d.promise();
                },
                insert: function (dati) {
                    jQuery("#loadingVisteLoadingPanel").dxLoadPanel("instance").show();

                    let d = jQuery.Deferred();
                    var config = {
                        param: dati,
                        page: page
                    };
                    jQuery.ajax({
                        url: page + '/saveVista',
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        data: JSON.stringify(config),
                        success: function (result) {
                            let view = JSON.parse(result.d);
                            console.log(view);
                            if (view.state != "Fail") {
                                DevExpress.ui.notify(view.message, "success", 2000);
                                SELECTBOX.option().dataSource.reload();
                                d.resolve();
                            } else {
                                DevExpress.ui.notify(view.message, "error", 4000);
                            }
                            jQuery("#loadingVisteLoadingPanel").dxLoadPanel("instance").hide();
                        }
                    });
                    return d.promise();
                },
                remove: function (data) {
                    let d = jQuery.Deferred();
                    var config = {
                        param: data,
                        page: page
                    };
                    console.log(config);
                    jQuery.ajax({
                        url: page + '/removeVista',
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        data: JSON.stringify(config),
                        success: function (result) {
                            var response = JSON.parse(result.d);
                            let status = "success";
                            if (response.status == "Done") {
                                SELECTBOX.option().dataSource.reload();
                                d.resolve(response.id);
                            } else {
                                status = "error";
                            }
                            DevExpress.ui.notify(response.message, status, 3000);
                            console.log(response);
                        }
                    });
                    return d.promise();
                },
                update: function (key, data) {
                    var d = jQuery.Deferred();
                    console.log(key);
                    console.log(data);
                    var config = {
                        type: "update",
                        param: JSON.stringify({ id: key, data: data })
                    };
                    jQuery.ajax({
                        url: page + '/masterServe',
                        type: "POST",
                        contentType: 'application/json; charset=utf-8',
                        data: JSON.stringify(config),
                        dataType: 'json',
                        success: function (modKey) {

                            d.resolve(parseInt(modKey.d));
                        }
                    });
                    return d.promise();
                },
                byKey: function (key) {
                    console.log("key searching");
                    var d =  jQuery.Deferred();
                    SELECTBOX.option().items.forEach(function (item, index) {
                        if (item.DataGrid_Id == key) {
                            console.log(item.DataGrid_Id);
                            d.resolve(item);
                        }
                    });
                    return d.promise();
                },
                key: "DataGrid_Id",
            })
        }),
        displayExpr: 'Nome_Layout',
        onValueChanged: function (e) {
            $scope.lastView = e.previousValue;
            //Controllo che il value cambiato sia effettivamente un oggetto consono
            if (e.value.DataGrid_Id != undefined) {
                GRIGLIA.state(e.value.Layout_DataGrid);
                GRIGLIA.repaint();
             }
        },
        placeholder: 'Seleziona viste',
        width: function () {
            return jQuery(window).width * 0.4;
        }

    }
    //Opzione popup mappa
    $scope.mapPopupOptions = {

        height: function () { return $(window).height() * 0.5 },
        resizeEnabled: true,
        showTitle: true,
        shading: false,
        title: 'Mappa cantieri',
        visible: false,
        width: function () { return $(window).height() * 0.9 },
    }
    //Opzioni mappa
    $scope.mapOptions = {
        bindingOptions: {
            markers: 'cantCoordinates'
        },
        height: '100%',
        key: {
            bing: getBingKey().key
        },
        onInitialized: function (e) {
            $scope.getCoordinates();
        },
        onReady: function (e) {

        },
        provider: 'bing',
        type: "roadmap",
        width: '100%'
    }


    //Funzione attivata dal bottone di salvataggio per le viste
    $scope.saveVista = function (e) {
        var nome = SELECTBOX.option("value");

        console.log(nome);
        if (typeof nome === 'string') {
            var exist = SELECTBOX.option().items.findIndex(i => i.Nome_Layout == nome);
            if (nome == "Default") {
                DevExpress.ui.notify("Non si può salvare una vista con il nome DEFAULT", "error", 3000);
            } else if (exist >= 0) {
                var stato = GRIGLIA.state();
                console.log()
                SELECTBOX.option().dataSource.store.update(SELECTBOX.option().items[exist].DataGrid_Id, stato).done(function (data, state) {

                });
            } else {
                var stato = GRIGLIA.state();
                console.log(stato);
                var dati = { "nome": nome, "stato": stato };
                SELECTBOX.option().dataSource.store().insert(dati).done(function(values, key) {
                    console.log("bene");
                    console.log(SELECTBOX.option().items);
                })
    .fail(function(error) {
        console.log(error);
    });
            }
        } else {
            var exist = SELECTBOX.option().items.findIndex(i => i.Nome_Layout == nome.Nome_Layout);
            if (nome == "Default") {
                DevExpress.ui.notify("Non si può salvare una vista con il nome DEFAULT", "error", 3000);
            } else if (nome == "" || nome == undefined || nome == null) {
                DevExpress.ui.notify("Nessuna vista inserita", "error", 3000);
            } else if (exist >= 0) {
                var stato = GRIGLIA.state();
                console.log(stato);
                SELECTBOX.option().dataSource.store.update(SELECTBOX.option().items[exist].DataGrid_Id, stato).done(function (data, state) {

                    console.log(data);
                    console.log(stato);
                    GRIGLIA.state(stato);
                    GRIGLIA.refresh();
                    GRIGLIA.repaint();
                });
            }
        }

    }

    //Funzione attivata dal bottone di cancellazione per la vista selezionata
    $scope.deleteVista = function (e) {
        var selected = SELECTBOX.option().selectedItem;
        console.log(selected);
        if (selected == null) {
            DevExpress.ui.notify("Nessuna vista inserita", "error", 3000);
        } else {
            console.log(selected);
            SELECTBOX.option().dataSource.store().remove(selected.DataGrid_Id).done(function (data, state) {

                //console.log(data); console.log(state);
                //GRIGLIA.beginUpdate();
                //setGridState(data.stato);
                //GRIGLIA.endUpdate();

            });
        }
    }

    //Abilita bottone mappa se in pagina cantieri
    $scope.enableMap = function () {
        return page == 'CantierePage.aspx';
    }

    //Mostra mappa
    $scope.showMap = function () {
        $scope.getCoordinates();
        console.log($scope.cantCoordinates);
        jQuery('#mapPopup').dxPopup("instance").show();
    }
    //Ottieni coordinate dei cant selezionati
    $scope.getCoordinates = function () {
        var selRecords = GRIGLIA.getSelectedRowsData();
        console.log(selRecords);
        $scope.cantCoordinates = [];

        selRecords.forEach(function (item, index) {
            if ((item.LatitudineGps_Can != 0 && item.LongitudineGps_Can != 0) && (item.LatitudineGps_Can != null && item.LongitudineGps_Can != null) && (item.LatitudineGps_Can != undefined && item.LongitudineGps_Can != undefined)) {
                console.log(item);
                var coordItem = {
                    location: [item.LatitudineGps_Can, item.LongitudineGps_Can],
                    isShown: true,
                    tooltip: { text: item.Codice_Cantiere, isShown: true },
                };
                $scope.cantCoordinates.push(coordItem);
            }
        });
        console.log($scope.cantCoordinates);
    }

    //Aggiorna la disposizione della griglia
    var setGridState = function (e) {
        GRIGLIA.state(e);
    }
});



