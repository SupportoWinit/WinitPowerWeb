<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RegReportModule.ascx.cs"
    Inherits="PowerWeb.Modules.ReportModules.RegReportModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxPanel" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridLookup" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<style type="text/css">
    .style1
    {
        text-align: left;
    }
</style>
<dx:ASPxPanel ID="customPrintOptionsPanel" runat="server" Width="100%">
    <PanelCollection>
        <dx:PanelContent runat="server" SupportsDisabledAttribute="True">
            <table style="width: 100%">
                <tr>
                    <td>
                        Collaboratore
                    </td>
                    <td>
                        <dx:ASPxGridLookup ID="glCol" runat="server" AutoGenerateColumns="False" Width="100%"
                            SelectionMode="Multiple" TextFormatString="{3}" MultiTextSeparator=", ">
                            <Columns>
                                <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0" />
                                <dx:GridViewDataTextColumn FieldName="Col_Id" ShowInCustomizationForm="True" VisibleIndex="1"
                                    Visible="false">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Codice_Collaboratore" ShowInCustomizationForm="True"
                                    VisibleIndex="2">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Cognome_Col" ShowInCustomizationForm="True"
                                    VisibleIndex="3">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Nome_Col" ShowInCustomizationForm="True" VisibleIndex="4">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Col" ShowInCustomizationForm="True"
                                    VisibleIndex="5">
                                </dx:GridViewDataCheckColumn>
                            </Columns>
                            <GridViewProperties>
                                <SettingsBehavior AllowFocusedRow="True" />
                                <Settings ShowFilterRow="True" ShowFilterRowMenu="True" />
                            </GridViewProperties>
                        </dx:ASPxGridLookup>
                    </td>
                    <td>
                        Cant.
                    </td>
                    <td>
                        <dx:ASPxGridLookup ID="glCant" runat="server" AutoGenerateColumns="False" Width="100%"
                            SelectionMode="Multiple" TextFormatString="{2}" MultiTextSeparator=", ">
                            <Columns>
                                <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0" />
                                <dx:GridViewDataTextColumn FieldName="Cant_Id" ShowInCustomizationForm="True" VisibleIndex="1"
                                    Visible="false">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Codice_Cantiere" ShowInCustomizationForm="True"
                                    VisibleIndex="2">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Descrizione_Can" ShowInCustomizationForm="True"
                                    VisibleIndex="3">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataCheckColumn FieldName="DisAbilitazione_Can" ShowInCustomizationForm="True"
                                    VisibleIndex="4">
                                </dx:GridViewDataCheckColumn>
                            </Columns>
                            <GridViewProperties>
                                <SettingsBehavior AllowFocusedRow="True" />
                                <Settings ShowFilterRow="True" ShowFilterRowMenu="True" />
                            </GridViewProperties>
                        </dx:ASPxGridLookup>
                    </td>
                </tr>
                <tr>
                    <td>
                        Filiale Coll.
                    </td>
                    <td>
                        <dx:ASPxGridLookup ID="glFilCol" runat="server" AutoGenerateColumns="False" Width="100%"
                            SelectionMode="Multiple" TextFormatString="{1}" MultiTextSeparator=", ">
                            <Columns>
                                <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0" />
                                <dx:GridViewDataTextColumn FieldName="Tab_Decod_Id" ShowInCustomizationForm="True"
                                    VisibleIndex="1" Visible="false">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Chiave_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="2">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Decodifica_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="3">
                                </dx:GridViewDataTextColumn>
                            </Columns>
                            <GridViewProperties>
                                <SettingsBehavior AllowFocusedRow="True" />
                                <Settings ShowFilterRow="True" ShowFilterRowMenu="True" />
                            </GridViewProperties>
                        </dx:ASPxGridLookup>
                    </td>
                    <td>
                        Filiale Cant.
                    </td>
                    <td>
                        <dx:ASPxGridLookup ID="glFilCant" runat="server" AutoGenerateColumns="False" Width="100%"
                            SelectionMode="Multiple" TextFormatString="{1}" MultiTextSeparator=", ">
                            <Columns>
                                <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0" />
                                <dx:GridViewDataTextColumn FieldName="Tab_Decod_Id" ShowInCustomizationForm="True"
                                    VisibleIndex="1" Visible="false">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Chiave_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="2">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Decodifica_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="3">
                                </dx:GridViewDataTextColumn>
                            </Columns>
                            <GridViewProperties>
                                <SettingsBehavior AllowFocusedRow="True" />
                                <Settings ShowFilterRow="True" ShowFilterRowMenu="True" />
                            </GridViewProperties>
                        </dx:ASPxGridLookup>
                    </td>
                </tr>
                <tr>
                    <td>
                        Resp. Coll.
                    </td>
                    <td>
                        <dx:ASPxGridLookup ID="glRespCol" runat="server" AutoGenerateColumns="False" Width="100%"
                            SelectionMode="Multiple" TextFormatString="{1}" MultiTextSeparator=", ">
                            <Columns>
                                <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0" />
                                <dx:GridViewDataTextColumn FieldName="Tab_Decod_Id" ShowInCustomizationForm="True"
                                    VisibleIndex="1" Visible="false">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Chiave_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="2">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Decodifica_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="3">
                                </dx:GridViewDataTextColumn>
                            </Columns>
                            <GridViewProperties>
                                <SettingsBehavior AllowFocusedRow="True" />
                                <Settings ShowFilterRow="True" ShowFilterRowMenu="True" />
                            </GridViewProperties>
                        </dx:ASPxGridLookup>
                    </td>
                    <td>
                        Resp. Cant.
                    </td>
                    <td>
                        <dx:ASPxGridLookup ID="glRespCant" runat="server" AutoGenerateColumns="False" Width="100%"
                            SelectionMode="Multiple" TextFormatString="{1}" MultiTextSeparator=", ">
                            <Columns>
                                <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0" />
                                <dx:GridViewDataTextColumn FieldName="Tab_Decod_Id" ShowInCustomizationForm="True"
                                    VisibleIndex="1" Visible="false">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Chiave_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="2">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Decodifica_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="3">
                                </dx:GridViewDataTextColumn>
                            </Columns>
                            <GridViewProperties>
                                <SettingsBehavior AllowFocusedRow="True" />
                                <Settings ShowFilterRow="True" ShowFilterRowMenu="True" />
                            </GridViewProperties>
                        </dx:ASPxGridLookup>
                    </td>
                </tr>
<tr>
                    <td class="style1">
                        Motivazioni
                    </td>
                    <td>
                        <dx:ASPxGridLookup ID="glMotivazioni" runat="server" AutoGenerateColumns="False" Width="100%"
                            SelectionMode="Multiple" TextFormatString="{1}" MultiTextSeparator=", ">
                            <Columns>
                                <dx:GridViewCommandColumn ShowSelectCheckbox="True" VisibleIndex="0" />
                                <dx:GridViewDataTextColumn FieldName="Tab_Decod_Id" ShowInCustomizationForm="True"
                                    VisibleIndex="1" Visible="false">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Chiave_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="2">
                                </dx:GridViewDataTextColumn>
                                <dx:GridViewDataTextColumn FieldName="Decodifica_Tab" ShowInCustomizationForm="True"
                                    VisibleIndex="3">
                                </dx:GridViewDataTextColumn>
                            </Columns>
                            <GridViewProperties>
                                <SettingsBehavior AllowFocusedRow="True" />
                                <Settings ShowFilterRow="True" ShowFilterRowMenu="True" />
                            </GridViewProperties>
                        </dx:ASPxGridLookup>
                    </td>
                </tr>
            </table>
        </dx:PanelContent>
    </PanelCollection>
</dx:ASPxPanel>
