<%@ Page Title="PowerWeb - Cartellino" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="CartellinoPage.aspx.cs" Inherits="PowerWeb.Pages.CartellinoPage" %>


<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
  
    <script type="text/javascript">function getColColumns() { return <%=_colColumns%>; }</script>
    <script type="text/javascript">function getResources() { return <%=_resources%>; }</script>
    <script type="text/javascript">function getMotivazioni() { return <%=_motivazioni%>; }</script>
    <script type="text/javascript">function getExports() { return <%=_exports%>; }</script>
    <script type="text/javascript">function getConfig() { return <%= _cartellinoOptionsJSON%>; }</script>
    <script type="text/javascript">function getDisabledColsCustomization() { return <%=_disabledColsCustomization.ToString().ToString().ToLower()%>; }</script>


    <%--<link href="../Styles/CSS/Carmine.css" rel="stylesheet" type="text/css" />--%>
    <link href="../Styles/CSS/CartellinoPage/CartellinoPage.css" rel="stylesheet" type="text/css" />

    <div ng-app="cartellino" ng-controller="cartellinoController">

        <div id="header" class="row fit">
            <div class="col-md-3"></div>
            <div class="col-md-6" style="text-align: center;">
                <div id="datePicker" dx-date-box="datePickerOptions" style="display: inline-block;"></div>
            </div>
            <div class="col-md-3">
                <div id="divisionTypeSelectBox" dx-select-box="divisionTypeSelectBoxOptions"></div>
            </div>
        </div>

        <fieldset id="full-col-grid-container" class="fit">
            <legend style="width: inherit; margin-bottom: 2px; margin-left: 1px; margin-right: 1px;">Collaboratori</legend>
            <!-- Griglia dei collaboratori -->
            <div id="colGrid" dx-data-grid="collabDataGridOptions"></div>
        </fieldset>

        <div id="colSelectors" class="row fit" style="display: none;">

            <div class="col-md-2" style="text-align: center;">
                <div id="swapEditModeButton" style="float: left;" dx-button="swapEditModeButtonOptions"></div>
            </div>
            <div class="col-md-8" style="text-align: center;">
                <!-- Bottone 'Precedente' -->
                <div class="col-md-2 col-xs-2">
                    <div id="backwardCartellino" dx-button=" backwardCartellinoButtonOptions"></div>
                </div>
                <!-- Combobox collaboratori -->
                <div class="col-md-8 col-xs-8">
                    <div id="selectedColsSelectBox" dx-select-box="collabSelectBoxOptions"></div>
                </div>
                <!-- Bottone 'Successivo' -->
                <div class="col-md-2 col-xs-2" style="text-align: center;">
                    <div id="forwardCartellino" dx-button="forwardCartellinoButtonOptions"></div>
                </div>
            </div>
            <div class="col-md-2" style="text-align: center;">
            </div>
        </div>

        <!-- Multiview dei cartellini -->
        <div id="cartellinoContainer" class="row fit" style="display: none;">
            <div id="cartellino">
                <div class="row">
                    <div class="col-md-4">
                        <h4 class="labelText" ng-bind="getCurrentCol()"></h4>
                    </div>
                    <div class="col-md-4" style="text-align: center;">
                        <h4 class="labelText" ng-bind="getCurrentDate()"></h4>
                    </div>
                    <div class="col-md-4" ng-if="cartellinoElabOptions.useMonteMinuti" style="text-align: center;">
                        <h4 class="labelText" ng-bind="getCurrentColMonteMin()"></h4>
                    </div>
                </div>
                <div id="cartellinoGrid" class="fitNoBorders" style="margin-top: 2%; margin-bottom: 2%; margin-left: 0.5%; margin-right: 0.5%;">
                </div>
                <div id="editableCartellinoGrid" class="fitNoBorders"  style="margin-top: 2%; margin-bottom: 2%; margin-left: 0.5%; margin-right: 0.5%;">
                </div>
            </div>
            <div id="cartellinoEditabile" ng-if="cartellinoElabOptions.useEditableCartellino && !cartellinoElabOptions.devideByOtherEntity">
                <h4 class="labelText">Cartellino ore straordinarie</h4>
            </div>

        </div>


        <!-- Bottonistica varia -->
        <div class="row fit" id="bottoniListaCollaboratori">
            <div class="col-md-4">
                <div class="row">
                    <div class="col-md-6 buttonCont">
                        <div dx-button="{ text: 'Report', onClick: showReportPrintPopup, icon: 'fa fa-file-pdf-o',width:'70%' }"></div>
                    </div>
                    <div class="col-md-6 buttonCont">
                        <div dx-button="{ text: 'Excel', onClick: showExcelPrintPopup, icon: 'fa fa-file-excel-o',width:'70%' }"></div>
                    </div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="row">
                    <div class="col-md-6 buttonCont">
                        <div dx-button="{ text: 'Elabora cartellino', onClick: generateCartellino, icon: 'toolbox', width:'70%' }"></div>
                    </div>
                    <div class="col-md-6 buttonCont">
                        <div dx-button="{ text: 'Funzioni', onClick: showFunctionsPopup, icon: 'toolbox', width:'70%'  }"></div>
                    </div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="row">
                    <div class="col-md-6 buttonCont"></div>
                </div>
            </div>
        </div>

        <div class="row fit" id="bottoniCartellino" style="display: none">
            <div class="col-md-4" style="text-align: center;">
            </div>
            <div class="col-md-4" style="text-align: center">
                <div id="chooseCollaboratoriButton" dx-button="{ text: 'Scegli collaboratori', onClick: showCollaboratoriGrid, icon: 'group' }"></div>
            </div>
            <div class="col-md-4">
                <div ng-show="cartellinoElabOptions.editableMode">
                    <div class="dx-field">
                        <div class="dx-field-label">Motivazione</div>
                        <div class="dx-field-value" dx-drop-down-box="motivationSelectBoxOptions"></div>
                    </div>
                    <div class="dx-field" ng-hide="cartellinoElabOptions.devideByOtherEntity">
                        <div class="dx-field-label">Cantiere</div>
                        <div class="dx-field-value" dx-drop-down-box="cantSelectBox"></div>
                    </div>
                </div>
            </div>
        </div>

        

        <!-- Popup errori -->
        <div dx-popup="errorsPopupOptions" id="errorsPopup">
        </div>

        <!-- Tooltip stampa excel -->
        <div dx-popup="printExcelPopupOptions" id="excelPrintPopup">
        </div>

        <!-- Tooltip stampa report -->
        <div dx-popup="printReportPopupOptions" id="reportPrintPopup">
        </div>

        <!-- Popup funzioni cartellino -->
        <div dx-popup="functionsPopupOptions" id="functionsPopup">
        </div>

        <!-- Popup autorettifiche -->
        <div dx-popover="autoRettifichePopoverOptions" id="autoRettifichePopover">
        </div>



        <!-- Pannelli di caricamento -->
        <div id="elaborateExportLoadPanel"></div>
        <div id="elaborateCartellinoLoadPanel"></div>
        <div id="deleteCartellinoLoadPanel"></div>
        <div id="generateRettificheLoadPanel"></div>
        <div id="deleteRettificheLoadPanel"></div>

    </div>

    <script type="text/javascript" src="/Scripts/Custom/CartellinoPage/CartellinoController.js"></script>
</asp:Content>
