<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Reg_VModule.ascx.cs" Inherits="PowerWeb.Modules.Reg_VModule" %>
<%@ Register Assembly="DevExpress.Web.ASPxScheduler.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxScheduler" TagPrefix="dxs" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxUploadControl" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register TagPrefix="pw" TagName="CustomVerticalAppointmentTemplate" Src="CustomForms/VerticalAppointmentTemplate.ascx" %>
<%@ Register TagPrefix="pw" TagName="CustomHorizontalAppointmentTemplate" Src="CustomForms/HorizontalAppointmentTemplate.ascx" %>
<%@ Register TagPrefix="pw" TagName="CustomHorizontalSameDayAppointmentTemplate" Src="CustomForms/HorizontalSameDayAppointmentTemplate.ascx" %>
<%@ Register TagPrefix="pw" TagName="RegReportModule" Src="ReportModules/RegReportModule.ascx" %>
<%@ Register Assembly="DevExpress.XtraScheduler.v14.1.Core, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.XtraScheduler" TagPrefix="cc1" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxCallback" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxFormLayout" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxTimer" TagPrefix="dx" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxPopupControl" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>
<script type="text/javascript" src="http://ecn.dev.virtualearth.net/mapcontrol/mapcontrol.ashx?v=7.0"></script>
<script type="text/javascript">


    $(window).on('load', function () {
        var currentGrid = ASPxClientGridView.Cast(grid);
        var filterValid = currentGrid.cpIsFilterValid;
        if (currentGrid.cpIsFilterValid == false && currentGrid.cpFilterError != "") {
            DisplayDialogInfo('Power', currentGrid.cpFilterError);
            currentGrid.cpIsFilterValid = !currentGrid.cpIsFilterValid;
        }
    });

    function ShowExportWindow() {
        pcExportOptions.Show();
    }

    function OnAppointmentsSelectionChanged(scheduler, appointmentIds) {
        if (appointmentIds != null && appointmentIds.length == 1) {

            var string = "";

            for (var i = 0; i < appointmentIds.length; i++) {
                string += appointmentIds[i];
            }
        }
    }

    function scRegV_OnMenuItemClick(s, e, args) {
        if (args.item.name == "NewCommand") {
            var currentGrid = ASPxClientGridView.Cast(grid);
            currentGrid.AddNewRow();
            window.scrollTo(0, 200);
        }
        if (args.item.name == "NewCloneCommand") {
            var currentScheduler = ASPxClientScheduler.Cast(s);
            var currentGrid = ASPxClientGridView.Cast(grid);
            window.scrollTo(0, 200);
            gvRegVPanel.PerformCallback("newclone|" + currentScheduler.GetSelectedAppointmentIds()[0]);
        }
        else if (args.item.name == "EditCommand") {
            var currentScheduler = ASPxClientScheduler.Cast(s);
            var currentGrid = ASPxClientGridView.Cast(grid);
            window.scrollTo(0, 200);
            gvRegVPanel.PerformCallback("edit|" + currentScheduler.GetSelectedAppointmentIds()[0]);
        }
        else if (args.item.name == "DeleteCommand") {
            var currentScheduler = ASPxClientScheduler.Cast(s);
            var currentGrid = ASPxClientGridView.Cast(grid);
            window.scrollTo(0, 200);
            gvRegVPanel.PerformCallback("delete|" + currentScheduler.GetSelectedAppointmentIds()[0]);
        }
    }


    function cbAutoSync_OnCheckedChanged(s, e) {
        var currentCB = ASPxClientCheckBox.Cast(s);
        var currentBTN = ASPxClientButton.Cast(btnLoad);
        currentBTN.SetEnabled(!currentCB.GetChecked());
        if (currentCB.GetChecked())
            scRegVPanel.PerformCallback();

    }

    function btnLoad_OnInit(s, e) {
        var currentCB = ASPxClientCheckBox.Cast(cbAutoSync);
        var currentBTN = ASPxClientButton.Cast(s);
        currentBTN.SetEnabled(!currentCB.GetChecked());
    }

    function btnLoad_OnClick(s, e) {
        scRegVPanel.PerformCallback();
    }

    var isSchedulerToUpdate = false;

    function gvRegV_OnBeginCallback(s, e) {
        var currentCB = ASPxClientCheckBox.Cast(cbAutoSync);
        if (e.command == 'APPLYFILTER' ||
            e.command == 'APPLYCOLUMNFILTER' ||
            e.command == 'APPLYHEADERCOLUMNFILTER' ||
            e.command == 'UPDATEEDIT' ||
            e.command == 'ADDNEWROW' ||
            e.command == 'DELETEROW' ||
            e.command == 'SETFILTERENABLED' ||
            e.command == 'REFRESH' ||
            e.command == 'CUSTOMCALLBACK') {
            if (currentCB.GetChecked())
                isSchedulerToUpdate = true;
        }
    }

    function gvRegV_OnEndCallback(s, e) {
        AttachContextmenu();
        var currentCB = ASPxClientCheckBox.Cast(cbAutoSync);
        if (currentCB && currentCB.GetChecked() && isSchedulerToUpdate) {
            scRegVPanel.PerformCallback();
            isSchedulerToUpdate = false;
        }

        var currentGrid = ASPxClientGridView.Cast(grid);
        var filterValid = currentGrid.cpIsFilterValid;
        if (currentGrid.cpIsFilterValid == false && currentGrid.cpFilterError != "") {
            DisplayDialogInfo('Power', currentGrid.cpFilterError);
            currentGrid.cpIsFilterValid = !currentGrid.cpIsFilterValid;
        }
    }

    function gvRegVPanel_OnEndCallback(s, e) {
        scRegVPanel.PerformCallback();
    }

    // -------------------- GESTIONE VISUALIZZAZIONE CARTINA REGISTRAZIONI -----------------------------

    var bingMap = null;
    var searchManager = null;
    var isFirstTimeRequest = true;

    function loadMap() {
        isFirstTimeRequest = true;
        //
        var currentGrid = ASPxClientGridView.Cast(grid);
        var currentLat = currentGrid.GetEditValue("Registrazione_Lat_Orig_E");
        var currentLon = currentGrid.GetEditValue("Registrazione_Long_Orig_E");

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
                bingMap.setView({ zoom: 17, center: new Microsoft.Maps.Location(currentLatValue, currentLonValue) });
                bingMap.entities.push(pushpin);
            }
        }
    }

    // -------------------- FINE GESTIONE VISUALIZZAZIONE CARTINA REGISTRAZIONI -----------------------------
</script>


<dx:ASPxCallbackPanel ID="gvRegVPanel" runat="server" Width="100%" ClientInstanceName="gvRegVPanel"
    OnCallback="gvRegVPanel_Callback" ClientSideEvents-EndCallback="gvRegVPanel_OnEndCallback" OnCustomJSProperties="gvRegVPanel_CustomJSProperties">
    <PanelCollection>
        <dx:PanelContent ID="PanelContent3" runat="server" SupportsDisabledAttribute="True">
            <dx:ASPxGridView ID="gvRegV" runat="server" AutoGenerateColumns="False" Width="100%" OnCellEditorInitialize="gvRegV_CellEditorInitialize" OnAutoFilterCellEditorInitialize="gvRegV_AutoFilterCellEditorInitialize"
                OnDataBinding="gvRegV_DataBinding"
                OnRowInserting="gvRegV_RowInserting"
                OnRowUpdating="gvRegV_RowUpdating"
                OnRowDeleting="gvRegV_RowDeleting"
                OnRowValidating="gvRegV_RowValidating"
                OnInitNewRow="gvRegV_InitNewRow"
                ClientSideEvents-BeginCallback="gvRegV_OnBeginCallback"
                ClientSideEvents-EndCallback="gvRegV_OnEndCallback">
                <Columns>
                    <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
                        <CustomButtons>
                            <dx:GridViewCommandColumnCustomButton ID="add">
                                <Image ToolTip="Add" Url="../Icons/Add/Add.png" />
                            </dx:GridViewCommandColumnCustomButton>
                            <dx:GridViewCommandColumnCustomButton ID="addClone">
                                <Image ToolTip="Favorites" Url="../Icons/Add/Add.png" />
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
                            <Image Url="../Icons/Cancel/Cancel.png" />
                        </ClearFilterButton>
                    </dx:GridViewCommandColumn>
                    <dx:GridViewDataTextColumn FieldName="RegE" Visible="False" ShowInCustomizationForm="True" EditFormSettings-Visible="False">
                    </dx:GridViewDataTextColumn>
                </Columns>
            </dx:ASPxGridView>
        </dx:PanelContent>
    </PanelCollection>
</dx:ASPxCallbackPanel>

<div style="clear: both; margin-top: 10px;">
    <dx:ASPxButton ID="btnLoad" runat="server" AutoPostBack="False" CssClass="headerButtons" ClientInstanceName="btnLoad" ClientSideEvents-Init="btnLoad_OnInit" UseSubmitBehavior="false">
        <Image Url="~/Icons/Load/Load.png">
        </Image>
        <ClientSideEvents Click="btnLoad_OnClick" />
    </dx:ASPxButton>
    <dx:ASPxCheckBox ID="cbAutoSync" runat="server" ClientInstanceName="cbAutoSync" ClientSideEvents-CheckedChanged="cbAutoSync_OnCheckedChanged" CssClass="headerButtons" />
    <dx:ASPxLabel ID="lblAutoSync" runat="server" CssClass="headerButtons" AssociatedControlID="cbAutoSync"></dx:ASPxLabel>
</div>

<div style="clear: both">
    <dx:ASPxCallbackPanel ID="scRegVPanel" runat="server" Width="100%" ClientInstanceName="scRegVPanel"
        OnCallback="scRegVPanel_Callback">
        <PanelCollection>
            <dx:PanelContent ID="PanelContent1" runat="server" SupportsDisabledAttribute="True">
                <dxs:ASPxScheduler ID="scRegV" runat="server" Width="100%" ActiveViewType="Timeline"
                    ClientInstanceName="scRegV"
                    OnPopupMenuShowing="scRegV_PopupMenuShowing">
                    <Views>
                        <DayView>
                            <Templates>
                                <VerticalAppointmentTemplate>
                                    <pw:CustomVerticalAppointmentTemplate ID="VerticalAppointment1" runat="server" />
                                </VerticalAppointmentTemplate>
                            </Templates>
                        </DayView>
                        <WorkWeekView>
                            <Templates>
                                <VerticalAppointmentTemplate>
                                    <pw:CustomVerticalAppointmentTemplate ID="VerticalAppointment2" runat="server" />
                                </VerticalAppointmentTemplate>
                            </Templates>
                        </WorkWeekView>
                        <WeekView>
                            <Templates>
                                <HorizontalAppointmentTemplate>
                                    <pw:CustomHorizontalAppointmentTemplate ID="HorizontalAppointment3" runat="server" />
                                </HorizontalAppointmentTemplate>
                                <HorizontalSameDayAppointmentTemplate>
                                    <pw:CustomHorizontalSameDayAppointmentTemplate ID="HorizontalSameDayAppointment3"
                                        runat="server" />
                                </HorizontalSameDayAppointmentTemplate>
                            </Templates>
                        </WeekView>
                        <MonthView>
                            <Templates>
                                <HorizontalAppointmentTemplate>
                                    <pw:CustomHorizontalAppointmentTemplate ID="HorizontalAppointment4" runat="server" />
                                </HorizontalAppointmentTemplate>
                                <HorizontalSameDayAppointmentTemplate>
                                    <pw:CustomHorizontalSameDayAppointmentTemplate ID="HorizontalSameDayAppointment3"
                                        runat="server" />
                                </HorizontalSameDayAppointmentTemplate>
                            </Templates>
                        </MonthView>
                        <TimelineView>
                            <Scales>
                                <cc1:TimeScaleYear Enabled="False"></cc1:TimeScaleYear>
                                <cc1:TimeScaleQuarter Enabled="False"></cc1:TimeScaleQuarter>
                                <cc1:TimeScaleMonth Enabled="False"></cc1:TimeScaleMonth>
                                <cc1:TimeScaleWeek></cc1:TimeScaleWeek>
                                <cc1:TimeScaleDay></cc1:TimeScaleDay>
                                <cc1:TimeScaleHour Enabled="False"></cc1:TimeScaleHour>
                                <cc1:TimeScaleFixedInterval Enabled="False"></cc1:TimeScaleFixedInterval>
                            </Scales>
                        </TimelineView>
                    </Views>
                    <OptionsCustomization AllowAppointmentCopy="None" AllowAppointmentDrag="None"
                        AllowAppointmentDragBetweenResources="None" AllowAppointmentResize="None" />
                </dxs:ASPxScheduler>
            </dx:PanelContent>
        </PanelCollection>
    </dx:ASPxCallbackPanel>
</div>

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
