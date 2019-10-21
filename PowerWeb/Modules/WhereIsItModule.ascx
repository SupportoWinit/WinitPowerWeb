<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="WhereIsItModule.ascx.cs"
    Inherits="PowerWeb.Modules.WhereIsItModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register TagPrefix="dxe" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxCallback" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPopupControl" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<script type="text/javascript" src="http://ecn.dev.virtualearth.net/mapcontrol/mapcontrol.ashx?v=7.0"></script>
<script type="text/javascript">
    
    // -------------------- INIZIO GESTIONE SELEZIONE IN GRIGLIA ------------------------------
    var _selectNumber = 0;
    var _selectNumberOverPage = 0;
    var selectionType = "none";
    var _handle = true;

    function OnAllCheckedChanged(s, e) {
        if (s.GetChecked()) {
            DisplayJConfirm('Power', s.cpMessage, function (r) {
                if (r) {
                    grid.SelectRows();
                    selectionType = "sAll";
                }
                else {
                    s.SetChecked(false);
                    selectionType = "uAll";
                    grid.UnselectRows();
                }

            });
        }
        else {
            s.SetChecked(false);
            selectionType = "uAll";
            grid.UnselectRows();
        }

    }

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
    // -------------------- TERMINE GESTIONE SELEZIONE IN GRIGLIA ------------------------------

    // -------------------- INIZIO GESTIONE CARICAMENTO MAPPA ------------------------------

    var bingMap = null;
    var searchManager = null;
    var isFirstTimeRequest = true;
    var pinInfobox;
    var infoboxLayer;

    function loadMap(objectsToShow) {
        isFirstTimeRequest = true;
        
        var mapElement = document.getElementById('bingMap');

        if (bingMap) {
            bingMap.dispose();
            bingMap = null;
        }

        var bingKey = "<%= BingKey %>";

        bingMap = new Microsoft.Maps.Map(mapElement, { credentials: bingKey, showDashboard: false, enableClickableLogo: false, enableSearchLogo: false });
        bingMap.setView({ mapTypeId: Microsoft.Maps.MapTypeId.road });

        pinInfobox = new Microsoft.Maps.Infobox(new Microsoft.Maps.Location(0, 0), { visible: false });
        infoboxLayer = new Microsoft.Maps.EntityCollection();
        infoboxLayer.push(pinInfobox);

        var showedPinsLoc = [];

        for (var i = 0; i < objectsToShow.length; i++) {
            var currentLatValue = parseFloat(objectsToShow[i].Latitude.replace(",", "."));
            var currentLonValue = parseFloat(objectsToShow[i].Longitude.replace(",", "."));
            var currentPushpinLoc = new Microsoft.Maps.Location(currentLatValue, currentLonValue);
            showedPinsLoc.push(currentPushpinLoc);
            var pushpinIcon = '';
            if (objectsToShow[i].State == 'IsInIt')
                pushpinIcon = '/images/green_pushpin.png';
            else if (objectsToShow[i].State == 'IsOutOfIt')
                pushpinIcon = '/images/red_pushpin.png';
            else if (objectsToShow[i].State == 'LastPass')
                pushpinIcon = '/images/orange_pushpin.png';

            var pushpinOptions = { draggable: false, icon: pushpinIcon, text: objectsToShow[i].PushpinText };
            var pushpin = new Microsoft.Maps.Pushpin(currentPushpinLoc, pushpinOptions);
            pushpin.Title = objectsToShow[i].InfoboxTitle;
            pushpin.Description = objectsToShow[i].InfoboxDescription;
            bingMap.entities.push(pushpin);
            Microsoft.Maps.Events.addHandler(pushpin, 'click', displayInfobox);
        }
        bingMap.entities.push(infoboxLayer);

        if (showedPinsLoc.length == 1) {
            bingMap.setView({ center: showedPinsLoc[0], zoom: 10 });
        } else {
            var bestView = Microsoft.Maps.LocationRect.fromLocations(showedPinsLoc);
            bingMap.setView({ bounds: bestView });
        }
    }

    function displayInfobox(e) {
        pinInfobox.setOptions({ title: e.target.Title, description: e.target.Description, visible: true, offset: new Microsoft.Maps.Point(0, 25) });
        pinInfobox.setLocation(e.target.getLocation());
    }

    function hideInfobox(e) {
        pinInfobox.setOptions({ visible: false });
    }

    //eseguito al termine di cambio di selezione
    function gridSelectionChangeEndCallback(s, e) {
        loadMap(s.cpSelectedColsWithCoordinates);
    }

    // -------------------- TERMINE GESTIONE CARICAMENTO MAPPA ------------------------------

</script>
<dx:ASPxCallback runat="server" ID="gridSelectionChange" ClientInstanceName="gridSelectionChange" OnCallback="gridSelectionChange_OnCallback" ClientSideEvents-EndCallback="gridSelectionChangeEndCallback"></dx:ASPxCallback>
<table>
    <tr>
        <dx:ASPxGridView ID="gvWhereIsIt" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvWhereIsIt_OnDataBinding"
            OnHtmlDataCellPrepared="gvWhereIsIt_OnHtmlDataCellPrepared" OnPageIndexChanged="gvWhereIsIt_OnPageIndexChanged" OnCustomJSProperties=gvWhereIsIt_OnCustomJSProperties>
            <ClientSideEvents SelectionChanged="OnGridSelectionChanged" EndCallback="OnGridEndCallback" />
            <Columns>
                <dx:GridViewCommandColumn ShowSelectCheckbox="True">
                    <HeaderTemplate>
                        <dxe:ASPxCheckBox ID="cbAll" runat="server" ClientInstanceName="cbAll" ToolTip="Select all rows" BackColor="White" OnInit="cbAll_Init" OnCustomJSProperties="cbAll_OnCustomJSProperties">
                            <ClientSideEvents CheckedChanged="OnAllCheckedChanged" />
                        </dxe:ASPxCheckBox>
                        <dxe:ASPxCheckBox ID="cbPage" runat="server" ClientInstanceName="cbPage" ToolTip="Select all rows within the page" OnInit="cbPage_Init">
                            <ClientSideEvents CheckedChanged="OnPageCheckedChanged" />
                        </dxe:ASPxCheckBox>
                    </HeaderTemplate>
                    <HeaderStyle HorizontalAlign="Center"/>
                </dx:GridViewCommandColumn>
                <dx:GridViewDataComboBoxColumn FieldName="Col_Id" SortIndex="0">
                    <Settings AllowHeaderFilter="False" />
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="Cognome_Col" Visible="False"/>
                <dx:GridViewDataTextColumn FieldName="CognomeNome_Col" Visible="False"/>
                <dx:GridViewDataTextColumn FieldName="Nome_Col" Visible="False" />
                <dx:GridViewDataTextColumn FieldName="LastPruCode" Visible="False" />
                <dx:GridViewDataComboBoxColumn FieldName="WhereIsItState"/>
                <dx:GridViewDataDateColumn FieldName="WhereIsItDate"/>
                <dx:GridViewDataTimeEditColumn FieldName="WhereIsItHour"/>
                <dx:GridViewDataComboBoxColumn FieldName="WhereIsItCantId">
                    <Settings AllowHeaderFilter="False" />
                </dx:GridViewDataComboBoxColumn>
                <dx:GridViewDataTextColumn FieldName="WhereIsItCantCode" Visible="False"  />
                <dx:GridViewDataTextColumn FieldName="WhereIsItCantDes"  Visible="False" />
                <dx:GridViewDataTextColumn FieldName="WhereIsItTurn"  Visible="False" />
                <dx:GridViewDataTextColumn FieldName="WhereIsItSubCant"  Visible="False" />
                <dx:GridViewDataTextColumn FieldName="WhereIsItActivityType"  Visible="False" />
                <dx:GridViewDataTextColumn FieldName="WhereIsItCantAddress" />
                <dx:GridViewDataTextColumn FieldName="WhereIsItCantCity" />
                <dx:GridViewDataTextColumn FieldName="WhereIsItCantCap" />
                <dx:GridViewDataTextColumn FieldName="WhereIsItCantProvince" />
                <dx:GridViewDataTextColumn FieldName="WhereIsItCantNation"  Visible="False" />
            </Columns>
        </dx:ASPxGridView>
    </tr>
</table>
<div id='bingMap' style="position: relative; width: 100%; height: 400px;"></div>

