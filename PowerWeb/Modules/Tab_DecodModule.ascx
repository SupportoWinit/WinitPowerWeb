<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_DecodModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_DecodModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<dx:ASPxGridView ID="gvTab_Decods" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvTab_Decods_DataBinding"
    OnInitNewRow="gvTab_Decods_InitNewRow"
    OnRowValidating="gvTab_Decods_RowValidating"
    OnRowInserting="gvTab_Decods_RowInserting"  
    OnRowDeleting="gvTab_Decods_RowDeleting" 
    OnRowUpdating="gvTab_Decods_RowUpdating">    
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
                <Image Url="../Icons/Edit/Edit.png"/>
            </EditButton>
            <ClearFilterButton Visible="True">
                <Image Url="../Icons/Undo/Undo.png" />
            </ClearFilterButton>
        </dx:GridViewCommandColumn> 
        <dx:GridViewDataTextColumn FieldName="Tab_Decod_Id" Visible="False">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Gruppo_Tab" VisibleIndex="10"  Width ="10%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Nome_Tab" VisibleIndex="20" Width ="20%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Chiave_Tab" VisibleIndex="30" Width ="15%">
        </dx:GridViewDataTextColumn>      
        <dx:GridViewDataTextColumn FieldName="Decodifica_Tab" VisibleIndex="40" Width= "50%">      
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Campo1_Tab" VisibleIndex="50" Width= "10%">      
        </dx:GridViewDataTextColumn>      
    </Columns>    
</dx:ASPxGridView>
