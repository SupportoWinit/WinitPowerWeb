<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CantModule.ascx.cs"
    Inherits="PowerWeb.Modules.CantModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxFormLayout" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxPopupControl" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallback" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxUploadControl" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxTimer" TagPrefix="dx" %>

<script type="text/javascript" src="http://www.bing.com/api/maps/mapcontrol"></script>

<script type="text/javascript">
    //#region --------------- Gestione CASCADE----------------------------------------
    function OnNascLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var nascLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Provincia_Nascita_Can"));
        var nascLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Cap_Nascita_Can"));
        var nascLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Codice_Luogo_Nascita_Can"));
        nascLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        nascLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        nascLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Luogo_Tab_Comuni"));
    }
    function OnLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var LuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Provincia_Can"));
        var LuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Cap_Can"));
        var LuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Codice_Luogo_Residenza_Can"));
        console.log(LuogoProvCmb);
        console.log(LuogoCapCmb);
        console.log(LuogoCodiceCmb);
        LuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        LuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        LuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Luogo_Tab_Comuni"));
    }
    function OnNascCodLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var nascCodLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Provincia_Nascita_Can"));
        var nascCodLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Cap_Nascita_Can"));
        var nascCodLuogoCmb = ASPxClientComboBox.Cast(grid.GetEditor("Luogo_Nascita_Can"));
        nascCodLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        nascCodLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        nascCodLuogoCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Luogo_Tab_Comuni"));
    }
    function OnCodLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var CodLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Provincia_Can"));
        var CodLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Cap_Can"));
        var CodLuogoCmb = ASPxClientComboBox.Cast(grid.GetEditor("Luogo_Can"));
        CodLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        CodLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        CodLuogoCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Luogo_Tab_Comuni"));
    }

    function OnDomLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var domLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Provincia_Can"));
        var domLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Cap_Can"));
        var domLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Codice_Domicilio_Luogo_Can"));
        domLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        domLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        domLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Luogo_Tab_Comuni"));
    }

    function OnDomCodLuogoChanged(s, e) {
        var currentCmb = ASPxClientComboBox.Cast(s);
        var domCodLuogoProvCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Provincia_Can"));
        var domCodLuogoCapCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Cap_Can"));
        var domCodLuogoCodiceCmb = ASPxClientComboBox.Cast(grid.GetEditor("Domicilio_Luogo_Can"));
        domCodLuogoProvCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Codice_Prov_Tab_Comuni"));
        domCodLuogoCapCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Cap_Tab_Comuni"));
        domCodLuogoCodiceCmb.SetValue(currentCmb.GetSelectedItem().GetColumnText("Luogo_Tab_Comuni"));
    }


    //#endregion     

    //#region --------------- Gestione Area CARICAMENTO MAPPA DA BING ---------------------------------------

    var bingMap = null;
    var searchManager = null;
    var isFirstTimeRequest = true;

    function loadMap() {
        isFirstTimeRequest = true;
        //
        var currentGrid = ASPxClientGridView.Cast(grid);
        var currentLat = currentGrid.GetEditValue("LatitudineGps_Can");
        var currentLon = currentGrid.GetEditValue("LongitudineGps_Can");

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
            console.log(currentLatValue);
            console.log(currentLonValue);
            if (currentLatValue != 0.0 && currentLonValue != 0.0) {
                var pushpinOptions = { draggable: true };
                var pushpin = new Microsoft.Maps.Pushpin(bingMap.getCenter(), pushpinOptions);
                pushpin = new Microsoft.Maps.Pushpin(new Microsoft.Maps.Location(currentLatValue, currentLonValue), pushpinOptions);
                var pushpindragend = Microsoft.Maps.Events.addHandler(pushpin, 'dragend', OnEndDragPushpin);
                bingMap.setView({ zoom: 17, center: new Microsoft.Maps.Location(currentLatValue, currentLonValue) });
                bingMap.entities.push(pushpin);
            }
            else {
                LoadSearchModule();
            }
                
        }
    }

    function createSearchManager() {
        Microsoft.Maps.loadModule('Microsoft.Maps.Search', function () {
            searchManager = new Microsoft.Maps.Search.SearchManager(bingMap);
        });
    }

    function LoadSearchModule() {
        Microsoft.Maps.loadModule('Microsoft.Maps.Search', { callback: geocodeRequest })
    }

    function geocodeRequest() {
        createSearchManager();
        var currentGrid = ASPxClientGridView.Cast(grid);
        var currentAddress = currentGrid.GetEditValue("Indirizzo_Can");
        var currentPlace = currentGrid.GetEditValue("Luogo_Can");
        var currentZip = currentGrid.GetEditValue("Cap_Can");
        var where = "";
        if (currentAddress && isFirstTimeRequest)
            where += currentAddress;
        if (currentPlace)
            where += " " + currentPlace;
        if (currentZip)
            where += " " + currentZip;
        console.log(where);
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
                console.log(pushpin);
                var currentGrid = ASPxClientGridView.Cast(grid);
                var currentLatString = pushpin.geometry.y.toString().replace(".", ",");
                var currentLonString = pushpin.geometry.x.toString().replace(".", ",");
                var currentLat = currentGrid.SetEditValue("LatitudineGps_Can", currentLatString);
                var currentLon = currentGrid.SetEditValue("LongitudineGps_Can", currentLonString);

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
        var currentLatString = e.location.latitude.toString().replace(".", ",");
        var currentLonString = e.location.longitude.toString().replace(".", ",");
        var currentLat = currentGrid.SetEditValue("LatitudineGps_Can", currentLatString);
        var currentLon = currentGrid.SetEditValue("LongitudineGps_Can", currentLonString);
    }
    //#endregion


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


    // -------------------------- GESTIONE DELL'INSERIMENTO DI NUOVE ASSOCIAZIONI -------------------------------------------

    // effettua il controllo dei valori di input della procedura di inserimento
    // delle nuove associazioni e ritorna true in caso la validazione abbia dato esito positivo
    // e false in caso la validazione abbia dato esito negativo
    function validateAddNewAssociation(s) {
        // inizializzazione del valore di ritorno della funzione
        var returnValue = false;

        // devono essere valorizzati tutti e tre i campi che indicano codice unità fissa, id cantiere e data
        // (i nomi dei combobox sono modificati dalla fillComboboxes e quindi cambiano rispetto a quelli dichiarti nel presente file)
        if (typeof Fru_Id.GetText() == 'undefined' || typeof Search_Cant_Id.GetText() == 'undefined' || typeof deNewAssociationDate.GetText() == 'undefined' ||
            Fru_Id.GetText() == '' || Search_Cant_Id.GetText() == '' || deNewAssociationDate.GetText() == '') {
            DisplayDialogError('Power Web', s.cpErrorMessage);
        }
        else {
            returnValue = true;
        }

        // ritorno del valore calcolato dalla funzione
        return returnValue;
    }

    // scatenato al termine del callback della griglia per l'inserimento di una nuova associazione
    function gvCant_EndCallback(s, e) {
        if (typeof s.cpErrorMessage != 'undefined' && s.cpErrorMessage != '') {
            var errorMessage = s.cpErrorMessage;
            s.cpErrorMessage = '';
            DisplayDialogError('Errori inserimento', errorMessage);
        }
    }

</script>
<table>
    <tr>
        <dx:ASPxGridView ID="gvCant" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvCant_DataBinding"
            OnCellEditorInitialize="gvCant_CellEditorInitialize"
            OnAutoFilterCellEditorInitialize="gvCant_AutoFilterCellEditorInitialize"
            OnInitNewRow="gvCant_InitNewRow"
            OnRowValidating="gvCant_RowValidating"
            OnRowInserting="gvCant_RowInserting"
            OnRowUpdating="gvCant_RowUpdating"
            OnRowDeleting="gvCant_RowDeleting"
            OnCustomCallback="gvCant_OnCustomCallback">
            <ClientSideEvents EndCallback="gvCant_EndCallback" />
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
                <dx:GridViewDataDateColumn FieldName="LastDateActiveFru" ReadOnly="True" VisibleIndex="1" />
                <dx:GridViewDataTextColumn FieldName="LastFruCode" ReadOnly="True" VisibleIndex="2"></dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="LastFruNSerie" ReadOnly="True" VisibleIndex="2"></dx:GridViewDataTextColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Arrot_Durata_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Canone_Mensile_Fascia1_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Canone_Mensile_Fascia2_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" Visible="True">
                    <Settings AllowHeaderFilter="False" />
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Cap_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Cap_Nascita_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Cli_Id" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Cod_Fisc_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Cantiere" VisibleIndex="20" Width="10%">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Luogo_Nascita_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Luogo_Residenza_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Voucher_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Cognome_Assistito_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="CognomeNome_Cli" Visible="false">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Contributo_Disabili_Fascia1_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Contributo_Disabili_Fascia2_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Costo_Mezzora_Prescuola_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Costo_Orario_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Isee_Can" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Nascita_Can" Visible="False">
                    <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_1_Can" Visible="False">
                    <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_2_Can" Visible="False">
                    <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_3_Can" Visible="False">
                    <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_4_Can" Visible="False">
                    <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Fine_5_Can" Visible="False">
                    <PropertiesDateEdit EditFormat="Date" EditFormatString="dd/MM/yy" DisplayFormatString="dd/MM/yy" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Cap_Can" VisibleIndex="40" Width="5%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Domicilio_Indirizzo_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Domicilio_Interno_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Domicilio_Localita_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Luogo_Can" VisibleIndex="50" Width="15%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Codice_Domicilio_Luogo_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Domicilio_Provincia_Can" VisibleIndex="30" Width="5%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_1_Can" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_2_Can" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_3_Can" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_4_Can" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Rapporto_Inizio_5_Can" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="Data_Registrazione_Can" Visible="False" ReadOnly="true">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="DataOraUltimaModifica_Can" Visible="False" ReadOnly="true">
                    <PropertiesDateEdit EditFormat="DateTime" />
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataDateColumn FieldName="DataVarGps_Can" Visible="False">
                </dx:GridViewDataDateColumn>
                <dx:GridViewDataTextColumn FieldName="Descrizione_Can" VisibleIndex="30" Width="20%">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Can" VisibleIndex="110" Width="5%">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Max_Gruppo_Notte_Ril_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Max_Gruppo_Ril_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Max_Ril_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Min_Ril_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fax_1_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fax_1_Rif_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fax_2_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Fax_2_Rif_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Fil_Id" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Mattina_Cant" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Flag_NON_Esportare_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="FlagGps_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Gestione_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Indirizzo_Can" VisibleIndex="70" Width="25%">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="LatitudineGps_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Inizio_Notte_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Entrata_Pomeriggio_Cant" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Uscita_Mattina_Cant" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Limite_Uscita_Pomeriggio_Cant" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Livello_Assistito_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="LongitudineGps_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Luogo_Can" VisibleIndex="60" Width="20%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Luogo_Nascita_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataCheckColumn FieldName="Mensa_Can" Visible="False">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Metodo_Arrotondamento_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Minuti_Tolleranza_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Minuti_Tolleranza_F_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Nazione_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Nazione_Nascita_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Nome_Assistito_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Note_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="N_Fru_Cant" VisibleIndex="1" Width="10%" ReadOnly="true" CellStyle-HorizontalAlign="Center">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Nr_Isee_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Numero_Bimbi_Fascia_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Numero_GG_Lavorativi" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_Fascia1_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_Fascia2_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PreScuola_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PostScuola_1Mezzora_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Numero_Utenti_PostScuola_2Mezzora_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Note_Unita_Associata" Visible="True">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Ore_Massime_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Can" Visible="False">
                    <PropertiesSpinEdit DisplayFormatString="P" DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_1_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_2_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_3_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_4_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Percentuale_Servizio_5_Can" Visible="False">
                    <PropertiesSpinEdit DecimalPlaces="2" />
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Provincia_Can" VisibleIndex="40" Width="5%" CellStyle-HorizontalAlign="Center">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Provincia_Nascita_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataSpinEditColumn FieldName="RaggioGps_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento1_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Raggruppamento2_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Residenza_Interno_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Residenza_Localita_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Sesso_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataCheckColumn FieldName="Singola_Reg" VisibleIndex="90" Width="7%">
                </dx:GridViewDataCheckColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Soglia_Arrot_Fig_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Soglia_Arrot_Fig_F_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                <dx:GridViewDataSpinEditColumn FieldName="Soglia_Durata_Can" Visible="False">
                </dx:GridViewDataSpinEditColumn>
                 <dx:GridViewDataTextColumn FieldName="Durata_Notturno_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_1_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_1_Rif_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_2_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_2_Rif_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_3_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_3_Rif_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_4_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Telefono_4_Rif_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Arrotondamento_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Calcolo_Viaggi_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Cantiere_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Interv_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_1_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_2_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_3_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_4_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipo_Servizio_5_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tipologia_Can" VisibleIndex="10" Width="7%">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="TipoNotturno_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tolleranza_Limite_Uscita_Mattina_Cant" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tolleranza_Limite_Uscita_Pomeriggio_Cant" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno1_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno2_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno3_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno4_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno5_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno6_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno7_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno8_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno9_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataTimeEditColumn FieldName="Turno10_Can" Visible="False">
                </dx:GridViewDataTimeEditColumn>
                <dx:GridViewDataDateColumn FieldName="LastReg" ReadOnly="True" VisibleIndex="5" />
                <dx:GridViewDataComboBoxColumn FieldName="Zona_Can" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Tab_Orari_Tipo_Id" Visible="False">
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Frazione_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Durata_Notturno_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Tolleranza_Limite_Entrata_Pomeriggio_Cant" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Tipo_Attivita_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Tempo_Attivita_Can" Visible="False">
                    <PropertiesTextEdit>
                        <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                        <MaskSettings Mask="00:00" IncludeLiterals="None" />
                    </PropertiesTextEdit>
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Commessa_Can" Visible="False">
                </dx:GridViewDataTextColumn>
                <dx:GridViewDataTextColumn FieldName="Codice_Gestionale_Can" Visible="False">
                </dx:GridViewDataTextColumn>

            </Columns>
        </dx:ASPxGridView>
    </tr>
</table>


<dx:ASPxFormLayout ID="flImportMosaico" runat="server" Width="80%" Visible="false" Style="margin-top: 0px">
    <Items>
        <dx:LayoutGroup Caption="Import Anagrafiche" SettingsItemHelpTexts-Position="Bottom">
            <Items>
                <dx:LayoutItem Caption=" " HelpText="">
                    <LayoutItemNestedControlCollection>
                        <dx:LayoutItemNestedControlContainer>
                            <table>
                                <tr>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="filialeLbl" ClientInstanceName="filialeLbl" Text="Filiale:" />
                                    </td>
                                    <td>
                                        <dx:ASPxComboBox ID="cmbFil" ClientInstanceName="cmbFil" runat="server" Width="600px">
                                        </dx:ASPxComboBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="ASPxLabel2" ClientInstanceName="fileDaImportareLbl" Text="File da importare:" />
                                    </td>
                                    <td>
                                        <dx:ASPxUploadControl ID="ASPxUploadControl1" Width="600px" runat="server" ClientInstanceName="uploader"
                                            ShowProgressPanel="True" OnFileUploadComplete="upldImport_FileUploadComplete" FileUploadMode="OnPageLoad"
                                            CssClass="btnInline">
                                            <ClientSideEvents TextChanged="function(s, e) { UpdateUploadButton(); }" FileUploadStart="function(s, e) { Uploader_OnUploadStart(); }"
                                                FileUploadComplete="function(s, e) { Uploader_OnFilesUploadComplete(e); }" />
                                            <ValidationSettings AllowedFileExtensions=".csv,.xls,.xlsx">
                                            </ValidationSettings>
                                        </dx:ASPxUploadControl>
                                    </td>
                                    <td style="width: 30%; padding-left: 5px;">
                                        <dx:ASPxButton ID="ASPxButton2" Width="100%" runat="server" ClientInstanceName="btnUpload" HorizontalAlign="Center" VerticalAlign="Top"
                                            ClientEnabled="False" UseSubmitBehavior="false" Text="Import">
                                            <ClientSideEvents Click="function(s, e) { uploader.Upload(); }" />
                                        </dx:ASPxButton>
                                    </td>
                                    <td style="visibility: hidden;">
                                        <dx:ASPxButton runat="server" Text="Aggiusta codici" OnClick="OnClick"></dx:ASPxButton>
                                    </td>
                                </tr>
                                 <tr>
                                      <td>
                                        <dx:ASPxLabel runat="server" ID="ASPxLabel3" ClientInstanceName="avanzamentoLbl" Text="Avanzamento:" />
                                    </td>
                                    <td style="vertical-align: top">
                                        <dx:ASPxProgressBar ID="ASPxProgressBar1" runat="server" Minimum="0" Maximum="100" Position="0" Width="600px" ClientInstanceName="pbProgress">
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
                                        <dx:ASPxLabel runat="server" ID="LblNewAssociationFru" ClientInstanceName="lblNewAssociationFru" />
                                    </td>
                                    <td>
                                        <dx:ASPxComboBox runat="server" ID="CmbFruToAssociate" ClientInstanceName="cmbFruToAssociate" Width="200" />
                                    </td>
                                    <td>
                                        <dx:ASPxLabel runat="server" ID="LblNewAssociationCant" ClientInstanceName="lblNewAssociationCol" />
                                    </td>
                                    <td>
                                        <dx:ASPxComboBox runat="server" ID="CmbCantToAssociate" ClientInstanceName="cmbCantToAssociate" Width="200" />
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

<dx:ASPxPopupControl ID="pcShowMap" runat="server" Height="400px" LoadContentViaCallback="OnPageLoad"
    Width="600px" HeaderText="Map popup" ClientSideEvents-Shown="loadMap" PopupElementID="btnShowMap" CloseAction="OuterMouseClick" ShowCloseButton="false">
    <ContentCollection>
        <dx:PopupControlContentControl>
            <div id='bingMap' style="position: relative; width: 640px; height: 400px;"></div>
        </dx:PopupControlContentControl>
    </ContentCollection>
</dx:ASPxPopupControl>
<dx:ASPxDateEdit runat="server" ID="__ReferenceDateEdit" ClientVisible="false">
</dx:ASPxDateEdit>

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
