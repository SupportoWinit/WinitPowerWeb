using Business.BusinessServices.RegTranslatorService.Classes.JsonReg;
using Business.Repository;
using Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace PowerWeb.Api
{
    public class SolarisController : ApiController
    {

        [HttpPost]
        public HttpResponseMessage Post(IEnumerable<SolarisReg> solarisRegs)
        {
            var response = new HttpResponseMessage();

            if (!solarisRegs.Any())
                return response;

            var matriculations = solarisRegs.Select(x => x.Matriculation).ToList();
            var prus = RepoManager.PruRepo.DbSet.Where(pru => matriculations.Contains(pru.Codice_Pru.Trim())).ToDictionary(x => x.Codice_Pru.Trim());


            var regs = new List<Reg>();
            var malformedRegs = new List<SolarisReg>();

            foreach (var solarisReg in solarisRegs)
            {
                Pru pru;


                if (!prus.TryGetValue(solarisReg.Matriculation, out pru))
                {
                    //Add reg To backuplist and continue
                    malformedRegs.Add(solarisReg);
                    continue;
                }

                var powerWebReg = new Reg
                {
                    DataOraUltimaModifica_Reg = DateTime.Now,
                    Data_Registrazione_Reg = DateTime.Now,
                    Cant_Id = solarisReg.Cant,
                    Pru = pru,
                    Flag_EU_Reg = solarisReg.Verso,
                    Registrazione_Data_Ora_Orig_Reg = solarisReg.Date,
                    Registrazione_Data_Ora_Fig_Reg = solarisReg.Date,
                    Registrazione_Data_Ora_Fis_Reg = solarisReg.Date,
                    CentroDiCosto = RepoManager.CentroDiCostoRepo.DbSet.FirstOrDefault(x => x.CentroDiCosto_Id == solarisReg.IdCentroDiCosto)
                };

                RepoManager.RegRepo.SetEntityBeforeAddOrUpdate(powerWebReg);

                regs.Add(powerWebReg);
            }

            RepoManager.RegRepo.DbSet.AddRange(regs);

            RepoManager.RegRepo.Context.SaveChanges();

            if (malformedRegs.Any())
            {
                var filesInput = Common.Properties.Settings.Default.Files_Input_Path;

                var fullPath = System.Web.HttpContext.Current.Server.MapPath(filesInput);

                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                fullPath = Path.Combine(fullPath, $"Solaris_Reg_Sospese_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}");

                File.WriteAllText(fullPath, JsonConvert.SerializeObject(malformedRegs));

            }

            if (!regs.Any())
                return response;

            var min = regs.Min(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date;
            var max = regs.Max(reg => reg.Registrazione_Data_Ora_Fis_Reg).Date.AddDays(1);

            //Aggiungere collaboratore
            var regsToElaborate = RepoManager.RegRepo.DbSet
                                                     .Where(reg => reg.Registrazione_Data_Ora_Fis_Reg >= min
                                                                && reg.Registrazione_Data_Ora_Fis_Reg < max)
                                                     .ToList();

            var userId = RepoManager.UtentiRepo.DbSet.First().Utenti_Id;


            RepoManager.RegRepo.Elaborate(regsToElaborate, min, max, elaborateUserId: userId, isToAssociatePruFru: true);

            return response;
        }
    }
}