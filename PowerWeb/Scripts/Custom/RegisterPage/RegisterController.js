
var _textBoxes = {

}

var _buttons = {

}

var groupValidator;

angular.module("registerModule", ['dx']).controller("registerController", function ($scope) {

    $scope.userName;
    $scope.password;
    $scope.confirmPassword;

    $scope.helpPopOverVisible = false;

    //#region CONFIGURAZIONE WIDGET

    $scope.userNameTextBoxOptions = {
        onInitialized: function (e) {
            _textBoxes.userNameTextBox = e.component;
        },
        placeholder: "Nome utente"
    }

    $scope.passWordTextBoxOptions = {
        mode: "password",
        onInitialized: function (e) {
            _textBoxes.newPassWord = e.component;
        },
        placeholder: "Password"
    }

    $scope.confirmPassWordTextBoxOptions = {
        mode: "password",
        onInitialized: function (e) {
            _textBoxes.confirmPassWordTextBox = e.component;
        },
        placeholder: "Verifica password"
    }

    $scope.secretQuestionTextBoxOptions = {
        onInitialized: function (e) {
            _textBoxes.secretQuestionTextBox = e.component;
        },
        placeholder: "Domanda segreta"
    }

    $scope.secretAnswerTextBoxOptions = {
        onInitialized: function (e) {
            _textBoxes.secretAnswerTextBox = e.component;
        },
        placeholder: "Risposta segreta"
    }

    $scope.submitButtonOptions = {
        elementAttr: { "class": "form-submit-button" },
        onClick: function (e) {
            if (groupValidator.validate().isValid) {
                jQuery.ajax({
                    contentType: "application/json",
                    type: "POST",
                    url: "RegisterPage.aspx/CreateUser",
                    data: JSON.stringify({ userName: $scope.userName, password: $scope.password, question: $scope.secretQuestion , answer:$scope.secretAnswer})
                }).done(function (data) {
                    let result = JSON.parse(data.d);
                    if (result.isValid) {
                        DevExpress.ui.dialog.alert("Utente creato con successo. Sarete reindirizzati automaticamente alla pagina di login!", "Utente inizializzato correttamente").done(function () { window.location.replace("LoginPage.aspx") })
                    } else {

                        if (result.validationError)
                            DevExpress.ui.notify(result.validationError, "error", 3000);

                        result.formErrors.forEach((item) => {
                            _textBoxes[item.field].element().dxValidator("instance").option("validationRules").push({ type: 'custom', message: item.message, reevaluate: true, validationCallback: function (options) { return false; } });
                        });

                        groupValidator.validate()
                        result.formErrors.forEach((item) => {
                            _textBoxes[item.field].element().dxValidator("instance").option("validationRules").pop();
                        });

                    }
                }).fail(function (err) {
                    console.log(err);
                });
            };
        },
        onInitialized: function (e) {
            _buttons.submitButton = e.component;
        },
        text: "Registrati"
    }

    $scope.infoButton = {
        elementAttr: { "class": "info" },
        icon: "help"
    }

    //#endregion CONFIGURAZIONE WIDGET

    //#region VALIDATORS

    $scope.userNameTextBoxValidator = {
        validationRules: [
            { type: 'required', message: 'Il campo è obbligatorio!' }
        ]
    }

    $scope.passwordTextBoxValidator = {
        validationRules: [
            { type: 'required', message: 'Il campo è obbligatorio!' }
        ]
    }

    $scope.confirmPassWordTextBoxValidator = {
        validationRules: [
            { type: 'required', message: 'Il campo è obbligatorio!' },
            { type: 'compare', message: "Le due password non corrispondono!", comparisonTarget: function () { if ($scope.password) { return $scope.password; } } }
        ]
    }

    $scope.secretQuestionTextBoxValidator = {
        validationRules: [
            { type: 'required', message: 'Il campo è obbligatorio!' }
        ]
    }
    $scope.secretAnswerTextBoxValidator = {
        validationRules: [
            { type: 'required', message: 'Il campo è obbligatorio!' }
        ]
    }

    $scope.registerValidationGroupOptions = {
        onInitialized: function (e) {
            groupValidator = e.component;
        }
    }

    //#endregion

    //#region FUNZIONI ANGULAR

    $scope.onInfoButtonHoverStart = function () {
        $scope.helpPopOverVisible = true;
    }

    $scope.onInfoButtonHoverEnd = function () {
        $scope.helpPopOverVisible = false;
    }

    $scope.popoverShow = function (e) {
        e.component.content().css("background-color", "#333333");
    }

    //#endregion FUNZIONI ANGULAR

});