var module = angular.module('loginModule', ['dx']);

var form = {
}

var _PassWordBox;
var _validationGroup;



module.controller("loginController", function ($scope) {

    $scope.isToChangePsw = false;
    $scope.isToCompleteSecretQuestion = false;
    $scope.isToCompleteSecretAnswer = false;
    $scope.buttonLoginText = "Login";
    $scope.enableDoubleCheck = document.getElementById("enableDoubleCheck").value == "True";

    $scope.isSuperUserRequired = false;

    //#region CONFIGURAZIONE WIDGETS

    $scope.requestSuperUserCheckBoxOption = {
        bindingOptions: {
            value: 'isSuperUserRequired'
        },
        onInitialized: function (e) {
            $scope.superUserCheckBox = e.component;
        },
        onValueChanged: function (e) {
            if (e.value) {
                form.superUserName.element().dxValidator({ name: 'SuperUtente', validationRules: [{ type: 'required', message: 'Il campo PASSWORD è obbligatorio!' }] });
                form.superUserPassword.element().dxValidator({ name: 'SuperPassword', validationRules: [{ type: 'required', message: 'Il campo PASSWORD è obbligatorio!' }] });
            } else {
                form.superUserName.element().dxValidator({ name: 'SuperUtente', validationRules: [] });
                form.superUserPassword.element().dxValidator({ name: 'SuperPassword', validationRules: [] });
            }
        },
        text: "Richiedi utente supervisore",
        visible: $scope.enableDoubleCheck
    }

    $scope.userNameTextBoxOptions = {
        elementAttr: { "class": "form-input" },
        onInitialized: function (e) {
            form.userName = e.component;
        },
        placeholder: "Nome utente"
    }

    $scope.passWordTextBoxOptions = {
        elementAttr: { "class": "form-input" },
        mode: "password",
        onInitialized: function (e) {
            form.passWord = e.component;
        },
        placeholder: "Password"
    }

    $scope.superUserNameTextBoxOptions = {
        bindingOptions: {
            visible: 'isSuperUserRequired'
        },
        elementAttr: { "class": "form-input" },
        onInitialized: function (e) {
            form.superUserName = e.component;
        },
        placeholder: "Nome utente amministratore"
    }

    $scope.superUserPasswordTextBoxOptions = {
        bindingOptions: {
            visible: 'isSuperUserRequired'
        },
        elementAttr: { "class": "form-input" },
        mode: "password",
        onInitialized: function (e) {
            form.superUserPassword = e.component;
        },
        placeholder: "Password amministratore"
    }

    $scope.newPassWordTextBoxOptions = {
        bindingOptions: {
            visible: "isToChangePsw"
        },
        elementAttr: { "class": "form-input" },
        mode: "password",
        onInitialized: function (e) {
            form.newPassWord = e.component;
        },
        placeholder: "Nuova password",
    }

    $scope.secretQuestionTextBoxOptions = {
        bindingOptions: {
            visible: "isToCompleteSecretQuestion"
        },
        elementAttr: { "class": "form-input" },
        onInitialized: (e) => {
            form.secretQuestion = e.component;
        },
        placeholder: "Domanda segreta",
    }

    $scope.secretAnswerTextBoxOptions = {
        bindingOptions: {
            visible: "isToCompleteSecretAnswer"
        },
        elementAttr: { "class": "form-input" },
        onInitialized: (e) => {
            form.secretAnswer = e.component;
        },
        placeholder: "Risposta segreta",
    }

    $scope.remMeCheckBoxOptions = {
        elementAttr: { "class": "form-remember" },
        text: "Ricordamelo"
    }

    $scope.submitButtonOptions = {
        bindingOptions: {
            onClick: 'validate',
            text: 'buttonLoginText'
        },
        elementAttr: { "class": "form-submit-button" },
        text: 'Login',
    }

    $scope.forgetPasswordButtonClick = function () {

        if (form.userName.option("value")) {
            jQuery.ajax({

                url: "LoginPage.aspx/ChangePassword",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ userName: form.userName.option("value") })

            }).done(function (data) {
                if (data.d == "true")
                    window.location.href = "ChangePasswordPage.aspx?user=" + form.userName.option("value");
                else
                    DevExpress.ui.notify("L'utente inserito non esiste!", "warning", 3000);
            }).fail(function (data) {
                console.log(data);
            });
        } else {
            DevExpress.ui.notify("Inserire un utente!");
        }
    }

    $scope.loginValidationGroupOptions = {
        onInitialized: function (e) {
            _validationGroup = e.component;
        }
    }


    //#endregion CONFIGURAZIONE WIDGETS

    $scope.catchEnterPress = function (event) {
        if (event.which == 13) {
            $scope.validate();
        }
    }

    //Viene chiamata questa funzione dopo aver ricevuto risposta dal server e per utilizzare quindi i risultati
    $scope.afterLoginValidation = function (validationResult) {
        console.log(validationResult);
        if (validationResult.isValid) {
            if (validationResult.validationError) {
                DevExpress.ui.dialog.alert(validationResult.validationError, 'Attenzione').done(function () { window.location.replace("/../../Default.aspx") });
            }
            else {
                window.location.href = "/../../Default.aspx"
            }
        } else {
            if (Object.keys(validationResult.formErrors).length > 0) {

                validationResult.formErrors.forEach((item) => {
                    form[item.field].element().dxValidator("instance").option("validationRules").push({ type: 'custom', message: item.message, validationCallback: function (a) { return false; } });
                }); 

                _validationGroup.validate();

                validationResult.formErrors.forEach((item) => {
                    form[item.field].element().dxValidator("instance").option("validationRules").pop();
                });

            } else if (validationResult.missingAdminUser) {
                if (validationResult.isWinitUser) {
                    DevExpress.ui.notify(validationResult.validationError, "warning", 3000);
                    setTimeout(()  => window.location.replace("RegisterPage.aspx"), 3000);
                } else {
                    DevExpress.ui.dialog.alert(validationResult.validationError, 'Attenzione');
                }
            }
            else if (validationResult.fired) {
                DevExpress.ui.dialog.alert(validationResult.validationError, 'Attenzione');
            }
            else {
                $scope.isToChangePsw = true;
                jQuery(".changePasswordParagraph").css("display", "block");

                if (validationResult.completeSecretQuestion) {
                    $scope.isToCompleteSecretQuestion = true;
                    $scope.isToCompleteSecretAnswer = true;
                }

                $scope.validate = function (params) {

                    var result = params.validationGroup.validate();

                    if (result.isValid) {
                        jQuery.ajax({
                            type: "POST",
                            cache: false,
                            url: "LoginPage.aspx/UpdateMail",
                            data: JSON.stringify({
                                userName: form.userName.option("value"),
                                passWord: form.passWord.option("value"),
                                newPassWord: form.newPassWord.option("value"),
                                secretQuestion: form.secretQuestion.option("value"),
                                secretAnswer: form.secretAnswer.option("value")
                            }),
                            contentType: "application/json; charset=utf-8",
                            dataType: "json",
                            success: function (result) {

                                let validationResult = JSON.parse(result.d);
                                $scope.afterPswChangeValidation(validationResult);
                            },
                            error: function (a, b, c) { console.log(a); console.log(b); console.log(c); }
                        })
                    }
                };

                $scope.buttonLoginText = "Modifica";

                form.newPassWord.element().dxValidator({ name: 'newPassword', validationRules: [{ type: 'required', message: 'Il campo PASSWORD è obbligatorio!' }] });
                //form.secretQuestion.element().dxValidator({ name: 'newPassword', validationRules: [{ type: 'required', message: 'Il campo DOMANDA SEGRETA è obbligatorio!' }] });
                //form.secretAnswer.element().dxValidator({ name: 'newPassword', validationRules: [{ type: 'required', message: 'Il campo RISPOSTA SEGRETA è obbligatorio!' }] });

                DevExpress.ui.notify(validationResult.validationError, "warning", 5000);

            }
        }
    }

    //Callback di validazione per la modifica della password
    $scope.afterPswChangeValidation = function (validationResult) {

        if (Object.keys(validationResult.formErrors).length > 0) {

            validationResult.formErrors.forEach((item) => {
                form[item.field].element().dxValidator("instance").option("validationRules").push({ type: 'custom', message: item.message, reevaluate: true, validationCallback: function (options) { return false; } });
            });

            validationResult.formErrors.forEach((item) => {
                form[item.field].element().dxValidator("instance").option("validationRules").pop();
            });
        } else if (validationResult.validationError) {
            DevExpress.ui.notify(validationResult.validationError, "error", 4000);
        }
        else {
            DevExpress.ui.dialog.alert("Password modificata con successo, confermare per procedere alla pagina di login", 'Attenzione').done(function () { window.location.reload(); });
        }
    }

    //Valida il dxValidationGroup
    $scope.validate = function (params) {
        console.log(params);
        var result = params != undefined ? params.validationGroup.validate() : _validationGroup.validate();
        if (result.isValid) {

            var request = {
                userName: form.userName.option("value"),
                passWord: form.passWord.option("value"),
                doubleCheck: false
            }

            if ($scope.enableDoubleCheck) {
                request.doubleCheck = $scope.isSuperUserRequired;
                request.superUserName = form.superUserName.option("value");
                request.superUserPassword = form.superUserPassword.option("value");
            }

            jQuery.ajax({
                type: "POST",
                cache: false,
                url: "LoginPage.aspx/ValidateLogin",
                data: JSON.stringify({
                    loginRequest: request
                }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (result) {
                    let validationResult = JSON.parse(result.d);
                    $scope.afterLoginValidation(validationResult);
                },
                error: function (a, b, c) { console.log("Fail"); console.log(a); console.log(b); console.log(c); }
            })
        }

    };


});

