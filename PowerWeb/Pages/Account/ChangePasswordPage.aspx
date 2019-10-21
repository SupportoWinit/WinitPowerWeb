<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.master" CodeBehind="ChangePasswordPage.aspx.cs" Inherits="PowerWeb.Pages.Account.ChangePasswordPage" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">

    <script type="text/javascript" src="/Scripts/Custom/ChangePasswordPage/ChangePasswordController.js"></script>
    <script type="text/javascript">function getSecretQuestion(){return "<%=secretQuestion%>";}</script>
    <link href="/Styles/CSS/ChangePasswordPage/ChangePasswordPage.css" rel="stylesheet" type="text/css" />
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <div ng-app="changePasswordModule" ng-controller="changePasswordController">
        <div class="form-container">
            <div class="form-wrapper">
                <span class="form-title">Reset password</span>

                <p>Utente : <%=user%></p>

                <div dx-validation-group="changePasswordValidationGroupOptions" class="validationGroup">

                    <div dx-text-box="secretQuestionTextBoxOptions" dx-validator="{name:'Nuova password', validationRules: [{type: 'required', message:'Il campo PASSWORD è obbligatorio!'}]}"></div>

                    <div dx-text-box="answerTextBoxOptions" dx-validator="{name:'Conferma password', validationRules: [{type: 'required', message:'Il campo CONFERMA PASSWORD è obbligatorio!'}]}"></div>

                     <div dx-validation-summary="{}"></div>

                </div>
                
                <div dx-button="submitButtonOptions"></div>

            </div>
        </div>

    </div>
</asp:Content>
