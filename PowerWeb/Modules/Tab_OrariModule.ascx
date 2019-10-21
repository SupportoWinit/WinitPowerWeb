<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_OrariModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_OrariModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxRoundPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxNavBar" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxFormLayout" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.ASPxScheduler.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxScheduler" TagPrefix="dxwschs" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.XtraScheduler.v14.1.Core, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.XtraScheduler" TagPrefix="cc1" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxCallbackPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>

<%@ Register TagPrefix="pw" TagName="CustomVerticalAppointmentTemplate" Src="CustomForms/VerticalAppointmentTemplate.ascx" %>
<%@ Register TagPrefix="pw" TagName="CustomHorizontalAppointmentTemplate" Src="CustomForms/HorizontalAppointmentTemplate.ascx" %>
<%@ Register TagPrefix="pw" TagName="CustomHorizontalSameDayAppointmentTemplate" Src="CustomForms/HorizontalSameDayAppointmentTemplate.ascx" %>
<script type="text/javascript">

    var isGenerateHoursEnabledFromdeTabOrari = false;
    var isGenerateHoursEnabledFromgvTabOrariTipo = false;

    function deTabOrari_OnValueChanged(s, e) {
        var date = deTabOrari.GetDate();
        if (date)
            isGenerateHoursEnabledFromdeTabOrari = true;
        else
            isGenerateHoursEnabledFromdeTabOrari = false;
        if (isGenerateHoursEnabledFromdeTabOrari && isGenerateHoursEnabledFromgvTabOrariTipo)
            btnGenerateHours.SetEnabled(true);
        else
            btnGenerateHours.SetEnabled(false);
    }

    function scTabOrari_OnEndCallback(s, e) {

        if (scRegV.cpScRegVAction) {
            gvRegVPanel.PerformCallback('reload');
        }
    }

    function gvTabOrariTipo_OnSelectionChanged(s, e) {
        s.GetSelectedFieldValues("Tab_Orario_Tipo_Id", GetSelectedFieldValuesCallback)
    }

    function GetSelectedFieldValuesCallback(values) {
        if (values.length > 0)
            isGenerateHoursEnabledFromgvTabOrariTipo = true;
        else
            isGenerateHoursEnabledFromgvTabOrariTipo = false;
        if (isGenerateHoursEnabledFromdeTabOrari && isGenerateHoursEnabledFromgvTabOrariTipo)
            btnGenerateHours.SetEnabled(true);
        else
            btnGenerateHours.SetEnabled(false);
    }

    function btnCloneTimetable_Click(s, e) {
        grid.PerformCallback("clone");
    }

    function gvTabOrariTipo_EndCallback(s, e) {
        if (s.cpCloneValidationError != '' && typeof s.cpCloneValidationError != 'undefined' && s.cpCloneValidationError != 'allOK') {
            DisplayDialogError('Errore validazione', s.cpCloneValidationError);
            s.cpCloneValidationError = ''
        }
        else if (s.cpCloneValidationError == 'allOK') {
            s.cpCloneValidationError = ''
            s.Refresh();
        }
    }

    // -------------------------------------- GESTIONE DELLA SELEZIONE SOLO MESE/ANNO IN CAMPO PERIODO -----------------------------------------
    function OndeTabOrari_Init(s, e) {
        var calendar = s.GetCalendar();
        calendar.owner = s;
        calendar.GetMainElement().style.opacity = '0';
    }

    function OndeTabOrari_DropDown(s, e) {
        var calendar = s.GetCalendar();
        var fastNav = calendar.fastNavigation;
        fastNav.activeView = calendar.GetView(0, 0);
        fastNav.Prepare();
        fastNav.GetPopup().popupVerticalAlign = "Below";
        fastNav.GetPopup().ShowAtElement(s.GetMainElement())

        fastNav.OnOkClick = function () {
            var parentDateEdit = this.calendar.owner;
            var currentDate = new Date(fastNav.activeYear, fastNav.activeMonth, 1);
            parentDateEdit.SetDate(currentDate);
            parentDateEdit.HideDropDown();
        }

        fastNav.OnCancelClick = function () {
            var parentDateEdit = this.calendar.owner;
            parentDateEdit.HideDropDown();
        }
    }

    //----------------------------------------- VISUALIZZAZIONE DEL PULSANTE DI CANCELLAZIONE DEL COMBOBOX -----------------------------------------
    function onCustomEditButtonComboBoxClick(s, e) {
        var combo = ASPxClientComboBox.Cast(s);
        combo.SetText(" ");
        combo.ShowDropDown();
        combo.SetSelectedItem(null);
        combo.SetSelectedIndex(-1);
        combo.PerformCallback();

    }
</script>
<div style="clear: both; width: 100%">
    <dx:ASPxFormLayout ID="ASPxFormLayout1" runat="server" Width="100%">
        <Items>
            <dx:LayoutGroup Caption="" SettingsItemHelpTexts-Position="Bottom" GroupBoxDecoration="None">
                <Items>
                    <dx:LayoutItem HelpText="" ShowCaption="False">
                        <LayoutItemNestedControlCollection>
                            <dx:LayoutItemNestedControlContainer>
                                <dx:ASPxRoundPanel ID="CloneTimetablePanel" runat="server" ShowCollapseButton="true" HeaderText="" Width="100%" Collapsed="true">
                                    <PanelCollection>
                                        <dx:PanelContent>
                                            <table>
                                                <tr>
                                                    <td>
                                                        <dx:ASPxLabel runat="server" ID="LblCloneTimetable" ClientInstanceName="lblCloneTimetable" />
                                                    </td>
                                                    <td>
                                                        <dx:ASPxDateEdit runat="server" ID="DeCloneNewDate" ClientInstanceName="deCloneNewDate" />
                                                    </td>
                                                    <td>
                                                        <dx:ASPxButton runat="server" ID="BtnCloneTimetable" ClientInstanceName="btnCloneTimetable" ClientSideEvents-Click="btnCloneTimetable_Click" UseSubmitBehavior="False" AutoPostBack="False">
                                                            <ClientSideEvents Click="btnCloneTimetable_Click" />
                                                        </dx:ASPxButton>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td>
                                                        <dx:ASPxLabel runat="server" ID="LblCloneTimetableDate" ClientInstanceName="lblCloneTimetableDate" />
                                                    </td>
                                                    <td>
                                                        <dx:ASPxDateEdit runat="server" ID="DeCloneTimetableFromDate" ClientInstanceName="deCloneTimetableFromDate" />
                                                    </td>
                                                    <td>
                                                        <dx:ASPxLabel runat="server" ID="LblIfNullLastTimetable" ClientInstanceName="lblIfNullLastTimetable" />
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td>
                                                        <dx:ASPxLabel runat="server" ID="LblCloneToTimetable" ClientInstanceName="lblCloneToTimetable" />
                                                    </td>
                                                    <td>
                                                        <dx:ASPxComboBox runat="server" ID="CmbCloneToTimetable" ClientInstanceName="cmbCloneToTimetable" />
                                                    </td>
                                                    <td>
                                                        <dx:ASPxLabel runat="server" ID="LblIfNullSelectedTimetable" ClientInstanceName="lblIfNullSelectedTimetable" />
                                                    </td>
                                                </tr>
                                            </table>
                                        </dx:PanelContent>
                                    </PanelCollection>
                                </dx:ASPxRoundPanel>
                            </dx:LayoutItemNestedControlContainer>
                        </LayoutItemNestedControlCollection>
                    </dx:LayoutItem>
                </Items>

                <SettingsItemHelpTexts Position="Bottom"></SettingsItemHelpTexts>
            </dx:LayoutGroup>
            <dx:LayoutGroup Caption="GESTIONE TIPO ORARI" SettingsItemHelpTexts-Position="Bottom" GroupBoxDecoration="HeadingLine">
                <Items>
                    <dx:LayoutItem HelpText="" ShowCaption="False">
                        <LayoutItemNestedControlCollection>
                            <dx:LayoutItemNestedControlContainer>
                                <dx:ASPxGridView ID="gvTabOrariTipo" runat="server" AutoGenerateColumns="False" Width="100%" ClientInstanceName="gvTabOrariTipo"
                                    SettingsBehavior-AllowSelectSingleRowOnly="true"
                                    OnRowUpdating="gvTabOrariTipo_RowUpdating"
                                    OnRowDeleting="gvTabOrariTipo_RowDeleting"
                                    OnRowInserting="gvTabOrariTipo_RowInserting"
                                    OnRowValidating="gvTabOrariTipo_OnRowValidating"
                                    OnInitNewRow="gvTabOrariTipo_OnInitNewRow"
                                    OnCustomCallback="gvTabOrariTipo_CustomCallback">
                                    <ClientSideEvents SelectionChanged="gvTabOrariTipo_OnSelectionChanged" EndCallback="function (s, e) { gvTabOrariTipo_EndCallback(s, e); }" />
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
                                        <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="5" ShowInCustomizationForm="false" Width="5%">
                                        </dx:GridViewCommandColumn>
                                        <dx:GridViewDataComboBoxColumn FieldName="Tab_Orari_Tipo_Entita_Rif" VisibleIndex="10" Width="20%">
                                        </dx:GridViewDataComboBoxColumn>
                                        <dx:GridViewDataTextColumn FieldName="Tab_Orari_Tipo_Desc" VisibleIndex="20" Width="70%">
                                        </dx:GridViewDataTextColumn>
                                        <dx:GridViewDataTextColumn FieldName="Tab_Orari_Tipo_Inizio_Not" VisibleIndex="30" Width="10%">
                                            <PropertiesTextEdit>
                                                <ClientSideEvents Validation="OnGridTimeSpanValidation" />
                                                <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                            </PropertiesTextEdit>
                                        </dx:GridViewDataTextColumn>
                                        <dx:GridViewDataTextColumn FieldName="Tab_Orari_Tipo_Fine_Not" VisibleIndex="40" Width="10%">
                                            <PropertiesTextEdit>
                                                <ClientSideEvents Validation="OnGridTimeSpanValidation" />
                                                <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                            </PropertiesTextEdit>
                                        </dx:GridViewDataTextColumn>
                                        <dx:GridViewDataCheckColumn FieldName="Tab_Orari_Tipo_NotDiu_Auto" VisibleIndex="50" Width="10%">
                                        </dx:GridViewDataCheckColumn>
                                    </Columns>
                                    <Templates>
                                        <DetailRow>

                                            <dx:ASPxGridView ID="gvTabOrari" runat="server" Width="100%" AutoGenerateColumns="False"
                                                OnInit="gvTabOrari_Init"
                                                OnInitNewRow="gvTabOrari_InitNewRow"
                                                OnRowValidating="gvTabOrari_RowValidating"
                                                OnRowInserting="gvTabOrari_RowInserting"
                                                OnRowUpdating="gvTabOrari_RowUpdating"
                                                OnRowDeleting="gvTabOrari_RowDeleting"
                                                OnBeforePerformDataSelect="gvTabOrari_BeforePerformDataSelect">
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
                                                    <dx:GridViewDataTextColumn FieldName="Tab_Orari_Id" Visible="false">
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Cant_Id" VisibleIndex="10" Width="20%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataComboBoxColumn FieldName="Col_Id" VisibleIndex="10" Width="20%">
                                                    </dx:GridViewDataComboBoxColumn>
                                                    <dx:GridViewDataDateColumn FieldName="Data_Inizio" VisibleIndex="30" Width="7%">
                                                    </dx:GridViewDataDateColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Ora_E" VisibleIndex="31" Width="8%">
                                                        <PropertiesTextEdit>
                                                            <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                                                            <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                                        </PropertiesTextEdit>
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="Ora_U" VisibleIndex="32" Width="8%">
                                                        <PropertiesTextEdit>
                                                            <ClientSideEvents Validation="OnGridTimeSpanValidation"></ClientSideEvents>
                                                            <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                                        </PropertiesTextEdit>
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataTextColumn FieldName="DisplayedDuration" VisibleIndex="40" Width="7%">
                                                        <PropertiesTextEdit>
                                                            <ClientSideEvents Validation="OnGridTimeSpanValidation" />
                                                            <MaskSettings Mask="00:00" IncludeLiterals="None" />
                                                        </PropertiesTextEdit>
                                                    </dx:GridViewDataTextColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="G1" VisibleIndex="50" Width="6%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="G2" VisibleIndex="60" Width="6%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="G3" VisibleIndex="70" Width="6%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="G4" VisibleIndex="80" Width="6%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="G5" VisibleIndex="90" Width="6%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="G6" VisibleIndex="100" Width="6%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="G7" VisibleIndex="110" Width="6%">
                                                    </dx:GridViewDataCheckColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Ripetizione" VisibleIndex="120" Width="8%">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataSpinEditColumn FieldName="Sequenza" VisibleIndex="130" Width="8%">
                                                        <PropertiesSpinEdit DisplayFormatString="g"></PropertiesSpinEdit>
                                                    </dx:GridViewDataSpinEditColumn>
                                                    <dx:GridViewDataCheckColumn FieldName="Usa_Pausa" VisibleIndex="140" Width="6%" />
                                                    <dx:GridViewDataCheckColumn FieldName="Orario_Mensile" VisibleIndex="140" Width="6%" />
                                                </Columns>
                                            </dx:ASPxGridView>
                                        </DetailRow>
                                    </Templates>
                                    <SettingsBehavior AllowSelectSingleRowOnly="True"></SettingsBehavior>
                                    <SettingsDetail ShowDetailRow="true" />
                                </dx:ASPxGridView>
                            </dx:LayoutItemNestedControlContainer>
                        </LayoutItemNestedControlCollection>
                    </dx:LayoutItem>
                </Items>
                <SettingsItemHelpTexts Position="Bottom"></SettingsItemHelpTexts>
            </dx:LayoutGroup>
        </Items>
    </dx:ASPxFormLayout>
</div>
<div style="margin-top: 10px; clear: both;">
    <dx:ASPxDateEdit ID="deTabOrari" runat="server" ClientInstanceName="deTabOrari" CssClass="headerButtons" UseMaskBehavior="true" EditFormatString="MMM yyyy" DisplayFormatString="MMM yyyy" ShowShadow="False" OnInit="deTabOrari_OnInit">
        <ClientSideEvents ValueChanged="deTabOrari_OnValueChanged" DropDown="OndeTabOrari_DropDown" Init="OndeTabOrari_Init" />
    </dx:ASPxDateEdit>
    <dx:ASPxButton ID="btnGenerateHours" ClientInstanceName="btnGenerateHours" runat="server" AutoPostBack="False" Text="Generate" ClientEnabled="false"
        CssClass="headerButtons" UseSubmitBehavior="false">
        <ClientSideEvents Click="function(s, e) { scTabOrariPanel.PerformCallback(); }" />
    </dx:ASPxButton>
</div>
<div style="clear: both">
    <dx:ASPxCallbackPanel ID="scTabOrariPanel" runat="server" Width="100%" ClientInstanceName="scTabOrariPanel"
        OnCallback="scTabOrariPanel_Callback">
        <PanelCollection>
            <dx:PanelContent ID="PanelContent1" runat="server">
                <dxwschs:ASPxScheduler ID="scTabOrari" runat="server" ClientInstanceName="scTabOrari" Width="100%" ActiveViewType="Timeline">
                    <ResourceColorSchemas>
                        <cc1:SchedulerColorSchema Cell="255, 244, 188" CellBorder="243, 228, 177" CellBorderDark="234, 208, 152" CellLight="255, 255, 213" CellLightBorder="255, 239, 199" CellLightBorderDark="246, 219, 162"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="Control" CellBorder="ControlDark" CellBorderDark="ControlDark" CellLight="Window" CellLightBorder="ControlDark" CellLightBorderDark="ControlDark"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="179, 212, 151" CellBorder="168, 203, 138" CellBorderDark="140, 180, 104" CellLight="213, 236, 188" CellLightBorder="205, 228, 180" CellLightBorderDark="186, 209, 162"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="139, 158, 191" CellBorder="128, 147, 181" CellBorderDark="97, 116, 152" CellLight="207, 216, 230" CellLightBorder="193, 201, 219" CellLightBorderDark="161, 175, 204"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="190, 134, 161" CellBorder="180, 124, 149" CellBorderDark="156, 101, 122" CellLight="227, 203, 214" CellLightBorder="218, 189, 199" CellLightBorderDark="197, 163, 171"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="137, 177, 167" CellBorder="123, 168, 156" CellBorderDark="84, 142, 128" CellLight="193, 214, 209" CellLightBorder="174, 202, 195" CellLightBorderDark="145, 182, 173"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="247, 180, 127" CellBorder="235, 167, 113" CellBorderDark="202, 131, 71" CellLight="250, 208, 174" CellLightBorder="238, 196, 163" CellLightBorderDark="225, 166, 118"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="221, 140, 142" CellBorder="210, 129, 131" CellBorderDark="179, 100, 101" CellLight="239, 200, 201" CellLightBorder="233, 187, 189" CellLightBorderDark="222, 164, 166"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="137, 150, 132" CellBorder="129, 138, 122" CellBorderDark="102, 100, 89" CellLight="208, 216, 203" CellLightBorder="196, 207, 191" CellLightBorderDark="172, 181, 169"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="0, 199, 200" CellBorder="0, 186, 187" CellBorderDark="0, 151, 153" CellLight="168, 236, 236" CellLightBorder="144, 226, 227" CellLightBorderDark="84, 203, 204"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="168, 148, 207" CellBorder="155, 136, 194" CellBorderDark="118, 99, 155" CellLight="221, 213, 236" CellLightBorder="210, 199, 230" CellLightBorderDark="185, 169, 216"></cc1:SchedulerColorSchema>
                        <cc1:SchedulerColorSchema Cell="204, 204, 204" CellBorder="189, 189, 189" CellBorderDark="121, 121, 121" CellLight="230, 230, 230" CellLightBorder="204, 204, 204" CellLightBorderDark="177, 177, 177"></cc1:SchedulerColorSchema>
                    </ResourceColorSchemas>

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
                    <%--<ClientSideEvents EndCallback="scTabOrari_OnEndCallback" />--%>
                </dxwschs:ASPxScheduler>
            </dx:PanelContent>
        </PanelCollection>
    </dx:ASPxCallbackPanel>
</div>
