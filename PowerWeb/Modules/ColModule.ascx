<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ColModule.ascx.cs" Inherits="PowerWeb.Modules.ColModule" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxRoundPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxGridView" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dxe" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxFormLayout" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxCallback" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxTimer" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPopupControl" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPanel" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxUploadControl" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<% if (DesignMode)
    {  %><script type="text/javascript" src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script><% }  %>

<script type="text/javascript" src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script type="text/javascript">
    function OnNascLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var nascLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Nascita_Provincia_Col"));
        var nascLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Nascita_Cap_Col"));
        var nascLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Codice_Nascita_Luogo_Col"));
        nascLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        nascLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        nascLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Luogo_Tab_Comuni"));
    }
    function OnResLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var resLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Residenza_Provincia_Col"));
        var resLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Residenza_Cap_Col"));
        var resLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Codice_Residenza_Luogo_Col"));
        resLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        resLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        resLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Luogo_Tab_Comuni"));
    }
    function OnDomLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var domLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Provincia_Col"));
        var domLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Cap_Col"));
        var domLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Codice_Domicilio_Luogo_Col"));
        domLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        domLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        domLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Luogo_Tab_Comuni"));
    }
    function OnNascCodLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var nascCodLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Nascita_Provincia_Col"));
        var nascCodLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Nascita_Cap_Col"));
        var nascCodLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Nascita_Luogo_Col"));
        nascCodLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        nascCodLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        nascCodLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Luogo_Tab_Comuni"));
    }
    function OnResCodLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var resCodLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Residenza_Provincia_Col"));
        var resCodLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Residenza_Cap_Col"));
        var resCodLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Residenza_Luogo_Col"));
        resCodLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        resCodLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        resCodLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Luogo_Tab_Comuni"));
    }
    function OnDomCodLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var domCodLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Provincia_Col"));
        var domCodLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Cap_Col"));
        var domCodLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Luogo_Col"));
        domCodLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        domCodLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        domCodLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Luogo_Tab_Comuni"));
    }

    //#region ------------------ Gestione UPLOAD File ANAGRAFICHE --------------------------
    function Uploader_OnUploadStart() {
        btnUpload.SetEnabled(false);
    }

    function Uploader_OnFilesUploadComplete(e) {
        if (e.errorText) {
            var errorResult = e.errorText.split('|');
            DisplayDialogError(errorResult[0], errorResult[1]);
        } else {
            tUplImportPing.SetEnabled(true);
            cUplImportCommand.PerformCallback();
        }
    }

    //quando ho terminato il callback vado 
    function cUplImportPing_OnCallbackComplete(s, e) {

        var progrAndCommandArray = e.result.split('|');
        var currentPB = ASPxClientProgressBar.Cast(pbProgress);
        var currentProgress = parseFloat(progrAndCommandArray[0].replace(",", "."));
        currentPB.SetPosition(currentProgress);
        lblResult.SetText(progrAndCommandArray[1]);
    }


    function cUplImportCommand_OnCallbackComplete(s, e) {

        if (cUplImportCommand.cpImportError) {
            pbProgress.SetPosition(0);
            var message = cUplImportCommand.cpImportError;
            cUplImportCommand.cpImportError = '';
            DisplayDialogError('Power Web', message);

            // se l'importazione è andata a buon fine allora effettuo un callback della vista al fine di visualizzare
            // i nuovi dati importati
            grid.PerformCallback('updateDataSource');

        } else {
            tUplImportPing.SetEnabled(false);
            pbProgress.SetPosition(100);
            DisplayDialogInfo('Power Web', 'Inserimento avvenuto con successo');
            var callbackResult = e.result.split('|');
            if (callbackResult[0] == 'Error') {
                DisplayDialogError(callbackResult[1], callbackResult[2]);
            } else {
                lblResult.SetText(callbackResult[1]);
            }
        }
    }

    function tUplImportPing_OnTick(s, e) {
        cUplImportPing.PerformCallback();
    }

    function cmbFil_OnValueChanged() {
        UpdateUploadButton();
    }

    function UpdateUploadButton() {

        if (typeof cmbFil != 'undefined') {

            var comboFil = ASPxClientComboBox.Cast(cmbFil);

            if (comboFil.GetValue() != null && comboFil.GetValue() != null && uploader.GetText(0) != "")
                btnUpload.SetEnabled(true);
            else
                btnUpload.SetEnabled(false);
        }
        else {
            if (uploader.GetText(0) != "")
                btnUpload.SetEnabled(true);
            else
                btnUpload.SetEnabled(false);
        }
    }

    //#endregion

    //#region -----------Gestione Combobox di selezione righe da elaborare ------------------------------------------------
    var _selectNumber = 0;
    var _selectNumberOverPage = 0;
    var selectionType = "none";
    //btnTrips.SetEnabled(false);

    function OnAllCheckedChanged(s, e) {
        if (s.GetChecked()) {
            DisplayJConfirm('Power', s.cpMessage, function (r) {
                if (r) {
                    if (typeof btnTrips != 'undefined')
                        btnTrips.SetEnabled(true);

                    grid.SelectRows();
                    selectionType = "sAll";
                }
                else {
                    s.SetChecked(false);
                    if (typeof btnTrips != 'undefined')
                        btnTrips.SetEnabled(false);
                    selectionType = "uAll";
                    grid.UnselectRows();
                }

            });
        }
        else {
            s.SetChecked(false);
            if (typeof btnTrips != 'undefined')
                btnTrips.SetEnabled(false);
            selectionType = "uAll";
            grid.UnselectRows();
        }

    }

    var _handle = true;

    function OnPageCheckedChanged(s, e) {
        _handle = false;

        if (s.GetChecked()) {
            selectionType = "sPage";
            grid.SelectAllRowsOnPage();
        }

        else {
            selectionType = "uPage";
            grid.UnselectAllRowsOnPage();
        }
    }


    function OnGridSelectionChanged(s, e) {

        cbAll.SetChecked(s.GetSelectedRowCount() == s.cpVisibleRowCount);

        if (e.isChangedOnServer == false) {
            if (e.isAllRecordsOnPage && e.isSelected) {
                _selectNumberOverPage = s.GetVisibleRowsOnPage();
                _selectNumber = _selectNumber + s.GetVisibleRowsOnPage(); // when all rows are selected within the page
            }
            else if (e.isAllRecordsOnPage && !e.isSelected) {
                _selectNumber = _selectNumber - s.GetVisibleRowsOnPage(); // when all rows are deselected within the page
                if (_selectNumber == 0)
                    _selectNumberOverPage = 0;
            }

            else if (!e.isAllRecordsOnPage && e.isSelected) {
                selectionType = "sRow";

                _selectNumber++; // when one row is selected
                _selectNumberOverPage++;
            }

            else if (!e.isAllRecordsOnPage && !e.isSelected) {
                selectionType = "uRow";

                _selectNumber--; // when one row is deselected
                _selectNumberOverPage--;
            }

            if (_handle) { // if the selection wasn’t performed by clicking the cbPage
                cbPage.SetChecked(_selectNumberOverPage == s.GetVisibleRowsOnPage()); // let’s change the cbPage state if needed
                _handle = false;
            }

            _handle = true;
        }
        else {
            cbPage.SetChecked(cbAll.GetChecked()); // if the selection was performed on the server, let’s check cbPage
        }

        //controllo se ho selezioanto o no qualche collaboratore per abilitare o no  il pulsante di elaborazione viaggi
        if (_selectNumber != 0)
            if (typeof btnTrips !== 'undefined')
                btnTrips.SetEnabled(true);
            else {
                if (typeof btnTrips !== 'undefined')
                    btnTrips.SetEnabled(false);
            }

        if (typeof lblNColSelValue !== 'undefined') {
            lblNColSelValue.SetText(_selectNumber);
        }

        gridSelectionChange.PerformCallback(selectionType);
    }



    function OnGridEndCallback(s, e) {
        if (typeof s.cpErrorMessage != 'undefined' && s.cpErrorMessage != '') {
            var errorMessage = s.cpErrorMessage;
            s.cpErrorMessage = '';
            DisplayDialogError('Errori inserimento', errorMessage);
        }

        if (grid.cpPageChanged == 1) {
            _selectNumberOverPage = 0;
        }

        if (selectionType == 'sAll')
            _selectNumber = s.cpVisibleRowCount;
        else if (selectionType == 'uAll') {
            _selectNumber = 0;
        }
    }

    function btnTrips_onClick(s, e) {

        var to = deTo.GetDate();
        var from = deFrom.GetDate();

        var selectedComboCol = '';

        if (typeof Search_Col_Id_2 != 'undefined')
            selectedComboCol = Search_Col_Id_2.GetText();
        if (to != null && from != null && to >= from) {
            if (selectedComboCol != '' || ((lblNColSelValue.GetText() != '') && (lblNColSelValue.GetText() != '0'))) {
                DisplayJConfirm("Power", btnTrips.cpMessage, function (r) {
                    if (r) {
                        tPing.SetEnabled(false);
                        cTrips.PerformCallback("elaborateTrips");
                    }
                });
            } else {
                DisplayDialogError('Power', btnTrips.cpErrorMessageCol);
            }
        } else DisplayDialogError('Power', btnTrips.cpErrorMessage);
    }

    function btnDeleteTrips_onClick(s, e) {
        var to = deTo.GetDate();
        var from = deFrom.GetDate();

        var selectedComboCol = '';

        if (typeof Search_Col_Id_2 != 'undefined')
            selectedComboCol = Search_Col_Id_2.GetText();

        if (to != null && from != null && to >= from) {
            if (selectedComboCol != '' || ((lblNColSelValue.GetText() != '') && (lblNColSelValue.GetText() != '0'))) {
                DisplayJConfirm("Power", btnDeleteTrips.cpMessage, function (r) {
                    if (r) {
                        tPing.SetEnabled(true);
                        cTrips.PerformCallback("deleteTrips");
                    }
                });
            } else {
                DisplayDialogError('Power', btnDeleteTrips.cpErrorMessageCol);
            }
        } else DisplayDialogError('Power', btnDeleteTrips.cpErrorMessage);
    }

    function btnRoundings_onClick(s, e) {
        var to = deTo.GetDate();
        var from = deFrom.GetDate();
        var selectedComboCol = '';
        if (typeof Search_Col_Id_2 != 'undefined')
            selectedComboCol = Search_Col_Id_2.GetText();
        if (to != null && from != null && to >= from) {
            if (selectedComboCol != '' || ((lblNColSelValue.GetText() != '') && (lblNColSelValue.GetText() != '0'))) {
                DisplayJConfirm("Power", BtnRoundings.cpMessage, function (r) {
                    if (r) {
                        tPing.SetEnabled(true);
                        cRoundings.PerformCallback("elaborateRoundings");
                    }
                });
            } else {
                DisplayDialogError('Power', BtnRoundings.cpErrorMessageCol);
            }
        } else DisplayDialogError('Power', BtnRoundings.cpErrorMessage);
    }

    function btnDeleteRoundings_onClick(s, e) {
        var to = deTo.GetDate();
        var from = deFrom.GetDate();

        var selectedComboCol = '';

        if (typeof Search_Col_Id_2 != 'undefined')
            selectedComboCol = Search_Col_Id_2.GetText();
        if (to != null && from != null && to >= from) {
            if (selectedComboCol != '' || ((lblNColSelValue.GetText() != '') && (lblNColSelValue.GetText() != '0'))) {
                DisplayJConfirm("Power", BtnDeleteRoundings.cpMessage, function (r) {
                    if (r) {
                        tPing.SetEnabled(true);
                        cRoundings.PerformCallback("deleteRoundings");
                    }
                });
            } else {
                DisplayDialogError('Power', BtnDeleteRoundings.cpErrorMessageCol);
            }
        } else DisplayDialogError('Power', BtnDeleteRoundings.cpErrorMessage);
    }

    function btnLaunchElaborate_onClick(s, e) {
        var to = deTo.GetDate();
        var from = deFrom.GetDate();

        var selectedComboCol = '';

        if (typeof Search_Col_Id_2 != 'undefined')
            selectedComboCol = Search_Col_Id_2.GetText();
        if (to != null && from != null && to >= from) {
            if (selectedComboCol != '' || ((lblNColSelValue.GetText() != '') && (lblNColSelValue.GetText() != '0'))) {
                DisplayJConfirm("Power", btnLaunchElaborate.cpMessage, function (r) {
                    if (r) {
                        tPingElaborate.SetEnabled(true);
                        cElaborate.PerformCallback();
                    }
                });
            } else {
                DisplayDialogError('Power', btnLaunchElaborate.cpErrorMessageCol);
            }
        } else DisplayDialogError('Power', btnLaunchElaborate.cpErrorMessage);
    }


    function btnLaunchExport56_onClick(s, e) {
        var to = deTo.GetDate();
        var from = deFrom.GetDate();
        if (to != null && from != null && to >= from) {
            e.processOnServer = true;
            viewLoadingExport();
        } else {
            DisplayDialogError('Power', btnDeleteTrips.cpErrorMessage);
            e.processOnServer = false;
        }
    }

    function cComplete_OnCallbackComplete(s, e) {
        tPing.SetEnabled(false);

        var currentPB = ASPxClientProgressBar.Cast(pbProgress);
        currentPB.SetPosition(100);
        lblResult.SetText('Elaborazione Terminata');
        lblResult.SetText('fine');
        DisplayDialogInfo('Power', e.result);
    }

    function cEalborateComplete_OnCallbackComplete(s, e) {
        tPing.SetEnabled(false);

        var currentPB = ASPxClientProgressBar.Cast(pbProgress);
        currentPB.SetPosition(100);
        lblResult.SetText('Elaborazione Terminata');
        lblResult.SetText('fine');
        DisplayDialogInfo('Power', e.result);
    }

    function tPing_OnTick(s, e) {
        cPing.PerformCallback();
    }

    function tPingElaborate_OnTick(s, e) {
        cPingElaborate.PerformCallback();
    }

    function cPing_OnCallbackComplete(s, e) {
        if (e.result != null) {
            var progrAndCommandArray = e.result.split('|');
            var currentPB = ASPxClientProgressBar.Cast(pbProgress);
            var currentProgress = parseFloat(progrAndCommandArray[0].replace(",", "."));
            currentPB.SetPosition(currentProgress);
            lblResult.SetText(progrAndCommandArray[1]);
        }
    }

    // ------------------------- GESTIONE LOADING IMAGE EXPORT 56 ------------------------------
    function cPingLoadingExport_OnCallbackComplete(s, e) {
        if (e.result != null) {
            if (e.result == "True") {
                tPingLoadingExport.SetEnabled(false);
                lpLoading.Hide();
            }
        }
    }

    function onCustomEditButtonComboBoxClick(s, e) {
        var combo = ASPxClientComboBox.Cast(s);
        combo.SetText(" ");
        combo.ShowDropDown();
        combo.SetSelectedItem(null);
        combo.SetSelectedIndex(-1);
        combo.PerformCallback();

    }


    function tPingLoadingExport_OnTick(s, e) {
        cPingLoadingExport.PerformCallback();
    }

    function viewLoadingExport(s, e) {
        tPingLoadingExport.SetEnabled(true);
        lpLoading.Show();
    }
    // ------------------------- FINE GESTIONE LOADING IMAGE EXPORT 56 -------------------------

    //#region --------------- Gestione Area CARICAMENTO MAPPA DA BING ---------------------------------------

    var bingMap = null;
    var searchManager = null;
    var isFirstTimeRequest = true;

    var map;
    var marker;

    function newinitMap() {
        // Distruggi mappa precedente se esiste
        if (typeof map !== "undefined" && map.remove) {
            map.remove();
        }

        var tryInitMap = function (attemptsLeft) {
            var container = document.getElementById('bingMap');
            if (!container || container.clientHeight === 0 || container.clientWidth === 0) {
                if (attemptsLeft > 0) {
                    setTimeout(function () {
                        tryInitMap(attemptsLeft - 1);
                    }, 200); // Aspetta 200ms e riprova
                } else {
                    console.warn("Map container still not ready after retries.");
                }
                return;
            }

            // Ottieni coordinate dal grid
            var currentGrid = ASPxClientGridView.Cast(grid);
            var currentLat = currentGrid.GetEditValue("LatitudineGps_Col");
            var currentLon = currentGrid.GetEditValue("LongitudineGps_Col");
            if (currentLat && currentLon) {
                currentLat = parseFloat(currentLat.replace(",", "."));
                currentLon = parseFloat(currentLon.replace(",", "."));
            }

            var initialLatLng = [currentLat || 45.4642, currentLon || 9.1900];

            // Inizializza la mappa
            map = L.map('bingMap').setView(initialLatLng, 16);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19
            }).addTo(map);

            // Icona marker
            delete L.Icon.Default.prototype._getIconUrl;
            L.Icon.Default.mergeOptions({
                iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
                iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
                shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png'
            });

            marker = L.marker(initialLatLng, { draggable: true }).addTo(map);

            marker.on('dragend', function (e) {
                var latLng = e.target.getLatLng();
                updateCoordinates(latLng.lat, latLng.lng);
            });

            updateCoordinates(initialLatLng[0], initialLatLng[1]);

            // Dopo aver creato tutto, ricalcola dimensione
            map.invalidateSize();
        };

        // Avvia tentativi di init (5 tentativi x 200ms = 1s massimo)
        tryInitMap(5);
    }

    function updateCoordinates(lat, lon) {
        // Converte in stringa con virgola (come nel tuo codice Bing)
        var latString = lat.toString().replace(".", ",");
        var lonString = lon.toString().replace(".", ",");

        var currentGrid = ASPxClientGridView.Cast(grid); // Assicurati che 'grid' sia il nome corretto
        currentGrid.SetEditValue("LatitudineGps_Can", latString);
        currentGrid.SetEditValue("LongitudineGps_Can", lonString);
    }

    function loadMap() {
        isFirstTimeRequest = true;
        //
        var currentGrid = ASPxClientGridView.Cast(grid);
        var currentLat = currentGrid.GetEditValue("LatitudineGps_Col");
        var currentLon = currentGrid.GetEditValue("LongitudineGps_Col");

        var mapElement = document.getElementById('bingMap');

        if (bingMap) {
            bingMap.dispose();
            bingMap = null;
        }

        var bingKey = "<%= BingKey %>";

        bingMap = new Microsoft.Maps.Map(mapElement, { credentials: bingKey });
        bingMap.setView({ mapTypeId: Microsoft.Maps.MapTypeId.road });


        if (currentLat && currentLon) {
            var currentLatValue = parseFloat(currentLat.replace(",", "."));
            var currentLonValue = parseFloat(currentLon.replace(",", "."));

            if (currentLatValue != 0.0 && currentLonValue != 0.0) {
                var pushpinOptions = { draggable: true };
                var pushpin = new Microsoft.Maps.Pushpin(bingMap.getCenter(), pushpinOptions);
                pushpin = new Microsoft.Maps.Pushpin(new Microsoft.Maps.Location(currentLatValue, currentLonValue), pushpinOptions);
                var pushpindragend = Microsoft.Maps.Events.addHandler(pushpin, 'dragend', OnEndDragPushpin);
                bingMap.setView({ zoom: 17, center: new Microsoft.Maps.Location(currentLatValue, currentLonValue) });
                bingMap.entities.push(pushpin);
            }
            else
                LoadSearchModule();
        }
    }

    function createSearchManager() {
        bingMap.addComponent('searchManager', new Microsoft.Maps.Search.SearchManager(bingMap));
        searchManager = bingMap.getComponent('searchManager');
    }

    function LoadSearchModule() {
        Microsoft.Maps.loadModule('Microsoft.Maps.Search', { callback: geocodeRequest })
    }

    function geocodeRequest() {
        createSearchManager();
        var currentGrid = ASPxClientGridView.Cast(grid);
        var currentAddress = currentGrid.GetEditValue("Domicilio_Indirizzo_Col");
        var currentPlace = currentGrid.GetEditValue("Domicilio_Luogo_Col");
        var currentZip = currentGrid.GetEditValue("Domicilio_Cap_Col");
        var where = "";
        if (currentAddress && isFirstTimeRequest)
            where += currentAddress;
        if (currentPlace)
            where += " " + currentPlace;
        if (currentZip)
            where += " " + currentZip;

        if (where && where.length > 0) {
            var userData = { name: 'Maps Test User', id: 'XYZ' };
            var request =
            {
                where: where,
                count: 5,
                bounds: bingMap.getBounds(),
                callback: onGeocodeSuccess,
                errorCallback: onGeocodeFailed,
                userData: userData
            };

            searchManager.geocode(request);
        }
    }

    function onGeocodeSuccess(result, userData) {
        if (result) {
            bingMap.entities.clear();
            var topResult = result.results && result.results[0];
            if (topResult) {
                var offset = new Microsoft.Maps.Point(0, 5);
                var pushpinOptions = { draggable: true };
                if (!isFirstTimeRequest)
                    pushpinOptions = { text: '!', visible: true, textOffset: offset, draggable: true };

                var pushpin = new Microsoft.Maps.Pushpin(topResult.location, pushpinOptions);
                var pushpindragend = Microsoft.Maps.Events.addHandler(pushpin, 'dragend', OnEndDragPushpin);
                bingMap.setView({ zoom: 16, center: topResult.location });
                bingMap.entities.push(pushpin);

            } else {
                isFirstTimeRequest = false;
                geocodeRequest();
            }
        }
    }

    function onGeocodeFailed(result, userData) {
        var offset = new Microsoft.Maps.Point(0, 5);
        var pushpinOptions = { text: '!', visible: true, textOffset: offset, draggable: true };
        var pushpin = new Microsoft.Maps.Pushpin(topResult.location, pushpinOptions);
        var pushpindragend = Microsoft.Maps.Events.addHandler(pushpin, 'dragend', OnEndDragPushpin);
        bingMap.setView({ zoom: 16, center: bingMap.getCenter() });
        bingMap.entities.push(pushpin);
    }

    function OnEndDragPushpin(e) {
        var currentGrid = ASPxClientGridView.Cast(grid);
        var currentLatString = e.entity.getLocation().latitude.toString().replace(".", ",");
        var currentLonString = e.entity.getLocation().longitude.toString().replace(".", ",");
        var currentLat = currentGrid.SetEditValue("LatitudineGps_Col", currentLatString);
        var currentLon = currentGrid.SetEditValue("LongitudineGps_Col", currentLonString);
    }
    //#endregion

    // -------------------------- GESTIONE DELL'INSERIMENTO DI NUOVE ASSOCIAZIONI -------------------------------------------

    // effettua il controllo dei valori di input della procedura di inserimento
    // delle nuove associazioni e ritorna true in caso la validazione abbia dato esito positivo
    // e false in caso la validazione abbia dato esito negativo
    function validateAddNewAssociation(s) {
        // inizializzazione del valore di ritorno della funzione
        var returnValue = false;

        // devono essere valorizzati tutti e tre i campi che indicano codice unità fissa, id cantiere e data
        // (i nomi dei combobox sono modificati dalla fillComboboxes e quindi cambiano rispetto a quelli dichiarti nel presente file)
        if (typeof Pru_Id.GetText() == 'undefined' || typeof Search_Col_Id.GetText() == 'undefined' || typeof deNewAssociationDate.GetText() == 'undefined' ||
            Pru_Id.GetText() == '' || Search_Col_Id.GetText() == '' || deNewAssociationDate.GetText() == '') {
            DisplayDialogError('Power Web', s.cpErrorMessage);
        }
        else {
            returnValue = true;
        }

        // ritorno del valore calcolato dalla funzione
        return returnValue;
    }
</script>

<dx:ASPxCallback runat="server" ID="gridSelectionChange" ClientInstanceName="gridSelectionChange" OnCallback="gridSelectionChange_OnCallback"></dx:ASPxCallback>

<table>
    <tr>
        <dx:ASPxGridView ID="gvCol" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvCol_DataBinding" OnCellEditorInitialize="gvCol_CellEditorInitialize" OnAutoFilterCellEditorInitialize="gvCol_AutoFilterCellEditorInitialize"
            OnInitNewRow="gvCol_InitNewRow"
            OnRowValidating="gvCol_RowValidating"
            OnRowInserting="gvCol_RowInserting"
            OnRowUpdating="gvCol_RowUpdating"
            OnRowDeleting="gvCol_RowDeleting"
            OnCustomJSProperties="gvCol_CustomJsProperties"
            OnPageIndexChanged="gvCol_OnPageIndexChanged"
            OnCustomCallback="gvCol_OnCustomCallback">
            <ClientSideEvents SelectionChanged="OnGridSelectionChanged" EndCallback="OnGridEndCallback" />
            <Columns>
                <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
                    <CustomButtons>
                        <dx:GridViewCommandColumnCustomButton ID="add">
                            <Image ToolTip="Add" Url="../Icons/Add/Add.png" />
                        </dx:GridViewCommandColumnCustomButton>
                        <dx:GridViewCommandColumnCustomButton ID="addClone">
                            <Image ToolTip="AddClone" Url="../Icons/Add/Add.png" />
                        </dx:GridViewCommandColumnCustomButton>
                        <dx:GridViewCommandColumnCustomButton ID="delete">
                            <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
                        </dx:GridViewCommandColumnCustomButton>
                        <dx:GridViewCommandColumnCustomButton ID="view">
                            <Image ToolTip="View" Url="../Icons/Search/Search.png" />
                        </dx:GridViewCommandColumnCustomButton>
                    </CustomButtons>
                    <EditButton Visible="True">
                        <Image Url="../Icons/Edit/Edit.png" />
                    </EditButton>
                    <ClearFilterButton Visible="True">
                        <Image Url="../Icons/Undo/Undo.png" />
                    </ClearFilterButton>
                </dx:GridViewCommandColumn>

                <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0">
                    <HeaderTemplate>

                        <dxe:ASPxCheckBox ID="cbAll" runat="server" ClientInstanceName="cbAll" ToolTip="Select all rows" BackColor="White" OnInit="cbAll_Init" OnCustomJSProperties="cbAll_OnCustomJSProperties">
                            <ClientSideEvents CheckedChanged="OnAllCheckedChanged" />
                        </dxe:ASPxCheckBox>
                        <dxe:ASPxCheckBox ID="cbPage" runat="server" ClientInstanceName="cbPage" ToolTip="Select all rows within the page" OnInit="cbPage_Init">
                            <ClientSideEvents CheckedChanged="OnPageCheckedChanged" />
                        </dxe:ASPxCheckBox>
                    </HeaderTemplate>
                    <HeaderStyle HorizontalAlign="Center" />
                </dx:GridViewCommandColumn>

                <dx:GridViewDataSpinEditColumn FieldName="Arrot_Durata_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="ArrotF_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="ArrotI_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataCheckColumn FieldName="Assegni_Famigliari_Col" Visible="False">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataDateColumn FieldName="Assegni_Famigliari_Fine_Col" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Assegni_Famigliari_Inizio_Col" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Proroga_Contratto" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataCheckColumn FieldName="Automunito_Col" Visible="False">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="false">
                    <Settings AllowHeaderFilter="False" />
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Collaboratore" VisibleIndex="10" Width="10%">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Codice_Domicilio_Luogo_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Fiscale_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Iban_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Codice_Nascita_Luogo_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Codice_Residenza_Luogo_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Col_Id" VisibleIndex="1">
                    <Settings AllowHeaderFilter="False" />
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Cognome_Col" Visible="false">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="CognomeNome_Col" VisibleIndex="20" Width="25%">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Disponibilita_Fine_Col" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Disponibilita_Inizio_Col" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Col" Visible="false" ReadOnly="True">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Sorv_San_Col" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Col" Visible="false" ReadOnly="True">
                    <PropertiesDateEdit EditFormat="DateTime" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataCheckColumn FieldName="Disabile_Col" Visible="False">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Col" VisibleIndex="110" Width="5%">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Cap_Col" VisibleIndex="40" Width="5%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Domicilio_Indirizzo_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Domicilio_Interno_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Domicilio_Localita_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Luogo_Col" VisibleIndex="50" Width="15%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Provincia_Col" VisibleIndex="30" Width="5%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Max_Gruppo_Notte_Ril_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Max_Gruppo_Ril_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Max_Ril_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Min_Ril_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Pausa_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fax_1_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fax_1_Rif_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fax_2_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fax_2_Rif_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_1_Inizio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_1_Fine_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_2_Inizio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_2_Fine_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_3_Inizio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_3_Fine_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_4_Inizio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_4_Fine_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_5_Inizio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fascia_Ore_Viaggi_5_Fine_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Flag_Ore_Viaggi_Col_Inizio_Fine" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Flag_INPS_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Flag_NON_Esportare_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Flag_Ore_Viaggi_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Flag_Viaggio_InizioFine_GIS" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataCheckColumn FieldName="Flag_Monte_Ore" Visible="False">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataSpinEditColumn FieldName="GGConsMax_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Indennita_Sanificazione_Col" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Indennita_Trasporto_Col" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataDateColumn FieldName="LastDateActivePru" ReadOnly="True" VisibleIndex="2" />
                <dx:GridViewDataDateColumn FieldName="LastReg" ReadOnly="True" VisibleIndex="5" />
                <dx:GridViewDataTextColumn FieldName="LastPruCode" ReadOnly="True" VisibleIndex="3"></dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="LastPruNSerie" ReadOnly="True" VisibleIndex="4"></dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="LatitudineGps_Col" VisibleIndex="999" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Libretto_Sanitario_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Mattina_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>

                 <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Entrata_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>

                <dx:GridViewDataTextColumn FieldName="Limite_Uscita_Mattina_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Uscita_Pomeriggio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Inizio_Notte_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Livello_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="LongitudineGps_Col" VisibleIndex="999" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Matricola_Col" VisibleIndex="80" Width="7%">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Metodo_Arrotondamento_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Minuti_Arrot_Durata_Fig_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="N_Persone_A_Carico_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataTextColumn FieldName="N_Pos_INAIL_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="N_Pos_INPS_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="N_Pru_Col" VisibleIndex="1" Width="10%" ReadOnly="true" CellStyle-HorizontalAlign="Center">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Nascita_Cap_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataDateColumn FieldName="Nascita_Data_Col" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Nascita_Luogo_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Nascita_Provincia_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Nazionalita_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Nome_Col" Visible="false">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Note_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="OreMaxGG_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataSpinEditColumn FieldName="OreMassime" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Patente_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Prova" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Qualifica_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Quota_PTime_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento1_Col">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento2_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Residenza_Cap_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Residenza_Indirizzo_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Residenza_Interno_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Residenza_Localita_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Residenza_Luogo_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Residenza_Provincia_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Residenza_Provincia_GEN_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Resp_Id" VisibleIndex="90" Width="10%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Retribuzione_Netta_Col" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Retribuzione_Lorda_Col" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Retribuzione_Oraria_Col" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Retribuzione_Straordinaria_Col" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataDateColumn FieldName="Scadenza_Patente_Col" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Sesso_Col" VisibleIndex="60" Width="5%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataCheckColumn FieldName="Singola_Reg" VisibleIndex="90" Width="7%">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Soglia_Arrot_Durata_Fig_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Soglia_Durata_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g">
                    </PropertiesSpinEdit>
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataTextColumn FieldName="Soglia_Minima_Arrotondamento_Durata_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataSpinEditColumn FieldName="SogliaF_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="SogliaI_Col" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Stato_Civile_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataCheckColumn FieldName="Straniero_CEE_Col" Visible="False">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataCheckColumn FieldName="Straniero_Col" Visible="False">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataDateColumn FieldName="Straniero_Scadenza_Permesso_Col" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tab_Orari_Tipo_Id" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_1_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_1_Rif_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_2_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_2_Rif_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_3_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_3_Rif_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_4_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_4_Rif_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Email_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Arrotondamento_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Rapporto_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Contratto_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Titolo_Studio_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="TipoNotturno_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                 <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Uscita_Mattina_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                 <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Uscita_Pomeriggio_Col" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Trattenuta_Vitto_Col" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" DisplayFormatString="g" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Zona_Col" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Pomeriggio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Entrata_Pomeriggio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="HHMMMonte_Minuti" Visible="false" ReadOnly="true" />
                <dx:GridViewDataTextColumn FieldName="Durata_Notturno_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Ritardo_Tolleranza_Minuti_Col" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Inizio_Pomeriggio_Col" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
            </Columns>
        </dx:ASPxGridView>
    </tr>
</table>
<dx:ASPxDateEdit runat="server" ID="__ReferenceDateEdit" ClientVisible="false">
</dx:ASPxDateEdit>

<dx:ASPxFormLayout ID="FlElaborate" runat="server" Width="100%">
    <Items>
        <dx:LayoutGroup Caption="ELABORAZIONI" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td style="left: 0">
                                        <dx:ASPxLabel ID="lblNColSel" runat="server" ClientInstanceName="lblNColSel" Width="100%">
                                        </dx:ASPxLabel>
                                    </td>
                                    <td style="left: 0">
                                        <dx:ASPxLabel ID="lblNColSelValue" runat="server" ClientInstanceName="lblNColSelValue" Width="10%">
                                        </dx:ASPxLabel>
                                    </td>
                                    <tr>
                                        <td style="padding-left: 5px;">
                                            <dx:ASPxLabel ID="lblDal" runat="server" ClientInstanceName="lblDal" Text="07/01/2013" Width="100%">
                                            </dx:ASPxLabel>
                                        </td>
                                        <td style="padding-left: 5px;">
                                            <dx:ASPxDateEdit ID="deFrom" runat="server" ClientInstanceName="deFrom">
                                            </dx:ASPxDateEdit>
                                        </td>
                                        <td style="padding-left: 5px;">
                                            <dx:ASPxLabel ID="lblAl" runat="server" ClientInstanceName="lblDal" Text="07/02/2013" Width="100%">
                                            </dx:ASPxLabel>
                                        </td>
                                        <td style="padding-left: 5px;">
                                            <dx:ASPxDateEdit ID="deTo" runat="server" ClientInstanceName="deTo">
                                            </dx:ASPxDateEdit>
                                        </td>
                                        <td style="padding-left: 50px">
                                            <dx:ASPxButton ID="BtnLaunchElaborate" runat="server" AutoPostBack="False" ClientInstanceName="btnLaunchElaborate" UseSubmitBehavior="False" Width="100%" OnCustomJSProperties="BtnLaunchElaborate_OnCustomJSProperties">
                                                <ClientSideEvents Click="btnLaunchElaborate_onClick" />
                                            </dx:ASPxButton>
                                        </td>
                                        <td style="padding-left: 50px;">
                                            <dx:ASPxButton ID="BtnDeleteTrips" runat="server" AutoPostBack="False" ClientInstanceName="btnDeleteTrips" UseSubmitBehavior="False" Width="100%" OnCustomJSProperties="BtnDeleteTrips_OnCustomJSProperties">
                                                <ClientSideEvents Click="btnDeleteTrips_onClick" />
                                            </dx:ASPxButton>
                                        </td>
                                        <td style="padding-left: 50px;">
                                            <dx:ASPxButton ID="BtnDeleteRoundings" runat="server" AutoPostBack="False" ClientInstanceName="BtnDeleteRoundings" UseSubmitBehavior="False" Width="100%" OnCustomJSProperties="BtnDeleteRoundings_OnCustomJSProperties">
                                                <ClientSideEvents Click="btnDeleteRoundings_onClick" />
                                            </dx:ASPxButton>
                                        </td>
                                    </tr>
                                <tr>
                                    <td style="padding-left: 5px;">
                                        <dx:ASPxLabel runat="server" ID="LblCol" ClientInstanceName="lblCol" />
                                    </td>

                                    <td style="padding-left: 5px;" colspan="3">
                                        <dx:ASPxComboBox runat="server" ID="CmbColToElaborate" ClientInstanceName="cmbColToElaborate" Width="100%" />
                                    </td>
                                    <td style="padding-left: 50px">
                                        <dx:ASPxButton ID="BtnLaunchExport56" runat="server" AutoPostBack="False" ClientInstanceName="btnLaunchExport56" UseSubmitBehavior="False" Width="100%" OnCustomJSProperties="BtnLaunchExport56_OnCustomJSProperties" OnClick="BtnLaunchExport56_OnClick">
                                            <ClientSideEvents Click="btnLaunchExport56_onClick" />
                                        </dx:ASPxButton>
                                    </td>
                                    <td style="padding-left: 50px;">
                                        <dx:ASPxButton ID="BtnTrips" runat="server" AutoPostBack="False" ProcessOnServer="False"  ClientInstanceName="btnTrips" OnCustomJSProperties="BtnTrips_CustomJSProperties" UseSubmitBehavior="False" Width="100%">
                                            <ClientSideEvents Click="btnTrips_onClick" />
                                        </dx:ASPxButton>
                                    </td>
                                    <td style="padding-left: 50px">
                                        <dx:ASPxButton ID="BtnRoundings" runat="server" AutoPostBack="False" ClientInstanceName="BtnRoundings" UseSubmitBehavior="False" Width="100%" OnCustomJSProperties="BtnRoundings_OnCustomJSProperties">
                                            <ClientSideEvents Click="btnRoundings_onClick" />
                                        </dx:ASPxButton>
                                    </td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
                <dx:LayoutItem Caption="Avanzamento" HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td style="vertical-align: top">
                                        <dx:ASPxProgressBar ID="ASPxProgressBarElaborate" runat="server" Minimum="0" Maximum="100" Position="0" Width="600px" ClientInstanceName="pbProgress">
                                        </dx:ASPxProgressBar>
                                    </td>
                                    <td style="width: 100%; float: left">
                                        <dx:ASPxLabel ID="lblResult" Text="MESSAGGIO" runat="server" ClientInstanceName="lblResult"></dx:ASPxLabel>
                                    </td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>

            <SettingsItemHelpTexts Position="Bottom"></SettingsItemHelpTexts>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>

<dx:ASPxFormLayout ID="flImport" runat="server" Width="80%" Visible="true">
    <Items>
        <dx:LayoutGroup Caption="Import Anagrafiche" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption="File da importare" HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td>
                                        <dx:ASPxUploadControl ID="uploader" Width="600px" runat="server" ClientInstanceName="uploader"
                                            ShowProgressPanel="True" OnFileUploadComplete="upldImport_FileUploadComplete" FileUploadMode="OnPageLoad"
                                            CssClass="btnInline">
                                            <ClientSideEvents TextChanged="function(s, e) { UpdateUploadButton(); }" FileUploadStart="function(s, e) { Uploader_OnUploadStart(); }"
                                                FileUploadComplete="function(s, e) { Uploader_OnFilesUploadComplete(e); }" />
                                            <ValidationSettings AllowedFileExtensions=".csv,.xls,.xlsx">
                                            </ValidationSettings>
                                        </dx:ASPxUploadControl>
                                    </td>
                                    <td style="width: 30%; padding-left: 5px;">
                                        <dx:ASPxButton ID="ASPxButton1" Width="100%" runat="server" ClientInstanceName="btnUpload" HorizontalAlign="Center" VerticalAlign="Top"
                                            ClientEnabled="False" UseSubmitBehavior="false" Text="Import">
                                            <ClientSideEvents Click="function(s, e) { uploader.Upload(); }" />
                                        </dx:ASPxButton>
                                    </td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
                <dx:LayoutItem Caption="Avanzamento" HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td style="vertical-align: top">
                                        <dx:ASPxProgressBar ID="ImportProgressBar" runat="server" Minimum="0" Maximum="100" Position="0" Width="600px" ClientInstanceName="pbProgress">
                                        </dx:ASPxProgressBar>
                                    </td>
                                    <td style="padding-left: 5px;">
                                        <dx:ASPxLabel ID="ASPxLabel1" runat="server" Width="100%" ClientInstanceName="lblResult">
                                        </dx:ASPxLabel>
                                    </td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>

<dx:ASPxFormLayout ID="FlAddNewAssociation" runat="server" Width="80%">
    <Items>
        <dx:LayoutGroup Caption="AGGIUNGI ASSOCIAZIONE" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblNewAssociationPru" ClientInstanceName="lblNewAssociationPru" />
                                    </td>
                                    <td>
                                        <dx:ASPxComboBox runat="server" ID="CmbPruToAssociate" ClientInstanceName="cmbPruToAssociate" Width="200" />
                                    </td>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblNewAssociationCol" ClientInstanceName="lblNewAssociationCol" />
                                    </td>
                                    <td>
                                        <dx:ASPxComboBox runat="server" ID="CmbColToAssociate" ClientInstanceName="cmbColToAssociate" Width="200" />
                                    </td>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblNewAssociationDate" ClientInstanceName="lblNewAssociationDate" />
                                    </td>
                                    <td>
                                        <dx:ASPxDateEdit runat="server" ID="DeNewAssociationDate" ClientInstanceName="deNewAssociationDate" />
                                    </td>
                                    <td>
                                        <dx:ASPxButton runat="server" ID="BtnAddNewAssociation" ClientInstanceName="btnAttNewAssociation" AutoPostBack="False"
                                            OnCustomJSProperties="BtnAddNewAssociation_OnCustomJSProperties">
                                            <ClientSideEvents Click="function (s, e) { if (validateAddNewAssociation(s)) grid.PerformCallback('AddNewAssociation'); }" />
                                        </dx:ASPxButton>
                                    </td>
                                </tr>
                            </table>
                        </dx:LayoutItemNestedControlContainer>
                    </LayoutItemNestedControlCollection>
                </dx:LayoutItem>
            </Items>
        </dx:LayoutGroup>
    </Items>
</dx:ASPxFormLayout>

<div style="padding: 8px; margin: 5px;">
    <dx:ASPxRoundPanel ID="InitMinutesAmmountPanel" runat="server" ShowCollapseButton="true" Collapsed="true" Width="80%">
        <PanelCollection>
            <dx:PanelContent>
                <table>
                    <tr>
                        <td>
                            <dx:ASPxLabel runat="server" ID="LblNewMinutesAmmountValue" />
                        </td>
                        <td style="width: 50px">
                            <dx:ASPxTextBox runat="server" ID="TxtNewMinutesAmmountValue" Text="000:00">
                                <MaskSettings Mask="000:00" IncludeLiterals="None" />
                            </dx:ASPxTextBox>
                        </td>
                        <td>
                            <dx:ASPxButton runat="server" ID="BtnInitMinutesAmmountForSelecteds" AutoPostBack="false" UseSubmitBehavior="false">
                                <ClientSideEvents Click="function(s, e) { grid.PerformCallback('initSelectedsMinutesAmmount'); }" />
                            </dx:ASPxButton>
                        </td>
                    </tr>
                    <tr>
                        <td></td>
                        <td>
                            <dx:ASPxCheckBox runat="server" ID="ChkBoxNegativeDuration" ClientInstanceName="chkBoxNegativeDuration" Checked="false"/>
                        </td>
                        <td></td>
                    </tr>
                </table>
            </dx:PanelContent>
        </PanelCollection>
    </dx:ASPxRoundPanel>
</div>


<div>
    <dx:ASPxCallback ID="cPing" ClientInstanceName="cPing" runat="server" OnCallback="cPing_Callback">
        <ClientSideEvents CallbackComplete="cPing_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxTimer ID="tPing" ClientInstanceName="tPing" runat="server" Enabled="false" Interval="1000">
        <ClientSideEvents Tick="tPing_OnTick" />
    </dx:ASPxTimer>
    <dx:ASPxCallback ID="cTrips" ClientInstanceName="cTrips" runat="server" OnCallback="cTrips_Callback">
        <ClientSideEvents CallbackComplete="cComplete_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxCallback ID="cRoundings" ClientInstanceName="cRoundings" runat="server" OnCallback="cRoundings_Callback">
        <ClientSideEvents CallbackComplete="cComplete_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxTimer ID="tPingLoadingExport" ClientInstanceName="tPingLoadingExport" runat="server" Enabled="false" Interval="500">
        <ClientSideEvents Tick="tPingLoadingExport_OnTick" />
    </dx:ASPxTimer>
    <dx:ASPxCallback ID="cPingLoadingExport" ClientInstanceName="cPingLoadingExport" runat="server" OnCallback="cPingLoadingExport_OnCallback">
        <ClientSideEvents CallbackComplete="cPingLoadingExport_OnCallbackComplete" />
    </dx:ASPxCallback>

    <dx:ASPxTimer ID="tPingElaborate" ClientInstanceName="tPingElaborate" runat="server" Enabled="false" Interval="1000">
        <ClientSideEvents Tick="tPingElaborate_OnTick" />
    </dx:ASPxTimer>
    <dx:ASPxCallback ID="cElaborate" ClientInstanceName="cElaborate" runat="server" OnCallback="cElaborate_OnCallback">
        <ClientSideEvents CallbackComplete="cEalborateComplete_OnCallbackComplete" />
    </dx:ASPxCallback>
</div>

<dx:ASPxPopupControl ID="pcShowMap" runat="server" Height="400px" LoadContentViaCallback="OnPageLoad"
    Width="600px" HeaderText="Map popup" ClientSideEvents-Shown="newinitMap" PopupElementID="btnShowMap" CloseAction="OuterMouseClick" ShowCloseButton="false">
    <ContentCollection>
        <dx:PopupControlContentControl>
            <div id='bingMap' style="position: relative; width: 640px; height: 400px;"></div>
        </dx:PopupControlContentControl>
    </ContentCollection>
</dx:ASPxPopupControl>

<div>
    <dx:ASPxCallback ID="cUplImportCommand" ClientInstanceName="cUplImportCommand" runat="server" OnCallback="cUplImportCommand_Callback">
        <ClientSideEvents CallbackComplete="cUplImportCommand_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxCallback ID="cUplImportPing" ClientInstanceName="cUplImportPing" runat="server" OnCallback="cUplImportPing_Callback">
        <ClientSideEvents CallbackComplete="cUplImportPing_OnCallbackComplete" />
    </dx:ASPxCallback>
    <dx:ASPxTimer ID="tUplImportPing" ClientInstanceName="tUplImportPing" runat="server" Enabled="false" Interval="1000">
        <ClientSideEvents Tick="tUplImportPing_OnTick" />
    </dx:ASPxTimer>
</div>

