<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TrackingModule.ascx.cs"
    Inherits="PowerWeb.Modules.TrackingModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register TagPrefix="dxe" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxCallback" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxEditors" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPopupControl" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<script type="text/javascript" src="http://ecn.dev.virtualearth.net/mapcontrol/mapcontrol.ashx?v=7.0"></script>
<script type="text/javascript">
    

    // -------------------- INIZIO GESTIONE CARICAMENTO MAPPA ------------------------------

    function checkSelections()
    {
        // variabile che mi segnala se proseguire o meno
        var prosegui = false;
        // per proseguire con la visualizzazione delle registrazioni in mappa
        // è necessario che sia stata selezionata una data (il collaboratore c'è in ogni caso)
        if (SearchDate.GetValue() != null)
        {
            btnApply_click.PerformCallback();
        }
        else
        {
            alert("È necessario selezionare un collaboratore e una data per visualizzarne le registrazioni!");
        }
    }

    function btnApplyEndCallback(s, e)
    {
        //Se si sono riscontrati degli errori, gli segnala
        if (typeof s.cpErrorMessage != 'undefined' && s.cpErrorMessage != '') {
            var errorMessage = s.cpErrorMessage;
            s.cpErrorMessage = '';
            var mapElement = document.getElementById('bingMap');
            //Nasconde l'elemento del DOM contenente la mappa
            mapElement.style.visibility = 'hidden';
            DisplayDialogError('Errori inserimento', errorMessage);
        }
        //Altrimenti chiama la routine per visualizzare la mappa
        else
        {
            loadMap(s.cpCoordinatesToShow);
        }
    }


    var bingMap = null;
    var pinInfobox;
    var infoboxLayer;

    //Caricamento della mappa e dei pushpin
    function loadMap(coordinatesToShow)
    {
        //Selezione l'elemento del DOM in cui visualizzare la mappa
        var mapElement = document.getElementById('bingMap');
        //Rende l'elemento visibile (potrebbe non esserlo se si è provato a visualizzare una mappa per un collaboratore/giorno senza registrazioni)
        mapElement.style.visibility = 'visible';

        if (bingMap)
        {
            bingMap.dispose();
            bingMap = null;
        }

        var bingKey = "<%= BingKey %>";

        //Crea la mappa e ne imposta le opzioni di visualizzazione
        bingMap = new Microsoft.Maps.Map(mapElement, { credentials: bingKey, showDashboard: true, mapTypeId: Microsoft.Maps.MapTypeId.auto, enableClickableLogo: false, enableSearchLogo: false });
        bingMap.setView({ mapTypeId: Microsoft.Maps.MapTypeId.road });

        //Crea e aggiunge alla mappa un layer e un infoBox (popup per i pushpin)
        pinInfobox = new Microsoft.Maps.Infobox(new Microsoft.Maps.Location(0, 0), { visible: false });
        infoboxLayer = new Microsoft.Maps.EntityCollection();
        infoboxLayer.push(pinInfobox);

        var showedPinsLoc = [];
        var iniziale;
        //Crea e mette su mappa i pushpin, saltando il primo (altrimenti finirebbe 'sotto' a tutti gli altri) 
        for (var i = 1; i < coordinatesToShow.length; i++) {
            //Recupera e imposta le coordinate
            var currentLatValue = parseFloat(String(coordinatesToShow[i].CurrentLatitude).replace(",", "."));
            var currentLonValue = parseFloat(String(coordinatesToShow[i].CurrentLongitude).replace(",", "."));
            var currentPushpinLoc = new Microsoft.Maps.Location(currentLatValue, currentLonValue);
            showedPinsLoc.push(currentPushpinLoc);

            //Di default, imposta il pushpin arancione
            var pushpinIcon = '/images/orange_pushpin.png';
            // Se è l'ultima timbratura di giornata, imposta il pushpin rosso
            if (coordinatesToShow[i].Color == 2 && i == coordinatesToShow.length - 1) {
                pushpinIcon = '/images/red_pushpin.png';
            }
            if (coordinatesToShow[i].Color == 1) {
                pushpinIcon = '/images/red_pushpin.png';
            }
            if (coordinatesToShow[i].Color == 0) {
                pushpinIcon = '/images/green_pushpin.png';
            }
            //Imposta tutte le opzioni e informazioni del pushpin e lo aggiunge alla mappa
            var pushpinOptions = { draggable: false, icon: pushpinIcon, text: coordinatesToShow[i].PushpinLabel };
            var pushpin = new Microsoft.Maps.Pushpin(currentPushpinLoc, pushpinOptions);
            pushpin.Title = coordinatesToShow[i].InfoboxTitle;
            pushpin.Description = coordinatesToShow[i].InfoboxDescription;
            bingMap.entities.push(pushpin);
            Microsoft.Maps.Events.addHandler(pushpin, 'click', displayInfobox);
            if (i == coordinatesToShow.length - 1) {
                iniziale = coordinatesToShow[0].Color;
            }
        }

        //Si inserisce in mappa anche la prima registrazione
        var currentLatValue = parseFloat(String(coordinatesToShow[0].CurrentLatitude).replace(",", "."));
        var currentLonValue = parseFloat(String(coordinatesToShow[0].CurrentLongitude).replace(",", "."));
        var currentPushpinLoc = new Microsoft.Maps.Location(currentLatValue, currentLonValue);
        showedPinsLoc.push(currentPushpinLoc);
        //Si imposta il pushpin verde
        var pushpinIcon = '/images/green_pushpin.png';
        if (iniziale == 1) {
            pushpinIcon = '/images/red_pushpin.png';
        }
        //Imposta tutte le opzioni e informazioni del pushpin e lo aggiunge alla mappa
        var pushpinOptions = { draggable: false, icon: pushpinIcon, text: coordinatesToShow[0].PushpinLabel };
        var pushpin = new Microsoft.Maps.Pushpin(currentPushpinLoc, pushpinOptions);
        pushpin.Title = coordinatesToShow[0].InfoboxTitle;
        pushpin.Description = coordinatesToShow[0].InfoboxDescription;
        bingMap.entities.push(pushpin);
        Microsoft.Maps.Events.addHandler(pushpin, 'click', displayInfobox);

        bingMap.entities.push(infoboxLayer);
        //Se c'è solo un pushpin, centra la mappa su di esso
        if (showedPinsLoc.length == 1) {
            bingMap.setView({ center: showedPinsLoc[0], zoom: 10 });
        }
        //Altrimenti centra la mappa nel baricentro dei pushpin
        else {
            var bestView = Microsoft.Maps.LocationRect.fromLocations(showedPinsLoc);
            bestView.height += 0.05;
            bestView.width += 0.05;
            bingMap.setView({
                bounds: bestView
            });
        }
    }

    //Al click di un pushpin, ne fa comparire l'infobox (chiudendo quelli degli altri
    function displayInfobox(e) {
        pinInfobox.setOptions({ title: e.target.Title, description: e.target.Description, visible: true, offset: new Microsoft.Maps.Point(0, 25) });
        pinInfobox.setLocation(e.target.getLocation());
    }

    // -------------------- TERMINE GESTIONE CARICAMENTO MAPPA ------------------------------

</script>
<!-- Callback scatenato al click del bottone 'Applica', ma dopo il controllo sulla validità dei campi selezionati -->
<dx:ASPxCallback runat="server" ID="btnApply_click" ClientInstanceName="btnApply_click" OnCallback="btnApply_OnCallback" ClientSideEvents-EndCallback="btnApplyEndCallback"></dx:ASPxCallback>
<div style="clear: both; float: none; border: 1px solid darkgrey; padding: 5px;">
    <div style="display:table; margin: 0 auto; font-size: 2em; font-family:sans-serif; font-weight:bold">
        <h5>Selezionare un collaboratore e una data per visualizzarne le registrazioni</h5>
    </div>
    <table style="width: 100%; padding-top:1em" >
        <tr>
            <!-- Collaboratore -->
            <td style="width: 15%; text-align: right; padding-right: 1%; font-family:sans-serif;">
                <dx:ASPxLabel runat="server" ID="lblCol" Text="Collaboratore: " Font-Bold="True" />
            </td>
            <td style="width: 30%; text-align: left; padding-left: 1%;">
                <dx:ASPxComboBox Id="cmbCol" ClientInstanceName="cmbCol" runat="server" Width="100%" OnInit="cmbCol_Init"/>
            </td>
            <!-- Data -->
            <td style="width: 15%; text-align: right; padding-right: 1%;">
                <dx:ASPxLabel ID="SearchDateLabel" runat="server" Text="Data: " Font-Bold="true" Client-Enabled="true" />
            </td>
            <td style="width: 40%; text-align: left; padding-left: 1%;">
                <dx:ASPxDateEdit ID="SearchDate" ClientInstanceName="SearchDate" runat="server" max-width="100%" Width="50%" />
            </td>
        </tr>
    </table>
    <!-- Bottone 'Applica'-->
    <div style="display:table; margin: 0 auto; padding-top: 1em; padding-bottom: 1em">
        <dx:ASPxButton Text="Applica" ClientInstanceName="btnApply" ID="btnApply" runat="server" AutoPostBack="false">
            <ClientSideEvents Click="function(s, e) {checkSelections();}" />
        </dx:ASPxButton>
    </div>
</div>

<!-- Mappa-->
<div id='bingMap' style="position: relative; width: 100%; height: 60vh"></div>


