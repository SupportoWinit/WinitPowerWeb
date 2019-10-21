<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Aut_StrModule.ascx.cs" Inherits="PowerWeb.Modules.Aut_StrModule" %>
<%@ Register TagPrefix="dx" Namespace="DevExpress.Web.ASPxGridView" Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" %>


<dx:ASPxGridView ID="gvAutStr" runat="server" AutoGenerateColumns="False" Width="100%"
    OnDataBinding="gvAutStr_OnDataBinding"
    OnInitNewRow="gvAutStr_OnInitNewRow"
    OnRowValidating="gvAutStr_OnRowValidating"
    OnRowInserting="gvAutStr_OnRowInserting"
    OnRowUpdating="gvAutStr_OnRowUpdating"
    OnRowDeleting="gvAutStr_OnRowDeleting">
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
        <dx:GridViewDataTextColumn FieldName="Aut_Str_Id" Visible="False" VisibleIndex="10" />
        <dx:GridViewDataComboBoxColumn FieldName="Aut_Str_Col_Id" VisibleIndex="20" />
        <dx:GridViewDataSpinEditColumn FieldName="Aut_Str_Num_Ore" VisibleIndex="30" />
        <dx:GridViewDataDateColumn FieldName="Aut_Str_Data_Inizio" VisibleIndex="40" />
        <dx:GridViewDataDateColumn FieldName="Aut_Str_Data_Fine" VisibleIndex="50" />
        <dx:GridViewDataComboBoxColumn FieldName="Aut_Str_Resp_Id" VisibleIndex="60"/>
        <dx:GridViewDataTextColumn FieldName="Aut_Str_Note" VisibleIndex="70" />
    </Columns>
</dx:ASPxGridView>
