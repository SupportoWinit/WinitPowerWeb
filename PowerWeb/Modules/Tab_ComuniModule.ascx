<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="Tab_ComuniModule.ascx.cs"
    Inherits="PowerWeb.Modules.Tab_ComuniModule" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxGridView" TagPrefix="dx" %>
<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>

<table>
    <tr>
<dx:ASPxGridView ID="gvTabComuni" runat="server" AutoGenerateColumns="False" Width="100%" OnDataBinding="gvTabComuni_DataBinding" 
    OnInitNewRow="gvTabComuni_InitNewRow"
    OnRowValidating="gvTabComuni_RowValidating"
    OnRowInserting="gvTabComuni_RowInserting"
    OnRowUpdating="gvTabComuni_RowUpdating"      
    OnRowDeleting="gvTabComuni_RowDeleting">      
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
        <dx:GridViewDataTextColumn FieldName="Tab_Comuni_Id" Visible="false" ShowInCustomizationForm = "false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Cap_Tab_Comuni" VisibleIndex="20" Width="10%">
        </dx:GridViewDataTextColumn>       
        <dx:GridViewDataTextColumn FieldName="Codice_Istat_Tab_Comuni" VisibleIndex="50" Width="5%">
        </dx:GridViewDataTextColumn>             
        <dx:GridViewDataTextColumn FieldName="Codice_Luogo_Tab_Comuni" VisibleIndex="40" Width="5%">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Codice_Prov_Tab_Comuni" Visible="false">
        </dx:GridViewDataTextColumn>
        <dx:GridViewDataTextColumn FieldName="Luogo_Tab_Comuni" VisibleIndex="10" Width="65%">
        </dx:GridViewDataTextColumn> 
        <dx:GridViewDataComboBoxColumn FieldName="Tab_Prov_Id" VisibleIndex="30" Width="5%">                  
        </dx:GridViewDataComboBoxColumn> 
        
    </Columns>   
</dx:ASPxGridView>
        </tr>
    </table>


