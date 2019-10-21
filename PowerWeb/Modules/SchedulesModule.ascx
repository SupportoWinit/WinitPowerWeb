<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="SchedulesModule.ascx.cs"
    Inherits="PowerWeb.Modules.SchedulesModule" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxGridView" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>

    <%@ Register assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" namespace="DevExpress.Web.ASPxEditors" tagprefix="dx" %>

    <dx:ASPxGridView ID="gvSchedule" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvSchedule_DataBinding" OnRowDeleting="gvSchedule_RowDeleting">
    <Columns>
        <dx:GridViewCommandColumn VisibleIndex="0" Width="100px" ButtonType="Image">
            <CustomButtons>
                <dx:GridViewCommandColumnCustomButton ID="delete">
                    <Image ToolTip="Delete" Url="../Icons/Delete/Delete.png" />
                </dx:GridViewCommandColumnCustomButton>
                <dx:GridViewCommandColumnCustomButton ID="view">
                    <Image ToolTip="View" Url="../Icons/Search/Search.png" />
                </dx:GridViewCommandColumnCustomButton>
            </CustomButtons>
            <ClearFilterButton Visible="True">
                <Image Url="../Icons/Undo/Undo.png" />
            </ClearFilterButton>
        </dx:GridViewCommandColumn>       
        <dx:GridViewDataTextColumn FieldName="ScheduleId" Visible="False" VisibleIndex="0" />
        <dx:GridViewDataTextColumn FieldName="CustomerPublicKey" Visible="False" VisibleIndex="10" />
        <dx:GridViewDataTextColumn FieldName="ScheduleType" Visible="True" VisibleIndex="20" />
        <dx:GridViewDataTextColumn FieldName="JobType" Visible="True" VisibleIndex="30" />
        <dx:GridViewDataDateColumn FieldName="ScheduleCreationDateTime" Visible="True" VisibleIndex="40" />
        <dx:GridViewDataDateColumn FieldName="ScheduleEditDateTime" Visible="True" VisibleIndex="50" />
        <dx:GridViewDataTextColumn FieldName="ScheduleParameters" Visible="True" VisibleIndex="60" />
        <dx:GridViewDataTextColumn FieldName="ScheduleExclusions" Visible="True" VisibleIndex="70" />
        <dx:GridViewDataTextColumn FieldName="JobParameters" Visible="True" VisibleIndex="80" />
        <dx:GridViewDataDateColumn FieldName="ScheduleStartDateTime" Visible="True" VisibleIndex="90" />
        <dx:GridViewDataDateColumn FieldName="ScheduleEndDateTime" Visible="True" VisibleIndex="100" />
        <dx:GridViewDataComboBoxColumn FieldName="OwnerUserId" Visible="False" VisibleIndex="110" />
        <dx:GridViewDataTextColumn FieldName="OwnerUserLevel" Visible="False" VisibleIndex="120" />
    </Columns>
</dx:ASPxGridView>

