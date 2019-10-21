<%@ Page Title="Registrati" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="RegisterPage.aspx.cs" Inherits="PowerWeb.Pages.Account.RegisterPage" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">

    <script type="text/javascript" src="/Scripts/Custom/RegisterPage/RegisterController.js"></script>
    <link href="/Styles/CSS/RegisterPage/RegisterPage.css" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">

    <div ng-app="registerModule" ng-controller="registerController">
        <div class="form-container">
            <div class="form-wrapper">
                <span class="form-title">Registrati</span>

                <div dx-validation-group="registerValidationGroupOptions" class="validationGroup">

                    <div ng-model="userName" dx-text-box="userNameTextBoxOptions" dx-validator="userNameTextBoxValidator"></div>

                    <div ng-model="password" dx-text-box="passWordTextBoxOptions" dx-validator="passwordTextBoxValidator"></div>

                    <div ng-model="confirmPassword" dx-text-box="confirmPassWordTextBoxOptions" dx-validator="confirmPassWordTextBoxValidator"></div>

                    <div ng-model="secretQuestion" dx-text-box="secretQuestionTextBoxOptions" dx-validator="secretQuestionTextBoxValidator"></div>

                    <div ng-model="secretAnswer" dx-text-box="secretAnswerTextBoxOptions" dx-validator="secretAnswerTextBoxValidator"></div>

                    <div dx-validation-summary="{}"></div>

                    <div class="form-submit-container">
                        <div dx-button="submitButtonOptions"></div>
                    </div>

                    <div class="form-actions" >
                        <div class="infoButton" ng-mouseover="onInfoButtonHoverStart()" ng-mouseleave="onInfoButtonHoverEnd()">?</div>
                    </div>

                </div>
            </div>

            <div dx-popup="errorPoup"></div>

             <div dx-popover="{
                    width: 200,
                    height: 'auto',
                    target: '.infoButton',
                    bindingOptions: {
                        visible: 'helpPopOverVisible'
                    },
                    onShowing:popoverShow
                }">

                 <p style="color:white" align="center">State per creare un nuovo utente amministratore</p>

             </div>

        </div>
    </div>
</asp:Content>
