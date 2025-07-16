<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Reg_VMModule.ascx.cs" Inherits="PowerWeb.Modules.Reg_VMModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPopupControl" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<script type="text/javascript" src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script type="text/javascript">

    var isEditPending = false;

    function setEditGridVisible(visibility) {

        if (visibility) {
            divEdit.style.display = 'block';
            isEditPending = false;
        }
        else {
            divEdit.style.display = 'none';
            isEditPending = true;
        }
    }

    function setEditGridVisibilityAndMessage(s) {

        lblCollaboratore.SetText(s.cpCurrentColId);
        lblDataReg.SetText(s.cpCurrentData_Reg);

        if (s.cpErrorString != null) {
            DisplayDialogError("Power", s.cpErrorString);
        }

        setEditGridVisible(s.cpIsToShowEditGrid);

        if (!s.cpIsToShowEditGrid && cbDoRefresh.GetChecked())
            gvRegVMPanel.PerformCallback('refresh');
    }

    function addEditRow() {

        if (isEditPending)
            gvRegVMEdit.PerformCallback('empty');
        else
            gvRegVMEdit.PerformCallback('add');

        setEditGridVisible(true);

        isEditPending = false;
    }

    function OnCustomMultiEditButtonClick(s, e) {

        var currentGrid = ASPxClientGridView.Cast(s);

        if (e.buttonID == 'editMultiRow') {
            var currKey = currentGrid.GetRowKey(e.visibleIndex);
            gvRegVMEdit.PerformCallback('initMulti|' + currKey);
            setEditGridVisible(true);
            divInclude.style.display = 'block';
        }
    }

    //funzione client che permette la cancellazione del combobox
    function onCustomEditButtonComboBoxClick(s, e) {
        var combo = ASPxClientComboBox.Cast(s);
        combo.SetText(" ");
        combo.ShowDropDown();
        combo.SetSelectedItem(null);
        combo.SetSelectedIndex(-1);
        combo.PerformCallback();

    }

    function OnCustomSingleEditButtonClick(s, e) {

        var currentGrid = ASPxClientGridView.Cast(s);

        if (e.buttonID == 'editSingleRow') {
            var currKey = currentGrid.GetRowKey(e.visibleIndex);
            gvRegVMEdit.PerformCallback('initSingle|' + currKey);
            setEditGridVisible(true);
            divInclude.style.display = 'none';
        }
    }

    function OnCustomAddCloneMultiButtonClick(s, e) {

        var currentGrid = ASPxClientGridView.Cast(s);

        if (e.buttonID == 'addCloneMulti') {
            var currKey = currentGrid.GetRowKey(e.visibleIndex);
            gvRegVMEdit.PerformCallback('initClone|' + currKey);
            setEditGridVisible(true);
        }
    }

    function cbIncludeReg_OnCheckedChanged(s, e) {
        gvRegVMEdit.PerformCallback('includeReg');
    }

    function gvRegV_OnEndCallback(s, e) {
        var currentGrid = ASPxClientGridView.Cast(grid);
        var filterValid = currentGrid.cpIsFilterValid;
        if (currentGrid.cpIsFilterValid == false && currentGrid.cpFilterError != "") {
            DisplayDialogInfo('Power', currentGrid.cpFilterError);
            currentGrid.cpIsFilterValid = !currentGrid.cpIsFilterValid;
        }
    }

    // -------------------- GESTIONE VISUALIZZAZIONE CARTINA REGISTRAZIONI -----------------------------

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
            var currentLat = currentGrid.GetEditValue("LatitudineGps_Can");
            var currentLon = currentGrid.GetEditValue("LongitudineGps_Can");
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
        var currentLatE = currentGrid.GetEditValue("Registrazione_Lat_Orig_E");
        var currentLonE = currentGrid.GetEditValue("Registrazione_Long_Orig_E");
        var currentLatU = currentGrid.GetEditValue("Registrazione_Lat_Orig_U");
        var currentLonU = currentGrid.GetEditValue("Registrazione_Long_Orig_U");

        var mapElement = document.getElementById('bingMap');

        if (bingMap) {
            bingMap.dispose();
            bingMap = null;
        }

        var bingKey = "<%= BingKey %>";

        bingMap = new Microsoft.Maps.Map(mapElement, { credentials: bingKey });
        bingMap.setView({ mapTypeId: Microsoft.Maps.MapTypeId.road });


        if ((currentLatE && currentLonE) || (currentLatU && currentLonU)) {
            var currentLatValueE = 0.0;
            if (currentLatE != null)
                currentLatValueE = parseFloat(currentLatE.replace(",", "."));
            var currentLonValueE = 0.0;
            if (currentLonE != null)
                currentLonValueE = parseFloat(currentLonE.replace(",", "."));
            var currentLatValueU = 0.0;
            if (currentLatU != null)
                currentLatValueU = parseFloat(currentLatU.replace(",", "."));
            var currentLonValueU = 0.0;
            if (currentLonU != null)
                currentLonValueU = parseFloat(currentLonU.replace(",", "."));

            if ((currentLatValueE != 0.0 && currentLonValueE != 0.0) || (currentLatValueU != 0.0 && currentLonValueU != 0.0)) {
                if (currentLatValueE != 0.0 && currentLonValueE != 0.0) {
                    var pushpinOptions = { draggable: true };
                    var pushpin = new Microsoft.Maps.Pushpin(bingMap.getCenter(), pushpinOptions);
                    pushpin = new Microsoft.Maps.Pushpin(new Microsoft.Maps.Location(currentLatValueE, currentLonValueE), pushpinOptions);
                    bingMap.setView({ zoom: 17, center: new Microsoft.Maps.Location(currentLatValueE, currentLonValueE) });
                    bingMap.entities.push(pushpin);
                }
                if (currentLatValueU != 0.0 && currentLonValueU != 0.0) {
                    var pushpinOptions2 = { draggable: true };
                    var pushpin2 = new Microsoft.Maps.Pushpin(bingMap.getCenter(), pushpinOptions2);
                    pushpin2 = new Microsoft.Maps.Pushpin(new Microsoft.Maps.Location(currentLatValueU, currentLonValueU), pushpinOptions2);
                    bingMap.setView({ zoom: 17, center: new Microsoft.Maps.Location(currentLatValueU, currentLonValueU) });
                    bingMap.entities.push(pushpin2);
                }

            }
        }
    }

    // -------------------- FINE GESTIONE VISUALIZZAZIONE CARTINA REGISTRAZIONI ------------------------------

</script>

<dx:ASPxCallbackPanel ID="gvRegVMPanel" runat="server" Width="100%" ClientInstanceName="gvRegVMPanel"
    OnCallback="gvRegVMPanel_Callback">
    <PanelCollection>
        <dx:PanelContent ID="PanelContent3" runat="server" SupportsDisabledAttribute="True">
            <dx:ASPxGridView ID="gvRegVM" runat="server" AutoGenerateColumns="False" Width="100%"
                OnDataBinding="gvRegVM_DataBinding"
                OnRowInserting="gvRegVM_RowInserting"
                OnRowUpdating="gvRegVM_RowUpdating"
                OnRowDeleting="gvRegVM_RowDeleting"
                OnRowValidating="gvRegVM_RowValidating"
                OnInitNewRow="gvRegVM_InitNewRow"
                OnCellEditorInitialize="gvRegVM_CellEditorInitialize"
                OnAutoFilterCellEditorInitialize="gvRegVM_AutoFilterCellEditorInitialize"
                ClientSideEvents-EndCallback="gvRegV_OnEndCallback">
                <Columns>
                    <dx:GridViewCommandColumn VisibleIndex="0" Width="120px" ButtonType="Image">
                        <CustomButtons>
                            <dx:GridViewCommandColumnCustomButton ID="editSingleRow">
                                <Image Url="../Icons/Edit/EditSpeed.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="add">
                                <Image ToolTip="Add" Url="../Icons/Add/Add.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="addClone">
                                <Image ToolTip="Clone" Url="../Icons/Add/Add.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="delete">
                                <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="view">
                                <Image ToolTip="View" Url="../Icons/Search/Search.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="editMultiRow">
                                <Image ToolTip="EditMultiRow" Url="../Icons/Edit/EditMulti.png" />
                            </dx:GridViewCommandColumnCustomButton>
                        </CustomButtons>
                        <EditButton Visible="True">
                            <Image Url="../Icons/Edit/Edit.png" />
                        </EditButton>
                        <ClearFilterButton Visible="True">
                            <Image Url="../Icons/Cancel/Cancel.png" />
                        </ClearFilterButton>
                    </dx:GridViewCommandColumn>
                    <dx:GridViewDataTextColumn FieldName="RegE" Visible="False" EditFormSettings-Visible="False" ShowInCustomizationForm="false">
                    </dx:GridViewDataTextColumn>
                </Columns>
            </dx:ASPxGridView>
        </dx:PanelContent>
    </PanelCollection>
</dx:ASPxCallbackPanel>


<div id="divEdit" style="display: none; width: 100%">

    <div id="divInclude" style="align-content: center; margin-top: 10px; display: block;">
        <dx:ASPxCheckBox ID="cbIncludeActivity" runat="server" ClientInstanceName="cbIncludeActivity" CssClass="headerButtons" Text="Includi Attività">
            <ClientSideEvents CheckedChanged="cbIncludeReg_OnCheckedChanged" />
        </dx:ASPxCheckBox>
        <dx:ASPxCheckBox ID="cbIncludePass" runat="server" ClientInstanceName="cbIncludePass" CssClass="headerButtons" Text="Includi Passaggi">
            <ClientSideEvents CheckedChanged="cbIncludeReg_OnCheckedChanged" />
        </dx:ASPxCheckBox>
        <dx:ASPxCheckBox ID="cbIncludeTrips" runat="server" ClientInstanceName="cbIncludeTrips" CssClass="headerButtons" Text="Includi Viaggi">
            <ClientSideEvents CheckedChanged="cbIncludeReg_OnCheckedChanged" />
        </dx:ASPxCheckBox>
        <dx:ASPxCheckBox ID="cbIncludeBlocked" runat="server" ClientInstanceName="cbIncludeBlocked" CssClass="headerButtons" Text="Includi Bloccate">
            <ClientSideEvents CheckedChanged="cbIncludeReg_OnCheckedChanged" />
        </dx:ASPxCheckBox>
        <dx:ASPxCheckBox ID="cbDoRefresh" runat="server" ClientInstanceName="cbDoRefresh" CssClass="headerButtons" Text="Aggiorna Dopo Elaborazione">
            <ClientSideEvents CheckedChanged="cbIncludeReg_OnCheckedChanged" />
        </dx:ASPxCheckBox>
    </div>



    <div style="align-content: center; margin-top: 10px; margin-bottom: 10px; display: block;">
        <table style="text-align: left;" cellpadding="2" cellspacing="2">
            <tbody>
                <tr>
                    <td style="vertical-align: top;">
                        <dx:ASPxLabel ID="lblCol_Id" runat="server" Text="Collaboratore: " Font-Bold="true" />
                        <br>
                    </td>
                    <td style="vertical-align: top;">
                        <dx:ASPxLabel ID="ASPxLabel2" runat="server" ClientInstanceName="lblCollaboratore" />
                        <br>
                    </td>
                    <td colspan="1" rowspan="2"
                        style="vertical-align: middle;">
                        <dx:ASPxButton ID="btnAddEditGrid" ClientInstanceName="btnAddEditGrid" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" Paddings-Padding="10px" RenderMode="Link">
                            <Image Url="~/Icons/Add/Add.png" />
                            <ClientSideEvents Click="function (s, e) { addEditRow(); }" />
                        </dx:ASPxButton>
                        <br>
                    </td>
                </tr>
                <tr>
                    <td style="vertical-align: top;">
                        <dx:ASPxLabel ID="lblData_Reg" runat="server" Text="Data: " Font-Bold="true" />
                        <br>
                    </td>
                    <td style="vertical-align: top;">
                        <dx:ASPxLabel ID="ASPxLabel1" runat="server" ClientInstanceName="lblDataReg" />
                        <br>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>


    <dx:ASPxGridView ID="gvRegVMEdit" ClientInstanceName="gvRegVMEdit" runat="server" AutoGenerateColumns="False" Width="100%"
        OnCustomCallback="gvRegVMEdit_CustomCallback" OnDataBinding="gvRegVMEdit_DataBinding" OnAfterPerformCallback="gvRegVMEdit_AfterPerformCallback"
        OnInit="gvRegVMEdit_OnInit" OnHtmlDataCellPrepared="gvRegVMEdit_HtmlDataCellPrepared"
        ClientSideEvents-EndCallback="function(s,e){ setEditGridVisibilityAndMessage(s); }">
        <Columns>
            <dx:GridViewDataTextColumn FieldName="RegE" ReadOnly="True" VisibleIndex="0" Visible="false">
                <EditFormSettings Visible="False" />
            </dx:GridViewDataTextColumn>
            <dx:GridViewDataTextColumn FieldName="RegU" ReadOnly="True" VisibleIndex="0" Visible="false">
                <EditFormSettings Visible="False" />
            </dx:GridViewDataTextColumn>
            <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" VisibleIndex="2" Width="30%">
                <DataItemTemplate>
                    <dx:ASPxComboBox Width="100%" ID="cbCant_Id" runat="server" Value='<%# Eval("Cant_Id") %>' OnInit="cbmxCant_Id_Init" />
                </DataItemTemplate>
            </dx:GridViewDataComboBoxColumn>
            <dx:GridViewDataComboBoxColumn FieldName="CentroDiCosto_Id" VisibleIndex="14" Width="30%" Visible="true">
                <DataItemTemplate>
                    <dx:ASPxComboBox Width="100%" ID="cbCentroDiCosto_Id" runat="server" Value='<%# Eval("CentroDiCosto_Id") %>' OnInit="cbmxCentroDiCosto_Id_Init" />
                </DataItemTemplate>
            </dx:GridViewDataComboBoxColumn>
            <dx:GridViewDataTextColumn FieldName="Data_Ora_Fis_E" VisibleIndex="4" Width="10%">
                <DataItemTemplate>
                    <dx:ASPxDateEdit Width="100%" ID="teData_Ora_Fis_E" runat="server" Value='<%# Eval("Data_Ora_Fis_E") %>' DisplayFormatString="HH:mm" EditFormat="Time" OnInit="de_Init">
                    </dx:ASPxDateEdit>
                </DataItemTemplate>
            </dx:GridViewDataTextColumn>
            <dx:GridViewDataTextColumn FieldName="Data_Ora_Fis_U" VisibleIndex="5" Width="10%">
                <DataItemTemplate>
                    <dx:ASPxDateEdit Width="100%" ID="teData_Ora_Fis_U" runat="server" Value='<%# Eval("Data_Ora_Fis_U") %>' DisplayFormatString="HH:mm" EditFormat="Time" OnInit="de_Init">
                    </dx:ASPxDateEdit>
                </DataItemTemplate>
            </dx:GridViewDataTextColumn>
            <dx:GridViewDataCheckColumn FieldName="IsUTimeSameDayE" VisibleIndex="6" Width="5%">
                <DataItemTemplate>
                    <dx:ASPxCheckBox Width="100%" ID="cbIsUTimeSameDayE" runat="server" Value='<%# Eval("IsUTimeSameDayE") %>' OnInit="standardEditControl_Init" />
                </DataItemTemplate>
            </dx:GridViewDataCheckColumn>
            <dx:GridViewDataComboBoxColumn FieldName="Registrazione_Tipo_Reg" VisibleIndex="7" Width="10%" />
            <dx:GridViewDataComboBoxColumn FieldName="Registrazione_Stato_Reg" VisibleIndex="8" Width="10%" />
            <dx:GridViewDataComboBoxColumn FieldName="Motivazione_Reg_Id" VisibleIndex="9" Width="20%">
                <DataItemTemplate>
                    <dx:ASPxComboBox Width="100%" ID="cbMotivazione_Reg_Id" runat="server" Value='<%# Eval("Motivazione_Reg_Id") %>' OnInit="cmbMotivazione_Reg_Id" />
                </DataItemTemplate>
            </dx:GridViewDataComboBoxColumn>
            <dx:GridViewDataCheckColumn FieldName="Registrazione_Bloccata" VisibleIndex="10" Width="5%">
                <DataItemTemplate>
                    <dx:ASPxCheckBox Width="100%" ID="cbRegistrazioneBloccata" runat="server" Value='<%# Eval("Registrazione_Bloccata") %>' OnInit="standardEditControl_Init" />
                </DataItemTemplate>
            </dx:GridViewDataCheckColumn>
            <dx:GridViewDataComboBoxColumn FieldName="Tipo_Modifica" VisibleIndex="0" Visible="False" Width="0%">
                <EditFormSettings Visible="False" />
            </dx:GridViewDataComboBoxColumn>
            <dx:GridViewDataComboBoxColumn FieldName="Registrazione_Tipo_Reg" VisibleIndex="0" Visible="False" Width="0%">
                <EditFormSettings Visible="False" />
            </dx:GridViewDataComboBoxColumn>
            <dx:GridViewDataTextColumn FieldName="EntrataEU" VisibleIndex="11" Width="5%" Visible="False">
                <DataItemTemplate>
                    <dx:ASPxTextBox runat="server" Width="100%" ID="txtEntrataEU" Value='<%# Eval("EntrataEU") %>' OnInit="standardEditControl_Init" />
                </DataItemTemplate>
            </dx:GridViewDataTextColumn>
            <dx:GridViewDataTextColumn FieldName="UscitaEU" VisibleIndex="12" Width="5%" Visible="False">
                <DataItemTemplate>
                    <dx:ASPxTextBox runat="server" Width="100%" ID="txtUscitaEU" Value='<%# Eval("UscitaEU") %>' OnInit="standardEditControl_Init" />
                </DataItemTemplate>
            </dx:GridViewDataTextColumn>
            <dx:GridViewDataComboBoxColumn FieldName="Activity_Evaluation" VisibleIndex="13" Width="20%">
                <DataItemTemplate>
                    <dx:ASPxComboBox Width="100%" ID="cbActivity_Evaluation" runat="server" Value='<%# Eval("Activity_Evaluation") %>' OnInit="cbActivity_Evaluation_Init" />
                </DataItemTemplate>
            </dx:GridViewDataComboBoxColumn>
        </Columns>
        <SettingsBehavior ColumnResizeMode="Control" AllowGroup="false" />
        <Settings HorizontalScrollBarMode="Auto" />
        <SettingsPager Mode="ShowAllRecords" />
    </dx:ASPxGridView>

    <div style="clear: both; margin-top: 10px; float: left">
        <dx:ASPxButton ID="btnUndo" ClientInstanceName="btnUndo" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" Paddings-Padding="10px" RenderMode="Link">
            <Image Url="~/Icons/Undo/Undo.png">
            </Image>
            <ClientSideEvents Click="function(s, e) { setEditGridVisible(false);}" />
        </dx:ASPxButton>
        <dx:ASPxButton ID="btnUpdate" ClientInstanceName="btnUpdate" runat="server" AutoPostBack="False" CssClass="headerButtons" EnableTheming="false" Paddings-Padding="10px" RenderMode="Link">
            <Image Url="~/Icons/Check/Check.png">
            </Image>
            <ClientSideEvents Click="function(s, e) { gvRegVMEdit.PerformCallback('update');}" />
        </dx:ASPxButton>
    </div>
</div>

<dx:ASPxPopupControl ID="pcShowMap" runat="server" Height="400px" LoadContentViaCallback="OnPageLoad"
    Width="600px" HeaderText="Map popup" ClientSideEvents-Shown="newinitMap" PopupElementID="btnShowMap" CloseAction="OuterMouseClick" ShowCloseButton="false">
    <ContentCollection>
        <dx:PopupControlContentControl>
            <div id='bingMap' style="position: relative; width: 640px; height: 400px;"></div>
        </dx:PopupControlContentControl>
    </ContentCollection>
</dx:ASPxPopupControl>
<dx:ASPxDateEdit runat="server" ID="__ReferenceDateEdit" ClientVisible="false">
</dx:ASPxDateEdit>
