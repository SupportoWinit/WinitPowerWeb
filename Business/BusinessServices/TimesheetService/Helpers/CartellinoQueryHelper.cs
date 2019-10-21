using Business.BusinessClasses.CartellinoServiceDTOs;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.BusinessServices.TimesheetService.Helpers
{
    /// <summary>
    /// Classe contente i metodi(extensions) per evitare di dilungarsi con linq nella parte business ed ottenere un codice leggibile
    /// (che era il problema del cartellino precedente)
    /// </summary>
    public static class CartellinoQueryHelper
    {

        internal static IEnumerable<CartellinoRegV> OreLavorate(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.Where(reg => (reg.Registrazione_Tipo_Reg == RegTypeEnum.None || reg.Registrazione_Tipo_Reg == RegTypeEnum.Duration) && reg.Motivazione_Reg_Id == null).ToList();
        }

        internal static IEnumerable<CartellinoRegV> OreMotivate(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.Where(reg => reg.Motivazione_Reg_Id != null).ToList();
        }

        internal static IEnumerable<CartellinoRegV> OreViaggi(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.Where(reg => reg.Registrazione_Tipo_Reg == RegTypeEnum.Trip).ToList();
        }

        internal static IEnumerable<CartellinoRegV> OreRettificheAutomatiche(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.Where(reg => reg.Registrazione_Tipo_Reg == RegTypeEnum.RettTimesheet).ToList();

        }

        internal static IEnumerable<CartellinoRegV> OreRettificheManuali(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.Where(reg => reg.Registrazione_Tipo_Reg == RegTypeEnum.RettTimeSheetManual).ToList();

        }

        internal static IEnumerable<CartellinoRegV> OreArrotondamenti(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.Where(reg => reg.Registrazione_Tipo_Reg == RegTypeEnum.ArrotDur).ToList();
        }
        
        internal static IDictionary<DateTime,TimeSpan> PerGiornoConDurata(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.GroupBy(reg => reg.Data_Reg)
                            .ToDictionary(reg => reg.Key, reg => TimeSpan.FromMinutes(reg.Sum(r => r.Durata_Fig)));
        }

        internal static IEnumerable<MotivazioneConRegistrazioniConDurata>  PerMotivazionePerGiornoConDurata(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.GroupBy(reg => reg.Motivazione_Reg_Id)
                               .Select(reg => new MotivazioneConRegistrazioniConDurata
                               {
                                   Motivation_Id = (int)reg.Key,
                                   RegistrazioniPerDataConDurata = reg.GroupBy(c => c.Data_Reg)
                                                   .ToDictionary(key => key.Key, value => TimeSpan.FromMinutes(value.Sum(r => r.Durata_Fig)))

                               })
                               .ToList();
        }

        internal static ILookup<CollaboratoreConDescrizione, CartellinoRegV> RaggruppatePerCollaboratore(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.ToLookup(regv => new CollaboratoreConDescrizione
            {
                Col_Id = (int)regv.Col_Id,
                Col_Desc = regv.Col_Desc
            }, regv => regv);
        }

        internal static ILookup<CantiereConDescrizione, CartellinoRegV> RaggruppatePerCantiere(this IEnumerable<CartellinoRegV> regs)
        {
            return regs.ToLookup(regv => new CantiereConDescrizione
            {
                Cant_Id = (int)regv.Cant_Id,
                Cant_Mnemonic = regv.Cant_Mnemonic
            }, regv => regv);
        }
    }
}
