<%@ Page Title="PowerWeb - Parametri" Language="C#" MasterPageFile="~/GridMasterPage.master" AutoEventWireup="true"
    CodeBehind="ParamPage.aspx.cs" Inherits="PowerWeb.Pages.ParamPage" EnableSessionState="ReadOnly" %>
<%@ MasterType  virtualPath="~/GridMasterPage.master"%>

<%@ Register TagPrefix="pw" TagName="ParamModule" Src="~/Modules/ParamModule.ascx" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <pw:ParamModule runat="server" ID="mdlParamModule" />
</asp:Content>
