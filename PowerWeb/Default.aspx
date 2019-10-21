<%@ Page Title="Home page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="PowerWeb._Default" %>

<%@ Register Assembly="DevExpress.Web.v14.1, Version=14.1.9.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a"
    Namespace="DevExpress.Web.ASPxEditors" TagPrefix="dx" %>
<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <div id="LogoWinit" style="text-align: center;">
        <br />
        <br />
        <div id="NomeAzienda" style="text-align: center">
            <table style="width:100%">
                <tr>
                    <td style="text-align: center; vertical-align: middle; width:100%">
                        <dx:ASPxImage ID="ASPxImage1" runat="server" ImageUrl="~/Images/PowerWeb.png" Width="600px">
                        </dx:ASPxImage>
                    </td>
                </tr>
                <tr>
                    <td style="text-align: center; vertical-align: middle; width:100%">
                        <h6>
                            <dx:ASPxLabel ID="lblFor" runat="server" Style="text-align: center"
                                Text="Attivato per il cliente:" Font-Size="20pt">
                            </dx:ASPxLabel>
                        </h6>
                    </td>
                </tr>
                <tr>

                    <td style="text-align: center; vertical-align: middle; width:100%">
                        <h2>
                            <dx:ASPxLabel ID="lblAzienda" runat="server"
                                Text="ASPxLabel" Font-Size="50pt">
                            </dx:ASPxLabel>
                        </h2>
                    </td>

                </tr>
                <tr>
                    <td style="text-align: center; vertical-align: middle; width:100%">
                        <dx:ASPxBinaryImage ID="CompanyLogo" runat="server" OnInit="CompanyLogo_Init" Width="200px">
                        </dx:ASPxBinaryImage>
                    </td>
                </tr>
            </table>
            <br />
            <br />
            <br />
            <br />

        </div>
        <br />
        <div class="centeredDivPadding">
            <dx:ASPxLabel ID="lblExpireDate" runat="server" />
        </div>
        <div class="centeredDivPadding">
            <dx:ASPxLabel ID="lblExpireDateWarning" runat="server" />
        </div>
        <br />
        <br />
        <br />
    </div>
</asp:Content>
