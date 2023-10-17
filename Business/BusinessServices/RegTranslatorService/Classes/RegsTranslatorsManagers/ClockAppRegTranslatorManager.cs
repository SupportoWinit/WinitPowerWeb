using Business.BusinessServices.RegTranslatorService.Classes.JsonReg;
using Business.BusinessServices.RegTranslatorService.Enums;
using Business.BusinessServices.RegTranslatorService.Helpers;
using Business.BusinessServices.RegTranslatorService.Interfaces.RegsTranslatorsManagers;
using Business.Repository;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.BusinessServices.RegTranslatorService.Classes.RegsTranslatorsManagers
{
    /// <summary>
    /// Manager per la traduzione da app reg in linee formato txt
    /// </summary>
    /// <seealso cref="Business.BusinessServices.RegTranslatorService.Interfaces.RegsTranslatorsManagers.IRegTranslatorManager" />
    internal class ClockAppRegTranslatorManager : IRegTranslatorManager
    {
        IDictionary<JsonRegTypeEnum, Func<ClockAppReg, IEnumerable<string>>> _traslationProcedures;

        public ClockAppRegTranslatorManager()
        {
            _traslationProcedures = new Dictionary<JsonRegTypeEnum, Func<ClockAppReg, IEnumerable<string>>>()
            {
                { JsonRegTypeEnum.M, TranslateJsonOfType_N },
                { JsonRegTypeEnum.N, TranslateJsonOfType_N },
                { JsonRegTypeEnum.G, TranslateJsonOfType_G },
                { JsonRegTypeEnum.Q, TranslateJsonOfType_Q },
                { JsonRegTypeEnum.NG, TranslateJsonOfType_NG },
                { JsonRegTypeEnum.QG, TranslateJsonOfType_QG }
            };
        }


        /// <summary>
        /// Parsa le registrazioni in formato stringa per la scrittura su txt.
        /// </summary>
        /// <param name="jsonRegs">Le reg da parsare.</param>
        /// <returns></returns>
        public IEnumerable<string> Translate(IEnumerable<ClockAppReg> jsonRegs)
        {
            List<string> lines = new List<string>();

            foreach (var reg in jsonRegs)
            {
                JsonRegTypeEnum transponderType = reg.TransponderType;

                if (reg.RegistrationSquadra != null && reg.RegistrationSquadra.Length > 0) //Registrazioni di squadra
                {
                    foreach (var badgeCode in reg.RegistrationSquadra)
                    {
                        if (reg.TransponderType == JsonRegTypeEnum.G)
                            reg.DeviceCode = badgeCode;
                        else
                            reg.BadgeCode = badgeCode;

                        IEnumerable<string> currentMemberLines = _traslationProcedures[transponderType](reg);
                        lines.AddRange(currentMemberLines);
                    }
                }
                else //Registrazioni singole
                {
                    IEnumerable<string> regLines = _traslationProcedures[transponderType](reg);
                    lines.AddRange(regLines);
                }
            }
            //string blankLine = "";
            //lines.Add(blankLine);
            return lines;
        }

        #region NFC

        /// <summary>
        /// Traduzione reg di tipo NFC.
        /// </summary>
        /// <param name="reg">The reg.</param>
        /// <returns></returns>
        IEnumerable<string> TranslateJsonOfType_N(ClockAppReg reg)
        {
            List<string> currentRegLines = new List<string>();

            if (reg.IdManualCant != default)
            {
                var fruAss = RepoManager.Fru_CantRepo.DbSet
                                                     .Where(fc => fc.Cant_Id == reg.IdManualCant && fc.Abilitazione_Data_Inizio_Fru_Can <= reg.RegistrationDateTime)
                                                     .OrderByDescending(fru => fru.Abilitazione_Data_Inizio_Fru_Can)
                                                     .Select(fc => fc.Fru)
                                                     .FirstOrDefault();

                if (fruAss == null)
                {
                    throw new InvalidOperationException("nessuna unità fissa associata!");
                }

                reg.DeviceCode = fruAss.Codice_Fru;
            }


            var nfcLines = ClockAppStringFormatter.CreateNfcOrQrCodeLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction);
            var activityLines = ClockAppStringFormatter.CreateActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.ActivityTypeCode);
            var pruCodeActivityLines = ClockAppStringFormatter.CreatePruCodeActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.PruCodeForActivity);
            var turnLines = ClockAppStringFormatter.CreateTurnLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.TurnCode);
            var subCantLines = ClockAppStringFormatter.CreateSubCantLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.SubCantCode, reg.SubCantDesc);

            currentRegLines.AddRange(nfcLines);
            currentRegLines.AddRange(activityLines);
            currentRegLines.AddRange(pruCodeActivityLines);
            currentRegLines.AddRange(turnLines);
            currentRegLines.AddRange(subCantLines);

            return currentRegLines;
        }

        #endregion

        #region GPS

        /// <summary>
        /// Traduzione reg di tipo GPS.
        /// </summary>
        /// <param name="reg">The reg.</param>
        /// <returns></returns>
        private IEnumerable<string> TranslateJsonOfType_G(ClockAppReg reg)
        {
            List<string> currentRegLines = new List<string>();

            var gpsLines = ClockAppStringFormatter.CreateGpsLines(reg.DeviceCode, reg.Latitude, reg.Longitude, reg.RegistrationDateTime);
            var firstActLine = ClockAppStringFormatter.CreateFirstActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.PruCodeForActivity);
            var activityLines = ClockAppStringFormatter.CreateActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.ActivityTypeCode);
            var pruCodeActivityLines = ClockAppStringFormatter.CreatePruCodeActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.PruCodeForActivity);
            var turnLines = ClockAppStringFormatter.CreateTurnLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.TurnCode);
            var subCantLines = ClockAppStringFormatter.CreateSubCantLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.SubCantCode, reg.SubCantDesc);

            currentRegLines.AddRange(gpsLines);
            currentRegLines.Add(firstActLine);
            currentRegLines.AddRange(activityLines);
            currentRegLines.AddRange(pruCodeActivityLines);
            currentRegLines.AddRange(turnLines);
            currentRegLines.AddRange(subCantLines);

            return currentRegLines;
        }

        #endregion

        #region QRCODE

        /// <summary>
        /// Traduzione reg di tipo QRCODE.
        /// </summary>
        /// <param name="reg">The reg.</param>
        /// <returns></returns>
        private IEnumerable<string> TranslateJsonOfType_Q(ClockAppReg reg)
        {
            List<string> currentRegLines = new List<string>();

            var qrCodeLines = ClockAppStringFormatter.CreateNfcOrQrCodeLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction);
            var activityLines = ClockAppStringFormatter.CreateActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.ActivityTypeCode);
            var pruCodeActivityLines = ClockAppStringFormatter.CreatePruCodeActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.PruCodeForActivity);
            var turnLines = ClockAppStringFormatter.CreateTurnLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.TurnCode);
            var subCantLines = ClockAppStringFormatter.CreateSubCantLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.SubCantCode, reg.SubCantDesc);

            currentRegLines.AddRange(qrCodeLines);
            currentRegLines.AddRange(activityLines);
            currentRegLines.AddRange(pruCodeActivityLines);
            currentRegLines.AddRange(turnLines);
            currentRegLines.AddRange(subCantLines);

            return currentRegLines;
        }

        #endregion

        #region NFC + GPS

        /// <summary>
        /// Traduzione reg di tipo NFC+GPS.
        /// </summary>
        /// <param name="reg">The reg.</param>
        /// <returns></returns>
        private IEnumerable<string> TranslateJsonOfType_NG(ClockAppReg reg)
        {
            List<string> currentRegLines = new List<string>();

            var nfcLines = ClockAppStringFormatter.CreateNfcGLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime);
            var gpsLines = ClockAppStringFormatter.CreateGpsNLines(reg.DeviceCode, reg.Latitude, reg.Longitude, reg.RegistrationDateTime);
            var pruCodeActivityLines = ClockAppStringFormatter.CreatePruCodeActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.PruCodeForActivity);
            var turnLines = ClockAppStringFormatter.CreateTurnLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.TurnCode);
            var subCantLines = ClockAppStringFormatter.CreateSubCantLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.SubCantCode, reg.SubCantDesc);

            currentRegLines.AddRange(nfcLines);
            currentRegLines.AddRange(gpsLines);
            currentRegLines.AddRange(pruCodeActivityLines);
            currentRegLines.AddRange(turnLines);
            currentRegLines.AddRange(subCantLines);

            return currentRegLines;
        }

        #endregion

        #region QRCODE + GPS


        /// <summary>
        /// Traduzione reg di tipo QRCODE+GPS.
        /// </summary>
        /// <param name="reg">The reg.</param>
        /// <returns></returns>
        private IEnumerable<string> TranslateJsonOfType_QG(ClockAppReg reg)
        {
            List<string> currentRegLines = new List<string>();

            var qrcodeLines = ClockAppStringFormatter.CreateNfcLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime);
            var gpsLines = ClockAppStringFormatter.CreateGpsLines(reg.DeviceCode, reg.Latitude, reg.Longitude, reg.RegistrationDateTime);
            var pruCodeActivityLines = ClockAppStringFormatter.CreatePruCodeActivityLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.PruCodeForActivity);
            var turnLines = ClockAppStringFormatter.CreateTurnLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.TurnCode);
            var subCantLines = ClockAppStringFormatter.CreateSubCantLines(reg.DeviceCode, reg.BadgeCode, reg.RegistrationDateTime, reg.Direction, reg.SubCantCode, reg.SubCantDesc);

            currentRegLines.AddRange(qrcodeLines);
            currentRegLines.AddRange(gpsLines);
            currentRegLines.AddRange(pruCodeActivityLines);
            currentRegLines.AddRange(turnLines);
            currentRegLines.AddRange(subCantLines);

            return currentRegLines;
        }

        #endregion

    }
}
